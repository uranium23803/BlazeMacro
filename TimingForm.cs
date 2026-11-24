namespace BlazeMacro;

public sealed class TimingForm : Form
{
    readonly ComboBox modes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    readonly Button test = new() { Text = "Сравнить режимы", Width = 190, Height = 35 };
    readonly Button stop = new() { Text = "Остановить тест", Width = 170, Height = 35, Enabled = false };
    readonly Button save = new() { Text = "Сохранить замер", Width = 175, Height = 35, Enabled = false };
    readonly Button accept = new() { Text = "Использовать для макроса", Width = 250, Height = 35 };
    readonly Label status = new() { Dock = DockStyle.Fill, Text = "Запрос точности активен только во время воспроизведения или теста." };
    readonly DataGridView table = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, RowHeadersVisible = false };
    CancellationTokenSource? testCancellation;
    TimingBenchmark.Report? report;
    bool closeAfterTest;
    public TimingMode SelectedMode => (TimingMode)modes.SelectedIndex;
    public TimingForm(TimingMode current)
    {
        Text = "Blaze · точность пауз"; Size = new Size(920, 650); MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(17, 21, 29); ForeColor = Color.FromArgb(232, 238, 246);
        table.BackgroundColor = Color.FromArgb(27, 33, 44); table.EnableHeadersVisualStyles = false;
        table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 43, 57); table.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        table.DefaultCellStyle.BackColor = Color.FromArgb(27, 33, 44); table.DefaultCellStyle.ForeColor = ForeColor;
        table.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 86, 82); table.DefaultCellStyle.SelectionForeColor = Color.White;
        table.RowTemplate.Height = 30;
        foreach (var button in new[] { test, stop, save, accept }) { button.FlatStyle = FlatStyle.Flat; button.BackColor = Color.FromArgb(35, 43, 57); }
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(20) };
        foreach (int size in new[] { 40, 95, 48, 48 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, size));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); Controls.Add(root);
        root.Controls.Add(new Label { Text = "ТОЧНОСТЬ ПАУЗ", AutoSize = true, Font = new Font("Segoe UI Semibold", 20) }, 0, 0);
        root.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Обычный — стандартные ожидания .NET.\nЗапрос 1 мс — обычный таймер Windows с timeBeginPeriod(1).\nТочный — высокоточный таймер Windows с запросом 1 мс; без постоянного опроса CPU.\nМеняет паузы макроса. Не ускоряет USB или другие приложения; точные сроки не гарантируются." }, 0, 1);
        modes.Items.AddRange(Enum.GetValues<TimingMode>().Select(m => (object)TimingBenchmark.Name(m)).ToArray()); modes.SelectedIndex = (int)current;
        var selection = new FlowLayoutPanel { Dock = DockStyle.Fill }; selection.Controls.Add(modes); selection.Controls.Add(accept); root.Controls.Add(selection, 0, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill }; buttons.Controls.Add(test); buttons.Controls.Add(stop); buttons.Controls.Add(save); root.Controls.Add(buttons, 0, 3);
        foreach (var (property, title, width) in new[] { ("Mode", "Режим", 145), ("RequestedMs", "Задано, мс", 105), ("MeanMs", "Среднее, мс", 120), ("P95Ms", "P95, мс", 110), ("MaxMs", "Макс., мс", 110), ("MeanOvershootMs", "Лишняя пауза, мс", 155) })
        {
            var column = new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = title, Width = width };
            if (property is not ("Mode" or "RequestedMs")) column.DefaultCellStyle.Format = "F3";
            table.Columns.Add(column);
        }
        root.Controls.Add(table, 0, 4);
        root.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "P95: 95% замеров не превышают это время. Тест не нажимает клавиши и не кликает.\nНагрузка и видимость окна влияют на результат. В Windows 11 свёрнутое окно может получить иную точность." }, 0, 5);
        root.Controls.Add(status, 0, 6);
        accept.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        stop.Click += (_, _) => testCancellation?.Cancel();
        save.Click += (_, _) =>
        {
            if (report == null) return;
            using var d = new SaveFileDialog { Filter = "Текст (*.txt)|*.txt|JSON (*.json)|*.json", FileName = "timing-benchmark.txt" };
            if (d.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(d.FileName, d.FilterIndex == 2 ? System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) : TimingBenchmark.Describe(report)); }
            catch (Exception e) { status.Text = e.Message; }
        };
        test.Click += async (_, _) =>
        {
            testCancellation = new CancellationTokenSource(); test.Enabled = accept.Enabled = modes.Enabled = save.Enabled = false; stop.Enabled = true;
            report = null; table.DataSource = null;
            try
            {
                var progress = new Progress<string>(message => { if (testCancellation != null && !IsDisposed) status.Text = message; });
                report = await TimingBenchmark.Run(testCancellation.Token, progress); table.DataSource = report.Rows;
                status.Text = "Замер завершён. Запросы 1 мс освобождены. " + (report.Rows.Any(r => r.Backend.StartsWith("Резерв")) ? "Высокоточный таймер недоступен: использован резервный." : "");
            }
            catch (OperationCanceledException) { status.Text = "Тест остановлен. Запросы 1 мс освобождены."; }
            catch (Exception e) { status.Text = e.Message; }
            finally
            {
                testCancellation.Dispose(); testCancellation = null; test.Enabled = accept.Enabled = modes.Enabled = true; stop.Enabled = false; save.Enabled = report != null;
                if (closeAfterTest) Close();
            }
        };
        FormClosing += (_, e) => { if (testCancellation != null) { e.Cancel = true; closeAfterTest = true; testCancellation.Cancel(); } };
    }
}
