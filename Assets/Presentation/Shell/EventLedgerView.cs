using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation
{
    /// <summary>
    /// What kind of thing happened. The vocabulary has four of them plus a muted one for an event the player
    /// caused and already knows about; nothing else may be invented for a new event.
    /// </summary>
    public enum LedgerKind
    {
        Gain,
        Spend,
        Milestone,
        Problem,
        Muted
    }

    /// <summary>
    /// One line, one slot, always in the same place: what changed, said in words. It is also the only surface
    /// a refusal or a failed save can be reported on, which is why it is not decoration.
    /// </summary>
    public sealed class EventLedgerView : View
    {
        private readonly float _restingAlpha = 0.4f;

        [SerializeField] private CanvasGroup _line;
        [SerializeField] private TextMeshProUGUI _lineLabel;
        [SerializeField] private Image _kindMarker;
        [SerializeField] private float _holdSeconds = 4f;

        [Header("Colour by kind")]
        [SerializeField] private Color _gain = new Color(0.55f, 0.82f, 0.55f);
        [SerializeField] private Color _spend = new Color(0.85f, 0.74f, 0.45f);
        [SerializeField] private Color _milestone = new Color(0.62f, 0.72f, 0.95f);
        [SerializeField] private Color _problem = new Color(0.90f, 0.50f, 0.48f);
        [SerializeField] private Color _muted = new Color(0.62f, 0.66f, 0.72f);

        private Coroutine _hold;
        private int _problemFrame = -1;

        /// <summary>
        /// Writes the newest event into the one slot. A problem keeps the slot for the rest of the frame it
        /// was raised in: a failed save is raised by the same change that produces the find it accompanies,
        /// and the find would otherwise overwrite it in the same frame and leave the failure unsaid.
        /// </summary>
        public void Show(string message, LedgerKind kind)
        {
            if (kind != LedgerKind.Problem && _problemFrame == Time.frameCount)
            {
                return;
            }

            if (kind == LedgerKind.Problem)
            {
                _problemFrame = Time.frameCount;
            }

            Color colour = ColourOf(kind);

            _kindMarker.color = colour;
            _lineLabel.color = colour;
            SetText(_lineLabel, message);

            _line.alpha = 1f;

            if (_hold != null)
            {
                StopCoroutine(_hold);
                _hold = null;
            }

            if (isActiveAndEnabled)
            {
                _hold = StartCoroutine(FadeAfterHold());
            }
        }

        private void OnDisable()
        {
            if (_hold == null)
            {
                return;
            }

            StopCoroutine(_hold);
            _hold = null;
        }

        private IEnumerator FadeAfterHold()
        {
            yield return new WaitForSeconds(_holdSeconds);

            _line.alpha = _restingAlpha;
            _hold = null;
        }

        private Color ColourOf(LedgerKind kind)
        {
            switch (kind)
            {
                case LedgerKind.Gain:
                    return _gain;

                case LedgerKind.Spend:
                    return _spend;

                case LedgerKind.Milestone:
                    return _milestone;

                case LedgerKind.Problem:
                    return _problem;

                case LedgerKind.Muted:
                    return _muted;

                default:
                    Debug.LogError($"{nameof(EventLedgerView)}.{nameof(ColourOf)} unhandled kind '{kind}'.");
                    return _muted;
            }
        }
    }
}
