"""Independently check actual dedicated-player route control/contact/resource evidence and immutable legacy trajectories."""
import argparse
import copy
import gzip
import hashlib
import json
import math
from pathlib import Path
from analyze_m03_fuel import read, rotate, vector, cross


def clamp(value, low, high):
    """Bound a diagnostic command using explicit inclusive limits."""
    return max(low, min(high, value))


def check(route):
    """Check every recorded 5 ms authority state against independent commands, tire geometry, fuel debit and route gates."""
    assert route['profile']=='DEBUG_AUTHORITY_ROUTE_V1' and route['passed'] and route['dt']==.005 and route['commandSeconds']==.02
    initial=route['initial']; previous=initial; phase=0; commands=0; airborne=longest=stopped=height=consumed=0.0
    seen=touched=False; first_air=first_touch=None; maximum_speed=0.0; minimum_margin=math.inf
    assert initial['PositionM']==dict(X=0.0,Y=2.05,Z=0.0) and initial['FuelKg']==400 and initial['WaterKg']==0
    rows=route['samples']; assert 10000<len(rows)<=24000
    for i,row in enumerate(rows):
        state=row['state']; p=state['PositionM']; q=state['Attitude']; fuel=state['FuelKg']; inp=row['input']
        assert row['tick']==i+1 and row['fireTick']==i//40 and row['seconds']==(i+1)*.005
        assert abs(sum(q[k]**2 for k in ('X','Y','Z','W'))-1)<1e-8
        if i%4==0:
            commands+=1; old=previous['PositionM']; speed=math.dist(vector(previous['VelocityMps']),[0,0,0])
            if phase==0 and old['Y']>3: phase=1
            if phase==1 and old['Z']>1400: phase=2
            if phase==2 and old['Y']<2.1: phase=3
            pitch=math.asin(clamp(rotate(previous['Attitude'],[0,0,1])[1],-1,1))
            vertical=-1 if phase==2 else clamp((10-old['Y'])*.4,-1,2)
            cl=(10000+previous['FuelKg']+previous['WaterKg'])*9.80665/(.5*1.225*max(speed**2,100)*100)
            alpha=(cl-.2-.4)/4.5; target=alpha+math.atan2(vertical,max(speed,1))
            command=(1 if speed>46 else 0) if phase==0 else clamp(7.5*alpha+32*(target-pitch)+3*previous['AngularVelocityRadps']['X'],-1,1)
            expected=dict(Throttle=0.0 if phase==3 else 1.0,Pitch=0.0 if phase==3 else command,Roll=0.0,Yaw=0.0,Flaps=1.0,Gear=1.0)
        assert row['phase']==phase and row['brake']==(1 if phase==3 else 0)
        assert all(abs(inp[k]-expected[k])<1e-10 for k in expected)
        used=min(previous['FuelKg'],.25*inp['Throttle']*.005); consumed+=used
        assert abs(fuel-(previous['FuelKg']-used))<1e-12 and state['WaterKg']==0
        cg=(-.8*fuel)/(10000+fuel); supported=0; compression=0.0
        for wheel in ((-3,-2,-3),(3,-2,-3),(0,-2,6)):
            lever=[wheel[0],wheel[1],wheel[2]-cg]; offset=rotate(q,lever)
            point=[a+b for a,b in zip(vector(p),offset)]
            margin=min(22.5-abs(point[0]),point[2]+500,2500-point[2]); minimum_margin=min(minimum_margin,margin)
            assert margin>=0, 'Wheel left the finite runway footprint.'
            if point[1]<=0:
                depth=-point[1]; assert depth<=.75
                velocity_y=state['VelocityMps']['Y']+rotate(q,cross(vector(state['AngularVelocityRadps']),lever))[1]
                load=clamp(300000*depth-50000*velocity_y,0,250000)
                supported+=int(load>0); compression=max(compression,depth)
        assert supported==row['supportedWheels'] and abs(compression-row['maximumCompressionM'])<1e-9
        airborne=airborne+.005 if supported==0 and p['Y']>5 else 0
        longest=max(longest,airborne); seen|=airborne>=5
        if first_air is None and airborne>0: first_air=row['seconds']
        if seen and phase>=2 and supported>=2:
            touched=True
            if first_touch is None: first_touch=row['seconds']
        speed=math.dist(vector(state['VelocityMps']),[0,0,0]); maximum_speed=max(maximum_speed,speed)
        stopped=stopped+.005 if touched and phase==3 and supported==3 and speed<.2 and abs(state['VelocityMps']['Y'])<.1 else 0
        height=max(height,p['Y']); previous=state
    assert longest>=5 and stopped>=5 and phase==3 and commands==route['acceptedCommands']
    assert abs(consumed-route['consumedFuelKg'])<1e-7 and abs(height-route['maximumHeightM'])<1e-12
    assert abs(longest-route['longestAirborneSeconds'])<1e-10 and abs(stopped-route['stoppedSeconds'])<1e-10
    return dict(samples=len(rows),acceptedCommands=commands,maximumHeightM=height,maximumSpeedMps=maximum_speed,
        firstAbove5mSeconds=first_air,firstTouchdownSeconds=first_touch,longestAirborneSeconds=longest,
        settledSeconds=stopped,durationSeconds=rows[-1]['seconds'],finalNorthM=previous['PositionM']['Z'],
        finalSpeedMps=math.dist(vector(previous['VelocityMps']),[0,0,0]),consumedFuelKg=route['consumedFuelKg'],minimumWheelEdgeMarginM=minimum_margin)


def main():
    """Capture two successful process archives or revalidate their immutable hashes and reject deliberately corrupted candidates."""
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--numerical',type=Path,default=Path('docs/reports/m03-route-evidence/numerical'))
    parser.add_argument('--output',type=Path,default=Path('docs/reports/m03-route-evidence/route-verification.json'))
    args=parser.parse_args(); process=read(args.numerical/'process-summary.json')
    assert process['passed'] and len(process['runs'])==2 and all(r['exitCode']==0 for r in process['runs'])
    runs=[]
    for i in (1,2):
        raw=gzip.decompress((args.numerical/f'run-{i}.json.gz').read_bytes())
        assert hashlib.sha256(raw).hexdigest().upper()==process['runs'][i-1]['resultSha256']
        runs.append(json.loads(raw.decode('utf-8-sig')))
    baseline=read(Path('docs/reports/m03-instrument-evidence/physics-regression.json.gz'))
    summaries=[check(run['route']) for run in runs]
    assert runs[0]['route']==runs[1]['route'] and summaries[0]==summaries[1]
    for run in runs:
        assert all(run[k]==baseline[k] for k in ('cases','polar','fuel'))
    bad=[]
    for kind in ('command','fuel','contact','early-stop','footprint'):
        candidate=copy.deepcopy(runs[0]['route'])
        if kind=='command': candidate['samples'][100]['input']['Throttle']=.5
        elif kind=='fuel': candidate['samples'][100]['state']['FuelKg']+=1
        elif kind=='contact': candidate['samples'][-1]['supportedWheels']=0
        elif kind=='early-stop': candidate['samples']=candidate['samples'][:10001]
        else: candidate['samples'][100]['state']['PositionM']['X']=23
        try: check(candidate)
        except AssertionError: bad.append(kind)
        else: raise AssertionError('Corrupt candidate incorrectly accepted: '+kind)
    result=dict(passed=True,route=summaries[0],exactTwoProcessRouteReplay=True,unchangedLegacyFlightFuel=True,negativeChecksRejected=bad,
        rawArchives=[dict(path=f'numerical/run-{i}.json.gz',rawSha256=process['runs'][i-1]['resultSha256']) for i in (1,2)],
        limits='Accelerated headless calm-air QA authority route, not real-time transport, manual flight, rendered gameplay, CL-415 calibration, water or FPS qualification.')
    args.output.parent.mkdir(parents=True,exist_ok=True); args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result))


if __name__=='__main__':
    main()
