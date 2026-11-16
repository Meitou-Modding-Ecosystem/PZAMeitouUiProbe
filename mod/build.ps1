# PZA's UI Probe for Meitou: build the private test mod and stage its install folder. Author: PZA. Licence: GPLv3 or later.
#
#   .\build.ps1            build Release, stage out\PZAUiProbe\ (PZAUiProbe.dll, meitou.json, PZAUiProbe.mod)
#   .\build.ps1 -Check     also run tools\check: Meitou's own code-mod checks, the patches applied to the real MeitouClient.dll, and the window on a
#                          REAL MyGuiScreenHost with Kenshi's own skins from the install (register, open, bind, arrange, press, drag, Escape),
#                          on a copy of the .NET 11 runtime inside the Meitou release. It never starts the game; it reads the Kenshi install only.
#   -MeitouDir <dir>       an unpacked Meitou release (default: MeitouDir in mod\local.props, git-ignored; see README "Build")
#   -KenshiDir <dir>       the Kenshi install whose data\gui the check loads (default the Steam one)
#
# It installs nothing (README "Install"). out\ is git-ignored.
param(
    [switch]$Check,
    [string]$MeitouDir = "",
    [string]$KenshiDir = "C:\Program Files (x86)\Steam\steamapps\common\Kenshi"
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$localProps = Join-Path $here "local.props"
if ($MeitouDir -eq "" -and (Test-Path $localProps)) { $MeitouDir = ([xml](Get-Content $localProps -Raw)).Project.PropertyGroup.MeitouDir }
if (-not $MeitouDir) { throw "Set -MeitouDir or create mod\local.props (README `"Build`")" }
$MeitouDir = [System.IO.Path]::GetFullPath($MeitouDir)
if (-not (Test-Path (Join-Path $MeitouDir "MeitouClient.dll"))) { throw "MeitouClient.dll not found in $MeitouDir" }

& dotnet build (Join-Path $here "PZAUiProbe.csproj") -c Release -nologo "-p:MeitouDir=$MeitouDir"
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }

$bin = Get-ChildItem (Join-Path $here "bin\Release") -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$stage = Join-Path $here "out\PZAUiProbe"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item (Join-Path $bin.FullName "PZAUiProbe.dll") $stage -Force
Copy-Item (Join-Path $here "meitou.json") $stage -Force

# the data mod Meitou lists the folder by: an FCS mod file with no records, author PZA (same layout as PZA's other Meitou mods)
function Write-FcsString([System.IO.BinaryWriter]$w, [string]$s) {
    $b = [System.Text.Encoding]::ASCII.GetBytes($s); $w.Write([int]$b.Length); $w.Write($b)
}
$fs = [System.IO.File]::Create((Join-Path $stage "PZAUiProbe.mod"))
$w = New-Object System.IO.BinaryWriter($fs)
try {
    $w.Write([int]16); $w.Write([int]1)
    Write-FcsString $w "PZA"
    Write-FcsString $w "PZA's UI Probe for Meitou (code mod pza.uiprobe): a private test window on Meitou's MyGUI window engine. Holds no records. GPLv3."
    Write-FcsString $w ""; Write-FcsString $w ""
    $w.Write([int]0); $w.Write([int]0)
} finally { $w.Close() }

Write-Host "staged $stage :"
Get-ChildItem $stage | ForEach-Object { Write-Host ("  {0,-22} {1,8} bytes" -f $_.Name, $_.Length) }

if ($Check) {
    $rt = Join-Path $here "out\checkrt"
    New-Item -ItemType Directory -Force $rt | Out-Null
    $patterns = "System*.dll", "Microsoft.*.dll", "mscorlib.dll", "netstandard.dll", "WindowsBase.dll", "coreclr.dll", "clrjit.dll",
                "clrgc.dll", "hostfxr.dll", "hostpolicy.dll", "Meitou*.dll", "0Harmony.dll", "Bepu*.dll", "DotRecast*.dll"
    foreach ($p in $patterns) { Copy-Item (Join-Path $MeitouDir $p) $rt -Force }
    $stagedDll = Join-Path $stage "PZAUiProbe.dll"
    & dotnet build (Join-Path $here "tools\check\check.csproj") -c Release -nologo "-p:MeitouDir=$MeitouDir" "-p:ModDll=$stagedDll" -o (Join-Path $here "out\checkbin")
    if ($LASTEXITCODE -ne 0) { throw "check tool build failed ($LASTEXITCODE)" }
    Copy-Item (Join-Path $here "out\checkbin\check.dll") $rt -Force
    Copy-Item $stagedDll $rt -Force
    $cfg = '{ "runtimeOptions": { "tfm": "net11.0", "includedFrameworks": [ { "name": "Microsoft.NETCore.App", "version": "11.0.0" } ] } }'
    Set-Content -Path (Join-Path $rt "check.runtimeconfig.json") -Value $cfg -Encoding ascii
    Push-Location $rt
    try { & dotnet exec --runtimeconfig check.runtimeconfig.json check.dll $stage $KenshiDir } finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw "check FAILED ($LASTEXITCODE)" }
}
