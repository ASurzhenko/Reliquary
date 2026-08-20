using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// One relic in the collection grid. A tile is married to its relic when it is built and never re-bound:
    /// what a change moves is its content, never which relic it is.
    /// </summary>
    public sealed class RelicTileView : View
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private GameObject _badge;
        [SerializeField] private TextMeshProUGUI _badgeLabel;

        [Header("How a state reads")]
        [SerializeField] private Color _ownedBackground = new Color(0.20f, 0.22f, 0.26f);
        [SerializeField] private Color _unfoundBackground = new Color(0.14f, 0.15f, 0.18f);
        [SerializeField] private Color _ownedName = new Color(0.92f, 0.93f, 0.96f);
        [SerializeField] private Color _unfoundName = new Color(0.92f, 0.93f, 0.96f, 0.6f);

        private RelicId _boundId;
        private Action<RelicId> _clicked;

        public RelicId BoundId => _boundId;

        /// <summary>Called once per tile object, ever. Identity is a relic id, never a position in a list.</summary>
        public void Bind(RelicId id, Action<RelicId> clicked)
        {
            _boundId = id;
            _clicked = clicked;
        }

        public void Refresh(RelicTileModel model)
        {
            _icon.sprite = model.Icon;
            _icon.enabled = model.Icon != null;
            _icon.color = model.State == RelicTileState.Unfound ? new Color(1f, 1f, 1f, 0.25f) : Color.white;

            _nameLabel.color = model.State == RelicTileState.Unfound ? _unfoundName : _ownedName;
            _background.color = model.State == RelicTileState.Unfound ? _unfoundBackground : _ownedBackground;
            SetText(_nameLabel, model.Name);

            _badge.SetActive(model.State == RelicTileState.Duplicate);

            if (model.State == RelicTileState.Duplicate)
            {
                SetText(_badgeLabel, $"×{model.Copies}");
            }
        }

        private void Awake()
        {
            // Wired once on the tile's own button. Bind assigns the delegate rather than adding to it, so a
            // tile that is shown a hundred times still raises one click.
            _button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            _clicked?.Invoke(_boundId);
        }
    }
}
