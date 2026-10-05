namespace BetaCalendars.Core;

public static class Gregorian
{
    public const int MinYear = 1;
    public const int MaxYear = 9999;
    public static bool IsLeapYear(int year)
    {
        EnsureYear(year);
        return year % 400 == 0 || (year % 4 == 0 && year % 100 != 0);
    }
    public static int DaysInYear(int year) => IsLeapYear(year) ? 366 : 365;
    public static int DaysInMonth(int year, int month)
    {
        EnsureYear(year);
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        return month switch { 2 => IsLeapYear(year) ? 29 : 28, 4 or 6 or 9 or 11 => 30, _ => 31 };
    }
    public static void EnsureValid(int year, int month, int day)
    {
        var days = DaysInMonth(year, month);
        if (day < 1 || day > days) throw new ArgumentOutOfRangeException(nameof(day), $"Day must be 1 through {days} for {year:D4}-{month:D2}.");
    }
    public static void EnsureYear(int year)
    {
        if (year is < MinYear or > MaxYear) throw new ArgumentOutOfRangeException(nameof(year), "Year must be from 1 through 9999.");
    }
    public static int DayOfYear(int year, int month, int day)
    {
        EnsureValid(year, month, day);
        var result = day;
        for (var m = 1; m < month; m++) result += DaysInMonth(year, m);
        return result;
    }
}
