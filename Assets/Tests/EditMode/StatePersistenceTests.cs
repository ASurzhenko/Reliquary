using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>
    /// The single write. Everything here is about one property: nothing is spent until the whole resulting
    /// state is on disk, so a refused write costs a tap and never a copy.
    /// </summary>
    public class StatePersistenceTests
    {
        private static readonly RelicId Bell = new RelicId("relic.drowned_bell");

        [Test]
        public void AcquisitionStillSavesOnChange()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = new Inventory();

            using (new StatePersistence(store, inventory, new EssenceWallet(0), Array.Empty<InventorySnapshotEntry>()))
            {
                inventory.Add(new RelicId("relic.sunken_crown"));
                inventory.Add(new RelicId("relic.pyre_key"));
            }

            Assert.That(store.Saved.Count, Is.EqualTo(2), "one write per change");
            Assert.That(store.Saved[1].Entries.Length, Is.EqualTo(2), "every save carries the whole state, not a delta");
            Assert.That(store.Saved[1].Version, Is.EqualTo(SaveFormat.Current));
        }

        [Test]
        public void TryApply_WritesOnceForBothHalves()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = Owning(Bell, 2);
            EssenceWallet wallet = new EssenceWallet(12);

            using (StatePersistence persistence = Build(store, inventory, wallet))
            {
                Assert.That(persistence.TryApply(StateChange.Dissolve(Bell, 14), out _), Is.True);
            }

            // Two writes would mean a moment on disk where the copy is gone and the essence has not arrived.
            Assert.That(store.Saved.Count, Is.EqualTo(1));
            Assert.That(store.Saved[0].Essence, Is.EqualTo(26));
            Assert.That(store.Saved[0].Entries[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void TryApply_WritesBeforeItMutates()
        {
            Inventory inventory = Owning(Bell, 2);
            EssenceWallet wallet = new EssenceWallet(12);
            WatchingStore store = new WatchingStore(inventory, wallet);

            using (StatePersistence persistence = Build(store, inventory, wallet))
            {
                persistence.TryApply(StateChange.Dissolve(Bell, 14), out _);
            }

            // The store was handed the state that WOULD exist while the live objects still held the old one.
            Assert.That(store.WrittenEssence, Is.EqualTo(26));
            Assert.That(store.WrittenCount, Is.EqualTo(1));
            Assert.That(store.LiveBalanceAtWrite, Is.EqualTo(12));
            Assert.That(store.LiveCountAtWrite, Is.EqualTo(2));
        }

        [Test]
        public void TryApply_OnRefusal_RaisesApplyRefusedAndMutatesNothing()
        {
            RecordingInventoryStore store = new RecordingInventoryStore { RefusesWrites = true };
            Inventory inventory = Owning(Bell, 2);
            EssenceWallet wallet = new EssenceWallet(12);
            List<string> refusals = new List<string>();
            List<string> saveFailures = new List<string>();

            using (StatePersistence persistence = Build(store, inventory, wallet))
            {
                persistence.ApplyRefused += failure => refusals.Add(failure);
                persistence.SaveFailed += failure => saveFailures.Add(failure);

                Assert.That(persistence.TryApply(StateChange.Dissolve(Bell, 14), out string failure), Is.False);
                Assert.That(failure, Is.EqualTo("the store refused the write"));
            }

            Assert.That(inventory.CountOf(Bell), Is.EqualTo(2), "the copy is not consumed");
            Assert.That(wallet.Balance, Is.EqualTo(12), "and the essence never arrived");
            Assert.That(refusals.Count, Is.EqualTo(1));

            // The two channels mean opposite things: SaveFailed says the state is in memory, ApplyRefused
            // says nothing was applied at all.
            Assert.That(saveFailures, Is.Empty);
        }

        [Test]
        public void GrantWithNoCopy_RaisesNoInventoryChange()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = Owning(Bell, 1);
            EssenceWallet wallet = new EssenceWallet(0);
            List<InventoryChange> changes = new List<InventoryChange>();

            using (StatePersistence persistence = Build(store, inventory, wallet))
            {
                inventory.Changed += change => changes.Add(change);

                Assert.That(persistence.TryApply(StateChange.Grant(100), out _), Is.True);
            }

            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(changes, Is.Empty, "subscribers key on the sign of Delta and have no branch for zero");
        }

        [Test]
        public void StatelessSnapshot_IsRefusedOnBothWriters()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = new Inventory();
            EssenceWallet wallet = new EssenceWallet(50);
            List<string> saveFailures = new List<string>();
            List<string> refusals = new List<string>();

            using (StatePersistence persistence = Build(store, inventory, wallet))
            {
                persistence.SaveFailed += failure => saveFailures.Add(failure);
                persistence.ApplyRefused += failure => refusals.Add(failure);

                // Writer two: a grant that empties the balance leaves a payload the next boot refuses.
                Assert.That(persistence.TryApply(StateChange.Grant(-50), out _), Is.False);
                Assert.That(wallet.Balance, Is.EqualTo(50));
            }

            using (StatePersistence persistence =
                Build(new RecordingInventoryStore(), new Inventory(), new EssenceWallet(0)))
            {
                persistence.SaveFailed += failure => saveFailures.Add(failure);

                // Writer one: the auto-save path, driven directly.
                persistence.Save();
            }

            Assert.That(store.Saved, Is.Empty);
            Assert.That(refusals.Count, Is.EqualTo(1));
            Assert.That(refusals[0], Does.Contain("no state"));
            Assert.That(saveFailures.Count, Is.EqualTo(1));
            Assert.That(saveFailures[0], Does.Contain("no state"));
        }

        [Test]
        public void ReentrantTryApply_Throws()
        {
            RecordingInventoryStore store = new RecordingInventoryStore();
            Inventory inventory = Owning(Bell, 2);
            EssenceWallet wallet = new EssenceWallet(0);

            using (StatePersistence persistence = Build(store, inventory, wallet))
            {
                inventory.Changed += change => persistence.TryApply(StateChange.Grant(10), out _);

                // A handler that starts a second exchange would build its snapshot from half-announced state.
                Assert.Throws<InvalidOperationException>(() =>
                    persistence.TryApply(StateChange.Dissolve(Bell, 14), out _));
            }
        }

        private static StatePersistence Build(IInventoryStore store, Inventory inventory, EssenceWallet wallet)
        {
            return new StatePersistence(store, inventory, wallet, Array.Empty<InventorySnapshotEntry>());
        }

        private static Inventory Owning(RelicId id, int copies)
        {
            Inventory inventory = new Inventory();

            for (int i = 0; i < copies; i++)
            {
                inventory.Add(id);
            }

            return inventory;
        }

        /// <summary>Records what the live objects held at the moment the write was handed over.</summary>
        private sealed class WatchingStore : IInventoryStore
        {
            private readonly Inventory _inventory;
            private readonly EssenceWallet _wallet;

            public WatchingStore(Inventory inventory, EssenceWallet wallet)
            {
                _inventory = inventory;
                _wallet = wallet;
            }

            public int WrittenEssence { get; private set; }

            public int WrittenCount { get; private set; }

            public int LiveBalanceAtWrite { get; private set; }

            public int LiveCountAtWrite { get; private set; }

            public StoredState Load() => StoredState.None();

            public bool TrySave(InventorySnapshot snapshot, out string failure)
            {
                WrittenEssence = snapshot.Essence;
                WrittenCount = snapshot.Entries.Length == 0 ? 0 : snapshot.Entries[0].Count;
                LiveBalanceAtWrite = _wallet.Balance;
                LiveCountAtWrite = _inventory.CountOf(Bell);
                failure = null;
                return true;
            }
        }
    }
}
