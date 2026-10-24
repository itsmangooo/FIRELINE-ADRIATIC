"""Retain actual dedicated physics replay evidence and reject changes to the prior flight/fuel trajectories."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
from analyze_m03_fuel import read, verify


def main():
    """Capture one full raw run plus two real process outcomes, or revalidate the published archive without Unity."""
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture',type=Path)
    parser.add_argument('--evidence',type=Path,default=Path('docs/reports/m03-instrument-evidence'))
    args=parser.parse_args(); out=args.evidence; out.mkdir(parents=True,exist_ok=True)
    original=read(Path('docs/reports/m03-fuel-evidence/numerical/run-1.json.gz'))
    if args.capture:
        processes=read(args.capture/'process-summary.json')
        assert processes['passed'] and len(processes['runs'])==2
        runs=[read(args.capture/f'run-{i}.json.gz') for i in (1,2)]
        for run in runs:
            verify(run)
            assert run['cases']==original['cases'] and run['polar']==original['polar'] and run['fuel']==original['fuel']
        assert all(item['exitCode']==0 for item in processes['runs'])
        (out/'physics-regression.json.gz').write_bytes((args.capture/'run-1.json.gz').read_bytes())
        result=dict(passed=True,originalFlightTrajectoriesUnchanged=True,originalFuelTrajectoriesUnchanged=True,
            resourceSamplesPerRun=8800,retainedRawRun=1,processes=processes,
            retainedRawSha256=processes['runs'][0]['resultSha256'],
            kernelHashes={str(path):hashlib.sha256(path.read_bytes()).hexdigest() for path in sorted(Path('Assets/Fireline/Simulation').glob('*.cs'))},
            limits='Two actual current dedicated numerical processes; one full raw run retained here, second raw run remains at recorded ignored path. No airborne route, rendered/gameplay or CPU-cost qualification.')
        (out/'physics-regression.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    result=read(out/'physics-regression.json')
    raw=gzip.decompress((out/'physics-regression.json.gz').read_bytes())
    assert hashlib.sha256(raw).hexdigest().upper()==result['retainedRawSha256']
    recorded=json.loads(raw.decode('utf-8-sig')); verify(recorded)
    assert recorded['cases']==original['cases'] and recorded['fuel']==original['fuel'] and recorded['polar']==original['polar']
    print(json.dumps({key:result[key] for key in ('passed','originalFlightTrajectoriesUnchanged','originalFuelTrajectoriesUnchanged','resourceSamplesPerRun')}))


if __name__=='__main__':
    main()
