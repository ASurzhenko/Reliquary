using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation
{
    /// <summary>
    /// What every view in this project shares: a way to write a label that is laid out in the same frame it
    /// is written, and a way to say "this number just moved" without inventing a second vocabulary for it.
    /// Nothing here reads or decides anything about the rules.
    /// </summary>
    public abstract class View : MonoBehaviour
    {
        private readonly Dictionary<Graphic, Coroutine> _flashes = new Dictionary<Graphic, Coroutine>();
        private readonly Dictionary<Graphic, Color> _restore = new Dictionary<Graphic, Color>();

        /// <summary>
        /// Sets a label and lays it out in the same frame. Assigning text only marks layout dirty, so a
        /// ContentSizeFitter still reports the old size and whatever lays the label out uses a stale width —
        /// visibly overlapping until something else triggers a rebuild. Use it for any label whose own size,
        /// or the size of a container above it, follows the text; a label with a fixed rect does not need it.
        /// </summary>
        protected static void SetText(TMP_Text label, string value)
        {
            label.text = value;
            label.ForceMeshUpdate();

            RectTransform target = label.GetComponent<ContentSizeFitter>() != null
                ? (RectTransform)label.transform
                : null;

            // The walk does not stop at the first ancestor that drives nothing: a button is usually
            // Button -> Text, and the button's own rect drives no layout while the row above it does. It is
            // bounded at the canvas, so a rebuild cannot propagate across the whole tree.
            for (Transform current = label.transform.parent; current != null; current = current.parent)
            {
                if (current.GetComponent<Canvas>() != null)
                {
                    break;
                }

                bool drivesLayout = current.GetComponent<LayoutGroup>() != null
                    || current.GetComponent<ContentSizeFitter>() != null;

                if (drivesLayout)
                {
                    target = (RectTransform)current;
                }
            }

            if (target != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(target);
            }
        }

        /// <summary>
        /// Marks the element that owns a changed value: the colour is applied and held, then released. It is
        /// the whole pulse until motion lands — the shape the vocabulary degrades to with animation switched
        /// off — and it is deliberately the ONLY way this layer says "look here".
        ///
        /// Returns false when it could not play because the object is not on screen. The caller records the
        /// request and replays it on OnShown; a coroutine cannot start on an inactive GameObject at all.
        /// </summary>
        protected bool Flash(Graphic target, Color colour, float seconds)
        {
            if (target == null)
            {
                return true;
            }

            if (!isActiveAndEnabled)
            {
                return false;
            }

            if (_flashes.TryGetValue(target, out Coroutine running))
            {
                StopCoroutine(running);
            }
            else
            {
                // Captured on the first flash only, so a second flash during the first does not record the
                // flash colour as the colour to go back to.
                _restore[target] = target.color;
            }

            _flashes[target] = StartCoroutine(FlashRoutine(target, colour, seconds));
            return true;
        }

        /// <summary>
        /// Writes a state colour without fighting a flash. A redraw that lands mid-flash would otherwise
        /// overwrite the flash colour and then be overwritten in turn by the colour the flash restores —
        /// leaving the element the wrong colour and the change unsaid. During a flash the new state colour
        /// becomes what the flash restores to instead.
        /// </summary>
        protected void SetColour(Graphic target, Color colour)
        {
            if (target == null)
            {
                return;
            }

            if (_restore.ContainsKey(target))
            {
                _restore[target] = colour;
                return;
            }

            target.color = colour;
        }

        /// <summary>
        /// Restores anything a flash was in the middle of. Every view that needs OnDisable overrides this,
        /// so the teardown here always runs — a private OnDisable in a subclass would hide it silently.
        /// </summary>
        protected virtual void OnDisable()
        {
            foreach (KeyValuePair<Graphic, Coroutine> flash in _flashes)
            {
                if (flash.Value != null)
                {
                    StopCoroutine(flash.Value);
                }
            }

            foreach (KeyValuePair<Graphic, Color> original in _restore)
            {
                if (original.Key != null)
                {
                    original.Key.color = original.Value;
                }
            }

            _flashes.Clear();
            _restore.Clear();
        }

        private IEnumerator FlashRoutine(Graphic target, Color colour, float seconds)
        {
            target.color = colour;

            yield return new WaitForSeconds(seconds);

            if (_restore.TryGetValue(target, out Color original))
            {
                target.color = original;
            }

            _restore.Remove(target);
            _flashes.Remove(target);
        }
    }
}
