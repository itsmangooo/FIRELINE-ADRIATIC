"""Independently reconstruct actual dedicated water release, drag/impact, bilinear coverage, conservation and fire cooling."""
import argparse
import copy
import gzip
import hashlib
import json
import math
from pathlib import Path
from analyze_m03_fuel import vector, rotate, cross, read
from analyze_m03_instruments import wind_at


def close(a,b,tolerance=1e-8):
    """Require finite scalar/vector agreement without hiding NaN behind a comparison."""
    if isinstance(a,(list,tuple)):
        assert len(a)==len(b) and all(math.isfinite(x) and math.isfinite(y) for x,y in zip(a,b))
        assert math.dist(a,b)<tolerance,(a,b)
    else:
        assert math.isfinite(a) and math.isfinite(b) and abs(a-b)<tolerance,(a,b)


def cg(aircraft):
    """Compute the unchanged generic body's centerline mass reference in meters."""
    return (-.8*aircraft['FuelKg']+1.2*aircraft['WaterKg'])/(10000+aircraft['FuelKg']+aircraft['WaterKg'])


def step(p,v,t,wind,dt,density,drag,ground):
    """Independent semi-implicit point-mass update and segment-plane intersection; no game assembly is loaded."""
    relative=[w-speed for w,speed in zip(wind,v)]; speed=math.sqrt(sum(x*x for x in relative))
    acceleration=[x*.5*density*drag*speed for x in relative]; acceleration[1]-=9.80665
    velocity=[a+b*dt for a,b in zip(v,acceleration)]; position=[a+b*dt for a,b in zip(p,velocity)]
    hit=position[1]<=ground; fraction=(p[1]-ground)/(p[1]-position[1]) if hit else 1
    if hit:
        position=[a+(b-a)*fraction for a,b in zip(p,position)]; position[1]=ground
        velocity=[a+(b-a)*fraction for a,b in zip(v,velocity)]
    return position,velocity,t+dt*fraction,hit


def check(case):
    """Validate every emitted/final parcel, all first-parcel steps and each actual affected fire cell."""
    aircraft=case['aircraft']; release=case['release']; after=release['AircraftAfter']; parcels=release['Parcels']
    assert release['ReleasedKg']==2000 and len(parcels)==64 and aircraft['WaterKg']==4000 and after['WaterKg']==2000
    assert after['FuelKg']==aircraft['FuelKg'] and after['Attitude']==aircraft['Attitude'] and after['AngularVelocityRadps']==aircraft['AngularVelocityRadps']
    q=aircraft['Attitude']; p0=vector(aircraft['PositionM']); v0=vector(aircraft['VelocityMps']); omega=vector(aircraft['AngularVelocityRadps'])
    shift=[0,0,cg(after)-cg(aircraft)]
    close(vector(after['PositionM']),[a+b for a,b in zip(p0,rotate(q,shift))])
    close(vector(after['VelocityMps']),[a+b for a,b in zip(v0,rotate(q,cross(omega,shift)))])
    total=sum(p['MassKg'] for p in parcels); close(total,release['ReleasedKg'],1e-10)
    impacts=case['impacts']; assert len(impacts)==64
    expected_cells={}; maximum=0.; checked=0
    for index,(parcel,impact) in enumerate(zip(parcels,impacts)):
        row,col=divmod(index,8); lever=[((col+.5)/8-.5)*4,-1,1+((row+.5)/8-.5)*2-cg(aircraft)]
        p=[a+b for a,b in zip(p0,rotate(q,lever))]; velocity=cross(omega,lever); velocity[1]-=2
        v=[a+b for a,b in zip(v0,rotate(q,velocity))]
        close(p,vector(parcel['PositionM'])); close(v,vector(parcel['VelocityMps'])); close(parcel['MassKg'],31.25,1e-10)
        assert parcel['FlightSeconds']==0 and not parcel['HitGround']
        t=0.; hit=False; count=0
        while not hit and count<20000:
            wind_tick=math.floor((count*case['dt']+1e-10)/.2)
            wx,wz=wind_at(dict(X=p[0],Y=p[1],Z=p[2],FireTick=wind_tick),case['wind'],case['seed'])
            wind=[wx,0,wz]
            p,v,t,hit=step(p,v,t,wind,case['dt'],case['densityKgM3'],case['dragAreaPerKg'],case['groundY']); count+=1
            if index==0:
                observed=case['trajectory'][count-1]; actual=observed['parcel']
                assert observed['step']==count and observed['windTick']==wind_tick
                close(wind,vector(observed['windMps']),1e-10); close(p,vector(actual['PositionM'])); close(v,vector(actual['VelocityMps']))
                close(t,actual['FlightSeconds'],1e-10); assert actual['HitGround']==hit and actual['MassKg']==parcel['MassKg']
                checked+=1
        assert hit and impact['HitGround'] and impact['MassKg']==parcel['MassKg']
        maximum=max(maximum,math.dist(p,vector(impact['PositionM']))); close(p,vector(impact['PositionM'])); close(v,vector(impact['VelocityMps']))
        close(t,impact['FlightSeconds'],1e-10)
        if index==0: assert count==len(case['trajectory'])
        gx,gz=p[0]/10-.5,p[2]/10-.5; x,z=math.floor(gx),math.floor(gz); fx,fz=gx-x,gz-z
        for dz in range(2):
            for dx in range(2):
                mass=parcel['MassKg']*(fx if dx else 1-fx)*(fz if dz else 1-fz)
                if mass==0: continue
                cx,cz=(x+dx)//32,(z+dz)//32; cell=(z+dz-cz*32)*32+x+dx-cx*32
                key=(1,cx,cz,cell); expected_cells[key]=expected_cells.get(key,0)+mass
    coverage=case['coverage']; close(coverage['MassKg'],total,1e-10); close(case['appliedWaterKg'],total,1e-10)
    hits=coverage['Hits']; assert len(hits)==len(expected_cells)==len(case['cells'])
    close(sum(h['Weight'] for h in hits),1,1e-12)
    seen=[]; retained=0.; suppressed=0
    for hit,cell in zip(hits,case['cells']):
        key=tuple(hit['Chunk'][k] for k in ('Sector','X','Z'))+(hit['Cell'],); seen.append(key)
        close(hit['Weight'],expected_cells[key]/total,1e-10); assert cell['hit']==hit
        a,b=cell['before'],cell['after']; dose=expected_cells[key]/100; retained+=b['WaterKgPerM2']*100
        assert a['Phase']==2 and a['Intensity']==1 and a['Heat']==1 and a['WaterKgPerM2']==0
        close(b['WaterKgPerM2'],dose); close(b['Heat'],math.exp(-dose/.5))
        extinguished=dose>=.75 or math.exp(-dose/.5)<.15
        close(b['Intensity'],0 if extinguished else math.exp(-dose/.5)); assert b['Phase']==(4 if extinguished else 2)
        assert b['RemainingFuelKgPerM2']==a['RemainingFuelKgPerM2'] and b['VegetationId']==a['VegetationId']
        suppressed+=int(b['Intensity']<1)
    assert seen==sorted(expected_cells) and suppressed>0; close(retained,total,1e-8)
    centroid=[sum(p['MassKg']*p['PositionM'][k] for p in impacts)/total for k in ('X','Z')]
    grid_centroid=[sum(mass*((key[1]*32+key[3]%32+.5)*10) for key,mass in expected_cells.items())/total,
        sum(mass*((key[2]*32+key[3]//32+.5)*10) for key,mass in expected_cells.items())/total]
    close(centroid,grid_centroid,1e-8)
    return dict(case=case['name'],parcels=64,checkedFirstParcelSteps=checked,impactCentroidXM=centroid[0],impactCentroidZM=centroid[1],
        minimumImpactSeconds=min(p['FlightSeconds'] for p in impacts),maximumImpactSeconds=max(p['FlightSeconds'] for p in impacts),
        impactedKg=sum(p['MassKg'] for p in impacts),tankAfterKg=after['WaterKg'],affectedCells=len(hits),retainedFireKg=retained,
        maximumImpactDifferenceM=maximum,massBalanceErrorKg=abs(aircraft['WaterKg']-after['WaterKg']-retained))


def rejected_cases(original):
    """Require independent verification to reject changed tank debit, launch velocity, wind, impact and coverage evidence."""
    changes=[('tank',lambda c:c['release']['AircraftAfter'].__setitem__('WaterKg',2001)),
        ('launch',lambda c:c['release']['Parcels'][0]['VelocityMps'].__setitem__('X',99)),
        ('wind',lambda c:c['trajectory'][0]['windMps'].__setitem__('X',99)),
        ('impact',lambda c:c['impacts'][0]['PositionM'].__setitem__('X',99)),
        ('coverage',lambda c:c['coverage']['Hits'][0].__setitem__('Weight',.99))]
    rejected=[]
    for label,change in changes:
        modified=copy.deepcopy(original); change(modified)
        try: check(modified)
        except (AssertionError,KeyError,IndexError): rejected.append(label)
        else: raise AssertionError('Corruption accepted: '+label)
    return rejected


def main():
    """Bind two real zero-exit process archives, replay all numerical boundaries and preserve old flight/fuel/authority evidence."""
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--numerical',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True); args=parser.parse_args()
    process=read(args.numerical/'process-summary.json'); assert process['passed'] and len(process['runs'])==2
    runs=[]
    for record in process['runs']:
        raw=gzip.decompress((args.numerical/f"run-{record['run']}.json.gz").read_bytes())
        assert record['exitCode']==0 and hashlib.sha256(raw).hexdigest().upper()==record['resultSha256']
        runs.append(json.loads(raw.decode('utf-8-sig')))
    names=['low_calm','high_calm','low_crosswind','high_crosswind','fine_high_crosswind','tilted_spinning','vacuum']
    assert all(r['passed'] and r['headless'] and r['water']['profile']=='DEBUG_WATER_PARCELS_V1' for r in runs)
    assert [c['name'] for c in runs[0]['water']['cases']]==names and runs[0]['water']['cases']==runs[1]['water']['cases']
    baseline=read(Path('docs/reports/m03-realtime-evidence/numerical-final/run-1.json.gz'))
    assert all(all(r[k]==baseline[k] for k in ('cases','polar','fuel','route')) for r in runs)
    summaries=[check(c) for c in runs[0]['water']['cases']]; assert summaries==[check(c) for c in runs[1]['water']['cases']]
    by_name={s['case']:s for s in summaries}
    for height in ('low','high'):
        assert by_name[height+'_crosswind']['impactCentroidXM']>by_name[height+'_calm']['impactCentroidXM']+5
    assert by_name['high_crosswind']['maximumImpactSeconds']>by_name['low_crosswind']['maximumImpactSeconds']
    convergence=math.dist([by_name['high_crosswind'][k] for k in ('impactCentroidXM','impactCentroidZM')],
        [by_name['fine_high_crosswind'][k] for k in ('impactCentroidXM','impactCentroidZM')]); assert convergence<.5
    vacuum=next(c for c in runs[0]['water']['cases'] if c['name']=='vacuum')
    initial=vacuum['release']['Parcels'][0]; height=initial['PositionM']['Y']-vacuum['groundY']; vy=initial['VelocityMps']['Y']
    exact_time=(vy+math.sqrt(vy*vy+2*9.80665*height))/9.80665
    vacuum_time_error=abs(initial['FlightSeconds']+exact_time-vacuum['impacts'][0]['FlightSeconds'])
    vacuum_north_error=abs(initial['PositionM']['Z']+initial['VelocityMps']['Z']*exact_time-vacuum['impacts'][0]['PositionM']['Z'])
    assert vacuum_time_error<vacuum['dt'] and vacuum_north_error<.6
    timings=[]
    for proof in (r['water'] for r in runs):
        values=proof['batchStepMs']; assert len(values)==1000 and all(math.isfinite(v) and v>=0 for v in values)
        ordered=sorted(values); timings.append(dict(calls=1000,warmupCalls=100,parcelsPerBatch=64,meanBatchMs=sum(values)/1000,
            p95BatchMs=ordered[949],maximumBatchMs=max(values),gc0Collections=proof['gc0Collections']))
    result=dict(passed=True,exactTwoProcessWaterReplay=True,originalFlightFuelAuthorityUnchanged=True,caseSummaries=summaries,
        coarseVsFineHighCrosswindCentroidM=convergence,vacuumVsContinuousImpactTimeErrorSeconds=vacuum_time_error,
        vacuumVsContinuousNorthErrorM=vacuum_north_error,negativeChecksRejected=rejected_cases(runs[0]['water']['cases'][0]),timings=timings,
        limits='Accelerated headless numerical release prerequisite only: no live tank installation, release RPC, scooping, release duration, terrain/water geometry, VFX, multiplayer suppression, capacity or FPS acceptance.')
    args.output.parent.mkdir(parents=True,exist_ok=True); args.output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))


if __name__=='__main__':
    main()
