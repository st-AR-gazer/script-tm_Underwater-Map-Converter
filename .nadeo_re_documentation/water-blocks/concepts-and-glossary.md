# Concepts and Glossary

This file defines the main ideas and words used across the water docs.

The goal is not perfect academic precision. The goal is to make the rest of the doc set much easier to read.

## Core layers

### Visual water

This means:

- something looks like water
- it uses water-looking geometry, materials, or shading

Important:

- visual water does not automatically mean gameplay water

### Engine water rendering

This means:

- the game is using its real water-rendering pipeline
- reflection/refraction/Fresnel/normal resources are involved

This is usually tied to:

- `Water.Material.Gbx`
- the shared Techno3 water material chain

### Gameplay water

This means:

- the car is affected
- the player gets the water-on-car / wet-wheels behavior

This is the hardest layer to pin down exactly.

## Important asset types

### BlockInfo

A `BlockInfo` is the authored definition of a block family.

Common kinds:

- `*.EDClassic.Gbx`
- `*.EDFlat.Gbx`
- `*.EDFrontier.Gbx`
- `*.EDTransition.Gbx`
- `*.EDClip.Gbx`

Think of BlockInfo as:

- "what kind of block is this?"
- "what variants, clips, and payloads does it use?"

### Clip

A clip is part of the block connection system.

Roughly:

- clips help determine what happens at joins, borders, and compatible neighbors
- water blocks often have special water-specific clips

### Mobil

In this doc set, "mobil" usually means the payload choice inside a variant row.

Practically, the main question is:

- which prefab does this variant actually instantiate?

### Prefab

A prefab is a composition graph.

It can contain:

- nested prefabs
- meshes
- solids
- materials
- transform offsets

Very often, the real answer to "why does this block behave like that?" lives in the prefab graph.

### Ref table

The ref table is the quick list of file references inside a GBX.

It is useful because:

- it shows what other assets a file depends on
- it often reveals whether a block/prefab references water materials or local water base prefabs

## The two big water worlds

### Stadium authored water

This is the explicit Stadium water family:

- `WaterBase`
- `RoadWater*`
- `PlatformWater*`
- `TrackWallWater*`

This world is very authored and clip-heavy.

### Vista / terrain water

This is the water system used by:

- BlueBay
- GreenCoast
- RedIsland
- WhiteShore

This world uses a mix of:

- zone flats
- frontiers
- transitions
- decoration slabs
- local shoreline / river / rocky / lake blocks

## Global slab vs local carrier

### Global water slab

This means:

- the environment itself supplies a large shared water volume/plane
- the slab is controlled by decoration/scene assets

### Local water carrier

This means:

- a block or prefab carries water with it
- the water is local to that block composition

Examples:

- some `DecoLake*` blocks
- some `DecoRiver*` blocks
- some `*OnBeach*`, `*OnLakeShore*`, `*OnWaterHill*`, `*OnWaterShore1*` blocks

## The top-level local-water-base rule

One of the most useful current ideas is:

- check whether the top-level prefab directly includes the local zone water base prefab

Examples:

- BlueBay local base: `Zone\Sea\Base.Prefab.Gbx`
- GreenCoast local base: `Zone\Lake\Base.Prefab.Gbx`
- RedIsland / WhiteShore local base: `Zone\Water\Base.Prefab.Gbx`

Why it matters:

- in BlueBay and WhiteShore, this is the strongest simple predictor of whether a block applies water on the car

Why it is not the whole story:

- GreenCoast and RedIsland seem broader, so deeper prefabs likely matter too

## Custom `.Block.Gbx` wrapper

This is not a full new block definition.

In the current model, a wrapper is usually:

- a `CGameItemModel`
- whose `EntityModelEdition` is a `CGameBlockItem`
- that points at an existing archetype

Important consequence:

- most wrappers do not define their own BlockInfo -> Prefab graph
- they usually redirect to an existing block archetype

## Crystal / CustomizedVariants

These are the mysterious custom-wrapper payload areas.

What matters for now:

- some wrappers seem to be mostly link-only
- some evidence suggests a `Crystal` can carry real geometry

This is promising, but still an active research area.
