# Reliquary

An item collection & inventory system, built for the Azulon Unity technical challenge.

You dig for relics. Every relic exists in a catalogue you can see from the first second, whether you
own it or not. Duplicates — normally the anticlimax of a collection game — dissolve into essence, and
essence buys the exact relic you are missing. Complete a set and its perk changes how digging works
from then on.

Unity **6000.0.64f1**, 2D Built-In Render Pipeline, uGUI + TextMeshPro. **No third-party packages.**

---

## Running it

Open the project, open `Assets/Scenes/Main.unity`, press Play. Nothing else to set up.

The UI is authored for portrait, 1080×1920. It holds at other aspect ratios — the frame keeps its
authored size and the surplus falls outside it, rather than the layout stretching.

Tests: `Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`. **135 tests, ~6 seconds.** They need no
scene and no play mode, for the reason in the next section.

---

## The one architectural boundary

`Assets/Domain` is its own assembly, and its assembly definition has **No Engine References**. The
domain cannot see `MonoBehaviour`, `Sprite`, `Debug` or `JsonUtility` — not by convention, by compiler.

Put `using UnityEngine;` in any file under `Assets/Domain` and the project stops compiling:

```
error CS0246: The type or namespace name 'UnityEngine' could not be found
```

Three things follow, and none of them is a preference:

- **Persistence is an interface the domain declares**, because `JsonUtility` is unreachable from there.
  `Assets/Infrastructure` implements it over `PlayerPrefs`.
- **The domain never learns what a relic looks like.** Icons and copy live in `Assets/Content`. The
  domain knows what a relic *does*.
- **Rules are testable without the engine**, which is why the suite runs in six seconds.

Everything else lives in the default assembly, organised by folder: `Content` (authored data),
`Infrastructure` (storage and the local acquisition service), `Presentation` (views), `App` (the
composition root). One boundary, placed where it pays. There are no singletons — the composition root
constructs every service and hands it over.

---

## Adding content without touching code

This is the claim the challenge asks about, so here are the numbers rather than the adjective.

**A new relic: one new asset. Zero existing files edited. No recompile.**

`Create ▸ Reliquary ▸ Relic`, drop it in `Assets/Content/Resources/Relics`, fill in the fields. The
catalogue reads every relic asset in that folder, so putting the file there *is* the registration —
there is no list to remember.

**A new behaviour: one new file.**

Subclass `RelicEffectDefinition` and implement two members: a sentence for the player and the effect
itself. No switch statement, no enum, no registry, no factory. Nothing keeps a list of behaviours,
which is why nothing has to be edited to add one.

Commit `f3ffb88` is exactly this, done on camera: a relic, an effect asset and one class — six new
files and, verified with `git show --diff-filter=M`, **no modified files at all**.

A content validator runs on import and from `Tools ▸ Reliquary ▸ Validate Content`. It names the
asset and the problem: a duplicate id, a missing icon, an empty effect slot, a set member that is not
in the catalogue, an economy number outside its rails.

---

## The gamification, and why it is not bolted on

The loop uses both halves the brief defines: sets are a fact about the **collection**, duplicates a
fact about the **inventory**.

> dig → a duplicate lands → it dissolves into essence → essence buys the specific relic a set is
> missing → the set completes → its perk changes acquisition → dig again

A completed set's perk is authored with **the same asset type a relic's own effect uses**. That is
what makes the mechanic a consumer of the extensibility system rather than a special case beside it —
and it is visible in the arithmetic: a relic worth 21 essence, held by a player who owns a relic
granting +10 %, dissolves for `floor(21 × 1.10) = 23`. A completed set's +25 % adds into the same
accumulator.

**The perk is never granted and never stored.** It is derived from what you own every time it is
read, so it cannot be granted twice, cannot be lost to a failed write, and is simply there after a
restart. What happens exactly once is the *announcement*, and that is seeded from the state the
session started with — no flag on disk.

---

## State, and data this build did not write

Saving happens on change, not on quit. The file is small, readable and versioned:

```json
{"Version":1,"Entries":[{"RelicId":"relic.cinder_mask","Count":2}],"Essence":14}
```

There is no stored "set complete" flag and no stored perk — both are computed from that list.

**An exchange lands whole or not at all.** Spending essence and receiving a relic is one write: the
state the change *would* produce is persisted first, and only then is anything mutated. A refused
write costs the player a tap instead of a copy, and a crash between the write and the mutation
resolves on the next boot to the completed exchange, because the disk already holds both halves.

Two cases the loader is deliberate about, both reachable from `Tools ▸ Reliquary ▸ Save`:

- **A save from a newer version** is refused, with both numbers in the message — and **not
  overwritten**. A build that understands it will find the data where it was left.
- **A relic this build does not have** is carried, not dropped. It is not counted and not shown, but
  it survives every rewrite. If that relic returns in a later build, the player's copies are waiting.

---

## Tests

135 EditMode tests over the rules: identity, the catalogue, inventory and duplicates, the acquisition
order and its cancellation, the saved-state reader's rules, essence, the exchange, the trader, set
progress, completion, and the modifier accumulator. Each one states an obligation rather than
describing the current implementation, and the codec test is fed a payload captured from a real run
rather than one built by the serializer it checks. As rules were added, their tests were confirmed by
deleting the line each defends and watching that specific test go red.

Two of them guard the assembly boundary itself: one reads the assembly definition, one reflects over
the built assembly and fails if it ever gains an engine reference.

---

## Deliberately not built

Named because the boundary is a decision, not an omission.

No networking and no backend — though acquisition goes through an awaitable, cancellable service
interface, so the seam where an authoritative service would replace the local one is a single line in
the composition root, and nothing above it would change. No Web3. No third-party packages of any kind.
No localization. No crafting trees, equipment slots, character stats or combat. No cloud save and no
save encryption. No custom render pipeline. No analytics.

---

## Where things are

| Path | What |
|---|---|
| `Assets/Domain` | The rules. Pure C#, no engine references. |
| `Assets/Content` | Authored data: relics, sets, effects, the economy, and the validator. |
| `Assets/Infrastructure` | `PlayerPrefs` storage, the local acquisition service, save tooling. |
| `Assets/Presentation` | Screens, overlays, the shell. Owns no rule. |
| `Assets/App` | The composition root — the only place a service is constructed. |
| `Assets/Tests/EditMode` | The suite. |
