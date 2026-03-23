# Porting Notes: Stadium to Non-Stadium (Preserving Water)

This file is about one practical question:

- if we move a Stadium water-related block into a non-Stadium environment, what has to survive for it to still behave correctly?

## Short version

Preserving water visuals is easier than preserving water correctness.

The current best model is:

- copying one prefab is usually not enough
- copying one BlockInfo is usually not enough
- clips, helper prefabs, materials, and sometimes environment-side infrastructure all matter

## What success really means

There are at least three levels of success:

1. it looks watery
2. it renders like proper TM2020 water
3. it behaves like proper gameplay water

Those are not the same level of difficulty.

## What you almost certainly need for visuals

At minimum, the water surfaces need a working `Water.Material.Gbx` chain that resolves the shared Techno3 water resources.

## What you almost certainly need for Stadium-authored water blocks

Stadium water blocks depend on:

- water BlockInfos
- water clips
- helper / clip-prefabs
- water materials

That is why a simple "copy one prefab" approach is not enough.

## Main takeaway

The biggest mistake would be to think:

- "this is just one mesh with a water material"

The current evidence does not support that model for authored Stadium water.

The safer model is:

- Stadium water is a small authored subsystem, not a single asset
