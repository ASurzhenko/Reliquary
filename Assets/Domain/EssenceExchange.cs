using System;

namespace Reliquary.Domain
{
    public enum DissolveOutcome
    {
        Dissolved,

        /// <summary>The id is not in this build's catalogue.</summary>
        NoSuchRelic,

        /// <summary>Only one copy is owned. The last copy of a relic is never consumable.</summary>
        NoSpareCopy,

        /// <summary>The copy would pay nothing, and destroying it for nothing is pure loss.</summary>
        NoYield,

        /// <summary>The write was refused, so nothing was applied.</summary>
        NotSaved
    }

    /// <summary>What a dissolve would do, without doing it.</summary>
    public readonly struct DissolvePreview
    {
        private DissolvePreview(bool canDissolve, int yield, DissolveOutcome refusal)
        {
            CanDissolve = canDissolve;
            Yield = yield;
            Refusal = refusal;
        }

        internal static DissolvePreview Allowed(int yield) =>
            new DissolvePreview(true, yield, DissolveOutcome.Dissolved);

        internal static DissolvePreview Refused(DissolveOutcome refusal, int yield = 0) =>
            new DissolvePreview(false, yield, refusal);

        public bool CanDissolve { get; }

        /// <summary>What it would pay at the current modifiers. The number on the button.</summary>
        public int Yield { get; }

        /// <summary>Why not, when it cannot. Dissolved when it can.</summary>
        public DissolveOutcome Refusal { get; }
    }

    public readonly struct DissolveResult
    {
        private DissolveResult(DissolveOutcome outcome, RelicId relic, int yield, int balance, string failure)
        {
            Outcome = outcome;
            Relic = relic;
            Yield = yield;
            Balance = balance;
            Failure = failure;
        }

        internal static DissolveResult Dissolved(RelicId relic, int yield, int balance) =>
            new DissolveResult(DissolveOutcome.Dissolved, relic, yield, balance, null);

        internal static DissolveResult Refused(DissolveOutcome outcome, RelicId relic, int balance,
            string failure = null) =>
            new DissolveResult(outcome, relic, 0, balance, failure);

        public DissolveOutcome Outcome { get; }

        public bool Succeeded => Outcome == DissolveOutcome.Dissolved;

        public RelicId Relic { get; }

        /// <summary>Essence gained. 0 on every refusal.</summary>
        public int Yield { get; }

        /// <summary>The balance after the exchange, or the untouched balance on a refusal.</summary>
        public int Balance { get; }

        /// <summary>The store's reason, when the outcome is NotSaved. Null otherwise.</summary>
        public string Failure { get; }
    }

    /// <summary>
    /// Turns a spare copy into essence. Every ambiguous branch refuses to consume: a grant that fails to
    /// persist is a gift we can repeat, and a spend that fails to persist is a theft we cannot undo.
    /// </summary>
    public sealed class EssenceExchange
    {
        private readonly RelicCatalog _relics;
        private readonly SetCatalog _sets;
        private readonly Inventory _inventory;
        private readonly EssenceWallet _wallet;
        private readonly StatePersistence _persistence;

        public EssenceExchange(RelicCatalog relics, SetCatalog sets, Inventory inventory, EssenceWallet wallet,
            StatePersistence persistence)
        {
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _sets = sets;
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        /// <summary>
        /// What dissolving one spare copy of this relic would pay, and why it cannot when it cannot. Floor
        /// rather than round, so the number on the button is never larger than what the player receives.
        /// </summary>
        public DissolvePreview Preview(RelicId relic)
        {
            if (!relic.IsValid || !_relics.TryGet(relic, out Relic found))
            {
                return DissolvePreview.Refused(DissolveOutcome.NoSuchRelic);
            }

            if (_inventory.CountOf(relic) < 2)
            {
                return DissolvePreview.Refused(DissolveOutcome.NoSpareCopy);
            }

            int yield = YieldOf(found);

            if (yield <= 0)
            {
                return DissolvePreview.Refused(DissolveOutcome.NoYield);
            }

            return DissolvePreview.Allowed(yield);
        }

        public DissolveResult TryDissolve(RelicId relic)
        {
            // Re-run at the moment of the press: the preview the player saw was computed for the state the
            // screen was drawn in, and an acquisition may have landed since.
            DissolvePreview preview = Preview(relic);

            if (!preview.CanDissolve)
            {
                return DissolveResult.Refused(preview.Refusal, relic, _wallet.Balance);
            }

            if (!_persistence.TryApply(StateChange.Dissolve(relic, preview.Yield), out string failure))
            {
                return DissolveResult.Refused(DissolveOutcome.NotSaved, relic, _wallet.Balance, failure);
            }

            return DissolveResult.Dissolved(relic, preview.Yield, _wallet.Balance);
        }

        private int YieldOf(Relic relic)
        {
            RelicModifiers modifiers = ActiveModifiers.For(_relics, _sets, _inventory);
            return (int)Math.Floor(relic.EssenceValue * modifiers.EssenceMultiplier);
        }
    }
}
