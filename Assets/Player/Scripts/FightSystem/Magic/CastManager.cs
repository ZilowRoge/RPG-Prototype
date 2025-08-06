using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Barracuda;
using Player.UI;

namespace Player.FightSystem.Magic {
    public class CastManager : MonoBehaviour {
        [SerializeField]
        private SymbolDrawUI symbolDrawUI;
        [SerializeField]
        private NNModel modelAsset;

        private PlayerControlls controls;
        private Vector2 currentMousePosition;
        private SymbolRecognizer symbolRecognizer;

        private void Awake() {
            controls = new PlayerControlls();
            symbolRecognizer = new SymbolRecognizer(modelAsset);
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void FinalizeDrawing() {
            symbolDrawUI.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            (int symbolId, float probability) = symbolRecognizer.GetSymbol(symbolDrawUI.GetNormalizedTexture64());
            Debug.Log($"Predicted symbol: {symbolId} with probability {probability}");
            symbolDrawUI.ClearTexture();
        }
    }
}
