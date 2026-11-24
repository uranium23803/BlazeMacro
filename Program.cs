namespace BlazeMacro;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--benchmark-timing")
        {
            try { TimingBenchmark.Save(args[1]).GetAwaiter().GetResult(); }
            catch (Exception e) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "timing-error.txt"), e.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--check-direct-reading")
        {
            try
            {
                var reading = EvisionControl.Read(CancellationToken.None).GetAwaiter().GetResult();
                Directory.CreateDirectory(args[1]);
                File.WriteAllText(Path.Combine(args[1], "direct-reading.json"), System.Text.Json.JsonSerializer.Serialize(reading, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception e) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "direct-reading-error.txt"), e.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--read-lighting")
        {
            try { EvisionLighting.SaveReading(args[1]).GetAwaiter().GetResult(); }
            catch (Exception e) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "lighting-read-error.txt"), e.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--diagnose-hid")
        {
            try { HidDiagnostics.Save(HidDiagnostics.Scan(), args[1]); }
            catch (Exception e) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "HID-error.txt"), e.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Contains("--self-test") || args.Contains("--self-test-input")) { SelfTest.Run(args.Contains("--self-test-input")); return; }
        using var mutex = new Mutex(true, "Local\\BlazeMacro.Desktop.v1", out var first);
        if (!first) { MessageBox.Show("Blaze Macro уже запущена.", "Blaze Macro"); return; }
        try { Application.Run(new MainForm()); }
        catch (Exception e) { MessageBox.Show(e.Message, "Не удалось запустить Blaze Macro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
