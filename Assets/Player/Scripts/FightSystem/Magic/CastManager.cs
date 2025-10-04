using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Barracuda;
using Player.UI;
using System.Collections.Generic;
using Spells;

namespace Player.FightSystem.Magic
{
    public class CastManager : MonoBehaviour
    {
        [SerializeField] private SymbolDrawUI symbolDrawUI;
        [SerializeField] private NNModel modelAsset;
        [SerializeField] private SkillDatabase skillDatabase;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private Transform target;
        [SerializeField] private Statistics.StatsController stats;

        private PlayerControlls controls;
        private SymbolRecognizer symbolRecognizer;
        private SpellCastingService spellService;

        private List<int> currentSymbols = new();
        private bool isDrawing = false;
        private bool spellReady = false;

        private void Awake()
        {
            controls = new PlayerControlls();
            symbolRecognizer = new SymbolRecognizer(modelAsset);
            spellService = new SpellCastingService(skillDatabase);

            symbolDrawUI.gameObject.SetActive(false);

            controls.Player.AlternativeUse.started += ctx => StartDrawing();
            controls.Player.AlternativeUse.canceled += ctx => FinalizeSpell();

            controls.Player.Attack.started += ctx => OnFireStarted();
            controls.Player.Attack.canceled += ctx => OnFireCanceled();
        }

        private void OnEnable() => controls.Enable();
        private void OnDestroy() => symbolRecognizer.Clear();
        private void OnDisable() {
            controls.Disable();
            symbolRecognizer.Clear();
        }


        private void StartDrawing()
        {
            currentSymbols.Clear();
            spellReady = false;
            isDrawing = true;

            symbolDrawUI.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("Drawing mode enabled");
        }

        private void OnFireStarted()
        {
            if (isDrawing)
            {
                return;
            }
            else if (spellReady)
            {
                var casterData = new CasterData(stats, castOrigin, target);
                var result = spellService.Cast(currentSymbols, casterData);

                if (result == CastResult.Success)
                    Debug.Log("Spell cast successfully.");
                else
                    Debug.LogWarning($"Spell cast failed: {result}");

                spellReady = false;
                currentSymbols.Clear();
            }
        }

        private void OnFireCanceled()
        {
            if (!isDrawing) return;

            // symbolDrawUI.EndDraw();

            (int symbolId, float probability) = symbolRecognizer.GetSymbol(symbolDrawUI.GetNormalizedTexture64());
            symbolDrawUI.ClearTexture();

            if (probability < .8f)
            {
                Debug.LogWarning("Symbol not recognized, clearing sequence.");
                currentSymbols.Clear();
                return;
            }

            currentSymbols.Add(symbolId);
            Debug.Log($"Added symbol {symbolId} to sequence. Current: {string.Join(",", currentSymbols)}");
        }

        private void FinalizeSpell()
        {
            isDrawing = false;
            symbolDrawUI.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (currentSymbols.Count == 0)
            {
                Debug.Log("No symbols drawn, spell canceled.");
                return;
            }

            var casterData = new CasterData(stats, castOrigin, target);
            var result = spellService.Activate(currentSymbols, casterData);

            if (result == CastResult.Success)
            {
                Debug.Log($"Spell activated with sequence [{string.Join(",", currentSymbols)}]");
                spellReady = true; // jeśli nie instant, czekamy na LPM
            }
            else
            {
                Debug.LogWarning($"Spell not recognized or invalid. Sequence: [{string.Join(",", currentSymbols)}]");
            }
            currentSymbols.Clear();
        }
    }
}
