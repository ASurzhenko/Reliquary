using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation
{
    /// <summary>
    /// What every view in this project shares: a way to write a label that is laid out in the same frame it
    /// is written. Nothing here reads or decides anything about the rules.
    /// </summary>
    public abstract class View : MonoBehaviour
    {
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
    }
}
