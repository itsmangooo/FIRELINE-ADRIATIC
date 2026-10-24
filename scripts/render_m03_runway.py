"""Render implementation-based runway BEFORE/AFTER diagrams and plots from actual dedicated-player records."""
import argparse
import gzip
import json
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator

EVIDENCE = Path("docs/reports/m03-runway-evidence")
OUT = Path("docs/physics/runway-contact")


def save(fig, name):
    """Export self-contained deterministic SVG plus PNG for inspection, keeping geometry unchanged while trimming whitespace."""
    fig.tight_layout()
    fig.savefig(OUT/f"{name}.svg", metadata={"Date": None})
    fig.savefig(OUT/f"{name}.png", dpi=125)
    path = OUT/f"{name}.svg"
    path.write_text("\n".join(line.rstrip() for line in path.read_text(encoding="utf-8").splitlines())+"\n", encoding="utf-8")
    plt.close(fig)


def diagram(name, title, labels):
    """Draw the actual four-stage implementation boundary; arrows indicate force/state flow, not future network behavior."""
    fig, ax = plt.subplots(figsize=(12, 2.8))
    ax.set(xlim=(0,12), ylim=(0,2.5)); ax.axis("off"); ax.set_title(title, fontsize=12)
    for index, label in enumerate(labels):
        x = 1.5+index*3
        ax.text(x, 1.2, label, ha="center", va="center", fontsize=9,
                bbox=dict(boxstyle="round,pad=0.7", facecolor="#e8f1f8", edgecolor="#38526b"))
        if index < 3:
            ax.annotate("", xy=(x+1.85,1.2), xytext=(x+1.15,1.2), arrowprops=dict(arrowstyle="->",lw=1.8,color="#38526b"))
    save(fig,name)


def trajectories(cases):
    """Plot equivalent-input fall/support and braking traces with units, explicit baselines and actual retained samples."""
    fig, axes = plt.subplots(2,2,figsize=(12,7.6))
    for name, label in (("before_rest","BEFORE: no ground reaction"),("rest","AFTER: contact")):
        rows=[r for r in cases[name]["samples"] if r["seconds"] <= 2]
        axes[0,0].plot([r["seconds"] for r in rows],[r["state"]["PositionM"]["Y"] for r in rows],label=label)
    axes[0,0].axhline(0,color="#a2453c",ls=":",label="Runway elevation")
    axes[0,0].set(title="Same initial rest state: support versus pass-through",ylabel="CoG height (m)")
    axes[0,0].xaxis.set_major_locator(MultipleLocator(.5))
    axes[0,0].yaxis.set_major_locator(MultipleLocator(5))
    for name,label in (("rest","10,400 kg"),("loaded","14,400 kg")):
        rows=cases[name]["samples"]
        axes[0,1].plot([r["seconds"] for r in rows],[r["contact"]["MaximumCompressionM"] for r in rows],label=label)
    axes[0,1].axhline(.75,color="#a2453c",ls=":",label="Rejection envelope")
    axes[0,1].set(title="Same initial pose: mass changes gear compression",ylabel="Maximum tire penetration (m)",ylim=(0,.8))
    axes[0,1].yaxis.set_major_locator(MultipleLocator(.2))
    for name,label in (("before_brake","BEFORE: no ground model"),("coast","AFTER: brake=0"),("brake","AFTER: brake=1")):
        rows=cases[name]["samples"];t=[r["seconds"] for r in rows]
        axes[1,0].plot(t,[r["state"]["VelocityMps"]["Z"] for r in rows],label=label)
        axes[1,1].plot(t,[r["state"]["PositionM"]["Z"] for r in rows],label=label)
    axes[1,0].set(title="25 m/s initial roll, density=0: isolated tire forces",ylabel="Forward velocity (m/s)")
    axes[1,0].yaxis.set_major_locator(MultipleLocator(5))
    axes[1,1].set(title="Equivalent roll inputs: actual northward travel",ylabel="Northward distance (m)")
    axes[1,1].yaxis.set_major_locator(MultipleLocator(50))
    for row in range(2):
        for col in range(2):
            ax=axes[row,col];ax.set_xlabel("Physical elapsed time (s)");ax.grid(alpha=.25);ax.legend(fontsize=7)
            if (row,col)!=(0,0):ax.xaxis.set_major_locator(MultipleLocator(2))
    save(fig,"numerical-comparison")


def timing(runs):
    """Keep all actual elapsed calls and annotate each maximum; this is not render timing or a complete host tick."""
    fig, axes = plt.subplots(2,1,figsize=(11,6),sharex=True)
    for index,(ax,run) in enumerate(zip(axes,runs),1):
        values=[v*1000 for v in run["stepMs"]]
        peak=max(range(len(values)),key=values.__getitem__)
        ax.plot(range(1,len(values)+1),values,lw=.7,color="#2c5879")
        ax.annotate(f"#{peak+1}: {values[peak]:.3f} µs",xy=(peak+1,values[peak]),xytext=(.5,.75),textcoords="axes fraction",arrowprops=dict(arrowstyle="->"))
        ax.set(title=f"Run {index}: independent fresh-state contact Step calls after 100 warm-ups",ylabel="Elapsed call time (µs)",ylim=(0,None))
        ax.xaxis.set_major_locator(MultipleLocator(200));ax.grid(alpha=.25)
    axes[-1].set_xlabel("Retained call index (not physical time or server tick)")
    save(fig,"step-timing")


def main():
    """Read raw process evidence and produce only data-backed diagrams/plots, never fabricated gameplay screenshots."""
    global EVIDENCE, OUT
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence",type=Path,default=EVIDENCE);parser.add_argument("--output",type=Path,default=OUT)
    args=parser.parse_args();EVIDENCE,OUT=args.evidence,args.output
    OUT.mkdir(parents=True,exist_ok=True)
    plt.rcParams.update({"svg.fonttype":"none","svg.hashsalt":"fireline-m03-runway"})
    runs=[json.loads(gzip.decompress((EVIDENCE/f"run-{i}.json.gz").read_bytes())) for i in (1,2)]
    diagram("before","BEFORE — 1abf438: free body has no physical ground contact",[
        "State + validated input\nMass / wind / atmosphere","Aerodynamics + gravity\nNo wheel reactions","Existing 6DOF integrator\nNo collision boundary","Candidate state\nCan pass through y=0"])
    diagram("after","AFTER — isolated bounded contact prerequisite; no live pilot/network binding",[
        "State + gear + brake\nFinite runway rectangle","Three point reactions\nSpring / damper / friction","Sum forces + moments\nSame 6DOF integrator","Validate candidate\nReject invalid contact"])
    trajectories({c["name"]:c for c in runs[0]["cases"]})
    timing(runs)


if __name__ == "__main__":
    main()
