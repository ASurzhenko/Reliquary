using System.Runtime.CompilerServices;

// The mutators behind StatePersistence.TryApply are internal so that nothing outside the rules can move a
// count or a balance without going through the single write. Their guards still have to be driven, and the
// tests are a separate assembly.
[assembly: InternalsVisibleTo("Reliquary.Tests.EditMode")]
