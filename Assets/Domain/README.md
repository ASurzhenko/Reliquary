# Domain

Pure C#. Item identity, inventory and collection rules, set completion, the essence economy, and the
acquisition/persistence interfaces this project's rules depend on.

Compiled as `Reliquary.Domain` with engine references switched off: there is no `MonoBehaviour`,
no `ScriptableObject`, no `Sprite`, no `JsonUtility` and no `Debug` in scope here. That is deliberate —
it is what makes "gameplay rules are not coupled to UI or scene components" a compile error rather than
a claim.

This layer references nothing. Everything else may reference it.
