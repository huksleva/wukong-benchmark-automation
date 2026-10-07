# Предпросмотр документации

Этот вспомогательный проект использует настоящие `ResultParser` и `ReportWriter` из `Wukong.Core`, но передаёт им **синтетические OCR-фикстуры**. Steam и Benchmark Tool не запускаются. FPS и оборудование в отчёте не являются измерениями.

Из корня репозитория:

```powershell
dotnet run --project tools/DocumentationPreview -c Release -- docs/examples
Start-Process docs/examples/report.html
```

Генератор создаёт `report.html`, `report.json`, две OCR-фикстуры и SVG-иллюстрации их содержимого. Метка `demo` и предупреждение входят в сам отчёт. Скриншот `docs/images/report-preview.png` снят с верхней части этого HTML в браузере.

`docs/images/doctor-output.png` показывает реальный вывод команды `doctor`, отрендеренный в HTML для документации. Его исходный текст находится в [doctor-output.txt](../../docs/examples/doctor-output.txt). При обновлении иллюстрации используйте новый реальный вывод; не заменяйте его вымышленными результатами.
