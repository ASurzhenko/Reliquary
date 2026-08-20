namespace Reliquary.Domain
{
    /// <summary>
    /// What one accepted change to the inventory carries. Raised after the count has been updated, so
    /// <see cref="Count"/> is the new total rather than the previous one.
    /// </summary>
    public readonly struct InventoryChange
    {
        public InventoryChange(RelicId id, int count, bool wasFirstCopy)
        {
            Id = id;
            Count = count;
            WasFirstCopy = wasFirstCopy;
        }

        public RelicId Id { get; }

        /// <summary>How many copies are owned after the change.</summary>
        public int Count { get; }

        /// <summary>True when this change is what made the relic owned at all.</summary>
        public bool WasFirstCopy { get; }
    }
}
