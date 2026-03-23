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
