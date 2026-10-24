"""Draw actual instrument protocol/view boundaries and recorded airdata/callback plots; never synthesize gameplay screenshots."""
import argparse
import gzip
import json
import math
from pathlib import Path

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator
from render_m03_flight_view import save, timing


def flow(output, after):
    """Draw the actual version-one diagnostic path and new sampled-wind/read-only instrument path."""
    fig,ax=plt.subplots(figsize=(12,3)); ax.set(xlim=(0,12),ylim=(0,3)); ax.axis('off')
    labels=(['Server pose + wind sample\nSame position / fire tick','Aircraft protocol v2\nComplete read-only replica',
             'Validated airdata math\nNo client integration','Actual uGUI text\nSource + text archived'] if after else
            ['Server aircraft pose\nWind revision only','Aircraft protocol v1\nNo sampled wind vector',
             '3D speed called ground\nNo airspeed / attitude','Existing engineering HUD\nNo fuel/mass readout'])
    ax.set_title('AFTER: authoritative sampled wind and verified flight instruments' if after else
                 'BEFORE c90b6e8: pose/control HUD, incomplete airdata',fontsize=12)
    for i,label in enumerate(labels):
        x=1.5+3*i
        ax.text(x,1.5,label,ha='center',va='center',fontsize=9,bbox=dict(boxstyle='round,pad=.6',facecolor='#e8f1f8',edgecolor='#38526b'))
        if i<3: ax.annotate('',xy=(x+1.85,1.5),xytext=(x+1.1,1.5),arrowprops=dict(arrowstyle='->',lw=1.8))
    save(fig,output,'after-flow' if after else 'before-flow')


def instruments(evidence, output):
    """Plot distinct source samples actually used by the visible readout, using aircraft physical tick time rather than frame timing."""
    rows=[json.loads(line)['source'] for line in gzip.decompress((evidence/'view-owner.instruments.jsonl.gz').read_bytes()).decode('utf-8').splitlines()]
    unique={row['Tick']:row for row in rows if row['Version']==2}; rows=[unique[tick] for tick in sorted(unique)]
    first=rows[0]['Tick']; seconds=[(r['Tick']-first)*.005 for r in rows]
    air=[math.sqrt((r['Vx']-r['WindX'])**2+r['Vy']**2+(r['Vz']-r['WindZ'])**2) for r in rows]
    ground=[math.hypot(r['Vx'],r['Vz']) for r in rows]; wind=[math.hypot(r['WindX'],r['WindZ']) for r in rows]
    fig,axes=plt.subplots(2,1,figsize=(10,6),sharex=True)
    for values,label in ((air,'Air-relative 3D speed'),(ground,'Horizontal ground speed'),(wind,'Sampled horizontal wind')):
        axes[0].plot(seconds,values,label=label)
    axes[0].set(title='Actual server samples used by the visible instruments; taxi proof',ylabel='Speed (m/s)'); axes[0].legend(fontsize=8)
    axes[0].yaxis.set_major_locator(MultipleLocator(2))
    axes[1].plot(seconds,[r['FuelKg'] for r in rows],label='Received server fuel'); axes[1].legend(fontsize=8)
    axes[1].set(ylabel='Fuel (kg)',xlabel='Aircraft physical seconds from first instrument source tick')
    axes[1].yaxis.set_major_locator(MultipleLocator(.2))
    for ax in axes: ax.xaxis.set_major_locator(MultipleLocator(5)); ax.grid(alpha=.25)
    save(fig,output,'airdata-trace')


def main():
    """Render actual published evidence with static SVG exports and inspectable PNG fallbacks."""
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--evidence',type=Path,default=Path('docs/reports/m03-instrument-evidence/runtime'))
    parser.add_argument('--output',type=Path,default=Path('docs/previews/m03-flight-instruments'))
    args=parser.parse_args(); args.output.mkdir(parents=True,exist_ok=True); plt.rcParams['svg.fonttype']='none'
    flow(args.output,False); flow(args.output,True); instruments(args.evidence,args.output); timing(args.evidence,args.output)


if __name__=='__main__':
    main()
