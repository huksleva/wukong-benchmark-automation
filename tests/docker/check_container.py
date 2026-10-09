"""Exercise the actual Compose service; Docker and Python 3.12+ are required."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
COMPOSE = ["docker", "compose", "-f", str(ROOT / "compose.yaml")]
PASSED = []
inputs = ROOT / "results"
inputs.mkdir(exist_ok=True)


def command(args, ok=True):
    result = subprocess.run(args, cwd=ROOT, capture_output=True, text=True,
                            encoding="utf-8", timeout=120)
    if ok:
        assert result.returncode == 0, (args, result.stdout, result.stderr)
    else:
        assert result.returncode == 1 and "ERROR:" in result.stderr, (
            args, result.returncode, result.stdout, result.stderr)
    return result.stdout


def run(*args, ok=True):
    return command([*COMPOSE, "run", "--rm", "-T", "reports", *args], ok=ok)


def checked(name):
    PASSED.append(name)
    print("PASS: " + name, flush=True)


assert "does NOT launch" in run("--help")
checked("help states report-only scope")
assert "No new benchmark" in run()
checked("default command reads bundled real report")
doctor = json.loads(run("doctor"))
assert doctor["benchmarkAutomation"] is False and doctor["bundledSample"]
assert doctor["mode"] == "saved-reports-only"
checked("container diagnostics and packaged sample")
report = json.loads(run("show", "--report", "/app/sample/report.json", "--json"))
assert report["status"] == "completed"
assert [p["metrics"]["averageFps"] for p in report["passes"]] == [27, 2]
checked("published CPU/GPU report values")
for profile, expected in [("cpu", (27, 22, 32, 24)), ("gpu", (2, 2, 2, 2))]:
    metrics = json.loads(run("parse", "--ocr", f"/app/sample/{profile}/result-ocr.json"))
    assert tuple(metrics[key] for key in [
        "averageFps", "minimumFps", "maximumFps", "fps95PercentAbove"]) == expected
    checked(profile + " saved OCR values")

# Files are created under the service's real host mount; paths include spaces.
# Linux CI runs Docker with a different UID, so fixtures must be readable by it.
with tempfile.TemporaryDirectory(prefix="docker checks ", dir=inputs) as temporary:
    folder = Path(temporary)
    folder.chmod(0o755)
    fixture = folder / "saved report.json"
    shutil.copyfile(ROOT / "docs/results/2026-10-08/report.json", fixture)
    fixture.chmod(0o644)
    before = hashlib.sha256(fixture.read_bytes()).hexdigest()
    mounted = f"/data/{folder.name}/saved report.json"
    loaded = json.loads(run("show", "--report", mounted, "--json"))
    assert loaded == report
    checked("host bind mount and paths with spaces")
    run("show", "--report", f"/data/{folder.name}/missing.json", ok=False)
    checked("missing input exits with clear error")
    invalid = folder / "invalid.json"
    invalid.write_text("{}", encoding="utf-8")
    invalid.chmod(0o644)
    run("show", "--report", f"/data/{folder.name}/invalid.json", ok=False)
    checked("invalid report rejected")
    invalid.write_text('{"width":1280,"height":720,"lines":['
                       '{"text":"Average FPS","words":null}]}', encoding="utf-8")
    run("parse", "--ocr", f"/data/{folder.name}/invalid.json", ok=False)
    checked("invalid OCR rejected")
    assert hashlib.sha256(fixture.read_bytes()).hexdigest() == before
    checked("input bytes preserved")

run("run", ok=False)
checked("new benchmark request rejected")
run("--unknown", ok=False)
checked("unknown command rejected")
config = json.loads(command([*COMPOSE, "config", "--format", "json"]))["services"]["reports"]
assert config["network_mode"] == "none" and config["read_only"]
assert "ALL" in config["cap_drop"] and "no-new-privileges:true" in config["security_opt"]
assert any(v["target"] == "/data" and v["read_only"] for v in config["volumes"])
checked("offline runtime and least-privilege Compose settings")
probe = command([*COMPOSE, "run", "--rm", "-T", "--entrypoint", "/bin/sh", "reports", "-c",
                 '[ "$(id -u)" = 1654 ] && '
                 '! touch /app/.write-probe 2>/dev/null && '
                 '! touch /data/.write-probe 2>/dev/null && echo "readonly nonroot"'])
assert "readonly nonroot" in probe
checked("actual non-root process and read-only root/input mount")
print(f"{len(PASSED)} Docker checks passed on {doctor['os']} / {doctor['architecture']}")
