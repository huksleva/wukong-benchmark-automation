<p align="center">
  <img src="docs/images/banner.svg" alt="Wukong Benchmark Automation — CPU / GPU profiles, OCR, reports" width="100%">
</p>

<h1 align="center">Wukong Benchmark Automation</h1>

<p align="center">Два профиля производительности. Результаты самого бенчмарка. Один локальный отчёт.</p>

<p align="center">
  <a href="https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml"><img src="https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml/badge.svg?branch=main" alt="Build and checks"></a>
  <a href="https://learn.microsoft.com/dotnet/csharp/"><img src="https://img.shields.io/badge/C%23-512BD4?style=flat" alt="C#"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white" alt=".NET 8"></a>
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

Утилита на C# запускает **Black Myth: Wukong Benchmark Tool** из Steam с CPU- и GPU-профилями, распознаёт итоговый экран через **Windows OCR** и сохраняет FPS, оборудование и настройки в **HTML и JSON**.

> **Статус: проверка интеграции.** Сборка и автоматические проверки проходят; установленный Benchmark Tool, OCR и аппаратная диагностика проверены. Два полных автоматических прохода пока не подтверждены. [Подробности и критерии готовности →](docs/VALIDATION.md)

## Возможности

- Обнаружение Steam и установленного Benchmark Tool по AppID `3132990`.
- Последовательные CPU- и GPU-профили с проверкой выбранных значений в меню.
- Разбор среднего, минимального и максимального FPS; сохранение исходного изображения и OCR.
- Единый HTML/JSON-отчёт с CPU, GPU, RAM и подтверждёнными настройками.
- Резервное копирование INI, восстановление после штатного завершения и частичный отчёт при ошибке.

## Демонстрация

**Диагностика `doctor`.** Настоящий вывод команды с локального компьютера, оформленный в виде изображения для документации. Команда проверяет установку, OCR и оборудование, не запуская игру.

![Вывод doctor: установленный Benchmark Tool, успешный OCR, CPU, GPU и RAM](docs/images/doctor-output.png)

**HTML-отчёт.** Скриншот отчёта, созданного штатным генератором. **Синтетические демонстрационные данные; это не результат реального CPU/GPU-теста.**

![Предпросмотр отчёта со статусом demo и синтетическими FPS](docs/images/report-preview.png)

[Пример JSON](docs/examples/report.json) · [Исходный HTML](docs/examples/report.html) · [Как воспроизвести предпросмотр](tools/DocumentationPreview/README.md)

## Быстрый старт

Нужны Windows 10/11 **x64**, Steam и бесплатный [Benchmark Tool](https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/). Полная игра не требуется. Распознаются английские и русские подписи меню; для русского интерфейса нужен русский OCR Windows, для английского — English OCR.

**Готовая сборка:** [скачайте ZIP из успешного запуска Actions](#github-actions), распакуйте его полностью в доступную для записи папку и запустите **`Wukong.Automation.exe` двойным щелчком**. .NET уже включён; SDK, Git и Visual Studio для этого не нужны. Откроется мастер `start`: он проверит готовность и подскажет установку недостающих компонентов. Вход в Steam и первоначальные соглашения требуют вашего решения.

**Из исходников:** установите [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) и Git, затем выполните:

```powershell
git clone https://github.com/huksleva/wukong-benchmark-automation.git
cd wukong-benchmark-automation
dotnet build src/Wukong.Automation/Wukong.Automation.csproj -c Release
dotnet run --project src/Wukong.Automation -c Release -- start
```

Перед `run` закройте Benchmark Tool и диалоги Steam. Во время проходов оставьте окно видимым и не используйте мышь/клавиатуру. При первом ручном запуске дождитесь компиляции шейдеров и пройдите первоначальные диалоги. `Ctrl+C` отменяет сценарий с попыткой восстановления INI.

| Команда | Назначение |
|---|---|
| `start` / запуск EXE без аргументов | Проверить готовность, помочь с установкой и выполнить оба профиля |
| `doctor` | Проверить установку, английский OCR и CPU/GPU/RAM |
| `run` | Выполнить оба профиля и записать отчёт |
| `parse --image result.png` | Разобрать сохранённое изображение результата |
| `parse --ocr result-ocr.json` | Разобрать ранее сохранённые OCR-данные |

Нестандартные пути, таймауты и подписи меню задаются через [runner.example.json](runner.example.json). [Полная инструкция и устранение ошибок →](docs/USAGE.md)

## Профили

| Параметр | CPU | GPU |
|---|---|---|
| Разрешение | 1280 × 720 | Разрешение монитора |
| Качество | Low; дальность High | Cinematic |
| Внутренний рендер | TSR, 50% | TSR, 100% |
| Трассировка лучей | Off | Very High при поддержке |
| Генерация кадров / VSync / лимит FPS | Off | Off |

CPU-профиль уменьшает стоимость рендера, GPU-профиль повышает её. Названия выражают цель настроек: на слабой видеокарте CPU-проход тоже может быть ограничен GPU. [Обоснование настроек и границы методики →](docs/PROFILES.md)

## Результаты

Каждый запуск создаёт `results/<дата-время-id>/` с `report.html`, `report.json`, журналом, резервными INI и изображениями обоих проходов. Парсер отклоняет неоднозначные значения и проверяет `min ≤ average ≤ max`. Показатель «95% FPS above» сохраняется отдельно и не является 1% low.

[Формат и состав артефактов →](docs/OUTPUT.md) · [Какие данные сохраняются →](DATA_HANDLING.md)

## Разработка

```powershell
dotnet run --project tests/Wukong.Tests -c Release
dotnet publish src/Wukong.Automation/Wukong.Automation.csproj -c Release -r win-x64 --self-contained true -o artifacts/runner
```

## GitHub Actions

[**Build and checks**](https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml) — автоматическая проверка проекта. Она запускается при каждом push и открытии или обновлении pull request. Значок вверху README показывает состояние этой проверки для `main`.

На отдельной машине GitHub с Windows workflow:

1. Получает исходники и устанавливает .NET 8 SDK.
2. Собирает приложение в режиме Release.
3. Запускает автоматические проверки парсера FPS, INI, отчётов, конфигурации и распознавания стартовых экранов.
4. Публикует сборку Windows x64 со встроенным .NET и сохраняет её в артефакт **`wukong-runner-windows`**.

Чтобы скачать сборку, откройте **Actions → Build and checks → успешный запуск для нужного коммита → Artifacts → wukong-runner-windows**. Для скачивания артефактов войдите в GitHub. Распакуйте ZIP полностью и запустите `Wukong.Automation.exe` двойным щелчком: откроется мастер подготовки и запуска. **Устанавливать .NET отдельно не нужно.** В архиве есть `QUICKSTART.txt` и `build-info.json` с идентификатором исходного коммита. Артефакты хранятся 14 дней. Из PowerShell в распакованной папке также можно выполнить:

```powershell
.\Wukong.Automation.exe doctor
.\Wukong.Automation.exe start
```

Зелёный статус означает, что сборка и автоматические проверки прошли. **Actions не запускает Steam или Benchmark Tool и не измеряет FPS.** Проверка двух настоящих проходов выполняется локально и описана в [статусе проверки](docs/VALIDATION.md). Workflow находится в [.github/workflows/build.yml](.github/workflows/build.yml).


| Документ | Содержание |
|---|---|
| [Архитектура](docs/ARCHITECTURE.md) | Компоненты и последовательность обработки |
| [Contributing](CONTRIBUTING.md) | Как сообщить об ошибке и предложить изменение |
| [Security](SECURITY.md) | Приватное сообщение об уязвимости |
| [Code of conduct](CODE_OF_CONDUCT.md) | Правила общения в проекте |
| [Обработка данных](DATA_HANDLING.md) | Локальные артефакты и публикация отчётов |

## Лицензия

[MIT](LICENSE) · © 2026 Leonid Tots. Независимый проект; не связан с Game Science, Valve или VK. Названия сторонних продуктов принадлежат их владельцам.

Решение подготовлено с помощью ИИ-помощника Codex; статус реальной проверки указан отдельно.
