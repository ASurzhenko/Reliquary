using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class SetCompletionWatcherTests
    {
        [Test]
        public void SeededWithACompleteSet_RaisesNothing()
        {
            // The restart claim, as a unit: a set completed in an earlier session has no transition left to
            // make, so the card does not reappear at every launch. Nothing is persisted to know this — the
            // save already records the ownership completion is a function of.
            Inventory inventory = Owning("relic.a", "relic.b");
            List<SetId> announced = new List<SetId>();

            using (SetCompletionWatcher watcher = new SetCompletionWatcher(Sets(), inventory))
            {
                watcher.Completed += completion => announced.Add(completion.Id);

                inventory.Add(new RelicId("relic.a"));
            }

            Assert.That(announced, Is.Empty);
        }

        [Test]
        public void CompletingASet_RaisesOnce()
        {
            Inventory inventory = Owning("relic.a");
            List<SetId> announced = new List<SetId>();

            using (SetCompletionWatcher watcher = new SetCompletionWatcher(Sets(), inventory))
            {
                watcher.Completed += completion => announced.Add(completion.Id);

                inventory.Add(new RelicId("relic.b"));
            }

            Assert.That(announced.Count, Is.EqualTo(1));
            Assert.That(announced[0].ToString(), Is.EqualTo("set.tideworn"));
        }

        [Test]
        public void AnotherCopyOfAMember_DoesNotRaiseAgain()
        {
            Inventory inventory = Owning("relic.a");
            List<SetId> announced = new List<SetId>();

            using (SetCompletionWatcher watcher = new SetCompletionWatcher(Sets(), inventory))
            {
                watcher.Completed += completion => announced.Add(completion.Id);

                inventory.Add(new RelicId("relic.b"));
                inventory.Add(new RelicId("relic.b"));
                inventory.Add(new RelicId("relic.a"));
            }

            Assert.That(announced.Count, Is.EqualTo(1), "once per completion transition, not once per change");
        }

        [Test]
        public void CompletingASecondSet_RaisesForThatSetOnly()
        {
            Inventory inventory = Owning("relic.a", "relic.b", "relic.c");
            List<SetId> announced = new List<SetId>();

            using (SetCompletionWatcher watcher = new SetCompletionWatcher(Sets(), inventory))
            {
                watcher.Completed += completion => announced.Add(completion.Id);

                inventory.Add(new RelicId("relic.d"));
            }

            Assert.That(announced.Count, Is.EqualTo(1));
            Assert.That(announced[0].ToString(), Is.EqualTo("set.emberwrought"));
        }

        [Test]
        public void DisposedWatcher_RaisesNothing()
        {
            Inventory inventory = Owning("relic.a");
            List<SetId> announced = new List<SetId>();

            SetCompletionWatcher watcher = new SetCompletionWatcher(Sets(), inventory);
            watcher.Completed += completion => announced.Add(completion.Id);
            watcher.Dispose();

            inventory.Add(new RelicId("relic.b"));

            Assert.That(announced, Is.Empty, "the subscription is paired");
        }

        private static Inventory Owning(params string[] ids)
        {
            Inventory inventory = new Inventory();

            for (int i = 0; i < ids.Length; i++)
            {
                inventory.Add(new RelicId(ids[i]));
            }

            return inventory;
        }

        private static SetCatalog Sets()
        {
            RelicCatalog relics = RelicCatalog.Create(new[]
            {
                MakeRelic("relic.a"), MakeRelic("relic.b"), MakeRelic("relic.c"), MakeRelic("relic.d")
            }, out _);

            return SetCatalog.Create(new[]
            {
                Set("set.tideworn", "relic.a", "relic.b"),
                Set("set.emberwrought", "relic.c", "relic.d")
            }, relics, out _);
        }

        private static RelicSet Set(string id, params string[] members)
        {
            List<RelicId> ids = new List<RelicId>(members.Length);

            for (int i = 0; i < members.Length; i++)
            {
                ids.Add(new RelicId(members[i]));
            }

            return new RelicSet(new SetId(id), ids, new IRelicEffect[] { new FakeEffect(0.1f) });
        }

        private static Relic MakeRelic(string id)
        {
            return new Relic(new RelicId(id), 10, 100, Array.Empty<IRelicEffect>());
        }
    }
}
