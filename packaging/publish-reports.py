"""Publish and test a Reports distribution on its target OS and architecture."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]
VERSION = "0.2.0"
RUNTIME_VERSION = "8.0.31"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rid", required=True, choices=[
        "linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x64"])
    rid = parser.parse_args().rid
    host_os = {"Windows": "win", "Linux": "linux", "Darwin": "osx"}.get(platform.system())
    host_arch = {"x86_64": "x64", "amd64": "x64", "arm64": "arm64", "aarch64": "arm64"}.get(platform.machine().lower())
    if rid != f"{host_os}-{host_arch}":
        raise SystemExit("Select the RID of this host: native CLI tests must actually run here.")

    parent = ROOT / "artifacts/reports" / rid
    out = parent / ("Wukong.Reports-" + rid)
    out.mkdir(parents=True, exist_ok=True)
    subprocess.run([
        "dotnet", "publish", str(ROOT / "src/Wukong.Reports"), "-c", "Release",
        "-r", rid, "--self-contained", "true",
        "-p:RuntimeFrameworkVersion=" + RUNTIME_VERSION,
        "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true", "-p:DebugType=embedded", "-o", str(out),
    ], cwd=ROOT, check=True)
    name = "wukong-reports.exe" if host_os == "win" else "wukong-reports"
    exe = out / name
    exe.chmod(0o755)
    verify(exe)

    nuget = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages"))
    pack = nuget / ("microsoft.netcore.app.runtime." + rid) / RUNTIME_VERSION
    licenses = out / "licenses"
    licenses.mkdir(exist_ok=True)
    for notice in ["LICENSE.TXT", "THIRD-PARTY-NOTICES.TXT"]:
        matches = [path for path in pack.iterdir() if path.name.upper() == notice]
        if len(matches) != 1:
            raise RuntimeError("Cannot find runtime notice: " + notice)
        shutil.copyfile(matches[0], licenses / notice)

    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    (out / "build-info.json").write_text(json.dumps({
        "version": VERSION, "commit": commit, "platform": rid,
        "runtimeIncluded": True, "mode": "saved-reports-only", "nativeCliChecks": 11,
    }, indent=2), encoding="utf-8")
    (out / "QUICKSTART.txt").write_text(
        "Wukong Reports " + VERSION + " — saved reports only; no benchmark is launched.\n"
        "Run ./wukong-reports to read the bundled actual Windows measurement.\n"
        "Run ./wukong-reports show --report /path/to/report.json for your own result.\n"
        "Run ./wukong-reports parse --ocr /path/to/result-ocr.json for saved OCR data.\n"
        "Use --help for commands. Steam and an installed .NET runtime are not required.\n"
        "Full CPU/GPU automation currently requires the separate Windows runner.\n"
        "The sample is a previous Windows session, not a new measurement of this machine.\n",
        encoding="utf-8")
    archive = parent / (out.name + ".tar.gz")
    with tarfile.open(archive, "w:gz") as tar:
        for item in [exe, out / "sample", out / "LICENSE", licenses,
                     out / "build-info.json", out / "QUICKSTART.txt"]:
            tar.add(item, arcname=out.name + "/" + item.name)

    # Verify the actual delivery layout, including executable permissions and sample paths.
    with tempfile.TemporaryDirectory(prefix="wukong packaged reports ") as temp:
        with tarfile.open(archive, "r:gz") as tar:
            tar.extractall(temp, filter="data")
        extracted = Path(temp) / out.name
        if not (extracted / "LICENSE").is_file() or not (extracted / "licenses/LICENSE.TXT").is_file():
            raise RuntimeError("Missing distribution licenses")
        verify(extracted / name)
    sha = hashlib.sha256(archive.read_bytes()).hexdigest()
    archive.with_name(archive.name + ".sha256").write_text(
        sha + "  " + archive.name + "\n", encoding="ascii")
    print("Package: " + str(archive), flush=True)


def verify(exe):
    subprocess.run([sys.executable, str(ROOT / "tests/portable/check_cli.py"), str(exe)],
                   cwd=ROOT, check=True)


if __name__ == "__main__":
    main()
