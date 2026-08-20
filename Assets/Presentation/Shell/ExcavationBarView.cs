using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// Raises the intent to excavate and turns the terminal it gets back into a sentence. The reasons come
    /// from the rules; the English lives here, because player copy is not a domain concern.
    /// </summary>
    public sealed class ExcavationBarView : View
    {
        private readonly string _readyLabel = "EXCAVATE";
        private readonly string _busyLabel = "EXCAVATING";

        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _buttonLabel;
        [SerializeField] private TextMeshProUGUI _statusText;

        private AcquisitionCoordinator _coordinator;

        /// <summary>
        /// Raised once per terminal that has something to tell the player. The shell owns every domain
        /// subscription, so this view reports upwards instead of writing to the ledger itself.
        /// </summary>
        public event System.Action<string, LedgerKind> Reported;

        public void Bind(AcquisitionCoordinator coordinator)
        {
            _coordinator = coordinator;
            SetText(_statusText, "Dig for a relic.");
            RefreshBusyState();
        }

        /// <summary>
        /// Every terminal the coordinator can raise, routed here by the shell. The first line is the gate for
        /// all five: it reads the coordinator instead of each arm claiming what the button should be, so a
        /// superseded terminal — which arrives while a newer request is still in flight — leaves it closed.
        /// </summary>
        public void Report(AcquisitionCompletion completion)
        {
            RefreshBusyState();

            switch (completion.Reason)
            {
                case AcquisitionCompletionReason.Granted:
                    SetText(_statusText, "Ready");
                    break;

                case AcquisitionCompletionReason.Rejected:
                    string refusal = Describe(completion.Rejection);
                    SetText(_statusText, refusal);
                    Reported?.Invoke(refusal, LedgerKind.Problem);
                    break;

                case AcquisitionCompletionReason.Cancelled:
                    SetText(_statusText, "Excavation cancelled");
                    Reported?.Invoke("Excavation cancelled", LedgerKind.Muted);
                    break;

                case AcquisitionCompletionReason.Failed:
                    SetText(_statusText, "Something went wrong — try again");
                    Debug.LogError($"{nameof(ExcavationBarView)}.{nameof(Report)} [Acquisition] failed — {completion.Detail}");
                    Reported?.Invoke("The dig failed", LedgerKind.Problem);
                    break;

                case AcquisitionCompletionReason.Superseded:
                    // A newer request owns the bar. The status line still reads EXCAVATING, and it is true.
                    Debug.LogWarning($"{nameof(ExcavationBarView)}.{nameof(Report)} [Acquisition] superseded — {completion.Detail}");
                    break;

                default:
                    SetText(_statusText, "Ready");
                    Debug.LogError($"{nameof(ExcavationBarView)}.{nameof(Report)} unhandled reason '{completion.Reason}'.");
                    break;
            }
        }

        private void Awake()
        {
            // Wired once, on the view's own button: nothing here accumulates however often a screen is shown.
            _button.onClick.AddListener(OnExcavateClicked);
        }

        private void OnDisable()
        {
            // Closing the bar invalidates whatever is in flight: cancel event E1 of the cancellation matrix.
            // The counter lives with the coordinator, so it outlives this view.
            _coordinator?.CancelPending();
        }

        private async void OnExcavateClicked()
        {
            if (_coordinator == null)
            {
                return;
            }

            SetText(_statusText, "Excavating...");

            Task<AcquisitionCompletion> request = _coordinator.RequestAsync();
            RefreshBusyState();

            try
            {
                await request;
            }
            finally
            {
                // Completed subscribers are contracted not to throw; if one does, the task faults and this is
                // what keeps the button honest anyway.
                RefreshBusyState();
            }
        }

        private void RefreshBusyState()
        {
            bool busy = _coordinator != null && _coordinator.IsBusy;

            _button.interactable = !busy;
            SetText(_buttonLabel, busy ? _busyLabel : _readyLabel);
        }

        private static string Describe(AcquisitionRejection rejection)
        {
            switch (rejection)
            {
                case AcquisitionRejection.CatalogueEmpty:
                    return "There is nothing left to find.";

                default:
                    Debug.LogError($"{nameof(ExcavationBarView)}.{nameof(Describe)} unhandled rejection '{rejection}'.");
                    return "The dig turned up nothing.";
            }
        }
    }
}
