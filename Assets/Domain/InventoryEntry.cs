namespace Reliquary.Domain
{
    /// <summary>One line of ownership: which relic, and how many copies of it.</summary>
    public readonly struct InventoryEntry
    {
        public InventoryEntry(RelicId id, int count)
        {
            Id = id;
            Count = count;
        }

        public RelicId Id { get; }

        public int Count { get; }
    }
}
