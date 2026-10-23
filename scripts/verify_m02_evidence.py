"""Independently decode actual checkpoint bytes and verify real M02 process/transport evidence; no simulation is substituted."""
import hashlib
import json
import math
from pathlib import Path
import struct
from analyze_m01 import digest

ROOT = Path("docs/reports/m02-evidence")


def checkpoint(path):
    """Read the fixed little-endian FLS1 layout, validate SHA/FNV, and independently check SI mass totals and phase counts."""
    data = path.read_bytes()
    assert 144 <= len(data) <= 2 * 1024 * 1024
    assert hashlib.sha256(data[:-32]).digest() == data[-32:]
    offset = 0

    def take(fmt):
        """Consume one bounded fixed-format field tuple from the protected checkpoint payload."""
        nonlocal offset
        size = struct.calcsize(fmt)
        assert offset + size <= len(data) - 32
        result = struct.unpack_from(fmt, data, offset); offset += size
        return result[0] if len(result) == 1 else result

    assert take("<I") == 0x31534C46 and take("<i") == 1
    state = {"Seed": take("<i"), "Size": take("<i"), "CellSizeM": take("<d"), "StepSeconds": take("<d"),
             "Tick": take("<Q"), "InactiveCursor": take("<i"), "AppliedWaterKg": take("<d"),
             "EvaporatedWaterKg": take("<d"), "ConsumedFuelKg": take("<d")}
    state["Wind"] = dict(zip(("EastMps", "NorthMps", "GustMps", "Revision"), take("<dddQ")))
    state["Checksum"] = f'{take("<Q"):016x}'
    count = take("<i"); assert 0 <= count <= 16 and 4 <= state["Size"] <= 32
    chunks = []
    for _ in range(count):
        sector, x, z, revision, tick, active, cells = take("<iiiQQBi")
        assert active in (0, 1) and cells == state["Size"] ** 2
        values = []
        for _ in range(cells):
            raw = take("<i8diiQQ")
            values.append(dict(zip(("Fuel", "InitialFuelKgPerM2", "RemainingFuelKgPerM2", "Moisture", "WaterKgPerM2",
                                    "Heat", "Intensity", "Smoke", "ElevationM", "Phase", "Vegetation", "VegetationId", "LastChangedTick"), raw)))
        chunks.append({"Id": {"Sector": sector, "X": x, "Z": z}, "Revision": revision,
                       "LastSimulatedTick": tick, "Active": bool(active), "Cells": values})
    state["Chunks"] = chunks
    assert offset == len(data) - 32 and digest(state) == state["Checksum"]
    cells = [cell for chunk in chunks for cell in chunk["Cells"]]
    area = state["CellSizeM"] ** 2
    retained = sum(c["WaterKgPerM2"] for c in cells) * area
    consumed = sum(c["InitialFuelKgPerM2"] - c["RemainingFuelKgPerM2"] for c in cells) * area
    assert math.isclose(retained + state["EvaporatedWaterKg"], state["AppliedWaterKg"], rel_tol=1e-10, abs_tol=1e-6)
    assert math.isclose(consumed, state["ConsumedFuelKg"], rel_tol=1e-10, abs_tol=1e-6)
    return {"sha256": hashlib.sha256(data).hexdigest(), "checksum": state["Checksum"], "tick": state["Tick"],
            "wind": state["Wind"], "appliedWaterKg": state["AppliedWaterKg"], "retainedWaterKg": retained,
            "evaporatedWaterKg": state["EvaporatedWaterKg"], "consumedFuelKg": consumed,
            "charredVegetation": sum(c["Vegetation"] == 2 for c in cells),
            "exhaustedCharredCells": sum(c["Phase"] == 5 for c in cells)}


def main():
    """Require both direct/impaired initial/restart cases, exact three-client parity, real drops and explicit negative-case exits."""
    observations = []
    for case in ("direct", "impaired"):
        summary = json.loads((ROOT / case / "summary.json").read_text(encoding="utf-8-sig"))
        assert summary["passed"] and len(summary["phases"]) == 2
        for phase in ("initial", "restart"):
            folder = ROOT / case / phase
            server = json.loads((folder / "server.json").read_text(encoding="utf-8-sig"))
            value = checkpoint(folder / "server.fls")
            assert server["passed"] and value["tick"] == 120 and value["appliedWaterKg"] == 14700
            assert value["wind"]["Revision"] == 2 and value["wind"]["NorthMps"] == 8 and value["exhaustedCharredCells"] > 0
            ids = []
            for number in (1, 2, 3):
                client = json.loads((folder / f"client-{number}.json").read_text(encoding="utf-8-sig"))
                replica = checkpoint(folder / f"client-{number}.fls")
                assert client["passed"] and replica == value and client["pongs"] > 0 and client["rejected"] > 0
                ids.append(client["networkId"])
                if phase == "initial" and number == 3:
                    assert client["initialTick"] >= 40 and client["initialCharredCells"] > 0
            assert len(set(ids)) == 3
            if phase == "restart":
                assert value == observations[-1]["state"] and server["loadedChecksum"] == value["checksum"]
                assert "FIRELINE_PROOF_EVENT" not in (folder / "server-markers.txt").read_text(encoding="utf-8-sig")
            if case == "impaired":
                proxy = json.loads((folder / "proxy.json").read_text(encoding="utf-8-sig"))
                assert sum(c["randomDrops"] for c in proxy["clients"]) > 0 and proxy["maximumQueuedDatagrams"] <= 4096
            observations.append({"case": case, "phase": phase, "state": value})
    assert all(o["state"] == observations[0]["state"] for o in observations)
    negative = json.loads((ROOT / "negative/summary.json").read_text(encoding="utf-8-sig"))
    assert negative["passed"] and negative["disconnectActualExit"] == 9 and negative["corruptActualExit"] == 6
    assert negative["previousCheckpointPreserved"]
    (ROOT / "independent-verification.json").write_text(json.dumps({"passed": True, "observations": observations,
        "scope": "Actual Windows player artifacts; standard-library SHA/FNV/layout/mass verification, not client resimulation or WAN/capacity qualification"}, indent=2) + "\n", encoding="utf-8")
    print("PASS: 16 real process checkpoint files, independent SHA/FNV/mass checks, direct/impaired/restart parity and negative-case exits.")


if __name__ == "__main__":
    main()
