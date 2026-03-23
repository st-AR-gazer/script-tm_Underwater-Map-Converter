# Start Here: Water Blocks in TM2020

If you only read one file before diving into the rest of the water docs, read this one.

The rest of the doc set contains a lot of real evidence, but most of it was written during active reverse-engineering. That means it is rich in facts but not always the easiest first read.

This file gives the simplest useful mental model first.

## The problem in one sentence

We want to understand what makes something in Trackmania 2020 behave like water, especially when trying to:

- move water behavior outside its normal environment
- make more of a map feel underwater
- reuse or adapt water-carrying blocks in places they were not originally meant to go

## The most important idea

In TM2020, "water" is not one single mechanic.

There are at least three different layers that people casually call "water":

1. water visuals
2. engine water rendering
3. gameplay water

Those layers often overlap, but they are not always the same thing.

That is the single most important reason this whole topic gets confusing.

## The current high-level model

### 1) Some water comes from the environment itself

In the vista environments there is often a global decoration-controlled water slab.

That means:

- the environment can supply a large shared water volume
- the map can sometimes move that slab up/down
- but the slab is not always infinite

This is why whole-map underwater conversion is harder than just changing one height value.

### 2) Some water comes from local blocks and prefabs

Some blocks carry their own water geometry or water-carrying prefabs.

That means:

- not all water is the global vista slab
- some blocks can bring water with them
- those blocks matter a lot for map-only underwater experiments

### 3) Visual water is easier than gameplay water

A block can:

- use water materials
- look watery
- even carry water meshes

and still fail to apply the water effect to the car.

That is why the working-vs-not-working inventory matters so much.

## What we know with the most confidence

### Confirmed

- The water look is strongly tied to the shared `Water.Material.Gbx` chain.
- Stadium has an explicit authored water block family.
- The vista environments also have their own water systems via zones/decor assets.
- Some normal classic blocks carry local water geometry directly.
- Some shoreline-style hybrid blocks apply water on the car outside the normal slab.

### Strongly suggested

- BlueBay and WhiteShore are relatively strict.
- GreenCoast and RedIsland are broader.
- In BlueBay and WhiteShore, the strongest simple predictor is whether the top-level prefab directly includes the local zone water base prefab.

### Still open

- What exactly is the real gameplay-water trigger?
- How much gameplay water is carried by deeper transition prefabs?
- How far can stock-client map-only underwater conversion go?

## If your goal is practical

### Goal: "I want a whole map to feel underwater"

Read:

- `water-blocks/current-status.md`
- `water-blocks/comparisons/roadice-diagleft-water-carrier-bounds.md`
- `water-blocks/comparisons/water-effect-working-vs-not-working.txt`

### Goal: "I want to understand why some blocks work and others do not"

Read:

- `water-blocks/comparisons/water-effect-working-vs-not-working.txt`
- `water-blocks/comparisons/vista-on-water-block-inventory.md`
- `water-blocks/terrain-water/bluebay.md`
- `water-blocks/terrain-water/whiteshore.md`

### Goal: "I want the architecture"

Read:

- `water-blocks/concepts-and-glossary.md`
- `water-blocks/overview.md`
- `water-blocks/research-method.md`

## Best next file

If this file made sense, the next best file is:

- `water-blocks/concepts-and-glossary.md`
