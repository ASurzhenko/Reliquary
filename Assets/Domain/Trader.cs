using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>Why there is no offer. None while there is one.</summary>
    public enum TraderAbsence
    {
        None,

        /// <summary>No relic exists at all — a content failure, not a finished collection.</summary>
        NoCatalogue,

        /// <summary>Every relic is owned. A finished state, and the trader says so.</summary>
        NothingMissing
    }

    /// <summary>
    /// What the trader is selling right now. Recomputed from collection state on every read: nothing about
    /// the offer is stored, so there is no rotation counter to validate on load or clamp when a save says -1.
    /// </summary>
    public readonly struct TraderOffer
    {
        private TraderOffer(bool hasOffer, RelicId relic, int price, int balance, SetId focusSet,
            SetProgress focusProgress, TraderAbsence absence)
        {
            HasOffer = hasOffer;
            Relic = relic;
            Price = price;
            CanAfford = hasOffer && balance >= price;
            Deficit = hasOffer && balance < price ? price - balance : 0;
            FocusSet = focusSet;
            FocusProgress = focusProgress;
            Absence = absence;
        }

        internal static TraderOffer Of(RelicId relic, int price, int balance, SetId focusSet,
            SetProgress focusProgress) =>
            new TraderOffer(true, relic, price, balance, focusSet, focusProgress, TraderAbsence.None);

        internal static TraderOffer None(TraderAbsence absence) =>
            new TraderOffer(false, default, 0, 0, default, default, absence);

        public bool HasOffer { get; }

        public RelicId Relic { get; }

        public int Price { get; }

        public bool CanAfford { get; }

        /// <summary>How much more essence is needed. 0 when the offer is affordable or absent.</summary>
        public int Deficit { get; }

        /// <summary>
        /// The set this offer moves along. Invalid — never compared against default — when the offer closes
        /// no set, which is what SetId.IsValid answers.
        /// </summary>
        public SetId FocusSet { get; }

        public SetProgress FocusProgress { get; }

        /// <summary>The empty state, which lives here rather than on a purchase: there is nothing to press.</summary>
        public TraderAbsence Absence { get; }
    }

    public enum PurchaseOutcome
    {
        Purchased,

        /// <summary>The id is not in this build's catalogue.</summary>
        NoSuchRelic,

        /// <summary>The player already owns it. Checked before the offer, so the specific answer wins.</summary>
        AlreadyOwned,

        /// <summary>The request names something that is no longer the offer.</summary>
        OfferChanged,

        /// <summary>The balance is short. The result carries by how much.</summary>
        NotEnoughEssence,

        /// <summary>The write was refused, so nothing was applied.</summary>
        NotSaved
    }

    public readonly struct PurchaseResult
    {
        private PurchaseResult(PurchaseOutcome outcome, RelicId relic, int price, int balance, int deficit,
            bool completedASet, string failure)
        {
            Outcome = outcome;
            Relic = relic;
            Price = price;
            Balance = balance;
            Deficit = deficit;
            CompletedASet = completedASet;
            Failure = failure;
        }

        internal static PurchaseResult Purchased(RelicId relic, int price, int balance, bool completedASet) =>
            new PurchaseResult(PurchaseOutcome.Purchased, relic, price, balance, 0, completedASet, null);

        internal static PurchaseResult Refused(PurchaseOutcome outcome, RelicId relic, int price, int balance,
            int deficit = 0, string failure = null) =>
            new PurchaseResult(outcome, relic, price, balance, deficit, false, failure);

        public PurchaseOutcome Outcome { get; }

        public bool Succeeded => Outcome == PurchaseOutcome.Purchased;

        public RelicId Relic { get; }

        public int Price { get; }

        /// <summary>The balance after the purchase, or the untouched balance on a refusal.</summary>
        public int Balance { get; }

        /// <summary>How much more essence was needed, when the outcome is NotEnoughEssence.</summary>
        public int Deficit { get; }

        /// <summary>True when this copy was the one that completed a set.</summary>
        public bool CompletedASet { get; }

        /// <summary>The store's reason, when the outcome is NotSaved. Null otherwise.</summary>
        public string Failure { get; }
    }

    /// <summary>
    /// Sells one missing relic at a time, chosen to close the set the player is nearest to finishing. The
    /// offer rotates because the collection moves, not because a counter was stored: the seed is how many
    /// distinct relics are owned, which never decreases.
    /// </summary>
    public sealed class Trader
    {
        private readonly RelicCatalog _relics;
        private readonly SetCatalog _sets;
        private readonly Inventory _inventory;
        private readonly EssenceWallet _wallet;
        private readonly StatePersistence _persistence;
        private readonly EconomySettings _economy;

        public Trader(RelicCatalog relics, SetCatalog sets, Inventory inventory, EssenceWallet wallet,
            StatePersistence persistence, EconomySettings economy)
        {
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _sets = sets;
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        public TraderOffer CurrentOffer
        {
            get
            {
                // 1 — no catalogue wins over "nothing is missing": nothing is missing because nothing exists.
                if (_relics.Count == 0)
                {
                    return TraderOffer.None(TraderAbsence.NoCatalogue);
                }

                // 2 — ordinal by id, because RelicCatalog.All is.
                List<RelicId> missing = Missing();

                if (missing.Count == 0)
                {
                    return TraderOffer.None(TraderAbsence.NothingMissing);
                }

                // 3 — the nearest incomplete set that this trader can actually help with. "At least one
                // member in Missing" is what keeps the pool non-empty: a set whose only missing members are
                // relics this build lacks has the fewest purchasable ones — zero — and would otherwise win.
                RelicSet focus = FocusSet(missing);

                // 4 — non-empty by construction of step 3, and by step 2 when there is no focus.
                List<RelicId> pool = focus == null ? missing : MembersIn(focus, missing);

                // 5 — the seed is a count, never a position: it moves whenever a relic is found or bought.
                RelicId offered = pool[_inventory.DistinctCount % pool.Count];
                _relics.TryGet(offered, out Relic relic);

                return TraderOffer.Of(offered, _economy.PriceOf(relic), _wallet.Balance,
                    focus == null ? default : focus.Id,
                    focus == null ? default : SetProgress.For(focus, _inventory));
            }
        }

        public PurchaseResult TryBuy(RelicId requested)
        {
            // 1 — everything below reads relic data.
            if (!requested.IsValid || !_relics.TryGet(requested, out Relic relic))
            {
                return PurchaseResult.Refused(PurchaseOutcome.NoSuchRelic, requested, 0, _wallet.Balance);
            }

            // 2 — before the offer comparison, so the more specific answer wins and this branch is reachable
            // at all: the offer is drawn from the missing relics, so an owned relic is never the offer.
            if (_inventory.Owns(requested))
            {
                return PurchaseResult.Refused(PurchaseOutcome.AlreadyOwned, requested, 0, _wallet.Balance);
            }

            // 3 — the stale-request terminal. The pool moves whenever the collection does, so a screen drawn
            // one acquisition ago can name a relic that is no longer for sale.
            TraderOffer offer = CurrentOffer;

            if (!offer.HasOffer || offer.Relic != requested)
            {
                return PurchaseResult.Refused(PurchaseOutcome.OfferChanged, requested, 0, _wallet.Balance);
            }

            // 4
            if (!_wallet.CanAfford(offer.Price))
            {
                return PurchaseResult.Refused(PurchaseOutcome.NotEnoughEssence, requested, offer.Price,
                    _wallet.Balance, offer.Price - _wallet.Balance);
            }

            // 5 — nothing is spent until the whole resulting state is on disk.
            if (!_persistence.TryApply(StateChange.Purchase(requested, offer.Price), out string failure))
            {
                return PurchaseResult.Refused(PurchaseOutcome.NotSaved, requested, offer.Price,
                    _wallet.Balance, 0, failure);
            }

            return PurchaseResult.Purchased(requested, offer.Price, _wallet.Balance, CompletesASet(requested));
        }

        private List<RelicId> Missing()
        {
            IReadOnlyList<Relic> all = _relics.All;
            List<RelicId> missing = new List<RelicId>(all.Count);

            for (int i = 0; i < all.Count; i++)
            {
                if (!_inventory.Owns(all[i].Id))
                {
                    missing.Add(all[i].Id);
                }
            }

            return missing;
        }

        private RelicSet FocusSet(List<RelicId> missing)
        {
            if (_sets == null)
            {
                return null;
            }

            IReadOnlyList<RelicSet> all = _sets.All;
            RelicSet focus = null;
            int fewest = int.MaxValue;

            // All is ordinal by id, and only a strictly smaller count replaces the incumbent, so a tie is
            // settled ordinally without a second comparison.
            for (int i = 0; i < all.Count; i++)
            {
                RelicSet candidate = all[i];

                if (SetProgress.For(candidate, _inventory).IsComplete)
                {
                    continue;
                }

                int purchasable = MembersIn(candidate, missing).Count;

                if (purchasable == 0 || purchasable >= fewest)
                {
                    continue;
                }

                fewest = purchasable;
                focus = candidate;
            }

            return focus;
        }

        private static List<RelicId> MembersIn(RelicSet set, List<RelicId> missing)
        {
            List<RelicId> pool = new List<RelicId>(missing.Count);

            // Walked in the missing list's order, so the pool is ordinal by id and the same state produces
            // the same offer on every machine.
            for (int i = 0; i < missing.Count; i++)
            {
                if (set.Contains(missing[i]))
                {
                    pool.Add(missing[i]);
                }
            }

            return pool;
        }

        private bool CompletesASet(RelicId relic)
        {
            if (_sets == null)
            {
                return false;
            }

            IReadOnlyList<RelicSet> holders = _sets.SetsContaining(relic);

            for (int i = 0; i < holders.Count; i++)
            {
                if (SetProgress.For(holders[i], _inventory).IsComplete)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
