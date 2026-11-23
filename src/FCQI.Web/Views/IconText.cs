using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace FCQI.Web.Views;

// Ícono Fluent + texto, para el contenido de botones y pestañas:
// <v:IconText Icon="{StaticResource Icon.Search}" Text="Buscar" />.
// Con Vertical el texto va debajo (pestañas del teléfono). El
// texto se copia como nombre accesible del botón que lo contiene.
public class IconText : StackPanel
{
    // Propiedades con estilo: permiten enlace de datos y valores desde estilos XAML.
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<IconText, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconText, string?>(nameof(Text));

    public static readonly StyledProperty<bool> VerticalProperty =
        AvaloniaProperty.Register<IconText, bool>(nameof(Vertical));

    // Controles hijos creados una vez; los cambios de propiedad solo los actualizan.
    private readonly PathIcon _icon = new();
    private readonly TextBlock _text = new() { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.NoWrap };

    // Arma el ícono y el texto en horizontal, el diseño por omisión.
    public IconText()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 8;
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Center;
        _icon.VerticalAlignment = VerticalAlignment.Center;
        Children.Add(_icon);
        Children.Add(_text);
    }

    // Geometría del ícono Fluent; sin ícono se oculta su espacio.
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    // Texto visible y nombre accesible del botón contenedor.
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    // Texto debajo del ícono, con letra menor, para las pestañas del teléfono.
    public bool Vertical { get => GetValue(VerticalProperty); set => SetValue(VerticalProperty, value); }

    // Sincroniza los hijos con la propiedad que cambió, sin reconstruirlos.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty)
        {
            _icon.Data = Icon;
            _icon.IsVisible = Icon is not null;
        }
        else if (change.Property == TextProperty)
        {
            _text.Text = Text;
            _text.IsVisible = !string.IsNullOrEmpty(Text);
            NameParent();
        }
        else if (change.Property == VerticalProperty)
        {
            Orientation = Vertical ? Orientation.Vertical : Orientation.Horizontal;
            Spacing = Vertical ? 2 : 8;
            _text.FontSize = Vertical ? 11.5 : double.NaN;
            _icon.HorizontalAlignment = HorizontalAlignment.Center;
            _text.HorizontalAlignment = HorizontalAlignment.Center;
        }
    }

    // El padre solo se conoce al entrar al árbol; ahí se nombra si el texto llegó antes.
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        NameParent();
    }

    // Da al botón contenedor el nombre accesible del texto, para lectores de
    // pantalla, sin pisar un nombre que se haya fijado a mano.
    private void NameParent()
    {
        if (Parent is Control parent && !string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(AutomationProperties.GetName(parent)))
        {
            AutomationProperties.SetName(parent, Text);
        }
    }
}
