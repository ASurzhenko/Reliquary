using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Reliquary.Content;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// Draws what the player owns. It renders the state it is handed and decides nothing: how many copies a
    /// relic is worth, and whether an acquisition succeeded, are questions for the rules.
    /// </summary>
    public sealed class InventorySummaryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _headlineText;
        [SerializeField] private TextMeshProUGUI _entriesText;
        [SerializeField] private TextMeshProUGUI _lastFoundText;

        private readonly StringBuilder _builder = new StringBuilder();

        private Inventory _inventory;
        private RelicCatalog _catalog;
        private RelicPresentationLibrary _presentation;
        private string _lastFound;

        /// <summary>
        /// The single place this view subscribes. Unbinding first makes a second Bind harmless, which is
        /// what removes the ordering question between the composition root's Awake and this view's OnEnable.
        /// </summary>
        public void Bind(Inventory inventory, RelicCatalog catalog, RelicPresentationLibrary presentation)
        {
            Unbind();

            _inventory = inventory;
            _catalog = catalog;
            _presentation = presentation;
            Subscribe();

            // Restoring a save raises no change, so nothing else would draw the loaded state.
            Render();
        }

        private void OnEnable()
        {
            if (_inventory == null)
            {
                return;
            }

            Subscribe();
            Render();
        }

        /// <summary>
        /// Unsubscribes before subscribing, so it does not matter whether the composition root's Awake
        /// binds this view before or after its own OnEnable runs: one change stays one redraw.
        /// </summary>
        private void Subscribe()
        {
            _inventory.Changed -= OnInventoryChanged;
            _inventory.Changed += OnInventoryChanged;
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.Changed -= OnInventoryChanged;
        }

        private void OnInventoryChanged(InventoryChange change)
        {
            _lastFound = $"{NameOf(change.Id)} — copy {change.Count}";
            Render();
        }

        private void Render()
        {
            if (_inventory == null || _catalog == null)
            {
                return;
            }

            _headlineText.text = $"{_inventory.DistinctCount} of {_catalog.Count} found";
            _entriesText.text = Describe(_inventory.Entries());
            _lastFoundText.text = _lastFound ?? "Nothing unearthed yet.";
        }

        private string Describe(IReadOnlyList<InventoryEntry> entries)
        {
            if (entries.Count == 0)
            {
                return "The reliquary is empty.";
            }

            _builder.Length = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    _builder.AppendLine();
                }

                _builder.Append(NameOf(entries[i].Id));

                if (entries[i].Count > 1)
                {
                    _builder.Append("  x").Append(entries[i].Count);
                }
            }

            return _builder.ToString();
        }

        private string NameOf(RelicId id)
        {
            // A carried relic has no presentation in this build, so the id is the honest fallback.
            return _presentation != null && _presentation.TryGet(id, out RelicPresentation presentation)
                ? presentation.DisplayName
                : id.ToString();
        }
    }
}
