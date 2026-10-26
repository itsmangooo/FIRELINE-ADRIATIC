"""Render the real previous offline-control boundary, new authority proof and archived numerical trajectories as static SVGs."""
import argparse
import csv
import gzip
import json
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator


def save(fig, output, name):
    """Export a self-contained vector drawing plus a PNG fallback for visual inspection."""
    fig.tight_layout(); fig.savefig(output/(name+'.svg')); fig.savefig(output/(name+'.png'),dpi=130); plt.close(fig)
    vector=output/(name+'.svg')
    vector.write_text('\n'.join(line.rstrip() for line in vector.read_text(encoding='utf-8').splitlines())+'\n',encoding='utf-8')


def flow(output, after):
    """Draw only implemented paths and retain the explicit headless-versus-live boundary."""
    fig,ax=plt.subplots(figsize=(12,3)); ax.set(xlim=(0,12),ylim=(0,3)); ax.axis('off')
    labels=(['Bounded QA pilot\n20 ms held commands','Real seat / input guards\n5 ms authority substeps',
             'Actual tire / fuel state\nStay inside finite runway','5 s airborne + 5 s settled\nTwo headless replays'] if after else
            ['Offline .NET strategies\n5 ms control update','Existing pure flight\ncontact + fuel kernel',
             'No qualified runway stop\nBoth return exit 2','No authority route proof\nLive flight gate open'])
    for i,label in enumerate(labels):
        x=1.5+3*i
        ax.text(x,1.5,label,ha='center',va='center',fontsize=9,bbox=dict(boxstyle='round,pad=.6',facecolor='#e8f1f8',edgecolor='#38526b'))
        if i<3: ax.annotate('',xy=(x+1.85,1.5),xytext=(x+1.1,1.5),arrowprops=dict(arrowstyle='->',lw=1.8))
    ax.set_title('AFTER: accelerated calm-air authority route; rendered multiplayer flight still pending' if after else
                 'BEFORE e8604e9: retained failed control strategies; no authority route acceptance',fontsize=11)
    save(fig,output,'after' if after else 'before')


def trajectory(evidence, output):
    """Plot actual post-step records; never extrapolate a successful route to the old attempts' 120-second horizon."""
    raw=gzip.decompress((evidence/'numerical/run-1.json.gz').read_bytes()).decode('utf-8-sig'); route=json.loads(raw)['route']
    current=route['samples']; selected=current[::20]+[current[-1]]
    baselines=Path('docs/reports/m03-instrument-evidence/route-investigation')
    fig,axes=plt.subplots(3,1,figsize=(10,8),sharex=True)
    for name in ('initial','damped'):
        rows=list(csv.DictReader((baselines/(name+'.csv')).open())); x=[float(r['seconds']) for r in rows]
        axes[0].plot(x,[float(r['height_m']) for r in rows],label='Previous '+name+' (offline)')
        axes[1].plot(x,[float(r['north_m']) for r in rows],label='Previous '+name+' (offline)')
    x=[r['seconds'] for r in selected]
    axes[0].plot(x,[r['state']['PositionM']['Y'] for r in selected],label='Current authority QA route')
    axes[1].plot(x,[r['state']['PositionM']['Z'] for r in selected],label='Current authority QA route')
    axes[2].plot(x,[(sum(r['state']['VelocityMps'][k]**2 for k in ('X','Y','Z'))) ** .5 for r in selected],label='Current 3D speed')
    axes[0].set(title='Actual control investigations and bounded authority route; unchanged physical kernel',ylabel='CoG datum height (m)')
    axes[1].set(ylabel='North Z (m)'); axes[1].axhline(2500,ls='--',color='#a44737',label='Finite runway end (2500 m)')
    axes[2].set(ylabel='Speed (m/s)',xlabel='Physical post-step seconds; no warm-up removed')
    for ax,step in zip(axes,(200,1000,10)):
        ax.xaxis.set_major_locator(MultipleLocator(20)); ax.yaxis.set_major_locator(MultipleLocator(step)); ax.grid(alpha=.25); ax.legend(fontsize=8)
    save(fig,output,'trajectory')
    fig,axes=plt.subplots(2,1,figsize=(10,5.6),sharex=True)
    axes[0].plot(x,[r['state']['PositionM']['Y'] for r in selected],label='Current authority QA route'); axes[0].axhline(5,ls='--',label='Airborne segment height threshold')
    axes[0].set(ylabel='CoG datum height (m)',title='Current route: detail retained separately from failed-run altitude range')
    axes[0].yaxis.set_major_locator(MultipleLocator(2)); axes[0].legend(fontsize=8)
    axes[1].step(x,[r['phase'] for r in selected],where='post',label='QA command phase')
    axes[1].set(ylabel='Phase ID',xlabel='Physical post-step seconds; retained route samples every 0.1 s')
    axes[1].set_yticks((0,1,2,3),('0 roll','1 climb','2 approach','3 brake'))
    for ax in axes: ax.xaxis.set_major_locator(MultipleLocator(10)); ax.grid(alpha=.25)
    save(fig,output,'route-detail')


def main():
    """Draw from the accepted immutable player artifacts and historical failures only."""
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--evidence',type=Path,default=Path('docs/reports/m03-route-evidence'))
    parser.add_argument('--output',type=Path,default=Path('docs/physics/runway-route'))
    args=parser.parse_args(); args.output.mkdir(parents=True,exist_ok=True)
    plt.rcParams.update({'svg.fonttype':'none','svg.hashsalt':'fireline-m03-route'})
    flow(args.output,False); flow(args.output,True); trajectory(args.evidence,args.output)


if __name__=='__main__':
    main()
