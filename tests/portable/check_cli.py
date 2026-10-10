"""Integration checks of the packaged reports CLI on its actual host OS."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile

EXE = Path(sys.argv[1]).resolve()
ROOT = Path(__file__).resolve().parents[2]


def run(*args, ok=True):
    result = subprocess.run(
        [str(EXE), *map(str, args)], cwd=EXE.parent, capture_output=True,
        text=True, encoding="utf-8", timeout=30)
    if ok:
        assert result.returncode == 0, (args, result.stderr)
    else:
        assert result.returncode != 0 and "ERROR:" in result.stderr, (
            args, result.stdout, result.stderr)
    return result.stdout


assert "does NOT launch" in run("--help")
assert "No new benchmark" in run()
doctor = json.loads(run("doctor"))
assert doctor["benchmarkAutomation"] is False and doctor["bundledSample"]
actual = ROOT / "docs/results/2026-10-08"
report = json.loads(run("show", "--report", actual / "report.json", "--json"))
assert report["status"] == "completed"
assert [item["metrics"]["averageFps"] for item in report["passes"]] == [27, 2]
for name, expected in [("cpu", (27, 22, 32, 24)), ("gpu", (2, 2, 2, 2))]:
    metrics = json.loads(run("parse", "--ocr", actual / name / "result-ocr.json"))
    assert tuple(metrics[key] for key in [
        "averageFps", "minimumFps", "maximumFps", "fps95PercentAbove"]) == expected
with tempfile.TemporaryDirectory(prefix="wukong reports checks ") as temp:
    path = Path(temp) / "invalid.json"
    run("show", "--report", path, ok=False)
    path.write_text("{}", encoding="utf-8")
    run("show", "--report", path, ok=False)
    path.write_text('{"width":1280,"height":720,"lines":['
                    '{"text":"Average FPS","words":null}]}', encoding="utf-8")
    run("parse", "--ocr", path, ok=False)
    ocr = json.loads((actual / "cpu/result-ocr.json").read_text(encoding="utf-8"))
    ocr["lines"] = [line for line in ocr["lines"] if "перцентиль" not in line["text"]]
    ocr["lines"].append({"text": "95% FPS above 999", "words": []})
    path.write_text(json.dumps(ocr), encoding="utf-8")
    run("parse", "--ocr", path, ok=False)
run("run", ok=False)
run("--unknown", ok=False)
print("12 portable CLI checks passed on " + doctor["os"] + " / " + doctor["architecture"])
