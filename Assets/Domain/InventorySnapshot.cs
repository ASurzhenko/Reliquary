using System;

namespace Reliquary.Domain
{
    /// <summary>The save format this build writes and is able to read.</summary>
    public static class SaveFormat
    {
        public static readonly int Current = 1;
    }

    /// <summary>
    /// The persisted shape of a player's inventory. Public fields and arrays only: the domain cannot see the
    /// serializer that will write this, so the shape stays the lowest common denominator every serializer
    /// reads. Nothing here is a rule — the rules are in SavedInventoryReader.
    /// </summary>
    /// <remarks>
    /// A serializer tolerates a field it has never seen, so ADDING state to this shape does not require a
    /// version bump; an old save simply decodes with the new field defaulted. Bump SaveFormat.Current only
    /// when a field is removed or its meaning changes.
    /// </remarks>
    [Serializable]
    public sealed class InventorySnapshot
    {
        public int Version;
        public InventorySnapshotEntry[] Entries;

        /// <summary>
        /// Spendable essence. Added rather than versioned: a payload written before this field existed
        /// decodes with it at 0, which is exactly what an older save should restore.
        /// </summary>
        public int Essence;
    }

    [Serializable]
    public sealed class InventorySnapshotEntry
    {
        public string RelicId;
        public int Count;
    }
}
