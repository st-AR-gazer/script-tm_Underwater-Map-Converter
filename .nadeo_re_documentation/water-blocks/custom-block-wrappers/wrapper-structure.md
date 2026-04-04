# Wrapper Structure

Current working model of a `.Block.Gbx` wrapper:

- the file loads as `CGameItemModel`
- `EntityModelEdition` is an inline `CGameBlockItem`
- `CGameBlockItem` stores:
  - `ArchetypeBlockInfoId`
  - `ArchetypeBlockInfoCollectionId`
  - `CustomizedVariants`

## Practical meaning

The wrapper is usually:

- a shell around an existing block archetype

This is why wrapper-side stripping alone usually does not remove the archetype's own geometry.

## Important limitation

The wrapper format does not appear to define an entirely new BlockInfo -> Prefab graph by itself.

That is why a lot of wrapper experimentation ended up being:

- "which existing archetype can we safely expose?"

rather than:

- "how do we author a totally new water block from scratch?"

## Map-embedded wrapper path rule

One concrete rule from the underwater-map converter work:

- map-embedded custom wrapper data is not enough by itself
- the embedded ZIP entry path also has to look like a normal game-side path

Current confirmed behavior:

- a wrapper embedded as `Blocks/...` is recognized as an embedded custom block model
- a wrapper embedded with an absolute path like `C:/Users/.../Blocks/...` is not recognized correctly

Practical consequence:

- if the ZIP entry path is wrong, the map can still contain the wrapper bytes, but the expected embedded model list collapses to an empty id
- that produces the in-game "Missing custom blocks" dialog even though the data is physically inside the map

Useful working pattern:

- embed block wrappers under `Blocks/MinimalWaterWrappers/...`
- keep the placed block id aligned with the corresponding relative block path, for example:
  - `MinimalWaterWrappers\WhiteShore\Working\RoadIceWithWallLeftDiagLeftStraightOnWaterShore1.Block.Gbx_CustomBlock`
  - ZIP entry `Blocks/MinimalWaterWrappers/WhiteShore/Working/RoadIceWithWallLeftDiagLeftStraightOnWaterShore1.Block.Gbx`

## Placement note

During debugging, freeblock vs grid-aligned placement looked suspicious, but it was not the core embedding failure.

What actually mattered for recognition was:

- the embedded ZIP entry path rule above

Once that was corrected, both placement styles became testable again.
