"""Render actual real-time route architecture, server state, received/displayed poses and callback diagnostics as static SVGs."""
import argparse
import csv
import gzip
import json
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator
from render_m03_flight_view import save, timing


def records(path):
    """Read losslessly archived records from the accepted actual processes."""
    return [json.loads(line) for line in gzip.decompress(path.read_bytes()).decode('utf-8-sig').splitlines()]


def flow(output, after):
    """Draw the actual before/after ownership/transport boundary, without depicting planned gameplay systems."""
    fig,ax=plt.subplots(figsize=(12,3)); ax.set(xlim=(0,12),ylim=(0,3)); ax.axis('off')
    labels=(['Received server snapshot\nRead-only QA control math','Normal owner input RPC\nSeat / sequence / lease guards',
             'Actual accepted 5 ms steps\nTire / fuel / request traces','Owner + late observer\nRendered 100 ms interpolation'] if after else
            ['In-process QA controller\n20 ms synthetic commands','Actual server authority\n5 ms accelerated steps',
             'Bounded numerical hop\nTwo exact process replays','No live route evidence\nRendered / network gate open'])
    for i,label in enumerate(labels):
        x=1.5+i*3
        ax.text(x,1.5,label,ha='center',va='center',fontsize=8.5,bbox=dict(boxstyle='round,pad=.6',facecolor='#e8f1f8',edgecolor='#38526b'))
        if i<3: ax.annotate('',xy=(x+1.85,1.5),xytext=(x+1.1,1.5),arrowprops=dict(arrowstyle='->',lw=1.8))
    ax.set_title('AFTER: real-time automated calm-air route; server owns every physical state' if after else
                 'BEFORE 970d1f1: headless authority route only; graphical client verified for taxi',fontsize=11)
    save(fig,output,'after-flow' if after else 'before-flow')


def plots(evidence, output):
    """Keep server physical time, client receipt/render wall time and callback ruler time in separate explicitly labeled figures."""
    rows=records(evidence/'server.route.jsonl.gz'); rows=rows[::20]+[rows[-1]]
    x=[r['tick']*.005 for r in rows]
    fig,axes=plt.subplots(3,1,figsize=(10,7),sharex=True)
    axes[0].plot(x,[r['state']['PositionM']['Y'] for r in rows],label='Actual server CoG')
    axes[0].axhline(5,ls='--',label='Airborne height gate'); axes[0].set(title='Actual live dedicated-server route; every accepted step archived',ylabel='CoG datum height (m)')
    axes[1].plot(x,[r['state']['PositionM']['Z'] for r in rows],label='Actual server north Z')
    axes[1].axhline(2500,ls='--',label='Finite runway end'); axes[1].set(ylabel='North Z (m)')
    axes[2].step(x,[r['input']['Throttle'] for r in rows],where='post',label='Held accepted throttle')
    axes[2].step(x,[r['brake'] for r in rows],where='post',label='Held brake')
    axes[2].set(ylabel='Normalized command',xlabel='Aircraft physical seconds since server aircraft creation')
    for ax,step in zip(axes,(2,500,.5)):
        ax.xaxis.set_major_locator(MultipleLocator(10)); ax.yaxis.set_major_locator(MultipleLocator(step)); ax.grid(alpha=.25); ax.legend(fontsize=8)
    save(fig,output,'server-route')
    source={r['Tick']:r for r in records(evidence/'owner.snapshots.jsonl.gz')}
    raw=gzip.decompress((evidence/'owner.csv.gz').read_bytes()).decode('utf-8-sig')
    import io
    frames=list(csv.DictReader(io.StringIO(raw))); frames=frames[::6]+[frames[-1]]
    origin=float(frames[0]['wall_s']); x=[float(r['wall_s'])-origin for r in frames]
    fig,axes=plt.subplots(2,1,figsize=(10,5.7),sharex=True)
    axes[0].plot(x,[source[int(r['aircraft_tick'])]['Y'] for r in frames],label='Latest received CoG Y')
    axes[0].plot(x,[float(r['displayed_y_m']) for r in frames],label='Displayed CoG Y (100 ms history)')
    axes[0].set(title='Actual received and displayed route; client wall clock',ylabel='CoG datum height (m)')
    axes[1].plot(x,[float(r['received_z_m']) for r in frames],label='Latest received north Z')
    axes[1].plot(x,[float(r['displayed_z_m']) for r in frames],label='Displayed north Z')
    axes[1].set(ylabel='North Z (m)',xlabel='Client wall seconds since first retained presentation frame')
    for ax,step in zip(axes,(2,500)):
        ax.xaxis.set_major_locator(MultipleLocator(10)); ax.yaxis.set_major_locator(MultipleLocator(step)); ax.grid(alpha=.25); ax.legend(fontsize=8)
    save(fig,output,'received-displayed')


def main():
    """Use accepted raw data only; screenshot imagery is captured by Unity and never synthesized here."""
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--evidence',type=Path,default=Path('docs/reports/m03-realtime-evidence/runtime'))
    parser.add_argument('--output',type=Path,default=Path('docs/previews/m03-realtime-flight'))
    args=parser.parse_args(); args.output.mkdir(parents=True,exist_ok=True); plt.rcParams.update({'svg.fonttype':'none','svg.hashsalt':'fireline-realtime-route'})
    flow(args.output,False); flow(args.output,True); plots(args.evidence,args.output); timing(args.evidence,args.output)


if __name__=='__main__':
    main()
