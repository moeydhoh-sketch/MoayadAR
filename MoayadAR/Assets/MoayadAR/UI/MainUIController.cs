using System;
using MoayadAR.Analysis;
using MoayadAR.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoayadAR.UI
{
    /// <summary>
    /// UI Toolkit shell: bottom action bar (Import / Capture / Realism / Projects), contextual
    /// editing panel, analysis card, RTL mirroring for Arabic. All strings resolve through
    /// LocalizationService — nothing user-facing is hardcoded (master prompt §15).
    /// EDITOR-PENDING (requires Unity UI Toolkit runtime).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainUIController : MonoBehaviour
    {
        public event Action ImportRequested;
        public event Action CaptureTapped;
        public event Action<RealismLevelStub> RealismRequested;

        public enum RealismLevelStub { Battery, Balanced, High, Ultra }

        private LocalizationService _loc;
        private VisualElement _root;

        private void Awake()
        {
            _loc = new LocalizationService(); // tables loaded by Bootstrapper from TextAssets
            _root = GetComponent<UIDocument>().rootVisualElement;
        }

        public void Initialize(LocalizationService loc)
        {
            _loc = loc;
            ApplyLanguage();
        }

        public void ApplyLanguage()
        {
            if (_root == null || _loc == null) return;
            _root.EnableInClassList("rtl", _loc.IsRtl);
            SetText("btn-import", _loc.Get("action.importModel"));
            SetText("btn-capture", _loc.Get("action.capture"));
            SetText("btn-realism", _loc.Get("action.realism"));
            SetText("btn-projects", _loc.Get("action.projects"));
            SetText("btn-settings", _loc.Get("action.settings"));

            Bind("btn-import", () => ImportRequested?.Invoke());
            Bind("btn-capture", () => CaptureTapped?.Invoke());
        }

        /// <summary>Analysis card: rig honesty is enforced here — OBJ never shows rig controls.</summary>
        public void ShowAnalysis(ModelReport report)
        {
            var card = _root?.Q<VisualElement>("analysis-card");
            if (card == null || report == null) return;
            card.style.display = DisplayStyle.Flex;

            SetText("analysis-file", report.FileName);
            SetText("analysis-format", report.Format.ToString());
            SetText("analysis-tris", report.TriangleCount.ToString("N0"));

            var rigBadge = card.Q<Label>("analysis-rig");
            var rigButton = card.Q<Button>("btn-rig-edit");
            if (report.RigDetected)
            {
                rigBadge.text = _loc.Get("rig.badge");
                rigButton.text = _loc.Get("action.rigEdit");
                rigButton.SetEnabled(true);
            }
            else
            {
                rigBadge.text = report.Format == Core.ModelFormat.Obj
                    ? _loc.Get("error.noRigObj") : _loc.Get("error.noRig");
                rigButton.text = _loc.Get("action.rigEdit");
                rigButton.SetEnabled(false); // disabled with explanation — never a dead fake button
            }
        }

        public void ShowImportProgress(string phaseLocalizedLabel, float fraction01)
        {
            SetText("import-phase", phaseLocalizedLabel);
            var bar = _root?.Q<ProgressBar>("import-bar");
            if (bar != null && fraction01 >= 0f) bar.value = fraction01 * 100f;
        }

        private void SetText(string name, string text)
        {
            var el = _root?.Q<Label>(name);
            if (el != null) el.text = text;
        }

        private void Bind(string name, Action onClick)
        {
            var el = _root?.Q<Button>(name);
            if (el != null) el.clicked += onClick;
        }
    }
}
