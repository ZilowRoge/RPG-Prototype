using UnityEngine;
using Unity.Barracuda;
using Player.UI;

namespace Player.FightSystem.Magic
{
    public class SymbolInputManager : MonoBehaviour
    {
        [SerializeField] private SymbolDrawUI symbolDrawUI;
        [SerializeField] private NNModel modelAsset;

        private PlayerControlls controls;
        private SymbolRecognizer symbolRecognizer;
        public ISymbolConsumer ActiveConsumer { get; set; }

        private void Awake()
        {
            controls = new PlayerControlls();
            symbolRecognizer = new SymbolRecognizer(modelAsset);

            controls.Player.AlternativeUse.started += ctx => StartDrawing();
            controls.Player.AlternativeUse.canceled += ctx => ActiveConsumer?.OnDrawingFinished();

            controls.Player.Attack.canceled += ctx => OnFireCanceled();
        }

        private void OnEnable() => controls.Enable();
        private void OnDisable() => controls.Disable();

        private void StartDrawing()
        {
            symbolDrawUI.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Drawing mode enabled");
        }

        private void OnFireCanceled()
        {
            (int symbolId, float probability) = symbolRecognizer.GetSymbol(symbolDrawUI.GetNormalizedTexture64());
            symbolDrawUI.ClearTexture();

            if (probability < .8f)
            {
                Debug.LogWarning("Symbol not recognized.");
                return;
            }

            ActiveConsumer?.OnSymbolRecognized(symbolId.ToString());
        }
    }
}
