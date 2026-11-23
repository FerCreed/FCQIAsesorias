using Avalonia.Controls;

namespace FCQI.Web.Views;

// Vista de inicio de sesión. El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class LoginView : UserControl
{
    // Construye los controles declarados en LoginView.axaml.
    public LoginView() => InitializeComponent();
}
