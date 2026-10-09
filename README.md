<p align="center">
  <img src="docs/images/banner.svg" alt="Wukong Benchmark Automation — CPU / GPU profiles, OCR, reports" width="100%">
</p>

<h1 align="center">Wukong Benchmark Automation</h1>

<p align="center">Два профиля производительности. Результаты самого бенчмарка. Один локальный отчёт.</p>

<p align="center">
  <a href="https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml"><img src="https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml/badge.svg?branch=main" alt="Build and checks"></a>
  <a href="https://learn.microsoft.com/dotnet/csharp/"><img src="https://img.shields.io/badge/C%23-512BD4?style=flat" alt="C#"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white" alt=".NET 8"></a>
  <a href="https://github.com/charlesw/tesseract"><img src="https://img.shields.io/badge/Tesseract-5.2-4A8F4A" alt="Local Tesseract OCR"></a>
  <a href="docs/DOCKER.md"><img src="https://img.shields.io/badge/Docker-Reports-2496ED?logo=docker&logoColor=white" alt="Docker: portable reports"></a>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4" alt="Windows 10 / 11">
  <a href="https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/"><img src="https://img.shields.io/badge/Steam-3132990-171A21?logo=steam&logoColor=white" alt="Steam AppID 3132990"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-2EA44F" alt="MIT License"></a>
</p>

<p align="center">
  <a href="#быстрый-старт">Быстрый старт</a> ·
  <a href="#демонстрация">Демонстрация</a> ·
  <a href="docs/USAGE.md">Руководство</a> ·
  <a href="docs/PROFILES.md">Методика</a> ·
  <a href="#github-actions">GitHub Actions</a> ·
  <a href="docs/VALIDATION.md">Статус проверки</a>
</p>

<p align="center">
  <a href="https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe"><strong>Скачать для Windows x64 — один EXE</strong></a> ·
  <a href="docs/PLATFORMS.md">Linux / macOS: скачать приложение для отчётов</a>
</p>

Утилита на C# запускает **Black Myth: Wukong Benchmark Tool** из Steam с CPU- и GPU-профилями, распознаёт итоговый экран через **Windows OCR** и локальный **Tesseract** для цифр и сохраняет FPS, оборудование и настройки в **HTML и JSON**.

> **Проверено на настоящем Benchmark Tool:** два последовательных автоматических прохода, распознавание FPS и побайтовое восстановление INI. Проверка выполнена 8 октября 2026 года на Windows 10 с Ryzen 5 5560U и Radeon Graphics. [Результаты и границы проверки →](docs/VALIDATION.md)

## Возможности

- Обнаружение Steam и установленного Benchmark Tool по AppID `3132990`.
- Последовательные CPU- и GPU-профили с проверкой выбранных значений в меню.
- Разбор среднего, минимального и максимального FPS; сохранение исходного изображения и OCR.
- Единый HTML/JSON-отчёт с CPU, GPU, RAM и подтверждёнными настройками.
- Резервное копирование INI, восстановление после штатного завершения и частичный отчёт при ошибке.

## Быстрый старт

**Выберите один вариант ниже и скопируйте его блок целиком кнопкой Copy в правом верхнем углу.** Вставьте в указанный терминал и нажмите Enter. Каждый блок включает скачивание и запуск; новая папка в `Downloads` создаётся автоматически. Повторный запуск не перезаписывает предыдущую папку.

| Задача | Вариант | Что нужно заранее |
|---|---|---|
| Выполнить два новых игровых теста | [Windows](#windows) | Windows 10/11 x64; PowerShell и `curl.exe` |
| Посмотреть результаты без Docker | [Linux и macOS](#linux-и-macos) | bash/zsh, `curl`, `tar`; Linux с glibc или macOS |
| Посмотреть результаты через Docker | [Docker](#docker) | Установленный и запущенный [Docker с Compose](https://docs.docker.com/get-started/get-docker/) |

Для скачивания нужен интернет. **Новые CPU/GPU-замеры выполняет только Windows-приложение.** Linux/macOS и Docker читают сохранённые данные; включённый пример — настоящий Windows-замер от 8 октября 2026 года.

### Windows

Откройте **PowerShell** — например, вкладку PowerShell в Windows Terminal — и вставьте весь блок:

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'
    Get-Command curl.exe -ErrorAction Stop | Out-Null
    $releaseBase = 'https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download'
    $taskFolder = Join-Path (Join-Path $env:USERPROFILE 'Downloads') ('wukong-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
    $taskExe = Join-Path $taskFolder 'Wukong.Automation-win-x64.exe'
    curl.exe -fL --retry 2 --connect-timeout 20 --max-time 300 -o $taskExe "$releaseBase/Wukong.Automation-win-x64.exe"
    if ($LASTEXITCODE -ne 0) { throw 'EXE download failed. Check your internet connection.' }
    curl.exe -fL --retry 2 --connect-timeout 20 --max-time 60 -o (Join-Path $taskFolder 'SHA256SUMS.txt') "$releaseBase/SHA256SUMS.txt"
    if ($LASTEXITCODE -ne 0) { throw 'Checksum download failed.' }
    $checksums = Get-Content (Join-Path $taskFolder 'SHA256SUMS.txt') -Raw
    if ($checksums -notmatch '(?im)^([a-f0-9]{64})\s+\*?Wukong\.Automation-win-x64\.exe\r?$') { throw 'EXE checksum is missing.' }
    $expectedHash = $Matches[1]
    if ((Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash -ne $expectedHash) { throw 'EXE checksum mismatch.' }
    Write-Host "Application and results folder: $taskFolder"
    Push-Location $taskFolder
    try {
        & $taskExe start
        if ($LASTEXITCODE -ne 0) { throw 'Application stopped. Read its diagnostic message above.' }
    } finally { Pop-Location }
}
```

Откроется мастер подготовки. Если Steam или бесплатный [Benchmark Tool](https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/) отсутствует, мастер предложит установку. Вход в Steam, первоначальные соглашения и установка языка Windows OCR могут потребовать ваших действий. Полная игра, .NET SDK, Git и Visual Studio не нужны: зависимости включены в EXE.

После подготовки программа сама применяет настройки, проходит начальную подсказку и выполняет оба теста. **Во время проходов не используйте мышь и клавиатуру, не переключайте и не перекрывайте окна.** Для отмены переключитесь в консоль утилиты, нажмите `Ctrl+C` и дождитесь восстановления настроек. После успешного завершения откроется HTML-отчёт; файлы останутся в созданной папке, исходные INI будут восстановлены.

Можно также [скачать EXE](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe) и открыть двойным щелчком. [Подготовка и диагностика →](docs/USAGE.md)

### Linux и macOS

Откройте **Terminal** с bash/zsh и вставьте весь блок. Он выбирает ОС и архитектуру, проверяет SHA-256, распаковывает архив и запускает Reports:

```bash
(
  set -eu
  case "$(uname -s):$(uname -m)" in
    Linux:x86_64) rid=linux-x64 ;;
    Linux:aarch64|Linux:arm64) rid=linux-arm64 ;;
    Darwin:x86_64) rid=osx-x64 ;;
    Darwin:arm64) rid=osx-arm64 ;;
    *) echo 'Supported platforms: Linux/macOS, x64/ARM64.'; exit 1 ;;
  esac
  mkdir -p "$HOME/Downloads"
  task_dir=$(mktemp -d "$HOME/Downloads/wukong-reports.XXXXXX")
  cd "$task_dir"
  asset="Wukong.Reports-$rid.tar.gz"
  release_base=https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download
  curl -fL --retry 2 --connect-timeout 20 --max-time 300 -o "$asset" "$release_base/$asset"
  curl -fL --retry 2 --connect-timeout 20 --max-time 60 -o "$asset.sha256" "$release_base/$asset.sha256"
  case "$rid" in
    linux-*) sha256sum -c "$asset.sha256" ;;
    osx-*) shasum -a 256 -c "$asset.sha256" ;;
  esac
  tar -xzf "$asset"
  cd "Wukong.Reports-$rid"
  printf 'Application folder: %s\n' "$PWD"
  ./wukong-reports
)
```

Reports покажет оборудование и FPS из включённого отчёта и завершится. Steam, браузер и отдельный .NET Runtime не нужны. Нативные сборки проверены на Ubuntu 22.04 x64, Ubuntu 24.04 ARM64 и macOS 15 Intel/Apple Silicon. Если macOS блокирует неподписанный файл или Linux сообщает об отсутствующей системной библиотеке, используйте [инструкцию по платформам](docs/PLATFORMS.md).

### Docker

Установите и запустите Docker с Compose. На Windows используйте **Linux containers**. Выберите блок для своего терминала; скачивать репозиторий вручную или устанавливать Git не нужно.

**Windows — PowerShell:**

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'
    Get-Command curl.exe -ErrorAction Stop | Out-Null
    Get-Command docker -ErrorAction Stop | Out-Null
    $dockerOs = docker info --format '{{.OSType}}'
    if ($LASTEXITCODE -ne 0) { throw 'Start Docker Desktop and wait until it is ready.' }
    if ($dockerOs -ne 'linux') { throw 'Select Linux containers in Docker Desktop.' }
    docker compose version
    if ($LASTEXITCODE -ne 0) { throw 'Install Docker Compose or update Docker Desktop.' }
    $taskFolder = Join-Path (Join-Path $env:USERPROFILE 'Downloads') ('wukong-docker-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
    $sourceZip = Join-Path $taskFolder 'source.zip'
    curl.exe -fL --retry 2 --connect-timeout 20 --max-time 300 -o $sourceZip 'https://github.com/huksleva/wukong-benchmark-automation/archive/refs/heads/main.zip'
    if ($LASTEXITCODE -ne 0) { throw 'Source download failed. Check your internet connection.' }
    Expand-Archive -LiteralPath $sourceZip -DestinationPath $taskFolder
    $projectFolder = Join-Path $taskFolder 'wukong-benchmark-automation-main'
    Write-Host "Project folder: $projectFolder"
    Push-Location $projectFolder
    try {
        docker compose run --rm --build reports
        if ($LASTEXITCODE -ne 0) { throw 'Docker stopped. Read its diagnostic message above.' }
    } finally { Pop-Location }
}
```

**Linux/macOS — Terminal с bash/zsh; нужны `curl` и `unzip`:**

```bash
(
  set -eu
  docker_os=$(docker info --format '{{.OSType}}')
  [ "$docker_os" = linux ] || { echo 'Use a Linux-container Docker engine.'; exit 1; }
  docker compose version
  mkdir -p "$HOME/Downloads"
  task_dir=$(mktemp -d "$HOME/Downloads/wukong-docker.XXXXXX")
  cd "$task_dir"
  curl -fL --retry 2 --connect-timeout 20 --max-time 300 -o source.zip https://github.com/huksleva/wukong-benchmark-automation/archive/refs/heads/main.zip
  unzip -q source.zip
  cd wukong-benchmark-automation-main
  printf 'Project folder: %s\n' "$PWD"
  docker compose run --rm --build reports
)
```

Первая сборка скачивает базовые образы .NET и может занять время; повторные сборки используют кэш. Контейнер покажет сохранённый отчёт и завершится. Свои папки отчётов помещайте в `results/` внутри созданного проекта; путь проекта выводится в терминале. Контейнер работает без сети и читает эту папку только для чтения. [Свой отчёт, требования и диагностика Docker →](docs/DOCKER.md)

Все основные команды запуска находятся в этом разделе. Дополнительные команды для своих файлов и настройки описаны в [руководстве](docs/USAGE.md), [Reports](docs/PLATFORMS.md#команды-reports) и [Docker](docs/DOCKER.md#свой-результат).

## Демонстрация

**Настоящий автоматический запуск, 8 октября 2026.** Оба прохода выполнены на одном компьютере: AMD Ryzen 5 5560U, встроенная Radeon Graphics, 15,4 GiB доступной RAM. GPU-проход выполнен без RT: это оборудование его не поддерживает. Benchmark Tool показывает предупреждение о низкой VRAM; результаты относятся к этому компьютеру и не гарантируют производительность полной игры.

| Профиль | Средний FPS | Минимальный FPS | Максимальный FPS | 5-й перцентиль |
|---|---|---|---|---|
| CPU | 27 | 22 | 32 | 24 |
| GPU | 2 | 2 | 2 | 2 |

[Полный HTML-отчёт](docs/results/2026-10-08/report.html) · [JSON](docs/results/2026-10-08/report.json) · [Исходные артефакты и SHA-256](docs/results/2026-10-08/provenance.json)

![Настоящий HTML-отчёт после двух автоматических проходов](docs/images/real-report-preview.png)

<details>
<summary>Исходные экраны результатов CPU и GPU</summary>

**CPU: 1280 × 720, TSR 50%, Low с высокой дальностью.**

![Исходный экран CPU-прохода](docs/results/2026-10-08/cpu/result.png)

**GPU: 1920 × 1080, TSR 100%, Cinematic, RT отключена из-за отсутствия поддержки.**

![Исходный экран GPU-прохода](docs/results/2026-10-08/gpu/result.png)

</details>

HTML на GitHub доступен как исходный файл. Чтобы открыть опубликованный отчёт с изображениями, скачайте репозиторий и откройте `docs/results/2026-10-08/report.html`. После собственного успешного запуска локальный отчёт открывается автоматически.

Цифры FPS дополнительно считываются локальным Tesseract. Модель включена в сборку; изображения не отправляются в облако. [Зависимости и лицензии →](THIRD_PARTY_NOTICES.md)

## Профили

Профили заданы заранее для воспроизводимости. По умолчанию программа не подбирает качество под желаемый FPS.

| Настройка | CPU-профиль | GPU-профиль |
|---|---|---|
| Выходное разрешение | 1280 × 720 | **1920 × 1080** |
| Пресет / набор настроек графики | Low / Низк. | Cinematic / Реалистичн. |
| Детализация объектов вдали | High / Выс. | Cinematic через пресет |
| Сглаживание, постобработка, тени, текстуры | Low через пресет | Cinematic через пресет |
| Эффекты, растительность, глобальное освещение, отражения | Low через пресет | Cinematic через пресет |
| Качество волос | Low через пресет | Cinematic через пресет |
| Размытие при движении | Off | Off |
| Метод избыточной выборки / Super Resolution Sampling | TSR | TSR |
| Масштаб рендера / степень избыточной выборки | 50% | 100% |
| Полная трассировка лучей | Off | Very High при поддержке; иначе явно отмеченный проход без RT |
| Генерация кадров | Off | Off |
| VSync / лимит FPS | Off / Off | Off / Off |

**Почему CPU-тест не в Full HD?** 720p и 50% рендера уменьшают обработку пикселей на GPU. High для дальности сохраняет больше объектов в сцене. GPU-профиль использует Full HD и максимальный пресет, чтобы повысить стоимость рендера. Размытие при движении отключено в обоих профилях для одинаковых условий постобработки. Генерация кадров отключена, чтобы измерять производительность без добавленных кадров; VSync и лимит FPS отключены, чтобы не ограничивать результат настройкой монитора.

Это выбор протокола проекта. При сравнении компьютеров используйте одинаковые разрешения, пресеты, режим RT и сборку Benchmark Tool. Изменённое разрешение или проход без RT нужно рассматривать как отдельный сценарий. Разрешение монитора можно запросить явно через `gpuWidth=null` и `gpuHeight=null`; оно больше не является значением по умолчанию.

Названия профилей выражают цель настроек. На слабой видеокарте CPU-проход тоже может быть ограничен GPU; текущая утилита не измеряет загрузку GPU и не доказывает изоляцию CPU. [Обоснование и границы методики →](docs/PROFILES.md)

### Нужно ли подбирать настройки под каждый компьютер?

В стандартном запуске — нет: программа применяет оба профиля сама. «Рекомендуемые настройки» самой игры предназначены для комфортной игры и не используются автоматизацией: они могут дать разное качество на разных компьютерах.

- **Сравнение компьютеров:** используйте одни и те же параметры каждого профиля. Изменение разрешения или режима RT создаёт отдельную серию результатов.
- **Исследование своего компьютера:** можно изменить разрешение GPU или отключить RT в конфигурации; выбранные условия будут видны в отчёте.
- **Слабый GPU:** низкие настройки CPU-профиля уменьшают работу видеокарты, но не превращают игровой пролёт в изолированный тест CPU. Программа сохраняет предупреждение об этом.

Разница CPU/GPU-профилей намеренная: они проверяют один и тот же пролёт с разной стоимостью графики. Их FPS нельзя трактовать как отдельные баллы процессора и видеокарты.

## Результаты

Каждый запуск создаёт `results/<дата-время-id>/` с `report.html`, `report.json`, журналом, резервными INI и изображениями обоих проходов. Парсер отклоняет неоднозначные значения и проверяет `min ≤ average ≤ max`. Показатель «95% FPS above» сохраняется отдельно и не является 1% low.

Chrome не требуется: HTML открывается приложением по умолчанию для `.html`. Средний, минимальный и максимальный FPS обоих проходов и пути к HTML/JSON всегда выводятся в консоль. Если браузер отсутствует или открытие не удалось, замер остаётся завершённым, файлы сохраняются, а консоль объясняет, как посмотреть результат позже. JSON можно открыть в текстовом редакторе; утилита не устанавливает браузер автоматически.

[Формат и состав артефактов →](docs/OUTPUT.md) · [Какие данные сохраняются →](DATA_HANDLING.md)

## Разработка

Для изменения кода, запуска проверок и сборки из исходников используйте [инструкцию разработчика](docs/DEVELOPMENT.md). Для обычного запуска выберите готовый блок в [быстром старте](#быстрый-старт).

`Wukong.Engine` подключает окно и OCR через интерфейсы адаптеров. Игровые адаптеры сейчас реализованы для Windows; Docker и нативный Reports читают сохранённые данные. [План переноса и развития →](docs/PORTING.md)

## GitHub Actions

[**Build and checks**](https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml) — автоматическая проверка проекта. Она запускается при каждом push и открытии или обновлении pull request. Значок вверху README показывает состояние этой проверки для `main`.

На отдельной машине GitHub с Windows workflow:

1. Получает исходники и устанавливает .NET 8 SDK.
2. Собирает приложение и запускает автоматические проверки парсера FPS, INI, отчётов, конфигурации и распознавания стартовых экранов.
3. Собирает автономный EXE с .NET, нативными библиотеками и OCR-моделью. Проверяет его запуск из папки без соседних DLL и разбор сохранённых CPU/GPU-данных.
4. Сохраняет EXE, инструкции, сведения о коммите и SHA-256 в артефакт **`wukong-windows-exe`** на 14 дней.

В четырёх дополнительных заданиях на Linux x64/ARM64 и macOS Intel/Apple Silicon workflow собирает нативный `wukong-reports` с включённым .NET. На каждой ОС он также запускает 5 проверок общего сценария меню с тестовыми адаптерами и 11 проверок CLI: чтение настоящего отчёта, разбор CPU/GPU OCR, диагностику и обработку неправильного ввода. Проверки повторяются после распаковки готового архива в папку с пробелами. Архив и SHA-256 сохраняются в `wukong-reports-<архитектура>` на 14 дней. Эти задания не запускают игру.

В двух заданиях **Docker / amd64** и **Docker / arm64** workflow собирает контейнер Reports на соответствующей архитектуре и выполняет 15 интеграционных проверок: пример и собственный отчёт через bind mount, OCR JSON, ошибки ввода, запрет записи, запуск без root и настройки изоляции. Сборки не публикуют образ в registry: Docker-вариант собирается одной командой из исходников. [Как запустить →](docs/DOCKER.md)

**Для обычного запуска используйте [GitHub Releases](https://github.com/huksleva/wukong-benchmark-automation/releases/latest)**: ссылка загрузки в начале README ведёт на последний опубликованный релиз и не требует поиска артефактов CI. Релиз публикуется отдельно после проверки; каждый push не заменяет стабильную версию.

Для проверки свежего коммита: **Actions → Build and checks → успешный запуск → Artifacts → wukong-windows-exe**. GitHub требует входа для скачивания артефактов и выдаёт ZIP; извлеките из него EXE. Он работает самостоятельно. `build-info.json` указывает исходный коммит, `SHA256SUMS.txt` — контрольные суммы.

Зелёный статус означает, что сборка и автоматические проверки прошли. **Actions не запускает Steam или Benchmark Tool и не измеряет FPS.** OCR настоящих PNG и диагностика автономного EXE дополнительно проверены локально; два настоящих прохода описаны в [статусе проверки](docs/VALIDATION.md). [Workflow](.github/workflows/build.yml) · [Сборка релиза](docs/RELEASING.md)


| Документ | Содержание |
|---|---|
| [Разработка](docs/DEVELOPMENT.md) | Исходники, проверки и сборка |
| [Архитектура](docs/ARCHITECTURE.md) | Компоненты, сценарий и генерация HTML |
| [Docker](docs/DOCKER.md) | Одна команда для отчётов на Windows/Linux/macOS |
| [План развития](docs/PORTING.md) | Полный Linux-порт, Docker и критерии готовности |
| [Contributing](CONTRIBUTING.md) | Как сообщить об ошибке и предложить изменение |
| [Security](SECURITY.md) | Приватное сообщение об уязвимости |
| [Code of conduct](CODE_OF_CONDUCT.md) | Правила общения в проекте |
| [Обработка данных](DATA_HANDLING.md) | Локальные артефакты и публикация отчётов |

## Лицензия

[MIT](LICENSE) · © 2026 Leonid Tots. Независимый проект; не связан с Game Science, Valve или VK. Названия сторонних продуктов принадлежат их владельцам.
