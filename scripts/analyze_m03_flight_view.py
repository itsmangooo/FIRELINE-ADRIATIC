"""Independently verify real server/view/observer traces, displayed pose interpolation and rendered callback locations."""
import argparse
import csv
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct
import tempfile


def read(path):
    """Read recorded UTF-8 JSON without treating an absent proof as success."""
    return json.loads(path.read_text(encoding="utf-8-sig"))


def frames(path):
    """Load bounded complete RPC snapshots recorded by an actual process."""
    return [json.loads(line) for line in path.read_text().splitlines()]


def float32(value):
    """Round a double to the actual Unity Transform float representation independently of C# conversion."""
    return struct.unpack("f", struct.pack("f", value))[0]


def verify(capture, evidence, image):
    """Require true process outcomes and recompute every initialized presentation interpolation from recorded source poses/timestamps."""
    roles = {role: read(capture / f"{role}.json") for role in ("server", "view-owner", "observer")}
    assert all(row["passed"] for row in roles.values())
    view, server = roles["view-owner"], roles["server"]
    for key in ("keyboardThrottle", "gamepadThrottle", "gamepadAxes", "neutralOnFocusLoss", "released", "reclaimed", "inspectionCapture"):
        assert view[key], key
    assert view["cameraMask"] == 3 and view["snapshots"] > 100 and view["acceptedInputs"] > 100
    assert server["owner"] == 0 and server["disconnectReleases"] == 1 and server["expirations"] >= 1 and not server["faulted"]
    publications = {(row["identity"], row["state"]["Tick"]): row["state"] for row in frames(capture / "server.snapshots.jsonl")}
    counts = {}
    source = {}
    first_ticks = {}
    for role in ("view-owner", "observer"):
        data = frames(capture / f"{role}.snapshots.jsonl")
        first_ticks[role] = data[0]["Tick"]
        assert len(data) == roles[role]["snapshots"]
        identity = roles[role]["identity"]
        for state in data:
            assert state == publications[identity, state["Tick"]]
            assert state["Owner"] in (0, view["identity"])
            if role == "view-owner":
                source[state["Tick"]] = state
        counts[role] = len(data)
    assert first_ticks["observer"] > first_ticks["view-owner"], "Observer must actually join after the rendered replica."
    rows = list(csv.DictReader((capture / "view-owner.csv").open(newline="")))
    previous = older = newer = None
    distinct = checked = 0
    max_position = max_quaternion = max_anchor = 0.0
    for row in rows:
        assert all(math.isfinite(float(value)) for value in row.values())
        tick = int(row["aircraft_tick"])
        current = source[tick]
        received = float(row["received_at_s"])
        if previous is None or tick != previous:
            if newer is None or current["Epoch"] != newer[0]["Epoch"] or math.dist([current[k] for k in ("X", "Y", "Z")], [newer[0][k] for k in ("X", "Y", "Z")]) > 100:
                older = (current, received)
            else:
                older = newer
            newer = (current, received)
            distinct += 1
            previous = tick
        if distinct < 2:
            continue
        span = newer[1] - older[1]
        t = 1 if span <= 0 else max(0, min(1, (float(row["wall_s"]) - .1 - older[1]) / span))
        expected = [older[0][key] * (1 - t) + newer[0][key] * t for key in ("X", "Y", "Z")]
        shown = [float(row[key]) for key in ("displayed_x_m", "displayed_y_m", "displayed_z_m")]
        error = math.dist(expected, shown)
        assert error < 1e-8, (row["wall_s"], error)
        a, b = ([state[key] for key in ("Qx", "Qy", "Qz", "Qw")] for state in (older[0], newer[0]))
        sign = -1 if sum(x * y for x, y in zip(a, b)) < 0 else 1
        q = [x * (1 - t) + y * t * sign for x, y in zip(a, b)]
        norm = math.sqrt(sum(value * value for value in q))
        q = [value / norm for value in q]
        q_error = max(abs(x - float(row[key])) for x, key in zip(q, ("qx", "qy", "qz", "qw")))
        assert q_error < 1e-10
        anchor_error = math.dist([float32(value) for value in shown], [float(row[key]) for key in ("anchor_x_m", "anchor_y_m", "anchor_z_m")])
        assert anchor_error < 1e-6
        assert float(row["visual_y_m"]) == -2 and float(row["visual_z_m"]) == 0
        if int(row["focused"]) == 0:
            assert float(row["throttle"]) == 0 and float(row["brake"]) == 1
        max_position, max_quaternion, max_anchor = max(max_position, error), max(max_quaternion, q_error), max(max_anchor, anchor_error)
        checked += 1
    assert checked > 500
    assert any(abs(float(row["pitch"]))>.01 and abs(float(row["roll"]))>.01 and abs(float(row["yaw"]))>.01 for row in rows)
    metrics = read(capture / "metrics.json")
    assert metrics["width"] == 1920 and metrics["height"] == 1080 and metrics["quality"] == "Balanced"
    intervals = [float(row["frame_interval_ms"]) for row in csv.DictReader((capture / "metrics.csv").open())]
    assert len(intervals) == metrics["samples"] and len(intervals) > 100
    assert abs(1000 / (sum(intervals) / len(intervals)) - metrics["averageFps"]) < 1e-4
    ordered=sorted(intervals)
    slow=ordered[-math.ceil(.01*len(ordered)):]
    assert abs(1000/(sum(slow)/len(slow))-metrics["onePercentLowFps"])<1e-4
    assert abs(ordered[math.ceil(.95*len(ordered))-1]-metrics["p95FrameMs"])<1e-5
    assert abs(ordered[math.ceil(.99*len(ordered))-1]-metrics["p99FrameMs"])<1e-5
    worst = []
    elapsed = 0
    evidence.mkdir(parents=True, exist_ok=True)
    with (evidence / "callback-intervals.marked.csv").open("w", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(("sample", "start_s", "end_s", "interval_ms", "over_16_667_ms", "over_33_333_ms"))
        for index, value in enumerate(intervals, 1):
            end = elapsed + value / 1000
            writer.writerow((index, elapsed, end, value, value > 1000/60, value > 1000/30))
            worst.append(dict(sample=index, startSeconds=elapsed, endSeconds=end, callbackMs=value))
            elapsed = end
    for path in (image, image.with_suffix(".inspection.png")):
        raw = path.read_bytes()
        assert raw[:8] == b"\x89PNG\r\n\x1a\n" and struct.unpack(">II", raw[16:24]) == (1920, 1080)
    archives = []
    for role in ("server", "view-owner", "observer"):
        for suffix in (".json", ".csv", ".snapshots.jsonl"):
            path = capture / (role + suffix)
            if path.exists():
                raw = path.read_bytes()
                output = path.name + ".gz"
                (evidence / output).write_bytes(gzip.compress(raw, mtime=0))
                archives.append(dict(path=output, rawSha256=hashlib.sha256(raw).hexdigest(), rawBytes=len(raw)))
    result = dict(passed=True, roles=roles, exactMatchedSnapshots=counts, firstReceivedTicks=first_ticks, independentlyCheckedDisplayFrames=checked,
                  maximumPositionDifferenceM=max_position, maximumQuaternionComponentDifference=max_quaternion, maximumFloatAnchorDifferenceM=max_anchor,
                  callbackMaximum=sorted(worst, key=lambda row: row["callbackMs"], reverse=True)[:10],
                  intervalsOver16_667Ms=sum(value > 1000/60 for value in intervals), intervalsOver33_333Ms=sum(value > 1000/30 for value in intervals),
                  archives=archives, limits="One rendered 1080p engineering runway client and one headless observer; synthetic Input System devices, no native OS key/gamepad hardware, airborne/water/WAN/capacity or representative 60 FPS acceptance.")
    (evidence / "verification.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(dict(passed=True, snapshots=counts, displayFrames=checked, maxPositionErrorM=max_position,
                          maximumCallback=result["callbackMaximum"][0], averageFps=metrics["averageFps"])))


def main():
    """Select raw actual-player traces, a fresh publication directory and the actual chase screenshot."""
    parser = argparse.ArgumentParser(description=__doc__)
    source=parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--capture", type=Path)
    source.add_argument("--archive", type=Path)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--image", type=Path, required=True)
    args = parser.parse_args()
    if args.capture:
        verify(args.capture, args.evidence, args.image)
    else:
        assert args.archive.resolve()!=args.evidence.resolve(), "Use a fresh replay output, not the published source."
        original=read(args.archive/"verification.json")
        process=read(args.archive/"process-summary.json")
        with tempfile.TemporaryDirectory(prefix="fireline-view-check-") as folder:
            capture=Path(folder)
            for item in original["archives"]:
                name=Path(item["path"])
                assert name.name==str(name) and name.suffix==".gz"
                raw=gzip.decompress((args.archive/name).read_bytes())
                assert len(raw)==item["rawBytes"] and hashlib.sha256(raw).hexdigest()==item["rawSha256"]
                (capture/name.stem).write_bytes(raw)
            for name in ("metrics.json","metrics.csv"):
                (capture/name).write_bytes((args.archive/name).read_bytes())
            for item in process["runs"]:
                assert item["exitCode"]==0
                assert hashlib.sha256((capture/(item["role"]+".json")).read_bytes()).hexdigest()==item["resultSha256"].lower()
            verify(capture,args.evidence,args.image)


if __name__ == "__main__":
    main()
