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
   nothing to recompile. Run **Tools > Reliquary > Validate Content** to see the new count.

The relic asset must live under a `Resources/Relics` folder — subfolders are fine. Anywhere else and it
will not be in the game; the validator says so and selects the file when you click the message.

## To add a behaviour

1. Copy `EssenceYieldEffectDefinition.cs`, rename the file and the class.
2. Change its serialized numbers, its `Summary` line, and what its nested effect adds to `RelicModifiers`.
3. Right-click in `Assets/Content/Effects` → **Create > Reliquary > Effects > …** to make its asset, then
   drag that asset into a relic's **Effects** list.

Nothing enumerates behaviours, so no existing file changes: one new class, one new asset. The same type
is what a completed set grants, so a set perk is authored exactly this way too.

## To add a set

1. Right-click `Assets/Content/Resources/Sets` → **Create > Reliquary > Set**.
2. Fill **Id** (`set.<snake_name>`, unique), **Display Name**, **Description**, then drag relic assets into
   **Members** and effect assets into **Perks**.
3. That is all — and note what did *not* happen: **no relic asset was edited**. A set lists its relics; a
   relic carries no set field, so joining a set costs the relic nothing and adding a set costs no code.

Progress is counted, never stored: owning every member completes the set, and completing it contributes
its perks to the same accumulator a relic's effects feed. Delete the set asset and the perk is gone on the
next launch, with nothing to migrate.

A member that does not live under `Resources/Relics` makes the set impossible to complete, because the
catalogue never loads it. That is an error the validator reports by name rather than a silently smaller
set — shrinking the goal would grant the perk for three quarters of a set.

## The trader's prices

`Assets/Content/Resources/Economy/Economy.asset` holds two numbers: a **price multiplier** and a **price
floor**. A relic costs its own essence value times the multiplier, never below the floor — so at ×3 a
targeted purchase costs roughly three duplicates of the same tier. Delete the asset and the game still
runs on built-in defaults, and the Console says why the numbers came from code.

## Validation

**Tools > Reliquary > Validate Content** checks every relic, set and economy asset in the project and
reports, per asset: wrong folder, missing or duplicate id, missing icon, an empty or broken effect,
member or perk slot, a member listed twice, a member outside `Resources/Relics`, out-of-range numbers, and
a blank display name or description. It also adds every authored effect and perk together and warns when
the total would run past the safety rails in `RelicModifiers`. Click a console line to select the
offending asset. It then runs the same load the game runs at boot and reports how many relics and sets
that returned. The sweep also runs by itself when a `.asset` under `Assets/Content/` is imported, moved or
deleted.
