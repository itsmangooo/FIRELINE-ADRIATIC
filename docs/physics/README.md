# Physics implementation comparisons

The [M00 fixed tick clock comparison](foundation-clock/COMPARISON.md) documents the initial scheduling utility and its unit assertions. No flight, fire, water or wind physics model exists yet. The [simulation specification](../SIMULATION_SPEC.md) and [aircraft specifications](../AIRCRAFT_SPECIFICATIONS.md) remain proposed designs.

For each significant physics change, create `<system-name>/COMPARISON.md` from the [physics template](../templates/PHYSICS_COMPARISON.md). Include actual old/new mathematics, SI variables, two diagrams, equivalent numerical cases, source references, verification and plots when needed. Keep raw data and plot generation instructions with the report. Link completed reports here and summarize them in root README. Never invent an old model for a newly implemented system.
