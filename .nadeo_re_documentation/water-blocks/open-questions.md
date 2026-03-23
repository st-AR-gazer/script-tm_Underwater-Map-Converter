# Open Questions

This file is the "what is still unresolved?" list for the water research.

## Gameplay water trigger

Still not fully nailed down:

- what exactly produces wet-wheels / water-on-car behavior?
- is it material-driven, surface-driven, volume-driven, clip-driven, or some mix?

## BlueBay / WhiteShore vs GreenCoast / RedIsland

Current evidence suggests:

- BlueBay and WhiteShore are stricter
- GreenCoast and RedIsland are broader

But the deeper reason is still not fully proven.

Best current hypothesis:

- in GreenCoast and RedIsland, deeper local transition/shore/hill prefabs may already carry enough gameplay-water semantics

## Custom wrappers and Crystal

Still open:

- how far can `CustomizedVariants / Crystal` go for custom visuals?
- can wrapper geometry help hide/rewrite ugly carrier geometry while preserving gameplay water?

## Whole-map underwater on stock clients

Still open in the strong sense:

- can a stock-client map produce truly convincing whole-map underwater behavior without depending on custom environment assets?

Current best answer:

- only approximately, through local carrier placement
- not through simply moving the vista slab

## Underwater converter project questions

Current implementation questions include:

- what the best default carrier family is per environment
- whether the one-layer or full-stack presets are more practical in real maps
- how to keep maps lightweight enough to open reliably while still feeling underwater
