using System.Windows;
using FlashLock.Core;

namespace FlashLock.App;

public partial class MainWindow : Window
{
    private readonly PortableDriveLocator _locator = new();
    private readonly ConfigStore _configStore = new();
    private readonly ElevatedHelperClient _helper = new();
    private PortableDriveInfo? _drive;
    private ProtectionCompatibilityResult? _compatibility;
    private FlashLockConfig? _config;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDriveContextAsync();
    }

    private async Task LoadDriveContextAsync()
    {
        try
        {
            _drive = _locator.LocateFromExecutable();
            _compatibility = ProtectionCompatibility.Evaluate(_drive);
            _config = await _configStore.LoadAsync(_drive.RootPath);

            var label = string.IsNullOrWhiteSpace(_drive.VolumeLabel)
                ? _drive.RootPath
                : $"{_drive.VolumeLabel} ({_drive.RootPath})";

            DriveNameText.Text = label;
            DriveDetailsText.Text = $"{_drive.FileSystem} • {_drive.DriveType} • {FormatBytes(_drive.TotalSize)} total";
            DriveIdentityText.Text = $"Volume ID: {_drive.VolumeSerialNumber}";
            CompatibilityIcon.Text = _compatibility.CanProtect ? "✓" : "!";
            StatusDescriptionText.Text = _compatibility.Message;
            RenderProtectionState();
        }
        catch (Exception ex)
        {
            DriveNameText.Text = "Drive detection failed";
            DriveDetailsText.Text = ex.Message;
            CompatibilityIcon.Text = "!";
            StatusText.Text = "UNAVAILABLE";
            StatusDescriptionText.Text = "FlashLock could not safely identify the drive containing this executable.";
            PrimaryButton.IsEnabled = false;
        }
    }

    private void RenderProtectionState()
    {
        if (_drive is null || _compatibility is null)
        {
            return;
        }

        RecoveryButton.Visibility = Visibility.Collapsed;
        PrimaryButton.IsEnabled = !_busy && _compatibility.CanProtect;

        if (!_compatibility.CanProtect)
        {
            StatusText.Text = "NOT AVAILABLE";
            PrimaryButton.Content = "Protection unavailable";
            PrimaryButton.IsEnabled = false;
            return;
        }

        if (_config is null)
        {
            StatusText.Text = "NOT CONFIGURED";
            StatusDescriptionText.Text = "Create an owner PIN and protect this NTFS USB. An ACL snapshot will be created before any permissions change.";
            PrimaryButton.Content = "Set up & protect";
            return;
        }

        switch (_config.State)
        {
            case ProtectionState.Unlocked:
                StatusText.Text = "UNLOCKED";
                StatusDescriptionText.Text = "Normal read/write access is enabled. Protect the drive before lending or ejecting it.";
                PrimaryButton.Content = "Protect drive";
                break;
            case ProtectionState.Protected:
                StatusText.Text = "PROTECTED";
                StatusDescriptionText.Text = "Normal users have read/execute access only. Enter the owner PIN to restore the original ACLs.";
                PrimaryButton.Content = "Unlock drive";
                break;
            case ProtectionState.Applying:
            case ProtectionState.Restoring:
            case ProtectionState.RecoveryRequired:
                StatusText.Text = "RECOVERY NEEDED";
                StatusDescriptionText.Text = "A previous permission operation did not finish cleanly. Use Recovery to restore the saved ACL snapshot.";
                PrimaryButton.IsEnabled = false;
                RecoveryButton.Visibility = Visibility.Visible;
                RecoveryButton.IsEnabled = !_busy;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_drive is null || _compatibility?.CanProtect != true || _busy)
        {
            return;
        }

        var isFirstSetup = _config is null;
        var action = _config?.State == ProtectionState.Protected ? HelperAction.Unlock : HelperAction.Protect;
        var dialog = new PinDialog(confirm: isFirstSetup) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await ExecuteOperationAsync(action, dialog.Pin);
    }

    private async void RecoveryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_drive is null || _busy)
        {
            return;
        }

        var dialog = new PinDialog(confirm: false) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await ExecuteOperationAsync(HelperAction.Recover, dialog.Pin);
    }

    private async Task ExecuteOperationAsync(HelperAction action, string pin)
    {
        if (_drive is null)
        {
            return;
        }

        _busy = true;
        PrimaryButton.IsEnabled = false;
        RecoveryButton.IsEnabled = false;
        OperationText.Text = action switch
        {
            HelperAction.Protect => "Creating rollback snapshot and applying read-only protection…",
            HelperAction.Unlock => "Restoring the original filesystem permissions…",
            HelperAction.Recover => "Recovering the saved filesystem permissions…",
            _ => "Working…"
        };

        try
        {
            var response = await _helper.ExecuteAsync(new HelperRequest(
                action,
                _drive.RootPath,
                pin,
                _drive.VolumeSerialNumber));

            OperationText.Text = response.Message;
            if (!response.Success)
            {
                MessageBox.Show(this, response.Message, "FlashLock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            OperationText.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "FlashLock", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
            await LoadDriveContextAsync();
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double gb = 1024d * 1024d * 1024d;
        return $"{bytes / gb:0.##} GB";
    }
}
