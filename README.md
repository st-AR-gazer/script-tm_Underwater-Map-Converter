# Underwater Map Converter

> OOPS, this only works on the NON stadium vistas, as the blocks needed for both are different. There is no easy way (as far as I can tell, tbh I cant tell if it's even possible), to automate adding water blocks to a map extenrally...

Standalone Trackmania 2020 R&D/conversion engine for turning existing maps into underwater-style variants.

This repo is the new home for the water-specific work that was previously prototyped inside `map_viewer`.

## Layout

- `src` - the actual C# project root
- `.nadeo_re_documentation/water-blocks` - water-focused R&D docs
- `.nadeo_re_documentation/testing` - notes about the testing bridge
- `research/legacy-scripts` - archived one-off prototype scripts from the earlier phase

## Main workflow

Use the top-level `make-underwater-map` command for the simplest workflow:

- `make-underwater-map <inputMapPath> <suffix> [--variant normal|meshless|both] [--coverage one-layer|full-stack]`

Examples:

```powershell
dotnet run --project src\UnderwaterMapConverter.csproj -- make-underwater-map "C:\Maps\Winter 2026 - 03.Map.Gbx" "Underwater" --variant both --coverage full-stack
dotnet run --project src\UnderwaterMapConverter.csproj -- make-underwater-map "C:\Maps\Winter 2026 - 01.Map.Gbx" "Underwater" --variant meshless --coverage one-layer
```

`make-underwater-map` automatically:

- detects the environment
- picks the matching carrier family
- applies the current uniform-sheet placement logic
- exports a new map with the suffix you choose
- places the suffix before the `.Map*.Gbx` tail, e.g. `Winter 2026 - 01 (Underwater).Map(1).Gbx`
- errors out for `Stadium` maps instead of trying to convert them

## Advanced workflow

Lower-level commands still exist for more manual control:

- `convert`
- `extrude-template-volume`
- `flood-vista`
- `place-water-carrier-lattice`
- `testing`

## Packaging

To publish a single Windows executable that bundles the native LZO dependency:

```powershell
dotnet publish src\UnderwaterMapConverter.csproj -c Release -r win-x64 -o dist\win-x64
```

That publish output should contain one distributable `UnderwaterMapConverter.exe` instead of requiring a separate `liblzo2.dll`.
