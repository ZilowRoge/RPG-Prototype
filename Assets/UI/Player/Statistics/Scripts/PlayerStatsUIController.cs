using UnityEngine;
using UnityEngine.InputSystem;
namespace UI.Player.Statistics {
public class PlayerStatsUIController : MonoBehaviour
{
    [SerializeField] private GameObject statsPanel;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            statsPanel.SetActive(!statsPanel.activeSelf);
        }
    }

}
}