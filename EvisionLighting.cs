using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace BlazeMacro;

// Independent implementation of documented EVision wire facts. No third-party RGB runtime.
// This stage only permits command 05, offset 0000, length 9 (compare two documented layouts).
public static class EvisionLighting
{
    public sealed record Header(int Mode, int Brightness, int Speed, int Direction, int RandomColor, string Color, bool Plausible);
    public sealed record Reading(DateTimeOffset TimeUtc, string UsbId, string DeviceRelease, string RequestHex, string ResponseHex,
        Header Layout1, int CandidateProfile, Header Layout2Profile0);
    internal static readonly SemaphoreSlim Gate = new(1, 1);
    public static byte[] ReadRequest()
    {
        var request = new byte[64]; request[0] = 4; request[3] = 5; request[4] = 9;
        request[1] = 14; // Sum of bytes 3..63, little-endian. No begin/end or configuration write.
        return request;
    }
    public static Reading Parse(byte[] response, string release)
    {
        var request = ReadRequest();
        if (response.Length != 64 || !response.AsSpan(0, 7).SequenceEqual(request.AsSpan(0, 7)))
            throw new InvalidDataException("Ответ не соответствует запросу чтения EVision.");
        if (response[7] != 0) throw new IOException($"Клавиатура вернула статус 0x{response[7]:X2}.");
        Header Decode(int offset, bool v2)
        {
            bool plausible = (response[offset] is >= 1 and <= 18 or 20) && response[offset + 1] <= 4 && response[offset + 2] <= 5 && response[offset + 3] <= 3 && (response[offset + 4] <= 1 || v2 && response[offset + 4] == 255);
            return new Header(response[offset], response[offset + 1], response[offset + 2], response[offset + 3], response[offset + 4], $"#{response[offset + 5]:X2}{response[offset + 6]:X2}{response[offset + 7]:X2}", plausible);
        }
        return new Reading(DateTimeOffset.UtcNow, "320F:5084", release, Convert.ToHexString(request), Convert.ToHexString(response), Decode(8, false), response[8], Decode(9, true));
    }
    public static async Task<Reading> Read(CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            var found = HidDiagnostics.Scan().Collections.Where(c => c.UsbId == "320F:5084" && c.Interface == "MI_01 / COL04" && c.UsagePage == "0xFF1C" && c.Usage == "0x0092" && c.InputBytes == 64 && c.OutputBytes == 64 && c.Error == null && c.Reports.Any(r => r.Type == "Output" && r.ReportId == 4)).ToList();
            if (found.Count != 1) throw new IOException("Нужна ровно одна клавиатура 320F:5084 с интерфейсом FF1C:0092 и отчётами по 64 байта.");
            var device = found[0];
            using var handle = CreateFile(device.DevicePath, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!HidD_FlushQueue(handle)) throw new Win32Exception(Marshal.GetLastWin32Error());
            using var stream = new FileStream(handle, FileAccess.ReadWrite, 64, true);
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token); limit.CancelAfter(2000);
            try
            {
                await stream.WriteAsync(ReadRequest(), limit.Token);
                var response = new byte[64];
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int size = await stream.ReadAsync(response, limit.Token);
                    if (size == 0) throw new IOException("Клавиатура закрыла канал.");
                    if (size == 64 && response.AsSpan(0, 7).SequenceEqual(ReadRequest().AsSpan(0, 7))) return Parse(response, device.DeviceReleaseBcd);
                }
                throw new IOException("Не получен ответ на чтение параметров.");
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new TimeoutException("Клавиатура не ответила за 2 секунды. Настройки не изменялись."); }
        }
        finally { Gate.Release(); }
    }
    public static async Task<string> ReadCapabilities(CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            var found = HidDiagnostics.Scan().Collections.Where(c => c.UsbId == "320F:5084" && c.Interface == "MI_01 / COL04" && c.UsagePage == "0xFF1C" && c.Usage == "0x0092" && c.InputBytes == 64 && c.OutputBytes == 64 && c.Error == null).ToList();
            if (found.Count != 1) throw new IOException("Не найдена единственная подходящая клавиатура.");
            using var handle = CreateFile(found[0].DevicePath, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!HidD_FlushQueue(handle)) throw new Win32Exception(Marshal.GetLastWin32Error());
            using var stream = new FileStream(handle, FileAccess.ReadWrite, 64, true);
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token); limit.CancelAfter(2000);
            var request = new byte[64]; request[0] = 4; request[1] = 10; request[3] = 3; request[4] = 7;
            await stream.WriteAsync(request, limit.Token);
            var response = new byte[64];
            int size = await stream.ReadAsync(response, limit.Token);
            if (size != 64 || !request.AsSpan(0, 7).SequenceEqual(response.AsSpan(0, 7)) || response[7] != 0) throw new IOException("Ответ capabilities не соответствует запросу.");
            return Convert.ToHexString(response);
        }
        finally { Gate.Release(); }
    }
    public static async Task SaveReading(string directory)
    {
        Directory.CreateDirectory(directory);
        var reading = await Read(CancellationToken.None);
        File.WriteAllText(Path.Combine(directory, "lighting-read.json"), JsonSerializer.Serialize(reading, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(directory, "capabilities-read.txt"), await ReadCapabilities(CancellationToken.None));
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint sharing, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("hid.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_FlushQueue(SafeFileHandle handle);
}
