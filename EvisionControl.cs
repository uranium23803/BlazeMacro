using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace BlazeMacro;

// Restricted implementation for the observed 320F:5084 / bcdDevice 0101, profile 0.
// Only the 8-byte lighting header is writable. No key maps, firmware or per-key LED tables.
public static class EvisionControl
{
    public sealed record Snapshot(EvisionLighting.Reading Reading, string CapabilitiesHex)
    {
        public byte[] Header => Convert.FromHexString(Reading.ResponseHex).AsSpan(9, 8).ToArray();
    }
    public sealed record Applied(Snapshot Before, Snapshot After, string BackupFile);
    public static byte[] Packet(byte command, byte[]? data = null)
    {
        if (command is not (1 or 2 or 3 or 5 or 6)) throw new InvalidDataException("Команда не разрешена.");
        if (command == 6) ValidateHeader(data ?? []);
        else if (data != null) throw new InvalidDataException("Данные допустимы только для заголовка подсветки.");
        var packet = new byte[64]; packet[0] = 4; packet[3] = command;
        packet[4] = command switch { 3 => 7, 5 => 9, 6 => 8, _ => 0 };
        if (command == 6) { packet[5] = 1; data!.CopyTo(packet, 8); }
        ushort sum = (ushort)packet.AsSpan(3).ToArray().Sum(x => (int)x);
        packet[1] = (byte)sum; packet[2] = (byte)(sum >> 8);
        return packet;
    }
    public static void ValidateHeader(byte[] header)
    {
        if (header.Length != 8 || header[0] is < 1 or > 18 || header[1] > 4 || header[2] > 5 || header[3] > 3 || header[4] is not (0 or 255))
            throw new InvalidDataException("Неподдерживаемые параметры подсветки. Запись отменена.");
    }
    public static byte[] Desired(Snapshot current, int mode, int brightness, int speed, bool random, Color color)
    {
        if (mode is < 1 or > 18 || brightness is < 0 or > 4 || speed is < 0 or > 5) throw new InvalidDataException("Параметры за пределами диапазона.");
        var result = current.Header; result[0] = (byte)mode; result[1] = (byte)brightness; result[2] = (byte)speed;
        result[4] = random ? (byte)255 : (byte)0; result[5] = color.R; result[6] = color.G; result[7] = color.B;
        ValidateHeader(result); return result;
    }
    public static async Task<Snapshot> Read(CancellationToken token)
    {
        await EvisionLighting.Gate.WaitAsync(token);
        try { using var session = new Session(); return await session.Read(token); }
        finally { EvisionLighting.Gate.Release(); }
    }
    public static async Task<Applied> Apply(Snapshot expected, byte[] desired, CancellationToken token)
    {
        ValidateHeader(desired);
        await EvisionLighting.Gate.WaitAsync(token);
        try
        {
            using var session = new Session();
            var before = await session.Read(token);
            if (!before.Header.SequenceEqual(expected.Header)) throw new IOException("Подсветка изменилась после чтения. Нажмите «Прочитать» и задайте настройки заново.");
            if (before.Header.SequenceEqual(desired)) return new Applied(before, before, "Изменений нет — запись не выполнялась.");
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlazeMacro", "LightingBackups");
            Directory.CreateDirectory(directory);
            var backup = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(backup, JsonSerializer.Serialize(new { Before = before, RequestedHeaderHex = Convert.ToHexString(desired) }, new JsonSerializerOptions { WriteIndented = true }));
            bool begun = false;
            Exception? failure = null;
            try
            {
                begun = true; // End is also needed when the begin acknowledgement is lost.
                await session.Exchange(Packet(1), token);
                var inside = EvisionLighting.Parse(await session.Exchange(Packet(5), token), session.Release);
                if (inside.CandidateProfile != 0 || !Convert.FromHexString(inside.ResponseHex).AsSpan(9, 8).SequenceEqual(before.Header))
                    throw new IOException("Параметры изменились во время подготовки. Заголовок не записан.");
                await session.Exchange(Packet(6, desired), token);
            }
            catch (Exception e) { failure = e; }
            finally
            {
                // Close the configuration transaction even if the write failed or cancellation was requested.
                if (begun) { try { await session.Exchange(Packet(2), CancellationToken.None); } catch (Exception e) { failure = failure == null ? e : new AggregateException(failure, e); } }
            }
            if (failure != null) throw new IOException("Операция не подтверждена: " + failure.Message + " Резервная копия: " + backup, failure);
            var after = await session.Read(CancellationToken.None);
            if (!after.Header.SequenceEqual(desired)) throw new IOException("Чтение после записи не подтвердило настройки. Повторная запись не выполнялась. Резервная копия: " + backup);
            return new Applied(before, after, backup);
        }
        finally { EvisionLighting.Gate.Release(); }
    }
    sealed class Session : IDisposable
    {
        readonly FileStream stream;
        public string Release { get; }
        public Session()
        {
            var all = HidDiagnostics.Scan().Collections.Where(c => c.UsbId == "320F:5084" && c.Interface == "MI_01 / COL04" && c.UsagePage == "0xFF1C" && c.Usage == "0x0092" && c.InputBytes == 64 && c.OutputBytes == 64 && c.Error == null && c.Reports.Any(r => r.Type == "Input" && r.ReportId == 4) && c.Reports.Any(r => r.Type == "Output" && r.ReportId == 4)).ToList();
            if (all.Count != 1 || all[0].DeviceReleaseBcd != "0x0101") throw new IOException("Эта версия прямого управления рассчитана на одну DEXP Blaze 320F:5084, bcdDevice 0101.");
            Release = all[0].DeviceReleaseBcd;
            var handle = CreateFile(all[0].DevicePath, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            try
            {
                if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (!HidD_FlushQueue(handle)) throw new Win32Exception(Marshal.GetLastWin32Error());
                stream = new FileStream(handle, FileAccess.ReadWrite, 64, true);
            }
            catch { handle.Dispose(); throw; }
        }
        public async Task<byte[]> Exchange(byte[] packet, CancellationToken token)
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token); limit.CancelAfter(2000);
            try
            {
                await stream.WriteAsync(packet, limit.Token);
                var answer = new byte[64];
                for (int i = 0; i < 8; i++)
                {
                    var count = await stream.ReadAsync(answer, limit.Token);
                    if (count == 0) throw new IOException("Устройство отключено.");
                    if (count != 64 || answer[0] != 4 || answer[1] != packet[1] || answer[2] != packet[2] || answer[3] != packet[3] || answer[5] != packet[5] || answer[6] != packet[6]) continue;
                    if (answer[7] != 0) throw new IOException($"Клавиатура отклонила команду 0x{packet[3]:X2}, статус 0x{answer[7]:X2}.");
                    if (answer[4] != packet[4]) throw new InvalidDataException("Неожиданная длина ответа устройства.");
                    return answer;
                }
                throw new IOException("Не получен ответ на команду.");
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new TimeoutException("Тайм-аут HID. Результат отправленной команды не подтверждён; автоматического повтора нет."); }
        }
        public async Task<Snapshot> Read(CancellationToken token)
        {
            var capabilities = await Exchange(Packet(3), token);
            if (capabilities[8] != 0xAA || capabilities[9] != 0x55) throw new InvalidDataException("Сигнатура EVision V2 не подтверждена.");
            var answer = await Exchange(Packet(5), token);
            var reading = EvisionLighting.Parse(answer, Release);
            if (reading.CandidateProfile != 0 || !reading.Layout2Profile0.Plausible) throw new InvalidDataException("Расположение параметров не подтверждено. Запись недоступна.");
            ValidateHeader(answer.AsSpan(9, 8).ToArray());
            return new Snapshot(reading, Convert.ToHexString(capabilities));
        }
        public void Dispose() => stream.Dispose();
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("hid.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_FlushQueue(SafeFileHandle handle);
}
