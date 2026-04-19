using System;

namespace Common.Runtime
{
    public static class SaveRuntimeState
    {
        public static event Action PlayerLoadedFromSave;

        public static bool IsRestoring { get; private set; }

        public static bool SuppressNextTransformRestore { get; set; }

        public static void BeginRestore()
        {
            IsRestoring = true;
        }

        public static void EndRestore()
        {
            IsRestoring = false;
        }

        public static void NotifyPlayerLoadedFromSave()
        {
            PlayerLoadedFromSave?.Invoke();
        }
    }
}
