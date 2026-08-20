# Presentation

`MonoBehaviour` views and the widgets they are built from. A view renders the state it is given and
raises the intent the player expressed; it does not decide whether an acquisition succeeds, what a
duplicate is worth, or when a set completes.

If a rule starts to appear in a view, it belongs in `Domain` instead — that migration is the reason the
boundary exists.

This layer may reference `Domain` and `Content`. It does not reference `Infrastructure` or `App`.
