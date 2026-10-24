"""Draw the actual new flight boundary, recorded trajectories/polars, analytic convergence and measured Step costs; no gameplay screenshots."""
import gzip
import json
import math
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch
from matplotlib.ticker import MultipleLocator, MaxNLocator

ROOT=Path('docs/reports/m03-flight-evidence')
PHYSICS=Path('docs/physics/debug-flight')


def save(fig,folder,name):
    """Export self-contained editable-text SVG plus a PNG review copy, without unstable timestamp metadata."""
    folder.mkdir(parents=True,exist_ok=True); p=folder/(name+'.svg')
    fig.savefig(p,metadata={'Date':None})
    p.write_text('\n'.join(line.rstrip() for line in p.read_text(encoding='utf-8').splitlines())+'\n',encoding='utf-8')
    fig.savefig(folder/(name+'.png'),dpi=120);plt.close(fig)


def diagram(after):
    """Describe the real absence of flight at M02 or the implemented isolated headless flow, explicitly excluding planned gameplay."""
    fig,ax=plt.subplots(figsize=(12,3.4),layout='constrained');ax.set(xlim=(0,12),ylim=(0,3));ax.axis('off')
    ax.set_title('AFTER: M03 isolated DEBUG_FLIGHT_ONLY_V1' if after else 'BEFORE: M02 has no flight implementation')
    labels=['Validated state + commands\nExplicit SI wind / density','Mass + CoG + inertia\nAir-relative forces / moments','Euler gyro term\nSemi-implicit translation','Quaternion rotation\nValidate returned candidate'] if after else ['Server fire / wind\nReplicated fire checkpoints','No aircraft forces, mass model\nor 6DOF state integrator','Read-only client fire display\nNo aircraft state or pilot input']
    span=12/len(labels)
    for i,label in enumerate(labels):
        x=i*span+.12;ax.add_patch(FancyBboxPatch((x,1.1),span-.45,1,boxstyle='round,pad=.035',facecolor='#edf6fb',edgecolor='#2676a3'))
        ax.text(x+(span-.45)/2,1.6,label,ha='center',va='center',fontsize=9)
        if i<len(labels)-1:ax.add_patch(FancyArrowPatch((x+span-.4,1.6),(x+span-.15,1.6),arrowstyle='-|>',mutation_scale=14))
    ax.text(6,.45,'Opt-in dedicated-player proof → two independent process runs → JSON/CSV → analytic/replay checks' if after else 'Actual baseline absence; previous numerical flight results are unavailable, not zero',ha='center',fontsize=10)
    ax.text(6,.05,'No mesh / runway / collision / pilot RPC / flight replication / takeoff-landing loop implemented in this increment',ha='center',fontsize=9)
    save(fig,PHYSICS,'after' if after else 'before')


def main():
    """Plot only committed actual runtime observations and label analytic reference lines as references rather than previous implementations."""
    plt.rcParams.update({'svg.fonttype':'none','svg.hashsalt':'fireline-m03-debug-flight','font.size':9})
    diagram(False);diagram(True)
    data=json.loads(gzip.decompress((ROOT/'run-1.json.gz').read_bytes()))
    second=json.loads(gzip.decompress((ROOT/'run-2.json.gz').read_bytes()))
    cases={c['name']:c for c in data['cases']}
    fig,axes=plt.subplots(1,3,figsize=(14,4),layout='constrained')
    polar=data['polar']
    axes[0].plot([p['alphaDegrees'] for p in polar],[p['cleanCl'] for p in polar],label='Clean estimate')
    axes[0].plot([p['alphaDegrees'] for p in polar],[p['flapsCl'] for p in polar],label='Full flaps estimate')
    for edge in (-14,14): axes[0].axvline(edge,color='#999999',ls=':')
    axes[0].set(xlabel='Angle of attack (degrees)',ylabel='Lift coefficient (dimensionless)',title='Recorded debug lift curves',xlim=(-30,30))
    axes[0].xaxis.set_major_locator(MultipleLocator(10));axes[0].yaxis.set_major_locator(MultipleLocator(.5));axes[0].legend()
    for name in ('dry','loaded'):
        rows=cases[name]['samples'];axes[1].plot([0]+[r['seconds'] for r in rows],[1000]+[r['state']['PositionM']['Y'] for r in rows],label=f"{name}: {rows[0]['forces']['Mass']['Kg']:g} kg")
    axes[1].set(xlabel='Simulated seconds',ylabel='CoG height (m)',title='Untrimmed free flight: same commands',xlim=(0,4));axes[1].xaxis.set_major_locator(MultipleLocator(1));axes[1].yaxis.set_major_locator(MaxNLocator(5));axes[1].legend()
    errors=[];dts=[]
    for name in ('ballistic_5ms','ballistic_10ms','ballistic_20ms'):
        case=cases[name]; dts.append(case['dt']*1000);errors.append(abs(case['samples'][-1]['state']['PositionM']['Y']-(1000-.5*9.80665*16)))
    axes[2].plot(dts,errors,'o',label='Actual integration error')
    axes[2].plot(dts,[.5*9.80665*4*dt/1000 for dt in dts],label='Analytic semi-implicit error')
    axes[2].set(xlabel='Integration step (ms)',ylabel='Height error at 4 s (m)',title='Vacuum time-step convergence',xlim=(0,21),ylim=(0,.45));axes[2].xaxis.set_major_locator(MultipleLocator(5));axes[2].yaxis.set_major_locator(MultipleLocator(.1));axes[2].legend()
    for ax in axes:ax.grid(alpha=.2)
    height_ticks=axes[1].get_yticks().tolist()
    save(fig,PHYSICS,'numerical-comparison')
    all_us=[v*1000 for run in (data,second) for v in run['stepMs']]
    tick=max(1,math.ceil(max(all_us)/5)); upper=tick*6
    fig,axes=plt.subplots(2,1,figsize=(11,5.5),layout='constrained')
    for index,(ax,run) in enumerate(zip(axes,(data,second)),1):
        values=[v*1000 for v in run['stepMs']];p95=sorted(values)[949];worst=max(range(len(values)),key=values.__getitem__)
        ax.plot(range(1,len(values)+1),values,lw=.7);ax.axhline(p95,color='#a66a00',ls='--',label=f'p95: {p95:.3f} µs')
        ax.annotate(f"Call #{worst+1}: {values[worst]:.3f} µs",(worst+1,values[worst]),xytext=(.55,.82),textcoords='axes fraction',arrowprops={'arrowstyle':'->'})
        ax.set(title=f'Actual dedicated process run {index}',xlabel='Retained Step call index (100 warm-up calls excluded)',ylabel='Stopwatch elapsed (µs)',xlim=(0,1000),ylim=(0,upper));ax.xaxis.set_major_locator(MultipleLocator(200));ax.yaxis.set_major_locator(MultipleLocator(tick));ax.grid(alpha=.2);ax.legend(loc='upper left')
    save(fig,ROOT,'step-timing')
    (ROOT/'plot-scales.json').write_text(json.dumps({'timingYTickMicroseconds':tick,'timingUpperMicroseconds':upper,'heightYTicksM':height_ticks})+'\n',encoding='utf-8')


if __name__=='__main__':main()
