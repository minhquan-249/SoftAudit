# SoftAudit - build one package per site (same idea as SM-Tool\Build.ps1).
#
#   .\Build.ps1              -> all Sites\*.ini
#   .\Build.ps1 -Site HOT    -> only Sites\HOT.ini
#
# Output: Dist\SoftAudit-<SITE>\ (SoftAudit.exe, SoftAudit.exe.config, SoftAudit.ini,
#         SoftAudit-<SITE>.bat, SoftAudit-Fallback.ps1) and Dist\SoftAudit-<SITE>.zip
# Needs the .NET SDK (dotnet). Works in Windows PowerShell 5.1 and PowerShell 7.

param([string[]]$Site)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'Dist'

Write-Host '== dotnet build (Release)'
& dotnet build (Join-Path $root 'SoftAudit.csproj') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
$bin = Join-Path $root 'bin/Release/net48'

$inis = Get-ChildItem (Join-Path $root 'Sites') -Filter '*.ini' | Where-Object { $_.Name -ne '_example.ini' }
if ($Site) { $inis = $inis | Where-Object { $Site -contains $_.BaseName } }
if (-not $inis) { throw 'No Sites\<SITE>.ini found (copy Sites\_example.ini).' }

foreach ($ini in $inis) {
    $name = $ini.BaseName
    $out = Join-Path $dist "SoftAudit-$name"
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Path $out | Out-Null

    Copy-Item (Join-Path $bin 'SoftAudit.exe'), (Join-Path $bin 'SoftAudit.exe.config') $out
    Copy-Item $ini.FullName (Join-Path $out 'SoftAudit.ini')
    Copy-Item (Join-Path $root 'Templates/Run.bat') (Join-Path $out "SoftAudit-$name.bat")
    Copy-Item (Join-Path $root 'Templates/SoftAudit-Fallback.ps1') $out

    $zip = Join-Path $dist "SoftAudit-$name.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $out -DestinationPath $zip
    Write-Host "== $name -> $zip"
}
