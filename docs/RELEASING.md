# Сборка и публикация релиза

[← README](../README.md)

Эти шаги предназначены для сопровождающего проекта. Пользователю достаточно [скачать готовый EXE](https://github.com/huksleva/wukong-benchmark-automation/releases/latest/download/Wukong.Automation-win-x64.exe).

## Собрать и проверить

Нужны Windows x64, Git, .NET 8 SDK, Windows English OCR и Microsoft Visual C++ Runtime x64. Из корня репозитория:

```powershell
dotnet run --project tests/Wukong.Tests -c Release
powershell -ExecutionPolicy Bypass -File packaging/Publish-Windows.ps1 -VerifyImageOcr
.\artifacts\release\Wukong.Automation-win-x64.exe doctor
```

Проверьте код завершения каждой команды. Скрипт публикует EXE через [StandaloneWindows.pubxml](../src/Wukong.Automation/Properties/PublishProfiles/StandaloneWindows.pubxml), переносит только EXE в отдельную папку с пробелами, запускает `--help`, проверяет разбор сохранённых OCR-данных и, с `-VerifyImageOcr`, распознавание обоих настоящих PNG. Все четыре метрики сверяются с опубликованными CPU/GPU-результатами. Это проверка упаковки, а не новый игровой замер.

`artifacts/release` содержит:

- `Wukong.Automation-win-x64.exe` — самостоятельный Windows x64 EXE;
- `SHA256SUMS.txt` — SHA-256 остальных файлов;
- `build-info.json` — версия и исходный коммит;
- `QUICKSTART.txt`, `LICENSE`, `THIRD_PARTY_NOTICES.md` — инструкции и лицензирование.

.NET, managed/native-библиотеки, модель Tesseract и тексты лицензий включены в EXE. Используется извлечение всех компонентов при старте: Tesseract ожидает физические пути к DLL и модели. [Этот режим .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview) выбран для совместимости существующего адаптера; при обновлении .NET нужно повторно проверить его. Обрезка сборки отключена. Первый старт извлекает зависимости в пользовательский `%TEMP%/.net`; файлов рядом с EXE не требуется.

## Опубликовать

1. Обновите версию в профиле публикации, `Publish-Windows.ps1` и `QUICKSTART.txt`. Зафиксируйте изменения, выполните push и соберите пакет из чистого коммита.
2. Создайте черновик GitHub Release с тегом версии, например `v0.1.0`, на проверенном коммите `main`.
3. Прикрепите шесть перечисленных файлов. В описании укажите ОС, требования, реальные проверки и ограничения. Не включайте персональные логи или резервные INI.
4. Опубликуйте релиз как Latest. Имя EXE должно оставаться **`Wukong.Automation-win-x64.exe`**: на него ведёт стабильная ссылка README.
5. Скачайте EXE через ссылку README, сравните SHA-256 и повторите `--help`, `doctor`, разбор обоих PNG из папки без соседних DLL.

GitHub Actions автоматически проверяет коммиты и собирает пакет, но не публикует каждый push как стабильный релиз. Публикация выполняется отдельно после проверки. CI не запускает Steam и не измеряет FPS.
