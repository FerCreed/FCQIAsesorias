using CommunityToolkit.Mvvm.ComponentModel;
using FCQI.Web.Models;

namespace FCQI.Web.ViewModels;

// Un día de la semana como interruptor de filtro.
public partial class DayToggle : ObservableObject
{
    // Crea el interruptor con el nombre abreviado a tres letras para caber en teléfono.
    public DayToggle(int value) => (Value, Name) = (value, DayNames.Of(value)[..3]);

    // Número del día (0=domingo…6=sábado), igual que DayOfWeek.
    public int Value { get; }

    // Nombre corto que se muestra en el botón.
    public string Name { get; }

    // Nombre completo, para la descripción accesible del botón.
    public string FullName => DayNames.Of(Value);

    // Genera IsChecked con notificación de cambio para el enlace de datos.
    [ObservableProperty] private bool _isChecked;

    // Fábrica de lunes a sábado (no hay asesorías en domingo). Engancha
    // onChanged a cada interruptor para que el filtro se
    // recalcule al marcar o desmarcar cualquiera.
    public static List<DayToggle> Weekdays(Action onChanged)
    {
        var days = Enumerable.Range(1, 6).Select(d => new DayToggle(d)).ToList();
        foreach (var day in days)
        {
            day.PropertyChanged += (_, _) => onChanged();
        }

        return days;
    }
}

// Páginas con datos de la persona: se vacían al salir (BPMN 17).
public interface IResettable
{
    // Borra los datos cargados para que la siguiente persona no vea los de la anterior.
    void Reset();
}
