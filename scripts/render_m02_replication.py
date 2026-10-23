"""Render actual M02 process boundaries and analyze measured callback intervals without altering historical M00 evidence."""
import json
import csv
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch
from matplotlib.ticker import MultipleLocator, MaxNLocator
from analyze_frame_intervals import analyze

OUT = Path("docs/reports/m02-evidence")


def save(fig, name):
    """Export editable SVG and a PNG review copy, strip SVG trailing spaces and release the figure."""
    path = OUT / (name + ".svg")
    fig.savefig(path, metadata={"Date": None})
    path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")
    fig.savefig(OUT / (name + ".png"), dpi=120)
    plt.close(fig)


def diagram(after):
    """Draw the real earlier handshake-only path or the implemented replica/checkpoint/repair path; no proposed gameplay is shown."""
    fig, axis = plt.subplots(figsize=(12, 4 if after else 2.6), layout="constrained")
    axis.set(xlim=(0, 12), ylim=(0, 4 if after else 2.6)); axis.axis("off")
    axis.set_title("AFTER: M02 authoritative replication and local restart" if after else "BEFORE at 754178e: fire/checkpoint helpers without live replication", fontsize=13)
    labels = ["Server-owned 5 Hz world\nWind / water / char", "Per-peer acknowledged state\nBaseline or revision delta", "Reliable RPC fragments\nBounded 480-char parts", "Read-only client replica\nValidate / display / ACK"] if after else ["Server fire +\nlocal checkpoint helpers", "Netcode connection\nhandshake only", "Client calibration scene\nNo received fire state"]
    width = 12 / len(labels)
    for n, label in enumerate(labels):
        x = n * width + .1
        axis.add_patch(FancyBboxPatch((x, 1.5 if after else .8), width - .5, 1, boxstyle="round,pad=.04", facecolor="#edf6fb", edgecolor="#2676a3"))
        axis.text(x + (width - .5) / 2, 2 if after else 1.3, label, ha="center", va="center")
        if n < len(labels) - 1:
            axis.add_patch(FancyArrowPatch((x + width - .42, 2 if after else 1.3), (x + width - .15, 2 if after else 1.3), arrowstyle="-|>", mutation_scale=14))
    if after:
        axis.add_patch(FancyBboxPatch((.1, .15), 2.5, .8, boxstyle="round,pad=.04", facecolor="#edf6fb", edgecolor="#2676a3"))
        axis.text(1.35, .55, "Flushed local checkpoint\nExplicit load on fresh process", ha="center", va="center")
        axis.add_patch(FancyArrowPatch((.7, 1.4), (.7, 1.02), arrowstyle="-|>", mutation_scale=14))
        axis.add_patch(FancyArrowPatch((2, 1.02), (2, 1.4), arrowstyle="-|>", mutation_scale=14))
        axis.add_patch(FancyArrowPatch((10.4, 1.4), (4.4, 1.4), connectionstyle="bar,fraction=-.11", arrowstyle="-|>", mutation_scale=14, color="#2676a3"))
        axis.text(7.3, .35, "Exact-digest ACK / gap → baseline repair / 1 s heartbeat\nNo client fire mutation; diagnostic UI only; no M05A gameplay menus", ha="center", va="center")
    else:
        axis.text(6, .2, "Actual earlier implementation; no network fire baseline/delta or separate-process restart proof existed", ha="center")
    save(fig, "replication-after" if after else "replication-before")


def main():
    """Render architecture, independently verify available real callback CSVs and mark their worst measured overruns."""
    OUT.mkdir(parents=True, exist_ok=True)
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-m02-replication", "font.size": 9})
    diagram(False); diagram(True)
    metrics = sorted(OUT.glob("*/initial/*.metrics.json"))
    if not metrics:
        return
    summaries = []; fig, axes = plt.subplots(len(metrics), 2, figsize=(14, 2.7 * len(metrics)), layout="constrained", squeeze=False)
    for (axis, detail), path in zip(axes, metrics):
        rows, result = analyze(path.with_suffix(".csv"), path)
        with path.with_suffix('.marked.csv').open('w', newline='', encoding='utf-8') as output:
            writer = csv.DictWriter(output, fieldnames=list(rows[0])); writer.writeheader(); writer.writerows(rows)
        worst = max(rows, key=lambda row: row["frame_interval_ms"])
        summary = {"case": path.parent.parent.name + "/" + path.stem, "source": path.as_posix(),
                   **result["recomputedMetrics"], "worst": worst, "strictThresholdCounts": result["strictThresholdCounts"]}
        summaries.append(summary)
        for panel in (axis, detail):
            panel.plot([r["interval_end_s"] for r in rows], [r["frame_interval_ms"] for r in rows], lw=.7)
            panel.axhline(1000 / 60, color="#a66a00", ls="--", label="60 Hz spacing: 16.667 ms")
            panel.set(xlabel="Recording seconds (after 5 s wall warm-up)", ylabel="Callback interval (ms)", xlim=(0, rows[-1]['interval_end_s']))
            panel.xaxis.set_major_locator(MultipleLocator(5)); panel.grid(alpha=.2)
        axis.set_title(summary['case'] + ' / full range')
        axis.yaxis.set_major_locator(MaxNLocator(nbins=5)); axis.set_ylim(bottom=0)
        detail.set_title('Same samples / 14–26 ms detail; larger spikes cropped')
        detail.set_ylim(14, 26); detail.yaxis.set_major_locator(MultipleLocator(2))
        axis.annotate(f"#{worst['sample']}: {worst['frame_interval_ms']:.2f} ms", (worst["interval_end_s"], worst["frame_interval_ms"]),
                      xytext=(.55, .83), textcoords="axes fraction", arrowprops={"arrowstyle": "->"}, fontsize=8)
        axis.legend(fontsize=8, loc="upper left")
    fig.suptitle("M02: three concurrent diagnostic clients; callback spacing, not execution time or dropped presentations")
    save(fig, "callback-intervals")
    (OUT / "performance-analysis.json").write_text(json.dumps(summaries, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
