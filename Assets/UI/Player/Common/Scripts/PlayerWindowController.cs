using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Player;

namespace UI.Player
{
    public class PlayerWindowController : MonoBehaviour
    {
        [Serializable]
        private class WindowTab
        {
            public string id;
            public PlayerWindowBase window;
            public Button tabButton;
        }

        [Header("Root")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private GameObject windowRootObject;
        [SerializeField] private bool startHidden = true;
        [SerializeField] private bool manageCursor = true;

        [Header("Tabs")]
        [SerializeField] private List<WindowTab> tabs = new();
        [SerializeField] private int defaultTabIndex;
        [SerializeField] private Key toggleKey = Key.Tab;
        [SerializeField] private bool closeOnEscape = true;

        private WindowTab currentTab;
        private bool cursorCaptured;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;

        private void Awake()
        {
            EnsureRootCanvas();
            SetRootVisible(!startHidden);

            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab.window != null)
                {
                    var windowObject = tab.window.gameObject;
                    bool wasActive = windowObject.activeSelf;
                    if (!wasActive)
                        windowObject.SetActive(true);

                    tab.window.Hide();

                    if (!wasActive)
                        windowObject.SetActive(false);
                }

                if (tab.tabButton != null)
                {
                    int index = i;
                    tab.tabButton.onClick.AddListener(() => ShowTab(index));
                }
            }

            currentTab = null;

            if (!startHidden)
                ShowDefaultTab();
            else
                SetRootVisible(false);
        }

        private void OnDisable()
        {
            ReleaseCursor();
            PlayerInputLockService.Instance?.SetLock(this, false);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (toggleKey != Key.None && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                if (IsAnyWindowVisible())
                    HideAll();
                else
                    ShowDefaultTab();
            }

            if (closeOnEscape && IsAnyWindowVisible() && Keyboard.current.escapeKey.wasPressedThisFrame)
                HideAll();
        }

        public void ShowTab(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < tabs.Count; i++)
            {
                if (string.Equals(tabs[i].id, id, StringComparison.OrdinalIgnoreCase))
                {
                    ShowTab(i);
                    return;
                }
            }
        }

        public void ShowTab(int index)
        {
            if (index < 0 || index >= tabs.Count)
                return;

            var target = tabs[index];
            if (target.window == null)
                return;

            SetRootVisible(true);

            var targetObject = target.window.gameObject;
            if (!targetObject.activeSelf)
                targetObject.SetActive(true);

            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab.window == null || i == index) continue;

                if (tab.window.gameObject.activeSelf)
                    tab.window.Hide();

                tab.window.gameObject.SetActive(false);
            }

            target.window.Show();
            currentTab = target;
        }

        public void HideAll()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                var window = tabs[i].window;
                if (window != null)
                {
                    if (window.gameObject.activeSelf)
                        window.Hide();
                    window.gameObject.SetActive(false);
                }
            }

            currentTab = null;
            SetRootVisible(false);
        }

        public void ToggleUI()
        {
            if (IsAnyWindowVisible())
                HideAll();
            else
                ShowDefaultTab();
        }

        public void CloseUI() => HideAll();

        private void ShowDefaultTab()
        {
            if (tabs.Count == 0)
                return;

            SetRootVisible(true);

            int index = Mathf.Clamp(defaultTabIndex, 0, tabs.Count - 1);
            ShowTab(index);
        }

        private bool IsAnyWindowVisible()
        {
            if (RootVisible())
                return true;

            for (int i = 0; i < tabs.Count; i++)
            {
                var window = tabs[i].window;
                if (window != null && window.IsVisible)
                    return true;
            }

            return false;
        }

        private void EnsureRootCanvas()
        {
            if (rootCanvasGroup != null)
                return;

            rootCanvasGroup = GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (windowRootObject == null)
                windowRootObject = rootCanvasGroup.gameObject;
        }

        private void SetRootVisible(bool visible)
        {
            EnsureRootCanvas();

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;

            if (windowRootObject != null)
            {
                if (visible)
                {
                    if (!windowRootObject.activeSelf)
                        windowRootObject.SetActive(true);
                }
                else
                {
                    windowRootObject.SetActive(false);
                }
            }

            if (manageCursor)
            {
                if (visible)
                    CaptureCursor();
                else
                    ReleaseCursor();
            }

            PlayerInputLockService.Instance?.SetLock(this, visible);
        }

        private bool RootVisible()
        {
            EnsureRootCanvas();
            return windowRootObject != null && windowRootObject.activeSelf && rootCanvasGroup.alpha > 0.001f;
        }

        private void CaptureCursor()
        {
            if (cursorCaptured)
                return;

            cursorCaptured = true;
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ReleaseCursor()
        {
            if (!cursorCaptured)
                return;

            cursorCaptured = false;
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
    }
}








