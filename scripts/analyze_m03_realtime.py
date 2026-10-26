"""Verify actual live-route processes, command provenance, every server substep, read-only replicas and rendered presentation."""
import argparse
import csv
import gzip
import hashlib
import json
import math
from pathlib import Path
import struct
import tempfile
from decimal import Decimal, ROUND_HALF_UP
from analyze_m03_fuel import vector, rotate, cross
from analyze_m03_flight_view import float32
from analyze_m03_instruments import wind_at


def mono_fixed(value, digits, signed=False):
    """Reproduce this Mono player's 15-significant-digit conversion and decimal half-up readout rounding, including unsigned rounded zero."""
    rounded=Decimal(format(value,'.15g')).quantize(Decimal(1).scaleb(-digits),rounding=ROUND_HALF_UP)
    if rounded==0: rounded=abs(rounded)
    return format(rounded,('+' if signed and rounded else '')+f'.{digits}f')


def instrument_text(source):
    """Independently derive every actual SI readout and literal label using the shipped Mono formatting convention."""
    x,y,z,w=(source[k] for k in ('Qx','Qy','Qz','Qw'))
    forward=(2*(x*z+y*w),2*(y*z-x*w),1-2*(x*x+y*y))
    pitch=math.asin(max(-1,min(1,forward[1])))*180/math.pi
    bank=math.atan2(-2*(x*y+z*w),1-2*(x*x+z*z))*180/math.pi
    heading=math.atan2(forward[0],forward[2])*180/math.pi
    if heading<0: heading+=360
    air=math.sqrt((source['Vx']-source['WindX'])**2+source['Vy']**2+(source['Vz']-source['WindZ'])**2)
    ground=math.sqrt(source['Vx']**2+source['Vz']**2); mass=10000+source['FuelKg']+source['WaterKg']
    cg=(-.8*source['FuelKg']+1.2*source['WaterKg'])/mass
    return (f'Air speed {mono_fixed(air,2)} m/s\nGround (horizontal) {mono_fixed(ground,2)} m/s\n'
        f"Vertical {mono_fixed(source['Vy'],2,True)} m/s / CoG {mono_fixed(source['Y'],2)} m\n"
        f'Pitch {mono_fixed(pitch,1,True)} / Bank {mono_fixed(bank,1,True)} deg\nHeading {mono_fixed(heading,1):0>5} deg (N=0 / E=90)\n'
        f"Wind E {mono_fixed(source['WindX'],2,True)} / N {mono_fixed(source['WindZ'],2,True)} m/s\n"
        f"Fuel {mono_fixed(source['FuelKg'],2)} / Water {mono_fixed(source['WaterKg'],2)} kg\nMass {mono_fixed(mass,2)} kg / CoG Z {mono_fixed(cg,3)} m\n"
        f"Wind rev {source['WindRevision']} / Fire tick {source['FireTick']}")


def read(path):
    """Read a BOM-tolerant actual process JSON result."""
    return json.loads(path.read_text(encoding='utf-8-sig'))


def lines(path):
    """Read complete structured records; missing/empty role evidence is never inferred as success."""
    return [json.loads(row) for row in path.read_text(encoding='utf-8-sig').splitlines()]


def expected_control(source, phase):
    """Reconstruct the QA request independently from the exact source, including its latched phases and float wire conversion."""
    if phase==0 and source['Y']>2.2: phase=1
    if phase==1 and source['Z']>1200: phase=2
    if phase==2 and source['Y']<2.1: phase=3
    speed=math.sqrt(source['Vx']**2+source['Vy']**2+source['Vz']**2)
    q={k:source['Q'+k.lower()] for k in ('X','Y','Z','W')}
    pitch=math.asin(max(-1,min(1,rotate(q,[0,0,1])[1])))
    target_v=-1 if phase==2 else max(-1,min(1.5,(8-source['Y'])*.4))
    alpha=(((10000+source['FuelKg']+source['WaterKg'])*9.80665/(.5*1.225*max(speed**2,100)*100))-.6)/4.5
    target=alpha+math.atan2(target_v,max(speed,1))
    control=(1 if speed>46 else 0) if phase==0 else max(-1,min(1,7.5*alpha+16*(target-pitch)+3*source['Wx']))
    return phase,dict(Throttle=0 if phase==3 else 1,Pitch=0 if phase==3 else float32(control),Gear=1,Flaps=1,Brake=1 if phase==3 else 0,Roll=0,Yaw=0)


def support(state):
    """Recompute actual tire points/velocities, bounded normal support and finite-footprint margins from a recorded pose."""
    p=vector(state['PositionM']); q=state['Attitude']; fuel=state['FuelKg']; water=state['WaterKg']
    cg=(-.8*fuel+1.2*water)/(10000+fuel+water); count=0; compression=0; margin=math.inf
    for x,y,z in ((-3,-2,-3),(3,-2,-3),(0,-2,6)):
        lever=[x,y,z-cg]; point=[a+b for a,b in zip(p,rotate(q,lever))]
        margin=min(margin,22.5-abs(point[0]),point[2]+500,2500-point[2])
        if point[1]<=0:
            depth=-point[1]; vy=state['VelocityMps']['Y']+rotate(q,cross(vector(state['AngularVelocityRadps']),lever))[1]
            count+=int(max(0,min(250000,300000*depth-50000*vy))>0); compression=max(compression,depth)
    assert margin>=0 and compression<=.75
    return count,compression,margin


def presentation(rows, source):
    """Reconstruct the actual 100 ms receipt-history position/quaternion and float scene binding for every initialized frame."""
    previous=older=newer=None; distinct=checked=0; maximum=0.0
    for row in rows:
        tick=int(row['aircraft_tick']); current=source[tick]; received=float(row['received_at_s'])
        if tick!=previous:
            if newer is None or current['Epoch']!=newer[0]['Epoch'] or math.dist([current[k] for k in ('X','Y','Z')],[newer[0][k] for k in ('X','Y','Z')])>100:
                older=(current,received)
            else: older=newer
            newer=(current,received); distinct+=1; previous=tick
        if distinct<2: continue
        span=newer[1]-older[1]; t=1 if span<=0 else max(0,min(1,(float(row['wall_s'])-.1-older[1])/span))
        expected=[older[0][k]*(1-t)+newer[0][k]*t for k in ('X','Y','Z')]
        shown=[float(row[k]) for k in ('displayed_x_m','displayed_y_m','displayed_z_m')]
        error=math.dist(expected,shown); maximum=max(maximum,error); assert error<1e-8
        a,b=([state[k] for k in ('Qx','Qy','Qz','Qw')] for state in (older[0],newer[0]))
        sign=-1 if sum(x*y for x,y in zip(a,b))<0 else 1
        q=[x*(1-t)+y*t*sign for x,y in zip(a,b)]; norm=math.sqrt(sum(v*v for v in q)); q=[v/norm for v in q]
        assert max(abs(v-float(row[k])) for v,k in zip(q,('qx','qy','qz','qw')))<1e-10
        assert math.dist([float32(v) for v in shown],[float(row[k]) for k in ('anchor_x_m','anchor_y_m','anchor_z_m')])<1e-6
        cg=(-.8*current['FuelKg']+1.2*current['WaterKg'])/(10000+current['FuelKg']+current['WaterKg'])
        assert float(row['visual_y_m'])==-2 and abs(float(row['visual_z_m'])-float32(-cg))<1e-9
        checked+=1
    assert checked>1000
    return checked,maximum


def verify(raw, evidence, image):
    """Require actual successful roles and independently reconcile every authority/request/replica/render/readout boundary."""
    process=read(evidence/'process-summary.json'); assert process['passed'] and len(process['runs'])==3
    roles={role:read(raw/(role+'.json')) for role in ('server','owner','observer')}
    for record in process['runs']:
        assert record['exitCode']==0 and roles[record['role']]['passed']
        assert hashlib.sha256((raw/(record['role']+'.json')).read_bytes()).hexdigest().upper()==record['resultSha256']
    server,owner,observer=(roles[k] for k in ('server','owner','observer'))
    assert server['route']['passed'] and not server['faulted'] and server['owner']==0 and server['routeSeatReleases']==1 and server['disconnectReleases']==0 and server['expirations']==0
    assert owner['airborneCapture'] and owner['landedCapture'] and owner['released'] and observer['sawVacant'] and observer['sawAirborne']
    peers={p['identity']:p for p in server['peers']}
    assert peers[observer['identity']]['routeVacantAck'] and all(p['disconnected'] for p in peers.values())
    published={(r['identity'],r['state']['Tick']):r['state'] for r in lines(raw/'server.snapshots.jsonl')}
    received={}; matches={}
    for role in ('owner','observer'):
        data=lines(raw/(role+'.snapshots.jsonl')); identity=roles[role]['identity']
        assert len(data)==roles[role]['snapshots'] and len(data)>200
        assert all(s==published[identity,s['Tick']] for s in data)
        received[role]={s['Tick']:s for s in data}; matches[role]=len(data)
    assert min(received['observer'])>min(received['owner'])
    for s in published.values():
        assert math.dist(wind_at(s,server['sampledWindState'],server['sampledWindSeed']),[s['WindX'],s['WindZ']])<1e-10
        assert s['WindX']==0 and s['WindZ']==0
    queued={}; phase=0
    for row in lines(raw/'owner.commands.jsonl'):
        s,r=row['source'],row['request']; assert s==published[owner['identity'],s['Tick']]
        phase,expected=expected_control(s,phase); assert row['phase']==phase
        assert r['Version']==2 and r['Kind']==3 and r['Epoch']==s['Epoch'] and r['Tick']==s['Tick'] and s['Owner']==owner['identity']
        assert all(abs(float32(r[k])-float32(v))<1e-6 for k,v in expected.items())
        queued[r['Sequence']]=r
    assert phase==3 and len(queued)==owner['queuedInputs'] and len(queued)>500
    requests=lines(raw/'server.requests.jsonl'); accepted={}
    for row in requests:
        assert row['identity']==owner['identity'] and row['request']==queued[row['request']['Sequence']]
        if row['result']==0: accepted[(row['identity'],row['request']['Epoch'],row['request']['Sequence'])]=row['request']
    assert len(accepted)>500
    steps=lines(raw/'server.route.jsonl'); assert len(steps)==server['tick']==server['route']['recordedSteps']
    airborne=longest=stopped=0; touched=False; previous=400.0; maximum=0.0; margin=math.inf
    for index,row in enumerate(steps,1):
        assert row['tick']==index
        state=row['state']; inp=row['input']; n,compression,edge=support(state); margin=min(margin,edge)
        assert abs(sum(state['Attitude'][k]**2 for k in ('X','Y','Z','W'))-1)<1e-8
        assert n==row['supportedWheels'] and abs(compression-row['compressionM'])<1e-9
        if row['owner']==0 or row['sequence']==0:
            expected=dict(Throttle=0,Pitch=0,Roll=0,Yaw=0,Flaps=0,Gear=1,Brake=1)
        else: expected=accepted[row['owner'],row['epoch'],row['sequence']]
        assert all(abs(inp[k]-float32(expected[k]))<1e-12 for k in ('Throttle','Pitch','Roll','Yaw','Flaps','Gear')) and row['brake']==expected['Brake']
        assert abs(state['FuelKg']-(previous-min(previous,.25*inp['Throttle']*.005)))<1e-12 and state['WaterKg']==0
        previous=state['FuelKg']; y=state['PositionM']['Y']; maximum=max(maximum,y)
        airborne=airborne+1 if n==0 and y>5 else 0; longest=max(longest,airborne)
        touched|=longest>=1000 and n>=2
        speed=math.dist(vector(state['VelocityMps']),[0,0,0])
        stopped=stopped+1 if touched and n==3 and speed<.2 and abs(state['VelocityMps']['Y'])<.1 and inp['Throttle']==0 and row['brake']>=.99 else 0
    assert longest>=1000 and stopped>=1000
    assert server['route']['longestAirborneSeconds']==longest*.005 and server['route']['settledSeconds']==stopped*.005
    assert server['route']['maximumHeightM']==maximum and abs(server['consumedFuelKg']-(400-previous))<1e-10
    rows=list(csv.DictReader((raw/'owner.csv').open())); checked,error=presentation(rows,received['owner'])
    texts=lines(raw/'owner.instruments.jsonl'); initialized_texts=0
    for row in texts:
        if row['source']['Version']==0:
            assert row['text']=='Waiting for validated server data'
            continue
        assert row['source']==published[owner['identity'],row['source']['Tick']]
        assert row['text']==instrument_text(row['source']),row['source']['Tick']
        initialized_texts+=1
    assert initialized_texts>1000
    metrics=read(raw/'metrics.json'); intervals=[float(r['frame_interval_ms']) for r in csv.DictReader((raw/'metrics.csv').open())]
    assert metrics['width']==1920 and metrics['height']==1080 and metrics['quality']=='Balanced' and len(intervals)==metrics['samples']
    assert abs(len(intervals)*1000/sum(intervals)-metrics['averageFps'])<1e-4
    ordered=sorted(intervals); slow=ordered[-math.ceil(len(ordered)*.01):]
    assert abs(1000/(sum(slow)/len(slow))-metrics['onePercentLowFps'])<1e-4
    assert abs(ordered[math.ceil(.95*len(ordered))-1]-metrics['p95FrameMs'])<1e-5
    assert abs(ordered[math.ceil(.99*len(ordered))-1]-metrics['p99FrameMs'])<1e-5
    elapsed=0; worst=[]; evidence.mkdir(parents=True,exist_ok=True)
    with (evidence/'callback-intervals.marked.csv').open('w',newline='') as output:
        writer=csv.writer(output); writer.writerow(('sample','start_s','end_s','interval_ms','over_16_667_ms','over_33_333_ms'))
        for i,value in enumerate(intervals,1):
            end=elapsed+value/1000; writer.writerow((i,elapsed,end,value,value>1000/60,value>1000/30))
            worst.append(dict(sample=i,startSeconds=elapsed,endSeconds=end,callbackMs=value)); elapsed=end
    for suffix in ('.png','.airborne.png','.landed.png'):
        path=image.with_suffix(suffix); data=path.read_bytes()
        assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(1920,1080)
    archives=[]
    for role in ('server','owner','observer'):
        for suffix in ('.json','.csv','.snapshots.jsonl','.route.jsonl','.requests.jsonl','.instruments.jsonl','.commands.jsonl'):
            path=raw/(role+suffix)
            if not path.exists(): continue
            data=path.read_bytes(); name=path.name+'.gz'; (evidence/name).write_bytes(gzip.compress(data,mtime=0))
            archives.append(dict(path=name,rawBytes=len(data),rawSha256=hashlib.sha256(data).hexdigest()))
    for name in ('metrics.json','metrics.csv'): (evidence/name).write_bytes((raw/name).read_bytes())
    result=dict(passed=True,exactReceivedSnapshots=matches,checkedQueuedControls=len(queued),checkedAcceptedRequests=len(accepted),
        checkedServerSubsteps=len(steps),longestAirborneSeconds=longest*.005,settledSeconds=stopped*.005,maximumHeightM=maximum,
        minimumWheelFootprintMarginM=margin,consumedFuelKg=server['consumedFuelKg'],checkedDisplayFrames=checked,maximumDisplayDifferenceM=error,
        checkedInstrumentTexts=initialized_texts,maximumCallbacks=sorted(worst,key=lambda r:r['callbackMs'],reverse=True)[:10],
        intervalsOver16_667Ms=sum(v>1000/60 for v in intervals),intervalsOver33_333Ms=sum(v>1000/30 for v in intervals),archives=archives,
        limits='Automated calm-air straight runway hop over real local transport/rendering; not manual hardware, crosswind/payload envelope, water, WAN/capacity or representative 60 FPS qualification.')
    (evidence/'verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('archives','maximumCallbacks')}))


def main():
    """Check fresh raw processes or replay every immutable raw hash into a separate scratch directory."""
    parser=argparse.ArgumentParser(description=__doc__); group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--capture',type=Path); group.add_argument('--archive',type=Path)
    parser.add_argument('--evidence',type=Path,required=True); parser.add_argument('--image',type=Path,required=True)
    args=parser.parse_args()
    if args.capture: verify(args.capture,args.evidence,args.image)
    else:
        assert args.archive.resolve()!=args.evidence.resolve()
        original=read(args.archive/'verification.json'); process=read(args.archive/'process-summary.json')
        args.evidence.mkdir(parents=True,exist_ok=True); (args.evidence/'process-summary.json').write_text(json.dumps(process),encoding='utf-8')
        with tempfile.TemporaryDirectory(prefix='fireline-real-flight-') as name:
            raw=Path(name)
            for item in original['archives']:
                entry=Path(item['path']); assert str(entry)==entry.name and entry.suffix=='.gz'
                data=gzip.decompress((args.archive/entry).read_bytes()); assert len(data)==item['rawBytes'] and hashlib.sha256(data).hexdigest()==item['rawSha256']
                (raw/entry.stem).write_bytes(data)
            for filename in ('metrics.json','metrics.csv'): (raw/filename).write_bytes((args.archive/filename).read_bytes())
            verify(raw,args.evidence,args.image)
            assert original==read(args.evidence/'verification.json')


if __name__=='__main__':
    main()
