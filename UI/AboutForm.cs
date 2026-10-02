using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using QirimType.Utils;

namespace QirimType.UI;

public class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "О программе QırımType";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 510);
        BackColor = Color.FromArgb(248, 249, 250);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        Icon = IconHelper.CreateAppIcon(true, 32);

        var tablePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24, 20, 24, 20),
            AutoSize = true
        };
        tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Header with Logo and Name
        var headerPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        var logoBox = new PictureBox
        {
            Size = new Size(48, 48),
            Image = IconHelper.CreateAppIcon(true, 48).ToBitmap(),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Margin = new Padding(0, 0, 14, 0)
        };

        var titleBox = new Panel
        {
            AutoSize = true,
            Margin = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = "QırımType",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 102, 204),
            AutoSize = true,
            Location = new Point(0, 0)
        };

        var infoVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var displayVersion = !string.IsNullOrEmpty(infoVersion)
            ? infoVersion.Split('+')[0]
            : (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

        var versionLabel = new Label
        {
            Text = $"Версия {displayVersion} (Windows x64)",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(108, 117, 125),
            AutoSize = true,
            Location = new Point(2, 28)
        };

        titleBox.Controls.Add(titleLabel);
        titleBox.Controls.Add(versionLabel);
        headerPanel.Controls.Add(logoBox);
        headerPanel.Controls.Add(titleBox);

        // Subtitle description
        var descLabel = new Label
        {
            Text = "Kırım Tatar keyboard helper for Windows.\n\n" +
                   "Удобный ввод крымскотатарских букв на стандартной английской раскладке клавиатуры. " +
                   "Работает в фоне во всех приложениях Windows без изменения системных раскладок.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(33, 37, 41),
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 15)
        };

        // Table with Hotkeys
        var groupKeys = new GroupBox
        {
            Text = "Горячие клавиши по умолчанию",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(73, 80, 87),
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            Height = 215
        };

        var listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(33, 37, 41),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        listView.Columns.Add("Символ", 75);
        listView.Columns.Add("Сочетание", 110);
        listView.Columns.Add("Описание", 180);

        var items = new (string Symbol, string Hotkey, string Name)[]
        {
            ("ğ", "Alt + G", "ğ (g with breve)"),
            ("ı", "Alt + I", "ı (dotless i)"),
            ("ñ", "Alt + N", "ñ (n with tilde)"),
            ("ö", "Alt + O", "ö (o with diaeresis)"),
            ("ü", "Alt + U", "ü (u with diaeresis)"),
            ("ç", "Alt + C", "ç (c with cedilla)"),
            ("ş", "Alt + S", "ş (s with cedilla)"),
            ("â", "Alt + A", "â (a with circumflex)")
        };

        foreach (var (sym, hk, name) in items)
        {
            var lvi = new ListViewItem(sym);
            lvi.SubItems.Add(hk);
            lvi.SubItems.Add(name);
            listView.Items.Add(lvi);
        }
        groupKeys.Controls.Add(listView);

        // Footer button
        var bottomPanel = new Panel
        {
            Height = 45,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, 10, 0, 0)
        };

        var btnOk = new Button
        {
            Text = "Закрыть",
            DialogResult = DialogResult.OK,
            Size = new Size(110, 32),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(tablePanel.Width - 110 - 24, 8),
            BackColor = Color.FromArgb(0, 122, 217),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderSize = 0;
        bottomPanel.Controls.Add(btnOk);

        tablePanel.Controls.Add(headerPanel);
        tablePanel.Controls.Add(descLabel);
        tablePanel.Controls.Add(groupKeys);
        tablePanel.Controls.Add(bottomPanel);

        Controls.Add(tablePanel);
        AcceptButton = btnOk;
        CancelButton = btnOk;
    }
}
