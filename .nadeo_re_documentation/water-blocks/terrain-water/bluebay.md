# BlueBay Water

BlueBay is the clearest example of a strict vista water environment.

It is useful because it shows the difference between:

- blocks that look water-adjacent
- blocks that actually carry gameplay water reliably

## The basic system

BlueBay centers water on a sea-zone system:

- `Sea.EDFlat`
- `SeaClip.EDClip`
- `Sea.ZoneFlat`

This gives the environment its base sea topology.

## Local water base

Key local carrier:

- `BlueBay\Media\Prefab\Zone\Sea\Base.Prefab.Gbx`

Key materials:

- `Material\Water.Material.Gbx`
- `Material\SeaFloor.Material.Gbx`

## Why BlueBay matters

BlueBay is one of the environments where the simple structural rule works well:

- blocks that directly include the local zone water base prefab are the strongest candidates for gameplay water

That does not prove the entire system, but it is a very useful predictor here.

## Practical working pattern

The blocks that worked in testing were mostly the diagonal road-family shoreline blocks:

- `RoadTechDiagLeftStraightOnBeach`
- `RoadTechDiagRightStraightOnBeach`
- `RoadDirtDiagLeftStraightOnBeach`
- `RoadDirtDiagRightStraightOnBeach`
- `RoadBumpDiagLeftStraightOnBeach`
- `RoadBumpDiagRightStraightOnBeach`
- `RoadIceWithWallLeftDiagLeftStraightOnBeach`
- `RoadIceWithWallRightDiagLeftStraightOnBeach`
- `RoadIceWithWallLeftDiagRightStraightOnBeach`
- `RoadIceWithWallRightDiagRightStraightOnBeach`

The broader straight / platform / open-road families generally did not work.

## Practical takeaway

BlueBay is very useful for understanding the logic of water carriers, but it is not the broadest working environment for underwater-map experiments.
