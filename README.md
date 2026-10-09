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
  <a href="https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe"><strong>⬇ Скачать для Windows x64 — один EXE</strong></a> ·
  <a href="docs/PLATFORMS.md">Linux / macOS: поддержка платформ</a>
</p>

Утилита на C# запускает **Black Myth: Wukong Benchmark Tool** из Steam с CPU- и GPU-профилями, распознаёт итоговый экран через **Windows OCR** и локальный **Tesseract** для цифр и сохраняет FPS, оборудование и настройки в **HTML и JSON**.

> **Проверено на настоящем Benchmark Tool:** два последовательных автоматических прохода, распознавание FPS и побайтовое восстановление INI. Проверка выполнена 8 октября 2026 года на Windows 10 с Ryzen 5 5560U и Radeon Graphics. [Результаты и границы проверки →](docs/VALIDATION.md)

## Возможности

- Обнаружение Steam и установленного Benchmark Tool по AppID `3132990`.
- Последовательные CPU- и GPU-профили с проверкой выбранных значений в меню.
- Разбор среднего, минимального и максимального FPS; сохранение исходного изображения и OCR.
- Единый HTML/JSON-отчёт с CPU, GPU, RAM и подтверждёнными настройками.
- Резервное копирование INI, восстановление после штатного завершения и частичный отчёт при ошибке.

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

## Быстрый старт

Нужны Windows 10/11 **x64**, Steam и бесплатный [Benchmark Tool](https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/). Полная игра не требуется. Распознаются английские и русские подписи меню; для русского интерфейса нужен русский OCR Windows, для английского — English OCR.

### Скачать и запустить

**[Скачать EXE для Windows x64](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe)** · [Все файлы релиза и SHA-256](https://github.com/huksleva/wukong-benchmark-automation/releases/latest)

1. Скачайте **`Wukong.Automation-win-x64.exe`** в папку, доступную для записи, и запустите двойным щелчком.
2. Следуйте проверкам мастера `start`. Если Steam или бесплатный Benchmark Tool отсутствует, мастер предложит открыть установку. Вход в Steam и первоначальные соглашения требуют вашего решения.
3. После подготовки программа сама выбирает настройки, проходит стартовую подсказку и запускает оба теста. Оставьте окно видимым и не используйте мышь/клавиатуру.
4. После успешного завершения откроется HTML-отчёт; исходные настройки будут восстановлены.

Это **один EXE**: .NET, Tesseract и OCR-модель включены. Распаковывать архив, устанавливать SDK, Git или Visual Studio не нужно. Windows OCR и компоненты Steam проверяются отдельно; первое распаковывание внутренних библиотек в кэш Windows может занять время. Файл пока не имеет цифровой подписи издателя. Если Windows показывает предупреждение, сначала проверьте источник и SHA-256 из релиза.

| Операционная система | Что доступно |
|---|---|
| **Windows 10/11 x64** | [Скачать EXE](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe), автоматические CPU/GPU-проходы |
| Linux | Просмотр HTML/JSON-отчётов; запуск автоматизации не поддерживается |
| macOS | Просмотр HTML/JSON-отчётов; запуск автоматизации не поддерживается |

Benchmark Tool поставляется в Steam для Windows. Утилита использует Windows OCR и управление окном через Win32; сборок для Linux/macOS нет. [Поддержка платформ и команды просмотра отчёта →](docs/PLATFORMS.md)

### Из исходников (Windows)

Установите [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) и Git, затем выполните:

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

[Формат и состав артефактов →](docs/OUTPUT.md) · [Какие данные сохраняются →](DATA_HANDLING.md)

## Разработка

```powershell
dotnet run --project tests/Wukong.Tests -c Release
powershell -ExecutionPolicy Bypass -File packaging/Publish-Windows.ps1 -VerifyImageOcr
```

## GitHub Actions

[**Build and checks**](https://github.com/huksleva/wukong-benchmark-automation/actions/workflows/build.yml) — автоматическая проверка проекта. Она запускается при каждом push и открытии или обновлении pull request. Значок вверху README показывает состояние этой проверки для `main`.

На отдельной машине GitHub с Windows workflow:

1. Получает исходники и устанавливает .NET 8 SDK.
2. Собирает приложение и запускает автоматические проверки парсера FPS, INI, отчётов, конфигурации и распознавания стартовых экранов.
3. Собирает автономный EXE с .NET, нативными библиотеками и OCR-моделью. Проверяет его запуск из папки без соседних DLL и разбор сохранённых CPU/GPU-данных.
4. Сохраняет EXE, инструкции, сведения о коммите и SHA-256 в артефакт **`wukong-windows-exe`** на 14 дней.

**Для обычного запуска используйте [GitHub Releases](https://github.com/huksleva/wukong-benchmark-automation/releases/latest)**: ссылка загрузки в начале README ведёт на последний опубликованный релиз и не требует поиска артефактов CI. Релиз публикуется отдельно после проверки; каждый push не заменяет стабильную версию.

Для проверки свежего коммита: **Actions → Build and checks → успешный запуск → Artifacts → wukong-windows-exe**. GitHub требует входа для скачивания артефактов и выдаёт ZIP; извлеките из него EXE. Он работает самостоятельно. `build-info.json` указывает исходный коммит, `SHA256SUMS.txt` — контрольные суммы.

Зелёный статус означает, что сборка и автоматические проверки прошли. **Actions не запускает Steam или Benchmark Tool и не измеряет FPS.** OCR настоящих PNG и диагностика автономного EXE дополнительно проверены локально; два настоящих прохода описаны в [статусе проверки](docs/VALIDATION.md). [Workflow](.github/workflows/build.yml) · [Сборка релиза](docs/RELEASING.md)


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
