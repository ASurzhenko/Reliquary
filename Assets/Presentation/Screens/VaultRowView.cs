using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// One owned relic in the vault. Like a collection tile it is bound once and refreshed afterwards; what
    /// the vault's ordering moves is the row's sibling index, never which relic the row is.
    /// </summary>
    public sealed class VaultRowView : View
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _copiesLabel;

        private RelicId _boundId;
        private Action<RelicId> _clicked;

        public RelicId BoundId => _boundId;

        /// <summary>Called once per row object, ever.</summary>
        public void Bind(RelicId id, Action<RelicId> clicked)
        {
            _boundId = id;
            _clicked = clicked;
        }

        public void Refresh(VaultRowModel model)
        {
            _icon.sprite = model.Icon;
            _icon.enabled = model.Icon != null;

            SetText(_nameLabel, model.Name);
            SetText(_copiesLabel, $"×{model.Copies}");
        }

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            _clicked?.Invoke(_boundId);
        }
    }
}
