using Avalonia.Controls;

namespace FCQI.Web.Views;

// Vista de dirección para asignar materias a los asesores. El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class AdminView : UserControl
{
    // Construye los controles declarados en AdminView.axaml.
    public AdminView() => InitializeComponent();
}
