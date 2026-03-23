# WhiteShore Water

WhiteShore is one of the most important environments in the research because it demonstrates all of these at once:

- a global vista water slab
- local rocky-water blocks
- hybrid shoreline carriers
- a strict split between "looks watery" and "actually affects the car"

## The basic system

WhiteShore uses:

- `Water.EDFlat`
- `WaterClip.EDClip`

And it also has the famous vista-wide water slab through decoration assets.

That is why WhiteShore is such a useful study case:

- it shows both environment-level water and block-level water in the same environment

## Local water base

Key local carrier:

- `WhiteShore\Media\Prefab\Zone\Water\Base.Prefab.Gbx`

## Rocky shore system

WhiteShore also has a rocky shore family, including:

- rocky shoreline clips
- `DecoTerrainRocky`

This matters because `DecoTerrainRocky` proves that a normal classic block can carry local water geometry through its prefab payload.

## Hybrid `*OnWaterShore1*` blocks

WhiteShore also has the shoreline hybrid family:

- `RoadTechDiagLeftStraightOnWaterShore1`
- `RoadTechDiagRightStraightOnWaterShore1`
- `RoadDirtDiag*OnWaterShore1`
- `RoadBumpDiag*OnWaterShore1`
- `RoadIceWithWall*OnWaterShore1`

Some of these became the strongest map-only water carriers used later in the converter work.

## Practical working pattern

WhiteShore behaved strictly in testing.

Working blocks were mainly:

- diagonal shoreline road families
- `DecoTerrainRocky`

The broader straight / platform / open-road families generally did not work.

## Practical takeaway

WhiteShore is excellent for understanding the problem.

It is less ideal than GreenCoast or RedIsland if your only goal is "give me the broadest working water-block palette possible".
