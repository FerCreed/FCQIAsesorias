using CommunityToolkit.Mvvm.ComponentModel;
using FCQI.Web.Models;

namespace FCQI.Web.ViewModels;

/// <summary>Un día de la semana como interruptor de filtro.</summary>
public partial class DayToggle : ObservableObject
{
    public DayToggle(int value) => (Value, Name) = (value, DayNames.Of(value)[..3]);

    public int Value { get; }
    public string Name { get; }
    public string FullName => DayNames.Of(Value);

    [ObservableProperty] private bool _isChecked;

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

/// <summary>Páginas con datos de la persona: se vacían al salir (BPMN 17).</summary>
public interface IResettable
{
    void Reset();
}
