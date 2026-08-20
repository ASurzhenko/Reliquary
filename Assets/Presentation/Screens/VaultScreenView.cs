using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

        private readonly Dictionary<RelicId, VaultRowView> _rowsById = new Dictionary<RelicId, VaultRowView>();
        private readonly HashSet<RelicId> _listed = new HashSet<RelicId>();

        private UiContext _context;

        public void Bind(UiContext context, Action<RelicId> rowClicked)
        {
            _context = context;
            _rowTemplate.gameObject.SetActive(false);

            IReadOnlyList<Relic> all = context.Catalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                RelicId id = all[i].Id;
                VaultRowView row = Instantiate(_rowTemplate, _listContent);

                row.name = $"Row {id}";
                row.Bind(id, rowClicked);
                _rowsById.Add(id, row);
            }

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
                row.Refresh(ViewModels.VaultRow(entry.Id, _context.Presentation, entry.Count));

                // Order is a sibling index. Re-binding a row to a different relic would make every id-keyed
                // lookup — the pulse targets among them — point at the wrong relic after a single reorder.
                row.transform.SetSiblingIndex(i);
            }

            foreach (KeyValuePair<RelicId, VaultRowView> pair in _rowsById)
            {
                if (!_listed.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }

            SetText(_subHeaderLabel, Describe(ordered.Count, inventory));
        }

        private string Describe(int listed, Inventory inventory)
        {
            string relics = $"{listed} RELICS";

            return VaultQuery.HasAnySpares(inventory)
                ? $"{relics}  ·  {VaultQuery.SpareCopies(inventory)} SPARE COPIES"
                : $"{relics}  ·  no spare copies yet — duplicates become essence";
        }
    }
}
