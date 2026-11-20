$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/package-version.ps1"
$script:checks = 0

function Assert-Equal($Expected, $Actual) {
    if ($Expected -cne $Actual) { throw "Expected '$Expected', got '$Actual'" }
    $script:checks++
}

function Assert-Rejected([scriptblock] $Action, [string] $Message) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        $script:checks++
        return
    }
    throw "Expected rejection containing '$Message'"
}

foreach ($case in @(
    @('0.13.0-beta', 'Base', '0.13.0-beta'),
    @('0.13.0-beta', 'Discount', '0.13.1-beta'),
    @('0.13.0-beta', 'Groups', '0.13.2-beta'),
    @('0.13.10', 'Base', '0.13.10'),
    @('0.13.10', 'Discount', '0.13.11'),
    @('0.13.10', 'Groups', '0.13.12'),
    @('0.13.0', 'Groups', '0.13.2'),
    @('1.2.20-beta.3+build.7', 'Discount', '1.2.21-beta.3+build.7')
)) {
    Assert-Equal $case[2] (Get-LinePackageVersion -Release $case[0] -Line $case[1])
}
Assert-Equal $true ([Version]'0.13.0' -lt [Version]'0.13.1')
Assert-Equal $true ([Version]'0.13.1' -lt [Version]'0.13.2')
Assert-Equal $true ([Version]'0.13.2' -lt [Version]'0.13.10')

$id = 'Truvio.Commerce.PowerTools'
foreach ($bad in @('0.13.0.25-beta', '0.13.0.30-beta', '0.13.0.30', '0.13', '00.13.0', '0.13.0-beta.01', '0.13.0-')) {
    Assert-Rejected { Assert-DwAddinIdentity -PackageId $id -Version $bad } 'DW cannot resolve add-in identity'
}
Assert-Rejected { Get-LinePackageVersion -Release '0.13.1-beta' -Line Base } 'multiple of 10'
# Regression: the folder regex alone accepts the broken package, with a different id.
$misparsed = [regex]::Match("$id.0.13.0.30-beta", $script:DwAddinVersionPattern)
Assert-Equal $true $misparsed.Success
Assert-Equal "$id.0" $misparsed.Groups['name'].Value
Assert-Equal '13.0.30-beta' $misparsed.Groups['version'].Value

# Exercise the real packed-manifest guard, not just the version-string helper.
# No runnable DLL is needed: version rejection must happen before assembly inspection.
$work = Join-Path ([IO.Path]::GetTempPath()) ("addin-version-test-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    foreach ($version in @('0.13.0.30-beta', '0.13.1-beta')) {
        $package = Join-Path $work "$id.$version.nupkg"
        $archive = [IO.Compression.ZipFile]::Open($package, [IO.Compression.ZipArchiveMode]::Create)
        try {
            $entry = $archive.CreateEntry("$id.nuspec")
            $writer = [IO.StreamWriter]::new($entry.Open())
            try { $writer.Write("<package><metadata><id>$id</id><version>$version</version></metadata></package>") }
            finally { $writer.Dispose() }
            $null = $archive.CreateEntry('lib/net8.0/placeholder.dll')
        }
        finally { $archive.Dispose() }
        $message = if ($version -eq '0.13.0.30-beta') { 'DW cannot resolve add-in identity' } else { 'differs from requested version' }
        Assert-Rejected { & "$PSScriptRoot/verify-floor-binding.ps1" -Package $package -Floor '10.8.4' -ExpectedVersion '0.13.0-beta' } $message
    }
}
finally {
    $resolved = [IO.Path]::GetFullPath($work)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'addin-version-test-*') { throw "Unsafe cleanup target: $resolved" }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
Write-Host "PASS: $script:checks package-version checks (including packed four-part rejection)"
