"""Locate callback-interval overruns without attributing CPU/GPU or presentation loss.

Core analysis uses only Python's standard library. --plot additionally needs matplotlib.
Run from the repository root; outputs are derived evidence, never runtime replacements.
"""

import argparse
import csv
import hashlib
import io
import json
import math
from pathlib import Path
import statistics


def analyze(csv_path, metrics_path):
    """Validate positive callback milliseconds against the recorded JSON, then return indexed interval/time/threshold rows and a summary. Cumulative recording time starts at the first stored interval; this does not attribute CPU/GPU causes or count dropped presentations."""
    source = csv_path.read_text(encoding="utf-8-sig")
    values = [float(row["frame_interval_ms"]) for row in csv.DictReader(io.StringIO(source))]
    if not values or any(not math.isfinite(value) or value <= 0 for value in values):
        raise ValueError("Expected nonempty, finite, positive callback intervals.")
    ordered = sorted(values)
    count = len(values)
    slow_count = math.ceil(count * 0.01)
    stats = {
        "samples": count,
        "sampledSeconds": sum(values) / 1000,
        "averageFps": 1000 / statistics.fmean(values),
        "onePercentLowFps": 1000 / statistics.fmean(ordered[-slow_count:]),
        "p95FrameMs": ordered[math.ceil(count * 0.95) - 1],
        "p99FrameMs": ordered[math.ceil(count * 0.99) - 1],
    }
    recorded = json.loads(metrics_path.read_text(encoding="utf-8-sig"))
    for key, value in stats.items():
        if not math.isclose(value, recorded[key], rel_tol=1e-10, abs_tol=1e-9):
            raise ValueError(f"CSV disagrees with recorded JSON: {key}={value}, expected {recorded[key]}")

    budget = 1000 / 60
    slow_indices = set(sorted(range(count), key=lambda index: (-values[index], index))[:slow_count])
    rows = []
    elapsed = 0.0
    for index, value in enumerate(values):
        end = elapsed + value / 1000
        rows.append({
            "sample": index + 1,
            "interval_start_s": elapsed,
            "interval_end_s": end,
            "frame_interval_ms": value,
            "over_60hz_budget": value > budget,
            "over_20ms": value > 20,
            "over_two_budgets": value > 2 * budget,
            "slowest_one_percent": index in slow_indices,
        })
        elapsed = end
    summary = {
        "sourceCsv": csv_path.as_posix(),
        "sourceMetrics": metrics_path.as_posix(),
        "normalizedCsvSha256": hashlib.sha256(source.encode("utf-8")).hexdigest(),
        "hashNormalization": "UTF-8 without BOM, universal newlines (LF)",
        "timeOrigin": "Start of first stored callback interval; absolute event timestamps unavailable",
        "frameBudgetMs": budget,
        "recomputedMetrics": stats,
        "verifiedAgainstRecordedMetrics": True,
        "strictThresholdCounts": {
            "over60hzBudget": sum(value > budget for value in values),
            "over18ms": sum(value > 18 for value in values),
            "over20ms": sum(value > 20 for value in values),
            "overTwoBudgets": sum(value > 2 * budget for value in values),
            "over50ms": sum(value > 50 for value in values),
        },
        "slowestOnePercent": sorted(rows, key=lambda row: (-row["frame_interval_ms"], row["sample"]))[:slow_count],
        "sensitivityExcludingFirstTwo": {
            "samples": count - 2,
            "averageFps": 1000 / statistics.fmean(values[2:]) if count > 2 else None,
            "maxFrameMs": max(values[2:]) if count > 2 else None,
            "interpretation": "Descriptive subset only; not a replacement benchmark or measured optimization",
        },
    }
    return rows, summary


def plot(rows, output):
    """Write timeline/detail and analysis-flow SVG/PNG drawings from real marked rows. Highlight measured overruns against 60 Hz spacing while leaving optimization and causal profiling explicitly deferred."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.patches import FancyArrowPatch, FancyBboxPatch

    # Keep labels as editable SVG text, with deterministic IDs/metadata.
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-m00", "font.size": 10})
    x = [row["interval_end_s"] for row in rows]
    y = [row["frame_interval_ms"] for row in rows]
    fig, axes = plt.subplots(2, 1, figsize=(11, 6.2), sharex=True, layout="constrained")
    fig.suptitle("M00 callback intervals: measured hitch locations (optimization deferred)", fontsize=14)
    for axis in axes:
        axis.plot(x, y, color="#2676a3", linewidth=0.8)
        axis.axhline(1000 / 60, color="#7a4b00", linestyle="--", linewidth=1, label="60 Hz budget: 16.667 ms")
        axis.set_ylabel("Callback interval (ms)")
        axis.grid(alpha=0.2)
        axis.set_xlim(0, max(x))
    axes[0].set_ylim(0, 96)
    axes[0].axhline(1000 / 30, color="#b22222", linestyle=":", linewidth=1, label="Two budgets: 33.333 ms")
    axes[0].legend(loc="upper right", framealpha=1)
    # These are measured overruns; no GPU or presentation timestamps exist.
    offsets = {1: (0.7, 41), 2: (1.3, 81), 216: (4.4, 50)}
    for row in rows:
        if row["over_20ms"]:
            t, value = row["interval_end_s"], row["frame_interval_ms"]
            axes[0].scatter(t, value, color="#b22222", zorder=3, s=25)
            axes[0].annotate(f"#{row['sample']}: {value:.2f} ms\ninterval ends at {t:.3f} s",
                             xy=(t, value), xytext=offsets.get(row["sample"], (t + 0.1, value + 8)),
                             arrowprops={"arrowstyle": "->", "color": "#b22222"}, fontsize=9)
    axes[1].set_ylim(14, 21)
    axes[1].set_title("Detail at 14–21 ms; longer intervals extend above this panel", fontsize=10)
    axes[1].set_xlabel("Seconds since start of first stored interval (not application startup)")
    for row in rows:
        if row["slowest_one_percent"] and row["sample"] > 2:
            axes[1].scatter(row["interval_end_s"], row["frame_interval_ms"], color="#b22222", s=18, zorder=3)
    fig.savefig(output / "frame-interval-timeline.svg", metadata={"Date": None})
    fig.savefig(output / "frame-interval-timeline.png", dpi=130, metadata={"Software": "Matplotlib"})
    plt.close(fig)

    # Render the actual analysis flow, rather than requiring a Markdown Mermaid renderer.
    fig, axis = plt.subplots(figsize=(11, 2.3), layout="constrained")
    axis.set_xlim(0, 11)
    axis.set_ylim(0, 2.3)
    axis.axis("off")
    axis.set_title("Implemented offline analysis; profiling and optimization remain deferred", fontsize=12)
    labels = [f"Raw callback CSV\n{len(rows)} intervals", "Verify summary\nagainst runtime JSON",
              "Mark sample index,\ntime and thresholds", "Document suspects\nwithout attribution"]
    for index, label in enumerate(labels):
        left = 0.1 + index * 2.8
        axis.add_patch(FancyBboxPatch((left, 0.65), 2.35, 0.85, boxstyle="round,pad=0.05",
                                     edgecolor="#2676a3", facecolor="#edf6fb"))
        axis.text(left + 1.175, 1.075, label, ha="center", va="center", fontsize=10)
        if index < len(labels) - 1:
            axis.add_patch(FancyArrowPatch((left + 2.4, 1.075), (left + 2.72, 1.075),
                                           arrowstyle="-|>", mutation_scale=14, color="#2676a3"))
    axis.text(5.5, 0.12, "No runtime changes • No new benchmark run • No proven CPU/GPU cause", ha="center", fontsize=10)
    fig.savefig(output / "analysis-flow.svg", metadata={"Date": None})
    fig.savefig(output / "analysis-flow.png", dpi=130, metadata={"Software": "Matplotlib"})
    plt.close(fig)
    # Matplotlib adds trailing spaces in SVG path attributes; newlines already separate coordinates.
    for name in ("frame-interval-timeline.svg", "analysis-flow.svg"):
        path = output / name
        path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines())
                        + "\n", encoding="utf-8", newline="\n")


def main():
    """Parse source/output paths, verify the historical recording, write marked CSV/summary, and optionally render plots. Outputs are derived evidence; no Unity run or original sample replacement occurs."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--csv", type=Path, default=Path("docs/reports/m00-evidence/client-diagnostic-metrics.csv"))
    parser.add_argument("--metrics", type=Path, default=Path("docs/reports/m00-evidence/client-diagnostic-metrics.json"))
    parser.add_argument("--output", type=Path, default=Path("docs/performance/m00-frame-loss"))
    parser.add_argument("--plot", action="store_true", help="Generate SVG plots and PNG review copies with matplotlib")
    args = parser.parse_args()
    rows, summary = analyze(args.csv, args.metrics)
    args.output.mkdir(parents=True, exist_ok=True)
    with (args.output / "marked-intervals.csv").open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    (args.output / "analysis-summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    if args.plot:
        plot(rows, args.output)
    print(f"Verified {len(rows)} intervals against runtime JSON; marked evidence saved to {args.output}")


if __name__ == "__main__":
    main()
