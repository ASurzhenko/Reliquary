using System;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class EssenceExchangeTests
    {
        private static readonly RelicId Bell = new RelicId("relic.drowned_bell");
        private static readonly RelicId Worthless = new RelicId("relic.worthless");
        private static readonly RelicId Unknown = new RelicId("relic.not_in_this_build");

        [Test]
        public void SingleCopy_IsRefusedAsNoSpare()
        {
            Fixture fixture = new Fixture(Bell, 1);

            DissolveResult result = fixture.Exchange.TryDissolve(Bell);

            // The last copy of a relic is never consumable, which is also what keeps completion monotonic
            // inside a session.
            Assert.That(result.Outcome, Is.EqualTo(DissolveOutcome.NoSpareCopy));
            Assert.That(fixture.Inventory.CountOf(Bell), Is.EqualTo(1));
        }

        [Test]
        public void UnknownRelic_IsRefused()
        {
            Fixture fixture = new Fixture(Bell, 2);

            Assert.That(fixture.Exchange.TryDissolve(Unknown).Outcome, Is.EqualTo(DissolveOutcome.NoSuchRelic));
        }

        [Test]
        public void ZeroValueRelic_IsRefusedAsNoYield()
        {
            Fixture fixture = new Fixture(Worthless, 2);

            // Destroying a copy for nothing is pure loss, so the ambiguous branch refuses to consume.
            Assert.That(fixture.Exchange.TryDissolve(Worthless).Outcome, Is.EqualTo(DissolveOutcome.NoYield));
            Assert.That(fixture.Inventory.CountOf(Worthless), Is.EqualTo(2));
        }

        [Test]
        public void Dissolve_RemovesOneCopyAndCreditsTheYield()
        {
            Fixture fixture = new Fixture(Bell, 2, startingEssence: 12);

            DissolveResult result = fixture.Exchange.TryDissolve(Bell);

            Assert.That(result.Outcome, Is.EqualTo(DissolveOutcome.Dissolved));
            Assert.That(result.Yield, Is.EqualTo(14));
            Assert.That(fixture.Inventory.CountOf(Bell), Is.EqualTo(1));
            Assert.That(fixture.Wallet.Balance, Is.EqualTo(26));
            Assert.That(result.Balance, Is.EqualTo(26));
        }

        [Test]
        public void Yield_UsesTheEssenceMultiplier()
        {
            Fixture fixture = new Fixture(Bell, 2, essenceBonus: 0.25f);

            // Floor rather than round, so the number on the button is never larger than what is paid.
            Assert.That(fixture.Exchange.TryDissolve(Bell).Yield, Is.EqualTo(17));
        }

        [Test]
        public void RefusedWrite_ChangesNothingAndReportsNotSaved()
        {
            Fixture fixture = new Fixture(Bell, 2, startingEssence: 12);
            fixture.Store.RefusesWrites = true;

            DissolveResult result = fixture.Exchange.TryDissolve(Bell);

            Assert.That(result.Outcome, Is.EqualTo(DissolveOutcome.NotSaved));
            Assert.That(result.Failure, Is.EqualTo("the store refused the write"));
            Assert.That(fixture.Store.Saved, Is.Empty);
        }

        [Test]
        public void RefusedWrite_LeavesTheCopyAndTheBalanceExactlyAsTheyWere()
        {
            Fixture fixture = new Fixture(Bell, 2, startingEssence: 12);
            fixture.Store.RefusesWrites = true;

            fixture.Exchange.TryDissolve(Bell);

            // A grant that fails to persist is a gift we can repeat; a spend that fails to persist is a theft
            // we cannot undo. Both objects have to be untouched, not just the one the test author remembered.
            Assert.That(fixture.Inventory.CountOf(Bell), Is.EqualTo(2));
            Assert.That(fixture.Wallet.Balance, Is.EqualTo(12));
        }

        [Test]
        public void Preview_MatchesWhatTryDissolveActuallyPays()
        {
            Fixture fixture = new Fixture(Bell, 2, essenceBonus: 0.1f);

            DissolvePreview preview = fixture.Exchange.Preview(Bell);
            DissolveResult result = fixture.Exchange.TryDissolve(Bell);

            Assert.That(preview.CanDissolve, Is.True);
            Assert.That(result.Yield, Is.EqualTo(preview.Yield), "the button never lies");
        }

        private sealed class Fixture
        {
            public Fixture(RelicId owned, int copies, int startingEssence = 0, float essenceBonus = 0f)
            {
                IRelicEffect[] effects = essenceBonus == 0f
                    ? Array.Empty<IRelicEffect>()
                    : new IRelicEffect[] { new FakeEffect(essenceBonus) };

                RelicCatalog relics = RelicCatalog.Create(new[]
                {
                    new Relic(Bell, 14, 75, effects),
                    new Relic(Worthless, 0, 10, Array.Empty<IRelicEffect>())
                }, out _);

                SetCatalog sets = SetCatalog.Create(Array.Empty<RelicSet>(), relics, out _);

                Inventory = new Inventory();

                for (int i = 0; i < copies; i++)
                {
                    Inventory.Add(owned);
                }

                Wallet = new EssenceWallet(startingEssence);
                Store = new RecordingInventoryStore();
                Persistence = new StatePersistence(Store, Inventory, Wallet, Array.Empty<InventorySnapshotEntry>());
                Exchange = new EssenceExchange(relics, sets, Inventory, Wallet, Persistence);
            }

            public Inventory Inventory { get; }

            public EssenceWallet Wallet { get; }

            public RecordingInventoryStore Store { get; }

            public StatePersistence Persistence { get; }

            public EssenceExchange Exchange { get; }
        }
    }
}
