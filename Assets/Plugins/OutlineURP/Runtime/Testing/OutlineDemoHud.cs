using UnityEngine;

namespace OutlineURP.Testing
{
    public sealed class OutlineDemoHud : MonoBehaviour
    {
        [SerializeField]
        private OutlineDemoInteractor interactor;

        [SerializeField]
        private bool show = true;

        private GUIStyle style;

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = FindFirstObjectByType<OutlineDemoInteractor>();
            }
        }

        private void OnGUI()
        {
            if (!show)
            {
                return;
            }

            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            var hovered = interactor != null && interactor.HoveredTarget != null ? interactor.HoveredTarget.name : "-";
            var selected = interactor != null && interactor.SelectedTarget != null ? interactor.SelectedTarget.name : "-";
            var occlusion = OutlineController.CurrentOcclusionOverride?.ToString() ?? "Profile Default";
            var activeEntries = OutlineRegistry.ActiveEntryCount;
            var targets = FindObjectsByType<OutlineTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var targetCount = targets != null ? targets.Length : 0;
            var frame = Time.frameCount;

            GUILayout.BeginArea(new Rect(12, 12, 560, 600), GUI.skin.box);
            GUILayout.Label("OutlineURP Test Controls (HUD v6)", style);
            GUILayout.Label("Build: 2026-02-21", style);
            GUILayout.Label("LMB: Select target under cursor", style);
            GUILayout.Label("Tab: Cycle enemy target", style);
            GUILayout.Label("X: Toggle XRay/RespectDepth", style);
            GUILayout.Label("Esc: Clear selected target", style);
            GUILayout.Label("C: Clear all states", style);
            GUILayout.Space(6);
            GUILayout.Label($"Hovered: {hovered}", style);
            GUILayout.Label($"Selected: {selected}", style);
            GUILayout.Label($"Targets in scene: {targetCount}", style);
            GUILayout.Label($"Active entries: {activeEntries}", style);
            GUILayout.Label($"Occlusion override: {occlusion}", style);
            GUILayout.Space(6);
            GUILayout.Label($"Frame: {frame}", style);
            GUILayout.Label($"SetupRenderPasses: {OutlineDebugStats.LastSetupRenderPassesFrame}", style);
            GUILayout.Label($"AddRenderPasses: {OutlineDebugStats.LastAddRenderPassesFrame}", style);
            GUILayout.Label($"Mask RG record/exec: {OutlineDebugStats.LastMaskRecordGraphFrame}/{OutlineDebugStats.LastMaskExecuteGraphFrame}", style);
            GUILayout.Label($"Mask Compat exec: {OutlineDebugStats.LastMaskExecuteCompatFrame}", style);
            GUILayout.Label($"Composite RG record/exec: {OutlineDebugStats.LastCompositeRecordGraphFrame}/{OutlineDebugStats.LastCompositeExecuteGraphFrame}", style);
            GUILayout.Label($"Composite Compat exec: {OutlineDebugStats.LastCompositeExecuteCompatFrame}", style);
            GUILayout.EndArea();
        }
    }
}
