using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class TraderTests
    {
        private static readonly RelicId A = new RelicId("relic.a");
        private static readonly RelicId B = new RelicId("relic.b");
        private static readonly RelicId C = new RelicId("relic.c");
        private static readonly RelicId D = new RelicId("relic.d");
        private static readonly RelicId Orphan = new RelicId("relic.not_in_this_build");

        [Test]
        public void OfferIsAlwaysARelicThePlayerDoesNotOwn()
        {
            Fixture fixture = new Fixture(owned: new[] { A, B });

            TraderOffer offer = fixture.Trader.CurrentOffer;

            // No buy-then-dissolve arbitrage is possible regardless of the price multiplier.
            Assert.That(offer.HasOffer, Is.True);
            Assert.That(fixture.Inventory.Owns(offer.Relic), Is.False);
        }

        [Test]
        public void OfferTargetsTheNearestIncompleteSet()
        {
            Fixture fixture = new Fixture(
                owned: new[] { A },
                sets: new[] { Set("set.far", C, D), Set("set.near", A, B) });

            TraderOffer offer = fixture.Trader.CurrentOffer;

            // The essence earned from duplicates buys the relic that actually closes a set.
            Assert.That(offer.Relic, Is.EqualTo(B));
            Assert.That(offer.FocusSet.ToString(), Is.EqualTo("set.near"));
            Assert.That(offer.FocusProgress.Owned, Is.EqualTo(1));
            Assert.That(offer.FocusProgress.Total, Is.EqualTo(2));
        }

        [Test]
        public void OfferRotatesWhenTheCollectionGrows()
        {
            Fixture fixture = new Fixture(owned: Array.Empty<RelicId>());

            RelicId before = fixture.Trader.CurrentOffer.Relic;
            fixture.Inventory.Add(A);
            RelicId after = fixture.Trader.CurrentOffer.Relic;

            // The seed is the number of distinct relics owned, so finding one moves the offer along.
            Assert.That(after, Is.Not.EqualTo(before));
        }

        [Test]
        public void OfferIsStableWhileTheCollectionIs()
        {
            Fixture fixture = new Fixture(owned: new[] { A });

            Assert.That(fixture.Trader.CurrentOffer.Relic, Is.EqualTo(fixture.Trader.CurrentOffer.Relic),
                "an offer recomputed every frame must not flicker between them");
        }

        [Test]
        public void EmptyCatalogue_ReportsNoCatalogue()
        {
            Fixture fixture = new Fixture(owned: Array.Empty<RelicId>(), relics: Array.Empty<Relic>());

            TraderOffer offer = fixture.Trader.CurrentOffer;

            // Checked first, so it wins over NothingMissing when both hold: nothing is missing because
            // nothing exists is a content failure, not a finished collection.
            Assert.That(offer.HasOffer, Is.False);
            Assert.That(offer.Absence, Is.EqualTo(TraderAbsence.NoCatalogue));
        }

        [Test]
        public void NoMissingRelics_ReportsNothingMissing()
        {
            Fixture fixture = new Fixture(owned: new[] { A, B, C, D });

            TraderOffer offer = fixture.Trader.CurrentOffer;

            Assert.That(offer.HasOffer, Is.False);
            Assert.That(offer.Absence, Is.EqualTo(TraderAbsence.NothingMissing), "a finished state, not an error");
        }

        [Test]
        public void SetWhoseOnlyMissingMembersAreOrphans_IsNotChosenAsFocus()
        {
            Fixture fixture = new Fixture(
                owned: new[] { A },
                sets: new[] { Set("set.a_orphaned", A, Orphan), Set("set.b_real", B, C) });

            // "Fewest missing members" alone would choose the orphaned set — zero of its missing members can
            // be bought — leaving an empty pool and a modulo by zero on every read of the offer.
            TraderOffer offer = fixture.Trader.CurrentOffer;

            Assert.That(offer.HasOffer, Is.True);
            Assert.That(offer.FocusSet.ToString(), Is.EqualTo("set.b_real"));
        }

        [Test]
        public void MemberOutsideTheCatalogue_IsNeverOffered()
        {
            Fixture fixture = new Fixture(
                owned: new[] { A },
                sets: new[] { Set("set.a_orphaned", A, Orphan, B) });

            Assert.That(fixture.Trader.CurrentOffer.Relic, Is.Not.EqualTo(Orphan));
            Assert.That(fixture.Trader.TryBuy(Orphan).Outcome, Is.EqualTo(PurchaseOutcome.NoSuchRelic));
        }

        [Test]
        public void BuyingAnOwnedRelic_ReportsAlreadyOwned()
        {
            Fixture fixture = new Fixture(owned: new[] { A }, essence: 100);

            // This fails if the ownership check moves below the offer comparison: an owned relic is never the
            // offer, so the generic OfferChanged would answer instead of the specific reason.
            Assert.That(fixture.Trader.TryBuy(A).Outcome, Is.EqualTo(PurchaseOutcome.AlreadyOwned));
        }

        [Test]
        public void BuyingAStaleId_ReportsOfferChanged()
        {
            Fixture fixture = new Fixture(owned: Array.Empty<RelicId>(), essence: 100);

            RelicId offered = fixture.Trader.CurrentOffer.Relic;
            RelicId other = offered == A ? B : A;

            Assert.That(fixture.Trader.TryBuy(other).Outcome, Is.EqualTo(PurchaseOutcome.OfferChanged));
            Assert.That(fixture.Inventory.Owns(other), Is.False);
        }

        [Test]
        public void Purchase_SpendsExactlyThePriceAndGrantsExactlyOneCopy()
        {
            Fixture oneShort = new Fixture(owned: Array.Empty<RelicId>(), essence: 29);
            TraderOffer offer = oneShort.Trader.CurrentOffer;

            Assert.That(offer.Price, Is.EqualTo(30), "10 essence at a multiplier of 3, above a floor of 10");
            Assert.That(offer.CanAfford, Is.False);
            Assert.That(offer.Deficit, Is.EqualTo(1));

            PurchaseResult refused = oneShort.Trader.TryBuy(offer.Relic);

            Assert.That(refused.Outcome, Is.EqualTo(PurchaseOutcome.NotEnoughEssence));
            Assert.That(refused.Deficit, Is.EqualTo(1), "the number the screen renders as NEED 1 MORE");
            Assert.That(oneShort.Wallet.Balance, Is.EqualTo(29));

            Fixture rich = new Fixture(owned: Array.Empty<RelicId>(), essence: 30);
            PurchaseResult bought = rich.Trader.TryBuy(rich.Trader.CurrentOffer.Relic);

            Assert.That(bought.Outcome, Is.EqualTo(PurchaseOutcome.Purchased));
            Assert.That(rich.Wallet.Balance, Is.EqualTo(0));
            Assert.That(rich.Inventory.CountOf(bought.Relic), Is.EqualTo(1));
            Assert.That(rich.Store.Saved.Count, Is.EqualTo(1), "one write carries both halves");
        }

        private static RelicSet Set(string id, params RelicId[] members)
        {
            return new RelicSet(new SetId(id), new List<RelicId>(members),
                new IRelicEffect[] { new FakeEffect(0.1f) });
        }

        private sealed class Fixture
        {
            public Fixture(RelicId[] owned, RelicSet[] sets = null, Relic[] relics = null, int essence = 0)
            {
                RelicCatalog catalog = RelicCatalog.Create(relics ?? Everything(), out _);
                SetCatalog setCatalog = SetCatalog.Create(sets ?? Array.Empty<RelicSet>(), catalog, out _);

                Inventory = new Inventory();

                for (int i = 0; i < owned.Length; i++)
                {
                    Inventory.Add(owned[i]);
                }

                Wallet = new EssenceWallet(essence);
                Store = new RecordingInventoryStore();
                Persistence = new StatePersistence(Store, Inventory, Wallet, Array.Empty<InventorySnapshotEntry>());
                Trader = new Trader(catalog, setCatalog, Inventory, Wallet, Persistence,
                    new EconomySettings(3f, 10));
            }

            public Inventory Inventory { get; }

            public EssenceWallet Wallet { get; }

            public RecordingInventoryStore Store { get; }

            public StatePersistence Persistence { get; }

            public Trader Trader { get; }

            private static Relic[] Everything()
            {
                return new[] { MakeRelic(A), MakeRelic(B), MakeRelic(C), MakeRelic(D) };
            }

            private static Relic MakeRelic(RelicId id)
            {
                return new Relic(id, 10, 100, Array.Empty<IRelicEffect>());
            }
        }
    }
}
