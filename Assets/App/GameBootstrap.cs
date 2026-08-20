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

        private InventoryPersistence _persistence;
        private AcquisitionCoordinator _coordinator;

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

            IInventoryStore store = new PlayerPrefsInventoryStore(refuseWrites);
            StoredState stored = store.Load();

            Inventory inventory;
            IReadOnlyList<InventorySnapshotEntry> carried = Array.Empty<InventorySnapshotEntry>();

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
                    break;

                default:
                    Debug.LogError($"{nameof(GameBootstrap)}.{nameof(Awake)} [Save] unhandled stored state '{stored.Status}'.");
                    inventory = new Inventory();
                    break;
            }

            _persistence = new InventoryPersistence(store, inventory, carried);
            _persistence.SaveFailed += OnSaveFailed;

            IAcquisitionService service = new LocalAcquisitionService(content.Catalog, inventory,
                new System.Random(), _excavationMilliseconds, completeDespiteCancellation);

            _coordinator = new AcquisitionCoordinator(service, inventory, content.Catalog);
            _coordinator.Completed += OnAcquisitionCompleted;

            _shell.Bind(new UiContext(content.Catalog, content.Presentation, inventory, _coordinator, _persistence));

#if UNITY_EDITOR
            Debug.Log($"{nameof(GameBootstrap)}.{nameof(Awake)} [Thread] managed thread {System.Threading.Thread.CurrentThread.ManagedThreadId}.");
#endif
        }

        // There is deliberately no OnApplicationPause or OnApplicationFocus hook: nothing about a local draw
        // is invalidated by the window losing focus, and cancelling there would throw away a legitimate
        // acquisition every time a tester clicks the Console. A remote implementation would change that.
        private void OnDestroy()
        {
            if (_coordinator != null)
            {
                _coordinator.Completed -= OnAcquisitionCompleted;
                _coordinator.Dispose();
            }

            if (_persistence != null)
            {
                _persistence.SaveFailed -= OnSaveFailed;
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

        private void OnSaveFailed(string failure)
        {
            Debug.LogError($"{nameof(GameBootstrap)}.{nameof(OnSaveFailed)} [Save] {failure}. " +
                "The relic is owned in this session; the next successful save writes the whole state again.");
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
    }
}
