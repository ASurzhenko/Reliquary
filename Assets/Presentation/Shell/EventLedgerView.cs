using System.Collections;
using System.Collections.Generic;
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
    ///
    /// A single gesture can raise several entries in one frame — a purchase spends, then yields a relic, then
    /// completes a set, then starts a perk paying. Each line therefore holds the slot for a moment before the
    /// next one takes it, so a sequence is read rather than replaced by its own last entry.
    /// </summary>
    public sealed class EventLedgerView : View
    {
        private readonly float _restingAlpha = 0.4f;
        private readonly int _queueCeiling = 12;

        [SerializeField] private CanvasGroup _line;
        [SerializeField] private TextMeshProUGUI _lineLabel;
        [SerializeField] private Image _kindMarker;
        [SerializeField] private float _dwellSeconds = 1.1f;
        [SerializeField] private float _holdSeconds = 4f;

        [Header("Colour by kind")]
        [SerializeField] private Color _gain = new Color(0.55f, 0.82f, 0.55f);
        [SerializeField] private Color _spend = new Color(0.85f, 0.74f, 0.45f);
        [SerializeField] private Color _milestone = new Color(0.62f, 0.72f, 0.95f);
        [SerializeField] private Color _problem = new Color(0.90f, 0.50f, 0.48f);
        [SerializeField] private Color _muted = new Color(0.62f, 0.66f, 0.72f);

        private readonly Queue<Entry> _pending = new Queue<Entry>();
        private readonly List<string> _shown = new List<string>();

        private Coroutine _run;

        /// <summary>
        /// The lines this slot has actually displayed, newest last. Nothing in the product reads it: it
        /// exists so that a sequence raised by one gesture can be verified by execution, which is the only
        /// assurance available to a layer no test assembly can reach.
        /// </summary>
        public IReadOnlyList<string> Recent => _shown;

        public void Show(string message, LedgerKind kind)
        {
            if (_pending.Count >= _queueCeiling)
            {
                // Said out loud rather than dropped quietly: a ledger that silently loses entries is worse
                // than one that admits it, because the entry it loses is the one nobody saw.
                Debug.LogWarning($"{nameof(EventLedgerView)}.{nameof(Show)} the queue is full at {_queueCeiling}; " +
                    $"'{message}' was dropped.");
                return;
            }

            _pending.Enqueue(new Entry(message, kind));

            if (_run == null && isActiveAndEnabled)
            {
                _run = StartCoroutine(Drain());
            }
        }

        private void OnEnable()
        {
            // Entries can arrive while this object is off — nothing else in the shell is — and a queue that
            // nobody drains is a report nobody reads.
            if (_run == null && _pending.Count > 0)
            {
                _run = StartCoroutine(Drain());
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_run == null)
            {
                return;
            }

            StopCoroutine(_run);
            _run = null;
        }

        private IEnumerator Drain()
        {
            while (true)
            {
                while (_pending.Count > 0)
                {
                    Write(_pending.Dequeue());

                    yield return new WaitForSeconds(_dwellSeconds);
                }

                // Nothing else is waiting, so the last line stays readable and then steps back rather than
                // disappearing: the slot is never empty once something has happened in this session. The
                // wait is watched rather than slept through — an event raised during it is the player's most
                // recent action, and it would otherwise sit unsaid for the rest of the hold.
                float until = Time.time + Mathf.Max(0f, _holdSeconds - _dwellSeconds);

                while (Time.time < until && _pending.Count == 0)
                {
                    yield return null;
                }

                if (_pending.Count == 0)
                {
                    break;
                }
            }

            _line.alpha = _restingAlpha;
            _run = null;
        }

        private void Write(Entry entry)
        {
            Color colour = ColourOf(entry.Kind);

            _kindMarker.color = colour;
            _lineLabel.color = colour;
            SetText(_lineLabel, entry.Message);

            _line.alpha = 1f;

            _shown.Add(entry.Message);

            if (_shown.Count > _queueCeiling)
            {
                _shown.RemoveAt(0);
            }
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

        private readonly struct Entry
        {
            public Entry(string message, LedgerKind kind)
            {
                Message = message;
                Kind = kind;
            }

            public string Message { get; }

            public LedgerKind Kind { get; }
        }
    }
}
