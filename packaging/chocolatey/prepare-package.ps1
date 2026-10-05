[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$owner = 'mateopedersen'
$repository = 'betacalendars-windows'
$assetName = "BetaCalendarsStudio-$Version-win-x64.msi"
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repository/releases/tags/v$Version" -Headers @{ 'User-Agent' = 'BetaCalendars-Studio-Package-Builder' }
$asset = @($release.assets | Where-Object { $_.name -eq $assetName })
if ($asset.Count -ne 1) { throw "Expected one release asset named $assetName." }
$checksumsUrl = "https://github.com/$owner/$repository/releases/download/v$Version/SHA256SUMS.txt"
$checksumsPath = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString() + '-SHA256SUMS.txt')
$msiPath = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString() + '-' + $assetName)
try {
    Invoke-WebRequest -Uri $checksumsUrl -OutFile $checksumsPath
    Invoke-WebRequest -Uri $asset[0].browser_download_url -OutFile $msiPath
    $line = Get-Content $checksumsPath | Where-Object { $_ -match "\s+$([regex]::Escape($assetName))$" }
    if (@($line).Count -ne 1) { throw "Checksum entry for $assetName is missing or duplicated." }
    $expected = (($line -split '\s+')[0]).ToLowerInvariant()
    if ($expected -notmatch '^[a-f0-9]{64}$') { throw 'Release checksum is malformed.' }
    $actual = (Get-FileHash $msiPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $expected) { throw 'Downloaded MSI does not match the release checksum.' }
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $tools = Join-Path $OutputDirectory 'tools'
    New-Item -ItemType Directory -Force -Path $tools | Out-Null
    $packageUrl = "https://github.com/$owner/$repository/releases/download/v$Version/$assetName"
    $installScript = @"
`$ErrorActionPreference = 'Stop'
`$packageName = 'betacalendars-studio'
`$toolsDir = Split-Path -Parent `$MyInvocation.MyCommand.Definition
`$url64 = '$packageUrl'
`$checksum64 = '$expected'
Install-ChocolateyPackage -PackageName `$packageName -FileType 'msi' -SilentArgs '/qn /norestart' -Url64bit `$url64 -Checksum64 `$checksum64 -ChecksumType64 'sha256' -ValidExitCodes @(0, 3010)
"@
    Set-Content -Path (Join-Path $tools 'chocolateyInstall.ps1') -Value $installScript -Encoding UTF8
    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>betacalendars-studio</id>
    <version>$Version</version>
    <title>Beta Calendars Studio</title>
    <authors>Beta Calendars</authors>
    <owners>mateopedersen</owners>
    <iconUrl>https://cdn.jsdelivr.net/gh/$owner/$repository@v$Version/assets/icon.svg</iconUrl>
    <projectUrl>https://www.betacalendars.com/</projectUrl>
    <projectSourceUrl>https://github.com/$owner/$repository</projectSourceUrl>
    <packageSourceUrl>https://github.com/$owner/$repository/tree/main/packaging/chocolatey</packageSourceUrl>
    <bugTrackerUrl>https://github.com/$owner/$repository/issues</bugTrackerUrl>
    <docsUrl>https://github.com/$owner/$repository#readme</docsUrl>
    <licenseUrl>https://github.com/$owner/$repository/blob/main/LICENSE</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Beta Calendars Studio is an offline-first Windows calendar engineering and printable-calendar utility. It includes a native desktop calendar, civil-date and ISO-week inspection, month and year geometry, blank calendar design, print layout, a command-line interface, and SVG, HTML, JSON, and CSV export. Gregorian calculations run locally without an account, telemetry, advertising, or calendar data uploads.

Optional Resource Library links open only after the user selects them: [Monthly Calendar](https://www.betacalendars.com/monthly-calendar), [Blank Calendar](https://www.betacalendars.com/blank-calendar), and [Monthly Planner](https://www.betacalendars.com/monthly-planner).</description>
    <summary>Offline-first Windows calendar and civil-date utility.</summary>
    <releaseNotes>Initial release: calendar studio, civil-date engine, CLI, local exports, and silent MSI installer.</releaseNotes>
    <copyright>Copyright 2026 Beta Calendars</copyright>
    <tags>calendar gregorian date iso-week planner print offline cli</tags>
  </metadata>
</package>
"@
    Set-Content -Path (Join-Path $OutputDirectory 'betacalendars-studio.nuspec') -Value $nuspec -Encoding UTF8
    Push-Location $OutputDirectory
    try { choco pack betacalendars-studio.nuspec --outputdirectory $OutputDirectory } finally { Pop-Location }
    Write-Host "Package created and checksum verified: $expected"
}
finally {
    Remove-Item $checksumsPath, $msiPath -Force -ErrorAction SilentlyContinue
}
