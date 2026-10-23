"""Draw only the implemented M02 checkpoint helpers and their remaining integration boundary."""
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch


def main():
    """Write an editable SVG plus PNG review copy; arrows follow actual encode/store/read/restore calls, not future networking."""
    output = Path("docs/reports/m02-checkpoint-evidence")
    output.mkdir(parents=True, exist_ok=True)
    plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-m02-checkpoint", "font.size": 10})
    fig, axis = plt.subplots(figsize=(12, 5), layout="constrained")
    axis.set(xlim=(0, 12), ylim=(0, 5))
    axis.axis("off")
    axis.set_title("M02 partial increment: local checkpoint data path", fontsize=14)
    axis.text(6, 4.5, "BEFORE at 3b0e936: detached M01 DTO + store interface; no codec, file store or restoration", ha="center")
    points = [(0.2, 2.8), (4.2, 2.8), (8.2, 2.8), (8.2, 1.1), (4.2, 1.1), (0.2, 1.1)]
    labels = ["Detached snapshot DTO\nCaptureSnapshot()", "Validate / clone, encode\n≤2 MiB + SHA-256", "Flush temporary file\nMove / replace checkpoint",
              "Bounded file read\nNo silent fresh-world fallback", "Check hash / counts / cells\nMass balance + state digest", "Explicit Restore()\nIndependent candidate world"]
    for (x, y), label in zip(points, labels):
        axis.add_patch(FancyBboxPatch((x, y), 3.4, 1, boxstyle="round,pad=.04", facecolor="#edf6fb", edgecolor="#2676a3"))
        axis.text(x + 1.7, y + .5, label, ha="center", va="center")
    for start, end in [((3.65, 3.3), (4.1, 3.3)), ((7.65, 3.3), (8.1, 3.3)), ((9.9, 2.75), (9.9, 2.2)),
                       ((8.1, 1.6), (7.65, 1.6)), ((4.1, 1.6), (3.65, 1.6))]:
        axis.add_patch(FancyArrowPatch(start, end, arrowstyle="-|>", mutation_scale=15, color="#2676a3"))
    axis.text(6, .55, "Invalid data throws before authority replacement; SHA-256 detects corruption, not sender authenticity.", ha="center", fontsize=10)
    axis.text(6, .15, "Not wired: normal server startup/save scheduling, client transport, late join, event replay or score recovery.", ha="center", fontsize=10)
    svg = output / "checkpoint-flow.svg"
    fig.savefig(svg, metadata={"Date": None})
    svg.write_text("\n".join(line.rstrip() for line in svg.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")
    fig.savefig(output / "checkpoint-flow.png", dpi=120)
    plt.close(fig)


if __name__ == "__main__":
    main()
