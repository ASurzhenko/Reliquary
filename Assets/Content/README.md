# Content

Authored data: the `ScriptableObject` definitions a designer edits, the relic catalogue that collects
them, and the icons they point at. Adding a relic means adding an asset here — no code, no recompile.

Content describes *what exists*; it never decides what happens. Rules live in `Domain`, which is what
keeps a new asset from needing a new branch in a system somewhere.

This layer may reference `Domain`. It does not reference `Infrastructure`, `Presentation` or `App`.
