using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace FCQI.Web.Views;

/// <summary>
/// Ícono Fluent + texto, para el contenido de botones y pestañas:
/// <c>&lt;v:IconText Icon="{StaticResource Icon.Search}" Text="Buscar" /&gt;</c>.
/// Con <see cref="Vertical"/> el texto va debajo (pestañas del teléfono). El
/// texto se copia como nombre accesible del botón que lo contiene.
/// </summary>
public class IconText : StackPanel
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<IconText, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconText, string?>(nameof(Text));

    public static readonly StyledProperty<bool> VerticalProperty =
        AvaloniaProperty.Register<IconText, bool>(nameof(Vertical));

    private readonly PathIcon _icon = new();
    private readonly TextBlock _text = new() { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.NoWrap };

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

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool Vertical { get => GetValue(VerticalProperty); set => SetValue(VerticalProperty, value); }

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

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        NameParent();
    }

    private void NameParent()
    {
        if (Parent is Control parent && !string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(AutomationProperties.GetName(parent)))
        {
            AutomationProperties.SetName(parent, Text);
        }
    }
}
