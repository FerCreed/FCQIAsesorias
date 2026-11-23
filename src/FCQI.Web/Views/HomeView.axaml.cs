using Avalonia.Controls;

namespace FCQI.Web.Views;

// Portada con los accesos del rol activo. El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class HomeView : UserControl
{
    // Construye los controles declarados en HomeView.axaml.
    public HomeView() => InitializeComponent();
}
