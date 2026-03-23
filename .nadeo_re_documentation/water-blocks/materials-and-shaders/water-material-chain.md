# Water Material Chain

This file is about the shared rendering side of water.

## Main idea

Across Stadium and the vista environments, `Water.Material.Gbx` repeatedly points into the same shared Techno3 water rendering chain.

Key observed shared resources:

- `Techno3\Media\Material\Tech3_Water_MultiH.Material.gbx`
- `Techno3\Media\Texture\SeaRenderPlaneId.Texture.Gbx`
- `Techno3\Media\Texture\SeaRenderReflection.Texture.Gbx`
- `Techno3\Media\Texture\SeaRenderRefraction.Texture.Gbx`
- `Techno3\Media\Texture\InvFresnelPC3.Texture.gbx`
- `Techno3\Media\Texture\WaterNormalDecals.Texture.Gbx`

There is also usually a titlepack-local water height/shape texture wrapper.

## Why this matters

This is one of the strongest proofs that:

- waterness is not only a block-family concept
- the rendering pipeline itself is a major shared layer

## Practical meaning

If those shared dependencies are wrong or missing:

- you may still get blue geometry
- but you are much less likely to get proper TM2020 water rendering
