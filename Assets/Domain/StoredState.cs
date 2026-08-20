namespace Reliquary.Domain
{
    public enum StoredStateStatus
    {
        /// <summary>Nothing has ever been saved. A first run, not an error.</summary>
        None,

        /// <summary>A payload was decoded into a snapshot. Whether it makes sense is the reader's judgement.</summary>
        Loaded,

        /// <summary>A payload exists and could not be decoded at all.</summary>
        Unreadable
    }

    /// <summary>
    /// What a store found where a save was expected. Decoding is the store's job; deciding whether the
    /// result is usable is SavedInventoryReader's.
    /// </summary>
    public sealed class StoredState
    {
        private StoredState(StoredStateStatus status, InventorySnapshot snapshot, string detail)
        {
            Status = status;
            Snapshot = snapshot;
            Detail = detail;
        }

        public StoredStateStatus Status { get; }

        public InventorySnapshot Snapshot { get; }

        /// <summary>Developer diagnostics naming why a payload could not be decoded. Never player-facing.</summary>
        public string Detail { get; }

        public static StoredState None()
        {
            return new StoredState(StoredStateStatus.None, null, null);
        }

        public static StoredState Loaded(InventorySnapshot snapshot)
        {
            return new StoredState(StoredStateStatus.Loaded, snapshot, null);
        }

        public static StoredState Unreadable(string detail)
        {
            return new StoredState(StoredStateStatus.Unreadable, null, detail);
        }
    }
}
