namespace BetaCalendars.Core;

public sealed record DateAnalysis(CivilDate Date, int IsoWeekYear, int IsoWeek, int IsoWeekday, int DaysInMonth, int DaysInYear, bool IsLeapYear, int DaysRemainingInMonth, int DaysRemainingInYear, int MondayFirstColumn, int SundayFirstColumn, int MonthNaturalRows);

public static class DateAnalysisEngine
{
    public static DateAnalysis Inspect(CivilDate date)
    {
        var iso = ISOWeek.GetWeekOfYear(date.ToDateTime());
        var isoYear = ISOWeek.GetYear(date.ToDateTime());
        var isoDay = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
        var daysMonth = Gregorian.DaysInMonth(date.Year, date.Month);
        var grid = MonthGrid.Create(date.Year, date.Month);
        return new(date, isoYear, iso, isoDay, daysMonth, Gregorian.DaysInYear(date.Year), Gregorian.IsLeapYear(date.Year), daysMonth - date.Day, Gregorian.DaysInYear(date.Year) - date.DayOfYear, ((int)date.DayOfWeek + 6) % 7, (int)date.DayOfWeek, grid.NaturalRows);
    }
}
