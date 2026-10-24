"""Render actual finite-fuel architecture and numerical samples as static SVG/PNG, without inventing flight imagery."""
import argparse
from pathlib import Path

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator

from analyze_m03_fuel import read
from render_m03_flight_view import save, timing


def flow(output, after):
    """Draw the actual previous frozen-fuel path and the installed atomic resource/physics path."""
    fig, ax = plt.subplots(figsize=(12, 3))
    ax.set(xlim=(0, 12), ylim=(0, 3)); ax.axis('off')
    labels = (['Owner input + fuel\nBounded 5 ms step', 'Fuel-limited throttle\nExisting contact/flight',
               'Burn + rebase CoG\nValidate full candidate', 'Atomic server install\nFuel in read-only RPC'] if after else
              ['Owner input\nBounded 5 ms step', 'Existing contact/flight\nThrust ignores fuel',
               'Frozen fuel mass/CoG\nNo resource debit', 'Server install\nRead-only pose RPC'])
    ax.set_title('AFTER: DEBUG_FUEL_V1 — finite fuel, preserved body frame' if after else
                 'BEFORE: 146297c — frozen fuel, unlimited powered thrust', fontsize=12)
    for i, label in enumerate(labels):
        x = 1.5+3*i
        ax.text(x, 1.5, label, ha='center', va='center', fontsize=9,
                bbox=dict(boxstyle='round,pad=.6', facecolor='#e8f1f8', edgecolor='#38526b'))
        if i < 3:
            ax.annotate('', xy=(x+1.85, 1.5), xytext=(x+1.1, 1.5), arrowprops=dict(arrowstyle='->', lw=1.8))
    save(fig, output, 'after' if after else 'before')


def numerical(evidence, output):
    """Plot only recorded headless physical samples, with an explicit mean-throttle depletion zoom."""
    cases = {case['name']: case for case in read(evidence/'run-1.json.gz')['fuel']['cases']}
    fig, axes = plt.subplots(3, 1, figsize=(10, 9))
    for name, label in [('before_full', 'BEFORE: full, frozen'), ('full', 'AFTER: full'), ('half', 'AFTER: half'), ('off', 'AFTER: off')]:
        case = cases[name]
        axes[0].plot([0]+[r['seconds'] for r in case['samples']], [case['initial']['FuelKg']]+[r['state']['FuelKg'] for r in case['samples']], label=label)
    axes[0].set(title='Matched 400 kg initial fuel; throttle-proportional burn', ylabel='Fuel (kg)', xlabel='Physical seconds from case initial state')
    axes[0].xaxis.set_major_locator(MultipleLocator(1)); axes[0].yaxis.set_major_locator(MultipleLocator(.25))
    for name, label in [('before_starved', 'BEFORE: fuel ignored'), ('starved', 'AFTER: 0.000625 kg fuel')]:
        case = cases[name]
        times = [r['seconds']-case['dt'] for r in case['samples'][:5]]
        axes[1].step(times, [r['effectiveThrottle'] for r in case['samples'][:5]], where='post', label=label)
        axes[2].plot([0]+[r['seconds'] for r in case['samples']], [60]+[r['forces']['AirspeedMps'] for r in case['samples']], label=label)
    axes[1].set(title='Final-fuel zoom: mean throttle over each 5 ms integration interval',
        ylabel='Effective throttle (0–1)', xlabel='Physical seconds from case initial state', xlim=(0, .02), ylim=(-.05, 1.1))
    axes[1].xaxis.set_major_locator(MultipleLocator(.005)); axes[1].yaxis.set_major_locator(MultipleLocator(.25))
    axes[2].set(title='Same starved initial state/input, four seconds; uncalibrated debug aircraft', ylabel='Airspeed (m/s)', xlabel='Physical seconds from case initial state')
    axes[2].xaxis.set_major_locator(MultipleLocator(1)); axes[2].yaxis.set_major_locator(MultipleLocator(2))
    for ax in axes:
        ax.grid(alpha=.25); ax.legend(fontsize=8)
    save(fig, output, 'numerical')


def main():
    """Generate inspectable drawings from published raw numerical and rendered-runtime evidence."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--evidence', type=Path, default=Path('docs/reports/m03-fuel-evidence'))
    parser.add_argument('--output', type=Path, default=Path('docs/physics/aircraft-fuel'))
    args = parser.parse_args(); args.output.mkdir(parents=True, exist_ok=True)
    plt.rcParams['svg.fonttype'] = 'none'
    flow(args.output, False); flow(args.output, True)
    numerical(args.evidence/'numerical', args.output)
    timing(args.evidence/'runtime', args.output)


if __name__ == '__main__':
    main()
