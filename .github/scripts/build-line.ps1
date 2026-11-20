param(
    [Parameter(Mandatory)] [ValidateSet('Base', 'Discount', 'Groups')] [string] $Line,
    [string] $Output = 'artifacts'
)
$ErrorActionPreference = 'Stop'
$floor = switch ($Line) { Base { '10.8.4' } Discount { '10.25.5' } Groups { '10.30.6' } }
$revision = switch ($Line) { Base { '' } Discount { '.25' } Groups { '.30' } }
$project = 'src/Truvio.Commerce.PowerTools/Truvio.Commerce.PowerTools.csproj'
[xml]$xml = Get-Content -LiteralPath $project
$release = $xml.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
if ($release -notmatch '^(\d+\.\d+\.\d+)(-.*)?$') { throw "Expected a three-part release version: $release" }
$version = "$($Matches[1])$revision$($Matches[2])"
$properties = @("-p:DynamicwebVersion=$floor", "-p:Version=$version")
# Rebuild before --no-build test/pack: a previous floor must never leak into this package.
dotnet build Truvio.Commerce.PowerTools.sln -c Release --no-incremental @properties
if ($LASTEXITCODE) { throw "Build failed for $Line" }
dotnet test tests/Truvio.Commerce.PowerTools.Tests -c Release --no-build --no-restore @properties
if ($LASTEXITCODE) { throw "Tests failed for $Line" }
dotnet pack $project -c Release --no-build --no-restore @properties --output $Output
if ($LASTEXITCODE) { throw "Pack failed for $Line" }
$package = Join-Path $Output "Truvio.Commerce.PowerTools.$version.nupkg"
& "$PSScriptRoot/verify-floor-binding.ps1" -Package $package -Floor $floor
if ($LASTEXITCODE) { throw "Floor verification failed for $Line" }
Write-Host "Verified $Line : $version / Dynamicweb $floor"
