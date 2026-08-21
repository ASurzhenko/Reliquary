using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// What essence can buy. The trader is not a shop with a shelf: it sells one relic at a time, chosen to
    /// close the set the player is nearest to finishing, and the button says which set that is. Nothing here
    /// works out a price, an affordability or a deficit — every one of those is the domain's answer.
    /// </summary>
    public sealed class TraderScreenView : ScreenView
    {
        private readonly float _flashSeconds = 0.6f;

        [SerializeField] private TextMeshProUGUI _rotationLabel;
        [SerializeField] private GameObject _offerRoot;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _reasonLabel;
        [SerializeField] private TextMeshProUGUI _priceLabel;
        [SerializeField] private Button _acquireButton;
        [SerializeField] private Image _acquireBackground;
        [SerializeField] private TextMeshProUGUI _acquireLabel;
        [SerializeField] private GameObject _routeRoot;
        [SerializeField] private TextMeshProUGUI _routeLabel;
        [SerializeField] private Button _routeButton;
        [SerializeField] private GameObject _absenceRoot;
        [SerializeField] private TextMeshProUGUI _absenceHeadline;
        [SerializeField] private TextMeshProUGUI _absenceBody;
        [SerializeField] private Color _affordable = new Color(0.36f, 0.31f, 0.22f);
        [SerializeField] private Color _unaffordable = new Color(0.20f, 0.21f, 0.24f);
        [SerializeField] private Color _changedFlash = new Color(0.62f, 0.72f, 0.95f);

        private UiContext _context;
        private Action<RelicId> _buyRequested;
        private Action _vaultRequested;
        private RelicId _offered;

        public void Bind(UiContext context, Action<RelicId> buyRequested, Action vaultRequested)
        {
            _context = context;
            _buyRequested = buyRequested;
            _vaultRequested = vaultRequested;

            SetText(_rotationLabel, "One relic at a time, and it changes as your collection does.");
            SetText(_routeLabel, "Dissolve a spare copy in the Vault.");

            Refresh();
        }

        public override void Refresh()
        {
            if (_context == null)
            {
                return;
            }

            TraderOffer offer = _context.Trader.CurrentOffer;

            _offerRoot.SetActive(offer.HasOffer);
            _absenceRoot.SetActive(!offer.HasOffer);

            if (!offer.HasOffer)
            {
                ShowAbsence(offer.Absence);
                return;
            }

            _offered = offer.Relic;

            RelicTileModel relic = ViewModels.Tile(offer.Relic, _context.Presentation,
                _context.Inventory.CountOf(offer.Relic));

            _icon.sprite = relic.Icon;
            _icon.enabled = relic.Icon != null;

            SetText(_nameLabel, relic.Name);
            SetText(_reasonLabel, ReasonFor(offer));

            // The offer is never hidden because it cannot be afforded: a shop that hides what you cannot buy
            // removes the reason to earn.
            SetText(_priceLabel, offer.CanAfford
                ? $"{offer.Price} essence"
                : $"{offer.Price} essence — you have {_context.Wallet.Balance}");

            SetText(_acquireLabel, offer.CanAfford ? "ACQUIRE" : $"NEED {offer.Deficit} MORE");

            _acquireButton.interactable = offer.CanAfford;
            SetColour(_acquireBackground, offer.CanAfford ? _affordable : _unaffordable);
            _routeRoot.SetActive(!offer.CanAfford);
        }

        /// <summary>The panel now holds a different offer. Said where the change happened.</summary>
        public bool FlashOffer()
        {
            return Flash(_nameLabel, _changedFlash, _flashSeconds) & Flash(_priceLabel, _changedFlash, _flashSeconds);
        }

        private void Awake()
        {
            _acquireButton.onClick.AddListener(OnAcquireClicked);
            _routeButton.onClick.AddListener(OnRouteClicked);
        }

        private string ReasonFor(TraderOffer offer)
        {
            if (!offer.FocusSet.IsValid)
            {
                return "A relic you have not found yet.";
            }

            string name = ViewModels.SetName(offer.FocusSet, _context.SetPresentation);

            // "Completes" is a promise, so it is only made when buying this relic actually finishes the set.
            // Every other time the offer is a step towards it, and saying so is the difference between a
            // player expecting a completion card and getting one.
            return offer.FocusProgress.Owned + 1 == offer.FocusProgress.Total
                ? $"Completes {name} — {offer.FocusProgress.Owned} of {offer.FocusProgress.Total}"
                : $"Towards {name} — {offer.FocusProgress.Owned} of {offer.FocusProgress.Total}";
        }

        private void ShowAbsence(TraderAbsence absence)
        {
            switch (absence)
            {
                case TraderAbsence.NothingMissing:
                    SetText(_absenceHeadline, "You own every relic the Trader can source.");
                    SetText(_absenceBody,
                        $"{_context.Inventory.DistinctCount} of {_context.Catalog.Count} collected. " +
                        "Spare copies are still worth essence.");
                    return;

                case TraderAbsence.NoCatalogue:
                    SetText(_absenceHeadline, "The Trader has nothing to sell.");
                    SetText(_absenceBody, "This build ships no relics, so there is nothing to source.");
                    return;

                default:
                    SetText(_absenceHeadline, "The Trader has nothing to sell.");
                    SetText(_absenceBody, "Come back once you have found more.");
                    Debug.LogError($"{nameof(TraderScreenView)}.{nameof(ShowAbsence)} unhandled absence '{absence}'.");
                    return;
            }
        }

        private void OnAcquireClicked()
        {
            _buyRequested?.Invoke(_offered);
        }

        private void OnRouteClicked()
        {
            _vaultRequested?.Invoke();
        }
    }
}
