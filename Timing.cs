using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace BlazeMacro;

public enum TimingMode { Standard, Resolution1ms, Precision }

public sealed class PauseClock : IDisposable
{
    sealed class TimerHandle : WaitHandle { public TimerHandle(SafeWaitHandle handle) => SafeWaitHandle = handle; }
    TimerHandle? timer;
    bool periodRequested, disposed;
    static int activeRequests;
    public static int ActiveRequests => Volatile.Read(ref activeRequests);
    public string Backend { get; }
    public PauseClock(TimingMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        Backend = "Task.Delay (обычный режим)";
        if (mode == TimingMode.Standard) return;
        try
        {
            var high = mode == TimingMode.Precision;
            var handle = CreateWaitableTimerEx(IntPtr.Zero, null, high ? 2u : 0u, 0x00100002);
            if (handle.IsInvalid && high && Marshal.GetLastWin32Error() is 87 or 50)
            {
                handle.Dispose(); high = false;
                handle = CreateWaitableTimerEx(IntPtr.Zero, null, 0, 0x00100002);
            }
            if (handle.IsInvalid) { var error = Marshal.GetLastWin32Error(); handle.Dispose(); throw new Win32Exception(error); }
            timer = new TimerHandle(handle);
            var result = timeBeginPeriod(1);
            if (result != 0) throw new InvalidOperationException($"Windows отклонила запрос таймера 1 мс (код {result}). Выберите обычный режим.");
            periodRequested = true; Interlocked.Increment(ref activeRequests);
            Backend = high ? "Высокоточный waitable timer + запрос 1 мс" : mode == TimingMode.Precision ? "Резерв: обычный waitable timer + запрос 1 мс" : "Обычный waitable timer + запрос 1 мс";
        }
        catch { Dispose(); throw; }
    }
    // Native waits run only on playback/benchmark worker threads. There is no busy spinning.
    public ValueTask Delay(int milliseconds, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (milliseconds is < 0 or > 600000) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        token.ThrowIfCancellationRequested();
        if (milliseconds == 0) return ValueTask.CompletedTask;
        if (timer == null) return new ValueTask(Task.Delay(milliseconds, token));
        long due = -milliseconds * 10000L;
        if (!SetWaitableTimer(timer.SafeWaitHandle, ref due, 0, IntPtr.Zero, IntPtr.Zero, false)) throw new Win32Exception(Marshal.GetLastWin32Error());
        int signalled = WaitHandle.WaitAny([token.WaitHandle, timer]);
        if (signalled == 0) token.ThrowIfCancellationRequested();
        token.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer?.Dispose(); timer = null;
        if (periodRequested)
        {
            timeEndPeriod(1); periodRequested = false; Interlocked.Decrement(ref activeRequests);
        }
    }
    [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
    [DllImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeWaitHandle CreateWaitableTimerEx(IntPtr attributes, string? name, uint flags, uint access);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long due, int period, IntPtr callback, IntPtr arg, [MarshalAs(UnmanagedType.Bool)] bool resume);
}

public static class TimingBenchmark
{
    public sealed record Row(string Mode, string Backend, int RequestedMs, int Samples, double MeanMs, double P95Ms, double MaxMs, double MinMs)
    {
        public double MeanOvershootMs => MeanMs - RequestedMs;
    }
    public sealed record Report(DateTimeOffset CapturedUtc, string Windows, string Runtime, string Method, List<Row> Rows);
    public static Task<Report> Run(CancellationToken token, IProgress<string>? progress = null) => Task.Run(async () =>
    {
        const int samples = 60;
        var rows = new List<Row>();
        foreach (var mode in Enum.GetValues<TimingMode>())
        {
            using var clock = new PauseClock(mode);
            foreach (int duration in new[] { 1, 5, 10, 20 })
            {
                progress?.Report($"{Name(mode)} · пауза {duration} мс · {samples} измерений");
                for (int warm = 0; warm < 5; warm++) await clock.Delay(duration, token);
                var observed = new double[samples];
                for (int i = 0; i < samples; i++)
                {
                    long start = Stopwatch.GetTimestamp();
                    await clock.Delay(duration, token);
                    observed[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                }
                Array.Sort(observed);
                rows.Add(new Row(Name(mode), clock.Backend, duration, samples, observed.Average(), observed[(int)Math.Ceiling(samples * .95) - 1], observed[^1], observed[0]));
            }
        }
        return new Report(DateTimeOffset.UtcNow, Environment.OSVersion.VersionString, Environment.Version.ToString(),
            "60 sequential waits per duration after 5 warmups; Stopwatch measures each wait. No keyboard/mouse input. Modes run sequentially; load and window visibility affect results. Not an input-latency or game benchmark.", rows);
    }, token);
    public static string Name(TimingMode mode) => mode switch { TimingMode.Standard => "Обычный", TimingMode.Resolution1ms => "Запрос 1 мс", TimingMode.Precision => "Точный", _ => "Неизвестный" };
    public static async Task Save(string directory)
    {
        var result = await Run(CancellationToken.None);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "timing-benchmark.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(directory, "timing-benchmark.txt"), Describe(result));
    }
    public static string Describe(Report report) => "BLAZE — ТОЧНОСТЬ ПАУЗ\r\n" + report.Method + "\r\n" + report.Windows + " / .NET " + report.Runtime + "\r\n\r\n" +
        "Режим | Задано, мс | Среднее | P95 | Максимум | Средняя лишняя задержка\r\n" +
        string.Join("\r\n", report.Rows.Select(r => $"{r.Mode} | {r.RequestedMs} | {r.MeanMs:F3} | {r.P95Ms:F3} | {r.MaxMs:F3} | {r.MeanOvershootMs:F3}")) +
        "\r\n\r\nP95 — 95% замеров не длиннее этого значения. Это длительность ожидания в программе, не задержка клавиатуры, USB или игры.\r\n";
}
