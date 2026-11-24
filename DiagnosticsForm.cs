namespace BlazeMacro;

public sealed class DiagnosticsForm : Form
{
    readonly TextBox report = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
    readonly Button scan = new() { Text = "Прочитать метаданные", Width = 220, Height = 36 };
    readonly Button save = new() { Text = "Сохранить отчёт…", Width = 180, Height = 36, Enabled = false };
    readonly Label status = new() { Dock = DockStyle.Fill, Text = "Только USB 320F:5084 и 04D9:A09F. Настройки не изменяются; нажатия не читаются.", AutoSize = false };
    HidDiagnostics.Result? result;
    public DiagnosticsForm()
    {
        Text = "Blaze · собственная диагностика HID"; Size = new Size(980, 680); MinimumSize = new Size(720, 450);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(16) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill }; buttons.Controls.Add(scan); buttons.Controls.Add(save);
        layout.Controls.Add(buttons, 0, 0); layout.Controls.Add(report, 0, 1); layout.Controls.Add(status, 0, 2); Controls.Add(layout);
        scan.Click += async (_, _) =>
        {
            scan.Enabled = save.Enabled = false; status.Text = "Чтение дескрипторов Windows HID…";
            try
            {
                var captured = await Task.Run(HidDiagnostics.Scan);
                if (IsDisposed) return;
                result = captured; report.Text = HidDiagnostics.Describe(captured);
                status.Text = $"Найдено коллекций: {captured.Collections.Count}. Наличие служебного канала не подтверждает команды RGB. OpenRGB не используется.";
            }
            catch (Exception e) { if (!IsDisposed) status.Text = e.Message; }
            finally { if (!IsDisposed) { scan.Enabled = true; save.Enabled = result != null; } }
        };
        save.Click += (_, _) =>
        {
            if (result == null) return;
            using var dialog = new SaveFileDialog { Filter = "Текстовый отчёт (*.txt)|*.txt|JSON (*.json)|*.json", FileName = "HID-diagnostic.txt" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var text = dialog.FilterIndex == 2 ? System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) : HidDiagnostics.Describe(result);
                File.WriteAllText(dialog.FileName, text); status.Text = "Отчёт сохранён: " + dialog.FileName;
            }
            catch (Exception e) { status.Text = e.Message; }
        };
    }
}
