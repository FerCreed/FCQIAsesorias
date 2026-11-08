using Avalonia;
using Avalonia.Controls;

namespace FCQI.Web.Views;

public partial class ShellView : UserControl
{
    /// <summary>Por debajo de este ancho la app usa el diseño de teléfono.</summary>
    public const double CompactWidth = 640;

    public ShellView() => InitializeComponent();

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Root.Classes.Set("compact", e.NewSize.Width < CompactWidth);
    }
}
