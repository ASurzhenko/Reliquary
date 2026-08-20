using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class SetCatalogTests
    {
        [Test]
        public void MemberIdsAreKeptInAuthorOrder()
        {
            RelicSet set = Set("set.tideworn", "relic.tide_lantern", "relic.coral_signet");

            SetCatalog catalog = SetCatalog.Create(new[] { set }, Relics(), out _);

            Assert.That(catalog.TryGet(new SetId("set.tideworn"), out RelicSet found), Is.True);
            Assert.That(found.Members[0].ToString(), Is.EqualTo("relic.tide_lantern"));
            Assert.That(found.Members[1].ToString(), Is.EqualTo("relic.coral_signet"));
        }

        [Test]
        public void DuplicateSetId_IsReportedAndOneIsSkipped()
        {
            SetCatalog catalog = SetCatalog.Create(new[]
            {
                Set("set.tideworn", "relic.coral_signet"),
                Set("set.tideworn", "relic.tide_lantern")
            }, Relics(), out IReadOnlyList<RelicContentIssue> issues);

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(RelicContentSeverity.Error));
            Assert.That(issues[0].Message, Does.Contain("set.tideworn"));
        }

        [Test]
        public void EmptySet_IsSkippedWithAnError()
        {
            SetCatalog catalog = SetCatalog.Create(new[] { Set("set.empty", Array.Empty<string>()) }, Relics(),
                out IReadOnlyList<RelicContentIssue> issues);

            Assert.That(catalog.Count, Is.EqualTo(0), "a set with nothing in it is not a goal");
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(RelicContentSeverity.Error));
        }

        [Test]
        public void MemberNotInRelicCatalogue_KeepsTheSetAndReports()
        {
            SetCatalog catalog = SetCatalog.Create(new[]
            {
                Set("set.tideworn", "relic.coral_signet", "relic.not_in_this_build")
            }, Relics(), out IReadOnlyList<RelicContentIssue> issues);

            // Shrinking the goal would grant the perk for three quarters of a set. The set stays, and stays
            // uncompletable, which is the honest outcome.
            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.TryGet(new SetId("set.tideworn"), out RelicSet found), Is.True);
            Assert.That(found.Members.Count, Is.EqualTo(2));
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(RelicContentSeverity.Error));
            Assert.That(issues[0].Message, Does.Contain("relic.not_in_this_build"));
        }

        [Test]
        public void SetWithNoPerks_IsKeptWithAWarning()
        {
            SetCatalog catalog = SetCatalog.Create(new[] { PerklessSet("set.cosmetic", "relic.coral_signet") },
                Relics(), out IReadOnlyList<RelicContentIssue> issues);

            Assert.That(catalog.Count, Is.EqualTo(1), "progress still tracks; there is simply nothing to grant");
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(RelicContentSeverity.Warning));
        }

        [Test]
        public void SetsContaining_ReturnsEverySetHoldingTheRelic()
        {
            SetCatalog catalog = SetCatalog.Create(new[]
            {
                Set("set.tideworn", "relic.coral_signet", "relic.tide_lantern"),
                Set("set.salvage", "relic.coral_signet"),
                Set("set.embers", "relic.cinder_mask")
            }, Relics(), out _);

            Assert.That(catalog.SetsContaining(new RelicId("relic.coral_signet")).Count, Is.EqualTo(2));
            Assert.That(catalog.SetsContaining(new RelicId("relic.cinder_mask")).Count, Is.EqualTo(1));
            Assert.That(catalog.SetsContaining(new RelicId("relic.pyre_key")), Is.Empty,
                "a relic in no set is a normal state");
        }

        [Test]
        public void All_IsOrdinalBySetId()
        {
            SetCatalog catalog = SetCatalog.Create(new[]
            {
                Set("set.tideworn", "relic.coral_signet"),
                Set("set.embers", "relic.cinder_mask"),
                Set("set.salvage", "relic.tide_lantern")
            }, Relics(), out _);

            Assert.That(catalog.All[0].Id.ToString(), Is.EqualTo("set.embers"));
            Assert.That(catalog.All[1].Id.ToString(), Is.EqualTo("set.salvage"));
            Assert.That(catalog.All[2].Id.ToString(), Is.EqualTo("set.tideworn"));
        }

        private static RelicSet Set(string id, params string[] members)
        {
            return Build(id, new IRelicEffect[] { new FakeEffect(0.25f) }, members);
        }

        private static RelicSet PerklessSet(string id, params string[] members)
        {
            return Build(id, Array.Empty<IRelicEffect>(), members);
        }

        private static RelicSet Build(string id, IRelicEffect[] perks, string[] members)
        {
            List<RelicId> ids = new List<RelicId>(members.Length);

            for (int i = 0; i < members.Length; i++)
            {
                ids.Add(new RelicId(members[i]));
            }

            return new RelicSet(new SetId(id), ids, perks);
        }

        private static RelicCatalog Relics()
        {
            return RelicCatalog.Create(new[]
            {
                MakeRelic("relic.coral_signet"),
                MakeRelic("relic.tide_lantern"),
                MakeRelic("relic.cinder_mask"),
                MakeRelic("relic.pyre_key")
            }, out _);
        }

        private static Relic MakeRelic(string id)
        {
            return new Relic(new RelicId(id), 10, 100, Array.Empty<IRelicEffect>());
        }
    }
}
