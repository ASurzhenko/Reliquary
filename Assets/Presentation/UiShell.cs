using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// The only subscriber in this layer, and the router for every event the player is told about. It sits on
    /// a rect that is never deactivated, so it hears a change raised while the player is on another tab —
    /// which a screen that subscribed for itself could not, because only one screen is active at a time.
    /// The shell routes, the screens draw, the domain decides.
    /// </summary>
    public sealed class UiShell : View
    {
        [SerializeField] private HeaderView _header;
        [SerializeField] private TabBarView _tabBar;
        [SerializeField] private EventLedgerView _ledger;
        [SerializeField] private ExcavationBarView _excavationBar;
        [SerializeField] private CollectionScreenView _collection;
        [SerializeField] private VaultScreenView _vault;
        [SerializeField] private TraderScreenView _trader;
        [SerializeField] private OverlayRoot _overlays;
        [SerializeField] private RelicDetailView _detail;
        [SerializeField] private RevealCardView _card;

        private readonly List<SetCompletion> _completions = new List<SetCompletion>();

        private UiContext _context;

        /// <summary>
        /// The single place this layer subscribes. Unbinding first makes a second Bind harmless, which is what
        /// removes the ordering question between the composition root's Awake and this object's OnEnable.
        /// </summary>
        public void Bind(UiContext context)
        {
            Unbind();

            _context = context;

            _detail.Bind(context, _overlays);
            _card.Bind(_overlays);
            _collection.Bind(context, _detail.Show);
            _vault.Bind(context, _detail.Show, OnDissolveRequested, OnExcavateShortcut);
            _trader.Bind(context, OnBuyRequested, OnVaultShortcut);
            _excavationBar.Bind(context.Coordinator);

            Subscribe();

            _header.ShowBalance(context.Wallet.Balance);
            _tabBar.Bind();
            RefreshScreens();
        }

        private void OnEnable()
        {
            if (_context == null)
            {
                return;
            }

            Subscribe();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Subscribe()
        {
            _context.Inventory.Changed -= OnInventoryChanged;
            _context.Inventory.Changed += OnInventoryChanged;

            _context.Wallet.Changed -= OnEssenceChanged;
            _context.Wallet.Changed += OnEssenceChanged;

            _context.Completion.Completed -= OnSetCompleted;
            _context.Completion.Completed += OnSetCompleted;

            _context.Coordinator.Completed -= OnAcquisitionCompleted;
            _context.Coordinator.Completed += OnAcquisitionCompleted;

            _context.Persistence.SaveFailed -= OnSaveFailed;
            _context.Persistence.SaveFailed += OnSaveFailed;

            _context.Persistence.ApplyRefused -= OnApplyRefused;
            _context.Persistence.ApplyRefused += OnApplyRefused;

            _excavationBar.Reported -= OnBarReported;
            _excavationBar.Reported += OnBarReported;
        }

        private void Unbind()
        {
            if (_context == null)
            {
                return;
            }

            _context.Inventory.Changed -= OnInventoryChanged;
            _context.Wallet.Changed -= OnEssenceChanged;
            _context.Completion.Completed -= OnSetCompleted;
            _context.Coordinator.Completed -= OnAcquisitionCompleted;
            _context.Persistence.SaveFailed -= OnSaveFailed;
            _context.Persistence.ApplyRefused -= OnApplyRefused;
            _excavationBar.Reported -= OnBarReported;
        }

        private void OnInventoryChanged(InventoryChange change)
        {
            RefreshScreens();
            _collection.HighlightRelic(change.Id);
        }

        /// <summary>
        /// E-C and E-D. One event carries the sign, the new balance and what it was for, so the line is
        /// written from a single reading rather than by correlating two.
        /// </summary>
        private void OnEssenceChanged(EssenceChange change)
        {
            _header.ShowBalance(change.Balance);
            _header.FlashBalance(change.Delta > 0);

            RefreshScreens();

            string relic = ViewModels.Tile(change.Subject, _context.Presentation, 0).Name;

            switch (change.Reason)
            {
                case EssenceChangeReason.Dissolved:
                    _ledger.Show($"+{change.Delta} essence — {relic} dissolved", LedgerKind.Gain);
                    _vault.HighlightRow(change.Subject);
                    return;

                case EssenceChangeReason.Spent:
                    _ledger.Show($"−{-change.Delta} essence — {relic} acquired", LedgerKind.Spend);
                    _trader.FlashOffer();
                    return;

                case EssenceChangeReason.Granted:
                    _ledger.Show($"+{change.Delta} essence", LedgerKind.Gain);
                    return;

                default:
                    Debug.LogError($"{nameof(UiShell)}.{nameof(OnEssenceChanged)} unhandled reason '{change.Reason}'.");
                    return;
            }
        }

        /// <summary>
        /// A set completes INSIDE the change that completed it — before the copy that did it has been
        /// announced — so announcing it here would put the milestone ahead of the find that earned it. It is
        /// held and said afterwards instead: a purchase reads spend, then find, then milestone, then perk.
        /// </summary>
        private void OnSetCompleted(SetCompletion completion)
        {
            _completions.Add(completion);
        }

        /// <summary>
        /// The safety net for a completion nothing else follows — a diagnostic that adds a relic directly,
        /// say. Held milestones are never dropped, only ordered.
        /// </summary>
        private void LateUpdate()
        {
            if (_completions.Count > 0)
            {
                AnnounceCompletions();
            }
        }

        /// <summary>
        /// E-E and E-F. The milestone and the sentence that says the numbers moved are two lines rather than
        /// one, because they are two facts: the set is finished, and everything it touches now pays
        /// differently. The second one lands on surfaces the player is not looking at, which is what the
        /// screens' pending marks are for.
        /// </summary>
        private void AnnounceCompletions()
        {
            for (int i = 0; i < _completions.Count; i++)
            {
                Announce(_completions[i]);
            }

            _completions.Clear();
        }

        private void Announce(SetCompletion completion)
        {
            string name = ViewModels.SetName(completion.Id, _context.SetPresentation);
            string perk = ViewModels.PerkSummary(completion.Id, _context.SetPresentation);

            RefreshScreens();

            _ledger.Show(string.IsNullOrEmpty(perk) ? $"{name} complete" : $"{name} complete — {perk}",
                LedgerKind.Milestone);
            _card.Enqueue(new RevealRequest(RevealKind.SetComplete, name, null, 0, perk));
            _collection.HighlightSet(completion.Id);

            ModifierDimension dimensions = ViewModels.DimensionsOf(completion.Id, _context.SetPresentation);

            if (dimensions == ModifierDimension.None)
            {
                return;
            }

            _ledger.Show($"{name} perk active — {WhatMoved(dimensions)}", LedgerKind.Milestone);

            if ((dimensions & ModifierDimension.EssenceYield) != 0)
            {
                _vault.HighlightYields();
            }

            if ((dimensions & ModifierDimension.UnownedPull) != 0)
            {
                _excavationBar.FlashStatus();
            }
        }

        private void OnAcquisitionCompleted(AcquisitionCompletion completion)
        {
            _excavationBar.Report(completion);

            if (completion.Reason != AcquisitionCompletionReason.Granted)
            {
                return;
            }

            AnnounceFound(completion.RelicId, completion.WasFirstCopy);
        }

        /// <summary>
        /// A purchase is E-D then E-A, and nothing new is written for it: the essence leaving the pill is the
        /// wallet's own event, and the relic arriving gets the same card the dig gives, because the same
        /// thing happened.
        /// </summary>
        private void OnBuyRequested(RelicId relic)
        {
            PurchaseResult result = _context.Trader.TryBuy(relic);

            if (result.Succeeded)
            {
                AnnounceFound(result.Relic, true);
                return;
            }

            // NotSaved is deliberately silent here: the persistence raised ApplyRefused for it a moment ago,
            // and one refusal is one line.
            if (result.Outcome == PurchaseOutcome.NotSaved)
            {
                return;
            }

            _ledger.Show(Describe(result), LedgerKind.Problem);
        }

        private void OnDissolveRequested(RelicId relic)
        {
            DissolveResult result = _context.Exchange.TryDissolve(relic);

            if (result.Succeeded || result.Outcome == DissolveOutcome.NotSaved)
            {
                // A success already spoke through the wallet's own event, and a refused write spoke through
                // ApplyRefused. Neither needs a second sentence from here.
                return;
            }

            _ledger.Show(Describe(result), LedgerKind.Problem);
        }

        private void OnExcavateShortcut()
        {
            _excavationBar.RequestExcavation();
            _excavationBar.FlashButton();
        }

        private void OnVaultShortcut()
        {
            _tabBar.Show(_vault);
        }

        private void OnBarReported(string message, LedgerKind kind)
        {
            _ledger.Show(message, kind);
        }

        private void OnSaveFailed(string failure)
        {
            // The state is in memory either way, so play is not blocked — but a write that did not land is
            // the player's business, not only the console's.
            _ledger.Show("Your progress could not be saved.", LedgerKind.Problem);
        }

        private void OnApplyRefused(string failure)
        {
            // The opposite meaning to the line above, and that is exactly why it is a different sentence:
            // nothing happened at all, so there is nothing to worry about losing.
            _ledger.Show("Nothing was exchanged — your copies and essence are unchanged.", LedgerKind.Problem);
        }

        private void AnnounceRelic(RelicId relic, bool wasFirstCopy)
        {
            int copies = _context.Inventory.CountOf(relic);
            RelicTileModel model = ViewModels.Tile(relic, _context.Presentation, copies);

            if (wasFirstCopy)
            {
                _ledger.Show($"Found — {model.Name} (new)", LedgerKind.Gain);
                _card.Enqueue(new RevealRequest(RevealKind.New, model.Name, model.Icon, copies));
                return;
            }

            _ledger.Show($"Found — {model.Name} (copy {copies})", LedgerKind.Gain);
            _card.Enqueue(new RevealRequest(RevealKind.Duplicate, model.Name, model.Icon, copies));
        }

        /// <summary>Whatever this copy completed is said now, behind the find that completed it.</summary>
        private void AnnounceFound(RelicId relic, bool wasFirstCopy)
        {
            AnnounceRelic(relic, wasFirstCopy);
            AnnounceCompletions();
        }

        private static string WhatMoved(ModifierDimension dimensions)
        {
            bool essence = (dimensions & ModifierDimension.EssenceYield) != 0;
            bool pull = (dimensions & ModifierDimension.UnownedPull) != 0;

            if (essence && pull)
            {
                return "duplicates dissolve for more, and unfound relics surface more often";
            }

            return essence
                ? "duplicates now dissolve for more"
                : "unfound relics now surface more often";
        }

        private string Describe(PurchaseResult result)
        {
            switch (result.Outcome)
            {
                case PurchaseOutcome.NotEnoughEssence:
                    return $"Not enough essence — {result.Deficit} more needed.";

                case PurchaseOutcome.OfferChanged:
                    return "The Trader is offering something else now.";

                case PurchaseOutcome.AlreadyOwned:
                    return "You already own that relic.";

                case PurchaseOutcome.NoSuchRelic:
                    return "The Trader cannot source that relic.";

                default:
                    Debug.LogError($"{nameof(UiShell)}.{nameof(Describe)} unhandled purchase outcome '{result.Outcome}'.");
                    return "The purchase did not go through.";
            }
        }

        private string Describe(DissolveResult result)
        {
            switch (result.Outcome)
            {
                case DissolveOutcome.NoSpareCopy:
                    return "The last copy of a relic is never dissolved.";

                case DissolveOutcome.NoYield:
                    return "That copy would pay nothing right now.";

                case DissolveOutcome.NoSuchRelic:
                    return "This build knows nothing about that relic.";

                default:
                    Debug.LogError($"{nameof(UiShell)}.{nameof(Describe)} unhandled dissolve outcome '{result.Outcome}'.");
                    return "The exchange did not go through.";
            }
        }

        private void RefreshScreens()
        {
            _collection.Refresh();
            _vault.Refresh();
            _trader.Refresh();
            _detail.Refresh();
        }
    }
}
