using System;
using System.Collections.Generic;
using System.Text;
using NextStepGuide.Rules;
using NextStepGuide.State;
using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NextStepGuide.UI
{
    /// <summary>
    /// The on-screen panel (Phase 3). A single self-sizing card pinned to the
    /// left edge of the HUD: one rich-text block holding a header (cycle + count)
    /// over the current recommendations. Click anywhere on it to collapse/expand.
    ///
    /// Built from raw Unity UI (Image + VerticalLayoutGroup + ContentSizeFitter +
    /// one TextMeshProUGUI) rather than PLib's panel layout, which is meant for
    /// nesting in dialogs and doesn't self-size standalone. Using a single text
    /// element makes overlap impossible and sizing trivial. The font is borrowed
    /// from PLib so it still looks native.
    ///
    /// It lives on its own nested Canvas (+GraphicRaycaster) so it draws above and
    /// receives clicks independently of the native HUD. Everything is fail-soft.
    /// ASCII-only text: ONI's font lacks glyphs like ▾/● (they render as tofu).
    /// </summary>
    public sealed class GuidePanel
    {
        public static GuidePanel Instance { get; private set; }

        private const int WrapWidth = 50;        // chars per body line (manual wrap)
        private const int CanvasSortOrder = 100; // above the HUD, below tooltips

        private GameObject _root;
        private TextMeshProUGUI _text;
        private bool _collapsed;

        // Urgency-band colours (TMP hex, no '#').
        private const string CrisisHex = "E0533D";   // red
        private const string PressingHex = "E0A23D"; // amber
        private const string ProgressHex = "4F8FF0"; // blue
        private const string PolishHex = "9AA0A6";   // grey
        private const string WhyHex = "B8BCC2";      // muted body text

        public static void EnsureCreated()
        {
            if (Instance != null && Instance._root != null) return; // destroyed → rebuild

            GameObject parent = ResolveParent();
            if (parent == null) return; // HUD canvas not ready yet; retry next tick

            var panel = new GuidePanel();
            if (panel.Build(parent)) Instance = panel;
        }

        private static GameObject ResolveParent()
        {
            try
            {
                return GameScreenManager.Instance != null
                    ? GameScreenManager.Instance.ssOverlayCanvas
                    : null;
            }
            catch { return null; }
        }

        private bool Build(GameObject parent)
        {
            try
            {
                var root = new GameObject("NextStepGuide");
                root.transform.SetParent(parent.transform, false);

                var rt = root.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(24f, 0f);

                // Own canvas so we sit above the native HUD and get our own clicks.
                var canvas = root.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = CanvasSortOrder;
                root.AddComponent<GraphicRaycaster>();

                var bg = root.AddComponent<Image>();
                bg.color = new Color(0.07f, 0.09f, 0.12f, 0.92f);
                bg.raycastTarget = true; // block clicks to the game behind the card

                // Auto-size the card to its text.
                var vlg = root.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(12, 12, 9, 9);
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;

                var fitter = root.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Single text element (raw TMP, native font from PLib).
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(root.transform, false);
                textGO.AddComponent<RectTransform>();
                _text = textGO.AddComponent<TextMeshProUGUI>();
                ApplyFont(_text);
                _text.raycastTarget = true; // receives toggle/dismiss/reset link clicks
                _text.alignment = TextAlignmentOptions.TopLeft;
                _text.textWrappingMode = TextWrappingModes.NoWrap; // we wrap manually
                _text.richText = true;
                _text.text = "Reading colony...";

                var clicks = textGO.AddComponent<GuidePanelClicks>();
                clicks.Owner = this;
                clicks.Text = _text;

                _root = root;
                Render(GuideRuntime.LastSnapshot, GuideRuntime.Latest);

                Debug.Log($"{ModEntry.Prefix} guide panel created.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} panel build failed (UI disabled this session): {e}");
                return false;
            }
        }

        private static void ApplyFont(TextMeshProUGUI tmp)
        {
            try
            {
                var style = PUITuning.Fonts.UILightStyle;
                if (style != null)
                {
                    if (style.sdfFont != null) tmp.font = style.sdfFont;
                    tmp.fontSize = style.fontSize > 0 ? style.fontSize : 14f;
                }
                else
                {
                    tmp.fontSize = 14f;
                }
                tmp.color = Color.white;
            }
            catch { tmp.fontSize = 14f; }
        }

        /// <summary>Update the panel from the latest evaluation. Cheap; no game reads.</summary>
        public void Render(ColonySnapshot snapshot, IReadOnlyList<Recommendation> recs)
        {
            try
            {
                if (_text == null) return;
                recs = recs ?? Array.Empty<Recommendation>();
                bool showWhy = GuideRuntime.Settings == null || GuideRuntime.Settings.ShowWhy;

                string cycle = (snapshot != null && snapshot.CycleKnown) ? snapshot.Cycle.ToString() : "?";
                string toggle = _collapsed ? "[+]" : "[-]";
                string tips = recs.Count == 1 ? "1 tip" : recs.Count + " tips";

                var sb = new StringBuilder();
                // Header: the toggle box + title is a clickable collapse link.
                sb.Append("<link=\"toggle\"><b>").Append(toggle).Append(" Next Step Guide</b></link>")
                  .Append("  <color=#").Append(PolishHex).Append(">(Cycle ").Append(cycle)
                  .Append(", ").Append(tips).Append(")</color>");

                if (!_collapsed)
                {
                    if (recs.Count == 0)
                    {
                        sb.Append("\n\n<color=#").Append(PolishHex)
                          .Append(">No suggestions right now - nice work.</color>");
                    }
                    else
                    {
                        for (int i = 0; i < recs.Count; i++)
                        {
                            var r = recs[i];
                            sb.Append("\n\n<color=#").Append(BandHex(r.Band)).Append(">*</color> <b>")
                              .Append(WrapIndented(r.Title, "  ")).Append("</b>")
                              // per-tip dismiss link
                              .Append(" <link=\"d:").Append(r.Id).Append("\"><color=#")
                              .Append(PolishHex).Append(">[x]</color></link>");
                            if (showWhy)
                            {
                                sb.Append("\n<size=85%><color=#").Append(WhyHex).Append('>')
                                  .Append(WrapIndented(r.Why, "  ")).Append("</color></size>");
                            }
                        }
                    }

                    int dismissedCount = GuideRuntime.Settings?.DismissedIds?.Count ?? 0;
                    if (dismissedCount > 0)
                    {
                        sb.Append("\n\n<size=80%><link=\"reset\"><color=#").Append(PolishHex)
                          .Append(">[reset ").Append(dismissedCount).Append(" dismissed]</color></link></size>");
                    }
                }

                _text.text = sb.ToString();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} panel render failed: {e}");
            }
        }

        /// <summary>Dispatch a clicked TMP link id (toggle / dismiss / reset).</summary>
        internal void OnLinkClicked(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (id == "toggle") { ToggleCollapse(); return; }
            if (id == "reset") GuideRuntime.ResetDismissed();
            else if (id.StartsWith("d:", StringComparison.Ordinal)) GuideRuntime.Dismiss(id.Substring(2));
            Render(GuideRuntime.LastSnapshot, GuideRuntime.Latest);
        }

        /// <summary>Word-wrap to <see cref="WrapWidth"/> and indent continuation lines.</summary>
        private static string WrapIndented(string text, string indent)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder();
            int lineLen = 0;
            foreach (var word in text.Split(' '))
            {
                if (word.Length == 0) continue;
                if (lineLen > 0 && lineLen + 1 + word.Length > WrapWidth)
                {
                    sb.Append('\n').Append(indent);
                    lineLen = indent.Length;
                }
                else if (lineLen > 0)
                {
                    sb.Append(' ');
                    lineLen++;
                }
                sb.Append(word);
                lineLen += word.Length;
            }
            return sb.ToString();
        }

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            Render(GuideRuntime.LastSnapshot, GuideRuntime.Latest);
        }

        private static string BandHex(UrgencyBand band)
        {
            switch (band)
            {
                case UrgencyBand.Crisis: return CrisisHex;
                case UrgencyBand.Pressing: return PressingHex;
                case UrgencyBand.Progress: return ProgressHex;
                default: return PolishHex;
            }
        }
    }

    /// <summary>
    /// Routes clicks on the panel text to the TMP link under the cursor
    /// (toggle / dismiss / reset). Lives on the text GameObject.
    /// </summary>
    internal sealed class GuidePanelClicks : MonoBehaviour, IPointerClickHandler
    {
        public GuidePanel Owner;
        public TextMeshProUGUI Text;

        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                if (Text == null || Owner == null) return;
                // null camera = screen-space-overlay canvas.
                int idx = TMP_TextUtilities.FindIntersectingLink(Text, eventData.position, null);
                if (idx < 0) return;
                Owner.OnLinkClicked(Text.textInfo.linkInfo[idx].GetLinkID());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} panel click failed: {e}");
            }
        }
    }
}
