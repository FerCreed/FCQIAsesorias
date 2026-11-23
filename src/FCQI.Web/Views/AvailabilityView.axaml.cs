using Avalonia.Controls;

namespace FCQI.Web.Views;

// Vista del asesor para administrar sus bloques de horario. El code-behind solo carga el XAML: toda la lógica vive en su
// ViewModel (MVVM) para poder probarla sin interfaz.
public partial class AvailabilityView : UserControl
{
    // Construye los controles declarados en AvailabilityView.axaml.
    public AvailabilityView() => InitializeComponent();
}
