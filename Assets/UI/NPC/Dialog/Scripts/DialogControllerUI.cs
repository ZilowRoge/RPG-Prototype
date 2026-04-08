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
using UnityEngine.InputSystem.Controls;
using UI.Player.Common;

namespace UI.NPC.Dialog
{
    public class DialogControllerUI : MonoBehaviour, IPlayerReferenceReceiver
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text npcText;
        [SerializeField] private VerticalLayoutGroup responsesRoot;
        [SerializeField] private PlayerResponseSlotUI responsePrefab;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ScrollRect scrollRect;
        [Tooltip("Child GameObject to toggle when showing/hiding dialog UI.")]
        [SerializeField] private GameObject dialogRoot;

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

        public bool IsOpen
        {
            get
            {
                var cg = GetEffectiveCanvasGroup();
                bool rootActive = dialogRoot != null && dialogRoot.activeInHierarchy;
                bool cgOpen = cg != null && (cg.blocksRaycasts || cg.alpha > 0.001f);
                return currentDialog != null || rootActive || cgOpen;
            }
        }

        

        public void Begin(DialogAsset dialog)
        {
            if (dialog == null) { return; }
            currentDialog = dialog;
            if (dialogRoot == null && responsesRoot != null)
            {
                dialogRoot = responsesRoot.gameObject;
                
            }
            if (canvasGroup == null && dialogRoot != null)
            {
                canvasGroup = dialogRoot.GetComponent<CanvasGroup>();
                
            }
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

                    if (!allOk && opt.HideIfLocked) { continue; }
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
                var displayText = BuildOptionDisplayText(opt.text, i);
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
                    {
                        
                        Close();
                    }
                    else
                    {
                        
                        ShowNode(nextId);
                    }
                });

                slot.transform.SetAsLastSibling();
                if (isLocked) slot.SetInteractable(false);
                active.Add(slot);
            }

            if (responsesRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(responsesRoot.GetComponent<RectTransform>());
        }

        private string BuildOptionDisplayText(string optionText, int optionIndex)
        {
            string prefix = $"{optionIndex + 1}. ";
            if (string.IsNullOrEmpty(optionText)) return prefix.TrimEnd();

            const string colorTag = "<color";
            if (optionText.StartsWith(colorTag, StringComparison.OrdinalIgnoreCase))
            {
                int closing = optionText.IndexOf('>');
                if (closing >= 0)
                {
                    return optionText.Insert(closing + 1, prefix);
                }
            }

            return prefix + optionText;
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
            var cg = GetEffectiveCanvasGroup();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            // Always ensure dialog root is active if provided
            if (dialogRoot != null && !dialogRoot.activeSelf)
            {
                dialogRoot.SetActive(true);
            }
        }

        private void Hide()
        {
            var cg = GetEffectiveCanvasGroup();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }

        private void HideImmediate()
        {
            var cg = GetEffectiveCanvasGroup();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            if (dialogRoot != null && dialogRoot.activeSelf)
            {
                dialogRoot.SetActive(false);
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
            var canvasGroup = GetEffectiveCanvasGroup();
            bool isOpen = (dialogRoot != null && dialogRoot.activeInHierarchy) ||
                          (canvasGroup != null && canvasGroup.blocksRaycasts) ||
                          currentDialog != null;
            if (!isOpen) return;

            var keyboard = Keyboard.current;

            if (closeOnEscape && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                
                Close();
                return;
            }

            if (keyboard == null) return;
            HandleNumericOptionSelection(keyboard);
        }

        private void HandleNumericOptionSelection(Keyboard keyboard)
        {
            if (active.Count == 0) return;
            int optionIndex = GetPressedHotkeyIndex(keyboard);
            if (optionIndex < 0 || optionIndex >= active.Count) return;

            var slot = active[optionIndex];
            if (slot == null || !slot.IsInteractable) return;

            slot.TriggerSelection();
        }

        private int GetPressedHotkeyIndex(Keyboard keyboard)
        {
            if (keyboard == null) return -1;

            if (WasNumericHotkeyPressed(keyboard.digit1Key, keyboard.numpad1Key)) return 0;
            if (WasNumericHotkeyPressed(keyboard.digit2Key, keyboard.numpad2Key)) return 1;
            if (WasNumericHotkeyPressed(keyboard.digit3Key, keyboard.numpad3Key)) return 2;
            if (WasNumericHotkeyPressed(keyboard.digit4Key, keyboard.numpad4Key)) return 3;
            if (WasNumericHotkeyPressed(keyboard.digit5Key, keyboard.numpad5Key)) return 4;
            if (WasNumericHotkeyPressed(keyboard.digit6Key, keyboard.numpad6Key)) return 5;
            if (WasNumericHotkeyPressed(keyboard.digit7Key, keyboard.numpad7Key)) return 6;
            if (WasNumericHotkeyPressed(keyboard.digit8Key, keyboard.numpad8Key)) return 7;
            if (WasNumericHotkeyPressed(keyboard.digit9Key, keyboard.numpad9Key)) return 8;

            return -1;
        }

        private static bool WasNumericHotkeyPressed(KeyControl primary, KeyControl secondary)
        {
            if (primary != null && primary.wasPressedThisFrame) return true;
            if (secondary != null && secondary.wasPressedThisFrame) return true;
            return false;
        }

        

        public void SetDialogRoot(GameObject root)
        {
            dialogRoot = root;
            if (canvasGroup == null && dialogRoot != null)
                canvasGroup = dialogRoot.GetComponent<CanvasGroup>();
        }

        private CanvasGroup GetEffectiveCanvasGroup()
        {
            if (canvasGroup != null) return canvasGroup;
            if (dialogRoot != null) return dialogRoot.GetComponent<CanvasGroup>();
            return null;
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            progressController = refs.Progress;
            if (progressController == null)
                progressController = FindAnyObjectByType<ProgressController>();
        }
    }
}

