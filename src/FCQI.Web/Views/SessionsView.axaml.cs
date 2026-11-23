using Avalonia.Controls;

namespace FCQI.Web.Views;

// Vista de las asesorías propias (próximas e historial). El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class SessionsView : UserControl
{
    // Construye los controles declarados en SessionsView.axaml.
    public SessionsView() => InitializeComponent();
}
