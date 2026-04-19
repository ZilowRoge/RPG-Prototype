using System;
using System.Reflection;
using UnityEngine;

namespace Common.UI
{
    public interface IPlayerInputLockService
    {
        void SetLock(object source, bool locked);
        void ClearAllLocks();
    }

    public static class PlayerInputLocks
    {
        private static IPlayerInputLockService cachedService;
        private static MethodInfo cachedTryGetInstance;

        public static IPlayerInputLockService TryGetService()
        {
            if (cachedService is UnityEngine.Object unityObject && unityObject == null)
                cachedService = null;

            if (cachedService != null)
                return cachedService;

            var service = TryResolveViaReflection() ?? FindInScene();
            cachedService = service;
            return service;
        }

        public static void SetLock(object source, bool locked)
        {
            TryGetService()?.SetLock(source, locked);
        }

        public static void ClearAllLocks()
        {
            TryGetService()?.ClearAllLocks();
        }

        private static IPlayerInputLockService TryResolveViaReflection()
        {
            if (cachedTryGetInstance == null)
            {
                var playerLockType = FindType("Player.PlayerInputLockService");
                cachedTryGetInstance = playerLockType?.GetMethod(
                    "TryGetInstance",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);
            }

            if (cachedTryGetInstance == null)
                return null;

            return cachedTryGetInstance.Invoke(null, null) as IPlayerInputLockService;
        }

        private static IPlayerInputLockService FindInScene()
        {
            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPlayerInputLockService service)
                    return service;
            }

            return null;
        }

        private static Type FindType(string fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
