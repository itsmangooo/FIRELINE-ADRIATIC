"""Verify actual aircraft RPC traces against server publications and archive bounded, credential-free evidence."""
import argparse
import csv
import gzip
import hashlib
import json
import math
from pathlib import Path
import tempfile


def read_json(path):
    """Read one recorded proof object without accepting missing outcomes as success."""
    return json.loads(path.read_text(encoding="utf-8-sig"))


def verify(capture, evidence):
    """Cross-check every applied client pose, ownership and clock/cost rows against the actual server trace."""
    roles = {role: read_json(capture / f"{role}.json") for role in ("server", "owner", "observer")}
    assert all(result["passed"] for result in roles.values())
    server, owner, observer = (roles[role] for role in ("server", "owner", "observer"))
    assert owner["identity"] != observer["identity"] and owner["identity"] > 0 and observer["identity"] > 0
    assert server["owner"] == 0 and server["disconnectReleases"] == 1 and not server["faulted"]
    assert server["expirations"] >= 1 and len(server["peers"]) == 2
    assert all(owner[key] >= 1 for key in ("sequenceRejects", "epochRejects", "invalidRejects", "tickRejects"))
    assert owner["acceptedInputs"] >= 20 and owner["lastZ"] - owner["firstZ"] > 1
    assert observer["sawOwner"] and observer["sawVacantAfterOwner"] and observer["rejected"] >= 2
    published = {}
    for line in (capture / "server.snapshots.jsonl").read_text().splitlines():
        row = json.loads(line)
        key = row["identity"], row["state"]["Tick"]
        assert key not in published
        published[key] = row["state"]
    counts = {}
    for role in ("owner", "observer"):
        frames = [json.loads(line) for line in (capture / f"{role}.snapshots.jsonl").read_text().splitlines()]
        assert len(frames) == roles[role]["snapshots"] and len(frames) >= 30
        assert all(b["Tick"] > a["Tick"] for a, b in zip(frames, frames[1:]))
        for state in frames:
            assert state == published[roles[role]["identity"], state["Tick"]], (role, state["Tick"])
            assert all(math.isfinite(value) for value in state.values())
            assert abs(sum(state[key] ** 2 for key in ("Qx", "Qy", "Qz", "Qw")) - 1) < 1e-8
            assert state["Z"] != 999999 and state["FuelKg"] == 0 and state["WaterKg"] == 0
            assert state["Owner"] in (0, owner["identity"]) and state["Sequence"] < 1000
            assert state["WindRevision"] >= 1 and state["FireTick"] > 0
        counts[role] = len(frames)
    assert observer["firstTick"] > owner["firstTick"], "Observer must actually join the running session later."
    rows = list(csv.DictReader((capture / "server.csv").open(newline="")))
    previous_tick, discarded, overrun_rows = 0, 0.0, []
    for index, row in enumerate(rows, 1):
        tick, steps = int(row["tick"]), int(row["steps"])
        assert 0 <= steps <= 8 and tick - previous_tick == steps
        assert all(math.isfinite(float(value)) for value in row.values())
        loss = float(row["discarded_s"])
        assert loss >= discarded
        if loss > discarded + 1e-10:
            overrun_rows.append({"sample": index, "elapsedSeconds": float(row["elapsed_s"]),
                                 "callbackGapMs": float(row["callback_gap_s"]) * 1000,
                                 "discardedThisUpdateSeconds": loss - discarded})
        previous_tick, discarded = tick, loss
    assert previous_tick == server["tick"] and len(rows) == server["updates"]
    assert abs(discarded - server["discardedSeconds"]) < 1e-9
    costs = sorted(float(row["host_advance_ms"]) for row in rows)
    gaps = sorted(float(row["callback_gap_s"]) * 1000 for row in rows)
    result = {"passed": True, "roles": roles, "exactMatchedClientSnapshots": counts,
              "recording": "Every live host callback from startup; no warm-up removed. Stopwatch covers FlightAuthority.Advance only.",
              "hostAdvanceMs": {"p95": costs[math.ceil(.95 * len(costs)) - 1], "max": max(costs)},
              "callbackGapMs": {"p95": gaps[math.ceil(.95 * len(gaps)) - 1], "max": max(gaps)},
              "discardedTimeEvents": overrun_rows,
              "limits": "Two loopback headless client players; reliable RPC prototype. No rendered frames, WAN/loss, 100-player, full host CPU/GPU or bandwidth qualification."}
    evidence.mkdir(parents=True, exist_ok=True)
    archives = []
    for role in ("server", "owner", "observer"):
        for suffix in (".json", ".snapshots.jsonl", ".csv"):
            source = capture / (role + suffix)
            if not source.exists():
                continue
            raw = source.read_bytes()
            destination = evidence / (source.name + ".gz")
            destination.write_bytes(gzip.compress(raw, mtime=0))
            archives.append({"path": destination.name, "rawBytes": len(raw), "rawSha256": hashlib.sha256(raw).hexdigest()})
    result["archives"] = archives
    (evidence / "verification.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"passed": True, "matchedSnapshots": counts, "hostAdvanceMs": result["hostAdvanceMs"],
                      "callbackGapMs": result["callbackGapMs"], "discardedEvents": len(overrun_rows)}))


def main():
    """Select fresh raw process outputs or revalidate the immutable archived files in a separate scratch directory."""
    parser = argparse.ArgumentParser(description=__doc__)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--capture", type=Path)
    source.add_argument("--archive", type=Path)
    parser.add_argument("--evidence", type=Path, required=True)
    args = parser.parse_args()
    if args.capture:
        verify(args.capture, args.evidence)
    else:
        assert args.archive.resolve() != args.evidence.resolve(), "Write replay results to a fresh output, never published evidence."
        recorded = read_json(args.archive / "verification.json")
        processes = read_json(args.archive / "process-summary.json")
        with tempfile.TemporaryDirectory(prefix="fireline-flight-check-") as folder:
            capture = Path(folder)
            for item in recorded["archives"]:
                name = Path(item["path"])
                assert name.name == str(name) and name.suffix == ".gz"
                raw = gzip.decompress((args.archive / name).read_bytes())
                assert len(raw) == item["rawBytes"] and hashlib.sha256(raw).hexdigest() == item["rawSha256"]
                (capture / name.stem).write_bytes(raw)
            for process in processes["runs"]:
                assert process["exitCode"] == 0
                raw = (capture / f"{process['role']}.json").read_bytes()
                assert hashlib.sha256(raw).hexdigest() == process["resultSha256"].lower()
            verify(capture, args.evidence)


if __name__ == "__main__":
    main()
