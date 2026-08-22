using System.Windows;
using FlashLock.Core;

namespace FlashLock.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadDriveContext();
    }

    private void LoadDriveContext()
    {
        try
        {
            var locator = new PortableDriveLocator();
            var drive = locator.LocateFromExecutable();
            var compatibility = ProtectionCompatibility.Evaluate(drive);

            var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? drive.RootPath
                : $"{drive.VolumeLabel} ({drive.RootPath})";

            DriveNameText.Text = label;
            DriveDetailsText.Text = $"{drive.FileSystem} • {drive.DriveType} • {FormatBytes(drive.TotalSize)} total";
            CompatibilityIcon.Text = compatibility.CanProtect ? "✓" : "!";
            StatusDescriptionText.Text = compatibility.Message;
        }
        catch (Exception ex)
        {
            DriveNameText.Text = "Drive detection failed";
            DriveDetailsText.Text = ex.Message;
            CompatibilityIcon.Text = "!";
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double gb = 1024d * 1024d * 1024d;
        return $"{bytes / gb:0.##} GB";
    }
}
