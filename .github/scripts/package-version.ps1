# DW resolves the add-in identity from <package-id>.<version>, then searches for
# DLLs starting with the resolved id. A fourth numeric part can be swallowed by
# the greedy name group, so a regex match alone is NOT proof of compatibility.
$script:DwAddinVersionPattern = '^(?<name>[A-Za-z0-9_.]+)\.(?<version>(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<prerelease>(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*))?(?:\+(?<buildmetadata>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?)$'

function Assert-DwAddinIdentity {
    param([Parameter(Mandatory)] [string] $PackageId, [Parameter(Mandatory)] [string] $Version)
    $parsed = [regex]::Match("$PackageId.$Version", $script:DwAddinVersionPattern)
    if (-not $parsed.Success -or $parsed.Groups['name'].Value -cne $PackageId -or
        $parsed.Groups['version'].Value -cne $Version) {
        throw "DW cannot resolve add-in identity '$PackageId.$Version' correctly. Use a three-part SemVer version; four-part versions can change the resolved package name."
    }
}

function Get-LinePackageVersion {
    param(
        [Parameter(Mandatory)] [string] $Release,
        [Parameter(Mandatory)] [ValidateSet('Base', 'Discount', 'Groups')] [string] $Line,
        [string] $PackageId = 'Truvio.Commerce.PowerTools'
    )
    Assert-DwAddinIdentity -PackageId $PackageId -Version $Release
    $parts = [regex]::Match($Release, '^(?<prefix>[0-9]+\.[0-9]+\.)(?<patch>[0-9]+)(?<suffix>.*)$')
    $patch = [int]::Parse($parts.Groups['patch'].Value)
    if ($patch % 10 -ne 0) { throw "Base release patch must be a multiple of 10 (10n): $Release" }
    $offset = switch ($Line) { Base { 0 } Discount { 1 } Groups { 2 } }
    $version = "$($parts.Groups['prefix'].Value)$($patch + $offset)$($parts.Groups['suffix'].Value)"
    Assert-DwAddinIdentity -PackageId $PackageId -Version $version
    return $version
}
