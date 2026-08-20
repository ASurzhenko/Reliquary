using System.Collections.Generic;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>
    /// The wallet's mutators are internal so that nothing outside the rules can move a balance without going
    /// through the single write. The tests reach them through InternalsVisibleTo, because a guard nobody can
    /// drive is not a guard.
    /// </summary>
    public class EssenceWalletTests
    {
        [Test]
        public void CreditRaisesChangedWithDeltaAndBalance()
        {
            EssenceWallet wallet = new EssenceWallet(12);
            List<EssenceChange> raised = new List<EssenceChange>();
            wallet.Changed += change => raised.Add(change);

            wallet.Announce(wallet.ApplySilently(14, EssenceChangeReason.Dissolved, new RelicId("relic.drowned_bell")));

            Assert.That(wallet.Balance, Is.EqualTo(26));
            Assert.That(raised.Count, Is.EqualTo(1));
            Assert.That(raised[0].Delta, Is.EqualTo(14));
            Assert.That(raised[0].Balance, Is.EqualTo(26), "the balance AFTER the change, not before it");
        }

        [Test]
        public void SpendCannotGoNegative()
        {
            EssenceWallet wallet = new EssenceWallet(10);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                wallet.ApplySilently(-11, EssenceChangeReason.Spent, new RelicId("relic.pyre_key")));

            Assert.That(wallet.Balance, Is.EqualTo(10), "a refused move leaves the balance where it was");
        }

        [Test]
        public void CanAfford_IsFalseAtOneShort()
        {
            EssenceWallet wallet = new EssenceWallet(41);

            Assert.That(wallet.CanAfford(41), Is.True);
            Assert.That(wallet.CanAfford(42), Is.False);
        }

        [Test]
        public void ChangeCarriesTheSubjectRelic()
        {
            EssenceWallet wallet = new EssenceWallet(0);
            EssenceChange change = wallet.ApplySilently(18, EssenceChangeReason.Dissolved,
                new RelicId("relic.pyre_key"));

            // One event is enough to write "+18 essence — Pyre Key dissolved"; nothing has to correlate two.
            Assert.That(change.Subject.ToString(), Is.EqualTo("relic.pyre_key"));
            Assert.That(change.Reason, Is.EqualTo(EssenceChangeReason.Dissolved));
        }
    }
}
