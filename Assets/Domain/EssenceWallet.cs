using System;

namespace Reliquary.Domain
{
    public enum EssenceChangeReason
    {
        /// <summary>A spare copy was dissolved.</summary>
        Dissolved,

        /// <summary>Essence was spent at the trader.</summary>
        Spent,

        /// <summary>Essence arrived from a diagnostic, with no relic involved.</summary>
        Granted
    }

    /// <summary>One movement of the balance, raised after the balance has been updated.</summary>
    public readonly struct EssenceChange
    {
        public EssenceChange(int delta, int balance, EssenceChangeReason reason, RelicId subject)
        {
            Delta = delta;
            Balance = balance;
            Reason = reason;
            Subject = subject;
        }

        /// <summary>Signed: a yield is positive, a price is negative.</summary>
        public int Delta { get; }

        /// <summary>The balance after the change.</summary>
        public int Balance { get; }

        public EssenceChangeReason Reason { get; }

        /// <summary>
        /// The relic dissolved or bought; the default id when the change involved no relic. Carried so a
        /// view can write "+18 essence — Drowned Bell dissolved" from one event instead of correlating two.
        /// </summary>
        public RelicId Subject { get; }
    }

    /// <summary>
    /// What a player can spend. The mutators are internal: a balance moves only inside the single write that
    /// carries it and the copy it was exchanged for. A future writer that moves essence outside
    /// StatePersistence.TryApply must call StatePersistence.Save itself — nothing else persists this.
    /// </summary>
    public sealed class EssenceWallet
    {
        private int _balance;

        public EssenceWallet(int startingBalance)
        {
            if (startingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingBalance), startingBalance,
                    "A balance cannot start negative. The save reader clamps before it gets here.");
            }

            _balance = startingBalance;
        }

        /// <summary>Raised once per accepted change, after the balance has been updated.</summary>
        public event Action<EssenceChange> Changed;

        public int Balance => _balance;

        public bool CanAfford(int price) => price >= 0 && price <= _balance;

        /// <summary>
        /// Moves the balance and raises nothing. The guards are the last line, for a caller that skipped the
        /// validation the exchange performs before the write.
        /// </summary>
        internal EssenceChange ApplySilently(int delta, EssenceChangeReason reason, RelicId subject)
        {
            int next = _balance + delta;

            if (next < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delta), delta,
                    $"A delta of {delta} would take the balance of {_balance} below zero.");
            }

            _balance = next;
            return new EssenceChange(delta, next, reason, subject);
        }

        internal void Announce(EssenceChange change)
        {
            Changed?.Invoke(change);
        }
    }
}
