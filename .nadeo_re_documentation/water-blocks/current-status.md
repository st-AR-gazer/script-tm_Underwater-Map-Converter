# Water Blocks Current Status

This file is the shortest up-to-date snapshot of where the water R&D currently stands.

If you are new to the topic, read this after:

- `water-blocks/start-here.md`
- `water-blocks/concepts-and-glossary.md`

## Executive summary

If you only want the current practical answer:

- full stock-client "entire map underwater" is still not solved by simply moving the vista slab
- some blocks can carry local gameplay water outside the normal slab
- BlueBay and WhiteShore seem stricter than GreenCoast and RedIsland
- the `RoadIceWithWallLeftDiagLeftStraightOn*` family is one of the strongest current carrier candidates for line/sheet-based coverage experiments

## 1) Global vista water is a finite decoration-controlled slab (confirmed)

For WhiteShore, the map-wide water effect is not controlled only by terrain cells. It is anchored by the environment decoration chain:

- `WhiteShore\GameCtnDecoration\Base64x64Sunset.Decoration.Gbx`
- `WhiteShore\GameCtnDecoration\Size\64x64.DecorationSize.Gbx`
- `WhiteShore\GameCtnDecoration\Scene3d\Base64x64.Scene3d.Gbx`
- `WhiteShore\Media\Solid\Warp\Square64Water.Solid.Gbx`

Important observed fields:

- `CGameCtnDecorationSize.BaseHeightBase = 14`
- `CGameCtnChallenge.DecoBaseHeightOffset = 0` by default

Interpretation:

- the vista water effect behaves like a finite slab / volume, not an infinite "everything below a height is underwater" rule

## 2) Map-only flooding works structurally, but has a hard limit (confirmed)

What the map-only flood experiments proved:

- we can rewrite all vista terrain cells to water
- we can rewrite terrain-like ground blocks to water
- we can shift the decoration water slab upward

But because the slab is finite:

- "entire map underwater" is not solved just by moving it

## 3) Some normal blocks carry local water independently of the slab (confirmed)

Confirmed examples:

- `GreenCoast\GameCtnBlockInfo\GameCtnBlockInfoClassic\DecoLakeCurve2Out.EDClassic.Gbx`
- `WhiteShore\GameCtnBlockInfo\GameCtnBlockInfoClassic\DecoTerrainRocky.EDClassic.Gbx`

More importantly for gameplay:

- the vista families contain many `*OnWaterShore1*`, `*OnBeach*`, `*OnLakeShore*`, and `*OnWaterHill*` blocks

## 4) Best current predictor of "real water carrier" behavior (confirmed / strongly suggested)

The strongest simple structural test so far is:

- does the block's top-level prefab directly include the local zone water base prefab?

Examples:

- WhiteShore local water base: `WhiteShore\Media\Prefab\Zone\Water\Base.Prefab.Gbx`
- BlueBay local water base: `BlueBay\Media\Prefab\Zone\Sea\Base.Prefab.Gbx`
- GreenCoast local water base: `GreenCoast\Media\Prefab\Zone\Lake\Base.Prefab.Gbx`
- RedIsland local water base: `RedIsland\Media\Prefab\Zone\Water\Base.Prefab.Gbx`

Current pattern:

- diagonal road families usually do include the local zone water base prefab
- straight road families usually do not
- platform families usually do not
- open-road families usually do not

This pattern works especially well in BlueBay and WhiteShore.

## 5) Custom `.Block.Gbx` wrappers: what worked and what did not

Current model:

- `.Block.Gbx` files are `CGameItemModel` wrappers
- they use `EntityModelEdition = CGameBlockItem`
- `CGameBlockItem` points at an existing archetype via `ArchetypeBlockInfoId` and `ArchetypeBlockInfoCollectionId`

Important implication:

- wrappers do not define their own BlockInfo -> Prefab graph
- they usually re-point to an existing block archetype

### Wrapper experiments

Stadium water wrappers such as:

- `WaterBase`
- `PlatformWaterBase`
- `RoadWaterStraight`

crashed when tried as custom blocks.

Vista-local hybrid wrappers were more promising, but only some of them applied water-on-car behavior.

## 6) Current best practical path

The current best path is:

1. stay inside the local vista hybrid families
2. prioritize blocks whose top-level prefab directly includes the local zone water base prefab
3. avoid assuming pure Stadium water blocks can be safely wrapped as custom blocks

## 7) What is still not solved

- exactly what produces wet-wheels / gameplay water on the car
- whether a truly stock-client whole-map underwater solution exists without tradeoffs
- how much `CustomizedVariants / Crystal` can help with custom visuals
