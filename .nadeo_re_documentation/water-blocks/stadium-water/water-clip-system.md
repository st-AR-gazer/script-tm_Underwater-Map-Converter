# Stadium Water Clip System

This file explains why the Stadium water family depends so heavily on clips.

## Main idea

The important point is:

- a Stadium water block is usually not just one main prefab
- it also depends on clips and clip-prefab helpers

Those helpers often provide:

- borders
- joins
- edge handling
- collision/helper behavior

## What this means practically

If you only copy:

- the obvious main water prefab

but not the surrounding clip ecosystem, the result may lose:

- borders
- correct edge behavior
- the authored "water correctness" of the original block family

## Why this matters for porting

This is one of the main reasons Stadium water is special:

- the water family behaves like a small authored subsystem, not one isolated asset

That matters both for:

- Stadium-to-non-Stadium porting
- understanding why the vista environments feel architecturally different
