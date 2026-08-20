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
        Duplicate
    }

    public readonly struct RevealRequest
    {
        public RevealRequest(RevealKind kind, string name, Sprite icon, int copies)
        {
            Kind = kind;
            Name = name;
            Icon = icon;
            Copies = copies;
        }

        public RevealKind Kind { get; }

        public string Name { get; }

        public Sprite Icon { get; }

        public int Copies { get; }
    }

    /// <summary>
    /// The answer to a press the player waited for. Requests queue rather than stack: two cards at once is
    /// the only way this vocabulary can collide with itself, and a queue is the whole fix.
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
            _ribbon.color = request.Kind == RevealKind.New ? _newRibbon : _duplicateRibbon;

            SetText(_ribbonLabel, request.Kind == RevealKind.New ? "NEW" : $"COPY ×{request.Copies}");
            SetText(_nameLabel, request.Name);
            SetText(_lineLabel, Line(request));
        }

        private static string Line(RevealRequest request)
        {
            return request.Kind == RevealKind.New
                ? "A relic you have never held."
                : "Another copy for the reliquary — spare copies are what essence is made of.";
        }
    }
}
