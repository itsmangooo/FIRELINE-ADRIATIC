"""Render the implemented import/measurement path; no proposed gameplay flows are implied."""
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch

OUT = Path("docs/reports/m00-coastal-evidence/diagrams")
OUT.mkdir(parents=True, exist_ok=True)
plt.rcParams.update({"svg.fonttype": "none", "svg.hashsalt": "fireline-coastal", "font.size": 10})


def drawing(name, title, labels, footer):
    fig, axis = plt.subplots(figsize=(11, 2.6), layout="constrained")
    axis.set_xlim(0, 11)
    axis.set_ylim(0, 2.5)
    axis.axis("off")
    axis.set_title(title, fontsize=13)
    for index, label in enumerate(labels):
        x = 0.1 + index * 2.8
        axis.add_patch(FancyBboxPatch((x, 0.8), 2.35, 1, boxstyle="round,pad=0.05",
                                     facecolor="#edf6fb", edgecolor="#2676a3"))
        axis.text(x + 1.175, 1.3, label, ha="center", va="center")
        if index < len(labels) - 1:
            axis.add_patch(FancyArrowPatch((x + 2.4, 1.3), (x + 2.72, 1.3), arrowstyle="-|>",
                                           mutation_scale=14, color="#2676a3"))
    axis.text(5.5, 0.24, footer, ha="center", fontsize=10)
    path = OUT / f"{name}.svg"
    fig.savefig(path, metadata={"Date": None})
    path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines()) + "\n", encoding="utf-8")
    fig.savefig(OUT / f"{name}.png", dpi=130)
    plt.close(fig)


drawing("before", "BEFORE: M00 calibration foundation at db4ab83",
        ["Starter sky / lighting", "Generated box scene", "No imported coastal\nDSM or PBR surfaces", "Short 720p diagnostic\n896 render intervals"],
        "No coastal heightfield baseline existed; no comparative speedup can be calculated.")
drawing("after", "AFTER: implemented coastal import and benchmark foundation",
        ["Official CC0 maps\n+ GLO-90 DSM", "Verify hashes / licence\nPack masks / crop SI data", "Unity terrain +\nexplicit visual proxies", "Release capture +\nfive-minute samples"],
        "Client render workload only • CL-415 import blocked by conflicting licences • No gameplay fire or water model")
