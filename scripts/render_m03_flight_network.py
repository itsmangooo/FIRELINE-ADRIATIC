"""Render actual before/after authority diagrams and measured network-host traces as self-contained SVGs."""
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
    """Export vector evidence and a PNG inspection fallback with stable metadata."""
    fig.tight_layout()
    fig.savefig(output / f"{name}.svg", metadata={"Date": None})
    fig.savefig(output / f"{name}.png", dpi=125)
    path = output / f"{name}.svg"
    path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")
    plt.close(fig)


def diagram(output, after):
    """Draw implemented control/pose boundaries, explicitly showing the absent live aircraft host in the baseline."""
    fig, ax = plt.subplots(figsize=(12, 5 if after else 2.9))
    ax.set(xlim=(0, 12), ylim=(0, 5 if after else 3)); ax.axis("off")
    ax.set_title("AFTER: opt-in live aircraft authority + read-only replicas" if after else
                 "BEFORE: 50ce914 — isolated runway proof, no live aircraft authority", fontsize=12)
    if after:
        boxes = [(2, 3.9, "Actual source connection\nSubscribe / claim / input"),
                 (6, 3.9, "Server: owner + seat epoch\nSequence / tick / range / lease"),
                 (10, 3.9, "Bounded host clock\n≤ 8 × 5 ms / callback"),
                 (10, 1.4, "Existing contact + flight Step\nAuthoritative wind / fire tick"),
                 (6, 1.4, "Server pose / seat / tick\n10 Hz max, exact ACK"),
                 (2, 1.4, "Clients validate + retain\nNo client integration")]
        edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5)]
    else:
        boxes = [(2, 1.6, "Scripted inputs\nNo connected pilot"), (6, 1.6, "Isolated RunwayContact.Step\nNumerical JSON / CSV"),
                 (10, 1.6, "Static aircraft preview\nNo pose binding / RPC")]
        edges = [(0, 1)]
    for x, y, label in boxes:
        ax.text(x, y, label, ha="center", va="center", fontsize=9,
                bbox=dict(boxstyle="round,pad=.7", facecolor="#e8f1f8", edgecolor="#38526b"))
    for source, target in edges:
        x, y, _ = boxes[source]; xx, yy, _ = boxes[target]
        dx, dy = (xx - x), (yy - y)
        shrink_x = 1.4 if dx else 0
        shrink_y = .48 if dy else 0
        ax.annotate("", xy=(xx - (1 if dx > 0 else -1) * shrink_x, yy - (1 if dy > 0 else -1) * shrink_y),
                    xytext=(x + (1 if dx > 0 else -1) * shrink_x, y + (1 if dy > 0 else -1) * shrink_y),
                    arrowprops=dict(arrowstyle="->", lw=1.7, color="#38526b"))
    if after:
        ax.text(6, .25, "ACK returns on the existing connection; one pending snapshot per peer. Invalid controls/poses cannot install state.",
                ha="center", fontsize=8)
    save(fig, output, "after" if after else "before")


def traces(evidence, output):
    """Keep startup, callback stalls and discarded time visible; never label these headless measurements as rendered frame loss."""
    rows = list(csv.DictReader(io.StringIO(gzip.decompress((evidence / "server.csv.gz").read_bytes()).decode())))
    times = [float(row["elapsed_s"]) for row in rows]
    gaps = [float(row["callback_gap_s"]) * 1000 for row in rows]
    costs = [float(row["host_advance_ms"]) for row in rows]
    fig, axes = plt.subplots(3, 1, figsize=(11, 8.5), sharex=True)
    for ax, values, title in ((axes[0], gaps, "Unity host callback spacing (includes startup; not CPU/GPU execution)"),
                              (axes[1], costs, "Stopwatch around FlightAuthority.Advance only; no warm-up excluded")):
        ax.plot(times, values, lw=.75)
        maximum = max(range(len(values)), key=values.__getitem__)
        ax.annotate(f"sample #{maximum + 1}: {values[maximum]:.3f} ms", xy=(times[maximum], values[maximum]),
                    xytext=(.35, .78), textcoords="axes fraction", arrowprops=dict(arrowstyle="->"))
        ax.set(title=title, ylabel="Milliseconds", ylim=(0, None)); ax.grid(alpha=.25)
    axes[0].axhline(40, ls="--", color="#b94b40", label="40 ms accumulated-work cap")
    axes[0].legend(fontsize=8)
    axes[1].axhline(5, ls=":", color="#9b7840", label="5 ms physical step interval (not a CPU budget)")
    axes[1].legend(fontsize=8)
    axes[2].plot(times, [float(row["discarded_s"]) for row in rows], color="#b94b40")
    axes[2].set(title="Cumulative wall time discarded by catch-up cap; never counted as simulated time", ylabel="Discarded seconds")
    axes[2].grid(alpha=.25)
    for ax in axes:
        ax.xaxis.set_major_locator(MultipleLocator(2))
    axes[-1].set_xlabel("Wall seconds since server aircraft adapter Start (one sample per Update)")
    save(fig, output, "host-timing")
    fig, ax = plt.subplots(figsize=(11, 4))
    for role in ("owner", "observer"):
        frames = [json.loads(line) for line in gzip.decompress((evidence / f"{role}.snapshots.jsonl.gz").read_bytes()).decode().splitlines()]
        ax.plot([state["Tick"] * .005 for state in frames], [state["Z"] for state in frames], label=f"{role}: applied server poses", lw=1.4, alpha=.8)
    ax.set(title="Actual client replicas; every retained pose matches its server publication", xlabel="Server aircraft physical time (tick × 0.005 s)",
           ylabel="Northward CoG position (m)")
    ax.xaxis.set_major_locator(MultipleLocator(2)); ax.grid(alpha=.25); ax.legend()
    save(fig, output, "replica-trajectory")


def main():
    """Render only archived measured evidence; no diagram represents a screenshot or future gameplay implementation."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, default=Path("docs/reports/m03-flight-network-evidence"))
    parser.add_argument("--output", type=Path, default=Path("docs/physics/live-flight-host"))
    args = parser.parse_args(); args.output.mkdir(parents=True, exist_ok=True)
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-live-flight-host"})
    diagram(args.output, False); diagram(args.output, True); traces(args.evidence, args.output)


if __name__ == "__main__":
    main()
