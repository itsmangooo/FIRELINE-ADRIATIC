"""Render actual presentation boundaries and measured rendered callback/device traces as self-contained vectors."""
import argparse
import csv
import gzip
import io
import json
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator


def save(fig, output, name):
    """Keep inspectable PNG fallbacks and stable self-contained SVG exports without whitespace-only diffs."""
    fig.tight_layout()
    fig.savefig(output / f"{name}.svg", metadata={"Date": None})
    fig.savefig(output / f"{name}.png", dpi=125)
    path = output / f"{name}.svg"
    path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines()) + "\n",encoding="utf-8")
    plt.close(fig)


def diagram(output, after):
    """Draw the actual previous data-only path or the newly connected input/view path, never a proposed cockpit/flight loop."""
    fig, ax = plt.subplots(figsize=(12, 3.1))
    ax.set(xlim=(0, 12), ylim=(0, 3)); ax.axis("off")
    ax.set_title("AFTER: engineering controls + delayed authoritative model view" if after else "BEFORE: 3035aea — live data, separate static aircraft preview", fontsize=12)
    labels = (["Keyboard / gamepad\nFocus + seat input gate", "Server validates + steps\nComplete SI poses / ACK", "100 ms pose interpolation\nCoG → explicit mesh offset", "External cameras + HUD\nNo client integration"] if after else
              ["Server aircraft authority\nValidated controls / poses", "Read-only replica data\nScripted network proof", "Static preview model\nNo input / pose binding", "No flight view / telemetry\nNo local prediction"])
    for index, label in enumerate(labels):
        x = 1.5 + index * 3
        ax.text(x, 1.5, label, ha="center", va="center", fontsize=9,
                bbox=dict(boxstyle="round,pad=.65", facecolor="#e8f1f8", edgecolor="#38526b"))
        if index < 3 and (after or index == 0):
            ax.annotate("", xy=(x + 1.8, 1.5), xytext=(x + 1.15, 1.5), arrowprops=dict(arrowstyle="->", lw=1.8))
    save(fig, output, "after-flow" if after else "before-flow")


def timing(evidence, output, initial=False):
    """Show all actual callback intervals and a labeled zoom, keeping large hitches visible in the full panel."""
    if initial:
        raw=gzip.decompress((evidence / "rejected-attempt-callbacks.marked.csv.gz").read_bytes()).decode()
        rows=list(csv.DictReader(io.StringIO(raw)))
    else:
        rows = list(csv.DictReader((evidence / "callback-intervals.marked.csv").open()))
    x = [float(row["end_s"]) for row in rows]; y = [float(row["interval_ms"]) for row in rows]
    peak = max(range(len(y)), key=y.__getitem__)
    fig, axes = plt.subplots(2, 1, figsize=(11, 6.4), sharex=True)
    for ax in axes:
        ax.plot(x, y, lw=.75)
        ax.axhline(1000 / 60, ls="--", color="#3b8666", label="16.667 ms / 60 Hz ruler")
        ax.axhline(1000 / 30, ls=":", color="#aa5b44", label="33.333 ms ruler")
        ax.xaxis.set_major_locator(MultipleLocator(5)); ax.grid(alpha=.25); ax.set_ylabel("Callback interval (ms)")
    title="Initial attempt: view-trace gate rejected; actual callback data retained" if initial else "Accepted 1080p rendered client callbacks after five rendered seconds warm-up"
    axes[0].set(title=title, ylim=(0, max(50, max(y) * 1.1)))
    full_step = 10 if max(y) < 100 else 25 if max(y) < 250 else 100 if max(y) < 1000 else 250
    axes[0].yaxis.set_major_locator(MultipleLocator(full_step))
    axes[0].annotate(f"#{peak + 1}: {y[peak]:.3f} ms", xy=(x[peak], y[peak]), xytext=(.48, .8), textcoords="axes fraction", arrowprops=dict(arrowstyle="->"))
    axes[0].legend(fontsize=8)
    axes[1].set(title="Same raw intervals, 0–50 ms zoom; full panel retains larger spikes", ylim=(0, 50), xlabel="Seconds since first retained interval start (near warm-up boundary)")
    axes[1].yaxis.set_major_locator(MultipleLocator(10))
    name="initial-callback-timing" if initial else "callback-timing"
    (output / f"{name}-scales.json").write_text(json.dumps(dict(xMajorSeconds=5, fullYMajorMs=full_step, zoomYMajorMs=10,
        zoomMinMs=0, zoomMaxMs=50, maximumSample=peak+1, maximumMs=y[peak], recordingOrigin="First retained interval start near five rendered seconds warm-up"), indent=2)+"\n")
    save(fig, output, name)


def presentation(evidence, output):
    """Plot actual server receipt/display positions and local requests, shading only recorded focus-gate intervals."""
    raw = gzip.decompress((evidence / "view-owner.csv.gz").read_bytes()).decode()
    rows = list(csv.DictReader(io.StringIO(raw)))
    x = [float(row["wall_s"]) for row in rows]
    fig, axes = plt.subplots(2, 1, figsize=(11, 6.2), sharex=True)
    axes[0].plot(x, [float(row["received_z_m"]) for row in rows], label="Latest received server position", lw=.9)
    axes[0].plot(x, [float(row["displayed_z_m"]) for row in rows], label="Displayed (receipt delay, no extrapolation)", lw=1)
    axes[0].set(title="Actual replicated and displayed trajectory on the synthetic runway", ylabel="Northward CoG position (m)")
    axes[1].plot(x, [float(row["throttle"]) for row in rows], label="Local throttle request")
    axes[1].plot(x, [float(row["brake"]) for row in rows], label="Local brake request", alpha=.8)
    axes[1].set(ylabel="Normalized request", ylim=(-.05, 1.05), xlabel="Wall seconds since view proof's first ready pose")
    off = [float(row["wall_s"]) for row in rows if row["focused"] == "0"]
    for ax in axes:
        if off:
            ax.axvspan(min(off), max(off), alpha=.15, color="#ab5a42", label="Recorded logical focus gate")
        ax.xaxis.set_major_locator(MultipleLocator(5)); ax.grid(alpha=.25); ax.legend(fontsize=8)
    axes[1].yaxis.set_major_locator(MultipleLocator(.25))
    save(fig, output, "presentation-trace")


def main():
    """Render only checked measured data; graphical drawings are not substitutes for the actual game PNGs."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, default=Path("docs/reports/m03-flight-view-evidence"))
    parser.add_argument("--output", type=Path, default=Path("docs/previews/m03-flight-view"))
    args = parser.parse_args(); args.output.mkdir(parents=True, exist_ok=True)
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-m03-flight-view"})
    diagram(args.output, False); diagram(args.output, True); timing(args.evidence, args.output); presentation(args.evidence, args.output)
    if (args.evidence / "rejected-attempt-callbacks.marked.csv.gz").exists():
        timing(args.evidence,args.output,initial=True)


if __name__ == "__main__":
    main()
