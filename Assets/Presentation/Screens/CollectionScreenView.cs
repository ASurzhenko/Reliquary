using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// What exists, and what is missing. Every relic in the catalogue gets a tile at bind time — including
    /// the ones the player has never found, because the thing they are meant to hunt has to be visible.
    /// </summary>
    public sealed class CollectionScreenView : ScreenView
    {
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private RectTransform _gridContent;
        [SerializeField] private RelicTileView _tileTemplate;
        [SerializeField] private FilterChip[] _chips;

        private readonly Dictionary<RelicId, RelicTileView> _tilesById = new Dictionary<RelicId, RelicTileView>();
        private readonly HashSet<RelicId> _shown = new HashSet<RelicId>();

        private UiContext _context;
        private CollectionFilter _filter = CollectionFilter.All;

        public void Bind(UiContext context, Action<RelicId> tileClicked)
        {
            _context = context;
            _tileTemplate.gameObject.SetActive(false);

            IReadOnlyList<Relic> all = context.Catalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                RelicId id = all[i].Id;
                RelicTileView tile = Instantiate(_tileTemplate, _gridContent);

                tile.name = $"Tile {id}";
                tile.Bind(id, tileClicked);
                _tilesById.Add(id, tile);
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

            SetText(_countLabel, $"COLLECTED  {inventory.DistinctCount} / {_context.Catalog.Count}");

            IReadOnlyList<Relic> shown = CollectionQuery.Filter(_context.Catalog, inventory, _filter);
            _shown.Clear();

            for (int i = 0; i < shown.Count; i++)
            {
                _shown.Add(shown[i].Id);
            }

            foreach (KeyValuePair<RelicId, RelicTileView> pair in _tilesById)
            {
                // Filtering is visibility, not rebuilding: an inactive child is left out of the grid's layout,
                // so the tiles reflow with no object churn and every tile keeps its relic.
                pair.Value.gameObject.SetActive(_shown.Contains(pair.Key));
                pair.Value.Refresh(ViewModels.Tile(pair.Key, _context.Presentation, inventory.CountOf(pair.Key)));
            }
        }

        private void Awake()
        {
            for (int i = 0; i < _chips.Length; i++)
            {
                FilterChip chip = _chips[i];
                chip.Button.onClick.AddListener(() => Select(chip.Filter));
            }
        }

        private void Select(CollectionFilter filter)
        {
            _filter = filter;
            RefreshChips();
            Refresh();
        }

        private void Start()
        {
            RefreshChips();
        }

        private void RefreshChips()
        {
            for (int i = 0; i < _chips.Length; i++)
            {
                _chips[i].SetSelected(_chips[i].Filter == _filter);
            }
        }

        /// <summary>One filter chip: the control, and the filter it asks for.</summary>
        [Serializable]
        public sealed class FilterChip
        {
            [SerializeField] private Button _button;
            [SerializeField] private Image _background;
            [SerializeField] private TextMeshProUGUI _label;
            [SerializeField] private CollectionFilter _filter;
            [SerializeField] private Color _selectedBackground = new Color(0.36f, 0.31f, 0.22f);
            [SerializeField] private Color _restingBackground = new Color(0.18f, 0.19f, 0.22f);

            public Button Button => _button;

            public CollectionFilter Filter => _filter;

            public void SetSelected(bool selected)
            {
                _background.color = selected ? _selectedBackground : _restingBackground;
                _label.color = selected ? Color.white : new Color(0.72f, 0.75f, 0.80f);
            }
        }
    }
}
