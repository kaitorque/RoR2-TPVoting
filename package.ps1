# Builds the plugin and zips an r2modman/Thunderstore-importable package next to this repo.
$ErrorActionPreference = "Stop"
$name = "TPVoting"
$dir = $PSScriptRoot

dotnet build (Join-Path $dir "$name\$name.csproj") -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$staging = Join-Path ([System.IO.Path]::GetTempPath()) "$name-package"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory $staging | Out-Null
Copy-Item (Join-Path $dir "$name\bin\Release\$name.dll") $staging
foreach ($file in "manifest.json", "icon.png", "README.md", "CHANGELOG.md", "LICENSE") {
    Copy-Item (Join-Path $dir $file) $staging
}

$version = (Get-Content (Join-Path $dir "manifest.json") -Raw | ConvertFrom-Json).version_number
$zip = Join-Path (Split-Path $dir -Parent) "Kaitorque-$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip
Remove-Item $staging -Recurse -Force
Write-Host "Packaged $zip"
