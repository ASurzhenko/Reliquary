using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    public enum SavedInventoryStatus
    {
        /// <summary>The snapshot was usable; the inventory reflects it (possibly minus skipped entries).</summary>
        Restored,

        /// <summary>The snapshot could not be interpreted by this build. Play starts fresh; the bytes stay put.</summary>
        Refused
    }

    /// <summary>What a decoded snapshot turned out to be worth.</summary>
    public sealed class SavedInventory
    {
        public SavedInventory(SavedInventoryStatus status, Inventory inventory,
            IReadOnlyList<InventorySnapshotEntry> carried, IReadOnlyList<RelicContentIssue> issues)
        {
            Status = status;
            Inventory = inventory;
            Carried = carried;
            Issues = issues;
        }

        public SavedInventoryStatus Status { get; }

        public Inventory Inventory { get; }

        /// <summary>Entries naming relics this build's catalogue does not contain. Kept, and written back.</summary>
        public IReadOnlyList<InventorySnapshotEntry> Carried { get; }

        public IReadOnlyList<RelicContentIssue> Issues { get; }
    }

    /// <summary>
    /// Turns a decoded snapshot into an inventory, or refuses it. Every branch here is a judgement about
    /// data this build did not write in this run, which is why it lives in the domain where it is testable
    /// rather than beside the code that reads the bytes.
    /// </summary>
    public static class SavedInventoryReader
    {
        public static SavedInventory Read(InventorySnapshot snapshot, RelicCatalog catalog)
        {
            List<RelicContentIssue> issues = new List<RelicContentIssue>();

            if (snapshot == null)
            {
                issues.Add(RelicContentIssue.Error(default, "There was no snapshot to read."));
                return Refused(issues);
            }

            // Version is a value we always write, so one comparison catches both a payload that decoded
            // into defaults without complaining and a payload that never came from this game.
            if (snapshot.Version <= 0)
            {
                issues.Add(RelicContentIssue.Error(default,
                    "The save carries no version; it did not come from this game, or did not survive being written."));
                return Refused(issues);
            }

            if (snapshot.Version > SaveFormat.Current)
            {
                issues.Add(RelicContentIssue.Error(default,
                    $"The save was written by a newer version (found {snapshot.Version}, this build reads {SaveFormat.Current})."));
                return Refused(issues);
            }

            // A save with no entries did not come from this build: saving happens on change, and a change
            // means at least one entry. InventoryPersistence refuses to write one, which is what keeps this
            // premise true rather than merely assumed.
            if (snapshot.Entries == null || snapshot.Entries.Length == 0)
            {
                issues.Add(RelicContentIssue.Error(default,
                    "The save holds no entries; this build never writes one, so it did not come from here."));
                return Refused(issues);
            }

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> order = new List<string>();

            for (int index = 0; index < snapshot.Entries.Length; index++)
            {
                InventorySnapshotEntry entry = snapshot.Entries[index];

                if (entry == null)
                {
                    issues.Add(RelicContentIssue.Warning(default, $"Entry {index} was null and was skipped."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.RelicId))
                {
                    issues.Add(RelicContentIssue.Warning(default, $"Entry {index} has no relic id and was skipped."));
                    continue;
                }

                // The content pipeline trims the ids it authors; RelicId itself does not trim, so a padded
                // saved id would miss the catalogue and be carried as an unknown relic.
                string id = entry.RelicId.Trim();

                if (entry.Count <= 0)
                {
                    issues.Add(RelicContentIssue.Warning(new RelicId(id),
                        $"'{id}' was saved with a count of {entry.Count}; ownership starts at 1. The entry was skipped."));
                    continue;
                }

                if (counts.TryGetValue(id, out int already))
                {
                    int kept = Math.Max(already, entry.Count);
                    issues.Add(RelicContentIssue.Warning(new RelicId(id),
                        $"'{id}' appears twice in the save, with counts {already} and {entry.Count}. Kept {kept}."));
                    counts[id] = kept;
                    continue;
                }

                counts.Add(id, entry.Count);
                order.Add(id);
            }

            List<InventoryEntry> owned = new List<InventoryEntry>(order.Count);
            List<InventorySnapshotEntry> carried = new List<InventorySnapshotEntry>();

            foreach (string id in order)
            {
                RelicId relicId = new RelicId(id);

                if (catalog != null && catalog.Contains(relicId))
                {
                    owned.Add(new InventoryEntry(relicId, counts[id]));
                    continue;
                }

                // Carried, not dropped: dropping it would look like a skip and then delete the player's
                // copies on the very next save, one step later and in a different file.
                carried.Add(new InventorySnapshotEntry { RelicId = id, Count = counts[id] });
                issues.Add(RelicContentIssue.Warning(relicId,
                    $"'{id}' is not in this build's catalogue; it stays in the save and is not shown."));
            }

            return new SavedInventory(SavedInventoryStatus.Restored, new Inventory(owned), carried, issues);
        }

        private static SavedInventory Refused(IReadOnlyList<RelicContentIssue> issues)
        {
            return new SavedInventory(SavedInventoryStatus.Refused, new Inventory(),
                Array.Empty<InventorySnapshotEntry>(), issues);
        }
    }
}
