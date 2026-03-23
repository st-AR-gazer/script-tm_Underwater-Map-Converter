# Testing Environment

This repo exposes the old `MapViewer.Testing` environment through the new CLI:

```powershell
dotnet run --project src\UnderwaterMapConverter.csproj -- testing <legacy-command> ...
```

Examples:

```powershell
dotnet run --project src\UnderwaterMapConverter.csproj -- testing inspect-prefab "C:\path\to\Prefab.Gbx"
dotnet run --project src\UnderwaterMapConverter.csproj -- testing inspect-map-blocks "C:\path\to\Map.Gbx" "RoadIce" 20
```

## Where it points

Current bridge target:

- `C:\Users\ar\Desktop\trackmania\misc\map_viewer\backend\MapViewer.Testing`

## Why this exists

This keeps the new underwater-converter repo focused while still preserving access to the richer inspection tooling built earlier inside `map_viewer`.
