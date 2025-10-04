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

            controls.Player.AlternativeUse.started += ctx => StartDrawing();
            controls.Player.AlternativeUse.canceled += ctx => activeConsumer?.OnDrawingFinished();

            controls.Player.Attack.canceled += ctx => OnFireCanceled();
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

            activeConsumer?.OnSymbolRecognized(symbolId.ToString());
        }
    }
}
