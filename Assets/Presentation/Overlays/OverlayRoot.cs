using UnityEngine;

namespace Reliquary.Presentation
{
    /// <summary>
    /// Which overlay is up. It is the last child of the frame, so whatever it shows draws above the tab bar
    /// and its own dimmer owns the raycast — a "modal" that leaves the tabs clickable is not one.
    /// </summary>
    public sealed class OverlayRoot : MonoBehaviour
    {
        [SerializeField] private GameObject[] _overlays;

        private void Awake()
        {
            Hide();
        }

        public void Show(GameObject overlay)
        {
            for (int i = 0; i < _overlays.Length; i++)
            {
                _overlays[i].SetActive(_overlays[i] == overlay);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            for (int i = 0; i < _overlays.Length; i++)
            {
                _overlays[i].SetActive(false);
            }

            gameObject.SetActive(false);
        }
    }
}
