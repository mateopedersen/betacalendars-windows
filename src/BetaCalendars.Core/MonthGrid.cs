namespace BetaCalendars.Core;

public enum WeekStart { Monday, Sunday }
public sealed record CalendarCell(CivilDate Date, bool IsInMonth, int Row, int Column);
public sealed record MonthGrid(int Year, int Month, WeekStart WeekStart, int LeadingCells, int NaturalRows, IReadOnlyList<CalendarCell> Cells)
{
    public static MonthGrid Create(int year, int month, WeekStart weekStart = WeekStart.Monday, bool fixedSixRows = false)
    {
        Gregorian.EnsureYear(year);
        var days = Gregorian.DaysInMonth(year, month);
        var first = new CivilDate(year, month, 1);
        var weekday = (int)first.DayOfWeek;
        var start = weekStart == WeekStart.Monday ? 1 : 0;
        var leading = (weekday - start + 7) % 7;
        var naturalRows = (int)Math.Ceiling((leading + days) / 7d);
        var rows = fixedSixRows ? 6 : naturalRows;
        var firstCell = first.AddDays(-leading);
        var cells = Enumerable.Range(0, rows * 7).Select(i =>
        {
            var date = firstCell.AddDays(i);
            return new CalendarCell(date, date.Year == year && date.Month == month, i / 7, i % 7);
        }).ToArray();
        return new MonthGrid(year, month, weekStart, leading, naturalRows, cells);
    }

    public IReadOnlyList<string> Validate()
    {
        var failures = new List<string>();
        var monthDates = Cells.Where(c => c.IsInMonth).Select(c => c.Date).ToArray();
        var expected = Gregorian.DaysInMonth(Year, Month);
        if (monthDates.Length != expected) failures.Add("Incorrect in-month day count.");
        if (monthDates.Distinct().Count() != expected) failures.Add("Duplicate in-month date.");
        if (monthDates.Length > 0 && (monthDates[0].Day != 1 || monthDates[^1].Day != expected)) failures.Add("Month boundaries are incomplete.");
        for (var i = 1; i < Cells.Count; i++)
            if (Cells[i - 1].Date.AddDays(1) != Cells[i].Date) { failures.Add("Date sequence is not continuous."); break; }
        if (Cells.Count / 7 is < 4 or > 6) failures.Add("Grid row count must be between four and six.");
        return failures;
    }
}
