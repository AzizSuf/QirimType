using System.Drawing;
using System.Windows.Forms;
using QirimType.Utils;

namespace QirimType.UI;

public class KeyCaptureDialog : Form
{
    public Keys SelectedKey { get; private set; }

    public KeyCaptureDialog(string symbol, Keys currentKey)
    {
        SelectedKey = currentKey;
        InitializeComponent(symbol, currentKey);
    }

    private void InitializeComponent(string symbol, Keys currentKey)
    {
        Text = $"Назначение клавиши: {symbol}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 200);
        BackColor = Color.FromArgb(248, 249, 250);
        Font = new Font("Segoe UI", 9.5f);
        Icon = IconHelper.CreateAppIcon(true, 32);
        KeyPreview = true;

        var lblPrompt = new Label
        {
            Text = $"Нажмите клавишу на клавиатуре для символа «{symbol}»:",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 37, 41),
            Location = new Point(25, 20),
            Size = new Size(330, 40)
        };

        var lblStatus = new Label
        {
            Text = $"Alt + {currentKey}",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 102, 204),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(25, 65),
            Size = new Size(330, 45),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        var lblHint = new Label
        {
            Text = "Нажмите любую буквенную клавишу (A-Z). Нажмите Esc для отмены.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(108, 117, 125),
            Location = new Point(25, 115),
            Size = new Size(330, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnCancel = new Button
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
            Location = new Point(135, 150),
            Size = new Size(110, 32),
            BackColor = Color.FromArgb(230, 233, 236),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        Controls.Add(lblPrompt);
        Controls.Add(lblStatus);
        Controls.Add(lblHint);
        Controls.Add(btnCancel);

        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            // Filter out modifier keys themselves
            if (e.KeyCode is Keys.Menu or Keys.LMenu or Keys.RMenu or
                Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
                Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or
                Keys.LWin or Keys.RWin)
            {
                return;
            }

            // Accept key
            SelectedKey = e.KeyCode;
            lblStatus.Text = $"Alt + {SelectedKey}";
            e.Handled = true;
            e.SuppressKeyPress = true;

            DialogResult = DialogResult.OK;
            Close();
        };

        CancelButton = btnCancel;
    }
}
