"""Verify fresh Linux replay outputs, then retain scoped evidence and cross-OS differences."""
import hashlib
import json
import math
from pathlib import Path
import shutil

from analyze_m01 import digest


def main():
    target = Path("docs/reports/m01-evidence/linux")
    target.mkdir(parents=True, exist_ok=True)
    build = Path("Builds/LinuxServer")
    windows = json.loads(Path("docs/reports/m01-evidence/simulation.json").read_text(encoding="utf-8-sig"))
    runs = [json.loads((build / f"m01-proof-{i}.json").read_text(encoding="utf-8-sig")) for i in (1, 2)]
    assert all(r["passed"] and len(r["cases"]) == 9 for r in runs)
    comparison = []
    for win, first, second in zip(windows["cases"], runs[0]["cases"], runs[1]["cases"]):
        assert win["scenario"] == first["scenario"] == second["scenario"]
        assert first["checksum"] == second["checksum"] == digest(first["finalState"]) == digest(second["finalState"])
        cells = first["finalState"]["Chunks"][0]["Cells"]
        other = win["finalState"]["Chunks"][0]["Cells"]
        state = first["finalState"]; area = state["CellSizeM"] ** 2
        assert state["Tick"] == 600 and len(first["samples"]) == len(second["samples"]) == 600
        fuel = sum(c["InitialFuelKgPerM2"] - c["RemainingFuelKgPerM2"] for c in cells) * area
        water = sum(c["WaterKgPerM2"] for c in cells) * area + state["EvaporatedWaterKg"]
        assert math.isclose(fuel, state["ConsumedFuelKg"], abs_tol=1e-6, rel_tol=1e-10)
        assert math.isclose(water, state["AppliedWaterKg"], abs_tol=1e-6, rel_tol=1e-10)
        phases = sum(a["Phase"] != b["Phase"] for a, b in zip(cells, other))
        assert phases == 0
        comparison.append({"scenario": first["scenario"], "windowsChecksum": win["checksum"],
                           "linuxChecksum": first["checksum"], "linuxRepeatMatches": True,
                           "crossOsBitExact": win["checksum"] == first["checksum"], "phaseDifferences": phases,
                           "maximumAbsoluteCellDifferences": {
                               key: max(abs(a[key] - b[key]) for a, b in zip(cells, other))
                               for key in ("RemainingFuelKgPerM2", "WaterKgPerM2", "Heat", "Intensity", "Smoke")},
                           "linuxBurnedAreaM2": first["samples"][-1]["burnedAreaM2"]})
    for i in (1, 2):
        markers = [line for line in (build / f"m01-proof-{i}.log").read_text(encoding="utf-8", errors="replace").splitlines()
                   if line.startswith("FIRELINE_")]
        assert any("PROOF passed=True headless=True cases=9" in line for line in markers)
        (target / f"proof-{i}-markers.txt").write_text("\n".join(markers) + "\n", encoding="utf-8")
    for suffix in ("json", "csv"):
        shutil.copyfile(build / f"m01-proof-1.{suffix}", target / f"simulation.{suffix}")
    sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
    hashes = {p.name: sha(p) for p in (build / "FirelineServer_Data/Managed").glob("Fireline*.dll")}
    assert "Fireline.Client.dll" not in hashes
    summary = {"passed": True, "processes": 2, "casesPerProcess": 9,
               "scope": "Linux x64 Mono under WSL2/workspace Ubuntu Base 24.04.5 / GLIBC 2.39; not native hosting or Linux client networking",
               "binarySha256": sha(build / "FirelineServer.x86_64"), "runtimeHashes": hashes,
               "rawResultHashes": [sha(build / f"m01-proof-{i}.json") for i in (1, 2)], "comparison": comparison,
               "limitation": "Gust heat differs at floating-point precision across OS runtimes; same-runtime replays match. M02 must replicate authority rather than assume client resimulation matches."}
    (target / "headless-proof-summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print("PASS: two Linux replays, canonical digests, mass accounting and cross-OS phase comparisons; floating-point limits retained.")


if __name__ == "__main__":
    main()
