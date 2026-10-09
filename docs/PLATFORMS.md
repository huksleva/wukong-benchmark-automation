# Поддержка платформ

[← README](../README.md)

В проекте два приложения: **Wukong Automation** выполняет новые игровые замеры на Windows, **Wukong Reports** читает уже сохранённые результаты на Linux и macOS. Архивы Reports включают .NET и настоящий Windows-отчёт от 8 октября 2026 года с исходными кадрами. Запуск этого примера не является новым замером вашего компьютера.

| Платформа | Два новых прохода и OCR изображений | Сохранённые отчёты и OCR JSON |
|---|---|---|
| Windows 10/11 x64 | [Автономный EXE](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe) | В составе основного приложения; Reports также доступен из исходников |
| Linux x64 | Не реализовано | [Скачать архив](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Reports-linux-x64.tar.gz) |
| Linux ARM64 | Не реализовано | [Скачать архив](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Reports-linux-arm64.tar.gz) |
| macOS Apple Silicon | Не реализовано | [Скачать архив](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Reports-osx-arm64.tar.gz) |
| macOS Intel | Не реализовано | [Скачать архив](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Reports-osx-x64.tar.gz) |
| Windows ARM64 / x86 | Нативной сборки нет; эмуляция не проверена | Просмотр HTML/JSON в подходящем приложении |

## Windows

Скачайте EXE в доступную для записи папку и откройте двойным щелчком. Альтернатива из PowerShell в его папке:

```powershell
.\Wukong.Automation-win-x64.exe start
```

Мастер проверит Steam, бесплатный Benchmark Tool и OCR. [Подготовка и действия при отсутствующих компонентах →](USAGE.md)

## Linux

Команды для терминала с `curl` и `tar`. Они выбирают архив для x64 или ARM64, скачивают его в новую папку и запускают Reports:

```bash
(
  set -eu
  case "$(uname -m)" in
    x86_64) rid=linux-x64 ;;
    aarch64|arm64) rid=linux-arm64 ;;
    *) echo "Архитектура не поддерживается: $(uname -m)"; exit 1 ;;
  esac
  mkdir -p "$HOME/Downloads"
  task_dir=$(mktemp -d "$HOME/Downloads/wukong-reports.XXXXXX")
  cd "$task_dir"
  asset="Wukong.Reports-$rid.tar.gz"
  base=https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download
  curl -fL --retry 2 -o "$asset" "$base/$asset"
  curl -fL --retry 2 -o "$asset.sha256" "$base/$asset.sha256"
  sha256sum -c "$asset.sha256"
  tar -xzf "$asset"
  cd "Wukong.Reports-$rid"
  ./wukong-reports
)
```

Проверяется на GitHub runners: Ubuntu 22.04 x64 и Ubuntu 24.04 ARM64. Используется glibc-сборка; Alpine/musl не поддерживается этим архивом. Минимальная установка Linux может потребовать [системные библиотеки .NET](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#dependencies) (например, OpenSSL и zlib); SDK и отдельный .NET Runtime не требуются. Это не обещание запуска на любом дистрибутиве.

## macOS

Команды для Terminal выбирают Apple Silicon или Intel:

```bash
(
  set -eu
  case "$(uname -m)" in
    arm64) rid=osx-arm64 ;;
    x86_64) rid=osx-x64 ;;
    *) echo "Архитектура не поддерживается: $(uname -m)"; exit 1 ;;
  esac
  mkdir -p "$HOME/Downloads"
  task_dir=$(mktemp -d "$HOME/Downloads/wukong-reports.XXXXXX")
  cd "$task_dir"
  asset="Wukong.Reports-$rid.tar.gz"
  base=https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download
  curl -fL --retry 2 -o "$asset" "$base/$asset"
  curl -fL --retry 2 -o "$asset.sha256" "$base/$asset.sha256"
  shasum -a 256 -c "$asset.sha256"
  tar -xzf "$asset"
  cd "Wukong.Reports-$rid"
  ./wukong-reports
)
```

Проверяется на macOS 15 Intel и Apple Silicon в GitHub Actions. Архив содержит CLI, а не `.app`; запускайте его из Terminal. Сборка не подписана сертификатом Developer ID и не нотарифицирована. Если macOS блокирует скачанный файл, ознакомьтесь с источником и контрольной суммой и используйте штатное разрешение в настройках безопасности согласно [инструкции Apple](https://support.apple.com/102445). Команды не отключают Gatekeeper. CI не проверяет разрешение первого запуска скачанного файла на вашем Mac.

## Команды Reports

Откройте терминал в распакованной папке `Wukong.Reports-<rid>`:

```bash
./wukong-reports --help
./wukong-reports doctor
./wukong-reports show --report ./sample/report.json
./wukong-reports parse --ocr ./sample/cpu/result-ocr.json
```

Для собственного сохранённого замера замените путь после `--report` на `report.json` из Windows-запуска; `--json` выводит структурированные данные. `parse --ocr` читает OCR JSON, а не изображение. Steam, браузер и доступ к сети для этих команд не нужны. Пустой/повреждённый файл или неизвестная команда дают понятную ошибку и ненулевой код завершения.

HTML находится рядом с JSON и может быть открыт в браузере. Без браузера FPS, оборудование, время и статус доступны в терминале. Сохраняйте папки `cpu`/`gpu` рядом с HTML для отображения кадров.

## Почему пока нет игровых проходов на Linux/macOS

[Benchmark Tool в Steam](https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/) поставляется для Windows. Основная автоматизация использует Win32, Windows OCR, WinForms, реестр Steam и Windows CIM. Reports повторно использует переносимый парсер и модели данных, но не эти адаптеры.

[Proton](https://partner.steamgames.com/doc/steamhardware/proton) предоставляет совместимость Windows-игр с Linux; [Game Porting Toolkit](https://developer.apple.com/games/game-porting-toolkit/) содержит инструменты оценки Windows-игр на Apple Silicon. Это не подтверждение работоспособности данного бенчмарка или нашего сценария. Для полной автоматизации нужны отдельные адаптеры окон, снимков, OCR, установки и оборудования, затем два настоящих прохода на каждой ОС. Wine/Proton/macOS-совместимость, WSL и виртуальные машины здесь не проверены. Команда `run` в Reports явно объясняет ограничение и не создаёт ложный результат.
