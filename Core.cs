using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazeMacro;

public enum StepKind { Tap, Down, Up, Wait }
public sealed class Step
{
    public StepKind Kind { get; set; }
    public int Key { get; set; } = (int)Keys.A;
    public int Milliseconds { get; set; } = 50;
    public uint ScanCode { get; set; }
    public bool Extended { get; set; }
    [JsonIgnore] public string Action => Kind switch { StepKind.Tap => "Нажать и отпустить", StepKind.Down => "Зажать", StepKind.Up => "Отпустить", _ => "Пауза" };
    [JsonIgnore] public string KeyName => Kind == StepKind.Wait ? "—" : KeyNames.Name(Key);
    [JsonIgnore] public string Duration => Kind is StepKind.Wait or StepKind.Tap ? $"{Milliseconds} мс" : "—";
    public Step Copy() => (Step)MemberwiseClone();
}

public sealed class Macro
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Мой макрос";
    public int Trigger { get; set; } = (int)Keys.F6;
    public int Repeats { get; set; } = 1;
    public int RepeatDelay { get; set; } = 200;
    public TimingMode Timing { get; set; } = TimingMode.Standard;
    public List<Step> Steps { get; set; } = [];
    public void Validate(bool forPlayback = false)
    {
        if (Version is not (1 or 2 or 3)) throw new InvalidDataException("Эта версия файла не поддерживается.");
        if (!Enum.IsDefined(Timing)) throw new InvalidDataException("Неизвестный режим точности пауз.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 100) throw new InvalidDataException("Название должно содержать от 1 до 100 символов.");
        if (!KeyNames.IsTrigger(Trigger)) throw new InvalidDataException("Недопустимая клавиша запуска. F9 зарезервирована для остановки.");
        if (Repeats is < 1 or > 1000 || RepeatDelay is < 0 or > 600000) throw new InvalidDataException("Недопустимые параметры повторения.");
        if (Steps == null || Steps.Count > 20000) throw new InvalidDataException("Слишком много шагов (максимум 20 000).");
        if (forPlayback && Steps.Count == 0) throw new InvalidDataException("Добавьте хотя бы один шаг.");
        var held = new HashSet<int>();
        foreach (var s in Steps)
        {
            if (s == null || !Enum.IsDefined(s.Kind) || s.Milliseconds is < 0 or > 600000 || s.ScanCode > 255 || (s.Kind != StepKind.Wait && !KeyNames.IsInput(s.Key)))
                throw new InvalidDataException("Файл содержит недопустимый шаг.");
            if (!forPlayback || s.Kind == StepKind.Wait) continue;
            if (s.Kind == StepKind.Down) held.Add(s.Key);
            if (s.Kind == StepKind.Up && !held.Remove(s.Key)) throw new InvalidDataException($"Перед отпусканием {s.KeyName} добавьте зажатие.");
            if (s.Kind == StepKind.Tap && held.Contains(s.Key)) throw new InvalidDataException($"Клавиша {s.KeyName} уже зажата. Сначала отпустите её.");
        }
        if (forPlayback && held.Count != 0) throw new InvalidDataException("Добавьте отпускание всех зажатых клавиш в конце последовательности.");
    }
}

public static class MacroFile
{
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static Macro Load(string path)
    {
        if (new FileInfo(path).Length > 8_000_000) throw new InvalidDataException("Файл слишком большой.");
        var m = JsonSerializer.Deserialize<Macro>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Пустой файл.");
        m.Validate(); return m;
    }
    public static void Save(string path, Macro macro)
    {
        macro.Validate();
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(macro, Options)); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

public static class KeyNames
{
    public static readonly int[] MouseKeys = [1, 2, 4, 5, 6];
    public static bool IsMouse(int key) => key is 1 or 2 or 4 or 5 or 6;
    public static bool IsInput(int key) => IsKey(key) || IsMouse(key);
    public static bool IsKey(int key) => key is >= 8 and <= 254 && Enum.IsDefined(typeof(Keys), key);
    public static bool IsTrigger(int key) => IsKey(key) && key != (int)Keys.F9 && key is not (16 or 17 or 18 or 91 or 92 or 160 or 161 or 162 or 163 or 164 or 165);
    public static string Name(int key) => key switch
    {
        1 => "Мышь: ЛКМ", 2 => "Мышь: ПКМ", 4 => "Мышь: средняя", 5 => "Мышь: X1", 6 => "Мышь: X2",
        13 => "Enter", 27 => "Esc", 32 => "Space", 8 => "Backspace", 9 => "Tab", 46 => "Delete",
        160 => "Left Shift", 161 => "Right Shift", 162 => "Left Ctrl", 163 => "Right Ctrl", 164 => "Left Alt", 165 => "Right Alt",
        >= 48 and <= 57 => ((char)key).ToString(), _ => ((Keys)key).ToString()
    };
    public static int[] All => Enum.GetValues<Keys>().Select(k => (int)k).Where(IsKey).Distinct().ToArray();
}

public sealed class MouseHook : IDisposable
{
    readonly Native.HookProc proc;
    readonly IntPtr handle;
    public Func<KeyEvent, Point, bool>? Handler { get; set; }
    public MouseHook()
    {
        proc = Callback;
        handle = Native.SetWindowsHookEx(14, proc, Native.GetModuleHandle(null), 0);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public static KeyEvent? Decode(int message, uint data, uint flags)
    {
        int key = message switch { 0x201 or 0x202 => 1, 0x204 or 0x205 => 2, 0x207 or 0x208 => 4,
            0x20B or 0x20C => (data >> 16) switch { 1 => 5, 2 => 6, _ => 0 }, _ => 0 };
        return key == 0 ? null : new KeyEvent(key, 0, false, message is 0x202 or 0x205 or 0x208 or 0x20C, (flags & 1) != 0);
    }
    IntPtr Callback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var m = Marshal.PtrToStructure<Native.MouseHookData>(data);
            var e = Decode((int)message, m.data, m.flags);
            try { if (e.HasValue && Handler?.Invoke(e.Value, new Point(m.x, m.y)) == true) return (IntPtr)1; } catch { }
        }
        return Native.CallNextHookEx(handle, code, message, data);
    }
    public void Dispose() { Native.UnhookWindowsHookEx(handle); GC.KeepAlive(proc); }
}

public readonly record struct KeyEvent(int Key, uint ScanCode, bool Extended, bool Up, bool Injected);
public sealed class KeyboardHook : IDisposable
{
    readonly Native.HookProc proc;
    readonly IntPtr handle;
    public Func<KeyEvent, bool>? Handler { get; set; }
    public KeyboardHook()
    {
        proc = Callback;
        handle = Native.SetWindowsHookEx(13, proc, Native.GetModuleHandle(null), 0);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    IntPtr Callback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var k = Marshal.PtrToStructure<Native.HookData>(data);
            var e = new KeyEvent((int)k.vkCode, k.scanCode, (k.flags & 1) != 0, (k.flags & 128) != 0, (k.flags & 16) != 0);
            try { if (Handler?.Invoke(e) == true) return (IntPtr)1; } catch { /* Never unwind through native callback. */ }
        }
        return Native.CallNextHookEx(handle, code, message, data);
    }
    public void Dispose() { Native.UnhookWindowsHookEx(handle); GC.KeepAlive(proc); }
}

public static class Native
{
    public delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] public struct HookData { public uint vkCode, scanCode, flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] public struct MouseHookData { public int x, y; public uint data, flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct KeyboardInput { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct MouseInput { public int x, y; public uint data, flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public KeyboardInput keyboard; [FieldOffset(0)] public MouseInput mouse; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint type; public InputUnion union; }
    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint count, Input[] input, int size);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code, uint type);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point point);
    public static bool OwnPoint(Point point)
    {
        GetWindowThreadProcessId(WindowFromPoint(point), out var process);
        return process == Environment.ProcessId;
    }
    public static bool OwnForeground()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var process);
        return process == Environment.ProcessId;
    }
    public static void Send(Step step, bool up)
    {
        if (KeyNames.IsMouse(step.Key))
        {
            var (flags, data) = MousePacket(step.Key, up);
            var mouse = new Input { type = 0, union = new InputUnion { mouse = new MouseInput { flags = flags, data = data } } };
            if (SendInput(1, [mouse], Marshal.SizeOf<Input>()) != 1)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows отклонила нажатие мыши.");
            return;
        }
        var mapped = MapVirtualKey((uint)step.Key, 4);
        var scan = step.ScanCode != 0 ? step.ScanCode : mapped & 255;
        var extended = step.ScanCode != 0 ? step.Extended : (mapped & 0xff00) == 0xe000;
        // Pause uses a special multi-byte scan sequence; virtual-key injection handles it.
        if (step.Key == (int)Keys.Pause) scan = 0;
        var input = new Input { type = 1, union = new InputUnion { keyboard = new KeyboardInput {
            vk = scan == 0 ? (ushort)step.Key : (ushort)0, scan = (ushort)scan,
            flags = (scan != 0 ? 8u : 0u) | (up ? 2u : 0u) | (extended && scan != 0 ? 1u : 0u)
        } } };
        if (SendInput(1, [input], Marshal.SizeOf<Input>()) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows отклонила ввод. Проверьте права целевого приложения.");
    }
    public static (uint Flags, uint Data) MousePacket(int key, bool up) => key switch
    {
        1 => (up ? 0x4u : 0x2u, 0), 2 => (up ? 0x10u : 0x8u, 0),
        4 => (up ? 0x40u : 0x20u, 0), 5 => (up ? 0x100u : 0x80u, 1),
        6 => (up ? 0x100u : 0x80u, 2), _ => throw new ArgumentOutOfRangeException(nameof(key))
    };
}

public sealed class Player
{
    readonly Action<Step, bool> send;
    public Player(Action<Step, bool>? sender = null) => send = sender ?? Native.Send;
    public Task Play(Macro macro, CancellationToken token)
    {
        macro.Validate(true);
        return Task.Run(() => PlayCore(macro, token), token);
    }
    async Task PlayCore(Macro macro, CancellationToken token)
    {
        using var clock = new PauseClock(macro.Timing);
        var held = new Dictionary<int, Step>();
        try
        {
            for (var repeat = 0; repeat < macro.Repeats; repeat++)
            {
                foreach (var s in macro.Steps)
                {
                    token.ThrowIfCancellationRequested();
                    switch (s.Kind)
                    {
                        case StepKind.Wait: await clock.Delay(s.Milliseconds, token); break;
                        case StepKind.Down: send(s, false); held[s.Key] = s; break;
                        case StepKind.Up: send(s, true); held.Remove(s.Key); break;
                        case StepKind.Tap:
                            send(s, false); held[s.Key] = s;
                            await clock.Delay(Math.Max(1, s.Milliseconds), token);
                            send(s, true); held.Remove(s.Key); break;
                    }
                }
                if (repeat + 1 < macro.Repeats) await clock.Delay(macro.RepeatDelay, token);
            }
        }
        finally
        {
            Exception? releaseError = null;
            foreach (var s in held.Values.Reverse()) { try { send(s, true); } catch (Exception e) { releaseError = e; } }
            if (releaseError != null) throw releaseError;
        }
    }
}

public sealed class Recorder
{
    readonly Stopwatch clock = new();
    readonly Dictionary<int, KeyEvent> held = [];
    long previous;
    public List<Step> Steps { get; } = [];
    public bool IsFull => Steps.Count >= 19000;
    public void Start() { Steps.Clear(); held.Clear(); previous = 0; clock.Restart(); }
    public void Accept(KeyEvent e)
    {
        if (IsFull) return;
        Append(e);
    }
    void Append(KeyEvent e)
    {
        if (e.Injected || !KeyNames.IsInput(e.Key)) return;
        if (e.Up && !held.ContainsKey(e.Key)) return;
        var now = clock.ElapsedMilliseconds;
        if (Steps.Count != 0 && now > previous) Steps.Add(new Step { Kind = StepKind.Wait, Milliseconds = (int)Math.Min(600000, now - previous) });
        Steps.Add(new Step { Kind = e.Up ? StepKind.Up : StepKind.Down, Key = e.Key, ScanCode = e.ScanCode, Extended = e.Extended });
        if (e.Up) held.Remove(e.Key); else held[e.Key] = e;
        previous = now;
    }
    public void Stop()
    {
        foreach (var e in held.Values.Reverse().ToArray()) Append(e with { Up = true });
        clock.Stop();
    }
}
