# `RoadIceWithWallLeftDiagLeftStraight*` Water Carrier Bounds

This file isolates the most useful "clip-removable" carrier family:

- `WhiteShore\RoadIceWithWallLeftDiagLeftStraightOnWaterShore1`
- `BlueBay\RoadIceWithWallLeftDiagLeftStraightOnBeach`
- `GreenCoast\RoadIceWithWallLeftDiagLeftStraightOnLakeShore`
- `RedIsland\RoadIceWithWallLeftDiagLeftStraightOnWaterHill`

The goal is to separate:

- the full block / prefab footprint
- the top-level local water-carrier footprint

## Example map anchor

Map:

- `C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\example block placement roadice example no clips.Map.Gbx`

Observed repeat vector in the working no-clip pattern:

- `DeltaCoord = <-1, 0, -2>`

That is the concrete line step the later map experiments were built around.

## Shared top-level water layout

All four candidate prefabs have the same top-level water layout:

- local water base at `<0, 0, 32>`
- local water base at `<32, 0, 32>`
- local water base at `<64, 0, 32>`

So at top level, each block carries three adjacent `32x32` water-base tiles.

For North orientation, the top-level water-base union is therefore:

- `X = 0 .. 96`
- `Z = 32 .. 64`

In block-cell terms, that is a `3x1` strip offset one cell south from the prefab origin.

## Full prefab bounds vs water-base union

The important practical idea is:

- the full block footprint is larger than the useful water strip

That is why simple butt-joined tiling leaves gaps, and why overlap matters.

### WhiteShore

Full prefab bounds:

- `X = 0 .. 96`
- `Y = about 2 .. 35`
- `Z = 0 .. 64`

Water-base union:

- `X = 0 .. 96`
- `Y = about 2 .. 7`
- `Z = 32 .. 64`

### BlueBay

Full prefab bounds:

- `X = 0 .. 96`
- `Y = about 4 .. 35`
- `Z = 0 .. 64`

Water-base union:

- `X = 0 .. 96`
- `Y = about 4 .. 7`
- `Z = 32 .. 64`

### GreenCoast

Full prefab bounds:

- `X = 0 .. 96`
- `Y = about 2 .. 35`
- `Z = 0 .. 64`

Water-base union:

- `X = 0 .. 96`
- `Y = about 2 .. 7.2`
- `Z = 32 .. 64`

### RedIsland

Full prefab bounds:

- `X = 0 .. 96`
- `Y = about 2 .. 35`
- `Z = 0 .. 64`

Water-base union:

- `X = 0 .. 96`
- `Y = about 2 .. 7.7`
- `Z = 32 .. 64`

## Practical placement implications

- The useful top-level water strip only fills part of the full block footprint.
- This is why the later converter work needed overlap and sheet/volume placement rather than simple single-line placement.
- It also explains why the carrier blocks can be hidden or clipped away in useful ways while the water effect still remains.
