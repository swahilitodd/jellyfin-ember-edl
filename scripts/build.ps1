param([ValidateSet("12", "10.11")][string]$Jellyfin = "12")
$ErrorActionPreference = 'Stop'
$variant = if ($Jellyfin -eq "10.11") { "10.11" } else { "12" }
$framework = if ($Jellyfin -eq "10.11") { "net9.0" } else { "net10.0" }
$dest = Join-Path $PSScriptRoot "../dist/jf$Jellyfin"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$project = Join-Path $PSScriptRoot "../src/EmberEDL.csproj"
dotnet publish $project -c Release -f $framework -p:JellyfinVariant=$variant -o $dest
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Get-ChildItem $dest -File | Where-Object { $_.Name -ne 'Jellyfin.Plugin.EmberEDL.dll' -and $_.Extension -eq '.dll' } | Remove-Item
Write-Host "Built: $dest/Jellyfin.Plugin.EmberEDL.dll"
