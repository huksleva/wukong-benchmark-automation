# Руководство по запуску

[← README](../README.md)

## Требования

1. Windows 10/11 с интерактивным рабочим столом и установленным [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Steam, вход в аккаунт и установленный **бесплатный** [Black Myth: Wukong Benchmark Tool](https://store.steampowered.com/app/3132990/Black_Myth_Wukong_Benchmark_Tool/) — AppID `3132990`. Покупка полной игры не требуется.
3. Английский язык интерфейса бенчмарка и компонент английского OCR Windows. Утилита передаёт `-culture=en`; если сохранённые настройки переопределяют язык, один раз выберите English в интерфейсе инструмента.
4. Закройте работающий Benchmark Tool, оверлеи и диалоги Steam. Во время запуска оставьте окно теста видимым и не используйте клавиатуру/мышь. Steam и утилита должны работать с одинаковыми правами.

При первом запуске дождитесь компиляции шейдеров и самостоятельно пройдите первоначальные диалоги. Затем закройте Benchmark Tool перед запуском автоматизации. В Steam используйте обычный вариант запуска и сохраните выбор, чтобы диалог не перекрывал окно.

## Запуск из исходников

Из корня репозитория:

```powershell
dotnet build src/Wukong.Automation/Wukong.Automation.csproj -c Release
dotnet run --project src/Wukong.Automation -c Release -- doctor
dotnet run --project src/Wukong.Automation -c Release -- run
```

`doctor` проверяет установку, распознавание текста на синтетическом изображении и получение CPU/GPU/RAM. Бенчмарк при этом не запускается.

В основном сценарии утилита находит Steam и его библиотеки, сохраняет исходные INI, запускает инструмент через `Steam.exe -applaunch 3132990`, применяет профиль, считывает выбранные значения из меню, запускает тест и ожидает стабильный экран результатов. Затем закрывает инструмент и повторяет сценарий с другим профилем. В конце восстанавливает исходные INI. `Ctrl+C` отменяет запуск с сохранением частичного отчёта.

## Параметры и отдельный разбор результатов

Для стандартной установки параметры не требуются. Для другой папки или отличающейся сборки скопируйте пример:

```powershell
Copy-Item runner.example.json runner.local.json
dotnet run --project src/Wukong.Automation -c Release -- run --config runner.local.json
```

`steamPath` и `installationDirectory` позволяют переопределить обнаружение путей. `configDirectory` — активная папка `Config/Windows` именно **Benchmark Tool**, а не полной игры. По умолчанию это `<установка>/b1/Saved/Config/Windows`. При необходимости задайте `gpuWidth` и `gpuHeight`; окно должно полностью помещаться на одном мониторе. `enableGpuRayTracing=false` выбирает растеризационный GPU-проход. `valueColumnX` — относительное положение значений в строках меню, по умолчанию 0.75. `maxDiagnosticFrames` ограничивает кольцевой буфер диагностических кадров, по умолчанию 40 на проход; отдельные итоговые и контрольные изображения сохраняются дополнительно.

Можно переопределить массивы подписей через объект `labels`; если он задан, он должен содержать все ключи из `RunnerOptions.Labels`. Нераспознанная подпись или неприменённое обязательное значение останавливают сценарий с диагностикой, вместо продолжения с неизвестными настройками.

Существующий результат можно разобрать отдельно:

```powershell
dotnet run --project src/Wukong.Automation -c Release -- parse --image result.png
dotnet run --project src/Wukong.Automation -c Release -- parse --ocr result-ocr.json
```

## Сборка и проверки

```powershell
dotnet run --project tests/Wukong.Tests -c Release
dotnet publish src/Wukong.Automation/Wukong.Automation.csproj -c Release --self-contained false -o artifacts/runner
```

В каталоге `artifacts/runner` можно запускать `Wukong.Automation.exe doctor` и `Wukong.Automation.exe run`. Для такой сборки нужен установленный .NET 8 Desktop Runtime. GitHub Actions собирает Windows-версию, запускает 13 проверок и сохраняет архив сборки. CI не запускает Steam и не проверяет реальный бенчмарк.

## Диагностика

| Симптом | Что проверить |
|---|---|
| Установка не найдена | AppID `3132990`, библиотеки Steam, `installationDirectory` |
| OCR недоступен | Английский OCR Windows и English в Benchmark Tool |
| Не удаётся получить фокус | Диалог запуска Steam, оверлеи, одинаковые права процессов |
| Настройка не распознаётся | Последние `diagnostic-*.png` и подписи в `labels` |
| Окно выходит за границы монитора | Оконный режим или разрешение, помещающееся на одном мониторе |
| Сценарий остановился | `runner.log`, поле `error` в `report.json`, [статус проверки](VALIDATION.md) |

## Восстановление после аварии

При `Ctrl+C` утилита пытается закрыть запущенные процессы и восстановить исходные INI. При принудительном завершении процесса это не гарантируется. После закрытия Benchmark Tool верните исходные INI из `backup/` в папку из `backup/manifest.json`; удалите INI, созданные во время запуска и отсутствовавшие в манифесте. Не восстанавливайте файлы, пока Benchmark Tool работает.
