using System.Net;
using System.Text;
using System.Text.Json;

namespace BetaCalendars.Core;

public static class CalendarExport
{
    public static string ToYearJson(int year, WeekStart weekStart = WeekStart.Monday) => JsonSerializer.Serialize(new { Year = year, WeekStart = weekStart.ToString(), Months = Enumerable.Range(1, 12).Select(month => new { Month = month, Grid = MonthGrid.Create(year, month, weekStart) }) }, new JsonSerializerOptions { WriteIndented = true });

    public static string ToYearSvg(int year, WeekStart weekStart = WeekStart.Monday)
    {
        Gregorian.EnsureYear(year);
        const int width = 1200, cardWidth = 390, cardHeight = 210, originX = 15, originY = 58;
        var height = 4 * cardHeight + 90;
        var b = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><rect width=\"100%\" height=\"100%\" fill=\"#fff\"/><style>text{{font-family:Segoe UI,Arial,sans-serif;fill:#182330}}.muted{{fill:#8793a0}}</style><text x=\"24\" y=\"40\" font-size=\"28\" font-weight=\"600\">{year} Calendar</text>");
        var labels = weekStart == WeekStart.Monday ? new[] { "M", "T", "W", "T", "F", "S", "S" } : new[] { "S", "M", "T", "W", "T", "F", "S" };
        for (var month = 1; month <= 12; month++)
        {
            var x0 = originX + ((month - 1) % 3) * cardWidth;
            var y0 = originY + ((month - 1) / 3) * cardHeight;
            var grid = MonthGrid.Create(year, month, weekStart, fixedSixRows: true);
            b.Append($"<text x=\"{x0}\" y=\"{y0}\" font-size=\"18\" font-weight=\"600\">{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)}</text>");
            for (var c = 0; c < 7; c++) b.Append($"<text x=\"{x0 + c * 48}\" y=\"{y0 + 25}\" font-size=\"10\" font-weight=\"600\">{labels[c]}</text>");
            foreach (var cell in grid.Cells)
            {
                var x = x0 + cell.Column * 48;
                var y = y0 + 31 + cell.Row * 27;
                b.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"47\" height=\"26\" fill=\"none\" stroke=\"#d7dee6\"/>");
                if (cell.IsInMonth) b.Append($"<text x=\"{x + 7}\" y=\"{y + 17}\" font-size=\"11\">{cell.Date.Day}</text>");
            }
        }
        return b.Append("</svg>").ToString();
    }

    public static string ToYearHtml(int year, WeekStart weekStart = WeekStart.Monday) => $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Calendar {year}</title><style>body{{font:16px Segoe UI,Arial;margin:2rem}}svg{{max-width:100%;height:auto}}@media print{{body{{margin:0}}}}</style>{ToYearSvg(year, weekStart)}</html>";

    public static string ToJson(MonthGrid grid) => JsonSerializer.Serialize(new { grid.Year, grid.Month, WeekStart = grid.WeekStart.ToString(), grid.LeadingCells, grid.NaturalRows, Cells = grid.Cells.Select(c => new { Date = c.Date.ToIsoString(), c.IsInMonth, c.Row, c.Column }) }, new JsonSerializerOptions { WriteIndented = true });
    public static string ToCsv(MonthGrid grid)
    {
        var b = new StringBuilder("date,weekday,in_month,row,column\n");
        foreach (var c in grid.Cells) b.Append(c.Date.ToIsoString()).Append(',').Append(c.Date.DayOfWeek).Append(',').Append(c.IsInMonth).Append(',').Append(c.Row + 1).Append(',').Append(c.Column + 1).Append('\n');
        return b.ToString();
    }
    public static string ToSvg(MonthGrid grid, bool blank = false)
    {
        const int width = 980, titleHeight = 90, cellW = 140, cellH = 108;
        var height = titleHeight + 34 + grid.Cells.Count / 7 * cellH;
        var b = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/><style>text{{font-family:Segoe UI,Arial,sans-serif;fill:#182330}}.muted{{fill:#8793a0}}.grid{{stroke:#d7dee6;stroke-width:1}}</style>");
        b.Append($"<text x=\"24\" y=\"54\" font-size=\"32\" font-weight=\"600\">{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(grid.Month)} {grid.Year}</text>");
        var labels = grid.WeekStart == WeekStart.Monday ? new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" } : new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        for (var c = 0; c < 7; c++) b.Append($"<text x=\"{c * cellW + 14}\" y=\"{titleHeight + 23}\" font-size=\"15\" font-weight=\"600\">{labels[c]}</text>");
        foreach (var c in grid.Cells)
        {
            var x = c.Column * cellW; var y = titleHeight + 34 + c.Row * cellH;
            b.Append($"<rect class=\"grid\" x=\"{x}\" y=\"{y}\" width=\"{cellW}\" height=\"{cellH}\" fill=\"none\"/>");
            var muted = c.IsInMonth ? "" : " class=\"muted\"";
            b.Append($"<text{muted} x=\"{x + 12}\" y=\"{y + 25}\" font-size=\"15\">{c.Date.Day}</text>");
            if (blank) b.Append($"<line class=\"grid\" x1=\"{x + 10}\" y1=\"{y + cellH - 18}\" x2=\"{x + cellW - 10}\" y2=\"{y + cellH - 18}\"/>");
        }
        return b.Append("</svg>").ToString();
    }
    public static string ToHtml(MonthGrid grid)
    {
        var svg = WebUtility.HtmlEncode(ToSvg(grid));
        return $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Calendar {grid.Year}-{grid.Month:D2}</title><style>body{{font:16px Segoe UI,Arial;margin:2rem}}svg{{max-width:100%;height:auto}}@media print{{body{{margin:0}}}}</style><div>{WebUtility.HtmlDecode(svg)}</div></html>";
    }
}
