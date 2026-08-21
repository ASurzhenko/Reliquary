using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// What is owned, and what of it can be spared. The list is ordered by spare copies because that is the
    /// question this screen exists to answer — and the ordering is a rule, so the domain answers it.
    /// </summary>
    public sealed class VaultScreenView : ScreenView
    {
        [SerializeField] private TextMeshProUGUI _subHeaderLabel;
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private VaultRowView _rowTemplate;
        [SerializeField] private GameObject _listRoot;
        [SerializeField] private GameObject _emptyRoot;
        [SerializeField] private TextMeshProUGUI _emptyHeadline;
        [SerializeField] private TextMeshProUGUI _emptyBody;
        [SerializeField] private Button _emptyExcavateButton;

        private readonly Dictionary<RelicId, VaultRowView> _rowsById = new Dictionary<RelicId, VaultRowView>();
        private readonly HashSet<RelicId> _listed = new HashSet<RelicId>();
        private readonly HashSet<RelicId> _pendingRows = new HashSet<RelicId>();

        private UiContext _context;
        private Action<RelicId> _dissolveRequested;
        private Action _excavateRequested;
        private bool _pendingYields;

        public void Bind(UiContext context, Action<RelicId> rowClicked, Action<RelicId> dissolveRequested,
            Action excavateRequested)
        {
            _context = context;
            _dissolveRequested = dissolveRequested;
            _excavateRequested = excavateRequested;
            _rowTemplate.gameObject.SetActive(false);

            IReadOnlyList<Relic> all = context.Catalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                RelicId id = all[i].Id;
                VaultRowView row = Instantiate(_rowTemplate, _listContent);

                row.name = $"Row {id}";
                row.Bind(id, rowClicked, OnDissolveRequested, OnRowArmed);
                _rowsById.Add(id, row);
            }

            SetText(_emptyHeadline, "Your vault is empty.");
            SetText(_emptyBody, "Everything you excavate is kept here — and every spare copy becomes essence.");

            Refresh();
        }

        public override void Refresh()
        {
            if (_context == null)
            {
                return;
            }

            Inventory inventory = _context.Inventory;
            IReadOnlyList<InventoryEntry> ordered = VaultQuery.Order(_context.Catalog, inventory);

            _listed.Clear();

            for (int i = 0; i < ordered.Count; i++)
            {
                InventoryEntry entry = ordered[i];
                VaultRowView row = _rowsById[entry.Id];

                _listed.Add(entry.Id);
                row.gameObject.SetActive(true);
                row.Refresh(ViewModels.VaultRow(entry.Id, _context.Presentation, entry.Count,
                    _context.Exchange.Preview(entry.Id)));

                // Order is a sibling index. Re-binding a row to a different relic would make every id-keyed
                // lookup — the flash targets among them — point at the wrong relic after a single reorder.
                row.transform.SetSiblingIndex(i);
            }

            foreach (KeyValuePair<RelicId, VaultRowView> pair in _rowsById)
            {
                if (!_listed.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }

            // An empty list is the domain saying there is nothing owned, not a count this screen worked out.
            bool empty = ordered.Count == 0;

            _listRoot.SetActive(!empty);
            _emptyRoot.SetActive(empty);

            SetText(_subHeaderLabel, Describe(ordered.Count, inventory));
        }

        public override void OnShown()
        {
            foreach (RelicId relic in _pendingRows)
            {
                if (_rowsById.TryGetValue(relic, out VaultRowView row))
                {
                    row.FlashCopies();
                }
            }

            _pendingRows.Clear();

            if (!_pendingYields)
            {
                return;
            }

            _pendingYields = false;
            FlashYields();
        }

        /// <summary>Marks the row whose copies just moved, now or the next time this screen is shown.</summary>
        public void HighlightRow(RelicId id)
        {
            if (!_rowsById.TryGetValue(id, out VaultRowView row) || !row.FlashCopies())
            {
                _pendingRows.Add(id);
            }
        }

        /// <summary>
        /// A perk started paying: every dissolve label on this screen is now a different number. This is the
        /// loop's closing arrow, and it lands on a screen the player is usually not looking at.
        /// </summary>
        public void HighlightYields()
        {
            if (!isActiveAndEnabled)
            {
                _pendingYields = true;
                return;
            }

            FlashYields();
        }

        private void Awake()
        {
            _emptyExcavateButton.onClick.AddListener(OnEmptyExcavateClicked);
        }

        private void FlashYields()
        {
            foreach (KeyValuePair<RelicId, VaultRowView> pair in _rowsById)
            {
                if (pair.Value.gameObject.activeSelf)
                {
                    pair.Value.FlashYield();
                }
            }
        }

        private void OnEmptyExcavateClicked()
        {
            _excavateRequested?.Invoke();
        }

        private void OnDissolveRequested(RelicId id)
        {
            _dissolveRequested?.Invoke(id);
        }

        /// <summary>
        /// One row at a time may be armed. A confirm left standing on a row the player walked away from is a
        /// destructive tap waiting for an accident.
        /// </summary>
        private void OnRowArmed(RelicId armed)
        {
            foreach (KeyValuePair<RelicId, VaultRowView> pair in _rowsById)
            {
                if (pair.Key != armed)
                {
                    pair.Value.Disarm();
                }
            }
        }

        private string Describe(int listed, Inventory inventory)
        {
            string relics = $"{listed} {(listed == 1 ? "RELIC" : "RELICS")}";

            if (!VaultQuery.HasAnySpares(inventory))
            {
                return $"{relics}  ·  no spare copies yet — duplicates become essence";
            }

            int spares = VaultQuery.SpareCopies(inventory);

            return $"{relics}  ·  {spares} SPARE {(spares == 1 ? "COPY" : "COPIES")}";
        }
    }
}
