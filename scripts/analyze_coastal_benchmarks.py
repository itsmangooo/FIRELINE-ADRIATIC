"""Verify completed coastal benchmark CSV/JSON and render annotated measured timelines."""
import argparse
import csv
import json
import math
from pathlib import Path
import statistics


def percentile(values, fraction):
    """Return the nearest-rank percentile using ceil(count * fraction), or None for no available samples."""
    return sorted(values)[math.ceil(len(values) * fraction) - 1] if values else None


def analyze(path):
    """Read paired runtime JSON/CSV, verify interval-derived metrics, and return raw rows with worst-interval locations and available-counter percentiles. Blank counters remain unavailable, not zero."""
    recorded = json.loads(path.read_text(encoding="utf-8-sig"))
    rows = list(csv.DictReader(path.with_suffix(".csv").open(encoding="utf-8-sig")))
    intervals = [float(row["callback_interval_ms"]) for row in rows]
    assert intervals and all(math.isfinite(value) and value > 0 for value in intervals)
    low = sorted(intervals)[-math.ceil(len(intervals) * 0.01):]
    expected = {"samples": len(rows), "seconds": sum(intervals) / 1000,
                "averageFps": 1000 / statistics.fmean(intervals), "onePercentLowFps": 1000 / statistics.fmean(low),
                "p95FrameMs": percentile(intervals, 0.95), "p99FrameMs": percentile(intervals, 0.99)}
    for key, value in expected.items():
        assert math.isclose(value, recorded[key], rel_tol=1e-9, abs_tol=1e-7), (path, key)
    worst = max(range(len(rows)), key=lambda index: intervals[index])
    result = {"name": path.stem, "source": path.as_posix(), **expected,
              "worstSample": worst + 1, "worstEndSeconds": float(rows[worst]["end_s"]),
              "worstStartSeconds": float(rows[worst]["end_s"]) - intervals[worst] / 1000,
              "worstIntervalMs": intervals[worst], "over20ms": sum(x > 20 for x in intervals),
              "overTwoBudgets": sum(x > 1000 / 30 for x in intervals),
              "width": recorded["width"], "height": recorded["height"], "quality": recorded["quality"],
              "scenario": recorded["scenario"]}
    for key in ("cpu_frame_ms", "cpu_main_ms", "gpu_frame_ms", "draw_calls", "unity_allocated_bytes"):
        values = [float(row[key]) for row in rows if row[key] and float(row[key]) > 0]
        result[key] = {"availableSamples": len(values), "p50": percentile(values, 0.5),
                       "p95": percentile(values, 0.95), "p99": percentile(values, 0.99),
                       "max": max(values) if values else None}
    return rows, result


def main():
    """Find completed clear/smoky recordings, verify them, then write summaries, overrun rows and labeled SVG/PNG timelines in that directory. This analyzes existing data and does not establish a measured optimization."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    paths = sorted(p for p in args.directory.glob("*.json") if p.stem.endswith(("-clear", "-smoky")))
    assert paths, "No completed benchmark JSON files"
    results = [analyze(path) for path in paths]
    (args.directory / "analysis.json").write_text(json.dumps([r for _, r in results], indent=2) + "\n", encoding="utf-8")
    with (args.directory / "marked-overruns.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.writer(output)
        writer.writerow(["case", "sample", "start_s", "end_s", "interval_ms", "over_two_budgets"])
        for rows, result in results:
            for index, row in enumerate(rows, start=1):
                interval = float(row["callback_interval_ms"])
                if interval > 20:
                    end = float(row["end_s"])
                    writer.writerow([result["name"], index, end - interval / 1000, end, interval, interval > 1000 / 30])
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.ticker import MultipleLocator
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-coastal-timing", "font.size": 10})
    fig, axes = plt.subplots(len(results), 1, figsize=(11, 3 * len(results)), squeeze=False, layout="constrained")
    for axis, (rows, result) in zip(axes[:, 0], results):
        x = [float(row["end_s"]) for row in rows]
        y = [float(row["callback_interval_ms"]) for row in rows]
        axis.plot(x, y, linewidth=0.65, color="#2676a3")
        axis.axhline(1000 / 60, color="#7a4b00", linestyle="--", linewidth=1, label="60 Hz target spacing: 16.667 ms")
        axis.set_title(f"{result['quality']} / {result['scenario']}: {result['averageFps']:.2f} FPS avg, "
                       f"{result['onePercentLowFps']:.2f} FPS 1% low")
        axis.set_xlabel("Elapsed recording time (s; after 30 rendered seconds of warm-up)")
        axis.set_ylabel("Render-callback interval (ms)")
        axis.set_xlim(0, max(x))
        maximum = max(25, max(y) * 1.3)
        axis.set_ylim(0, maximum)
        axis.yaxis.set_major_locator(MultipleLocator(5 if maximum <= 40 else 20))
        axis.xaxis.set_major_locator(MultipleLocator(60))
        axis.grid(alpha=0.2)
        axis.legend(loc="upper right", fontsize=8)
        index = result["worstSample"] - 1
        axis.scatter(x[index], y[index], color="#b22222", s=22, zorder=3)
        axis.annotate(f"#{index + 1}: {y[index]:.2f} ms at {x[index]:.2f} s", xy=(x[index], y[index]),
                      xytext=(0.05, 0.85), textcoords="axes fraction", fontsize=9,
                      arrowprops={"arrowstyle": "->", "color": "#b22222"})
    fig.suptitle("M00 initial coastal proxy workload: measured intervals, no optimization comparison", fontsize=13)
    svg = args.directory / "frame-intervals.svg"
    fig.savefig(svg, metadata={"Date": None})
    svg.write_text("\n".join(line.rstrip() for line in svg.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")
    fig.savefig(args.directory / "frame-intervals.png", dpi=120)
    plt.close(fig)
    print(json.dumps([{"case": r["name"], "samples": r["samples"], "seconds": r["seconds"],
                       "averageFps": r["averageFps"], "onePercentLowFps": r["onePercentLowFps"],
                       "p95FrameMs": r["p95FrameMs"], "worstMs": r["worstIntervalMs"]} for _, r in results], indent=2))


if __name__ == "__main__":
    main()
