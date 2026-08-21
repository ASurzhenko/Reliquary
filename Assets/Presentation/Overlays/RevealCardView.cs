using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation
{
    /// <summary>What a reveal is about. One card, one state token — never a second card design.</summary>
    public enum RevealKind
    {
        New,
        Duplicate,
        SetComplete
    }

    public readonly struct RevealRequest
    {
        public RevealRequest(RevealKind kind, string name, Sprite icon, int copies, string detail = null)
        {
            Kind = kind;
            Name = name;
            Icon = icon;
            Copies = copies;
            Detail = detail;
        }

        public RevealKind Kind { get; }

        public string Name { get; }

        public Sprite Icon { get; }

        public int Copies { get; }

        /// <summary>What the milestone granted, in the perk's own words. Null for the two relic states.</summary>
        public string Detail { get; }
    }

    /// <summary>
    /// The answer to a press the player waited for. Requests queue rather than stack: a purchase that also
    /// completes a set raises two of these one after the other, and two cards at once is the only way this
    /// vocabulary can collide with itself.
    /// </summary>
    public sealed class RevealCardView : View
    {
        [SerializeField] private TextMeshProUGUI _ribbonLabel;
        [SerializeField] private Image _ribbon;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _lineLabel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Color _newRibbon = new Color(0.55f, 0.82f, 0.55f);
        [SerializeField] private Color _duplicateRibbon = new Color(0.85f, 0.74f, 0.45f);
        [SerializeField] private Color _milestoneRibbon = new Color(0.62f, 0.72f, 0.95f);

        private readonly Queue<RevealRequest> _queue = new Queue<RevealRequest>();

        private OverlayRoot _overlays;
        private bool _showing;

        public void Bind(OverlayRoot overlays)
        {
            _overlays = overlays;
        }

        public void Enqueue(RevealRequest request)
        {
            _queue.Enqueue(request);

            if (!_showing)
            {
                ShowNext();
            }
        }

        private void Awake()
        {
            _continueButton.onClick.AddListener(OnContinue);
        }

        private void OnContinue()
        {
            ShowNext();
        }

        private void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _showing = false;
                _overlays.Hide();
                return;
            }

            RevealRequest request = _queue.Dequeue();

            _showing = true;
            _overlays.Show(gameObject);

            _icon.sprite = request.Icon;
            _icon.enabled = request.Icon != null;
            _ribbon.color = RibbonOf(request.Kind);

            SetText(_ribbonLabel, RibbonTextOf(request));
            SetText(_nameLabel, request.Name);
            SetText(_lineLabel, Line(request));
        }

        private Color RibbonOf(RevealKind kind)
        {
            switch (kind)
            {
                case RevealKind.New:
                    return _newRibbon;

                case RevealKind.Duplicate:
                    return _duplicateRibbon;

                case RevealKind.SetComplete:
                    return _milestoneRibbon;

                default:
                    Debug.LogError($"{nameof(RevealCardView)}.{nameof(RibbonOf)} unhandled kind '{kind}'.");
                    return _newRibbon;
            }
        }

        private static string RibbonTextOf(RevealRequest request)
        {
            switch (request.Kind)
            {
                case RevealKind.New:
                    return "NEW";

                case RevealKind.Duplicate:
                    return $"COPY ×{request.Copies}";

                case RevealKind.SetComplete:
                    return "SET COMPLETE";

                default:
                    return "FOUND";
            }
        }

        private static string Line(RevealRequest request)
        {
            switch (request.Kind)
            {
                case RevealKind.New:
                    return "A relic you have never held.";

                case RevealKind.Duplicate:
                    return "Another copy for the reliquary — spare copies are what essence is made of.";

                case RevealKind.SetComplete:
                    return string.IsNullOrEmpty(request.Detail)
                        ? "The set is complete."
                        : $"Its perk is active from now on: {request.Detail}";

                default:
                    Debug.LogError($"{nameof(RevealCardView)}.{nameof(Line)} unhandled kind '{request.Kind}'.");
                    return string.Empty;
            }
        }
    }
}
