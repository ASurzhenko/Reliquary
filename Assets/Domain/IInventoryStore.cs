namespace Reliquary.Domain
{
    /// <summary>
    /// Where a snapshot is kept. Declared here because the rules need persistence; implemented outside,
    /// because every way of achieving it is an engine or platform concern.
    /// </summary>
    public interface IInventoryStore
    {
        StoredState Load();

        /// <summary>Writes the whole snapshot. Returns false with a reason rather than hiding a refusal.</summary>
        bool TrySave(InventorySnapshot snapshot, out string failure);
    }
}
