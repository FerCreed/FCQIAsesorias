using Avalonia.Controls;

namespace FCQI.Web.Views;

// Vista de reportes y tablero de métricas. El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class ReportsView : UserControl
{
    // Construye los controles declarados en ReportsView.axaml.
    public ReportsView() => InitializeComponent();
}
