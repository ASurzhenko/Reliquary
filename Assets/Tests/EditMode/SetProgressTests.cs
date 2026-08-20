using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class SetProgressTests
    {
        [Test]
        public void EmptyInventory_IsUnstartedAndZeroFraction()
        {
            SetProgress progress = SetProgress.For(Set("relic.a", "relic.b"), new Inventory());

            Assert.That(progress.Owned, Is.EqualTo(0));
            Assert.That(progress.Total, Is.EqualTo(2));
            Assert.That(progress.IsUnstarted, Is.True);
            Assert.That(progress.IsComplete, Is.False);
            Assert.That(progress.Fraction, Is.EqualTo(0f));
        }

        [Test]
        public void PartialOwnership_CountsDistinctMembersOnly()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.a"));
            inventory.Add(new RelicId("relic.a"));
            inventory.Add(new RelicId("relic.a"));

            SetProgress progress = SetProgress.For(Set("relic.a", "relic.b"), inventory);

            // Three copies of one member is one member. A duplicate is worth essence, not progress.
            Assert.That(progress.Owned, Is.EqualTo(1));
            Assert.That(progress.IsComplete, Is.False);
        }

        [Test]
        public void AllMembersOwned_IsComplete()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.a"));
            inventory.Add(new RelicId("relic.b"));

            SetProgress progress = SetProgress.For(Set("relic.a", "relic.b"), inventory);

            Assert.That(progress.IsComplete, Is.True);
            Assert.That(progress.Missing, Is.Empty);
        }

        [Test]
        public void Fraction_IsOwnedOverTotal()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.a"));

            SetProgress progress = SetProgress.For(Set("relic.a", "relic.b", "relic.c", "relic.d"), inventory);

            // On the struct so that no view divides two domain values.
            Assert.That(progress.Fraction, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void EmptySet_HasZeroFractionAndDoesNotDivide()
        {
            SetProgress progress = SetProgress.For(Set(), new Inventory());

            Assert.That(progress.Total, Is.EqualTo(0));
            Assert.That(progress.Fraction, Is.EqualTo(0f));
            Assert.That(progress.IsComplete, Is.False, "an empty set is not a completed one");
        }

        [Test]
        public void Missing_NamesExactlyTheUnownedMembers()
        {
            Inventory inventory = new Inventory();
            inventory.Add(new RelicId("relic.b"));

            SetProgress progress = SetProgress.For(Set("relic.a", "relic.b", "relic.c"), inventory);

            Assert.That(progress.Missing.Count, Is.EqualTo(2));
            Assert.That(progress.Missing[0].ToString(), Is.EqualTo("relic.a"));
            Assert.That(progress.Missing[1].ToString(), Is.EqualTo("relic.c"));
        }

        private static RelicSet Set(params string[] members)
        {
            List<RelicId> ids = new List<RelicId>(members.Length);

            for (int i = 0; i < members.Length; i++)
            {
                ids.Add(new RelicId(members[i]));
            }

            return new RelicSet(new SetId("set.under_test"), ids, Array.Empty<IRelicEffect>());
        }
    }
}
