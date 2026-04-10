using Common.World.Interaction;
using UnityEngine;

namespace Common.World.SceneTransitions
{
    [AddComponentMenu("Game/World/Scene/Scene Exit")]
    public class SceneExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private string targetSceneName = "Kampus_Akademii";
        [SerializeField] private string targetSpawnId = "Spawn_01";
        [SerializeField] private bool captureStateOnExit = true;
        [SerializeField] private InteractionMode supportedModes = InteractionMode.Both;
        [SerializeField] private InteractionTooltip tooltip;

        public InteractionMode SupportedModes => supportedModes;
        public InteractionTooltip Tooltip => tooltip = InteractionTooltipResolver.Resolve(this, tooltip);

        public void Interact(GameObject player)
        {
            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogWarning("[SceneExit] SceneTransitionManager not found.", this);
                return;
            }

            SceneTransitionManager.Instance.RequestSceneChange(targetSceneName, targetSpawnId, captureStateOnExit);
        }
    }
}
