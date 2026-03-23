# Water Blocks Overview (TM2020)

This file is the big-picture version of the water research.

It tries to answer:

- what the major water systems are
- how they relate to each other
- why underwater-map work becomes tricky so fast

## Main idea

In TM2020, "water" is not one mechanic.

The current best model is that water behavior is split across three layers:

1. water visuals
2. engine water rendering
3. gameplay water

Those layers often appear together, but not always.

That is why:

- some blocks look watery but do not affect the car
- some environments are stricter than others
- map-only underwater conversion is harder than a material swap

## The three big water systems

### 1) Shared water material / shader system

Across Stadium and the vista environments, `Water.Material.Gbx` repeatedly points into the same shared Techno3 water chain:

- `Techno3\Media\Material\Tech3_Water_MultiH.Material.gbx`
- `Techno3\Media\Texture\SeaRenderPlaneId.Texture.Gbx`
- `Techno3\Media\Texture\SeaRenderReflection.Texture.Gbx`
- `Techno3\Media\Texture\SeaRenderRefraction.Texture.Gbx`
- `Techno3\Media\Texture\InvFresnelPC3.Texture.gbx`
- `Techno3\Media\Texture\WaterNormalDecals.Texture.Gbx`

So one major conclusion is:

- the water look is not just a block-family naming convention
- it is also a shared rendering/material pipeline

### 2) Stadium authored water block family

Stadium has a clearly authored water family:

- `WaterBase`
- `RoadWater*`
- `PlatformWater*`
- `TrackWallWater*`

This system is explicit, clip-heavy, and clearly authored as its own family.

### 3) Vista / terrain water systems

The vista environments use a broader mix of:

- zone flats
- frontiers
- transitions
- decoration-controlled water slabs
- local shoreline / river / rocky / lake blocks

That means vista water is often a combination of:

- environment-level water
- block-level water

## Two key distinctions

### Global water slab vs local water carrier

Some water comes from the environment itself.

That is the global vista slab idea:

- the environment decoration chain supplies a large water volume/plane

But some water comes from individual block/prefab compositions:

- local water carriers
- shoreline hybrids
- deco lake / river / rocky shore blocks

### Visual water vs gameplay water

A block can:

- include `Water.Material`
- look like water
- even carry local water meshes

and still fail to apply the water effect to the car.

This is one of the most important practical findings in the doc set.

## What we know with high confidence

### Confirmed

- The water look is strongly tied to the shared `Water.Material` chain.
- Stadium water is a dedicated authored block family.
- The vista environments expose water through zone and decoration systems.
- Some classic blocks carry local water geometry directly.
- Some shoreline-style hybrid blocks apply gameplay water outside the normal slab.

### Strongly suggested

- BlueBay and WhiteShore are relatively strict.
- GreenCoast and RedIsland are broader.
- In BlueBay and WhiteShore, the strongest simple predictor is whether the top-level prefab directly includes the local zone water base prefab.

### Still open

- What exactly triggers gameplay water on the car?
- How much gameplay water is carried by deeper transition prefabs?
- How far can stock-client map-only underwater conversion actually go?

## Best follow-up reads

- `water-blocks/current-status.md`
- `water-blocks/comparisons/water-effect-working-vs-not-working.txt`
- `water-blocks/comparisons/roadice-diagleft-water-carrier-bounds.md`
- `water-blocks/porting/stadium-to-nonstadium-water-porting-notes.md`
