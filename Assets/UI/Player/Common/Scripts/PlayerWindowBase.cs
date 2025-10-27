using UnityEngine;

namespace UI.Player
{
    public abstract class PlayerWindowBase : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, HideInInspector] private bool startVisible;

        private bool initialized;

        protected virtual void Awake()
        {
            EnsureInitialized();
            ApplyState(startVisible, true);
        }

        protected GameObject WindowRoot => windowRoot;
        protected CanvasGroup WindowCanvasGroup => canvasGroup;

        protected void SetWindowRoot(GameObject root)
        {
            windowRoot = root;
            initialized = false;
        }

        protected void SetCanvasGroup(CanvasGroup group)
        {
            canvasGroup = group;
            initialized = false;
        }

        protected void SetStartVisibility(bool visible) => startVisible = visible;

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            if (windowRoot == null)
                windowRoot = gameObject;

            if (canvasGroup == null && windowRoot != null)
                canvasGroup = windowRoot.GetComponent<CanvasGroup>();

            if (canvasGroup == null && windowRoot != null)
                canvasGroup = windowRoot.AddComponent<CanvasGroup>();

            initialized = true;
        }

        public bool IsVisible
        {
            get
            {
                EnsureInitialized();
                return windowRoot != null && windowRoot.activeSelf && canvasGroup != null && canvasGroup.alpha > 0.001f;
            }
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        public void Show()
        {
            if (IsVisible) return;
            ApplyState(true);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            ApplyState(false);
        }

        private void ApplyState(bool visible, bool skipCallbacks = false)
        {
            EnsureInitialized();
            if (windowRoot == null || canvasGroup == null)
                return;

            if (visible)
                windowRoot.SetActive(true);

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            if (skipCallbacks)
            {
                if (!visible)
                    windowRoot.SetActive(false);
                return;
            }

            if (visible)
            {
                OnShow();
            }
            else
            {
                OnHide();
                windowRoot.SetActive(false);
            }
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }
}
