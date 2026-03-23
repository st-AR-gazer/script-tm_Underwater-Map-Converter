# Research Method (Water Blocks)

This pass intentionally prioritizes primary evidence:

- extracted GBX files under:
  - `C:\Users\ar\OpenplanetNext\Extract\GameData\Stadium\`
  - `C:\Users\ar\OpenplanetNext\Extract\GameData\BlueBay\`
  - `C:\Users\ar\OpenplanetNext\Extract\GameData\GreenCoast\`
  - `C:\Users\ar\OpenplanetNext\Extract\GameData\RedIsland\`
  - `C:\Users\ar\OpenplanetNext\Extract\GameData\WhiteShore\`
- direct inspection via the `testing` bridge into `MapViewer.Testing`

## Core technique: trace graphs end-to-end

We repeatedly trace:

1. BlockInfo / Zone GBX
2. clip references
3. mobil payloads
4. prefab ref tables
5. material ref tables
6. mood/decoration assets when relevant

## Tooling commands used most often

### BlockInfo inspection

- `inspect-blockinfo-classic <...EDClassic.Gbx>`
- `inspect-blockinfo <...EDFlat/EDFrontier/EDTransition/EDClip.Gbx>`

### Reference-table extraction

- `inspect-ref-table <any.gbx> [limit]`

### Primitive-field inspection

- `inspect-primitives <any.gbx> [maxDepth] [limit]`

### Prefab inventory

- `inspect-prefab <...Prefab.Gbx>`
- `inspect-prefab-bounds-and-orientation <...Prefab.Gbx> [cellSize] [edgeEpsilon]`

### Mesh extraction / geometry checks

- `extract-model-mesh <...gbx>`

### Map checks

- `inspect-map-blocks <map> <type-filter> [limit]`
- `inspect-map-zonegenealogy-grid <map>`

## Evidence levels

Statements in this doc set are tagged as:

- confirmed
- strongly suggested
- hypothesis / needs validation
