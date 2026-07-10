<#
.SYNOPSIS
    Projects Launcher'ın AFK Hesapları ekranı için MCC paketini üretir.

.DESCRIPTION
    Launcher .NET Framework 4.8, MCC ise .NET 10 üzerinde çalışır; aynı süreçte barınamazlar.
    Bu yüzden MCC kendi kendine yeten tek dosyalık bir çalıştırılabilir olarak yayımlanır ve
    launcher onu alt süreç olarak çalıştırır.

    Çıktı:
      dist/projects-afk-<version>.zip     -> LauncherUpdateStream köküne yüklenir
      dist/version-afk.txt                -> version-afk.php içeriği için sürüm metni

    Launcher paketi şu adresten indirir:
      https://mc.projects.gg/LauncherUpdateStream/projects-afk-<version>.zip
    ve güncel sürümü şuradan öğrenir:
      https://mc.projects.gg/LauncherUpdateStream/version-afk.php

    version-afk.php, sürümü tek bir HTML etiketinin içinde döndürmelidir; örneğin:
      <p>v1</p>

.EXAMPLE
    pwsh tools/publish-launcher-afk.ps1 -Version v1
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Runtime = 'win-x64',

    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'MinecraftClient/MinecraftClient.csproj'

if (-not (Test-Path $project)) {
    throw "MinecraftClient.csproj bulunamadi: $project"
}

if (-not $OutputDir) {
    $OutputDir = Join-Path $repoRoot 'dist'
}

$publishDir = Join-Path $repoRoot "artifacts/afk-publish/$Runtime"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "MCC yayimlaniyor ($Runtime)..." -ForegroundColor Cyan

# Tek dosya + kendi kendine yeten: hedef makinede .NET 10 runtime aranmaz.
& dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish basarisiz oldu (cikis kodu $LASTEXITCODE)."
}

$publishedExe = Join-Path $publishDir 'MinecraftClient.exe'
if (-not (Test-Path $publishedExe)) {
    throw "Yayim ciktisinda MinecraftClient.exe yok: $publishDir"
}

# Marka adi: Gorev Yoneticisi'nde 'MinecraftClient' yerine bu gorunsun. Tek dosyali apphost
# dosya adindan bagimsiz calistigi icin yeniden adlandirmak guvenlidir (ic kimlik/kaynaklar degismez).
$clientExeName = 'Projects-Konsol-Hesap.exe'
$exe = Join-Path $publishDir $clientExeName
Move-Item -Path $publishedExe -Destination $exe -Force
Write-Host "Istemci yeniden adlandirildi: $clientExeName" -ForegroundColor Cyan

# Launcher zip'in kokunde Projects-Konsol-Hesap.exe bekler; ara klasor olusturma.
$zipPath = Join-Path $OutputDir "projects-afk-$Version.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Host "Paketleniyor: $zipPath" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

Set-Content -Path (Join-Path $OutputDir 'version-afk.txt') -Value $Version -Encoding utf8 -NoNewline

$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)

Write-Host ''
Write-Host "Tamam. $zipPath ($sizeMb MB)" -ForegroundColor Green
Write-Host 'Yapilacaklar:' -ForegroundColor Yellow
Write-Host "  1. $zipPath dosyasini LauncherUpdateStream/ altina yukleyin."
Write-Host "  2. version-afk.php ciktisini '<p>$Version</p>' olarak guncelleyin."
