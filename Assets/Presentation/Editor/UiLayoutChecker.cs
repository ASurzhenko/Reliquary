using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Reliquary.Presentation.EditorTools
{
    /// <summary>
    /// Reports rects that were dragged into place instead of anchored, and rects that were sized by scaling.
    /// The constraint is stated in the project's notes; a constraint nobody can check is a discipline, and a
    /// discipline is what fails on the one machine that opens this project for the first time.
    /// </summary>
    public static class UiLayoutChecker
    {
        private readonly static float _overflowTolerance = 1f;

        [MenuItem("Tools/Reliquary/UI/Check Layout")]
        public static void CheckLayout()
        {
            int findings = 0;

            foreach (RectTransform rect in Sweep())
            {
                findings += ReportPositioned(rect);
                findings += ReportScaled(rect);
            }

            Debug.Log($"{nameof(UiLayoutChecker)}.{nameof(CheckLayout)} {findings} finding(s).");
        }

        /// <summary>
        /// The play-mode half: a rect whose corners fall outside its parent's is a label that has grown past
        /// what holds it. Scroll content is excluded — overflowing a viewport is what it is for.
        /// </summary>
        [MenuItem("Tools/Reliquary/UI/Audit Layout (Play Mode)")]
        public static void AuditLayout()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(UiLayoutChecker)}.{nameof(AuditLayout)} needs play mode: rects have no laid-out size until then.");
                return;
            }

            int overflows = 0;
            int audited = 0;
            int skipped = 0;

            foreach (RectTransform rect in Sweep())
            {
                // A rect that is not on screen has never been laid out, so its size is whatever it was
                // authored with and measuring it says nothing. Audit each tab and each overlay while it is up.
                if (!rect.gameObject.activeInHierarchy)
                {
                    skipped++;
                    continue;
                }

                audited++;
                overflows += ReportOverflow(rect);
            }

            RectTransform frame = Find("Frame");
            string frameSize = frame == null ? "no 'Frame' in the scene" : frame.rect.size.ToString();

            Debug.Log($"{nameof(UiLayoutChecker)}.{nameof(AuditLayout)} [Aspect] screen {Screen.width}x{Screen.height}, " +
                $"Frame.rect.size {frameSize}, {overflows} overflow(s) over {audited} rect(s); {skipped} were not on screen.");
        }

        private static int ReportPositioned(RectTransform rect)
        {
            bool pointAnchor = rect.anchorMin == rect.anchorMax;
            bool atCentre = rect.anchorMin == new Vector2(0.5f, 0.5f);
            bool offset = rect.anchoredPosition != Vector2.zero;

            // An edge or corner anchor with an inset survives any resize, so only the centre is a finding: a
            // rect measured from a point that itself moves when the parent changes size.
            if (!pointAnchor || !atCentre || !offset || LaidOutByParent(rect))
            {
                return 0;
            }

            Debug.LogWarning($"{nameof(UiLayoutChecker)}.{nameof(CheckLayout)} [R1] '{rect.name}' is positioned " +
                $"{rect.anchoredPosition} from the centre of a parent that does not lay it out. " +
                "Anchor it to an edge or put it in a layout group.", rect);
            return 1;
        }

        private static int ReportScaled(RectTransform rect)
        {
            // A canvas's own scale is driven by its scaler, so it is not something anyone typed.
            if (rect.localScale == Vector3.one || rect.GetComponent<Canvas>() != null)
            {
                return 0;
            }

            Debug.LogWarning($"{nameof(UiLayoutChecker)}.{nameof(CheckLayout)} [R2] '{rect.name}' has a scale of " +
                $"{rect.localScale}. Size comes from the rect, not from the scale.", rect);
            return 1;
        }

        private static int ReportOverflow(RectTransform rect)
        {
            RectTransform parent = rect.parent as RectTransform;

            if (parent == null || InsideScrollContent(rect))
            {
                return 0;
            }

            // Measured in the parent's own space, so the number in the message is design units rather than
            // whatever the canvas happens to be scaled by on this screen.
            Rect child = LocalRect(rect, parent);
            Rect holder = parent.rect;

            float left = holder.xMin - child.xMin;
            float right = child.xMax - holder.xMax;
            float below = holder.yMin - child.yMin;
            float above = child.yMax - holder.yMax;

            float worst = Mathf.Max(Mathf.Max(left, right), Mathf.Max(below, above));

            if (worst <= _overflowTolerance)
            {
                return 0;
            }

            string side = worst == left ? "left" : worst == right ? "right" : worst == below ? "bottom" : "top";

            Debug.LogWarning($"{nameof(UiLayoutChecker)}.{nameof(AuditLayout)} [R3] '{rect.name}' overflows " +
                $"'{parent.name}' by {worst:F0} units on the {side}.", rect);
            return 1;
        }

        private static bool LaidOutByParent(RectTransform rect)
        {
            RectTransform parent = rect.parent as RectTransform;

            return parent != null && parent.GetComponent<LayoutGroup>() != null;
        }

        private static bool InsideScrollContent(RectTransform rect)
        {
            for (Transform current = rect.parent; current != null; current = current.parent)
            {
                ScrollRect scroll = current.GetComponent<ScrollRect>();

                if (scroll != null && scroll.viewport != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Rect LocalRect(RectTransform rect, RectTransform space)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Vector3 min = space.InverseTransformPoint(corners[0]);
            Vector3 max = space.InverseTransformPoint(corners[2]);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// Every rect under a canvas root's children — the root itself is skipped, because its scale is the
        /// scaler's to own and R2 would report it on every run.
        /// </summary>
        private static IEnumerable<RectTransform> Sweep()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<RectTransform> swept = new List<RectTransform>();

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].transform.parent != null)
                {
                    continue;
                }

                foreach (RectTransform rect in canvases[i].GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect.transform != canvases[i].transform)
                    {
                        swept.Add(rect);
                    }
                }
            }

            return swept;
        }

        private static RectTransform Find(string name)
        {
            foreach (RectTransform rect in Sweep())
            {
                if (rect.name == name)
                {
                    return rect;
                }
            }

            return null;
        }
    }
}
