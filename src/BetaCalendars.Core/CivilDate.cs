namespace BetaCalendars.Core;

public readonly record struct CivilDate : IComparable<CivilDate>
{
    public int Year { get; }
    public int Month { get; }
    public int Day { get; }

    public CivilDate(int year, int month, int day)
    {
        Gregorian.EnsureValid(year, month, day);
        (Year, Month, Day) = (year, month, day);
    }

    public static CivilDate Parse(string value)
    {
        if (!TryParse(value, out var date)) throw new FormatException("Expected an ISO civil date in YYYY-MM-DD form.");
        return date;
    }

    public static bool TryParse(string? value, out CivilDate date)
    {
        date = default;
        if (value is null || value.Length != 10 || value[4] != '-' || value[7] != '-') return false;
        if (!int.TryParse(value.AsSpan(0, 4), out var year) || !int.TryParse(value.AsSpan(5, 2), out var month) || !int.TryParse(value.AsSpan(8, 2), out var day)) return false;
        try { date = new CivilDate(year, month, day); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    public DayOfWeek DayOfWeek => new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Unspecified).DayOfWeek;
    public int DayOfYear => Gregorian.DayOfYear(Year, Month, Day);
    public int Quarter => ((Month - 1) / 3) + 1;
    public CivilDate AddDays(int days) => FromDateTime(new DateTime(Year, Month, Day).AddDays(days));
    public CivilDate AddMonths(int months) => FromDateTime(new DateTime(Year, Month, Day).AddMonths(months));
    public CivilDate AddYears(int years) => FromDateTime(new DateTime(Year, Month, Day).AddYears(years));
    public int DaysUntil(CivilDate other) => (int)(other.ToDateTime() - ToDateTime()).TotalDays;
    public string ToIsoString() => $"{Year:D4}-{Month:D2}-{Day:D2}";
    public DateTime ToDateTime() => new(Year, Month, Day, 0, 0, 0, DateTimeKind.Unspecified);
    public int CompareTo(CivilDate other) => ToDateTime().CompareTo(other.ToDateTime());
    public override string ToString() => ToIsoString();
    private static CivilDate FromDateTime(DateTime value) => new(value.Year, value.Month, value.Day);
}
