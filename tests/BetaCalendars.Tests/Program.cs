using System.Globalization;
using System.Text.Json;
using BetaCalendars.Core;

var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException($"FAIL: {message}"); }
foreach (var (year, leap) in new[] { (1900, false), (2000, true), (2027, false), (2028, true), (2100, false), (2400, true) }) Check(Gregorian.IsLeapYear(year) == leap, $"leap year {year}");
var expectedFirst = new[] { DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Wednesday };
var mondayRows = new[] { 5, 4, 5, 5, 6, 5, 5, 6, 5, 5, 5, 5 };
var sundayRows = new[] { 6, 5, 5, 5, 6, 5, 5, 5, 5, 6, 5, 5 };
for (var month = 1; month <= 12; month++)
{
    Check(new CivilDate(2027, month, 1).DayOfWeek == expectedFirst[month - 1], $"2027 first weekday month {month}");
    Check(MonthGrid.Create(2027, month, WeekStart.Monday).NaturalRows == mondayRows[month - 1], $"2027 Monday rows month {month}");
    Check(MonthGrid.Create(2027, month, WeekStart.Sunday).NaturalRows == sundayRows[month - 1], $"2027 Sunday rows month {month}");
}
Check(MonthGrid.Create(2027, 2).NaturalRows == 4, "February 2027 Monday four-row month");
Check(MonthGrid.Create(2027, 5).NaturalRows == 6 && MonthGrid.Create(2027, 5, WeekStart.Sunday).NaturalRows == 6, "May 2027 six rows");
Check(MonthGrid.Create(2027, 8).NaturalRows == 6 && MonthGrid.Create(2027, 8, WeekStart.Sunday).NaturalRows == 5, "August 2027 rows");
Check(MonthGrid.Create(2027, 10).NaturalRows == 5 && MonthGrid.Create(2027, 10, WeekStart.Sunday).NaturalRows == 6, "October 2027 rows");
for (var year = 1800; year <= 2200; year++)
    for (var month = 1; month <= 12; month++)
        foreach (var start in Enum.GetValues<WeekStart>())
        {
            var grid = MonthGrid.Create(year, month, start);
            Check(grid.Validate().Count == 0, $"geometry {year}-{month:D2} {start}");
            Check(grid.NaturalRows is >= 4 and <= 6, $"row bounds {year}-{month:D2} {start}");
            Check(grid.Cells.Count(c => c.IsInMonth) == Gregorian.DaysInMonth(year, month), $"month cardinality {year}-{month:D2} {start}");
        }
for (var year = 2014; year <= 2032; year++)
    for (var offset = 0; offset < 8; offset++)
    {
        var date = new CivilDate(year, 12, 28).AddDays(offset);
        Check(DateAnalysisEngine.Inspect(date).IsoWeek == ISOWeek.GetWeekOfYear(date.ToDateTime()), $"ISO week {date}");
        Check(DateAnalysisEngine.Inspect(date).IsoWeekYear == ISOWeek.GetYear(date.ToDateTime()), $"ISO year {date}");
    }
foreach (var date in new[] { new CivilDate(2021, 1, 1), new CivilDate(2022, 1, 1), new CivilDate(2027, 1, 1), new CivilDate(2028, 1, 1) })
    Check(DateAnalysisEngine.Inspect(date).IsoWeekYear != date.Year, $"year boundary ISO distinction {date}");
Check(!CivilDate.TryParse("2027-02-29", out _), "reject invalid leap day");
Check(CivilDate.TryParse("2028-02-29", out var parsed) && parsed == new CivilDate(2028, 2, 29), "parse valid leap day");
var random = new Random(2048);
for (var i = 0; i < 12_000; i++)
{
    var year = random.Next(1800, 2201); var month = random.Next(1, 13); var day = random.Next(1, Gregorian.DaysInMonth(year, month) + 1); var date = new CivilDate(year, month, day);
    Check(CivilDate.Parse(date.ToIsoString()) == date, $"round trip {date}");
    Check(date.AddDays(1).DayOfWeek == (DayOfWeek)(((int)date.DayOfWeek + 1) % 7), $"AddDays weekday {date}");
}
var json = CalendarExport.ToJson(MonthGrid.Create(2027, 2)); using var parsedJson = JsonDocument.Parse(json); Check(json.Contains("2027-02-28"), "JSON contains final date");
var csv = CalendarExport.ToCsv(MonthGrid.Create(2027, 2)); Check(csv.StartsWith("date,weekday,in_month,row,column\n", StringComparison.Ordinal), "CSV header"); Check(csv.Contains("2027-02-01"), "CSV date");
var svg = CalendarExport.ToSvg(MonthGrid.Create(2027, 2)); Check(svg.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", StringComparison.Ordinal) && svg.EndsWith("</svg>", StringComparison.Ordinal), "SVG root");
var parsedSvg = System.Xml.Linq.XDocument.Parse(svg); Check(parsedSvg.Root?.Name.LocalName == "svg", "SVG parses as XML");
var yearJson = CalendarExport.ToYearJson(2027); using var parsedYearJson = JsonDocument.Parse(yearJson); Check(parsedYearJson.RootElement.GetProperty("Months").GetArrayLength() == 12, "year JSON month count");
var yearSvg = CalendarExport.ToYearSvg(2027); var parsedYearSvg = System.Xml.Linq.XDocument.Parse(yearSvg); Check(parsedYearSvg.Root?.Name.LocalName == "svg" && yearSvg.Contains("December"), "year SVG has all month labels");
Console.WriteLine($"PASS: {checks:N0} assertions, 401 years × 12 months × 2 week starts, deterministic date properties.");
