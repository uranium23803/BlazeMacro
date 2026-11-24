using System.ComponentModel;

namespace BlazeMacro;

public sealed class MainForm : Form
{
    static readonly Color Background = Color.FromArgb(17, 21, 29), Surface = Color.FromArgb(27, 33, 44), Ink = Color.FromArgb(232, 238, 246), Muted = Color.FromArgb(160, 173, 190), Accent = Color.FromArgb(107, 224, 190);
    readonly TextBox name = new() { Text = "Мой макрос", Width = 230, MaxLength = 100 };
    readonly Button triggerButton = new() { Text = "F6 · изменить", Width = 155 };
    readonly NumericUpDown repeats = new() { Minimum = 1, Maximum = 1000, Value = 1, Width = 70 };
    readonly NumericUpDown gap = new() { Maximum = 600000, Value = 200, Width = 95 };
    readonly NumericUpDown duration = new() { Maximum = 600000, Value = 50, Width = 95 };
    readonly ComboBox kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 185 };
    readonly ComboBox key = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    readonly DataGridView grid = new();
    readonly BindingList<Step> steps = [];
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    readonly Label summary = new() { AutoSize = true, ForeColor = Muted };
    readonly CheckBox armed = new() { Text = "Горячая клавиша включена", AutoSize = true, Checked = true };
    readonly Button recordButton = new() { Text = "●  Записать", Width = 155 };
    readonly Button playButton = new() { Text = "▶  Проверить через 3 с", Width = 200 };
    readonly Button timingButton = new() { Text = "Паузы: Обычный…", Width = 215 };
    TimingMode timingMode;
    readonly List<Control> editors = [];
    readonly Recorder recorder = new();
    readonly HashSet<int> physical = [];
    readonly HashSet<int> swallowed = [];
    KeyboardHook? hook;
    MouseHook? mouseHook;
    CancellationTokenSource? operation;
    bool recording, busy, capturingTrigger, dirty, loading, closeRequested, stopQueued;
    int? pendingTrigger;
    int trigger = (int)Keys.F6;
    string? filePath;

    public MainForm()
    {
        Text = "Blaze Macro"; Size = new Size(1160, 940); MinimumSize = new Size(720, 540);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10);
        BackColor = Background; ForeColor = Ink;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 9 };
        foreach (var height in new[] { 70, 49, 49, 46, 54 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 53));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        root.MinimumSize = new Size(1100, 850);
        viewport.Controls.Add(root); Controls.Add(viewport);
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        heading.Controls.Add(new Label { Text = "BLAZE / MACRO", Font = new Font("Segoe UI Semibold", 24), ForeColor = Accent, AutoSize = true });
        heading.Controls.Add(new Label { Text = "Запишите действие. Настройте шаги. Запустите одной клавишей.", AutoSize = true, ForeColor = Muted });
        root.Controls.Add(heading, 0, 0);
        var files = Row();
        files.Controls.Add(Label("Макрос")); files.Controls.Add(name);
        files.Controls.Add(Button("Новый", (_, _) => NewMacro(), 90));
        files.Controls.Add(Button("Открыть", (_, _) => OpenMacro(), 100));
        files.Controls.Add(Button("Сохранить", (_, _) => SaveMacro(), 110));
        files.Controls.Add(Button("Сохранить как…", (_, _) => SaveMacro(true), 150));
        files.Controls.Add(Button("Подсветка…", (_, _) => { using var lighting = new DirectLightingForm(); lighting.ShowDialog(this); }, 120));
        root.Controls.Add(files, 0, 1); editors.Add(files);
        var options = Row();
        options.Controls.Add(Label("Запуск")); options.Controls.Add(triggerButton);
        options.Controls.Add(Label("Повторов")); options.Controls.Add(repeats);
        options.Controls.Add(Label("Между повторами, мс")); options.Controls.Add(gap); options.Controls.Add(armed);
        root.Controls.Add(options, 0, 2); editors.Add(options);
        var actions = Row();
        actions.Controls.Add(recordButton); actions.Controls.Add(playButton);
        var stop = Button("■  Стоп · F9", (_, _) => Stop(), 155); stop.ForeColor = Color.FromArgb(255, 160, 160); actions.Controls.Add(stop);
        actions.Controls.Add(timingButton); editors.Add(timingButton);
        actions.Controls.Add(Label("F9 — остановка"));
        timingButton.Click += (_, _) =>
        {
            using var dialog = new TimingForm(timingMode);
            if (dialog.ShowDialog(this) == DialogResult.OK && timingMode != dialog.SelectedMode)
            {
                timingMode = dialog.SelectedMode; timingButton.Text = "Паузы: " + TimingBenchmark.Name(timingMode) + "…"; Changed();
            }
        };
        root.Controls.Add(actions, 0, 3);
        var edit = Row();
        kind.Items.AddRange(new object[] { "Нажать и отпустить", "Зажать", "Отпустить", "Пауза" }); kind.SelectedIndex = 0;
        key.DataSource = KeyNames.MouseKeys.Concat(KeyNames.All).Select(k => new KeyChoice(k, KeyNames.Name(k))).ToList(); key.DisplayMember = "Name"; key.ValueMember = "Code"; key.SelectedValue = (int)Keys.A;
        edit.Controls.Add(kind); edit.Controls.Add(key); edit.Controls.Add(Label("Длительность, мс")); edit.Controls.Add(duration);
        edit.Controls.Add(Button("+ Шаг", (_, _) => AddStep(EditorStep()), 95));
        edit.Controls.Add(Button("Применить к шагу", (_, _) => UpdateStep(), 165));
        root.Controls.Add(edit, 0, 4); editors.Add(edit);
        grid.Dock = DockStyle.Fill; grid.AutoGenerateColumns = false; grid.DataSource = steps;
        grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false;
        grid.RowHeadersVisible = false; grid.BackgroundColor = Surface; grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersHeight = 38; grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(35, 43, 57), ForeColor = Muted, Font = Font };
        grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface, ForeColor = Ink, SelectionBackColor = Color.FromArgb(45, 86, 82), SelectionForeColor = Color.White, Padding = new Padding(8, 0, 0, 0) };
        grid.RowTemplate.Height = 34; grid.GridColor = Color.FromArgb(44, 53, 68);
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Action", HeaderText = "ДЕЙСТВИЕ", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 45 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "KeyName", HeaderText = "КЛАВИША / КНОПКА", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Duration", HeaderText = "ВРЕМЯ", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25 });
        root.Controls.Add(grid, 0, 5); editors.Add(grid);
        var order = Row();
        order.Controls.Add(Button("↑ Выше", (_, _) => MoveStep(-1), 100)); order.Controls.Add(Button("↓ Ниже", (_, _) => MoveStep(1), 100));
        order.Controls.Add(Button("Дублировать", (_, _) => { if (Selected >= 0) AddStep(steps[Selected].Copy()); }, 130));
        order.Controls.Add(Button("Удалить", (_, _) => { if (Selected >= 0) steps.RemoveAt(Selected); }, 100));
        order.Controls.Add(Button("Очистить", (_, _) => { if (steps.Count > 0 && Confirm("Удалить все шаги?")) steps.Clear(); }, 100));
        order.Controls.Add(summary); root.Controls.Add(order, 0, 6); editors.Add(order);
        var keyboard = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        keyboard.Controls.Add(new Label { Text = "ЭКРАННАЯ КЛАВИАТУРА · щелчок добавляет шаг выбранного типа", AutoSize = true, ForeColor = Muted });
        AddKeyRow(keyboard, Enumerable.Range((int)Keys.F1, 12).ToArray());
        AddKeyRow(keyboard, new[] { 27 }.Concat(Enumerable.Range(49, 9)).Concat(new[] { 48, 8 }).ToArray());
        AddKeyRow(keyboard, new[] { 9 }.Concat("QWERTYUIOP".Select(c => (int)c)).Concat(new[] { 46 }).ToArray());
        AddKeyRow(keyboard, new[] { 162 }.Concat("ASDFGHJKL".Select(c => (int)c)).Concat(new[] { 13 }).ToArray());
        AddKeyRow(keyboard, new[] { 160, 164 }.Concat("ZXCVBNM".Select(c => (int)c)).Concat(new[] { 32, 37, 38, 40, 39 }).ToArray());
        AddKeyRow(keyboard, KeyNames.MouseKeys);
        root.Controls.Add(keyboard, 0, 7); editors.Add(keyboard);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(new Label
        {
            Text = "@uranium23803\n@uranium_23803", AutoSize = true, Anchor = AnchorStyles.Right,
            Font = new Font("Segoe UI", 12, FontStyle.Regular), ForeColor = Color.FromArgb(110, 123, 142),
            TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(12, 0, 0, 0)
        }, 1, 0);
        root.Controls.Add(footer, 0, 8);
        Style(root); SetStatus("Готово. Выберите целевое окно и нажмите F6. Остановка — F9.");
        name.TextChanged += (_, _) => Changed(); repeats.ValueChanged += (_, _) => Changed(); gap.ValueChanged += (_, _) => Changed();
        steps.ListChanged += (_, _) => { Changed(); UpdateSummary(); };
        kind.SelectedIndexChanged += (_, _) => { key.Enabled = kind.SelectedIndex != 3; duration.Enabled = kind.SelectedIndex is 0 or 3; };
        grid.SelectionChanged += (_, _) => LoadEditor();
        triggerButton.Click += (_, _) => CaptureTrigger();
        recordButton.Click += async (_, _) => await StartRecording();
        playButton.Click += async (_, _) => await StartPlayback(true);
        Shown += (_, _) =>
        {
            key.SelectedValue = (int)Keys.A;
            try { hook = new KeyboardHook { Handler = OnKey }; mouseHook = new MouseHook { Handler = OnMouse }; }
            catch (Exception e) { hook?.Dispose(); hook = null; mouseHook?.Dispose(); mouseHook = null; Error(e); Close(); return; }
            UpdateSummary();
        };
        FormClosing += OnClosing;
        FormClosed += (_, _) => { hook?.Dispose(); mouseHook?.Dispose(); };
    }

    sealed record KeyChoice(int Code, string Name);
    FlowLayoutPanel Row() => new() { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 5, 0, 0) };
    Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(4, 9, 9, 3), ForeColor = Muted };
    Button Button(string text, EventHandler action, int width) { var b = new Button { Text = text, Width = width }; b.Click += action; return b; }
    void Style(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is Button b) { b.Height = 33; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Color.FromArgb(58, 70, 88); b.BackColor = Surface; b.Cursor = Cursors.Hand; }
            if (c is TextBox or ComboBox or NumericUpDown) { c.BackColor = Surface; c.ForeColor = Ink; }
            if (c is not DataGridView) Style(c);
        }
    }
    void AddKeyRow(FlowLayoutPanel parent, int[] codes)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        foreach (var code in codes)
        {
            var text = KeyNames.Name(code); var b = Button(text, (_, _) => { key.SelectedValue = code; AddStep(EditorStep()); }, KeyNames.IsMouse(code) ? 145 : text.Length > 5 ? 92 : text.Length > 2 ? 66 : 48);
            b.Margin = new Padding(0, 1, 5, 1); row.Controls.Add(b);
        }
        parent.Controls.Add(row);
    }
    int Selected => grid.CurrentRow?.Index ?? -1;
    Step EditorStep() => new() { Kind = (StepKind)kind.SelectedIndex, Key = (int)(key.SelectedValue ?? (int)Keys.A), Milliseconds = (int)duration.Value };
    void AddStep(Step step)
    {
        if (steps.Count >= 20000) { SetStatus("Достигнут предел: 20 000 шагов."); return; }
        var index = Selected >= 0 ? Selected + 1 : steps.Count; steps.Insert(index, step); SelectRow(index);
    }
    void SelectRow(int index) { if (index >= 0 && index < grid.Rows.Count) { grid.CurrentCell = grid.Rows[index].Cells[0]; grid.Rows[index].Selected = true; } }
    void LoadEditor()
    {
        if (Selected < 0 || Selected >= steps.Count) return;
        var s = steps[Selected]; kind.SelectedIndex = (int)s.Kind; key.SelectedValue = s.Key; duration.Value = s.Milliseconds;
    }
    void UpdateStep() { if (Selected >= 0) { var i = Selected; var edited = EditorStep(); if (edited.Key == steps[i].Key) { edited.ScanCode = steps[i].ScanCode; edited.Extended = steps[i].Extended; } steps[i] = edited; SelectRow(i); } }
    void MoveStep(int delta) { var i = Selected; var j = i + delta; if (i < 0 || j < 0 || j >= steps.Count) return; var s = steps[i]; steps.RemoveAt(i); steps.Insert(j, s); SelectRow(j); }
    void Changed() { if (!loading) { dirty = true; Text = "Blaze Macro · " + name.Text + " *"; } }
    void UpdateSummary() { summary.Text = $"  {steps.Count} шагов · {steps.Sum(s => s.Kind is StepKind.Wait or StepKind.Tap ? (long)s.Milliseconds : 0) / 1000.0:0.##} с / повтор"; }
    void SetStatus(string text) => status.Text = text;
    bool Confirm(string text) => MessageBox.Show(this, text, "Blaze Macro", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    void Error(Exception e) { SetStatus(e.Message); MessageBox.Show(this, e.Message, "Blaze Macro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    Macro Snapshot() => new() { Version = timingMode != TimingMode.Standard ? 3 : steps.Any(s => s.Kind != StepKind.Wait && KeyNames.IsMouse(s.Key)) ? 2 : 1, Timing = timingMode, Name = name.Text.Trim(), Trigger = trigger, Repeats = (int)repeats.Value, RepeatDelay = (int)gap.Value, Steps = steps.Select(s => s.Copy()).ToList() };
    bool SaveMacro(bool saveAs = false)
    {
        try
        {
            var m = Snapshot(); m.Validate(); var path = filePath;
            if (saveAs || path == null) { using var d = new SaveFileDialog { Filter = "Blaze Macro (*.blaze.json)|*.blaze.json", FileName = "macro.blaze.json", AddExtension = true }; if (d.ShowDialog(this) != DialogResult.OK) return false; path = d.FileName; }
            MacroFile.Save(path, m); filePath = path; dirty = false; Text = "Blaze Macro · " + m.Name; SetStatus("Сохранено: " + path); return true;
        }
        catch (Exception e) { Error(e); return false; }
    }
    bool CanDiscard()
    {
        if (!dirty) return true;
        var result = MessageBox.Show(this, "Сохранить изменения макроса?", "Blaze Macro", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return result == DialogResult.No || result == DialogResult.Yes && SaveMacro();
    }
    void Apply(Macro m, string? path)
    {
        loading = true; name.Text = m.Name; trigger = m.Trigger; triggerButton.Text = KeyNames.Name(trigger) + " · изменить";
        repeats.Value = m.Repeats; gap.Value = m.RepeatDelay;
        steps.RaiseListChangedEvents = false;
        try { steps.Clear(); foreach (var s in m.Steps) steps.Add(s); }
        finally { steps.RaiseListChangedEvents = true; steps.ResetBindings(); }
        timingMode = m.Timing; timingButton.Text = "Паузы: " + TimingBenchmark.Name(timingMode) + "…";
        loading = false; dirty = false; filePath = path; Text = "Blaze Macro · " + m.Name; UpdateSummary();
    }
    void NewMacro() { if (CanDiscard()) { Apply(new Macro(), null); SetStatus("Новый макрос. Добавьте шаги или начните запись."); } }
    void OpenMacro()
    {
        if (!CanDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = "Blaze Macro (*.blaze.json)|*.blaze.json|JSON (*.json)|*.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { Apply(MacroFile.Load(dialog.FileName), dialog.FileName); SetStatus("Макрос открыт. Запуск: " + KeyNames.Name(trigger)); } catch (Exception e) { Error(e); }
    }
    void SetBusy(bool value)
    {
        busy = value; foreach (var c in editors) c.Enabled = !value;
        recordButton.Enabled = playButton.Enabled = !value;
    }
    void CaptureTrigger()
    {
        pendingTrigger = null; capturingTrigger = true; SetBusy(true); SetStatus("Нажмите клавишу запуска. Esc — отмена; F9 — остановка.");
    }
    bool OnKey(KeyEvent e)
    {
        if (e.Injected) return false;
        bool first = e.Up ? false : physical.Add(e.Key);
        if (e.Up) physical.Remove(e.Key);
        if (e.Up && swallowed.Remove(e.Key))
        {
            var launch = pendingTrigger == e.Key;
            if (launch) pendingTrigger = null;
            if (launch && Enabled && !busy && armed.Checked && e.Key == trigger && !Native.OwnForeground()) BeginInvoke(async () => { if (Enabled && armed.Checked) await StartPlayback(false); });
            return true;
        }
        if (capturingTrigger)
        {
            if (!e.Up && first)
            {
                swallowed.Add(e.Key);
                if (e.Key == (int)Keys.Escape) { capturingTrigger = false; SetBusy(false); SetStatus("Назначение отменено."); }
                else if (KeyNames.IsTrigger(e.Key)) { trigger = e.Key; triggerButton.Text = KeyNames.Name(trigger) + " · изменить"; capturingTrigger = false; SetBusy(false); Changed(); SetStatus("Клавиша запуска: " + KeyNames.Name(trigger)); }
                else SetStatus("Выберите обычную клавишу или F1–F12, кроме F9. Esc — отмена.");
            }
            return true;
        }
        if (e.Key == (int)Keys.F9 && busy)
        {
            if (!e.Up) { swallowed.Add(e.Key); if (first) BeginInvoke(Stop); }
            return true;
        }
        if (recording)
        {
            recorder.Accept(e);
            QueueStopIfFull();
            return false;
        }
        if (!e.Up && swallowed.Contains(e.Key)) return true;
        if (Enabled && armed.Checked && e.Key == trigger && !e.Up && !Native.OwnForeground())
        {
            if (!busy && first && !physical.Any(k => k != trigger)) { swallowed.Add(e.Key); pendingTrigger = e.Key; }
            return swallowed.Contains(e.Key);
        }
        return false;
    }
    bool OnMouse(KeyEvent e, Point point)
    {
        if (e.Injected) return false;
        if (recording && (!Native.OwnPoint(point) || e.Up))
        {
            recorder.Accept(e);
            QueueStopIfFull();
        }
        return false;
    }
    void QueueStopIfFull()
    {
        if (recorder.IsFull && !stopQueued) { stopQueued = true; BeginInvoke(() => { stopQueued = false; if (recording) Stop(); }); }
    }
    async Task Countdown(string action, CancellationToken token)
    {
        for (var i = 3; i > 0; i--) { SetStatus($"{action} через {i}… Переключитесь в целевое окно. F9 — отмена."); await Task.Delay(1000, token); }
    }
    async Task StartRecording()
    {
        if (busy) return;
        if (steps.Count > 0 && !Confirm("Новая запись заменит текущие шаги. Продолжить?")) return;
        SetBusy(true); operation = new CancellationTokenSource();
        try { await Countdown("Запись", operation.Token); recorder.Start(); recording = true; SetStatus("● ЗАПИСЬ КЛАВИАТУРЫ И МЫШИ · Координаты не записываются. F9 — завершить."); }
        catch (OperationCanceledException) { SetStatus("Запись отменена."); FinishOperation(); }
    }
    async Task StartPlayback(bool countdown)
    {
        if (busy) return;
        Macro m;
        try { m = Snapshot(); m.Validate(true); } catch (Exception e) { Error(e); return; }
        SetBusy(true); operation = new CancellationTokenSource(); var token = operation.Token;
        try
        {
            if (countdown) await Countdown("Воспроизведение", token);
            if (Native.OwnForeground()) { SetStatus("Выберите другое окно для воспроизведения."); return; }
            if (physical.Count > 0 || KeyNames.All.Concat(KeyNames.MouseKeys).Any(k => (Native.GetAsyncKeyState(k) & 0x8000) != 0)) { SetStatus("Отпустите клавиши и кнопки мыши и запустите макрос ещё раз."); return; }
            SetStatus("▶ ВОСПРОИЗВЕДЕНИЕ · F9 — остановить.");
            // Run off the UI thread so long runs of zero-delay events cannot block the stop hotkey.
            await new Player().Play(m, token);
            SetStatus("Воспроизведение завершено. Запуск: " + KeyNames.Name(trigger));
        }
        catch (OperationCanceledException) { SetStatus("Воспроизведение остановлено."); }
        catch (Exception e) { Error(e); }
        finally { FinishOperation(); }
    }
    void FinishOperation() { operation?.Dispose(); operation = null; SetBusy(false); if (closeRequested) BeginInvoke(Close); }
    void Stop()
    {
        pendingTrigger = null;
        if (capturingTrigger) { capturingTrigger = false; FinishOperation(); SetStatus("Назначение отменено."); return; }
        if (recording)
        {
            recording = false; recorder.Stop(); steps.RaiseListChangedEvents = false; steps.Clear(); foreach (var s in recorder.Steps) steps.Add(s);
            steps.RaiseListChangedEvents = true; steps.ResetBindings(); SetStatus($"Запись завершена: {steps.Count} шагов. Можно редактировать и сохранить."); FinishOperation();
        }
        else operation?.Cancel();
    }
    void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (busy) { closeRequested = true; e.Cancel = true; Stop(); return; }
        closeRequested = false; e.Cancel = !CanDiscard();
    }
}
