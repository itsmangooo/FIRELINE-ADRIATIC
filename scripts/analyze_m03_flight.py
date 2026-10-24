"""Verify actual headless flight artifacts independently, retain losslessly compressed raw data, and derive numerical/performance summaries."""
import argparse
import csv
import gzip
import hashlib
import io
import json
import math
from pathlib import Path

OUT = Path('docs/reports/m03-flight-evidence')


def read(path):
    """Decode UTF-8 JSON from an actual file; gzip is a lossless archive wrapper, not generated simulation evidence."""
    raw = path.read_bytes()
    return json.loads((gzip.decompress(raw) if path.suffix == '.gz' else raw).decode('utf-8-sig'))


def length(vector):
    """Compute a three-component SI vector magnitude independently of the C# solver."""
    return math.sqrt(sum(vector[k] ** 2 for k in ('X', 'Y', 'Z')))


def verify(data):
    """Check every recorded mass/CoG/inertia, unit attitude, airspeed, dynamic pressure, drag-power sign and analytic vacuum trajectory."""
    assert data['passed'] and data['headless'] and data['profile'] == 'DEBUG_FLIGHT_ONLY_V1'
    assert len(data['cases']) == 9 and len(data['polar']) == 31 and len(data['stepMs']) == 1000
    result=[]
    for case in data['cases']:
        assert len(case['samples']) == round((2 if case['name'] == 'controls' else 4) / case['dt'])
        for index, row in enumerate(case['samples'], 1):
            assert math.isclose(row['seconds'], index * case['dt'], abs_tol=1e-12)
            state=row['state']; force=row['forces']; mass=force['Mass']
            total=10000+state['FuelKg']+state['WaterKg']; cg=(-.8*state['FuelKg']+1.2*state['WaterKg'])/total
            assert mass['Kg'] == total and math.isclose(mass['CgZ'],cg,abs_tol=1e-12)
            shift=10000*cg**2+state['FuelKg']*(-.8-cg)**2+state['WaterKg']*(1.2-cg)**2
            assert math.isclose(mass['InertiaKgM2']['X'],50000+shift,abs_tol=1e-8)
            assert math.isclose(mass['InertiaKgM2']['Y'],100000+shift,abs_tol=1e-8)
            assert mass['InertiaKgM2']['Z'] == 120000
            q=state['Attitude']; assert abs(sum(q[k]**2 for k in ('X','Y','Z','W'))-1)<1e-12
            relative={k:state['VelocityMps'][k]-row['windMps'][k] for k in ('X','Y','Z')}
            speed=length(relative); assert math.isclose(speed,force['AirspeedMps'],rel_tol=1e-12)
            assert math.isclose(force['DynamicPressurePa'],.5*case['densityKgM3']*speed**2,rel_tol=1e-12,abs_tol=1e-10)
            assert sum(force['AerodynamicWorldN'][k]*relative[k] for k in relative) <= 1e-6
            assert length(state['VelocityMps']) <= 500 and length(state['AngularVelocityRadps']) <= 10
            if case['densityKgM3']==0:
                t=row['seconds']; dt=case['dt']
                assert abs(state['VelocityMps']['Y']+9.80665*t)<1e-8
                assert abs(state['PositionM']['Y']-(1000-.5*9.80665*t*(t+dt)))<1e-8
                assert abs(state['PositionM']['Z']-60*t)<1e-8
        last=case['samples'][-1]
        result.append({'case':case['name'],'samples':len(case['samples']),'dtSeconds':case['dt'],
            'endHeightM':last['state']['PositionM']['Y'],'endEastM':last['state']['PositionM']['X'],
            'endNorthM':last['state']['PositionM']['Z'],'endAirspeedMps':last['forces']['AirspeedMps'],
            'massKg':last['forces']['Mass']['Kg'],'cgZM':last['forces']['Mass']['CgZ']})
    for row in data['polar']:
        alpha=row['alphaDegrees']*math.pi/180; stall=14*math.pi/180
        edge=max(-stall,min(stall,alpha)); decay=max(.15,1-max(0,abs(alpha)-stall)/.6)
        assert math.isclose(row['cleanCl'],(.2+4.5*edge)*decay,rel_tol=1e-12,abs_tol=1e-12)
        assert math.isclose(row['flapsCl'],(.6+4.5*edge)*decay,rel_tol=1e-12,abs_tol=1e-12)
    return result


def main():
    """Capture two real process files once or re-verify the archived committed files; exclude only timing/runtime fields from replay equality."""
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--capture',type=Path); args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    if args.capture:
        for run in (1,2):
            for suffix in ('.json','.csv'):
                source=args.capture/f'run-{run}{suffix}'; raw=source.read_bytes()
                (OUT/f'run-{run}{suffix}.gz').write_bytes(gzip.compress(raw,mtime=0))
    runs=[read(OUT/f'run-{run}.json.gz') for run in (1,2)]
    summaries=[verify(data) for data in runs]
    assert summaries[0]==summaries[1] and runs[0]['cases']==runs[1]['cases'] and runs[0]['polar']==runs[1]['polar']
    processes=read(OUT/'process-summary.json'); assert processes['passed'] and len(set(r['pid'] for r in processes['runs']))==2
    csv_json_height_difference=0
    assert gzip.decompress((OUT/'run-1.csv.gz').read_bytes()) == gzip.decompress((OUT/'run-2.csv.gz').read_bytes())
    for record in processes['runs']:
        assert record['exitCode']==0
        for suffix,field in (('.json','resultSha256'),('.csv','csvSha256')):
            raw=gzip.decompress((OUT/f"run-{record['run']}{suffix}.gz").read_bytes())
            assert hashlib.sha256(raw).hexdigest().upper()==record[field]
        rows=list(csv.DictReader(io.StringIO(gzip.decompress((OUT/f"run-{record['run']}.csv.gz").read_bytes()).decode('utf-8-sig'))))
        expected=[(case,row) for case in runs[record['run']-1]['cases'] for row in case['samples']]
        assert len(rows)==len(expected)
        for csv_row,(case,row) in zip(rows,expected):
            # Unity JsonUtility and Mono's round-trip decimal formatter can differ by one parsed-double ULP.
            difference=abs(float(csv_row['height_m'])-row['state']['PositionM']['Y'])
            csv_json_height_difference=max(csv_json_height_difference,difference)
            assert csv_row['case']==case['name'] and difference<=1e-10
            assert math.isclose(float(csv_row['seconds']),row['seconds'],rel_tol=0,abs_tol=1e-12)
            assert float(csv_row['mass_kg'])==row['forces']['Mass']['Kg']
    timings=[]
    for data in runs:
        values=sorted(data['stepMs']); assert all(math.isfinite(v) and v>=0 for v in values)
        timings.append({'calls':len(values),'warmupCalls':data['timingWarmupCalls'],'p50Ms':values[499],
            'p95Ms':values[949],'p99Ms':values[989],'maxMs':values[-1],'gc0Collections':data['gc0Collections']})
    physical_bytes=json.dumps(runs[0]['cases'],sort_keys=True,separators=(',',':')).encode()
    summary={'passed':True,'exactSameRuntimeReplay':True,'exactCsvReplay':True,'maximumJsonCsvHeightDifferenceM':csv_json_height_difference,
        'trajectorySha256':hashlib.sha256(physical_bytes).hexdigest(),
        'caseSummaries':summaries[0],'timingRuns':timings,'source':'Two actual Windows dedicated-player processes; pure numerical debug model, no meshes/networked aircraft'}
    (OUT/'independent-verification.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
    print('PASS: exact two-process trajectory replay, all per-sample mass/attitude/flow checks, analytic vacuum integration, and raw CSV/SHA verification.')


if __name__=='__main__': main()
