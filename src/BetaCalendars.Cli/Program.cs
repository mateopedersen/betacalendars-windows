using System.Globalization;
using System.Text.Json;
using BetaCalendars.Core;

return Run(args);

static int Run(string[] args)
{
    try
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h") { Help(); return 0; }
        if (args[0] is "version" or "--version") { Console.WriteLine("Beta Calendars Studio CLI 1.0.0"); return 0; }
        var json = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
        var command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "month":
            case "blank":
            case "svg":
            case "html":
            case "csv":
                {
                    if (args.Length < 3 || !int.TryParse(args[1], out var year) || !int.TryParse(args[2], out var month)) throw new ArgumentException("Use: betacal month|blank|svg YEAR MONTH [--week-start monday|sunday] [--output FILE] [--json]");
                    var start = Option(args, "--week-start")?.ToLowerInvariant() switch { null or "monday" => WeekStart.Monday, "sunday" => WeekStart.Sunday, _ => throw new ArgumentException("Week start must be monday or sunday.") };
                    var grid = MonthGrid.Create(year, month, start);
                    if (command is "svg" or "html" or "csv")
                    {
                        var extension = command;
                        var content = command switch { "svg" => CalendarExport.ToSvg(grid), "html" => CalendarExport.ToHtml(grid), _ => CalendarExport.ToCsv(grid) };
                        WriteOutput(Option(args, "--output") ?? $"calendar-{year}-{month:D2}.{extension}", content);
                        return 0;
                    }
                    if (json) Console.WriteLine(CalendarExport.ToJson(grid));
                    else PrintGrid(grid, command == "blank");
                    return 0;
                }
            case "year":
                {
                    if (args.Length < 2 || !int.TryParse(args[1], out var year)) throw new ArgumentException("Use: betacal year YEAR [--week-start monday|sunday] [--json]");
                    Gregorian.EnsureYear(year);
                    var start = Option(args, "--week-start")?.Equals("sunday", StringComparison.OrdinalIgnoreCase) == true ? WeekStart.Sunday : WeekStart.Monday;
                    if (json) Console.WriteLine(CalendarExport.ToYearJson(year, start));
                    else foreach (var month in Enumerable.Range(1, 12)) { Console.WriteLine(); PrintGrid(MonthGrid.Create(year, month, start), false); }
                    return 0;
                }
            case "inspect":
            case "iso":
                {
                    if (args.Length < 2) throw new ArgumentException($"Use: betacal {command} YYYY-MM-DD [--json]");
                    var info = DateAnalysisEngine.Inspect(CivilDate.Parse(args[1]));
                    if (command == "iso" && !json) Console.WriteLine($"{info.Date}: ISO {info.IsoWeekYear}-W{info.IsoWeek:D2}-{info.IsoWeekday}");
                    else if (json) Console.WriteLine(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
                    else { Console.WriteLine($"Date: {info.Date}\nWeekday: {info.Date.DayOfWeek}\nDay of year: {info.Date.DayOfYear}\nQuarter: Q{info.Date.Quarter}\nLeap year: {info.IsLeapYear}\nISO week: {info.IsoWeekYear}-W{info.IsoWeek:D2}-{info.IsoWeekday}\nDays in month/year: {info.DaysInMonth}/{info.DaysInYear}\nRemaining in month/year: {info.DaysRemainingInMonth}/{info.DaysRemainingInYear}\nNatural month rows: {info.MonthNaturalRows}"); }
                    return 0;
                }
            case "validate":
                {
                    if (args.Length < 2 || !int.TryParse(args[1], out var year)) throw new ArgumentException("Use: betacal validate YEAR [--json]");
                    Gregorian.EnsureYear(year);
                    var failures = new List<string>();
                    foreach (var month in Enumerable.Range(1, 12)) foreach (var start in Enum.GetValues<WeekStart>()) failures.AddRange(MonthGrid.Create(year, month, start).Validate().Select(f => $"{year:D4}-{month:D2} ({start}): {f}"));
                    if (json) Console.WriteLine(JsonSerializer.Serialize(new { Year = year, Passed = failures.Count == 0, Failures = failures }, new JsonSerializerOptions { WriteIndented = true }));
                    else Console.WriteLine(failures.Count == 0 ? $"PASS: all 24 month grids in {year} validated." : "FAIL\n" + string.Join("\n", failures));
                    return failures.Count == 0 ? 0 : 2;
                }
            default: throw new ArgumentException($"Unknown command: {args[0]}");
        }
    }
    catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 2;
    }
}

static string? Option(string[] args, string name) { var i = Array.FindIndex(args, x => x.Equals(name, StringComparison.OrdinalIgnoreCase)); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
static void WriteOutput(string path, string content) { File.WriteAllText(path, content); Console.WriteLine($"Wrote {Path.GetFullPath(path)}"); }
static void PrintGrid(MonthGrid grid, bool blank)
{
    Console.WriteLine($"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(grid.Month)} {grid.Year}");
    var labels = grid.WeekStart == WeekStart.Monday ? new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" } : new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    Console.WriteLine(string.Join(" ", labels));
    for (var row = 0; row < grid.Cells.Count / 7; row++)
    {
        Console.WriteLine(string.Join(" ", grid.Cells.Skip(row * 7).Take(7).Select(c => c.IsInMonth ? c.Date.Day.ToString().PadLeft(2) : "  ")));
        if (blank) Console.WriteLine("                         ");
    }
    Console.WriteLine($"Days: {Gregorian.DaysInMonth(grid.Year, grid.Month)}\nNatural rows: {grid.NaturalRows}\nWeek start: {grid.WeekStart}");
}
static void Help() => Console.WriteLine("Beta Calendars Studio CLI 1.0.0\nCommands: month YEAR MONTH, year YEAR, inspect DATE, iso DATE, validate YEAR, blank YEAR MONTH, svg|html|csv YEAR MONTH --output FILE, version\nOptions: --week-start monday|sunday, --json");
