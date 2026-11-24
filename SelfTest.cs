namespace BlazeMacro;

internal static class SelfTest
{
    static readonly List<string> results = [];
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); results.Add("PASS " + message); }
    static void Reject(Action action, string message) { try { action(); } catch (InvalidDataException) { results.Add("PASS " + message); return; } throw new Exception(message); }
    public static void Run(bool nativeInput = false)
    {
        using var form = new Form { Text = "Blaze Macro · проверка ввода", Width = 450, Height = 180, StartPosition = FormStartPosition.CenterScreen };
        var box = new TextBox { Dock = DockStyle.Fill, Multiline = true }; form.Controls.Add(box);
        form.Shown += async (_, _) =>
        {
            try
            {
                var events = new List<(int Key, bool Up)>();
                var m = new Macro { Repeats = 2, RepeatDelay = 0, Steps = [new() { Key = 65, Milliseconds = 1 }] };
                await new Player((s, up) => events.Add((s.Key, up))).Play(m, CancellationToken.None);
                Check(events.SequenceEqual(new[] { (65, false), (65, true), (65, false), (65, true) }), "Tap ordering and repeat count");
                events.Clear();
                using (var cancel = new CancellationTokenSource())
                {
                    var heldMacro = new Macro { Steps = [new() { Kind = StepKind.Down, Key = 162 }, new() { Kind = StepKind.Wait, Milliseconds = 10000 }, new() { Kind = StepKind.Up, Key = 162 }] };
                    var task = new Player((s, up) => { events.Add((s.Key, up)); if (!up) cancel.Cancel(); }).Play(heldMacro, cancel.Token);
                    try { await task; } catch (OperationCanceledException) { }
                    Check(events.SequenceEqual(new[] { (162, false), (162, true) }), "Cancellation releases held Ctrl");
                }
                events.Clear();
                var failing = new Macro { Steps = [new() { Kind = StepKind.Down, Key = 162 }, new() { Key = 65 }, new() { Kind = StepKind.Up, Key = 162 }] };
                try { await new Player((s, up) => { if (s.Key == 65) throw new IOException("simulated failure"); events.Add((s.Key, up)); }).Play(failing, CancellationToken.None); } catch (IOException) { }
                Check(events.SequenceEqual(new[] { (162, false), (162, true) }), "Injection failure releases earlier keys");
                Reject(() => new Macro { Steps = [new() { Kind = StepKind.Up }] }.Validate(true), "Reject orphan key-up");
                Reject(() => new Macro { Steps = [new() { Kind = StepKind.Down }] }.Validate(true), "Reject unbalanced key-down");
                Reject(() => new Macro { Trigger = (int)Keys.F9 }.Validate(), "Reserve F9 for stop");
                Reject(() => new Macro { Steps = [new() { Milliseconds = -1 }] }.Validate(), "Reject negative delay");
                Reject(() => new Macro { Repeats = 1001 }.Validate(), "Bound repeats");
                var path = Path.Combine(Path.GetTempPath(), "blaze-test-" + Guid.NewGuid() + ".json");
                try { MacroFile.Save(path, m); var loaded = MacroFile.Load(path); Check(loaded.Repeats == 2 && loaded.Steps[0].Key == 65, "JSON round trip"); File.WriteAllText(path, "{\"Version\":99}"); Reject(() => MacroFile.Load(path), "Reject unsupported schema"); }
                finally { File.Delete(path); }
                var recorder = new Recorder(); recorder.Start();
                recorder.Accept(new(65, 30, false, false, true));
                recorder.Accept(new(65, 30, false, true, false));
                Check(recorder.Steps.Count == 0, "Recorder ignores injected events and orphan releases");
                recorder.Accept(new(162, 29, false, false, false)); recorder.Accept(new(65, 30, false, false, false)); recorder.Stop();
                var recorded = new Macro { Steps = recorder.Steps }; recorded.Validate(true);
                Check(recorder.Steps.Count(s => s.Kind == StepKind.Up) == 2, "Recorder closes held chord");
                recorder.Start();
                for (var i = 0; i < 25000; i++) recorder.Accept(new(65, 30, false, false, false));
                Check(recorder.IsFull && recorder.Steps.Count <= 19001, "Recorder bounds event flood before UI stop runs");
                recorder.Stop(); new Macro { Steps = recorder.Steps }.Validate(true);
                Check(recorder.Steps.Last().Kind == StepKind.Up && recorder.Steps.Count <= 20000, "Full recorder still releases held keys within file limit");
                using (var editor = new MainForm())
                {
                    const System.Reflection.BindingFlags access = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    object? Invoke(string method, params object?[] args) => typeof(MainForm).GetMethod(method, access)!.Invoke(editor, args);
                    editor.BindingContext = new BindingContext();
                    var source = new Macro { Steps = [new() { Key = 13, ScanCode = 28, Extended = true }] };
                    Invoke("Apply", source, null);
                    editor.CreateControl();
                    var table = (DataGridView)typeof(MainForm).GetField("grid", access)!.GetValue(editor)!;
                    Invoke("SelectRow", 0); Invoke("LoadEditor"); Invoke("UpdateStep");
                    var edited = (Macro)Invoke("Snapshot")!;
                    Check(edited.Steps[0].ScanCode == 28 && edited.Steps[0].Extended, "Editing recorded numpad Enter preserves physical key identity");
                    var large = new Macro { Steps = Enumerable.Range(0, 20000).Select(_ => new Step()).ToList() };
                    Invoke("Apply", large, null);
                    Check(((Macro)Invoke("Snapshot")!).Steps.Count == 20000, "Editor loads maximum-size macro with batched notifications");
                }
                foreach (int button in KeyNames.MouseKeys)
                {
                    events.Clear();
                    await new Player((s, up) => events.Add((s.Key, up))).Play(new Macro { Version = 2, Steps = [new() { Key = button, Milliseconds = 1 }] }, CancellationToken.None);
                    Check(events.SequenceEqual(new[] { (button, false), (button, true) }), $"Mouse button {button} click order");
                    events.Clear();
                    using var cancelMouse = new CancellationTokenSource();
                    try { await new Player((s, up) => { events.Add((s.Key, up)); if (!up) cancelMouse.Cancel(); }).Play(new Macro { Version = 2, Steps = [new() { Key = button, Milliseconds = 10000 }] }, cancelMouse.Token); } catch (OperationCanceledException) { }
                    Check(events.SequenceEqual(new[] { (button, false), (button, true) }), $"Cancellation releases mouse button {button}");
                }
                Reject(() => new Macro { Trigger = 1 }.Validate(), "Mouse button cannot become keyboard trigger");
                Reject(() => new Macro { Version = 2, Steps = [new() { Kind = StepKind.Up, Key = 1 }] }.Validate(true), "Reject orphan mouse release");
                recorder.Start(); recorder.Accept(new(162, 29, false, false, false)); recorder.Accept(new(1, 0, false, false, false)); recorder.Accept(new(2, 0, false, false, true)); recorder.Stop();
                new Macro { Version = 2, Steps = recorder.Steps }.Validate(true);
                Check(recorder.Steps.Count(s => s.Kind == StepKind.Up) == 2 && recorder.Steps.All(s => s.Key != 2), "Mixed keyboard/mouse recording balanced and synthetic clicks ignored");
                try { MacroFile.Save(path, new Macro { Version = 2, Steps = [new() { Key = 6 }] }); Check(MacroFile.Load(path).Steps[0].Key == 6, "Mouse macro schema v2 round trip"); } finally { File.Delete(path); }
                Check(MouseHook.Decode(0x200, 0, 0) == null && MouseHook.Decode(0x20A, 0, 0) == null, "Ignore pointer movement and wheel rotation");
                Check(MouseHook.Decode(0x20B, 2u << 16, 1) is { Key: 6, Up: false, Injected: true }, "Decode X2 high word and injected flag");
                Check(Native.MousePacket(5, false) == (0x80u, 1u) && Native.MousePacket(6, true) == (0x100u, 2u), "Encode side buttons with correct data and flags");
                var rgb = OpenRgb.Parse("0: Test RAM\n  Type: DRAM\n  Modes: [Static]\n1: Evision Keyboard\n  Type: Keyboard\n  Location: HID: test\n  Serial: abc\n  Modes: [Static] \"Rainbow Wave\" Breathing\n2: Other\n  Type: Mouse\n");
                Check(rgb.Count == 3 && rgb[1].Modes.SequenceEqual(new[] { "Static", "Rainbow Wave", "Breathing" }), "OpenRGB parses quoted modes and device identity");
                Check(OpenRgb.ApplyArguments(rgb[1], "Static", Color.FromArgb(0, 128, 255), 50, null).SequenceEqual(new[] { "--device", "1", "--mode", "Static", "--color", "0080FF", "--brightness", "50" }), "OpenRGB targets one keyboard and formats color");
                Reject(() => OpenRgb.ApplyArguments(rgb[0], "Static", Color.Red, null, null), "OpenRGB rejects non-keyboard device");
                Reject(() => OpenRgb.ApplyArguments(rgb[1], "Unknown", Color.Red, null, null), "OpenRGB rejects unavailable mode");
                Check(!rgb[1].SameDevice(rgb[1] with { Location = "changed" }), "OpenRGB detects changed device identity");
                var captured = new byte[64]; EvisionLighting.ReadRequest().CopyTo(captured, 0);
                new byte[] { 0, 9, 4, 1, 0, 255, 255, 255, 255 }.CopyTo(captured, 8);
                var decoded = EvisionLighting.Parse(captured, "0x0101");
                Check(!decoded.Layout1.Plausible && decoded.Layout2Profile0.Plausible && decoded.Layout2Profile0.Mode == 9 && decoded.Layout2Profile0.Brightness == 4, "Decode observed Blaze V2 header without mistaking profile for mode");
                var snapshot = new EvisionControl.Snapshot(decoded, "");
                var desired = EvisionControl.Desired(snapshot, 9, 3, 1, true, Color.White);
                Check(desired.SequenceEqual(new byte[] { 9, 3, 1, 0, 255, 255, 255, 255 }), "Brightness change preserves other requested header fields");
                var packet = EvisionControl.Packet(6, desired);
                Check(packet.Length == 64 && packet[3] == 6 && packet[4] == 8 && packet[5] == 1 && packet[6] == 0 && packet[1] == 0x18 && packet[2] == 4 && packet.AsSpan(8, 8).SequenceEqual(desired) && packet.Skip(16).All(b => b == 0), "Write packet restricted to 8 bytes at offset 1; unsigned checksum correct");
                Reject(() => EvisionControl.Packet(0x11, desired), "Reject per-key color command");
                Reject(() => EvisionControl.Packet(6, new byte[9]), "Reject write beyond lighting header");
                Reject(() => EvisionControl.Desired(snapshot, 20, 4, 1, false, Color.Red), "Reject unverified custom mode");
                Reject(() => EvisionControl.Desired(snapshot, 6, 5, 1, false, Color.Red), "Reject excessive device brightness");
                var wrong = captured.ToArray(); wrong[1] ^= 1;
                Reject(() => EvisionLighting.Parse(wrong, "0x0101"), "Reject stale or mismatched HID response");
                Check(EvisionControl.Packet(5).SequenceEqual(EvisionLighting.ReadRequest()), "Read-only query agrees across reader and controller");
                Reject(() => new Macro { Timing = (TimingMode)99 }.Validate(), "Reject unknown timing mode");
                try
                {
                    MacroFile.Save(path, new Macro { Version = 3, Timing = TimingMode.Precision, Steps = [new() { Key = 65 }] });
                    Check(MacroFile.Load(path).Timing == TimingMode.Precision, "Timing mode survives schema v3 save/load");
                }
                finally { File.Delete(path); }
                foreach (var timing in Enum.GetValues<TimingMode>())
                {
                    events.Clear();
                    await new Player((s, up) => events.Add((s.Key, up))).Play(new Macro { Version = 3, Timing = timing, Repeats = 2, RepeatDelay = 2, Steps = [new() { Kind = StepKind.Wait, Milliseconds = 2 }, new() { Key = 1, Milliseconds = 2 }] }, CancellationToken.None);
                    Check(events.SequenceEqual(new[] { (1, false), (1, true), (1, false), (1, true) }) && PauseClock.ActiveRequests == 0, $"{timing}: playback/repeats release buttons and timer request");
                    events.Clear();
                    using var cancelTiming = new CancellationTokenSource();
                    var pressed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var task = new Player((s, up) => { events.Add((s.Key, up)); if (!up) pressed.TrySetResult(); }).Play(new Macro { Version = 3, Timing = timing, Steps = [new() { Key = 162, Milliseconds = 10000 }] }, cancelTiming.Token);
                    await pressed.Task.WaitAsync(TimeSpan.FromSeconds(3)); await Task.Delay(30); cancelTiming.Cancel();
                    bool cancelled = false;
                    try { await task.WaitAsync(TimeSpan.FromSeconds(3)); } catch (OperationCanceledException) { cancelled = true; }
                    Check(cancelled && events.SequenceEqual(new[] { (162, false), (162, true) }) && PauseClock.ActiveRequests == 0, $"{timing}: cancel during wait promptly releases Ctrl and timer request");
                    events.Clear();
                    try { await new Player((s, up) => { if (s.Key == 65) throw new IOException("test"); events.Add((s.Key, up)); }).Play(new Macro { Version = 3, Timing = timing, Steps = [new() { Kind = StepKind.Down, Key = 162 }, new() { Key = 65 }, new() { Kind = StepKind.Up, Key = 162 }] }, CancellationToken.None); } catch (IOException) { }
                    Check(events.SequenceEqual(new[] { (162, false), (162, true) }) && PauseClock.ActiveRequests == 0, $"{timing}: input failure releases Ctrl and timer request");
                }
                using (var firstClock = new PauseClock(TimingMode.Resolution1ms))
                {
                    using (var secondClock = new PauseClock(TimingMode.Precision)) Check(PauseClock.ActiveRequests == 2, "Concurrent sessions have paired timer requests");
                    Check(PauseClock.ActiveRequests == 1, "Disposing one timer leaves other request alive");
                }
                Check(PauseClock.ActiveRequests == 0, "All timer requests released after tests");
                if (!nativeInput)
                {
                    results.Add("ALL CORE TESTS PASSED");
                    results.Add("SKIP native input: run --self-test-input while the test window has foreground focus; do not use keyboard/mouse during this optional test.");
                    return;
                }
                var observed = new List<KeyEvent>();
                using var hook = new KeyboardHook { Handler = e => { observed.Add(e); return false; } };
                form.Activate(); box.Focus(); box.Clear(); await Task.Delay(350);
                Check(Native.OwnForeground(), "Native integration target is foreground");
                await Task.Run(() => new Player().Play(new Macro { Steps = [new() { Key = 65, Milliseconds = 20 }] }, CancellationToken.None));
                for (int wait = 0; wait < 20 && box.Text.Length == 0; wait++) await Task.Delay(25);
                Check(box.Text.Length == 1, $"SendInput types into real Windows textbox (length={box.Text.Length}, focused={box.Focused})");
                Check(observed.Any(e => e.Key == 65 && !e.Up && e.Injected) && observed.Any(e => e.Key == 65 && e.Up && e.Injected), "Low-level hook sees injected down and up");
                Check((Native.GetAsyncKeyState(65) & 0x8000) == 0, "Native key is released after playback");
                var mouseObserved = new List<KeyEvent>();
                // Suppress test clicks in the hook so they never activate whatever is under the cursor.
                using var mouseHook = new MouseHook { Handler = (e, point) => { if (!e.Injected) return false; mouseObserved.Add(e); return true; } };
                foreach (var button in KeyNames.MouseKeys)
                {
                    await Task.Run(() => new Player().Play(new Macro { Version = 2, Steps = [new() { Key = button, Milliseconds = 10 }] }, CancellationToken.None));
                    // Low-level hook delivery can lag SendInput's return; keep pumping the UI queue.
                    for (int wait = 0; wait < 20 && !mouseObserved.Any(e => e.Key == button && e.Up); wait++) await Task.Delay(25);
                    Check(mouseObserved.Any(e => e.Key == button && !e.Up) && mouseObserved.Any(e => e.Key == button && e.Up), $"Native mouse hook observes button {button} down/up");
                }
                results.Add("ALL TESTS PASSED");
            }
            catch (Exception e) { results.Add("FAIL " + e); Environment.ExitCode = 1; }
            finally { File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "self-test-results.txt"), results); form.Close(); }
        };
        Application.Run(form);
    }
}
