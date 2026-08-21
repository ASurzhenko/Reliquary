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
    /// the ones the player has never found, because the thing they are meant to hunt has to be visible — and
    /// every tile sits under the set it belongs to, with that set's progress and the perk it will grant.
    /// </summary>
    public sealed class CollectionScreenView : ScreenView
    {
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private RectTransform _sectionsRoot;
        [SerializeField] private SetSectionView _sectionTemplate;
        [SerializeField] private RelicTileView _tileTemplate;
        [SerializeField] private FilterChip[] _chips;

        private readonly Dictionary<RelicId, RelicTileView> _tilesById = new Dictionary<RelicId, RelicTileView>();
        private readonly Dictionary<RelicId, SetSectionView> _sectionOfRelic = new Dictionary<RelicId, SetSectionView>();
        private readonly Dictionary<SetId, SetSectionView> _sectionsById = new Dictionary<SetId, SetSectionView>();
        private readonly List<SetSectionView> _sections = new List<SetSectionView>();
        private readonly HashSet<RelicId> _shown = new HashSet<RelicId>();
        private readonly HashSet<SetId> _pendingSets = new HashSet<SetId>();
        private readonly HashSet<RelicId> _pendingRelics = new HashSet<RelicId>();

        private UiContext _context;
        private SetSectionView _looseSection;
        private CollectionFilter _filter = CollectionFilter.All;

        public void Bind(UiContext context, Action<RelicId> tileClicked)
        {
            _context = context;
            _sectionTemplate.gameObject.SetActive(false);
            _tileTemplate.gameObject.SetActive(false);

            BuildSections(context);
            BuildTiles(context, tileClicked);

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

            RefreshSections();
        }

        /// <summary>
        /// Plays whatever was raised while this screen was somewhere else. A change that happened on another
        /// tab is the one most worth marking, and it is exactly the one a screen cannot show as it happens.
        /// </summary>
        public override void OnShown()
        {
            foreach (SetId set in _pendingSets)
            {
                if (_sectionsById.TryGetValue(set, out SetSectionView section))
                {
                    section.FlashProgress();
                }
            }

            foreach (RelicId relic in _pendingRelics)
            {
                if (_tilesById.TryGetValue(relic, out RelicTileView tile))
                {
                    tile.FlashFound();
                }
            }

            _pendingSets.Clear();
            _pendingRelics.Clear();
        }

        /// <summary>Marks a set whose progress moved, now or the next time this screen is shown.</summary>
        public void HighlightSet(SetId id)
        {
            if (!_sectionsById.TryGetValue(id, out SetSectionView section) || !section.FlashProgress())
            {
                _pendingSets.Add(id);
            }
        }

        /// <summary>Marks the tile of a relic that just arrived or gained a copy.</summary>
        public void HighlightRelic(RelicId id)
        {
            if (!_tilesById.TryGetValue(id, out RelicTileView tile) || !tile.FlashFound())
            {
                _pendingRelics.Add(id);
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

        private void Start()
        {
            RefreshChips();
        }

        private void BuildSections(UiContext context)
        {
            IReadOnlyList<RelicSet> sets = context.Sets == null
                ? Array.Empty<RelicSet>()
                : context.Sets.All;

            for (int i = 0; i < sets.Count; i++)
            {
                SetSectionView section = Instantiate(_sectionTemplate, _sectionsRoot);

                section.name = $"Section {sets[i].Id}";
                section.gameObject.SetActive(true);
                section.Bind(sets[i].Id);

                _sections.Add(section);
                _sectionsById.Add(sets[i].Id, section);
            }

            // Built whether or not anything lands in it, and hidden by the same emptiness rule every other
            // section follows: a relic no set names must still be somewhere, or adding one hides it.
            _looseSection = Instantiate(_sectionTemplate, _sectionsRoot);
            _looseSection.name = "Section (loose)";
            _looseSection.gameObject.SetActive(true);
            _looseSection.Bind(default);
            _sections.Add(_looseSection);
        }

        private void BuildTiles(UiContext context, Action<RelicId> tileClicked)
        {
            IReadOnlyList<Relic> all = context.Catalog.All;

            for (int i = 0; i < all.Count; i++)
            {
                RelicId id = all[i].Id;
                SetSectionView section = SectionFor(context, id);
                RelicTileView tile = Instantiate(_tileTemplate, section.Grid);

                tile.name = $"Tile {id}";
                tile.Bind(id, tileClicked);

                _tilesById.Add(id, tile);
                _sectionOfRelic.Add(id, section);
            }
        }

        /// <summary>
        /// One tile per relic, in the first set that names it. A relic in two sets is drawn once and counted
        /// by both — the counters come from the domain, so they stay right either way.
        /// </summary>
        private SetSectionView SectionFor(UiContext context, RelicId id)
        {
            if (context.Sets == null)
            {
                return _looseSection;
            }

            IReadOnlyList<RelicSet> holders = context.Sets.SetsContaining(id);

            if (holders.Count == 0)
            {
                return _looseSection;
            }

            return _sectionsById.TryGetValue(holders[0].Id, out SetSectionView section) ? section : _looseSection;
        }

        private void RefreshSections()
        {
            int loose = 0;

            for (int i = 0; i < _sections.Count; i++)
            {
                SetSectionView section = _sections[i];

                if (section == _looseSection)
                {
                    continue;
                }

                _context.Sets.TryGet(section.BoundId, out RelicSet set);
                section.Refresh(ViewModels.Section(set, _context.SetPresentation,
                    SetProgress.For(set, _context.Inventory)));
                section.gameObject.SetActive(HasVisibleTile(section));
            }

            foreach (KeyValuePair<RelicId, SetSectionView> pair in _sectionOfRelic)
            {
                if (pair.Value == _looseSection)
                {
                    loose++;
                }
            }

            _looseSection.Refresh(ViewModels.LooseSection(loose));
            _looseSection.gameObject.SetActive(HasVisibleTile(_looseSection));
        }

        /// <summary>
        /// Whether a filter left this section with anything to show. It reads the tiles the section holds —
        /// view state, not a rule: which relics a filter shows was answered by the domain a moment ago.
        /// </summary>
        private bool HasVisibleTile(SetSectionView section)
        {
            foreach (KeyValuePair<RelicId, SetSectionView> pair in _sectionOfRelic)
            {
                if (pair.Value == section && _shown.Contains(pair.Key))
                {
                    return true;
                }
            }

            return false;
        }

        private void Select(CollectionFilter filter)
        {
            _filter = filter;
            RefreshChips();
            Refresh();
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
