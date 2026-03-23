# Water Blocks (TM2020) - Documentation Index

This doc set is the water-focused reverse-engineering track for Trackmania 2020.

The core question is:

- what makes something in TM2020 behave like water?

That question turns into a few practical sub-questions:

- why do some blocks look watery but not affect the car?
- why do some blocks apply the water effect outside the normal vista slab?
- what has to survive when porting water-related blocks across environments?
- how far can we push map-only underwater conversion?

## Best entry points

If you are new, read these first:

1. `water-blocks/start-here.md`
2. `water-blocks/concepts-and-glossary.md`
3. `water-blocks/reading-paths.md`
4. `water-blocks/current-status.md`

Those four files give the quickest path to a useful mental model.

## Core docs

- `water-blocks/start-here.md`
- `water-blocks/concepts-and-glossary.md`
- `water-blocks/reading-paths.md`
- `water-blocks/overview.md`
- `water-blocks/current-status.md`
- `water-blocks/research-method.md`
- `water-blocks/open-questions.md`

## Stadium water

- `water-blocks/stadium-water/case-studies.md`
- `water-blocks/stadium-water/water-clip-system.md`

## Terrain / vista water

- `water-blocks/terrain-water/bluebay.md`
- `water-blocks/terrain-water/greencoast.md`
- `water-blocks/terrain-water/redisland.md`
- `water-blocks/terrain-water/whiteshore.md`

## Materials / shaders / moods

- `water-blocks/materials-and-shaders/water-material-chain.md`
- `water-blocks/materials-and-shaders/moods-and-water-color.md`

## Comparisons

- `water-blocks/comparisons/stadium-vs-terrain-water.md`
- `water-blocks/comparisons/vista-on-water-block-inventory.md`
- `water-blocks/comparisons/water-asset-matrix.md`
- `water-blocks/comparisons/water-effect-working-vs-not-working.txt`
- `water-blocks/comparisons/roadice-diagleft-water-carrier-bounds.md`

## Custom wrappers

- `water-blocks/custom-block-wrappers/overview.md`
- `water-blocks/custom-block-wrappers/wrapper-structure.md`
- `water-blocks/custom-block-wrappers/customizedvariants-and-crystal.md`
- `water-blocks/custom-block-wrappers/experiment-results.md`
- `water-blocks/custom-block-wrappers/crystal-editing-hypotheses.md`
- `water-blocks/custom-block-wrappers/map-only-custom-visual-feasibility.md`

## Investigation briefs / prompts

- `water-blocks/investigations/water-blocks-rd-brief.md`
- `water-blocks/investigations/custom-block-wrapper-crystal-rd-brief.md`

## Porting

- `water-blocks/porting/stadium-to-nonstadium-water-porting-notes.md`
