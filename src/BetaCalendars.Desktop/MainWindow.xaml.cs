using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Text.Json;
using BetaCalendars.Core;
using Microsoft.Win32;

namespace BetaCalendars.Desktop;

public partial class MainWindow : Window
{
    private int _year = 2027, _month = 2, _inspectionDay = 1;
    private bool _updating;
    private bool _showWeekNumbers;
    private bool _showAdjacentMonths = true;
    private bool _fixedSixRows;
    private string _paperSize = "A4";
    private string _orientation = "Portrait";
    private static string PreferencesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetaCalendars", "Studio", "settings.json");

    private sealed record Preferences(int WeekStart, bool ShowWeekNumbers, bool ShowAdjacentMonths, bool FixedSixRows, string PaperSize, string Orientation);

    public MainWindow()
    {
        InitializeComponent();
        MonthPicker.ItemsSource = Enumerable.Range(1, 12).Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m)).ToArray();
        LoadPreferences();
        _updating = true;
        MonthPicker.SelectedIndex = _month - 1;
        WeekStartPicker.SelectedIndex = Start == WeekStart.Sunday ? 1 : 0;
        _updating = false;
        Render();
    }

    private WeekStart Start => WeekStartPicker.SelectedIndex == 1 ? WeekStart.Sunday : WeekStart.Monday;
    private void Render()
    {
        if (CalendarGrid is null) return;
        _updating = true; MonthPicker.SelectedIndex = Math.Clamp(_month - 1, 0, 11); YearPicker.Text = _year.ToString(); _updating = false;
        var grid = MonthGrid.Create(_year, _month, Start, _fixedSixRows);
        CalendarGrid.Visibility = SelectedPage == "Year" ? Visibility.Collapsed : Visibility.Visible;
        DynamicPanel.Children.Clear();
        CalendarGrid.Children.Clear();
        CalendarGrid.Columns = _showWeekNumbers ? 8 : 7;
        var names = Start == WeekStart.Monday ? new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" } : new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        if (_showWeekNumbers) CalendarGrid.Children.Add(new TextBlock { Text = "Wk", FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray, Margin = new Thickness(6) });
        foreach (var name in names) CalendarGrid.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(87, 106, 119)), Margin = new Thickness(9, 6, 4, 9) });
        for (var row = 0; row < grid.Cells.Count / 7; row++)
        {
            if (_showWeekNumbers)
            {
                var mondayDate = grid.Cells[row * 7].Date;
                var daysToThursday = ((int)DayOfWeek.Thursday - (int)mondayDate.DayOfWeek + 7) % 7;
                var isoThursday = mondayDate.AddDays(daysToThursday);
                CalendarGrid.Children.Add(new TextBlock { Text = ISOWeek.GetWeekOfYear(isoThursday.ToDateTime()).ToString("D2"), Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(2, 10, 2, 0), FontSize = 11 });
            }
            foreach (var cell in grid.Cells.Skip(row * 7).Take(7))
            {
                var border = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(221, 228, 233)), BorderThickness = new Thickness(.5), MinHeight = 66, Padding = new Thickness(9), Background = cell.IsInMonth ? Brushes.White : new SolidColorBrush(Color.FromRgb(248, 249, 250)) };
                border.Child = new TextBlock { Text = cell.IsInMonth || _showAdjacentMonths ? cell.Date.Day.ToString() : "", Foreground = cell.IsInMonth ? new SolidColorBrush(Color.FromRgb(30, 46, 57)) : new SolidColorBrush(Color.FromRgb(160, 170, 177)), FontSize = 14 };
                CalendarGrid.Children.Add(border);
            }
        }
        Summary.Text = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(_month)} {_year}   ·   {grid.NaturalRows} natural rows   ·   {grid.LeadingCells} leading cells   ·   {(_fixedSixRows ? "six-row layout" : "natural layout")}";
        DetailText.Text = SelectedPage switch
        {
            "Calendar Lab" => LabText(grid),
            "ISO Week Lab" => IsoText(),
            "Date Inspector" => InspectorText(),
            "Year" => YearText(),
            "About" => "Beta Calendars Studio 1.0.0\nOffline-first calendar and civil-date tools. No telemetry, account, or automatic network access.",
            _ => $"{Gregorian.DaysInMonth(_year, _month)} dates · {Start}-first · natural geometry shown. Select Export SVG or Export JSON to save this calendar."
        };
        if (SelectedPage == "Year") RenderYear();
        else if (SelectedPage == "Date Inspector") RenderInspector();
        else if (SelectedPage == "Calendar Lab") RenderPresets();
        else if (SelectedPage == "Resources") RenderResources();
        else if (SelectedPage == "Blank Designer" || SelectedPage == "Print Studio") RenderDesigner(SelectedPage == "Print Studio");
        else if (SelectedPage == "Settings") RenderSettings();
        else DynamicPanel.Children.Clear();
    }
    private string SelectedPage => (Navigation.SelectedItem as ListBoxItem)?.Content?.ToString() ?? "Month";
    private string InspectorText()
    {
        _inspectionDay = Math.Clamp(_inspectionDay, 1, Gregorian.DaysInMonth(_year, _month));
        var info = DateAnalysisEngine.Inspect(new CivilDate(_year, _month, _inspectionDay));
        return $"Date: {info.Date} ({info.Date.DayOfWeek})\nDay of year: {info.Date.DayOfYear} · Quarter: Q{info.Date.Quarter}\nLeap year: {info.IsLeapYear} · Days this month/year: {info.DaysInMonth}/{info.DaysInYear}\nISO week: {info.IsoWeekYear}-W{info.IsoWeek:D2}-{info.IsoWeekday}\nMonday column: {info.MondayFirstColumn + 1} · Sunday column: {info.SundayFirstColumn + 1}\nRemaining in month/year: {info.DaysRemainingInMonth}/{info.DaysRemainingInYear}";
    }
    private string IsoText() { var b = new System.Text.StringBuilder("ISO week boundary explorer · December 28 through January 4\n"); var start = new CivilDate(_year, 12, 28); for (var i = 0; i < 8; i++) { var d = start.AddDays(i); var iso = DateAnalysisEngine.Inspect(d); b.AppendLine($"{d} {d.DayOfWeek,-9} → {iso.IsoWeekYear}-W{iso.IsoWeek:D2}-{iso.IsoWeekday}{(iso.IsoWeekYear != d.Year ? "  ·  week-year boundary" : "")}"); } return b.ToString(); }
    private string LabText(MonthGrid grid) { var problems = grid.Validate(); return $"CALENDAR LAB · {grid.Year}-{grid.Month:D2}\nFirst weekday: {new CivilDate(grid.Year, grid.Month, 1).DayOfWeek}\nDays: {Gregorian.DaysInMonth(grid.Year, grid.Month)} · Leading: {grid.LeadingCells} · Trailing: {grid.Cells.Count - grid.LeadingCells - Gregorian.DaysInMonth(grid.Year, grid.Month)}\nMonday rows: {MonthGrid.Create(grid.Year, grid.Month, WeekStart.Monday).NaturalRows} · Sunday rows: {MonthGrid.Create(grid.Year, grid.Month, WeekStart.Sunday).NaturalRows}\nGrid continuity, day count, uniqueness, and bounds: {(problems.Count == 0 ? "PASS" : "FAIL — " + string.Join("; ", problems))}"; }
    private string YearText() => $"Twelve month views for {_year}. Select a month card below to open its full grid.";
    private void RenderYear()
    {
        var months = new UniformGrid { Columns = 3 };
        for (var month = 1; month <= 12; month++)
        {
            var capturedMonth = month;
            var card = new Button { Margin = new Thickness(5), Padding = new Thickness(10), HorizontalContentAlignment = HorizontalAlignment.Stretch, Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(220, 228, 234)) };
            var content = new StackPanel();
            content.Children.Add(new TextBlock { Text = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)} · {MonthGrid.Create(_year, month, Start).NaturalRows} weeks", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(40, 92, 114)), Margin = new Thickness(0, 0, 0, 6) });
            var smallGrid = new UniformGrid { Columns = 7 };
            var labels = Start == WeekStart.Monday ? new[] { "M", "T", "W", "T", "F", "S", "S" } : new[] { "S", "M", "T", "W", "T", "F", "S" };
            foreach (var label in labels) smallGrid.Children.Add(new TextBlock { Text = label, FontSize = 9, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Gray });
            foreach (var cell in MonthGrid.Create(_year, month, Start).Cells) smallGrid.Children.Add(new TextBlock { Text = cell.IsInMonth ? cell.Date.Day.ToString() : " ", FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(1) });
            content.Children.Add(smallGrid); card.Content = content;
            card.Click += (_, _) => { _month = capturedMonth; Navigation.SelectedIndex = 0; Render(); };
            months.Children.Add(card);
        }
        DynamicPanel.Children.Add(months);
    }
    private void RenderInspector()
    {
        DynamicPanel.Children.Clear();
        DynamicPanel.Children.Add(new TextBlock { Text = "Inspect a civil date", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(40, 92, 114)) });
        var datePicker = new DatePicker
        {
            SelectedDate = new DateTime(_year, _month, Math.Clamp(_inspectionDay, 1, Gregorian.DaysInMonth(_year, _month))),
            Margin = new Thickness(0, 8, 0, 12),
            Width = 180,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        datePicker.SelectedDateChanged += (_, _) =>
        {
            if (datePicker.SelectedDate is not DateTime selected) return;
            _year = selected.Year;
            _month = selected.Month;
            _inspectionDay = selected.Day;
            Render();
        };
        DynamicPanel.Children.Add(datePicker);
    }
    private void RenderPresets()
    {
        DynamicPanel.Children.Clear();
        DynamicPanel.Children.Add(new TextBlock { Text = "Regression presets", FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(40, 92, 114)) });
        var presets = new WrapPanel();
        foreach (var (year, month) in new[] { (1900, 2), (2000, 2), (2027, 2), (2028, 2), (2100, 2), (2400, 2), (2027, 8), (2027, 10), (2027, 12) })
        {
            var button = new Button { Content = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}" };
            button.Click += (_, _) => { _year = year; _month = month; Render(); };
            presets.Children.Add(button);
        }
        DynamicPanel.Children.Add(presets);
    }
    private void RenderDesigner(bool print)
    {
        DynamicPanel.Children.Clear();
        DynamicPanel.Children.Add(new TextBlock { Text = print ? "PRINT STUDIO" : "BLANK CALENDAR DESIGNER", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(40, 92, 114)) });
        var options = new WrapPanel { Margin = new Thickness(0, 8, 0, 8) };
        options.Children.Add(new TextBlock { Text = "Paper", VerticalAlignment = VerticalAlignment.Center });
        var paper = new ComboBox { Width = 110, Margin = new Thickness(8, 0, 14, 0), ItemsSource = new[] { "A4", "US Letter" }, SelectedItem = _paperSize };
        paper.SelectionChanged += (_, _) => { _paperSize = paper.SelectedItem?.ToString() ?? "A4"; SavePreferences(); };
        options.Children.Add(paper);
        options.Children.Add(new TextBlock { Text = "Orientation", VerticalAlignment = VerticalAlignment.Center });
        var orientation = new ComboBox { Width = 110, Margin = new Thickness(8, 0, 14, 0), ItemsSource = new[] { "Portrait", "Landscape" }, SelectedItem = _orientation };
        orientation.SelectionChanged += (_, _) => { _orientation = orientation.SelectedItem?.ToString() ?? "Portrait"; SavePreferences(); };
        options.Children.Add(orientation);
        var fixedRows = new CheckBox { Content = "Fixed six rows", IsChecked = _fixedSixRows, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5) };
        fixedRows.Checked += (_, _) => { _fixedSixRows = true; SavePreferences(); Render(); };
        fixedRows.Unchecked += (_, _) => { _fixedSixRows = false; SavePreferences(); Render(); };
        options.Children.Add(fixedRows);
        DynamicPanel.Children.Add(options);
        DynamicPanel.Children.Add(new TextBlock { Text = $"Live layout preview: {CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(_month)} {_year} · {_paperSize} · {_orientation} · {Start}-first · {(_fixedSixRows ? 6 : MonthGrid.Create(_year, _month, Start).NaturalRows)} rows. Export SVG to save the blank writing cells; Print opens the Windows print dialog.", Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap });
    }
    private void RenderSettings()
    {
        DynamicPanel.Children.Clear();
        DynamicPanel.Children.Add(new TextBlock { Text = "Preferences are saved locally under this Windows user's Local AppData. Date calculations remain offline; no diagnostics are sent." });
        AddPreference("Show ISO week numbers", _showWeekNumbers, value => _showWeekNumbers = value);
        AddPreference("Show adjacent-month dates", _showAdjacentMonths, value => _showAdjacentMonths = value);
        AddPreference("Use fixed six-row month grids", _fixedSixRows, value => _fixedSixRows = value);
    }
    private void AddPreference(string label, bool value, Action<bool> setter)
    {
        var box = new CheckBox { Content = label, IsChecked = value, Margin = new Thickness(3, 8, 3, 8) };
        box.Checked += (_, _) => { setter(true); SavePreferences(); Render(); };
        box.Unchecked += (_, _) => { setter(false); SavePreferences(); Render(); };
        DynamicPanel.Children.Add(box);
    }
    private void LoadPreferences()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return;
            var settings = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(PreferencesPath));
            if (settings is null) return;
            _showWeekNumbers = settings.ShowWeekNumbers;
            _showAdjacentMonths = settings.ShowAdjacentMonths;
            _fixedSixRows = settings.FixedSixRows;
            _paperSize = settings.PaperSize is "A4" or "US Letter" ? settings.PaperSize : "A4";
            _orientation = settings.Orientation is "Portrait" or "Landscape" ? settings.Orientation : "Portrait";
            _updating = true;
            WeekStartPicker.SelectedIndex = settings.WeekStart == 1 ? 1 : 0;
            _updating = false;
        }
        catch (IOException) { }
        catch (JsonException) { }
    }
    private void SavePreferences()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            var settings = new Preferences(Start == WeekStart.Sunday ? 1 : 0, _showWeekNumbers, _showAdjacentMonths, _fixedSixRows, _paperSize, _orientation);
            File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private void RenderResources() { DynamicPanel.Children.Clear(); var links = new (string, string)[] { ("Beta Calendars", "https://www.betacalendars.com/"), ("Monthly Calendar", "https://www.betacalendars.com/monthly-calendar"), ("Blank Calendar", "https://www.betacalendars.com/blank-calendar"), ("Monthly Planner", "https://www.betacalendars.com/monthly-planner") }; foreach (var (title, url) in links) { var button = new Button { Content = title, HorizontalAlignment = HorizontalAlignment.Left, Tag = url }; button.Click += (_, _) => Process.Start(new ProcessStartInfo((string)button.Tag) { UseShellExecute = true }); DynamicPanel.Children.Add(button); } }
    private void Navigation_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) { PageTitle.Text = SelectedPage; Subtitle.Text = SelectedPage == "Month" ? "Explore Gregorian month geometry." : $"{SelectedPage} · deterministic offline tools"; Render(); } }
    private void SelectionChanged(object sender, RoutedEventArgs e) { if (_updating || !IsLoaded) return; if (MonthPicker.SelectedIndex >= 0) _month = MonthPicker.SelectedIndex + 1; if (int.TryParse(YearPicker.Text, out var y) && y is >= 1 and <= 9999) _year = y; SavePreferences(); Render(); }
    private void Previous_Click(object sender, RoutedEventArgs e) { if (_month == 1) { if (_year == 1) return; _month = 12; _year--; } else _month--; Render(); }
    private void Next_Click(object sender, RoutedEventArgs e) { if (_month == 12) { if (_year == 9999) return; _month = 1; _year++; } else _month++; Render(); }
    private void Today_Click(object sender, RoutedEventArgs e) { var now = DateTime.Today; _year = now.Year; _month = now.Month; Render(); }
    private void ExportSvg_Click(object sender, RoutedEventArgs e) => SaveFile("SVG calendar|*.svg", SelectedPage == "Year" ? "calendar-year.svg" : "calendar.svg", SelectedPage == "Year" ? CalendarExport.ToYearSvg(_year, Start) : CalendarExport.ToSvg(MonthGrid.Create(_year, _month, Start, _fixedSixRows), SelectedPage == "Blank Designer"));
    private void ExportJson_Click(object sender, RoutedEventArgs e) => SaveFile("JSON data|*.json", SelectedPage == "Year" ? "calendar-year.json" : "calendar.json", SelectedPage == "Year" ? CalendarExport.ToYearJson(_year, Start) : CalendarExport.ToJson(MonthGrid.Create(_year, _month, Start, _fixedSixRows)));
    private void ExportHtml_Click(object sender, RoutedEventArgs e) => SaveFile("HTML page|*.html", SelectedPage == "Year" ? "calendar-year.html" : "calendar.html", SelectedPage == "Year" ? CalendarExport.ToYearHtml(_year, Start) : CalendarExport.ToHtml(MonthGrid.Create(_year, _month, Start, _fixedSixRows)));
    private void ExportCsv_Click(object sender, RoutedEventArgs e) => SaveFile("CSV data|*.csv", "calendar.csv", CalendarExport.ToCsv(MonthGrid.Create(_year, _month, Start, _fixedSixRows)));
    private void SaveFile(string filter, string name, string value) { var dialog = new SaveFileDialog { Filter = filter, FileName = name }; if (dialog.ShowDialog(this) == true) File.WriteAllText(dialog.FileName, value); }
    private void Print_Click(object sender, RoutedEventArgs e) { var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) { var visual = new TextBlock { Text = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(_month)} {_year}\n\n" + string.Join("\n", Enumerable.Range(0, MonthGrid.Create(_year, _month, Start).NaturalRows).Select(r => string.Join("   ", MonthGrid.Create(_year, _month, Start).Cells.Skip(r * 7).Take(7).Select(c => c.IsInMonth ? c.Date.Day.ToString("D2") : "  ")))), FontSize = 22, Margin = new Thickness(32) }; visual.Measure(new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight)); visual.Arrange(new Rect(new Point(0, 0), visual.DesiredSize)); dialog.PrintVisual(visual, "Beta Calendars Studio"); } }
}
