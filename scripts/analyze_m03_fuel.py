"""Independently check fuel bookkeeping, starvation thrust, CoG-frame preservation and exact dedicated-player replay."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path

from analyze_m03_flight import verify as verify_legacy


def read(path):
    """Decode a recorded JSON file or its lossless gzip archive."""
    raw = path.read_bytes()
    return json.loads((gzip.decompress(raw) if path.suffix == '.gz' else raw).decode('utf-8-sig'))


def vector(value):
    """Read a recorded SI vector in its original XYZ order."""
    return [value[key] for key in ('X', 'Y', 'Z')]


def cross(a, b):
    """Compute the right-handed cross product independently of the C# math implementation."""
    return [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]]


def rotate(q, v):
    """Apply a recorded unit quaternion using the independent vector cross-product identity."""
    xyz = [q[key] for key in ('X', 'Y', 'Z')]
    t = [2*x for x in cross(xyz, v)]
    return [a+q['W']*b+c for a, b, c in zip(v, t, cross(xyz, t))]


def cg(state):
    """Derive the debug center of gravity from the actual fuel/water point masses in meters."""
    return (-.8*state['FuelKg']+1.2*state['WaterKg'])/(10000+state['FuelKg']+state['WaterKg'])


def verify(data):
    """Check every resource step and rebase, existing flight invariants, and physical differences against matched before cases."""
    legacy = verify_legacy(data)
    proof = data['fuel']
    assert proof['profile'] == 'DEBUG_FUEL_V1' and proof['fullThrottleKgPerSecond'] == .25
    assert len(proof['cases']) == 10
    summaries = []
    checked = 0
    max_position = max_velocity = max_mass = 0.0
    for case in proof['cases']:
        previous = case['initial']
        assert len(case['samples']) == round(4/case['dt'])
        consumed = 0.0
        for index, row in enumerate(case['samples'], 1):
            assert math.isclose(row['seconds'], index*case['dt'], abs_tol=1e-12)
            state, unrebased, force = row['state'], row['beforeRebase'], row['forces']
            demand = 0 if case['legacy'] else .25*case['input']['Throttle']*case['dt']
            burned = min(previous['FuelKg'], demand)
            effective = case['input']['Throttle'] if case['legacy'] else (case['input']['Throttle']*burned/demand if demand else 0)
            assert abs(row['requestedKg']-demand) < 1e-15 and abs(row['burnedKg']-burned) < 1e-15
            assert abs(row['effectiveThrottle']-effective) < 1e-12
            assert abs(state['FuelKg']-(previous['FuelKg']-burned)) < 1e-12
            assert state['WaterKg'] == case['initial']['WaterKg'] and 0 <= state['FuelKg'] <= previous['FuelKg']
            assert unrebased['FuelKg'] == previous['FuelKg']
            assert state['Attitude'] == unrebased['Attitude'] and state['AngularVelocityRadps'] == unrebased['AngularVelocityRadps']
            delta = [0, 0, cg(state)-cg(unrebased)]
            position_shift = rotate(state['Attitude'], delta)
            velocity_shift = rotate(state['Attitude'], cross(vector(state['AngularVelocityRadps']), delta))
            pos_error = math.dist(vector(state['PositionM']), [a+b for a, b in zip(vector(unrebased['PositionM']), position_shift)])
            vel_error = math.dist(vector(state['VelocityMps']), [a+b for a, b in zip(vector(unrebased['VelocityMps']), velocity_shift)])
            max_position = max(max_position, pos_error); max_velocity = max(max_velocity, vel_error)
            assert pos_error < 1e-10 and vel_error < 1e-10
            total = 10000+state['FuelKg']+state['WaterKg']
            assert force['Mass']['Kg'] == total and abs(force['Mass']['CgZ']-cg(state)) < 1e-12
            shift = 10000*cg(state)**2+state['FuelKg']*(-.8-cg(state))**2+state['WaterKg']*(1.2-cg(state))**2
            assert abs(force['Mass']['InertiaKgM2']['X']-50000-shift) < 1e-8
            assert abs(force['Mass']['InertiaKgM2']['Y']-100000-shift) < 1e-8 and force['Mass']['InertiaKgM2']['Z'] == 120000
            speed = math.dist(vector(state['VelocityMps']), [0, 0, 0])
            expected_thrust = 40000*effective*case['densityKgM3']/1.225*max(.25, 1-speed/150)
            assert abs(force['ThrustN']-expected_thrust) < 1e-8
            assert abs(sum(state['Attitude'][key]**2 for key in ('X', 'Y', 'Z', 'W'))-1) < 1e-12
            consumed += burned
            max_mass = max(max_mass, abs(case['initial']['FuelKg']-state['FuelKg']-consumed))
            assert abs(case['initial']['FuelKg']-state['FuelKg']-consumed) < 1e-8
            previous = state; checked += 1
        last = case['samples'][-1]
        summaries.append({'case': case['name'], 'samples': len(case['samples']), 'dtSeconds': case['dt'],
            'fuelKg': previous['FuelKg'], 'consumedKg': consumed, 'massKg': last['forces']['Mass']['Kg'],
            'cgZM': cg(previous), 'inertiaXKgM2': last['forces']['Mass']['InertiaKgM2']['X'],
            'northM': previous['PositionM']['Z'], 'heightM': previous['PositionM']['Y'], 'speedMps': last['forces']['AirspeedMps']})
    cases = {case['name']: case for case in proof['cases']}
    assert cases['full']['initial'] == cases['before_full']['initial'] and cases['starved']['initial'] == cases['before_starved']['initial']
    for old, new in (('before_full', 'full'), ('before_starved', 'starved')):
        assert cases[old]['dt'] == cases[new]['dt'] and cases[old]['input'] == cases[new]['input']
    assert cases['starved']['samples'][0]['effectiveThrottle'] == .5
    assert all(row['effectiveThrottle'] == 0 and row['forces']['ThrustN'] == 0 for row in cases['starved']['samples'][1:])
    assert all(row['forces']['ThrustN'] == 0 for row in cases['empty']['samples'])
    summary = {row['case']: row for row in summaries}
    assert summary['full']['consumedKg'] > summary['half']['consumedKg'] > 0 and summary['off']['consumedKg'] == 0
    assert summary['before_starved']['speedMps'] > summary['starved']['speedMps']
    convergence = abs(summary['full']['northM']-summary['fine']['northM'])
    assert convergence < .1
    return {'checkedResourceSamples': checked, 'caseSummaries': summaries, 'legacyCaseSummaries': legacy,
        'maximumRebasePositionDifferenceM': max_position, 'maximumRebaseVelocityDifferenceMps': max_velocity,
        'maximumFuelBalanceErrorKg': max_mass, 'fullVsFineNorthDifferenceM': convergence}


def main():
    """Capture two actual process outputs once or verify committed archives and bind every raw byte to the recorded process hashes."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', type=Path)
    parser.add_argument('--evidence', type=Path, default=Path('docs/reports/m03-fuel-evidence/numerical'))
    args = parser.parse_args(); out = args.evidence; out.mkdir(parents=True, exist_ok=True)
    if args.capture:
        for run in (1, 2):
            (out/f'run-{run}.json.gz').write_bytes(gzip.compress((args.capture/f'run-{run}.json').read_bytes(), mtime=0))
    processes = read(out/'process-summary.json')
    assert processes['passed'] and len(processes['runs']) == 2 and len({r['pid'] for r in processes['runs']}) == 2
    runs = []
    for record in processes['runs']:
        assert record['exitCode'] == 0
        raw = gzip.decompress((out/f"run-{record['run']}.json.gz").read_bytes())
        assert hashlib.sha256(raw).hexdigest().upper() == record['resultSha256']
        runs.append(json.loads(raw.decode('utf-8-sig')))
    results = [verify(data) for data in runs]
    assert runs[0]['fuel'] == runs[1]['fuel'] and runs[0]['cases'] == runs[1]['cases'] and results[0] == results[1]
    original = read(Path('docs/reports/m03-flight-evidence/run-1.json.gz'))
    assert original['cases'] == runs[0]['cases'] and original['polar'] == runs[0]['polar']
    result = dict(passed=True, exactTwoProcessReplay=True, originalFlightTrajectoriesUnchanged=True, **results[0])
    (out/'verification.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({'passed': True, 'checkedResourceSamplesPerProcess': result['checkedResourceSamples'],
        'maximumFuelBalanceErrorKg': result['maximumFuelBalanceErrorKg'], 'fullVsFineNorthDifferenceM': result['fullVsFineNorthDifferenceM']}))


if __name__ == '__main__':
    main()
