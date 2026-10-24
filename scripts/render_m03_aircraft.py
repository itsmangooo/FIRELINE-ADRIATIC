"""Render the actual prototype import flow and callback diagnostic from recorded build data."""
import csv
import json
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator

PREVIEW = Path("docs/previews/m03-cl415-prototype")
EVIDENCE = Path("docs/reports/m03-aircraft-evidence")


def flow(name, title, labels):
    """Draw and export a left-to-right implementation flow with fixed wrapping and arrow endpoints."""
    fig, ax = plt.subplots(figsize=(12, 2.7))
    ax.set(xlim=(0, 12), ylim=(0, 2.5))
    ax.axis("off")
    ax.set_title(title, fontsize=13)
    for index, label in enumerate(labels):
        x = 1.5 + index * 3
        ax.text(x, 1.2, label, ha="center", va="center", fontsize=10,
                bbox=dict(boxstyle="round,pad=0.7", facecolor="#e9f1f8", edgecolor="#334d68"))
        if index < len(labels) - 1:
            ax.annotate("", xy=(x + 1.85, 1.2), xytext=(x + 1.15, 1.2),
                        arrowprops=dict(arrowstyle="->", color="#334d68", lw=1.8))
    fig.tight_layout()
    fig.savefig(PREVIEW / f"{name}.svg", metadata={"Date": None})
    fig.savefig(PREVIEW / f"{name}.png", dpi=125)
    plt.close(fig)


def intervals():
    """Plot cumulative render-callback spacing, retain every interval, and mark the actual largest overrun."""
    source = EVIDENCE / "client-diagnostic-metrics.csv"
    with source.open(newline="") as stream:
        values = [float(row["frame_interval_ms"]) for row in csv.DictReader(stream)]
    assert values and all(value > 0 for value in values)
    elapsed = []
    total = 0
    for value in values:
        total += value / 1000
        elapsed.append(total)
    with (EVIDENCE / "callback-intervals.marked.csv").open("w", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["sample", "start_s", "end_s", "interval_ms", "over_16_667_ms", "over_33_333_ms"])
        for i, value in enumerate(values):
            writer.writerow([i + 1, elapsed[i] - value / 1000, elapsed[i], value,
                             value > 1000 / 60, value > 1000 / 30])
    peak = max(range(len(values)), key=values.__getitem__)
    fig, axes = plt.subplots(2, 1, figsize=(11, 7), sharex=True)
    for ax in axes:
        ax.plot(elapsed, values, color="#30587c", lw=0.7, label="Main-camera callback interval")
        ax.axhline(1000 / 60, color="#21874c", ls="--", lw=1, label="16.67 ms / 60 Hz ruler")
        ax.axhline(1000 / 30, color="#bc7414", ls=":", lw=1, label="33.33 ms / 30 Hz ruler")
        ax.grid(alpha=0.25)
        ax.set_ylabel("Callback interval (ms)")
        ax.xaxis.set_major_locator(MultipleLocator(5))
    axes[0].annotate(f"Maximum: #{peak + 1}\n{values[peak]:.3f} ms at {elapsed[peak]:.3f} s",
                     xy=(elapsed[peak], values[peak]), xytext=(0.30, 0.78), textcoords="axes fraction",
                     arrowprops=dict(arrowstyle="->"), fontsize=10)
    axes[0].set_title("CL-415 static preview: actual render-callback spacing; not CPU/GPU cost")
    axes[0].set_ylim(bottom=0)
    axes[0].legend(loc="upper right", fontsize=8)
    axes[1].set_ylim(0, 50)
    axes[1].yaxis.set_major_locator(MultipleLocator(10))
    axes[1].set_title("Same recording, 0–50 ms vertical zoom; larger intervals are clipped in this panel")
    axes[1].set_xlabel("Recorded elapsed time after five-second rendered warm-up (s)")
    fig.tight_layout()
    fig.savefig(PREVIEW / "callback-intervals.svg", metadata={"Date": None})
    fig.savefig(PREVIEW / "callback-intervals.png", dpi=125)
    full_y_ticks = axes[0].get_yticks().tolist()
    plt.close(fig)
    record = {"samples": len(values), "sampledSeconds": total,
              "above16_667ms": sum(v > 1000 / 60 for v in values),
              "above33_333ms": sum(v > 1000 / 30 for v in values),
              "maximumSample": peak + 1, "maximumMs": values[peak], "maximumRecordedSeconds": elapsed[peak],
              "plotGuide": {"xMajorSpacingSeconds": 5, "fullYMajorTicksMs": full_y_ticks,
                            "zoomYRangeMs": [0, 50], "zoomYMajorSpacingMs": 10},
              "largestIntervals": [{"sample": i + 1, "recordedSeconds": elapsed[i], "ms": values[i]}
                                   for i in sorted(range(len(values)), key=values.__getitem__, reverse=True)[:10]]}
    (EVIDENCE / "callback-analysis.json").write_text(json.dumps(record, indent=2) + "\n")


def main():
    """Generate editable-script-backed vector diagrams and quantitative plots without creating gameplay screenshots."""
    PREVIEW.mkdir(parents=True, exist_ok=True)
    plt.rcParams["svg.fonttype"] = "none"
    plt.rcParams["svg.hashsalt"] = "fireline-m03-aircraft"
    flow("before", "BEFORE — committed 0683cec: no aircraft visual implementation", [
        "Official archive\nDownloaded + audited", "Source outside Unity\nImport held", "No aircraft prefab\nNo visual binding", "Calibration clients\nNo CL-415 preview"])
    flow("after", "AFTER — user-authorized temporary prototype import", [
        "Audited exact source\nOBJ + MTL + 9 PNGs", "Unity OBJ import\n11 HDRP/Lit materials", "Static prefab + scene\nNo rig or flight binding", "Rendered client build\nCapture + raw intervals"])
    intervals()
    for path in PREVIEW.glob("*.svg"):
        path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
