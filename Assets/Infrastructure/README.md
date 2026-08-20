# Infrastructure

The engine-facing implementations of the interfaces `Domain` declares: the save store that persists
inventory and collection state, and the local acquisition service that stands where a real one would
attach.

Everything here is replaceable by construction — the domain names the capability it needs, this layer
supplies one way of meeting it, and `App` decides which.

This layer may reference `Domain`. It does not reference `Presentation` or `App`.
