using System.Windows;

namespace FlashLock.App;

public partial class PinDialog : Window
{
    private readonly bool _confirm;

    public PinDialog(bool confirm)
    {
        InitializeComponent();
        _confirm = confirm;
        ConfirmPanel.Visibility = confirm ? Visibility.Visible : Visibility.Collapsed;
        HeadingText.Text = confirm ? "Create owner PIN" : "Enter owner PIN";
        DescriptionText.Text = confirm
            ? "Choose at least 6 characters. FlashLock stores only a salted password hash."
            : "Enter the owner PIN to change this USB's protection state.";
        Loaded += (_, _) => PinBox.Focus();
    }

    public string Pin { get; private set; } = string.Empty;

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (PinBox.Password.Length < 6)
        {
            ErrorText.Text = "Use at least 6 characters.";
            return;
        }

        if (_confirm && !string.Equals(PinBox.Password, ConfirmPinBox.Password, StringComparison.Ordinal))
        {
            ErrorText.Text = "The two PIN/passphrase values do not match.";
            return;
        }

        Pin = PinBox.Password;
        DialogResult = true;
    }
}
