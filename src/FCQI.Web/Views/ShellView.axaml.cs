using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FCQI.Web.Views;

public partial class ShellView : UserControl
{
    /// <summary>Por debajo de este ancho (ya escalado por el zoom) la app usa el diseño de teléfono.</summary>
    public const double CompactWidth = 640;

    public ShellView()
    {
        InitializeComponent();
        // Se mide la raíz y no la vista: dentro del zoom, su ancho ya está dividido por la escala.
        Root.SizeChanged += (_, e) => Root.Classes.Set("compact", e.NewSize.Width < CompactWidth);
    }

    /// <summary>Al salir se cierra el menú ☰. Se pospone: Click llega antes que el comando, y cerrar antes lo cancelaría.</summary>
    private void CloseOptions(object? sender, RoutedEventArgs e)
        => Dispatcher.UIThread.Post(() => OptionsButton.Flyout?.Hide());
}
