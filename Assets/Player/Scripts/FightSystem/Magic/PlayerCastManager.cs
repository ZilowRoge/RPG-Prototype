using UnityEngine;
using System.Collections.Generic;
using Player.Statistics;
using Spells;

namespace Player.FightSystem.Magic
{
    public class PlayerCastManager : MonoBehaviour, ISymbolConsumer
    {
        [SerializeField] private SymbolInputManager inputManager;
        [SerializeField] private SkillDatabase skillDatabase;
        [SerializeField] private Transform castOrigin;

        private List<int> currentSymbols = new List<int>();
        private Spell preparedSpell = null;

        void Awake()
        {
            if (inputManager != null)
                inputManager.ActiveConsumer = this;
        }

        public void OnSymbolRecognized(string symbolId)
        {
            if (int.TryParse(symbolId, out int id))
            {
                currentSymbols.Add(id);
                Debug.Log($"[CastManager] Added symbol: {id}");
            }
            else
            {
                Debug.LogWarning($"Invalid symbolId: {symbolId}");
            }
        }

        public void OnDrawingFinished()
        {
            if (currentSymbols.Count == 0)
            {
                Debug.Log("[CastManager] No symbols drawn.");
                return;
            }

            preparedSpell = skillDatabase.GetSpellBySymbols(currentSymbols);
            if (preparedSpell != null)
            {
                Debug.Log($"[CastManager] Prepared spell: {preparedSpell.name}");
            }
            else
            {
                Debug.Log("[CastManager] Unknown spell.");
            }

            currentSymbols.Clear();
        }

        void Update()
        {
            if (preparedSpell != null && Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
            {
                CastPreparedSpell();
            }
        }

        private void CastPreparedSpell()
        {
            if (preparedSpell == null) return;

            var casterData = new CasterData(
                GetComponent<StatsController>(),
                castOrigin,
                null
            );

            preparedSpell.OnCast(casterData);
            Debug.Log($"Casting {preparedSpell.name}!");

            preparedSpell = null;
        }
    }
}
