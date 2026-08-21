using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// One set inside the collection: what it is called, how far along it is, and what completing it grants.
    /// Set progress is not a fourth tab because it is not a fourth thing — it is the collection's own reading
    /// of itself, so it is drawn where the relics are.
    /// </summary>
    public sealed class SetSectionView : View
    {
        private readonly float _flashSeconds = 0.6f;

        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _counterLabel;
        [SerializeField] private Image _barFill;
        [SerializeField] private GameObject _bar;
        [SerializeField] private TextMeshProUGUI _perkLabel;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private Color _completeBar = new Color(0.55f, 0.82f, 0.55f);
        [SerializeField] private Color _progressBar = new Color(0.62f, 0.72f, 0.95f);
        [SerializeField] private Color _milestoneFlash = new Color(0.62f, 0.72f, 0.95f);

        private SetId _boundId;

        /// <summary>Where this section's tiles are parented. Filled once, at bind.</summary>
        public RectTransform Grid => _grid;

        public SetId BoundId => _boundId;

        /// <summary>Called once per section object, ever. The default id is the loose-relics section.</summary>
        public void Bind(SetId id)
        {
            _boundId = id;
        }

        public void Refresh(SetSectionModel model)
        {
            SetText(_nameLabel, model.Name);
            SetText(_counterLabel, model.Counter);
            SetText(_perkLabel, model.PerkLine);

            _bar.SetActive(model.TracksProgress);

            if (!model.TracksProgress)
            {
                return;
            }

            _barFill.fillAmount = model.Fraction;
            SetColour(_barFill, model.IsComplete ? _completeBar : _progressBar);
        }

        /// <summary>The set moved. Returns false when the section is not on screen to say it.</summary>
        public bool FlashProgress()
        {
            return Flash(_barFill, _milestoneFlash, _flashSeconds) & Flash(_counterLabel, _milestoneFlash, _flashSeconds);
        }
    }
}
