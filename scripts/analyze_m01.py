"""Independently verify M01 final-state accounting/digests and draw actual numerical evidence."""
import csv
import json
import math
from pathlib import Path
import struct

ROOT = Path("docs/reports/m01-evidence")
OUT = Path("docs/physics/fire-wind")


def digest(snapshot):
    """Reconstruct the C# sorted FNV regression digest from serialized integer/double bits. This independent check detects representation changes; it is not a cryptographic signature or a promise of cross-OS bit identity."""
    value = 14695981039346656037

    def add(number):
        """Mix one masked 64-bit integer as little-endian bytes into the enclosing FNV accumulator."""
        nonlocal value
        for byte in struct.pack("<Q", number & ((1 << 64) - 1)):
            value = ((value ^ byte) * 1099511628211) & ((1 << 64) - 1)

    def number(n):
        """Reinterpret a Python double as its IEEE-754 64-bit pattern before hashing, preserving exact snapshot precision."""
        add(struct.unpack("<Q", struct.pack("<d", n))[0])

    add(snapshot["Tick"]); add(snapshot["Seed"]); add(snapshot["Size"]); number(snapshot["CellSizeM"])
    wind = snapshot["Wind"]
    add(wind["Revision"])
    for field in ("EastMps", "NorthMps", "GustMps"): number(wind[field])
    add(snapshot["InactiveCursor"])
    for field in ("AppliedWaterKg", "EvaporatedWaterKg", "ConsumedFuelKg"): number(snapshot[field])
    for chunk in snapshot["Chunks"]:
        for field in ("Sector", "X", "Z"): add(chunk["Id"][field])
        add(chunk["Revision"]); add(chunk["LastSimulatedTick"]); add(int(chunk["Active"]))
        for cell in chunk["Cells"]:
            add(cell["Fuel"])
            for field in ("InitialFuelKgPerM2", "RemainingFuelKgPerM2", "Moisture", "WaterKgPerM2", "Heat", "Intensity", "Smoke", "ElevationM"):
                assert math.isfinite(cell[field]), field
                number(cell[field])
            for field in ("Phase", "Vegetation", "VegetationId", "LastChangedTick"): add(cell[field])
    return f"{value:016x}"


def main():
    """Verify nine Windows cases, raw row counts, digests and kg/m2 accounting; write the analysis and actual model/area/timing/grid figures. These are offline numerical plots, not Unity screenshots or client FPS results."""
    report = json.loads((ROOT / "simulation.json").read_text(encoding="utf-8-sig"))
    rows = list(csv.DictReader((ROOT / "simulation.csv").open(encoding="utf-8-sig")))
    assert report["passed"] and len(rows) == 5400 and len(report["cases"]) == 9
    results = []
    for case in report["cases"]:
        state = case["finalState"]; cells = state["Chunks"][0]["Cells"]; last = case["samples"][-1]
        area = state["CellSizeM"] ** 2
        burned = sum(c["Phase"] >= 2 for c in cells) * area
        retained = sum(c["WaterKgPerM2"] for c in cells) * area
        consumed = sum(c["InitialFuelKgPerM2"] - c["RemainingFuelKgPerM2"] for c in cells) * area
        assert len(cells) == 1024 and state["Tick"] == 600 and len(case["samples"]) == 600
        assert digest(state) == case["checksum"] == state["Checksum"], case["scenario"]
        for actual, expected in ((burned, last["burnedAreaM2"]), (consumed, state["ConsumedFuelKg"]),
                                 (retained + state["EvaporatedWaterKg"], state["AppliedWaterKg"])):
            assert math.isclose(actual, expected, abs_tol=1e-6, rel_tol=1e-10), (case["scenario"], actual, expected)
        selected = [r for r in rows if r["scenario"] == case["scenario"]]
        assert len(selected) == 600
        for row, sample in zip(selected, case["samples"]):
            assert int(row["tick"]) == sample["tick"]
            assert math.isclose(float(row["burned_area_m2"]), sample["burnedAreaM2"], abs_tol=1e-9)
            assert math.isclose(float(row["step_ms"]), sample["stepMs"], abs_tol=1e-9)
        times = sorted(s["stepMs"] for s in case["samples"])
        results.append({"scenario": case["scenario"], "checksum": case["checksum"], **last,
                        "stepP95Ms": times[math.ceil(len(times) * 0.95) - 1], "stepMaxMs": max(times)})
    times = sorted(report["budgetStepMs"])
    analysis = {"cases": results, "budgetP95Ms": times[math.ceil(len(times) * 0.95) - 1],
                "budgetMaxMs": max(times), "budgetSamples": len(times),
                "budgetMaximumVisitedCells": report["budgetMaximumVisitedCells"],
                "budgetMaximumBacklogSeconds": report["budgetMaximumBacklogSeconds"],
                "budgetGc0Collections": report["budgetGc0Collections"]}
    (ROOT / "analysis.json").write_text(json.dumps(analysis, indent=2) + "\n", encoding="utf-8")

    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
    from matplotlib.ticker import MultipleLocator
    import numpy as np
    OUT.mkdir(parents=True, exist_ok=True)
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-m01", "font.size": 10})

    def save(fig, name):
        """Write the current figure as editable SVG and PNG, remove trailing SVG whitespace, and close it to release plotting memory."""
        path = OUT / f"{name}.svg"
        fig.savefig(path, metadata={"Date": None})
        path.write_text("\n".join(l.rstrip() for l in path.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")
        fig.savefig(OUT / f"{name}.png", dpi=115)
        plt.close(fig)

    for name, labels, footer in (
        ("before", ["M00 server\n60 Hz clock + connection", "No fire/wind/fuel model", "No suppression or\nchar/hazard state"], "BEFORE: actual foundation at 46d179c; numerical fire baseline unavailable"),
        ("after", ["Server-only manual\nignition / wind / water", "5 Hz bounded scheduler\nread previous buffers", "Heat / burn / fuel /\nwater / smoke / char", "Commit revision +\nsnapshot / digest"], "AFTER: implemented M01; client protocol, restart storage and VFX remain later work")):
        fig, axis = plt.subplots(figsize=(12, 2.4), layout="constrained")
        axis.set(xlim=(0, 12), ylim=(0, 2.4)); axis.axis("off")
        width = 12 / len(labels)
        for i, label in enumerate(labels):
            x = i * width + 0.1
            axis.add_patch(FancyBboxPatch((x, 0.8), width - 0.5, 1.1, boxstyle="round,pad=.04", facecolor="#edf6fb", edgecolor="#2676a3"))
            axis.text(x + (width - 0.5) / 2, 1.35, label, ha="center", va="center")
            if i < len(labels) - 1: axis.add_patch(FancyArrowPatch((x + width - .42, 1.35), (x + width - .15, 1.35), arrowstyle="-|>", mutation_scale=13))
        axis.text(6, .2, footer, ha="center", fontsize=10); save(fig, name)

    chosen = {c["scenario"]: c for c in report["cases"]}
    fig, axes = plt.subplots(1, 2, figsize=(12, 4), layout="constrained")
    for name in ("calm", "east", "rotated", "suppressed", "moist"):
        samples = chosen[name]["samples"]
        axes[0].plot([s["seconds"] for s in samples], [s["burnedAreaM2"] for s in samples], label=name)
    axes[0].set(xlabel="Simulation time (s; fixed step 0.2 s)", ylabel="Ever-ignited cell area (m²)", title="Actual synthetic-grid scenarios")
    axes[0].xaxis.set_major_locator(MultipleLocator(30)); axes[0].grid(alpha=.2); axes[0].legend(fontsize=8)
    axes[0].yaxis.set_major_locator(MultipleLocator(20000))
    axes[1].plot([.2 * (i + 1) for i in range(len(report["budgetStepMs"]))], report["budgetStepMs"], linewidth=.8)
    axes[1].axhline(analysis["budgetP95Ms"], color="#a44", linestyle="--", label=f"p95 {analysis['budgetP95Ms']:.3f} ms")
    axes[1].set(xlabel="Simulation time (s; accelerated 120 steps)", ylabel="Measured World.Step CPU duration (ms)",
                title="16 chunks; ≤5120 cells visited per step\n5 Hz deadline: 200 ms (above the shown range)")
    axes[1].xaxis.set_major_locator(MultipleLocator(6)); axes[1].set_ylim(bottom=0); axes[1].grid(alpha=.2); axes[1].legend(fontsize=8)
    axes[1].yaxis.set_major_locator(MultipleLocator(2))
    save(fig, "measurements")

    fig, axes = plt.subplots(1, 4, figsize=(12, 3.5), layout="constrained")
    for axis, name in zip(axes, ("calm", "east", "rotated", "suppressed")):
        values = np.array([c["Phase"] for c in chosen[name]["finalState"]["Chunks"][0]["Cells"]]).reshape(32, 32)
        im = axis.imshow(values, origin="lower", extent=(-165, 155, -165, 155), vmin=0, vmax=5, cmap="viridis", interpolation="nearest")
        axis.set(title=name, xlabel="East relative to ignition (m)", ylabel="North (m)")
        axis.xaxis.set_major_locator(MultipleLocator(100)); axis.yaxis.set_major_locator(MultipleLocator(100))
    bar = fig.colorbar(im, ax=axes, ticks=list(range(6)), shrink=.8)
    bar.ax.set_yticklabels(["unburned", "heating", "burning", "smoldering", "extinguished", "charred"])
    fig.suptitle("Actual authoritative cell phases at 120 simulated seconds; numerical plots, not Unity screenshots")
    save(fig, "final-grids")
    print(json.dumps(analysis, indent=2))


if __name__ == "__main__":
    main()
