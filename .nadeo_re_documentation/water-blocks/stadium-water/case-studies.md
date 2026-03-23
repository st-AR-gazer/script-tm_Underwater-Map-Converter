# Stadium Water Case Studies

This file records the main representative Stadium water blocks that were used to build the current mental model.

## `WaterBase`

Representative file:

- `Stadium\GameCtnBlockInfo\GameCtnBlockInfoClassic\WaterBase.EDClassic.Gbx`

Why it matters:

- this is the clearest "central water tile" style block in the Stadium family
- it is one of the strongest proofs that Stadium water is explicitly authored as its own block family

Key observations:

- water-specific clips are referenced
- the mobil payload points into the `Media\Prefab\Water\...` family

Practical meaning:

- `WaterBase` is not just a mesh with a water material
- it sits inside a water-specific clip/prefab ecosystem

## `RoadWaterStraight`

Representative file:

- `Stadium\GameCtnBlockInfo\GameCtnBlockInfoClassic\RoadWaterStraight.EDClassic.Gbx`

Why it matters:

- it proves that Stadium "road water" is an authored family, not a normal road with a water material swap

Key observations:

- water-specific clips
- water-related prefabs under `Media\Prefab\RoadWater\...`

## `PlatformWaterBase`

Representative file:

- `Stadium\GameCtnBlockInfo\GameCtnBlockInfoClassic\PlatformWaterBase.EDClassic.Gbx`

Why it matters:

- it shows the same authored-water pattern exists outside roads

## `TrackWallWaterStraight`

Representative file:

- `Stadium\GameCtnBlockInfo\GameCtnBlockInfoClassic\TrackWallWaterStraight.EDClassic.Gbx`

Why it matters:

- it shows that the water family extends into wall/border structures too

## Main takeaway

Across these cases, the important conclusion is:

- Stadium water is a dedicated authored system of BlockInfos, clips, clip-prefabs, and water-related payloads

That is why Stadium water is harder to port than a simple prefab/material transplant would suggest.
