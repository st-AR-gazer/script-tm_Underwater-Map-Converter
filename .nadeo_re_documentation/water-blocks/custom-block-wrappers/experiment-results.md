# Wrapper Experiment Results

This file is the short record of the most important wrapper experiments.

## Stadium water wrappers

Tried examples:

- `WaterBase`
- `PlatformWaterBase`
- `RoadWaterStraight`

Observed result:

- these crashed when tried as simple custom wrappers

Interpretation:

- the simple wrapper approach is not a safe general route for arbitrary Stadium water blocks

## Vista-local hybrid wrappers

Observed result:

- some vista-local wrappers were stable
- only some of them applied gameplay water on the car

Important practical outcome:

- wrapper stability and gameplay-water behavior are separate questions

## Converter relevance

Later underwater-map work used the wrapper approach mostly for:

- meshless or lower-visual-clutter carrier variants

rather than as proof of fully custom block authoring.
