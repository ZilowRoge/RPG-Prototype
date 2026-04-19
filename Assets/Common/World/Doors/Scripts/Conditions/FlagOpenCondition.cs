using Player.Interfaces;
using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/World/Doors/Conditions/Interaction With Flag")] 
    public class FlagOpenCondition : MonoBehaviour, IDoorOpenCondition
    {
        [Header("Flag Key (uses Id field)")]
        [Tooltip("Progress flag key to check (uses Id). If empty → do not block.")]
        [SerializeField] private string id = string.Empty;
        [Tooltip("If true, flag=true means LOCKED. If false, flag=false means LOCKED.")]
        [SerializeField] private bool lockedWhenFlagIsTrue = true;

        public string Id => id;

        public bool CanOpen(GameObject player)
        {
            if (string.IsNullOrEmpty(id))
                return true; // no flag configured -> do not block

            IProgressReadOnly progress = ResolveProgress(player);
            if (progress == null)
                progress = ResolveProgress(null);

            if (progress == null)
                return true; // cannot evaluate -> fail-open

            bool value = progress.GetFlag(id);
            bool locked = lockedWhenFlagIsTrue ? value : !value;
            return !locked;
        }

        private static IProgressReadOnly ResolveProgress(GameObject player)
        {
            if (player != null)
            {
                var components = player.GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is IProgressReadOnly readOnly)
                        return readOnly;
                }
            }

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IProgressReadOnly readOnly)
                    return readOnly;
            }

            return null;
        }
    }
}

