using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NPC.Dialog;
using Player.Progress;
using UnityEngine.InputSystem;

namespace UI.NPC.Dialog
{
    public class DialogControllerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text npcText;
        [SerializeField] private VerticalLayoutGroup responsesRoot;
        [SerializeField] private PlayerResponseSlotUI responsePrefab;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Behavior")]
        [SerializeField] private bool autoSelectFirstOption = true;
        [SerializeField] private bool closeOnEscape = true;

        [Header("Progress")]
        [SerializeField] private ProgressController progressController;

        private readonly List<PlayerResponseSlotUI> pool = new();
        private readonly List<PlayerResponseSlotUI> active = new();

        private DialogAsset currentDialog;
        private string currentNodeId;

        private List<DialogOption> scratchList = new();

        public void Begin(DialogAsset dialog)
        {
            if (dialog == null) return;
            currentDialog = dialog;
            EnsureVisible();
            ShowNode(currentDialog.StartNodeId);
        }

        public void Close()
        {
            ClearUI();
            Hide();
            currentDialog = null;
            currentNodeId = null;
            // Restore gameplay cursor state when dialog closes
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ShowNode(string nodeId)
        {
            if (currentDialog == null)
            {
                Close();
                return;
            }

            currentNodeId = nodeId;
            var node = currentDialog.GetNode(nodeId) as NpcLineNode;
            if (node == null)
            {
                Close();
                return;
            }

            SetNpcLine(node.text);
            var displayOptions = BuildUiOptionsFromNode(node);
            BuildOptions(displayOptions);

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
            if (autoSelectFirstOption) FocusFirstOption();
        }


        private void SetNpcLine(string text)
        {
            if (npcText != null) npcText.text = text ?? string.Empty;
        }

        private void SetResponsesInteractable(bool value)
        {
            foreach (var slot in active)
                if (slot != null) slot.SetInteractable(value);
        }

        private IReadOnlyList<DialogOption> BuildUiOptionsFromNode(NpcLineNode node)
        {
            scratchList.Clear();
            if (node.options != null)
            {
                foreach (var opt in node.options)
                {
                    if (opt == null) continue;
                    bool allOk = true;
                    if (opt.conditions != null && progressController != null)
                    {
                        for (int i = 0; i < opt.conditions.Count; i++)
                        {
                            var c = opt.conditions[i];
                            if (c == null) continue;
                            if (!c.Evaluate(progressController))
                            {
                                allOk = false;
                                break;
                            }
                        }
                    }

                    var uiOpt = new DialogOption
                    {
                        text = allOk ? opt.text : $"<color=#888888>{opt.text}</color>",
                        nextNodeId = opt.nextNodeId,
                        conditions = opt.conditions,
                        onSelect = opt.onSelect,
                        HideIfLocked = opt.HideIfLocked
                    };

                    if (!allOk && opt.HideIfLocked) continue;
                    scratchList.Add(uiOpt);
                }
            }
            return scratchList;
        }

        private void BuildOptions(IReadOnlyList<DialogOption> options)
        {
            ReleaseActiveSlots();
            if (options == null || options.Count == 0) return;

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                var slot = AcquireSlot();
                var displayText = opt.text ?? string.Empty;
                var nextId = opt.nextNodeId;
                var actions = opt.onSelect;
                var isLocked = IsOptionLocked(opt);

                slot.Initialize(displayText, () =>
                {
                    SetResponsesInteractable(false);
                    if (isLocked)
                    {
                        SetResponsesInteractable(true);
                        return;
                    }
                    if (actions != null)
                        foreach (var a in actions) a?.Run(progressController);
                    if (string.IsNullOrEmpty(nextId))
                        Close();
                    else
                        ShowNode(nextId);
                });

                slot.transform.SetAsLastSibling();
                if (isLocked) slot.SetInteractable(false);
                active.Add(slot);
            }

            if (responsesRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(responsesRoot.GetComponent<RectTransform>());
        }

        private bool IsOptionLocked(DialogOption opt)
        {
            if (opt == null) return true;
            if (opt.conditions == null || progressController == null) return false;
            bool allOk = true;
            for (int i = 0; i < opt.conditions.Count; i++)
            {
                var c = opt.conditions[i];
                if (c == null) continue;
                if (!c.Evaluate(progressController)) { allOk = false; break; }
            }
            return !allOk && !opt.HideIfLocked;
        }

        private PlayerResponseSlotUI AcquireSlot()
        {
            PlayerResponseSlotUI slot;
            if (pool.Count > 0)
            {
                int last = pool.Count - 1;
                slot = pool[last];
                pool.RemoveAt(last);
            }
            else
            {
                slot = Instantiate(responsePrefab, responsesRoot.transform);
                slot.gameObject.name = $"{responsePrefab.gameObject.name}_Instantiated";
            }
            slot.SetVisible(true);
            slot.SetInteractable(true);
            return slot;
        }

        private void ReleaseActiveSlots()
        {
            for (int i = 0; i < active.Count; i++)
            {
                var slot = active[i];
                if (slot != null)
                {
                    slot.ResetSlot();
                    if (!pool.Contains(slot)) pool.Add(slot);
                }
            }
            active.Clear();
        }

        private void EnsureVisible()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else gameObject.SetActive(true);
        }

        private void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            // Ensure the GameObject is fully deactivated so external toggles
            // see it as closed (prevents needing a double press to reopen).
            gameObject.SetActive(false);
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void FocusFirstOption()
        {
            if (active.Count == 0) return;
            var first = active[0];
            if (first == null) return;
            var btn = first.GetComponent<Button>();
            if (btn != null)
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(btn.gameObject);
                btn.Select();
            }
        }

        private void ClearUI()
        {
            SetNpcLine(string.Empty);
            ReleaseActiveSlots();
        }

        private void Update()
        {
            if (!closeOnEscape) return;
            if (!gameObject.activeInHierarchy) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }
    }
}
