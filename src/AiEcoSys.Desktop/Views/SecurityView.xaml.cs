using System.Windows;

namespace AiEcoSys.Desktop.Views;

/// <summary>
/// Interaction logic for SecurityView.xaml. Credential entry uses a
/// <see cref="System.Windows.Controls.PasswordBox"/> so the secret is never
/// exposed through data binding.
/// </summary>
public partial class SecurityView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityView"/> class.
    /// </summary>
    public SecurityView()
    {
        InitializeComponent();
    }

    private void StoreCredential_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.StoreCredential(CredentialValueBox.Password);
            CredentialValueBox.Clear();
        }
    }
}
