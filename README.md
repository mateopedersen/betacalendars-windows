# Beta Calendars Studio

Beta Calendars Studio is an offline-first Windows calendar engineering and printable-calendar utility. It combines a native WPF desktop application with a command line interface and a deterministic Gregorian date engine.

## What it does

- Explore month and year grids with Monday or Sunday week starts.
- Inspect civil dates, ISO weeks, leap years, and year-boundary behavior.
- Validate calendar geometry and Gregorian regression cases.
- Design blank calendars and export SVG, HTML, JSON, and CSV.
- Print through the normal Windows print dialog.
- Use the CLI without a network connection.

The application has no telemetry, account system, advertising, or automatic network access. Optional resource links open only after a user selects them.

## Build

Install the .NET 10 SDK on Windows, then run `dotnet build BetaCalendars.sln -c Release` and `dotnet run --project tests/BetaCalendars.Tests -c Release`.

The WPF application targets Windows. The core, CLI, and tests are cross-platform .NET projects.

The initial MSI authoring uses WiX Toolset 5.0.2. The current WiX release has separate maintenance-fee/EULA terms, so the project does not accept those terms automatically in CI.

## CLI

```text
betacal month 2027 2 --week-start monday
betacal year 2027
betacal inspect 2027-01-01
betacal iso 2027-01-01
betacal validate 2027
betacal blank 2027 2
betacal svg 2027 2 --output february.svg
betacal version
```

## Windows installation

Download the MSI from the [GitHub releases](https://github.com/mateopedersen/betacalendars-windows/releases) page and run it normally. For an unattended install, use `msiexec /i BetaCalendarsStudio-1.0.0-win-x64.msi /qn /norestart`. Chocolatey installation will be listed here after the Community Repository approves the package.

## Privacy

Date calculations run locally. Preferences are stored under the user's local application data directory. Exports are written only to the destination selected by the user.

## License

MIT. See [LICENSE](LICENSE).
