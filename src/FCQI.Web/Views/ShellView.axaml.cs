using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FCQI.Web.Views;

// Marco de la aplicación: navegación, menú ☰ y área de contenido. Alterna el
// diseño de teléfono con la clase de estilo "compact" según el ancho disponible.
public partial class ShellView : UserControl
{
    // Por debajo de este ancho (ya escalado por el zoom) la app usa el diseño de teléfono.
    public const double CompactWidth = 640;

    // Carga el XAML y engancha el cambio de diseño al tamaño de la raíz.
    public ShellView()
    {
        InitializeComponent();
        // Se mide la raíz y no la vista: dentro del zoom, su ancho ya está dividido por la escala.
        Root.SizeChanged += (_, e) => Root.Classes.Set("compact", e.NewSize.Width < CompactWidth);
    }

    // Al salir se cierra el menú ☰. Se pospone: Click llega antes que el comando, y cerrar antes lo cancelaría.
    private void CloseOptions(object? sender, RoutedEventArgs e)
        => Dispatcher.UIThread.Post(() => OptionsButton.Flyout?.Hide());
}
