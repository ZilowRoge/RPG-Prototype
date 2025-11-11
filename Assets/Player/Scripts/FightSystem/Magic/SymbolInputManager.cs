using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Barracuda;
using Player.UI;
using Player.Cameras;
using Systems.Debugging;

namespace Player.FightSystem.Magic
{
    public class SymbolInputManager : MonoBehaviour
    {
        [SerializeField] private SymbolDrawUI symbolDrawUI;
        [SerializeField] private NNModel modelAsset;
        [SerializeField] private MonoBehaviour defaultCombatConsumerBehaviour;
        [SerializeField, Tooltip("Time window (seconds) to allow chaining symbols before finalizing the sequence.")]
        private float continuationWindow = 0.75f;
        [SerializeField, Tooltip("Optional camera sensitivity controller to dampen look speed while drawing symbols.")]
        private CinemachineSensitivityController cameraSensitivityController;
        [SerializeField] private ComponentLogger logger = new ComponentLogger();

        private PlayerControlls controls;
        private SymbolRecognizer symbolRecognizer;
        private ISymbolConsumer activeConsumer;
        private ISymbolConsumer defaultCombatConsumer;
        private bool isDrawing;
        private Coroutine pendingFinishRoutine;

        public ISymbolConsumer ActiveConsumer => activeConsumer;
        public ISymbolConsumer DefaultCombatConsumer => defaultCombatConsumer;

        private void Awake()
        {
            InitializeLogger();
            controls = new PlayerControlls();
            symbolRecognizer = new SymbolRecognizer(modelAsset);
            if (cameraSensitivityController == null)
                cameraSensitivityController = FindFirstObjectByType<CinemachineSensitivityController>();

            if (defaultCombatConsumerBehaviour != null)
            {
                defaultCombatConsumer = defaultCombatConsumerBehaviour as ISymbolConsumer;
                if (defaultCombatConsumer == null)
                {
                    logger.LogWarning(ComponentLogger.LogFlag.Events,
                        "Default combat consumer does not implement ISymbolConsumer: {0}",
                        defaultCombatConsumerBehaviour.GetType().Name);
                }
            }

            if (defaultCombatConsumer != null)
                activeConsumer = defaultCombatConsumer;

            controls.Player.AlternativeUse.started += _ => StartDrawing();
            controls.Player.AlternativeUse.canceled += _ => FinishDrawing();

            controls.Player.Attack.started += _ => OnFireStarted();
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

        private void Update()
        {
            // ESC cancels current symbol flow (doors/lessons) and hides drawing UI
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isDrawing)
                    FinishDrawing(notifyConsumer: false);
                (activeConsumer as ICancelableSymbolFlow)?.CancelSymbolFlow();
            }
        }

        private void StartDrawing()
        {
            if (isDrawing)
                return;

            if (symbolDrawUI == null)
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events, "SymbolDrawUI is not assigned.");
                ApplyDrawingCameraSensitivity(false);
                return;
            }
            CancelPendingFinish();
            isDrawing = true;
            ApplyDrawingCameraSensitivity(true);
            symbolDrawUI.gameObject.SetActive(true);
            symbolDrawUI.ClearTexture();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void FinishDrawing(bool notifyConsumer = true)
        {
            if (!isDrawing)
            {
                ApplyDrawingCameraSensitivity(false);
                if (notifyConsumer)
                    ScheduleConsumerNotification();
                else
                    CancelPendingFinish();
                return;
            }

            isDrawing = false;
            ApplyDrawingCameraSensitivity(false);

            if (symbolDrawUI != null)
            {
                symbolDrawUI.gameObject.SetActive(false);
                symbolDrawUI.ClearTexture();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (notifyConsumer)
                activeConsumer?.OnSymbolSequenceCommitted();
            else
                CancelPendingFinish();
        }

        private void ApplyDrawingCameraSensitivity(bool drawing)
        {
            if (cameraSensitivityController == null)
                return;

            if (drawing)
                cameraSensitivityController.EnableReducedSensitivity();
            else
                cameraSensitivityController.DisableReducedSensitivity();
        }

        private void OnFireStarted()
        {
            if (!isDrawing)
                return;

            CancelPendingFinish();
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
                logger.LogWarning(ComponentLogger.LogFlag.Events, "Symbol not recognized.");
                return;
            }
            activeConsumer?.OnSymbolRecognized(symbolId.ToString());
            ScheduleConsumerNotification();
        }

        private void ScheduleConsumerNotification()
        {
            CancelPendingFinish();

            if (continuationWindow <= 0f)
            {
                activeConsumer?.OnSymbolSequenceCommitted();
                return;
            }

            pendingFinishRoutine = StartCoroutine(ContinuationWindowRoutine());
        }

        private IEnumerator ContinuationWindowRoutine()
        {
            logger.Log(ComponentLogger.LogFlag.Events,
                "Continuation window started ({0:F2}s).",
                continuationWindow);
            float elapsed = 0f;
            while (elapsed < continuationWindow)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            pendingFinishRoutine = null;
            logger.Log(ComponentLogger.LogFlag.Events,
                "Continuation window expired. Finalizing symbol sequence.");
            activeConsumer?.OnSymbolSequenceCommitted();
        }

        private void CancelPendingFinish()
        {
            if (pendingFinishRoutine != null)
            {
                StopCoroutine(pendingFinishRoutine);
                pendingFinishRoutine = null;
            }
        }
        private void OnValidate()
        {
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            if (logger == null)
                logger = new ComponentLogger();
            logger.BindContext(this);
        }
    }
}
