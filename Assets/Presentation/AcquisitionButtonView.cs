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
    public sealed class AcquisitionButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _statusText;

        private AcquisitionCoordinator _coordinator;

        /// <summary>
        /// The single place this view subscribes. Unbinding first makes a second Bind harmless, which is
        /// what removes the ordering question between the composition root's Awake and this view's OnEnable.
        /// </summary>
        public void Bind(AcquisitionCoordinator coordinator)
        {
            Unbind();

            _coordinator = coordinator;
            Subscribe();
            _statusText.text = "Dig for a relic.";
        }

        private void OnEnable()
        {
            if (_coordinator == null)
            {
                return;
            }

            Subscribe();
        }

        private void OnDisable()
        {
            // Closing the panel invalidates whatever is in flight: cancel event E1 of the cancellation
            // matrix. The counter lives with the coordinator, so it outlives this view.
            _coordinator?.CancelPending();
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Unsubscribes before subscribing, so it does not matter whether the composition root's Awake
        /// binds this view before or after its own OnEnable runs: one click stays one request.
        /// </summary>
        private void Subscribe()
        {
            _coordinator.Completed -= OnCompleted;
            _coordinator.Completed += OnCompleted;
            _button.onClick.RemoveListener(OnAcquireClicked);
            _button.onClick.AddListener(OnAcquireClicked);
        }

        private void Unbind()
        {
            if (_coordinator == null)
            {
                return;
            }

            _coordinator.Completed -= OnCompleted;
            _button.onClick.RemoveListener(OnAcquireClicked);
        }

        /// <summary>
        /// The button stays interactable while a request is in flight: pressing it again supersedes the
        /// older request, which is a path worth being able to drive by hand.
        /// </summary>
        private async void OnAcquireClicked()
        {
            if (_coordinator == null)
            {
                return;
            }

            _statusText.text = "Excavating...";

            try
            {
                await _coordinator.RequestAsync();
            }
            finally
            {
                // Completed subscribers are contracted not to throw; if one does, the task faults and this
                // is what keeps the button usable anyway.
                _button.interactable = true;
            }
        }

        private void OnCompleted(AcquisitionCompletion completion)
        {
            switch (completion.Reason)
            {
                case AcquisitionCompletionReason.Granted:
                    _statusText.text = completion.WasFirstCopy
                        ? "A relic you have never held."
                        : "Another copy for the reliquary.";
                    break;

                case AcquisitionCompletionReason.Rejected:
                    _statusText.text = Describe(completion.Rejection);
                    break;

                case AcquisitionCompletionReason.Cancelled:
                    _statusText.text = "The dig was called off.";
                    break;

                case AcquisitionCompletionReason.Superseded:
                    _statusText.text = "An older dig came back too late.";
                    break;

                case AcquisitionCompletionReason.Failed:
                    _statusText.text = "The dig failed. Try again.";
                    break;

                default:
                    _statusText.text = "The dig ended.";
                    Debug.LogError($"{nameof(AcquisitionButtonView)}.{nameof(OnCompleted)} unhandled reason '{completion.Reason}'.");
                    break;
            }
        }

        private static string Describe(AcquisitionRejection rejection)
        {
            switch (rejection)
            {
                case AcquisitionRejection.CatalogueEmpty:
                    return "There is nothing left to find.";

                default:
                    Debug.LogError($"{nameof(AcquisitionButtonView)}.{nameof(Describe)} unhandled rejection '{rejection}'.");
                    return "The dig turned up nothing.";
            }
        }
    }
}
