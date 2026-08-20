using System;
using System.Collections.Generic;
using UnityEngine;
using Reliquary.Content;
using Reliquary.Domain;
using Reliquary.Infrastructure;
using Reliquary.Presentation;

namespace Reliquary.App
{
    /// <summary>
    /// The composition root: the one place a concrete service is constructed and handed to whatever needs
    /// it. No singleton, no service locator, no lookup — every other layer can be read without asking where
    /// its dependencies came from.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int _excavationMilliseconds = 900;
        [SerializeField] private UiShell _shell;

#if UNITY_EDITOR
        [Header("Diagnostics (Editor only) — each one drives a failure branch by hand; see the README")]
        [SerializeField] private bool _simulateCommittedGrant;   // the service ignores cancellation
        [SerializeField] private bool _simulateSaveFailure;      // every write is refused
#endif

        private StatePersistence _persistence;
        private AcquisitionCoordinator _coordinator;
        private SetCompletionWatcher _completion;

        private RelicCatalog _relics;
        private SetCatalog _sets;
        private SetPresentationLibrary _setPresentation;
        private Inventory _inventory;
        private EssenceWallet _wallet;
        private EssenceExchange _exchange;
        private Trader _trader;

        private void Awake()
        {
#if UNITY_EDITOR
            // Read at write time rather than captured, so flipping either box mid-session reaches the object
            // that consults it. Both are null in a player build, which leaves no fault-injection surface.
            Func<bool> refuseWrites = () => _simulateSaveFailure;
            Func<bool> completeDespiteCancellation = () => _simulateCommittedGrant;
#else
            Func<bool> refuseWrites = null;
            Func<bool> completeDespiteCancellation = null;
#endif

            RelicContentResult content = new RelicContentLoader().Load();
            LogIssues(content.Issues);

            SetContentResult setContent = new SetContentLoader().Load(content.Catalog);
            LogIssues(setContent.Issues);

            EconomySettings economy = new EconomyLoader().Load(out IReadOnlyList<RelicContentIssue> economyIssues);
            LogIssues(economyIssues);

            IInventoryStore store = new PlayerPrefsInventoryStore(refuseWrites);
            StoredState stored = store.Load();

            Inventory inventory;
            IReadOnlyList<InventorySnapshotEntry> carried = Array.Empty<InventorySnapshotEntry>();

            // Hoisted beside `carried` because the saved state is declared inside the case below and is out
            // of scope by the time the wallet is built.
            int essence = 0;

            switch (stored.Status)
            {
                case StoredStateStatus.None:
                    inventory = new Inventory();
                    break;

                case StoredStateStatus.Unreadable:
                    Debug.LogError($"{nameof(GameBootstrap)}.{nameof(Awake)} [Save] {stored.Detail}. " +
                        "The bytes are left where they are and are replaced by the next save this game writes.");
                    inventory = new Inventory();
                    break;

                case StoredStateStatus.Loaded:
                    SavedInventory saved = SavedInventoryReader.Read(stored.Snapshot, content.Catalog);
                    LogIssues(saved.Issues);
                    inventory = saved.Inventory;
                    carried = saved.Carried;
                    essence = saved.Essence;
                    break;

                default:
                    Debug.LogError($"{nameof(GameBootstrap)}.{nameof(Awake)} [Save] unhandled stored state '{stored.Status}'.");
                    inventory = new Inventory();
                    break;
            }

            _relics = content.Catalog;
            _sets = setContent.Sets;
            _setPresentation = setContent.Presentation;
            _inventory = inventory;
            _wallet = new EssenceWallet(essence);

            _persistence = new StatePersistence(store, inventory, _wallet, carried);
            _persistence.SaveFailed += OnSaveFailed;
            _persistence.ApplyRefused += OnApplyRefused;

            // Constructed AFTER the persistence, so that on any inventory change the save handler runs first
            // and no completion is announced that the writer has not attempted to persist.
            _completion = new SetCompletionWatcher(_sets, inventory);
            _completion.Completed += OnSetCompleted;

            _exchange = new EssenceExchange(_relics, _sets, inventory, _wallet, _persistence);
            _trader = new Trader(_relics, _sets, inventory, _wallet, _persistence, economy);

            IAcquisitionService service = new LocalAcquisitionService(content.Catalog, _sets, inventory,
                new System.Random(), _excavationMilliseconds, completeDespiteCancellation);

            _coordinator = new AcquisitionCoordinator(service, inventory, content.Catalog);
            _coordinator.Completed += OnAcquisitionCompleted;

            _shell.Bind(new UiContext(content.Catalog, content.Presentation, inventory, _coordinator, _persistence));

            WarnIfModifiersWereCapped(ActiveModifiers.For(_relics, _sets, inventory));

#if UNITY_EDITOR
            Debug.Log($"{nameof(GameBootstrap)}.{nameof(Awake)} [Thread] managed thread {System.Threading.Thread.CurrentThread.ManagedThreadId}.");
#endif
        }

        // There is deliberately no OnApplicationPause or OnApplicationFocus hook: nothing about a local draw
        // is invalidated by the window losing focus, and cancelling there would throw away a legitimate
        // acquisition every time a tester clicks the Console. A remote implementation would change that.
        private void OnDestroy()
        {
            // Every object is null-guarded: Awake has loader branches that can fail, and a second throw on
            // top of the first hides the first.
            if (_coordinator != null)
            {
                _coordinator.Completed -= OnAcquisitionCompleted;
                _coordinator.Dispose();
            }

            if (_completion != null)
            {
                _completion.Completed -= OnSetCompleted;
                _completion.Dispose();
            }

            if (_persistence != null)
            {
                _persistence.SaveFailed -= OnSaveFailed;
                _persistence.ApplyRefused -= OnApplyRefused;
                _persistence.Dispose();
            }
        }

        private void OnAcquisitionCompleted(AcquisitionCompletion completion)
        {
            string thread = string.Empty;
#if UNITY_EDITOR
            thread = $" (managed thread {System.Threading.Thread.CurrentThread.ManagedThreadId})";
#endif

            switch (completion.Reason)
            {
                case AcquisitionCompletionReason.Granted:
                    Debug.Log($"{nameof(GameBootstrap)}.{nameof(OnAcquisitionCompleted)} [Acquisition] Granted " +
                        $"'{completion.RelicId}', first copy: {completion.WasFirstCopy}{thread}");
                    break;

                case AcquisitionCompletionReason.Rejected:
                    Debug.Log($"{nameof(GameBootstrap)}.{nameof(OnAcquisitionCompleted)} [Acquisition] Rejected — {completion.Rejection}{thread}");
                    break;

                case AcquisitionCompletionReason.Cancelled:
                    Debug.Log($"{nameof(GameBootstrap)}.{nameof(OnAcquisitionCompleted)} [Acquisition] Cancelled{thread}");
                    break;

                case AcquisitionCompletionReason.Superseded:
                    Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(OnAcquisitionCompleted)} [Acquisition] Superseded — " +
                        $"{completion.Detail}; '{completion.RelicId}' was not written{thread}");
                    break;

                case AcquisitionCompletionReason.Failed:
                    Debug.LogError($"{nameof(GameBootstrap)}.{nameof(OnAcquisitionCompleted)} [Acquisition] Failed — {completion.Detail}{thread}");
                    break;

                default:
                    Debug.LogError($"{nameof(GameBootstrap)}.{nameof(OnAcquisitionCompleted)} [Acquisition] unhandled reason '{completion.Reason}'.");
                    break;
            }
        }

        private void OnSetCompleted(SetCompletion completion)
        {
            string name = _setPresentation != null
                && _setPresentation.TryGet(completion.Id, out SetPresentation presentation)
                && !string.IsNullOrWhiteSpace(presentation.DisplayName)
                    ? presentation.DisplayName
                    : completion.Id.ToString();

            Debug.Log($"{nameof(GameBootstrap)}.{nameof(OnSetCompleted)} [Set] SET COMPLETE — {name}. " +
                "Its perk is active from the next draw onwards.");
        }

        private void OnSaveFailed(string failure)
        {
            Debug.LogError($"{nameof(GameBootstrap)}.{nameof(OnSaveFailed)} [Save] {failure}. " +
                "The relic is owned in this session; the next successful save writes the whole state again.");
        }

        private void OnApplyRefused(string failure)
        {
            Debug.LogError($"{nameof(GameBootstrap)}.{nameof(OnApplyRefused)} [Exchange] {failure}. " +
                "Nothing was changed: the copy and the balance are exactly as they were.");
        }

        private static void WarnIfModifiersWereCapped(RelicModifiers modifiers)
        {
            if (modifiers.EssenceMultiplierWasCapped)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(WarnIfModifiersWereCapped)} [Modifiers] the " +
                    $"essence multiplier hit its rail of {RelicModifiers.MaxEssenceMultiplier}. Authored content " +
                    "adds up further than the rules allow, so part of it is doing nothing.");
            }

            if (modifiers.UnownedPullBonusWasCapped)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(WarnIfModifiersWereCapped)} [Modifiers] the " +
                    $"unowned pull bonus hit its rail of {RelicModifiers.MaxUnownedPullBonus}. Authored content " +
                    "adds up further than the rules allow, so part of it is doing nothing.");
            }
        }

        private static void LogIssues(IReadOnlyList<RelicContentIssue> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                RelicContentIssue issue = issues[i];
                string subject = issue.Subject.IsValid ? issue.Subject.ToString() : "content";

                if (issue.Severity == RelicContentSeverity.Error)
                {
                    Debug.LogError($"{nameof(GameBootstrap)}.{nameof(LogIssues)} [{subject}] {issue.Message}");
                    continue;
                }

                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(LogIssues)} [{subject}] {issue.Message}");
            }
        }

#if UNITY_EDITOR
        // Editor-only diagnostics. Three of the eleven exchange terminals have no other route, and the
        // essence grant is the only way to reach a stateless save by hand. A player build carries none of
        // them, and each one takes the same path the feature takes rather than a shortcut around it.

        [ContextMenu("Explain economy")]
        private void ExplainEconomy()
        {
            if (_trader == null)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Diagnostics] the session is not built yet.");
                return;
            }

            RelicModifiers modifiers = ActiveModifiers.For(_relics, _sets, _inventory);

            Debug.Log($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Modifiers] essence ×{modifiers.EssenceMultiplier} " +
                $"(capped: {modifiers.EssenceMultiplierWasCapped}), unowned pull +{modifiers.UnownedPullBonus} " +
                $"(capped: {modifiers.UnownedPullBonusWasCapped}), balance {_wallet.Balance}.");

            DrawTable table = DrawTable.Build(_relics, _inventory, modifiers);
            Debug.Log($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Draw] total weight {table.TotalWeight}.");

            foreach (Relic relic in _relics.All)
            {
                int weight = relic.DiscoveryWeight + (_inventory.Owns(relic.Id) ? 0 : modifiers.UnownedPullBonus);
                DissolvePreview preview = _exchange.Preview(relic.Id);
                string dissolve = preview.CanDissolve ? $"+{preview.Yield}" : preview.Refusal.ToString();

                Debug.Log($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Draw] '{relic.Id}' " +
                    $"owned {_inventory.CountOf(relic.Id)}, weight {weight}, dissolve {dissolve}.");
            }

            foreach (RelicSet set in _sets.All)
            {
                SetProgress progress = SetProgress.For(set, _inventory);
                Debug.Log($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Set] '{set.Id}' " +
                    $"{progress.Owned} of {progress.Total}, complete: {progress.IsComplete}.");
            }

            TraderOffer offer = _trader.CurrentOffer;

            if (!offer.HasOffer)
            {
                Debug.Log($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Trader] no offer — {offer.Absence}.");
                return;
            }

            Debug.Log($"{nameof(GameBootstrap)}.{nameof(ExplainEconomy)} [Trader] '{offer.Relic}' for {offer.Price} " +
                $"(affordable: {offer.CanAfford}, deficit {offer.Deficit}), focus set " +
                $"{(offer.FocusSet.IsValid ? offer.FocusSet.ToString() : "none")} " +
                $"{offer.FocusProgress.Owned} of {offer.FocusProgress.Total}.");
        }

        [ContextMenu("Grant 100 essence")]
        private void GrantEssence()
        {
            if (_persistence == null)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(GrantEssence)} [Diagnostics] the session is not built yet.");
                return;
            }

            // The same write a dissolve takes: a diagnostic that bypasses the mechanism under test proves
            // nothing about it.
            if (!_persistence.TryApply(StateChange.Grant(100), out string failure))
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(GrantEssence)} [Diagnostics] refused — {failure}.");
                return;
            }

            Debug.Log($"{nameof(GameBootstrap)}.{nameof(GrantEssence)} [Diagnostics] balance is now {_wallet.Balance}.");
        }

        [ContextMenu("Acquire the offered relic")]
        private void AcquireTheOfferedRelic()
        {
            if (_trader == null)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(AcquireTheOfferedRelic)} [Diagnostics] the session is not built yet.");
                return;
            }

            TraderOffer offer = _trader.CurrentOffer;

            if (!offer.HasOffer)
            {
                Debug.Log($"{nameof(GameBootstrap)}.{nameof(AcquireTheOfferedRelic)} [Diagnostics] there is no offer — {offer.Absence}.");
                return;
            }

            // Added the way an acquisition adds it, so the auto-save runs and the watcher hears it.
            _inventory.Add(offer.Relic);
            Debug.Log($"{nameof(GameBootstrap)}.{nameof(AcquireTheOfferedRelic)} [Diagnostics] '{offer.Relic}' was granted.");
        }

        [ContextMenu("Probe an unknown id")]
        private void ProbeAnUnknownId()
        {
            if (_exchange == null || _trader == null)
            {
                Debug.LogWarning($"{nameof(GameBootstrap)}.{nameof(ProbeAnUnknownId)} [Diagnostics] the session is not built yet.");
                return;
            }

            RelicId unknown = new RelicId("relic.not_in_this_build");

            Debug.Log($"{nameof(GameBootstrap)}.{nameof(ProbeAnUnknownId)} [Diagnostics] dissolve → " +
                $"{_exchange.TryDissolve(unknown).Outcome}, buy → {_trader.TryBuy(unknown).Outcome}.");
        }
#endif
    }
}
