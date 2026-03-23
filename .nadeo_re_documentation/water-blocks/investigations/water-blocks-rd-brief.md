# Water Blocks R&D Brief

Use this file as a self-contained prompt for a fresh reverse-engineering session focused on water in TM2020.

## Mission

Build an evidence-backed model of how water works in Trackmania 2020, especially with the practical goal of:

- understanding what makes a block or zone behave like water
- understanding what needs to be preserved when porting Stadium water into non-Stadium environments

## Key questions

- What makes something "water" in TM2020?
- What part is visual?
- What part is engine-rendering?
- What part is gameplay-water?
- What is shared between Stadium water and vista water?

## Evidence sources

- extracted game files
- GBX.NET source / chunk definitions
- testing helpers / inspection tooling

## Deliverables

- a clear model of water layers
- a distinction between Stadium water and vista water
- practical porting implications
- open questions that still need validation
