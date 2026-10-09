# Разработка и сборка

[← README](../README.md) · [Готовые команды запуска](../README.md#быстрый-старт)

Этот раздел нужен тем, кто меняет код или собирает приложение самостоятельно. Для обычного запуска достаточно скачать готовую программу в [Releases](https://github.com/huksleva/wukong-benchmark-automation/releases/latest).

Из корня репозитория на Windows с .NET 8 SDK:

```powershell
dotnet run --project tests/Wukong.Tests -c Release
powershell -ExecutionPolicy Bypass -File packaging/Publish-Windows.ps1 -VerifyImageOcr
```

Первая команда `dotnet run --project tests/Wukong.Tests -c Release` собирает и запускает **проверки кода**, а не игровой бенчмарк. `--project` выбирает проект, `-c Release` — конфигурацию сборки. Вторая команда запускает PowerShell-скрипт упаковки EXE; `-VerifyImageOcr` дополнительно проверяет распознавание двух сохранённых PNG. `-ExecutionPolicy Bypass` относится к этому процессу PowerShell и не меняет постоянную политику системы.

Переносимый инструмент можно запустить из исходников на Windows/Linux/macOS с .NET 8 SDK:

```bash
dotnet run --project src/Wukong.Reports -c Release -- --help
dotnet run --project src/Wukong.Reports -c Release
```

Без аргументов он показывает включённый отчёт, не запускает тест. Сборка нативного архива требует также Python 3.12+: например, `python3 packaging/publish-reports.py --rid linux-x64` на Linux x64. Скрипт выполняет проверки на ОС сборки, поэтому выбранная архитектура должна соответствовать компьютеру. [Публикация пакетов →](RELEASING.md)

Проверки общего сценария меню выполняются на всех поддерживаемых ОС:

```bash
dotnet run --project tests/Wukong.Engine.Tests -c Release
```

`Wukong.Engine` не использует Windows API: окно и OCR подключаются через интерфейсы адаптеров. Сейчас рабочие игровые адаптеры есть для Windows; Linux-адаптер и игровой контейнерный запуск ещё предстоит реализовать и проверить. Docker для сохранённых отчётов уже доступен. [План Linux/Docker и усиления проекта →](PORTING.md)

## Запуск из исходников (Windows)

Установите [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) и Git, затем выполните:

```powershell
git clone https://github.com/huksleva/wukong-benchmark-automation.git
cd wukong-benchmark-automation
dotnet build src/Wukong.Automation/Wukong.Automation.csproj -c Release
dotnet run --project src/Wukong.Automation -c Release -- start
```

