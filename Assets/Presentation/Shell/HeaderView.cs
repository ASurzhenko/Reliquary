using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation
{
    /// <summary>
    /// The title, and the one value that is never a tab's property: essence. It lives here because three of
    /// the six feedback events move it, and a counter that is only visible on the screen that spends it
    /// cannot be the target of a change raised from another screen.
    /// </summary>
    public sealed class HeaderView : View
    {
        private readonly float _flashSeconds = 0.6f;

        [SerializeField] private TextMeshProUGUI _essenceLabel;
        [SerializeField] private Image _essenceIcon;
        [SerializeField] private Color _gainFlash = new Color(0.55f, 0.82f, 0.55f);
        [SerializeField] private Color _spendFlash = new Color(0.85f, 0.74f, 0.45f);

        /// <summary>Redraws the balance. The pill sits in a layout group, so the label is laid out with it.</summary>
        public void ShowBalance(int balance)
        {
            SetText(_essenceLabel, balance.ToString());
        }

        /// <summary>
        /// Says that the balance just moved, at the element that owns it. The direction is the sign the
        /// domain reported; nothing here works out what a change was worth.
        /// </summary>
        public void FlashBalance(bool gained)
        {
            Flash(_essenceLabel, gained ? _gainFlash : _spendFlash, _flashSeconds);
            Flash(_essenceIcon, gained ? _gainFlash : _spendFlash, _flashSeconds);
        }
    }
}
