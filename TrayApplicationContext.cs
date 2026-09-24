using System.Drawing;
using System.Windows.Forms;
using QirimType.Configuration;
using QirimType.Keyboard;
using QirimType.UI;
using QirimType.Utils;

namespace QirimType;

public class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsManager _settingsManager;
    private readonly HotkeyManager _hotkeyManager;
    private readonly GlobalKeyboardHook _keyboardHook;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;

    private readonly ToolStripMenuItem _itemHeader;
    private readonly ToolStripMenuItem _itemEnabled;
    private readonly ToolStripMenuItem _itemSettings;
    private readonly ToolStripMenuItem _itemStartWithWindows;
    private readonly ToolStripMenuItem _itemAbout;
    private readonly ToolStripMenuItem _itemExit;

    private SettingsForm? _settingsForm;
    private AboutForm? _aboutForm;

    public TrayApplicationContext()
    {
        _settingsManager = new SettingsManager();
        _hotkeyManager = new HotkeyManager(_settingsManager);
        _keyboardHook = new GlobalKeyboardHook(_hotkeyManager);

        // Build context menu
        _contextMenu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9f),
            RenderMode = ToolStripRenderMode.System
        };

        _itemHeader = new ToolStripMenuItem("QırımType")
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Enabled = false
        };

        _itemEnabled = new ToolStripMenuItem("Включено (Enabled)")
        {
            Checked = _settingsManager.Settings.IsEnabled,
            CheckOnClick = true
        };
        _itemEnabled.Click += OnToggleEnabled;

        _itemSettings = new ToolStripMenuItem("Настройки (Settings)...");
        _itemSettings.Click += OnOpenSettings;

        _itemStartWithWindows = new ToolStripMenuItem("Автозапуск (Start with Windows)")
        {
            Checked = _settingsManager.Settings.StartWithWindows,
            CheckOnClick = true
        };
        _itemStartWithWindows.Click += OnToggleAutostart;

        _itemAbout = new ToolStripMenuItem("О программе (About)...");
        _itemAbout.Click += OnOpenAbout;

        _itemExit = new ToolStripMenuItem("Выход (Exit)");
        _itemExit.Click += OnExit;

        _contextMenu.Items.Add(_itemHeader);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_itemEnabled);
        _contextMenu.Items.Add(_itemSettings);
        _contextMenu.Items.Add(_itemStartWithWindows);
        _contextMenu.Items.Add(_itemAbout);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_itemExit);

        // NotifyIcon setup
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => OpenSettings();

        // React to settings changes
        _settingsManager.SettingsChanged += OnSettingsChanged;

        UpdateTrayVisuals(_settingsManager.Settings.IsEnabled);

        // Install global hook
        try
        {
            _keyboardHook.Install();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось установить глобальный перехват клавиатуры:\n{ex.Message}",
                "Ошибка QırımType",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UpdateTrayVisuals(bool isEnabled)
    {
        _notifyIcon.Icon = IconHelper.CreateAppIcon(isEnabled, 16);
        _notifyIcon.Text = isEnabled
            ? "QırımType — Включено (Alt + буква)"
            : "QırımType — Приостановлено";

        _itemEnabled.Checked = isEnabled;
        _itemStartWithWindows.Checked = _settingsManager.Settings.StartWithWindows;
    }

    private void OnToggleEnabled(object? sender, EventArgs e)
    {
        bool newState = _itemEnabled.Checked;
        _settingsManager.SetEnabled(newState);
        UpdateTrayVisuals(newState);
    }

    private void OnToggleAutostart(object? sender, EventArgs e)
    {
        bool newState = _itemStartWithWindows.Checked;
        _settingsManager.SetStartWithWindows(newState);
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        UpdateTrayVisuals(settings.IsEnabled);
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        OpenSettings();
    }

    private void OpenSettings()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_settingsManager);
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }
    }

    private void OnOpenAbout(object? sender, EventArgs e)
    {
        if (_aboutForm == null || _aboutForm.IsDisposed)
        {
            _aboutForm = new AboutForm();
            _aboutForm.Show();
        }
        else
        {
            _aboutForm.WindowState = FormWindowState.Normal;
            _aboutForm.BringToFront();
            _aboutForm.Activate();
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        ExitApp();
    }

    private void ExitApp()
    {
        _notifyIcon.Visible = false;
        _keyboardHook.Dispose();
        _notifyIcon.Dispose();
        _contextMenu.Dispose();

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyboardHook.Dispose();
            _notifyIcon.Dispose();
            _contextMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
