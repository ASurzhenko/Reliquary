# Content

Authored data: the `ScriptableObject` definitions a designer edits, the relic catalogue that collects
them, and the icons they point at. Adding a relic means adding an asset here — no code, no recompile.

Content describes *what exists*; it never decides what happens. Rules live in `Domain`, which is what
keeps a new asset from needing a new branch in a system somewhere.

This layer may reference `Domain`. It does not reference `Infrastructure`, `Presentation` or `App`.

## To add a relic

1. Right-click `Assets/Content/Resources/Relics` → **Create > Reliquary > Relic**.
2. Fill five fields: **Id** (`relic.<snake_name>`, unique), **Display Name**, **Description**,
   **Icon** (a sprite from `Assets/Content/Icons`), **Essence Value** and **Discovery Weight**.
3. That is all. The catalogue is read from this folder at boot, so there is no list to register into and
   nothing to recompile. Run **Tools > Reliquary > Validate Relic Content** to see the new count.

The relic asset must live under a `Resources/Relics` folder — subfolders are fine. Anywhere else and it
will not be in the game; the validator says so and selects the file when you click the message.

## To add a behaviour

1. Copy `EssenceYieldEffectDefinition.cs`, rename the file and the class.
2. Change its serialized numbers, its `Summary` line, and what its nested effect adds to `RelicModifiers`.
3. Right-click in `Assets/Content/Effects` → **Create > Reliquary > Effects > …** to make its asset, then
   drag that asset into a relic's **Effects** list.

Nothing enumerates behaviours, so no existing file changes: one new class, one new asset. The same type
is what a completed set grants, so a set perk is authored exactly this way too.

## Validation

**Tools > Reliquary > Validate Relic Content** checks every relic asset in the project and reports, per
asset: wrong folder, missing or duplicate id, missing icon, an empty or broken effect slot, out-of-range
numbers, and a blank display name or description. Click a console line to select the offending asset.
It then runs the same load the game runs at boot and reports how many relics that returned. The sweep
also runs by itself when a `.asset` under `Assets/Content/` is imported, moved or deleted.
