using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// One owned relic in the vault, and the only destructive control in the product. Like a collection tile
    /// it is bound once and refreshed afterwards; what the vault's ordering moves is the row's sibling index,
    /// never which relic the row is.
    /// </summary>
    public sealed class VaultRowView : View
    {
        private readonly float _flashSeconds = 0.6f;

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _copiesLabel;
        [SerializeField] private GameObject _dissolveRoot;
        [SerializeField] private Button _dissolveButton;
        [SerializeField] private Image _dissolveBackground;
        [SerializeField] private TextMeshProUGUI _dissolveLabel;
        [SerializeField] private GameObject _noteRoot;
        [SerializeField] private TextMeshProUGUI _noteLabel;
        [SerializeField] private Color _restingDissolve = new Color(0.36f, 0.31f, 0.22f);
        [SerializeField] private Color _armedDissolve = new Color(0.62f, 0.30f, 0.26f);
        [SerializeField] private Color _gainFlash = new Color(0.55f, 0.82f, 0.55f);

        private RelicId _boundId;
        private Action<RelicId> _clicked;
        private Action<RelicId> _dissolveRequested;
        private Action<RelicId> _armedNotice;
        private int _yield;
        private bool _armed;

        public RelicId BoundId => _boundId;

        /// <summary>Called once per row object, ever.</summary>
        public void Bind(RelicId id, Action<RelicId> clicked, Action<RelicId> dissolveRequested,
            Action<RelicId> armedNotice)
        {
            _boundId = id;
            _clicked = clicked;
            _dissolveRequested = dissolveRequested;
            _armedNotice = armedNotice;
        }

        public void Refresh(VaultRowModel model)
        {
            _icon.sprite = model.Icon;
            _icon.enabled = model.Icon != null;
            _yield = model.Yield;

            SetText(_nameLabel, model.Name);
            SetText(_copiesLabel, $"×{model.Copies}");

            // Whether a copy can be spared is the exchange's answer, never a count this row compared itself.
            Disarm();
            _dissolveRoot.SetActive(model.CanDissolve);
            _noteRoot.SetActive(!model.CanDissolve && model.Refusal == DissolveOutcome.NoYield);

            if (model.CanDissolve)
            {
                SetText(_dissolveLabel, $"DISSOLVE  +{model.Yield}");
                return;
            }

            if (model.Refusal == DissolveOutcome.NoYield)
            {
                // A spare copy that would pay nothing is not a missing button — destroying it for nothing is
                // pure loss, and the row says so rather than leaving the player to wonder.
                SetText(_noteLabel, "A spare copy, worth nothing right now.");
            }
        }

        /// <summary>The essence this row just paid. Returns false when the row is not on screen to say it.</summary>
        public bool FlashCopies()
        {
            return Flash(_copiesLabel, _gainFlash, _flashSeconds);
        }

        /// <summary>What this row is worth changed — a completed set's perk moved the number on the button.</summary>
        public bool FlashYield()
        {
            return _dissolveRoot.activeSelf && Flash(_dissolveLabel, _gainFlash, _flashSeconds);
        }

        /// <summary>Takes the row out of its armed state without consuming anything.</summary>
        public void Disarm()
        {
            _armed = false;
            SetColour(_dissolveBackground, _restingDissolve);

            if (_dissolveRoot.activeSelf)
            {
                SetText(_dissolveLabel, $"DISSOLVE  +{_yield}");
            }
        }

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
            _dissolveButton.onClick.AddListener(OnDissolveClicked);
        }

        private void OnClicked()
        {
            _clicked?.Invoke(_boundId);
        }

        /// <summary>
        /// The first press arms, the second consumes. The interruption for a destructive act belongs before
        /// the press rather than after it, and an ambiguous tap must never destroy a copy.
        /// </summary>
        private void OnDissolveClicked()
        {
            if (!_armed)
            {
                _armed = true;
                _dissolveBackground.color = _armedDissolve;
                SetText(_dissolveLabel, $"TAP AGAIN  +{_yield}");
                _armedNotice?.Invoke(_boundId);
                return;
            }

            Disarm();
            _dissolveRequested?.Invoke(_boundId);
        }
    }
}
