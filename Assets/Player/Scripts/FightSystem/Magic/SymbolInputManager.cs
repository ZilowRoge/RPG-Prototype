using UnityEngine;
using Unity.Barracuda;
using Player.UI;

namespace Player.FightSystem.Magic
{
    public class SymbolInputManager : MonoBehaviour
    {
        [SerializeField] private SymbolDrawUI symbolDrawUI;
        [SerializeField] private NNModel modelAsset;
        [SerializeField] private MonoBehaviour defaultCombatConsumerBehaviour;

        private PlayerControlls controls;
        private SymbolRecognizer symbolRecognizer;
        private ISymbolConsumer activeConsumer;
        private ISymbolConsumer defaultCombatConsumer;
        private bool isDrawing;

        public ISymbolConsumer ActiveConsumer => activeConsumer;
        public ISymbolConsumer DefaultCombatConsumer => defaultCombatConsumer;

        private void Awake()
        {
            controls = new PlayerControlls();
            symbolRecognizer = new SymbolRecognizer(modelAsset);

            if (defaultCombatConsumerBehaviour != null)
            {
                defaultCombatConsumer = defaultCombatConsumerBehaviour as ISymbolConsumer;
                if (defaultCombatConsumer == null)
                {
                    Debug.LogError($"[SymbolInputManager] Default combat consumer does not implement ISymbolConsumer: {defaultCombatConsumerBehaviour.GetType().Name}", this);
                }
            }

            if (defaultCombatConsumer != null)
                activeConsumer = defaultCombatConsumer;

            controls.Player.AlternativeUse.started += _ => StartDrawing();
            controls.Player.AlternativeUse.canceled += _ => FinishDrawing();

            controls.Player.Attack.canceled += _ => OnFireCanceled();
        }

        public ISymbolConsumer SetActiveConsumer(ISymbolConsumer consumer)
        {
            var previous = activeConsumer;
            activeConsumer = consumer;
            return previous;
        }

        public void ResetToDefaultConsumer()
        {
            activeConsumer = defaultCombatConsumer;
        }

        private void OnEnable() => controls.Enable();

        private void OnDisable()
        {
            FinishDrawing(notifyConsumer: false);
            controls?.Disable();
            symbolRecognizer?.Clear();
        }

        private void StartDrawing()
        {
            if (isDrawing)
                return;

            if (symbolDrawUI == null)
            {
                Debug.LogWarning("[SymbolInputManager] SymbolDrawUI is not assigned.", this);
                return;
            }
            Debug.Log("Start drawing");
            isDrawing = true;
            symbolDrawUI.gameObject.SetActive(true);
            symbolDrawUI.ClearTexture();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Drawing mode enabled");
        }

        private void FinishDrawing(bool notifyConsumer = true)
        {
            if (!isDrawing)
                return;

            isDrawing = false;

            if (symbolDrawUI != null)
            {
                symbolDrawUI.gameObject.SetActive(false);
                symbolDrawUI.ClearTexture();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (notifyConsumer)
                activeConsumer?.OnDrawingFinished();
        }

        private void OnFireCanceled()
        {
            if (!isDrawing)
                return;

            if (symbolDrawUI == null || symbolRecognizer == null)
                return;

            (int symbolId, float probability) = symbolRecognizer.GetSymbol(symbolDrawUI.GetNormalizedTexture64());
            symbolDrawUI.ClearTexture();

            if (probability < .8f)
            {
                Debug.LogWarning("Symbol not recognized.");
                return;
            }

            activeConsumer?.OnSymbolRecognized(symbolId.ToString());
        }
    }
}

