<#
.SYNOPSIS
  Fails unless every Dynamicweb.* assembly reference inside the packed DLL binds to the floor version.

.DESCRIPTION
  The nuspec dependency versions come from the MSBuild property, but the DLL inside the package is
  whatever MSBuild last compiled. An earlier build against a newer platform version is reused by an
  incremental pack, and the result is a package that claims a 10.8.4 floor while its DLL demands
  10.28.x: older hosts log "Could not load file or assembly 'Dynamicweb.CoreUI, Version=10.28.1.0'"
  in Files/System/Log/AddInManager/TypeLoadErrors.log and silently skip every type in the add-in.
  (0.9.1-beta through 0.11.1-beta shipped that way.) This script is the guard against a repeat.

.PARAMETER Package
  Path to the .nupkg to inspect.

.PARAMETER Floor
  The platform version the package must bind to, e.g. 10.8.4.
#>
param(
    [Parameter(Mandatory)] [string] $Package,
    [Parameter(Mandatory)] [string] $Floor
)

$ErrorActionPreference = 'Stop'
$expected = [Version]::new("$Floor.0")

$work = Join-Path ([IO.Path]::GetTempPath()) ("floor-check-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $zip = Join-Path $work 'package.zip'
    Copy-Item -LiteralPath $Package -Destination $zip
    Expand-Archive -LiteralPath $zip -DestinationPath $work -Force

    $dlls = Get-ChildItem -Path (Join-Path $work 'lib') -Recurse -Filter '*.dll'
    if (-not $dlls) { throw "No lib/**/*.dll found in $Package" }

    $bad = @()
    foreach ($dll in $dlls) {
        $fs = [IO.File]::OpenRead($dll.FullName)
        try {
            $pe = [System.Reflection.PortableExecutable.PEReader]::new($fs)
            $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
            foreach ($handle in $md.AssemblyReferences) {
                $ref  = $md.GetAssemblyReference($handle)
                $name = $md.GetString($ref.Name)
                if ($name -notlike 'Dynamicweb*') { continue }
                $line = "$($dll.Name): $name $($ref.Version)"
                Write-Host $line
                if ($ref.Version -ne $expected) { $bad += $line }
            }
        }
        finally { $fs.Dispose() }
    }

    if ($bad.Count -gt 0) {
        Write-Host "::error::Packed DLL is not bound to the $Floor floor:"
        $bad | ForEach-Object { Write-Host "::error::  $_" }
        exit 1
    }
    Write-Host "OK: every Dynamicweb.* reference binds to $expected"
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
