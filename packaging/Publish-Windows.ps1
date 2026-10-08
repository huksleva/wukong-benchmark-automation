# Build a self-contained Windows EXE and verify it without adjacent DLLs.
[CmdletBinding()]
param([switch]$VerifyImageOcr)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $root 'artifacts\release'
$published = Join-Path $root 'artifacts\single-file'
$probe = Join-Path $root ('artifacts\Standalone probe ' + [guid]::NewGuid().ToString('N'))
Push-Location $root
try {
    dotnet publish src/Wukong.Automation/Wukong.Automation.csproj -c Release -p:PublishProfile=StandaloneWindows -o $published
    if ($LASTEXITCODE -ne 0) { throw 'Single-file publish failed.' }
    New-Item -ItemType Directory -Force $destination, $probe | Out-Null
    $name = 'Wukong.Automation-win-x64.exe'
    $exe = Join-Path $probe $name
    Copy-Item (Join-Path $published 'Wukong.Automation.exe') $exe -Force
    Push-Location $probe
    try {
        & $exe --help
        if ($LASTEXITCODE -ne 0) { throw 'Standalone help check failed.' }
        $cases = @(
            @{ Profile = 'cpu'; Average = 27; Minimum = 22; Maximum = 32; Percentile = 24 },
            @{ Profile = 'gpu'; Average = 2; Minimum = 2; Maximum = 2; Percentile = 2 }
        )
        foreach ($case in $cases) {
            $fixture = Join-Path $root ('docs\results\2026-10-08\' + $case.Profile)
            $formats = @('ocr')
            if ($VerifyImageOcr) { $formats += 'image' }
            foreach ($format in $formats) {
                $file = if ($format -eq 'image') { 'result.png' } else { 'result-ocr.json' }
                $result = & $exe parse ('--' + $format) (Join-Path $fixture $file)
                if ($LASTEXITCODE -ne 0) { throw ('Standalone parsing failed: ' + $case.Profile + '/' + $format) }
                $metrics = ($result -join "`n") | ConvertFrom-Json
                if ($metrics.averageFps -ne $case.Average -or $metrics.minimumFps -ne $case.Minimum -or
                    $metrics.maximumFps -ne $case.Maximum -or $metrics.fps95PercentAbove -ne $case.Percentile) {
                    throw ('Unexpected real fixture metrics: ' + $case.Profile + '/' + $format)
                }
                Write-Host ('PASS standalone EXE: ' + $case.Profile + '/' + $format)
            }
        }
        if (@(Get-ChildItem -LiteralPath $probe -Force).Count -ne 1) { throw 'Probe folder must contain only the EXE.' }
    } finally { Pop-Location }
    Copy-Item $exe (Join-Path $destination $name) -Force
    Copy-Item packaging/QUICKSTART.txt, THIRD_PARTY_NOTICES.md, LICENSE $destination -Force
    $commit = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source commit.' }
    @{ commit = $commit; platform = 'win-x64'; runtimeIncluded = $true; singleFile = $true;
       imageOcrChecked = [bool]$VerifyImageOcr; version = '0.1.0' } |
       ConvertTo-Json | Set-Content (Join-Path $destination 'build-info.json') -Encoding UTF8
    $assets = @($name, 'QUICKSTART.txt', 'THIRD_PARTY_NOTICES.md', 'LICENSE', 'build-info.json')
    $hashes = foreach ($asset in $assets) {
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $destination $asset)).Hash.ToLowerInvariant()
        "$hash  $asset"
    }
    $hashes | Set-Content (Join-Path $destination 'SHA256SUMS.txt') -Encoding ASCII
    Write-Host ('Release assets: ' + $destination)
} finally { Pop-Location }
