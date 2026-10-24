"""Independent snapshot-wind and actually displayed instrument-text verification for the bounded M03 runtime fixture."""
import json
import math


def signed(value, digits):
    """Match documented positive/negative/rounded-zero readout sections without calling C# formatting."""
    rounded = float(f'{value:.{digits}f}')
    return f'{rounded:+.{digits}f}' if rounded else f'{0:.{digits}f}'


def wind_at(source, state, seed):
    """Recompute the existing M01 horizontal field from recorded base/gust state, location and fire tick."""
    x, z = math.floor(source['X']/100), math.floor(source['Z']/100)
    tick = source['FireTick']; epoch = tick//20; blend = (tick%20)/20
    def noise(channel, time):
        """Reproduce the documented unsigned 32-bit hash with explicit overflow after each multiply."""
        mask = 0xffffffff
        h = ((seed+channel)&mask) ^ ((x&mask)*374761393&mask) ^ ((z&mask)*668265263&mask) ^ ((time&mask)*2246822519&mask) ^ (time>>32)
        h = ((h ^ (h>>13))*1274126177)&mask
        return (h ^ (h>>16))/mask
    factor = 1+max(0,min(300,source['Y']))*.001
    result=[]
    for channel, component in ((17,'EastMps'),(53,'NorthMps')):
        a, b = noise(channel,epoch), noise(channel,epoch+1)
        result.append(state[component]*factor+state['GustMps']*(2*(a+(b-a)*blend)-1))
    return result


def text_from(source):
    """Derive all nine visible SI instrument lines independently from the exact source used by the view."""
    x,y,z,w=(source[k] for k in ('Qx','Qy','Qz','Qw'))
    forward=(2*(x*z+y*w),2*(y*z-x*w),1-2*(x*x+y*y))
    pitch=math.degrees(math.asin(max(-1,min(1,forward[1]))))
    bank=math.degrees(math.atan2(-2*(x*y+z*w),1-2*(x*x+z*z)))
    heading=math.degrees(math.atan2(forward[0],forward[2]))%360
    air=math.sqrt((source['Vx']-source['WindX'])**2+source['Vy']**2+(source['Vz']-source['WindZ'])**2)
    ground=math.hypot(source['Vx'],source['Vz']); mass=10000+source['FuelKg']+source['WaterKg']
    cg=(-.8*source['FuelKg']+1.2*source['WaterKg'])/mass
    return (f'Air speed {air:.2f} m/s\nGround (horizontal) {ground:.2f} m/s\n'
        f"Vertical {signed(source['Vy'],2)} m/s / CoG {source['Y']:.2f} m\n"
        f'Pitch {signed(pitch,1)} / Bank {signed(bank,1)} deg\nHeading {heading:05.1f} deg (N=0 / E=90)\n'
        f"Wind E {signed(source['WindX'],2)} / N {signed(source['WindZ'],2)} m/s\n"
        f"Fuel {source['FuelKg']:.2f} / Water {source['WaterKg']:.2f} kg\nMass {mass:.2f} kg / CoG Z {cg:.3f} m\n"
        f"Wind rev {source['WindRevision']} / Fire tick {source['FireTick']}")


def verify(capture, publications, identity, server):
    """Bind each actual initialized displayed text frame to a published pose and independently check every sampled wind vector."""
    if not any(row['Version']==2 for row in publications.values()):
        return None
    path=capture/'view-owner.instruments.jsonl'
    assert path.exists(), 'Version-two acceptance requires actual rendered instrument text/source records.'
    wind=server['sampledWindState']; checked=0; maximum=0.0; distinct=set()
    for source in publications.values():
        assert source['Version']==2 and source['WindRevision']==wind['Revision'], 'This fixture requires one recorded wind revision.'
        expected=wind_at(source,wind,server['sampledWindSeed'])
        error=math.dist(expected,[source['WindX'],source['WindZ']]); maximum=max(maximum,error)
        assert error<1e-10
    for line in path.read_text(encoding='utf-8').splitlines():
        row=json.loads(line); source=row['source']
        if source['Version']==0:
            assert row['text']=='Waiting for validated server data'
            continue
        assert source==publications[identity,source['Tick']]
        assert row['text']==text_from(source), (source['Tick'],row['text'],text_from(source))
        checked+=1; distinct.add(row['text'])
    assert checked>1000 and len(distinct)>30
    return dict(checkedDisplayTexts=checked, distinctDisplayTexts=len(distinct),
        checkedPublishedWindVectors=len(publications), maximumWindDifferenceMps=maximum,
        meanings='Actual visible text/source, independent quaternion/airspeed/mass calculations; horizontal M01 wind and CoG datum, not radar altitude.')
