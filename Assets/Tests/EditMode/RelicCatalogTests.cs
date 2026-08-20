using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class RelicCatalogTests
    {
        [Test]
        public void DuplicateIds_ReportedAndOneKept()
        {
            Relic first = Make("relic.sunken_crown");
            Relic second = Make("relic.sunken_crown");

            RelicCatalog catalog = RelicCatalog.Create(new[] { first, second },
                out IReadOnlyList<RelicContentIssue> issues);

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(RelicContentSeverity.Error));
        }

        [Test]
        public void NullEntry_ReportedAndSkipped()
        {
            RelicCatalog catalog = RelicCatalog.Create(new[] { Make("relic.sunken_crown"), null },
                out IReadOnlyList<RelicContentIssue> issues);

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Severity, Is.EqualTo(RelicContentSeverity.Error));
        }

        [Test]
        public void TryGet_FindsByIdAndMissesUnknown()
        {
            RelicCatalog catalog = RelicCatalog.Create(new[] { Make("relic.sunken_crown") }, out _);

            Assert.That(catalog.TryGet(new RelicId("relic.sunken_crown"), out Relic found), Is.True);
            Assert.That(found.Id, Is.EqualTo(new RelicId("relic.sunken_crown")));
            Assert.That(catalog.Contains(new RelicId("relic.tideworn_sextant")), Is.False);
            Assert.That(catalog.TryGet(new RelicId("relic.tideworn_sextant"), out _), Is.False);
        }

        [Test]
        public void All_IsOrdinalById_RegardlessOfInputOrder()
        {
            RelicCatalog forwards = RelicCatalog.Create(
                new[] { Make("relic.a"), Make("relic.b"), Make("relic.c") }, out _);
            RelicCatalog backwards = RelicCatalog.Create(
                new[] { Make("relic.c"), Make("relic.b"), Make("relic.a") }, out _);

            Assert.That(Ids(forwards), Is.EqualTo(new[] { "relic.a", "relic.b", "relic.c" }));
            Assert.That(Ids(backwards), Is.EqualTo(new[] { "relic.a", "relic.b", "relic.c" }));
        }

        [Test]
        public void EmptyInput_YieldsEmptyCatalogueWithNoIssues()
        {
            RelicCatalog catalog = RelicCatalog.Create(Array.Empty<Relic>(),
                out IReadOnlyList<RelicContentIssue> issues);

            Assert.That(catalog.Count, Is.EqualTo(0));
            Assert.That(issues.Count, Is.EqualTo(0));
        }

        private static Relic Make(string id)
        {
            return new Relic(new RelicId(id), 10, 100, Array.Empty<IRelicEffect>());
        }

        private static string[] Ids(RelicCatalog catalog)
        {
            string[] ids = new string[catalog.Count];

            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = catalog.All[i].Id.ToString();
            }

            return ids;
        }
    }
}
