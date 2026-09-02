# Сборка portable-версии KeyboardTester (self-contained, single-file EXE).
# Использование:
#   .\build-portable.ps1                          — Release, версия из git describe
#   .\build-portable.ps1 -Configuration Debug     — другая конфигурация
#   .\build-portable.ps1 -SkipTests              — без запуска тестов
#   .\build-portable.ps1 -Version "v1.2.3"       — явно задать версию в имени артефакта
# Результат: artifacts/KeyboardTester-<версия>-win-x64/ и artifacts/KeyboardTester-<версия>-win-x64.zip
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [string]$Version,

    # '-SkipTests' — пропустить 'dotnet test' (при локальной сборке без SDK для тестов).
    [Parameter()]
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Версия артефакта -------------------------------------------------------
# git describe пишет ошибки в stderr; при $ErrorActionPreference='Stop'
# редирект 2>$null может бросить NativeCommandError в Windows PowerShell 5.1,
# поэтому оборачиваем в try/catch. При отсутствии git вообще не обращаемся
# к $LASTEXITCODE (StrictMode считает несуществующую переменную ошибкой).
if (-not $Version) {
    $Version = 'dev'
    if (Get-Command git -ErrorAction SilentlyContinue) {
        try {
            $gitTag = (git describe --tags --always 2>$null | Select-Object -First 1)
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace([string]$gitTag)) {
                $Version = ([string]$gitTag).Trim()
            }
        } catch {
            # git есть, но describe не сработал (нет коммитов/тегов) — остаётся 'dev'.
        }
    }
}

$projectPath = 'src/KeyboardTester.UI/KeyboardTester.UI.csproj'
$artifactsRoot = 'artifacts'
$outputDir = Join-Path $artifactsRoot "KeyboardTester-$Version-win-x64"
$zipPath = "$outputDir.zip"

Write-Host "=== KeyboardTester portable build ===" -ForegroundColor Cyan
Write-Host "Конфигурация : $Configuration"
Write-Host "Версия       : $Version"
Write-Host "Каталог      : $outputDir"
Write-Host ''

# --- Проверка предусловий ---------------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK не найден в PATH. Установите .NET 9 SDK или используйте CI-сборку.'
}

if (-not (Test-Path $projectPath)) {
    throw "Не найден проект $projectPath. Запускайте скрипт из корня репозитория."
}

# --- Тесты (необязательно) ---------------------------------------------------
if ($SkipTests) {
    Write-Host '>> Тесты пропущены (-SkipTests).' -ForegroundColor Yellow
}
else {
    Write-Host '>> dotnet test...' -ForegroundColor Cyan
    dotnet test --configuration $Configuration --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw 'Тесты не пройдены, сборка прервана.'
    }
}

# --- Publish ------------------------------------------------------------------
Write-Host '>> dotnet publish (win-x64, self-contained, single-file)...' -ForegroundColor Cyan
dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish завершился с кодом $LASTEXITCODE."
}

# --- Очистка лишних файлов -----------------------------------------------------
Get-ChildItem -Path $outputDir -Filter '*.pdb' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# --- ZIP-архив -----------------------------------------------------------------
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Архивируется каталог целиком: при распаковке получается папка
# KeyboardTester-<версия>-win-x64/ с EXE внутри.
Compress-Archive -Path $outputDir -DestinationPath $zipPath -Force

Write-Host ''
Write-Host "Portable build создан: $zipPath" -ForegroundColor Green
Write-Host "Распакованный каталог: $outputDir"
