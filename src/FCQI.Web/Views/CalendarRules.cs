using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace FCQI.Web.Views;

/// <summary>
/// Propiedad adjunta para <see cref="Calendar"/> y <see cref="CalendarDatePicker"/>:
/// solo deja elegir las fechas indicadas y bloquea visualmente el resto del
/// rango mostrado. BlackoutDates no admite enlace de datos; esto lo suple.
/// </summary>
public static class CalendarRules
{
    public static readonly AttachedProperty<IEnumerable<DateTime>?> EnabledDatesProperty =
        AvaloniaProperty.RegisterAttached<Control, IEnumerable<DateTime>?>("EnabledDates", typeof(CalendarRules));

    static CalendarRules()
    {
        EnabledDatesProperty.Changed.AddClassHandler<Control>((control, _) => Apply(control));

        // Los límites pueden llegar después que las fechas: se recalcula también entonces.
        Calendar.DisplayDateStartProperty.Changed.AddClassHandler<Calendar>((c, _) => Apply(c));
        Calendar.DisplayDateEndProperty.Changed.AddClassHandler<Calendar>((c, _) => Apply(c));
        CalendarDatePicker.DisplayDateStartProperty.Changed.AddClassHandler<CalendarDatePicker>((c, _) => Apply(c));
        CalendarDatePicker.DisplayDateEndProperty.Changed.AddClassHandler<CalendarDatePicker>((c, _) => Apply(c));
    }

    public static IEnumerable<DateTime>? GetEnabledDates(Control control) => control.GetValue(EnabledDatesProperty);
    public static void SetEnabledDates(Control control, IEnumerable<DateTime>? value) => control.SetValue(EnabledDatesProperty, value);

    private static void Apply(Control control)
    {
        var enabled = GetEnabledDates(control)?.Select(d => d.Date).ToHashSet();

        switch (control)
        {
            case Calendar calendar:
                Rebuild(calendar.BlackoutDates, enabled, calendar.DisplayDateStart, calendar.DisplayDateEnd,
                    () => calendar.SelectedDate, v => calendar.SelectedDate = v);
                break;
            case CalendarDatePicker picker:
                Rebuild(picker.BlackoutDates, enabled, picker.DisplayDateStart, picker.DisplayDateEnd,
                    () => picker.SelectedDate, v => picker.SelectedDate = v);
                break;
        }
    }

    private static void Rebuild(
        CalendarBlackoutDatesCollection? blackout, HashSet<DateTime>? enabled, DateTime? start, DateTime? end,
        Func<DateTime?> getSelected, Action<DateTime?> setSelected)
    {
        if (blackout is null) return;

        // El calendario no permite bloquear la fecha seleccionada: se quita y se repone después.
        var selected = getSelected();
        setSelected(null);
        blackout.Clear();

        if (enabled is not null && start is not null && end is not null)
        {
            DateTime? runStart = null;
            for (var d = start.Value.Date; d <= end.Value.Date; d = d.AddDays(1))
            {
                var blocked = !enabled.Contains(d);
                if (blocked && runStart is null) runStart = d;
                if (!blocked && runStart is not null)
                {
                    blackout.Add(new CalendarDateRange(runStart.Value, d.AddDays(-1)));
                    runStart = null;
                }
            }

            if (runStart is not null) blackout.Add(new CalendarDateRange(runStart.Value, end.Value.Date));
        }

        if (selected is { } s && (enabled is null || enabled.Contains(s.Date)))
        {
            setSelected(s);
        }
    }
}
