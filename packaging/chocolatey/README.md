# Chocolatey package source

This folder is the source of the `betacalendars-studio` package. The install script and nuspec are generated from a published, immutable GitHub release so the MSI SHA256 is a real value, never a placeholder.

After the Windows release gates pass, run `prepare-package.ps1 -Version 1.0.0 -OutputDirectory <folder>` on a machine with Chocolatey installed. The script validates the published release, downloads the MSI, checks it against `SHA256SUMS.txt`, writes the final nuspec and standard Chocolatey install helper call, then runs `choco pack`. It does not push to the Community Repository.

Do not submit until Windows Server 2019 compatibility, malware scanning, local silent install/uninstall, and the remaining release gates have documented passing results.
