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
        [SerializeField] private TabBarView _tabBar;
        [SerializeField] private EventLedgerView _ledger;
        [SerializeField] private ExcavationBarView _excavationBar;
        [SerializeField] private CollectionScreenView _collection;
        [SerializeField] private VaultScreenView _vault;
        [SerializeField] private TraderScreenView _trader;
        [SerializeField] private OverlayRoot _overlays;
        [SerializeField] private RelicDetailView _detail;
        [SerializeField] private RevealCardView _card;

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
            _vault.Bind(context, _detail.Show);
            _excavationBar.Bind(context.Coordinator);

            Subscribe();

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

        private void OnDisable()
        {
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

            _context.Coordinator.Completed -= OnAcquisitionCompleted;
            _context.Coordinator.Completed += OnAcquisitionCompleted;

            _context.Persistence.SaveFailed -= OnSaveFailed;
            _context.Persistence.SaveFailed += OnSaveFailed;

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
            _context.Coordinator.Completed -= OnAcquisitionCompleted;
            _context.Persistence.SaveFailed -= OnSaveFailed;
            _excavationBar.Reported -= OnBarReported;
        }

        private void OnInventoryChanged(InventoryChange change)
        {
            RefreshScreens();
        }

        private void OnAcquisitionCompleted(AcquisitionCompletion completion)
        {
            _excavationBar.Report(completion);

            if (completion.Reason != AcquisitionCompletionReason.Granted)
            {
                return;
            }

            int copies = _context.Inventory.CountOf(completion.RelicId);
            RelicTileModel model = ViewModels.Tile(completion.RelicId, _context.Presentation, copies);

            if (completion.WasFirstCopy)
            {
                _ledger.Show($"Found — {model.Name} (new)", LedgerKind.Gain);
                _card.Enqueue(new RevealRequest(RevealKind.New, model.Name, model.Icon, copies));
                return;
            }

            _ledger.Show($"Found — {model.Name} (copy {copies})", LedgerKind.Gain);
            _card.Enqueue(new RevealRequest(RevealKind.Duplicate, model.Name, model.Icon, copies));
        }

        private void OnBarReported(string message, LedgerKind kind)
        {
            _ledger.Show(message, kind);
        }

        private void OnSaveFailed(string failure)
        {
            // The relic is owned in this session either way, so play is not blocked — but a write that did not
            // land is the player's business, not only the console's.
            _ledger.Show("Your progress could not be saved.", LedgerKind.Problem);
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
