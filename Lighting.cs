using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazeMacro;

public sealed record RgbDevice(int Id, string Name, string Type, string Location, string Serial, string[] Modes)
{
    public string Display => $"{Id}: {Name}";
    public bool SameDevice(RgbDevice other) => Id == other.Id && Name == other.Name && Type == other.Type && Location == other.Location && Serial == other.Serial;
}

public static class OpenRgb
{
    public static List<RgbDevice> Parse(string output)
    {
        var devices = new List<RgbDevice>();
        var blocks = Regex.Matches(output, @"(?m)^(\d+):\s*(.+)\r?$");
        for (int i = 0; i < blocks.Count; i++)
        {
            var match = blocks[i];
            var end = i + 1 < blocks.Count ? blocks[i + 1].Index : output.Length;
            var block = output.Substring(match.Index + match.Length, end - match.Index - match.Length);
            string Field(string name) => Regex.Match(block, @"(?m)^\s*" + name + @":\s*([^\r\n]*)").Groups[1].Value.Trim();
            var modes = Regex.Matches(Field("Modes"), "\\[?(?:\"([^\"]+)\"|([^\\s\\[\\]]+))\\]?")
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).Distinct().ToArray();
            if (!int.TryParse(match.Groups[1].Value, out var id)) continue;
            devices.Add(new RgbDevice(id, match.Groups[2].Value.Trim(), Field("Type"), Field("Location"), Field("Serial"), modes));
        }
        return devices;
    }

    public static string[] ApplyArguments(RgbDevice device, string mode, Color color, int? brightness, int? speed)
    {
        if (device.Id < 0 || !device.Type.Equals("Keyboard", StringComparison.OrdinalIgnoreCase) || !device.Modes.Contains(mode))
            throw new InvalidDataException("Выберите обнаруженную клавиатуру и поддерживаемый режим.");
        if (brightness is < 0 or > 100 || speed is < 0 or > 100) throw new InvalidDataException("Значение должно быть от 0 до 100.");
        var args = new List<string> { "--device", device.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), "--mode", mode, "--color", $"{color.R:X2}{color.G:X2}{color.B:X2}" };
        if (brightness.HasValue) args.AddRange(["--brightness", brightness.Value.ToString()]);
        if (speed.HasValue) args.AddRange(["--speed", speed.Value.ToString()]);
        return args.ToArray();
    }

    public static async Task<string> Run(string exe, int port, string[] args, CancellationToken token)
    {
        if (!File.Exists(exe) || !Path.GetFileName(exe).Equals("OpenRGB.exe", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Выберите установленный OpenRGB.exe.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var ct = timeout.Token;
        using (var probe = new TcpClient())
        {
            try { await probe.ConnectAsync("127.0.0.1", port, ct); }
            catch (SocketException) { throw new IOException($"OpenRGB SDK Server не доступен на 127.0.0.1:{port}. Запустите сервер на вкладке SDK Server в OpenRGB."); }
        }
        var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "--nodetect", "--noautoconnect", "--client", $"127.0.0.1:{port}" }.Concat(args)) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new IOException("Не удалось запустить OpenRGB.");
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        try
        {
            await process.WaitForExitAsync(ct);
            var output = await stdout; var error = await stderr;
            if (process.ExitCode != 0 || Regex.IsMatch(output + error, @"(?im)^\s*Error:"))
                throw new IOException("OpenRGB: " + (output + "\n" + error).Trim()[..Math.Min((output + "\n" + error).Trim().Length, 1500)]);
            return output;
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(CancellationToken.None); }
            try { await Task.WhenAll(stdout, stderr); } catch (OperationCanceledException) { }
        }
    }

    public static async Task<List<RgbDevice>> List(string exe, int port, CancellationToken token)
    {
        var devices = Parse(await Run(exe, port, ["--list-devices"], token));
        // Older releases include details in --list-devices; newer releases use --list-detailed.
        if (devices.Count > 0 && devices.All(d => d.Type.Length == 0)) devices = Parse(await Run(exe, port, ["--list-detailed"], token));
        return devices.Where(d => d.Type.Equals("Keyboard", StringComparison.OrdinalIgnoreCase)).ToList();
    }
}

public sealed class LightingForm : Form
{
    static string executable = "";
    readonly ComboBox devices = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 620, DisplayMember = "Display" };
    readonly ComboBox modes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    readonly NumericUpDown port = new() { Minimum = 1024, Maximum = 65535, Value = 6742, Width = 90 };
    readonly NumericUpDown brightness = new() { Minimum = 0, Maximum = 100, Value = 100, Width = 65 };
    readonly NumericUpDown speed = new() { Minimum = 0, Maximum = 100, Value = 50, Width = 65 };
    readonly CheckBox useBrightness = new() { Text = "Яркость, %", AutoSize = true };
    readonly CheckBox useSpeed = new() { Text = "Скорость, %", AutoSize = true };
    readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = false };
    readonly Label pathLabel = new() { AutoSize = false, Width = 480, Height = 35, AutoEllipsis = true };
    readonly Button apply = new() { Text = "Применить к клавиатуре", Width = 240, Height = 36, Enabled = false };
    readonly Button refresh = new() { Text = "Найти клавиатуры", Width = 190, Height = 36 };
    readonly Button browse = new() { Text = "Выбрать OpenRGB.exe", Width = 210, Height = 36 };
    Color chosen = Color.FromArgb(107, 224, 190);
    readonly CancellationTokenSource lifetime = new();
    bool working;

    public LightingForm()
    {
        Text = "Подсветка · DEXP Blaze"; Size = new Size(820, 730); MinimumSize = Size;
        Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(17, 21, 29); ForeColor = Color.FromArgb(232, 238, 246);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 11, Padding = new Padding(22) };
        foreach (var height in new[] { 45, 62, 100, 44, 55, 48, 48, 48, 48, 45 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
        root.Controls.Add(new Label { Text = "ПОДСВЕТКА", Font = new Font("Segoe UI Semibold", 21), AutoSize = true }, 0, 0);
        root.Controls.Add(TextBlock("Управление на клавиатуре доступно без программы. Прямое USB-управление DEXP Blaze здесь не реализовано; подключение через OpenRGB зависит от поддержки вашей ревизии."), 0, 1);
        root.Controls.Add(TextBlock("Fn + Scroll Lock — следующий эффект (19 режимов)\nFn + Home — сменить цвет\nFn + ↑ / ↓ — ярче / темнее\nFn + ← / → — медленнее / быстрее"), 0, 2);
        var link = new LinkLabel { Text = "Открыть инструкцию DEXP Blaze", AutoSize = true, LinkColor = Color.FromArgb(107, 224, 190) };
        link.LinkClicked += (_, _) => { try { Process.Start(new ProcessStartInfo("https://drv.dns-shop.ru/drivers/oth/D/dexp-blaze_instrukcia_170555_11122023.pdf") { UseShellExecute = true }); } catch (Exception e) { status.Text = e.Message; } };
        var helpRow = Row(); helpRow.Controls.Add(link);
        var diagnose = new Button { Text = "Своя диагностика HID…", Width = 235, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(35, 43, 57) };
        diagnose.Click += (_, _) => { using var dialog = new DiagnosticsForm(); dialog.ShowDialog(this); };
        helpRow.Controls.Add(diagnose); root.Controls.Add(helpRow, 0, 3);
        root.Controls.Add(TextBlock("Через OpenRGB: запустите OpenRGB отдельно, убедитесь, что он видит клавиатуру, и включите SDK Server. Затем выберите OpenRGB.exe и найдите клавиатуры."), 0, 4);
        var fileRow = Row(); fileRow.Controls.Add(browse); fileRow.Controls.Add(pathLabel); pathLabel.Text = executable.Length == 0 ? "OpenRGB.exe не выбран" : executable;
        root.Controls.Add(fileRow, 0, 5);
        var connection = Row(); connection.Controls.Add(new Label { Text = "Порт сервера", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }); connection.Controls.Add(port); connection.Controls.Add(refresh); root.Controls.Add(connection, 0, 6);
        var deviceRow = Row(); deviceRow.Controls.Add(devices); root.Controls.Add(deviceRow, 0, 7);
        var effectRow = Row(); effectRow.Controls.Add(modes);
        var colorButton = new Button { Text = "Выбрать цвет", Width = 160, Height = 36, BackColor = chosen, ForeColor = Color.Black };
        colorButton.Click += (_, _) => { using var d = new ColorDialog { Color = chosen, FullOpen = true }; if (d.ShowDialog(this) == DialogResult.OK) { chosen = d.Color; colorButton.BackColor = chosen; } };
        effectRow.Controls.Add(colorButton); root.Controls.Add(effectRow, 0, 8);
        var values = Row(); values.Controls.Add(useBrightness); values.Controls.Add(brightness); values.Controls.Add(useSpeed); values.Controls.Add(speed); values.Controls.Add(apply); root.Controls.Add(values, 0, 9);
        status.Text = "Яркость, скорость и выбранный цвет применяются только если режим их поддерживает. Кнопки Fn нужно нажимать физически.";
        root.Controls.Add(status, 0, 10);
        foreach (var b in new[] { browse, refresh, apply }) { b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(35, 43, 57); }
        browse.Click += (_, _) => { using var d = new OpenFileDialog { Filter = "OpenRGB.exe|OpenRGB.exe" }; if (d.ShowDialog(this) == DialogResult.OK) { executable = d.FileName; pathLabel.Text = executable; ResetDevices(); } };
        port.ValueChanged += (_, _) => ResetDevices();
        devices.SelectedIndexChanged += (_, _) => { modes.DataSource = (devices.SelectedItem as RgbDevice)?.Modes; apply.Enabled = !working && modes.Items.Count > 0; };
        refresh.Click += async (_, _) => await Execute(async token =>
        {
            ResetDevices(); devices.DataSource = await OpenRgb.List(executable, (int)port.Value, token); devices.SelectedIndex = -1;
            status.Text = devices.Items.Count == 0 ? "OpenRGB не сообщил о поддерживаемых клавиатурах. Используйте сочетания Fn выше." : "Выберите свою клавиатуру и режим. Настройки ещё не отправлены.";
        });
        apply.Click += async (_, _) =>
        {
            if (devices.SelectedItem is not RgbDevice selected || modes.SelectedItem is not string mode) return;
            var args = OpenRgb.ApplyArguments(selected, mode, chosen, useBrightness.Checked ? (int)brightness.Value : null, useSpeed.Checked ? (int)speed.Value : null);
            await Execute(async token =>
            {
                var current = await OpenRgb.List(executable, (int)port.Value, token);
                if (!current.Any(d => d.SameDevice(selected) && d.Modes.Contains(mode))) { ResetDevices(); throw new IOException("Список устройств изменился. Нажмите «Найти клавиатуры» и выберите клавиатуру заново."); }
                await OpenRgb.Run(executable, (int)port.Value, args, token);
                status.Text = "Команда отправлена через OpenRGB. Проверьте результат на клавиатуре; подтверждения от самой подсветки нет.";
            });
        };
        FormClosing += (_, _) => lifetime.Cancel();
        FormClosed += (_, _) => { if (!working) lifetime.Dispose(); };
    }
    static Label TextBlock(string text) => new() { Text = text, Dock = DockStyle.Fill };
    static FlowLayoutPanel Row() => new() { Dock = DockStyle.Fill, WrapContents = false };
    void ResetDevices() { devices.DataSource = null; modes.DataSource = null; apply.Enabled = false; }
    async Task Execute(Func<CancellationToken, Task> action)
    {
        working = true; apply.Enabled = refresh.Enabled = browse.Enabled = port.Enabled = devices.Enabled = modes.Enabled = false;
        status.Text = "Подключение к OpenRGB…";
        try { await action(lifetime.Token); }
        catch (OperationCanceledException) { if (!IsDisposed) status.Text = "Подключение отменено или превышено время ожидания (30 с)."; }
        catch (Exception e) { if (!IsDisposed) status.Text = e.Message; }
        finally
        {
            working = false;
            if (!IsDisposed) { refresh.Enabled = browse.Enabled = port.Enabled = devices.Enabled = modes.Enabled = true; apply.Enabled = devices.SelectedItem is RgbDevice && modes.Items.Count > 0; }
            else lifetime.Dispose();
        }
    }
}
