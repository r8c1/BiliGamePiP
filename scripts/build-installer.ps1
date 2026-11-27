$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$ProjPath = Join-Path $RootDir "src\BiliGamePiP\BiliGamePiP.csproj"
$PublishDir = Join-Path $RootDir "publish\win-x64"
$IssPath = Join-Path $RootDir "installer\BiliGamePiP.iss"

Write-Host "[1/2] Publishing self-contained win-x64 binary..." -ForegroundColor Cyan
if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

dotnet publish $ProjPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $PublishDir

Write-Host "[2/2] Building Windows installer with Inno Setup..." -ForegroundColor Cyan
$IsccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)
$Iscc = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $Iscc) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        $Iscc = $cmd.Source
    }
}

if (-not $Iscc) {
    Write-Warning "Inno Setup 6 (ISCC.exe) not found. Self-contained executable is ready at: $PublishDir\BiliGamePiP.exe"
    Write-Host "Install Inno Setup 6 (https://jrsoftware.org/isdl.php) and re-run this script to generate the Setup .exe." -ForegroundColor Yellow
    exit 0
}

& $Iscc $IssPath
Write-Host "Done! Installer generated in: $(Join-Path $RootDir 'dist')" -ForegroundColor Green
