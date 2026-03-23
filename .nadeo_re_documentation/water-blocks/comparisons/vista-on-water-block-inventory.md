# Vista `OnWater*` / `OnBeach*` / `OnLakeShore*` Block Inventory

This file is the structural inventory of the shoreline-style hybrid block families across the four vista environments.

The main question it tracks is:

- does the block's top-level prefab directly include the local zone water base prefab?

Why that matters:

- in BlueBay and WhiteShore, this is the strongest simple predictor of whether the block really applies gameplay water on the car

## Local water base prefab by environment

- BlueBay: `BlueBay\Media\Prefab\Zone\Sea\Base.Prefab.Gbx`
- GreenCoast: `GreenCoast\Media\Prefab\Zone\Lake\Base.Prefab.Gbx`
- RedIsland: `RedIsland\Media\Prefab\Zone\Water\Base.Prefab.Gbx`
- WhiteShore: `WhiteShore\Media\Prefab\Zone\Water\Base.Prefab.Gbx`

## High-level pattern

Current broad pattern:

- diagonal road families usually do include the local zone water base prefab
- straight road families usually do not
- platform families usually do not
- open-road families usually do not

Important caveat:

- this inventory is structural, not final gameplay proof
- GreenCoast and RedIsland behave more broadly in testing than the top-level check alone would predict

## Practical use

Use this file together with:

- `water-blocks/comparisons/water-effect-working-vs-not-working.txt`

That pair gives:

- structure on one side
- gameplay result on the other

## Practical shortlist

Especially strong candidates in the strict environments:

- `RoadTechDiagLeftStraightOnBeach`
- `RoadTechDiagRightStraightOnBeach`
- `RoadIceWithWallLeftDiagLeftStraightOnBeach`
- `RoadTechDiagLeftStraightOnWaterShore1`
- `RoadTechDiagRightStraightOnWaterShore1`
- `RoadIceWithWallLeftDiagLeftStraightOnWaterShore1`

These are not the whole list, but they are the family of examples that mattered most in later underwater-map experiments.
