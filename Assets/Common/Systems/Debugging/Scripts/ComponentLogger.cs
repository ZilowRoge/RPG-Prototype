using System;
using UnityEngine;

namespace Systems.Debugging
{
    /// <summary>
    /// Lightweight, flag-based logger that can be embedded into any MonoBehaviour.
    /// </summary>
    [Serializable]
    public sealed class ComponentLogger
    {
        [Flags]
        public enum LogFlag
        {
            None = 0,
            General = 1 << 0,
            Lifecycle = 1 << 1,
            Events = 1 << 2,
            StateChange = 1 << 3,
            Interaction = 1 << 4,
            Combat = 1 << 5,
            Inventory = 1 << 6,
            UI = 1 << 7,
            AI = 1 << 8,
            Audio = 1 << 9,
            Custom1 = 1 << 10,
            Custom2 = 1 << 11,
            Custom3 = 1 << 12,
            All = ~0
        }

        [SerializeField] private bool loggingEnabled = true;
        [SerializeField] private LogFlag enabledFlags = LogFlag.All;
        [SerializeField] private bool alwaysLogWarnings = true;
        [SerializeField] private bool alwaysLogErrors = true;
        [SerializeField] [Tooltip("Optional prefix override displayed before each log entry.")]
        private string prefixOverride = string.Empty;

        [NonSerialized] private UnityEngine.Object boundContext;

        public bool LoggingEnabled
        {
            get => loggingEnabled;
            set => loggingEnabled = value;
        }

        public LogFlag EnabledFlags => enabledFlags;

        public void BindContext(UnityEngine.Object context)
        {
            boundContext = context;
        }

        public void EnableFlag(LogFlag flag)
        {
            enabledFlags |= flag;
        }

        public void DisableFlag(LogFlag flag)
        {
            enabledFlags &= ~flag;
        }

        public void ToggleFlag(LogFlag flag)
        {
            enabledFlags ^= flag;
        }

        public void SetFlags(LogFlag flags)
        {
            enabledFlags = flags;
        }

        public bool IsFlagEnabled(LogFlag flag)
        {
            return loggingEnabled && HasFlag(flag);
        }

        public void Log(LogFlag flag, string message, params object[] args)
        {
            Write(LogType.Log, boundContext, flag, message, args);
        }

        public void Log(UnityEngine.Object context, LogFlag flag, string message, params object[] args)
        {
            Write(LogType.Log, context, flag, message, args);
        }

        public void LogWarning(LogFlag flag, string message, params object[] args)
        {
            Write(LogType.Warning, boundContext, flag, message, args);
        }

        public void LogWarning(UnityEngine.Object context, LogFlag flag, string message, params object[] args)
        {
            Write(LogType.Warning, context, flag, message, args);
        }

        public void LogError(LogFlag flag, string message, params object[] args)
        {
            Write(LogType.Error, boundContext, flag, message, args);
        }

        public void LogError(UnityEngine.Object context, LogFlag flag, string message, params object[] args)
        {
            Write(LogType.Error, context, flag, message, args);
        }

        public void LogException(Exception exception, LogFlag flag = LogFlag.General, string message = null)
        {
            LogException(boundContext, exception, flag, message);
        }

        public void LogException(
            UnityEngine.Object context,
            Exception exception,
            LogFlag flag = LogFlag.General,
            string message = null)
        {
            if (!ShouldLog(LogType.Exception, flag) || exception == null)
                return;

            var resolvedContext = context ? context : boundContext;

            if (resolvedContext)
                Debug.LogException(exception, resolvedContext);
            else
                Debug.LogException(exception);

            if (string.IsNullOrEmpty(message))
                return;

            var formatted = FormatMessage(resolvedContext, flag, message, null);

            if (resolvedContext)
                Debug.Log(formatted, resolvedContext);
            else
                Debug.Log(formatted);
        }

        private void Write(
            LogType logType,
            UnityEngine.Object context,
            LogFlag flag,
            string message,
            object[] args)
        {
            if (!ShouldLog(logType, flag))
                return;

            var resolvedContext = context ? context : boundContext;
            var formatted = FormatMessage(resolvedContext, flag, message, args);

            switch (logType)
            {
                case LogType.Warning:
                    if (resolvedContext)
                        Debug.LogWarning(formatted, resolvedContext);
                    else
                        Debug.LogWarning(formatted);
                    break;
                case LogType.Error:
                    if (resolvedContext)
                        Debug.LogError(formatted, resolvedContext);
                    else
                        Debug.LogError(formatted);
                    break;
                default:
                    if (resolvedContext)
                        Debug.Log(formatted, resolvedContext);
                    else
                        Debug.Log(formatted);
                    break;
            }
        }

        private bool ShouldLog(LogType logType, LogFlag flag)
        {
            if (!loggingEnabled)
                return false;

            return logType switch
            {
                LogType.Error or LogType.Exception => alwaysLogErrors || HasFlag(flag),
                LogType.Warning => alwaysLogWarnings || HasFlag(flag),
                _ => HasFlag(flag)
            };
        }

        private bool HasFlag(LogFlag flag)
        {
            if (enabledFlags == LogFlag.All)
                return true;

            if (flag == LogFlag.None)
                return (enabledFlags & LogFlag.General) != 0;

            return (enabledFlags & flag) != 0;
        }

        private string FormatMessage(
            UnityEngine.Object context,
            LogFlag flag,
            string message,
            object[] args)
        {
            var prefix = ResolvePrefix(context);
            var label = flag == LogFlag.None ? LogFlag.General.ToString() : flag.ToString();
            var body = SafeFormat(message, args);

            if (string.IsNullOrEmpty(prefix))
                return $"[{label}] {body}";

            return $"[{prefix}][{label}] {body}";
        }

        private string SafeFormat(string message, object[] args)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            if (args == null || args.Length == 0)
                return message;

            try
            {
                return string.Format(message, args);
            }
            catch (FormatException)
            {
                return message;
            }
        }

        private string ResolvePrefix(UnityEngine.Object context)
        {
            if (!string.IsNullOrEmpty(prefixOverride))
                return prefixOverride;

            if (!context)
                return string.Empty;

            if (context is Component component)
                return $"{component.gameObject.name}/{component.GetType().Name}";

            return context.name;
        }
    }
}
