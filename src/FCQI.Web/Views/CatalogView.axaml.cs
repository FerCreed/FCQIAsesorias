using Avalonia.Controls;

namespace FCQI.Web.Views;

// Vista del catálogo donde el alumno busca asesor y solicita asesoría. El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class CatalogView : UserControl
{
    // Construye los controles declarados en CatalogView.axaml.
    public CatalogView() => InitializeComponent();
}
