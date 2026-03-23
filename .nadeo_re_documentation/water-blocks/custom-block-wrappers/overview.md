# Custom Block Wrappers Overview

This section is about TM2020 custom `.Block.Gbx` wrappers.

The practical question is:

- can wrappers help us create useful custom water blocks without requiring a custom titlepack or custom environment install?

## Current model

The current best model is:

- a `.Block.Gbx` wrapper is usually a `CGameItemModel`
- its `EntityModelEdition` is a `CGameBlockItem`
- that block item points at an existing archetype

Important consequence:

- most wrappers do not define their own BlockInfo -> Prefab graph
- they usually redirect to an existing block archetype

## Why wrappers still matter

Even with that limitation, wrappers matter because:

- they can expose useful archetypes as custom blocks
- they can let us reuse certain water-carrying hybrid blocks in more flexible ways

## Where the open question lives

The interesting part is:

- `CustomizedVariants`
- `Crystal`

Those fields may allow more than a pure archetype redirect, but they are still not fully understood.
