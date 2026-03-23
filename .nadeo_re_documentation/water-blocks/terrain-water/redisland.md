# RedIsland Water

RedIsland is the other environment that currently looks broad and practical for underwater-map work.

Like GreenCoast, it appears to support a much wider set of working water-adjacent blocks than BlueBay or WhiteShore.

## The basic system

RedIsland centers water on:

- `Water.EDFlat`
- `WaterClip.EDClip`

along with river-style local assets.

## Local water base

Key local carrier:

- `RedIsland\Media\Prefab\Zone\Water\Base.Prefab.Gbx`

Key materials:

- `Material\Water.Material.Gbx`
- `Material\LakeBottom.Material.Gbx`

## Deco river blocks

RedIsland also has local river-style classic blocks such as:

- `DecoRiverCurve2`
- `DecoRiverCurve3`
- `DecoRiverCurve4`
- `DecoRiverDiag2`
- `DecoRiverDiag3`
- `DecoRiverDiag4`

These matter because they show that local water content can be carried by block-prefab compositions.

## Practical working pattern

User testing showed a broad set of working blocks, including:

- `DecoLake*`
- `DecoRiver*`
- `Open*RoadStraightOnWaterHill*`
- `Platform*BaseOnWaterHill*`
- `Road*OnWaterHill*`
- `Road*OnWaterHillStraight*`

## Practical takeaway

If you want a broad working palette rather than a narrow proof-of-concept environment, RedIsland is one of the best current choices.
