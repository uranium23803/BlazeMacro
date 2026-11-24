namespace BlazeMacro;

public sealed class DirectLightingForm : Form
{
    readonly Button read = new() { Text = "Прочитать с клавиатуры", Width = 220, Height = 36 };
    readonly Button apply = new() { Text = "Применить", Width = 150, Height = 36, Enabled = false };
    readonly Button undo = new() { Text = "Вернуть предыдущие", Width = 205, Height = 36, Enabled = false };
    readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    readonly NumericUpDown brightness = new() { Minimum = 0, Maximum = 4, Value = 4, Width = 80 };
    readonly NumericUpDown speed = new() { Minimum = 0, Maximum = 5, Value = 1, Width = 80 };
    readonly CheckBox random = new() { Text = "Автоматические цвета эффекта", AutoSize = true };
    readonly Button color = new() { Text = "Выбрать цвет", Width = 170, Height = 36 };
    readonly Label status = new() { Dock = DockStyle.Fill, Text = "Нажмите «Прочитать с клавиатуры». До чтения запись недоступна." };
    readonly FlowLayoutPanel values = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Enabled = false };
    readonly CancellationTokenSource lifetime = new();
    EvisionControl.Snapshot? snapshot, previous;
    bool busy;
    Color chosen = Color.White;
    public DirectLightingForm()
    {
        Text = "Blaze · подсветка без OpenRGB"; Size = new Size(800, 640); MinimumSize = Size; StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(17, 21, 29); ForeColor = Color.FromArgb(232, 238, 246);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(22) };
        foreach (int height in new[] { 50, 72, 48, 200, 48, 50 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
        root.Controls.Add(new Label { Text = "BLAZE / ПОДСВЕТКА", AutoSize = true, Font = new Font("Segoe UI Semibold", 22), ForeColor = Color.FromArgb(107, 224, 190) }, 0, 0);
        root.Controls.Add(new Label { Text = "Собственный HID-модуль · 320F:5084 / 0101 / профиль 0\nЧтение проверено на этой клавиатуре. Запись пока экспериментальная.\nИзменения сохраняются в памяти клавиатуры только по кнопке «Применить».", Dock = DockStyle.Fill }, 0, 1);
        var actions = Row(); actions.Controls.Add(read);
        var diagnostic = new Button { Text = "Диагностика HID…", Width = 185, Height = 36 }; actions.Controls.Add(diagnostic);
        diagnostic.Click += (_, _) => { using var dialog = new DiagnosticsForm(); dialog.ShowDialog(this); };
        root.Controls.Add(actions, 0, 2);
        string[] names = ["Волна короткая", "Волна длинная", "Цветовое колесо", "Смена спектра", "Дыхание", "Постоянный свет", "Реакция на нажатие", "Рябь", "Бегущая линия", "Быстрые вспышки", "Расцветание", "Вертикальная радуга", "Ураган", "Накопление", "Медленные вспышки", "Визор", "Surmount", "Круговая радуга"];
        mode.Items.AddRange(names.Select((name, i) => (object)$"{i + 1}: {name}").ToArray());
        var modeRow = Row(); modeRow.Controls.Add(Label("Эффект")); modeRow.Controls.Add(mode); values.Controls.Add(modeRow);
        var brightRow = Row(); brightRow.Controls.Add(Label("Яркость: 0 — выкл., 4 — максимум")); brightRow.Controls.Add(brightness); values.Controls.Add(brightRow);
        var speedRow = Row(); speedRow.Controls.Add(Label("Скорость: 0 — быстро, 5 — медленно")); speedRow.Controls.Add(speed); values.Controls.Add(speedRow);
        var colors = Row(); colors.Controls.Add(color); colors.Controls.Add(random); values.Controls.Add(colors);
        root.Controls.Add(values, 0, 3);
        var commands = Row(); commands.Controls.Add(apply); commands.Controls.Add(undo); root.Controls.Add(commands, 0, 4);
        root.Controls.Add(new Label { Text = "Цвет и скорость зависят от выбранного эффекта. Имена эффектов — по протоколу EVision; соответствие всех режимов этой ревизии ещё не проверено.", Dock = DockStyle.Fill }, 0, 5);
        root.Controls.Add(status, 0, 6);
        foreach (var b in new[] { read, apply, undo, color, diagnostic }) { b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(35, 43, 57); }
        color.Click += (_, _) => { using var picker = new ColorDialog { Color = chosen, FullOpen = true }; if (picker.ShowDialog(this) == DialogResult.OK) { chosen = picker.Color; color.Text = $"#{chosen.R:X2}{chosen.G:X2}{chosen.B:X2}"; random.Checked = false; } };
        read.Click += async (_, _) => await Run(async () => { var current = await EvisionControl.Read(lifetime.Token); LoadSnapshot(current); status.Text = "Прочитано с клавиатуры. Изменений не отправлено."; });
        apply.Click += async (_, _) =>
        {
            if (snapshot == null) return;
            var expected = snapshot;
            await Run(async () =>
            {
                var desired = EvisionControl.Desired(expected, mode.SelectedIndex + 1, (int)brightness.Value, (int)speed.Value, random.Checked, chosen);
                if (desired.SequenceEqual(expected.Header)) { status.Text = "Параметры не изменились."; return; }
                var result = await EvisionControl.Apply(expected, desired, lifetime.Token); previous = result.Before; LoadSnapshot(result.After);
                status.Text = "Настройки подтверждены повторным чтением.\nКопия: " + result.BackupFile;
            });
        };
        undo.Click += async (_, _) =>
        {
            if (snapshot == null || previous == null) return;
            var expected = snapshot; var restore = previous.Header;
            await Run(async () => { var result = await EvisionControl.Apply(expected, restore, lifetime.Token); previous = result.Before; LoadSnapshot(result.After); status.Text = "Предыдущие параметры восстановлены и проверены."; });
        };
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; status.Text = "Дождитесь завершения HID-операции (обычно до нескольких секунд)."; } };
        FormClosed += (_, _) => { lifetime.Cancel(); lifetime.Dispose(); };
    }
    static FlowLayoutPanel Row() => new() { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
    static Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(0, 7, 12, 0) };
    void LoadSnapshot(EvisionControl.Snapshot current)
    {
        snapshot = current; var h = current.Reading.Layout2Profile0;
        mode.SelectedIndex = h.Mode - 1; brightness.Value = h.Brightness; speed.Value = h.Speed; random.Checked = h.RandomColor != 0;
        chosen = ColorTranslator.FromHtml(h.Color); color.Text = h.Color;
    }
    async Task Run(Func<Task> action)
    {
        busy = true; read.Enabled = apply.Enabled = undo.Enabled = values.Enabled = false; status.Text = "Обмен с клавиатурой…";
        try { await action(); }
        catch (Exception e) { snapshot = null; status.Text = e.Message; }
        finally { busy = false; read.Enabled = true; values.Enabled = apply.Enabled = snapshot != null; undo.Enabled = snapshot != null && previous != null; }
    }
}
