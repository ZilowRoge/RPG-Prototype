using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace NPC.Dialog {
[RequireComponent(typeof(Button))]
public class PlayerResponseSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;

    private Action onSelected;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (button != null) button.onClick.RemoveAllListeners();
    }

    public void Initialize(string text, Action onClick)
    {
        onSelected = onClick;
        if (label != null) label.text = text ?? string.Empty;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(InvokeSelection);
        }
        gameObject.SetActive(true);
    }

    public void SetInteractable(bool value)
    {
        if (button != null) button.interactable = value;
    }

    public void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }

    public void ResetSlot()
    {
        onSelected = null;
        if (label != null) label.text = string.Empty;
        if (button != null) button.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    private void InvokeSelection()
    {
        onSelected?.Invoke();
    }
}
}