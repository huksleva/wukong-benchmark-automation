# Поддержка платформ

[← README](../README.md)

| Платформа | Автоматизация | Отчёты |
|---|---|---|
| Windows 10/11 x64 | Автономный EXE; проверка готовности, два прохода, OCR | Создание и просмотр HTML/JSON |
| Linux | Не поддерживается | Просмотр опубликованного HTML/JSON |
| macOS | Не поддерживается | Просмотр опубликованного HTML/JSON |
| Windows ARM64 / x86 | Нативной сборки нет; эмуляция не проверена | Просмотр HTML/JSON |

В [официальной карточке Steam](https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/) Benchmark Tool указан для Windows. Текущая автоматизация использует Win32 для окон, Windows OCR, WinForms для экрана, реестр Steam и Windows CIM для оборудования. Поэтому публикация с другим Runtime Identifier сама по себе не создаёт работающий Linux/macOS-порт.

Wine, Proton, WSL и виртуальные машины не входят в проверенную конфигурацию. Для переноса понадобятся другая реализация управления окном, OCR, обнаружения установки и оборудования, а затем два настоящих интеграционных прохода на целевой платформе. Файлы с расширением для другой ОС без такой реализации не публикуются.

## Windows: запуск тестов

[Скачайте EXE](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe) в доступную для записи папку и откройте двойным щелчком. Альтернатива из PowerShell в его папке:

```powershell
.\Wukong.Automation-win-x64.exe start
```

## macOS: просмотр настоящего отчёта

Это команды для просмотра существующего отчёта, а не для запуска теста. Нужен Git:

```bash
git clone https://github.com/huksleva/wukong-benchmark-automation.git
cd wukong-benchmark-automation
open docs/results/2026-10-08/report.html
```

## Linux: просмотр настоящего отчёта

Нужны Git, графический рабочий стол и браузер:

```bash
git clone https://github.com/huksleva/wukong-benchmark-automation.git
cd wukong-benchmark-automation
xdg-open docs/results/2026-10-08/report.html
```

Без Git можно [скачать исходники ZIP](https://github.com/huksleva/wukong-benchmark-automation/archive/refs/heads/main.zip), распаковать и открыть `docs/results/2026-10-08/report.html` в браузере. Сохраните соседние папки `cpu` и `gpu`, чтобы изображения в отчёте отображались.
