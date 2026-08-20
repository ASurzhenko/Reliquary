using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// One relic, in full: what it is called, what it looks like, what it says, what the rules read off it,
    /// and whether it is owned. It holds the open relic as an id and re-resolves it on every change, so a
    /// copy found while the sheet is open is shown rather than remembered wrongly.
    /// </summary>
    public sealed class RelicDetailView : View
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _ownershipLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private TextMeshProUGUI _attributesLabel;
        [SerializeField] private TextMeshProUGUI _effectsLabel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _dimmerButton;

        private readonly StringBuilder _builder = new StringBuilder();

        private UiContext _context;
        private OverlayRoot _overlays;
        private RelicId _open;
        private bool _isOpen;

        public void Bind(UiContext context, OverlayRoot overlays)
        {
            _context = context;
            _overlays = overlays;
        }

        public void Show(RelicId id)
        {
            _open = id;
            _isOpen = true;

            _overlays.Show(gameObject);
            Render();
        }

        public void Refresh()
        {
            if (!_isOpen)
            {
                return;
            }

            Render();
        }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _dimmerButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            // Another overlay may take the root away — a reveal card raised by an acquisition, say. The sheet
            // is then not open any more, whoever closed it.
            _isOpen = false;
        }

        private void Close()
        {
            _isOpen = false;
            _overlays.Hide();
        }

        private void Render()
        {
            if (_context == null || !_context.Catalog.TryGet(_open, out Relic relic))
            {
                // Nothing in this build describes the relic that was asked for. Saying so is the honest
                // branch; leaving the previous relic's sheet up would be a lie about what was tapped.
                Debug.LogWarning($"{nameof(RelicDetailView)}.{nameof(Render)} '{_open}' is not in the catalogue.");
                Close();
                return;
            }

            RelicDetailModel model = ViewModels.Detail(relic, _context.Presentation, _context.Inventory.CountOf(_open));

            _icon.sprite = model.Icon;
            _icon.enabled = model.Icon != null;

            SetText(_nameLabel, model.Name);
            SetText(_ownershipLabel, model.Ownership);
            SetText(_descriptionLabel, model.Description);
            SetText(_attributesLabel, $"Dissolves into {model.EssenceValue} essence     ·     Discovery weight {model.DiscoveryWeight}");
            SetText(_effectsLabel, Effects(model));
        }

        private string Effects(RelicDetailModel model)
        {
            if (model.EffectSummaries == null || model.EffectSummaries.Count == 0)
            {
                return "Grants nothing on its own.";
            }

            _builder.Length = 0;

            for (int i = 0; i < model.EffectSummaries.Count; i++)
            {
                if (i > 0)
                {
                    _builder.AppendLine();
                }

                _builder.Append("• ").Append(model.EffectSummaries[i]);
            }

            return _builder.ToString();
        }
    }
}
