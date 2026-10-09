# Запуск через Docker

[← README](../README.md) · [Обычный запуск без Docker](PLATFORMS.md)

Docker запускает **Wukong Reports**: чтение сохранённых отчётов и разбор OCR JSON. Команда одинакова для Windows, Linux и macOS, на x64 и ARM64. Steam, браузер и установленный .NET для этого варианта не нужны.

**Два новых игровых замера выполняет Windows EXE.** Контейнер показывает результаты уже проведённого теста; FPS из примера относятся к Windows-компьютеру от 8 октября 2026 года. Docker не измеряет производительность вашего компьютера, не запускает Benchmark Tool, не распознаёт PNG и не создаёт новый HTML-отчёт. Существующий HTML можно открыть на хосте.

## Быстрый запуск

Установите [Git](https://git-scm.com/downloads) и запустите [Docker Desktop](https://docs.docker.com/get-started/get-docker/) на Windows/macOS или Docker Engine с [Compose plugin](https://docs.docker.com/compose/install/linux/) на Linux. На Windows нужен режим Linux containers. На Linux команда `docker info` должна работать из текущего пользователя.

В PowerShell, bash или zsh выполните весь блок:

```text
git clone https://github.com/huksleva/wukong-benchmark-automation.git
cd wukong-benchmark-automation
docker compose up --build --exit-code-from reports
```

`--build` собирает образ перед запуском, `--exit-code-from reports` завершает Compose после приложения и возвращает его код завершения. Если проект уже скачан, откройте терминал в его папке и выполните только последнюю команду. Вместо Git можно [скачать ZIP](https://github.com/huksleva/wukong-benchmark-automation/archive/refs/heads/main.zip), распаковать и выполнить последнюю команду из папки с `compose.yaml`.

Первая сборка скачивает закреплённые базовые образы .NET и собирает приложение. Для сборки нужен интернет; на компьютере не требуется SDK. Повторные сборки используют кэш Docker. Архитектуру выбирает Docker автоматически, принудительный `--platform` не нужен.

Контейнер выводит сохранённое оборудование, дату и CPU/GPU FPS в терминал, затем успешно завершает работу. Это нормально: сервер или окно приложения не должны оставаться открытыми. В комплекте настоящий пример: CPU **27 / 22 / 32 FPS**, GPU **2 / 2 / 2 FPS** (среднее / минимум / максимум). Диагностика `doctor` описывает Linux-среду контейнера, а оборудование в отчёте — компьютер исходного замера.

## Свой результат

Compose подключает папку `results/` проекта в `/data` **только для чтения**. Пустая папка уже включена в исходники, чтобы на Linux она принадлежала пользователю после распаковки. Не удаляйте её перед запуском. Скопируйте в неё сохранённую папку запуска, включая JSON, HTML и изображения. Например, при наличии `results/my-run/report.json`:

```text
docker compose run --rm --build reports show --report /data/my-run/report.json
```

Путь `/data/...` одинаков для всех ОС. Имя `my-run` замените на имя своей папки. Путь с пробелами заключайте в двойные кавычки. Не указывайте внутри контейнера путь вида `C:\...`.

Дополнительные команды после первой сборки:

```text
docker compose run --rm reports --help
docker compose run --rm reports doctor
docker compose run --rm reports show --report /data/my-run/report.json --json
docker compose run --rm reports parse --ocr /data/my-run/cpu/result-ocr.json
```

`show` выводит сохранённые метрики; `--json` даёт JSON в стандартный вывод. `parse` повторно разбирает уже сохранённые OCR-данные. Команды `run` и `start` здесь отклоняются с объяснением; они доступны в отдельном Windows-приложении.

## Если команда не запускается

| Сообщение / ситуация | Действие |
|---|---|
| `docker` не найден | Установите Docker по ссылкам выше и откройте новый терминал |
| `docker compose` не найден | Установите Compose plugin / обновите Docker Desktop |
| Не удаётся подключиться к Docker daemon | Запустите Docker Desktop или сервис Docker Engine; дождитесь готовности `docker info` |
| Permission denied для Docker socket на Linux | Настройте доступ к Docker согласно [официальной инструкции](https://docs.docker.com/engine/install/linux-postinstall/); приложение само не меняет права системы |
| No configuration file provided | Перейдите в распакованную папку, где находится `compose.yaml` |
| Ошибка Linux image / неподходящая платформа | На Windows выберите Linux containers; поддерживаются x64 и ARM64 |
| Не скачивается базовый образ | Проверьте интернет и доступ к `mcr.microsoft.com` и Docker Hub, повторите команду; успешная сборка сохраняется в кэше |
| Input file not found | Проверьте файл в `results/` хоста и путь `/data/...` |
| Invalid report / Invalid OCR | Используйте JSON, созданный приложением; повреждённые данные отклоняются, значения FPS не подставляются |
| Steam или Benchmark Tool отсутствует | Чтение отчётов продолжит работать; для новых Windows-замеров используйте [мастер EXE](USAGE.md) |

При успешном чтении код завершения — `0`, при неправильном вводе CLI — `1` с `ERROR:` в stderr. Ошибки подготовки Docker выдаёт сам Docker. Для остановки команды — `Ctrl+C`. После `up` завершённый контейнер сохраняется для повторного запуска; `docker compose down` удаляет его и служебные ресурсы Compose, сохраняя образ и файлы хоста. В дополнительных командах `run --rm` удаляет одноразовый контейнер после выполнения.

## Устройство и проверка

[Dockerfile](../Dockerfile) собирает только Core и Reports, затем оставляет runtime, приложение, лицензию и опубликованный пример. Базовые образы SDK/runtime закреплены по версии и SHA-256 manifest digest; runtime использует .NET 8.0.31. [.dockerignore](../.dockerignore) исключает приватные результаты, Git-метаданные, локальные настройки и рабочие файлы.

[Compose](../compose.yaml) отключает сеть контейнера, задаёт пользователя без root (`1654`), read-only корневую файловую систему и подключение результатов, снимает Linux capabilities и запрещает повышение привилегий. Временная папка `/tmp` находится в памяти. Чтение не меняет Steam/INI и не загружает результаты на сервер.

CI собирает образ и выполняет [15 интеграционных проверок](../tests/docker/check_container.py) на отдельных Linux x64 и ARM64 runners без эмуляции. Проверяются оба OCR-примера, чтение bind mount с пробелами в пути, ошибки ввода, сохранение байтов и действительные ограничения записи/пользователя. Эти проверки не являются игровыми замерами. Результаты CI и границы локальной проверки Docker Desktop на Windows/macOS перечислены в [статусе проверки](VALIDATION.md#docker-для-отчётов).

Для разработчика с Docker и Python 3.12+ после сборки:

```text
python tests/docker/check_container.py
```

Полный игровой Linux/macOS-порт и доступ к GPU контейнера требуют другой реализации и реальных проверок; [план переноса](PORTING.md) описывает их отдельно.
