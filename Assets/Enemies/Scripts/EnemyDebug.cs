using UnityEngine;

namespace Enemies
{
    internal static class EnemyDebug
    {
        [System.Diagnostics.Conditional("ENEMY_DEBUG_LOGS")]
        public static void Log(string message, Object context = null)
        {
            Debug.Log(message, context);
        }

        [System.Diagnostics.Conditional("ENEMY_DEBUG_WARNINGS")]
        public static void LogWarning(string message, Object context = null)
        {
            Debug.LogWarning(message, context);
        }

        [System.Diagnostics.Conditional("ENEMY_DEBUG_ERRORS")]
        public static void LogError(string message, Object context = null)
        {
            Debug.LogError(message, context);
        }
    }
}
