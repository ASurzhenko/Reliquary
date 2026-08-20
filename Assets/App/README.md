# App

The composition root. The one place where concrete services are constructed and handed to the objects
that need them — no singletons, no static service locator, no `FindObjectOfType` lookups.

Because everything is wired here, every other layer can be read without asking where its dependencies
come from, and swapping an implementation is a change to one file.

This layer may reference `Domain`, `Content`, `Infrastructure` and `Presentation`. Nothing references it.
