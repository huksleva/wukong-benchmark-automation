# Third-party notices

The project code is MIT licensed. Its distributed runtime and OCR dependencies keep their own licenses.

| Component | Use | License / source |
|---|---|---|
| .NET 8 runtime / Windows Desktop runtime | Self-contained Windows execution | [MIT runtime license](packaging/licenses/DotNet-runtime-LICENSE.txt), [third-party notices](packaging/licenses/DotNet-runtime-THIRD-PARTY-NOTICES.txt), [Windows Desktop license](packaging/licenses/DotNet-WindowsDesktop-LICENSE.txt) |
| Tesseract .NET wrapper 5.2.0 | Native OCR binding | [Apache-2.0](packaging/licenses/Tesseract-wrapper-LICENSE.txt), [charlesw/tesseract](https://github.com/charlesw/tesseract) |
| Tesseract OCR native engine | Local FPS digit recognition | [Apache-2.0](packaging/licenses/Tesseract-LICENSE.txt), [tesseract-ocr/tesseract](https://github.com/tesseract-ocr/tesseract) |
| Leptonica | Native image processing dependency | [BSD-style license](packaging/licenses/Leptonica-LICENSE.txt), [DanBloomberg/leptonica](https://github.com/DanBloomberg/leptonica) |
| English trained model | Numeric OCR model included with the app | [Apache-2.0](src/Wukong.Automation/tessdata/LICENSE), [pinned source and SHA-256](src/Wukong.Automation/tessdata/README.md) |

Copyright 2012–2022 Charles Weld applies to the .NET wrapper. It includes InteropDotNet, Copyright 2014 Andrey Akinshin, under the [included MIT notice](packaging/licenses/InteropDotNet-LICENSE.txt).

Windows OCR is a Windows component. Steam and Black Myth: Wukong Benchmark Tool are separately installed products and are not redistributed by this repository.

The standalone EXE bundles the license texts in its extracted `licenses` directory, alongside the runtime and OCR model. The release also provides this summary separately. Runtime notices were copied unchanged from the official .NET 8.0.31 runtime packages used for this release; update them when changing the bundled runtime.
