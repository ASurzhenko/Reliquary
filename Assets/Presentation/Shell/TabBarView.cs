using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation
{
    /// <summary>
    /// Three surfaces, three questions, one of them visible at a time. Acquisition is deliberately not one of
    /// them: it sits above this bar on every tab, so the core action is never a navigation step away.
    /// </summary>
    public sealed class TabBarView : View
    {
        [SerializeField] private Tab[] _tabs;

        public void Bind()
        {
            Select(0);
        }

        private void Awake()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                _tabs[i].Button.onClick.AddListener(() => Select(index));
            }
        }

        private void Select(int index)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool selected = i == index;

                _tabs[i].SetSelected(selected);
                _tabs[i].Screen.gameObject.SetActive(selected);
            }

            // A screen is refreshed by the shell whether it is active or not; being shown is a separate event,
            // and it is what a change that arrived while the tab was hidden waits for.
            _tabs[index].Screen.OnShown();
        }

        /// <summary>One tab: the control, its label, and the screen it shows.</summary>
        [Serializable]
        public sealed class Tab
        {
            [SerializeField] private Button _button;
            [SerializeField] private Image _background;
            [SerializeField] private TextMeshProUGUI _label;
            [SerializeField] private ScreenView _screen;
            [SerializeField] private Color _selectedBackground = new Color(0.18f, 0.19f, 0.22f);
            [SerializeField] private Color _restingBackground = new Color(0.12f, 0.13f, 0.15f);

            public Button Button => _button;

            public ScreenView Screen => _screen;

            public void SetSelected(bool selected)
            {
                _background.color = selected ? _selectedBackground : _restingBackground;
                _label.color = selected ? Color.white : new Color(0.62f, 0.66f, 0.72f);
            }
        }
    }
}
