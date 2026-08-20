using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class InventoryPersistenceTests
    {
        [Test]
        public void Change_WritesTheWholeSnapshot()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = new Inventory();

            using (new InventoryPersistence(store, inventory, Array.Empty<InventorySnapshotEntry>()))
            {
                inventory.Add(new RelicId("relic.sunken_crown"));
                inventory.Add(new RelicId("relic.pyre_key"));
            }

            Assert.That(store.Saved.Count, Is.EqualTo(2), "one write per change");

            InventorySnapshot last = store.Saved[1];
            Assert.That(last.Version, Is.EqualTo(SaveFormat.Current));
            Assert.That(last.Entries.Length, Is.EqualTo(2), "every save carries the whole state, not a delta");
        }

        [Test]
        public void EmptySnapshot_IsRefusedAndReported()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = new Inventory();
            List<string> failures = new List<string>();

            using (InventoryPersistence persistence =
                new InventoryPersistence(store, inventory, Array.Empty<InventorySnapshotEntry>()))
            {
                persistence.SaveFailed += failure => failures.Add(failure);

                // The trigger this build has cannot produce it; a later kind of state can, and the next boot
                // would refuse what it wrote.
                persistence.Save();
            }

            Assert.That(store.Saved, Is.Empty);
            Assert.That(failures.Count, Is.EqualTo(1));
            Assert.That(failures[0], Does.Contain("no entries"));
        }

        [Test]
        public void StoreRefusal_RaisesSaveFailed()
        {
            RecordingInventoryStore store = new RecordingInventoryStore { RefusesWrites = true };
            Inventory inventory = new Inventory();
            List<string> failures = new List<string>();

            using (InventoryPersistence persistence =
                new InventoryPersistence(store, inventory, Array.Empty<InventorySnapshotEntry>()))
            {
                persistence.SaveFailed += failure => failures.Add(failure);

                inventory.Add(new RelicId("relic.sunken_crown"));
            }

            Assert.That(store.Saved, Is.Empty);
            Assert.That(failures, Is.EqualTo(new[] { "the store refused the write" }));
        }
    }
}
