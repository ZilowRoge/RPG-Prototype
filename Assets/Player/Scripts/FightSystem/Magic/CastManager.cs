using UnityEngine;
using UnityEngine.InputSystem;
using Player.UI;

namespace Player.FightSystem.Magic {
    public class CastManager : MonoBehaviour {
        public SymbolDrawUI symbolDrawUI;

        private PlayerControlls controls;
        private bool isDrawing = false;
        private Vector2 currentMousePosition;

        private void Awake() {
            controls = new PlayerControlls();
            symbolDrawUI.gameObject.SetActive(false);
            // LPM start
            controls.Player.AlternativeUse.started += ctx => {
                StartDrawing();
            };

            controls.Player.AlternativeUse.canceled += ctx => {
                FinalizeDrawing();
            };
        }

        private void OnEnable() => controls.Enable();
        private void OnDisable() => controls.Disable();

        private void StartDrawing() {
            symbolDrawUI.gameObject.SetActive(true);
            isDrawing = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void FinalizeDrawing() {
            symbolDrawUI.gameObject.SetActive(false);
            isDrawing = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            symbolDrawUI.ClearTexture();
        }
    }
}
