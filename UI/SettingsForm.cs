using System.Drawing;
using System.Windows.Forms;
using QirimType.Configuration;
using QirimType.Utils;

namespace QirimType.UI;

public class SettingsForm : Form
{
    private readonly SettingsManager _settingsManager;
    private AppSettings _workingSettings;
    private readonly List<(HotkeyMapping Mapping, Label KeyLabel)> _rowBindings = new();

    private CheckBox _chkEnabled = null!;
    private CheckBox _chkAutostart = null!;
    private CheckBox _chkOnlyEnglish = null!;

    public SettingsForm(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _workingSettings = _settingsManager.Settings.Clone();

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "QırımType — Настройки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(540, 610);
        BackColor = Color.FromArgb(248, 249, 250);
        Font = new Font("Segoe UI", 9.5f);
        Icon = IconHelper.CreateAppIcon(true, 32);

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20, 16, 20, 16)
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 1. Title / Header
        var titlePanel = new Panel { Height = 45, Dock = DockStyle.Top };
        var lblTitle = new Label
        {
            Text = "Горячие клавиши крымскотатарских букв",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 37, 41),
            AutoSize = true,
            Location = new Point(0, 0)
        };
        var lblSubtitle = new Label
        {
            Text = "Нажмите «Изменить», чтобы переназначить клавишу для нужного символа.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(108, 117, 125),
            AutoSize = true,
            Location = new Point(0, 24)
        };
        titlePanel.Controls.Add(lblTitle);
        titlePanel.Controls.Add(lblSubtitle);

        // 2. Mappings Group
        var groupHotkeys = new GroupBox
        {
            Text = "Комбинации клавиш",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(73, 80, 87),
            Padding = new Padding(12, 10, 12, 10)
        };

        var mappingTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 8,
            AutoScroll = true
        };
        mappingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));  // Symbol
        mappingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));  // Name
        mappingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));  // Shortcut
        mappingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));  // Change button

        BuildMappingRows(mappingTable);
        groupHotkeys.Controls.Add(mappingTable);

        // 3. Options Group
        var groupOptions = new GroupBox
        {
            Text = "Параметры работы",
            Dock = DockStyle.Top,
            Height = 112,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(73, 80, 87),
            Padding = new Padding(15, 10, 15, 10)
        };

        _chkEnabled = new CheckBox
        {
            Text = "Включить QırımType (активен ввод символов)",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(33, 37, 41),
            Checked = _workingSettings.IsEnabled,
            AutoSize = true,
            Location = new Point(15, 24)
        };

        _chkAutostart = new CheckBox
        {
            Text = "Запускать вместе с Windows (Start with Windows)",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(33, 37, 41),
            Checked = _workingSettings.StartWithWindows,
            AutoSize = true,
            Location = new Point(15, 50)
        };

        _chkOnlyEnglish = new CheckBox
        {
            Text = "Работать только в английской раскладке (ENG)",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(33, 37, 41),
            Checked = _workingSettings.OnlyEnglishLayout,
            AutoSize = true,
            Location = new Point(15, 76)
        };

        groupOptions.Controls.Add(_chkEnabled);
        groupOptions.Controls.Add(_chkAutostart);
        groupOptions.Controls.Add(_chkOnlyEnglish);

        // 4. Action Buttons
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnRestore = new Button
        {
            Text = "По умолчанию",
            Size = new Size(130, 32),
            Location = new Point(0, 6),
            BackColor = Color.FromArgb(240, 242, 245),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand
        };
        btnRestore.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);
        btnRestore.Click += (s, e) => RestoreDefaults();

        var btnSave = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Size = new Size(100, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(bottomPanel.Width - 215, 6),
            BackColor = Color.FromArgb(0, 122, 217),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => SaveAndClose();

        var btnCancel = new Button
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(bottomPanel.Width - 105, 6),
            BackColor = Color.FromArgb(230, 233, 236),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        bottomPanel.Controls.Add(btnRestore);
        bottomPanel.Controls.Add(btnSave);
        bottomPanel.Controls.Add(btnCancel);

        mainPanel.Controls.Add(titlePanel);
        mainPanel.Controls.Add(groupHotkeys);
        mainPanel.Controls.Add(groupOptions);
        mainPanel.Controls.Add(bottomPanel);

        Controls.Add(mainPanel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void BuildMappingRows(TableLayoutPanel panel)
    {
        panel.Controls.Clear();
        _rowBindings.Clear();

        int row = 0;
        foreach (var mapping in _workingSettings.Mappings)
        {
            var lblSymbol = new Label
            {
                Text = mapping.Symbol,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var lblName = new Label
            {
                Text = mapping.DisplayName,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(73, 80, 87),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };

            var lblKey = new Label
            {
                Text = mapping.ShortcutText,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 252),
                Margin = new Padding(3, 4, 3, 4),
                Dock = DockStyle.Fill
            };

            var btnChange = new Button
            {
                Text = "Изменить",
                Font = new Font("Segoe UI", 8.5f),
                Size = new Size(85, 28),
                BackColor = Color.FromArgb(240, 242, 245),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 3, 3)
            };
            btnChange.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);

            var capturedMapping = mapping;
            btnChange.Click += (s, e) =>
            {
                using var dlg = new KeyCaptureDialog(capturedMapping.Symbol, capturedMapping.Key);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Check if another symbol already uses this key
                    var conflict = _workingSettings.Mappings.FirstOrDefault(m => m != capturedMapping && m.Key == dlg.SelectedKey);
                    if (conflict != null)
                    {
                        MessageBox.Show(
                            this,
                            $"Клавиша {dlg.SelectedKey} уже назначена для символа «{conflict.Symbol}»!\nПожалуйста, выберите другую клавишу.",
                            "Конфликт клавиш",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    capturedMapping.Key = dlg.SelectedKey;
                    lblKey.Text = capturedMapping.ShortcutText;
                }
            };

            panel.Controls.Add(lblSymbol, 0, row);
            panel.Controls.Add(lblName, 1, row);
            panel.Controls.Add(lblKey, 2, row);
            panel.Controls.Add(btnChange, 3, row);

            _rowBindings.Add((mapping, lblKey));
            row++;
        }
    }

    private void RestoreDefaults()
    {
        var result = MessageBox.Show(
            this,
            "Восстановить комбинации по умолчанию?\n\n" +
            "Alt+G → ğ\nAlt+I → ı\nAlt+N → ñ\nAlt+O → ö\nAlt+U → ü\nAlt+C → ç\nAlt+S → ş",
            "Сброс настроек",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _chkOnlyEnglish.Checked = true;
            var defaults = AppSettings.GetDefaultMappings();
            foreach (var def in defaults)
            {
                var match = _workingSettings.Mappings.FirstOrDefault(m => m.Symbol == def.Symbol);
                if (match != null)
                {
                    match.Key = def.Key;
                    match.RequireAlt = def.RequireAlt;
                }
            }

            foreach (var (mapping, label) in _rowBindings)
            {
                label.Text = mapping.ShortcutText;
            }
        }
    }

    private void SaveAndClose()
    {
        _workingSettings.IsEnabled = _chkEnabled.Checked;
        _workingSettings.StartWithWindows = _chkAutostart.Checked;
        _workingSettings.OnlyEnglishLayout = _chkOnlyEnglish.Checked;

        _settingsManager.SaveSettings(_workingSettings);
        Close();
    }
}
