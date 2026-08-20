namespace Reliquary.Domain
{
    /// <summary>
    /// What one accepted change to the inventory carries. Raised after the count has been updated, so
    /// <see cref="Count"/> is the new total rather than the previous one.
    /// </summary>
    public readonly struct InventoryChange
    {
        public InventoryChange(RelicId id, int count, bool wasFirstCopy, int delta)
        {
            Id = id;
            Count = count;
            WasFirstCopy = wasFirstCopy;
            Delta = delta;
        }

        public RelicId Id { get; }

        /// <summary>How many copies are owned after the change.</summary>
        public int Count { get; }

        /// <summary>Signed: a find is positive, a copy consumed is negative. A subscriber tells them apart by this.</summary>
        public int Delta { get; }

        /// <summary>True when this change is what made the relic owned at all.</summary>
        public bool WasFirstCopy { get; }
    }
}
