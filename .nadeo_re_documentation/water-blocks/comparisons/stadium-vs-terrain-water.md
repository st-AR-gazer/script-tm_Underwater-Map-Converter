# Stadium Water vs Terrain/Vista Water

This is the practical comparison doc for the two main water worlds in TM2020:

- Stadium's explicit authored water block family
- the terrain/vista water systems used by BlueBay, GreenCoast, RedIsland, and WhiteShore

## Where waterness is authored

### Stadium

Stadium water is authored primarily as:

- dedicated BlockInfo families
- water-specific clips
- water-specific prefab families

In other words:

- Stadium water is an authored block family
- not just a generic water material applied to random road pieces

### Vista / terrain environments

Vista water is authored primarily as:

- zone flats
- frontier / transition systems
- environment decoration slabs
- local shoreline / river / rocky / lake prefabs

In other words:

- terrain water is topology- and environment-driven first
- not just a simple port of Stadium's explicit water family

## Shared layer

The two worlds are still connected by the shared water material / shader system.

That is why they can look related even though the asset graphs are structured differently.

## Practical takeaway

The most important difference is:

- Stadium water behaves like a dedicated authored family
- terrain/vista water behaves like a layered environment system with some local carriers

That difference is exactly why porting and underwater-map conversion are tricky.
