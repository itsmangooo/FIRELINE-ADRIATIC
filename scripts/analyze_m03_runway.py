"""Independently verify and archive real dedicated-player runway trajectories, forces and raw CSV."""
import argparse
import csv
import gzip
import hashlib
import io
import json
import math
from pathlib import Path

OUT = Path("docs/reports/m03-runway-evidence")


def near(actual, expected, tolerance=1e-6):
    """Compare dimensional numeric values with a declared absolute tolerance and a small decimal-roundtrip allowance."""
    assert math.isfinite(actual) and math.isclose(actual, expected, abs_tol=tolerance, rel_tol=1e-12), (actual, expected)


def vector(value):
    """Read a serialized SI vector in X/Y/Z order."""
    return [value[k] for k in ("X", "Y", "Z")]


def cross(a, b):
    """Compute the physical lever cross force for the independent moment audit."""
    return [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]]


def matrix(q):
    """Build a body-to-world rotation matrix independently of the runtime's quaternion-vector shortcut."""
    x, y, z, w = [q[k] for k in ("X", "Y", "Z", "W")]
    near(x*x+y*y+z*z+w*w, 1, 1e-8)
    return [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
            [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
            [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]


def multiply(rotation, value):
    """Apply a rotation or its transpose using ordinary matrix multiplication."""
    return [sum(row[j]*value[j] for j in range(3)) for row in rotation]


def contacts(row, case):
    """Recompute wheel positions, point velocities, normal loads, moments, stored energy and Coulomb constraints."""
    state, c = row["state"], row["contact"]
    position, velocity, omega = [vector(state[k]) for k in ("PositionM", "VelocityMps", "AngularVelocityRadps")]
    rotation = matrix(state["Attitude"])
    inverse = list(zip(*rotation))
    mass = 10000 + state["FuelKg"] + state["WaterKg"]
    cg = (-.8*state["FuelKg"]+1.2*state["WaterKg"])/mass
    total, moment = [0.0]*3, [0.0]*3
    normal_sum = energy = power_sum = maximum = 0.0
    count = 0
    for index, (name, point) in enumerate(zip(("LeftMain", "RightMain", "Nose"), ((-3,-2,-3),(3,-2,-3),(0,-2,6)))):
        wheel = c[name]
        lever = [point[0], point[1], point[2]-cg]
        offset, angular = multiply(rotation, lever), multiply(rotation, cross(omega, lever))
        point_world = [position[j]+offset[j] for j in range(3)]
        point_velocity = [velocity[j]+angular[j] for j in range(3)]
        for actual, expected in zip(vector(wheel["PointM"]), point_world): near(actual, expected, 1e-9)
        for actual, expected in zip(vector(wheel["VelocityMps"]), point_velocity): near(actual, expected, 1e-9)
        for actual, expected in zip(vector(wheel["BodyLeverM"]), lever): near(actual, expected, 1e-12)
        inside = abs(point_world[0]) <= 22.5 and -500 <= point_world[2] <= 2500
        assert wheel["OnRunway"] == inside
        compression = max(0, -point_world[1]) if inside else 0
        normal = max(0, min(250000, 300000*compression-50000*point_velocity[1])) if inside and point_world[1] <= 0 else 0
        near(wheel["NormalN"], normal)
        near(wheel["CompressionM"], compression, 1e-9)
        assert compression <= .75+1e-12
        world_force = vector(wheel["WorldN"])
        friction = [world_force[0], world_force[1]-wheel["NormalN"], world_force[2]]
        power = sum(friction[j]*point_velocity[j] for j in range(3))
        near(wheel["FrictionPowerW"], power)
        assert power <= 1e-6
        if normal > 0:
            mu = .02 + .58*(case["brake"] if index < 2 else 0)
            ellipse = (wheel["LongitudinalN"]/(mu*normal))**2 + (wheel["LateralN"]/(.65*normal))**2
            assert ellipse <= 1+1e-10
            near(wheel["FrictionEllipse"], math.sqrt(ellipse), 1e-10)
            steer = case["input"]["Yaw"]*.35 if index == 2 else 0
            forward = multiply(rotation, [math.sin(steer), 0, math.cos(steer)])
            forward[1] = 0
            length = math.sqrt(sum(v*v for v in forward))
            forward = [v/length for v in forward]; right = cross([0,1,0], forward)
            for j in range(3):
                near(world_force[j], wheel["LongitudinalN"]*forward[j]+wheel["LateralN"]*right[j]+(wheel["NormalN"] if j == 1 else 0))
            near(world_force[1], normal)
            count += 1
        else:
            assert all(value == 0 for value in world_force)
        torque = cross(lever, multiply(inverse, world_force))
        total = [total[j]+world_force[j] for j in range(3)]
        moment = [moment[j]+torque[j] for j in range(3)]
        normal_sum += normal; power_sum += power
        energy += .5*300000*compression*compression
        maximum = max(maximum, compression)
    for key, expected in (("WorldN", total), ("BodyMomentNm", moment)):
        for actual, value in zip(vector(c[key]), expected): near(actual, value)
    near(c["NormalN"], normal_sum); near(c["FrictionPowerW"], power_sum); near(c["SpringEnergyJ"], energy)
    near(c["MaximumCompressionM"], maximum, 1e-9)
    assert c["SupportedWheels"] == count


def verify_run(run, csv_data):
    """Check every state/force, raw CSV agreement and equivalent-input physical acceptance case without using runtime summaries."""
    assert run["passed"] and run["headless"] and run["profile"] == "DEBUG_RUNWAY_CONTACT_V1"
    cases = {case["name"]: case for case in run["cases"]}
    assert len(cases) == 13
    rows = list(csv.DictReader(io.StringIO(csv_data.decode("utf-8-sig"))))
    expected_rows = [(case, row) for case in run["cases"] for row in case["samples"]]
    assert len(rows) == len(expected_rows)
    for csv_row, (case, row) in zip(rows, expected_rows):
        state = row["state"]; matrix(state["Attitude"])
        for key, maximum in (("PositionM", 1e7), ("VelocityMps", 500), ("AngularVelocityRadps", 10)):
            values = vector(state[key])
            assert all(math.isfinite(v) for v in values) and math.sqrt(sum(v*v for v in values)) <= maximum
        assert 0 <= state["FuelKg"] <= 1000 and 0 <= state["WaterKg"] <= 4000
        assert 10000+state["FuelKg"]+state["WaterKg"] <= 16000
        for key in ("FuelKg", "WaterKg"): assert state[key] == case["initial"][key]
        assert csv_row["case"] == case["name"]
        near(float(csv_row["seconds"]), row["seconds"], 1e-12)
        near(float(csv_row["height_m"]), state["PositionM"]["Y"], 1e-10)
        near(float(csv_row["normal_n"]), row["contact"]["NormalN"])
        for prefix, key in (("", "PositionM"), ("v", "VelocityMps")):
            for axis, field in (("x", "X"), ("y", "Y"), ("z", "Z")):
                column = ("height_m" if axis == "y" else axis+"_m") if not prefix else prefix+axis+"_mps"
                near(float(csv_row[column]), state[key][field], 1e-10)
        for axis in ("x", "y", "z", "w"): near(float(csv_row["q"+axis]), state["Attitude"][axis.upper()], 1e-12)
        near(float(csv_row["compression_m"]), row["contact"]["MaximumCompressionM"], 1e-10)
        near(float(csv_row["friction_power_w"]), row["contact"]["FrictionPowerW"])
        assert int(csv_row["supported_wheels"]) == row["contact"]["SupportedWheels"]
        if not case["before"]: contacts(row, case)
        if case["name"] in ("before_rest", "before_brake"):
            t = row["seconds"]; initial = case["initial"]
            near(state["PositionM"]["Y"], initial["PositionM"]["Y"] + initial["VelocityMps"]["Y"]*t - .5*9.80665*(t*t+case["dt"]*t), 1e-8)
            near(state["PositionM"]["Z"], initial["VelocityMps"]["Z"]*t, 1e-8)
    for name in ("rest", "rest_fine", "loaded", "touchdown", "analytic_static"):
        last = cases[name]["samples"][-1]; state = last["state"]
        assert 1.7 < state["PositionM"]["Y"] < 2
        assert math.sqrt(sum(value*value for value in vector(state["VelocityMps"]))) < 1e-5
        near(last["contact"]["NormalN"], (10000+state["FuelKg"]+state["WaterKg"])*9.80665, .1)
    assert cases["loaded"]["samples"][-1]["state"]["PositionM"]["Y"] < cases["rest"]["samples"][-1]["state"]["PositionM"]["Y"]
    near(cases["rest"]["samples"][-1]["state"]["PositionM"]["Y"], cases["rest_fine"]["samples"][-1]["state"]["PositionM"]["Y"], 1e-5)
    brake, coast = [cases[name]["samples"][-1]["state"] for name in ("brake", "coast")]
    assert abs(brake["VelocityMps"]["Z"]) < .2 and brake["PositionM"]["Z"] < coast["PositionM"]["Z"]
    for before, after in zip(cases["airborne_before"]["samples"], cases["airborne_after"]["samples"]):
        assert before["state"] == after["state"] and after["contact"]["NormalN"] == 0
    assert abs(cases["lateral"]["samples"][-1]["state"]["VelocityMps"]["X"]) < .01
    return cases, len(rows)


def main():
    """Archive fresh process output or recheck published lossless artifacts, then derive measured case and timing summaries."""
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument("--capture", type=Path)
    parser.add_argument("--evidence", type=Path, default=OUT); args = parser.parse_args(); out = args.evidence
    summary = json.loads((out/"process-summary.json").read_text(encoding="utf-8-sig"))
    runs = []
    for record in summary["runs"]:
        assert record["exitCode"] == 0
        files = {}
        for suffix, field in ((".json", "resultSha256"), (".csv", "csvSha256")):
            archive = out/f"run-{record['run']}{suffix}.gz"
            if args.capture: archive.write_bytes(gzip.compress((args.capture/f"run-{record['run']}{suffix}").read_bytes(), mtime=0))
            data = gzip.decompress(archive.read_bytes())
            assert hashlib.sha256(data).hexdigest().upper() == record[field]
            files[suffix] = data
        run = json.loads(files[".json"]); cases, count = verify_run(run, files[".csv"])
        runs.append(run)
    assert runs[0]["cases"] == runs[1]["cases"]
    physical = hashlib.sha256(json.dumps(runs[0]["cases"], sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    metrics = []
    for case in cases.values():
        last = case["samples"][-1]["state"]
        stop = next((row for row in case["samples"] if abs(row["state"]["VelocityMps"]["Z"]) < .2), None)
        metrics.append({"case": case["name"], "samples": len(case["samples"]), "dtSeconds": case["dt"],
                        "finalHeightM": last["PositionM"]["Y"], "finalNorthM": last["PositionM"]["Z"],
                        "finalForwardMps": last["VelocityMps"]["Z"], "finalLateralMps": last["VelocityMps"]["X"],
                        "maximumCompressionM": max(row["contact"]["MaximumCompressionM"] for row in case["samples"]),
                        "firstBelowPoint2MpsSeconds": stop["seconds"] if stop else None})
    timing = []
    for run in runs:
        values = run["stepMs"]; ordered = sorted(values)
        assert len(values) == 1000 and all(math.isfinite(v) and v >= 0 for v in values)
        timing.append({"warmups": run["warmupCalls"], "calls": len(values), "p50Ms": ordered[499], "p95Ms": ordered[949],
                       "p99Ms": ordered[989], "maxMs": max(values), "maximumRetainedCall": values.index(max(values))+1})
    result = {"passed": True, "replayExact": True, "physicalSha256": physical, "samplesPerRun": count,
              "tolerances": {"forcesNmWJ": 1e-6, "positionsM": 1e-9, "csvHeightM": 1e-10, "restSupportN": .1},
              "cases": metrics, "timing": timing}
    (out/"analysis.json").write_text(json.dumps(result, indent=2)+"\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
