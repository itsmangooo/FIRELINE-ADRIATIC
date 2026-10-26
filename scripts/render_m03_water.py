"""Render actual dedicated-player water release boundaries, mass/drift trajectories and measured raster doses as static SVGs."""
import argparse
import math
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import Normalize
from matplotlib.patches import Rectangle
from matplotlib.ticker import MultipleLocator
from analyze_m03_fuel import read
from render_m03_flight_view import save


def flow(output, after):
    """Draw the real numerical boundary and keep unimplemented live controls/VFX explicitly labeled."""
    fig,ax=plt.subplots(figsize=(14,3)); ax.set(xlim=(0,14),ylim=(0,3)); ax.axis('off')
    labels=(['Validated loaded state\nPure candidate release','Tank/CoG + 64 parcels\nInherited velocity + exit',
        'M01 wind + gravity/drag\nSegment-plane impacts','Mass-weighted bilinear cells\nTrusted fire ApplyWater',
        'Actual numerical fire response\nLive controls/VFX still pending'] if after else
        ['Frozen per-step water mass\nNo tank-release transaction','Aircraft force model only\nNo emitted water parcels',
        'No water trajectory/impact\nNo aerial coverage','Trusted manual water weights\nExisting fire cooling',
        'No aircraft-to-fire path\nRuntime controls pending'])
    for i,label in enumerate(labels):
        x=1.4+2.8*i
        ax.text(x,1.5,label,ha='center',va='center',fontsize=8,bbox=dict(boxstyle='round,pad=.6',facecolor='#e8f1f8',edgecolor='#38526b'))
        if i<4: ax.annotate('',xy=(x+1.6,1.5),xytext=(x+1.25,1.5),arrowprops=dict(arrowstyle='->',lw=1.5))
    ax.set_title('AFTER: actual headless water-release prerequisite; live gameplay remains open' if after else
        'BEFORE 4dc96fe: loaded aircraft mass and manual server water; no aerial release implementation',fontsize=11)
    save(fig,output,'after' if after else 'before')


def trajectories(cases, output):
    """Plot every actual first-parcel sample at its retained physical timestamp, including the fractional final impact."""
    fig,axes=plt.subplots(2,1,figsize=(10,7),sharex=True)
    for name in ('low_calm','high_calm','low_crosswind','high_crosswind'):
        case=cases[name]; points=[case['release']['Parcels'][0]]+[r['parcel'] for r in case['trajectory']]
        t=[r['FlightSeconds'] for r in points]; origin=points[0]['PositionM']['X']
        axes[0].plot(t,[r['PositionM']['Y'] for r in points],label=name)
        axes[1].plot(t,[r['PositionM']['X']-origin for r in points],label=name)
    axes[0].axhline(0,color='black',ls='--',lw=.8); axes[0].set(title='Actual representative water parcel: low/high release, calm/8 m/s base crosswind',ylabel='Height above flat plane (m)')
    axes[1].set(ylabel='East drift from release (m)',xlabel='Parcel physical seconds since release')
    for ax,spacing in zip(axes,(20,5)):
        ax.xaxis.set_major_locator(MultipleLocator(1)); ax.yaxis.set_major_locator(MultipleLocator(spacing)); ax.grid(alpha=.25); ax.legend(fontsize=8)
    save(fig,output,'trajectories')


def footprints(cases, output):
    """Draw actual post-application kg/m2 on the shared 10 m grid, with individual actual impact points overlaid."""
    names=('low_calm','high_calm','low_crosswind','high_crosswind')
    maximum=max(c['after']['WaterKgPerM2'] for name in names for c in cases[name]['cells']); normal=Normalize(0,maximum)
    fig=plt.figure(figsize=(11,8)); grid=fig.add_gridspec(2,3,width_ratios=(1,1,.06))
    axes=[fig.add_subplot(grid[row,col]) for row in range(2) for col in range(2)]; color=plt.cm.Blues
    for ax,name in zip(axes,names):
        case=cases[name]; xs=[]; zs=[]
        for cell in case['cells']:
            hit=cell['hit']; row,col=divmod(hit['Cell'],32); x=(hit['Chunk']['X']*32+col)*10; z=(hit['Chunk']['Z']*32+row)*10
            xs.append(x); zs.append(z); dose=cell['after']['WaterKgPerM2']
            ax.add_patch(Rectangle((x,z),10,10,facecolor=color(normal(dose)),edgecolor='#aaaaaa',lw=.5))
        ax.scatter([p['PositionM']['X'] for p in case['impacts']],[p['PositionM']['Z'] for p in case['impacts']],s=3,c='black',label='Actual parcel impacts')
        ax.set(xlim=(min(xs)-5,max(xs)+15),ylim=(min(zs)-5,max(zs)+15),title=name,xlabel='East X (m)',ylabel='North Z (m)',aspect='equal')
        ax.xaxis.set_major_locator(MultipleLocator(10)); ax.yaxis.set_major_locator(MultipleLocator(10)); ax.legend(fontsize=7)
    fig.suptitle('Actual conserved 2,000 kg release: bilinear grid dose, no evaporation or fire-time advance')
    bar=fig.add_subplot(grid[:,2])
    scale=fig.colorbar(plt.cm.ScalarMappable(norm=normal,cmap=color),cax=bar,label='Retained water (kg/m²)')
    scale.solids.set_rasterized(False)
    save(fig,output,'footprints')


def mass(cases, output):
    """Reconstruct the conserved tank/airborne/landed ledger from actual tilted-release parcel masses and impact clocks."""
    case=cases['tilted_spinning']; impacts=case['impacts']; end=max(p['FlightSeconds'] for p in impacts)
    t=[i*.02 for i in range(math.ceil(end/.02)+2)]; tank=case['release']['AircraftAfter']['WaterKg']
    airborne=[sum(p['MassKg'] for p in impacts if p['FlightSeconds']>time) for time in t]
    landed=[case['release']['ReleasedKg']-a for a in airborne]
    fig,ax=plt.subplots(figsize=(10,4.5))
    ax.step(t,airborne,where='post',label='Still airborne'); ax.step(t,landed,where='post',label='Landed cumulative')
    ax.plot(t,[tank]*len(t),ls='--',label='Tank after debit'); ax.plot(t,[tank+a+b for a,b in zip(airborne,landed)],label='Tank + airborne + landed')
    ax.set(title='Actual tilted/spinning numerical release: conserved water ledger',xlabel='Physical seconds since instantaneous release',ylabel='Water mass (kg)')
    ax.xaxis.set_major_locator(MultipleLocator(1)); ax.yaxis.set_major_locator(MultipleLocator(1000)); ax.grid(alpha=.25); ax.legend(fontsize=8)
    save(fig,output,'mass-balance')


def main():
    """Use recorded actual server data; no gameplay screenshot or water VFX is synthesized."""
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--numerical',type=Path,required=True)
    parser.add_argument('--output',type=Path,default=Path('docs/physics/water-parcels')); args=parser.parse_args()
    proof=read(args.numerical/'run-1.json.gz')['water']; cases={c['name']:c for c in proof['cases']}
    args.output.mkdir(parents=True,exist_ok=True); plt.rcParams.update({'svg.fonttype':'none','svg.hashsalt':'fireline-water-parcels'})
    flow(args.output,False); flow(args.output,True); trajectories(cases,args.output); footprints(cases,args.output); mass(cases,args.output)


if __name__=='__main__':
    main()
