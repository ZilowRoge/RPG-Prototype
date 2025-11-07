using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Player.Statistics;
using Spells;
using Player.Targeting;
using Systems.Perks;

namespace Player.FightSystem.Magic
{
    public class CastManager : MonoBehaviour, ISymbolConsumer
    {
        [SerializeField] private SkillDatabase skillDatabase;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private Transform target;
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private StatsController statsController;

        private readonly List<int> currentSymbols = new();
        private Spell preparedSpell = null;
        private SpellCastingService spellService;

        [SerializeField] private Player.Perks.PlayerPerkRuntime perkRuntime;

        private void Awake()
        {
            if (skillDatabase != null)
            {
                spellService = new SpellCastingService(skillDatabase);
            }
            else
            {
                Debug.LogWarning("[CastManager] SkillDatabase is not assigned.", this);
            }

            if (statsController == null)
            {
                statsController = GetComponent<StatsController>();
                if (statsController == null)
                {
                    Debug.LogWarning("[CastManager] StatsController is missing.", this);
                }
            }

            if (perkRuntime == null)
                perkRuntime = GetComponent<Player.Perks.PlayerPerkRuntime>();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
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

        public void OnSymbolSequenceCommitted()
        {
            if (currentSymbols.Count == 0)
            {
                Debug.Log("[CastManager] No symbols drawn.");
                return;
            }

            if (spellService == null)
            {
                Debug.LogWarning("[CastManager] Spell service is not initialized.", this);
                currentSymbols.Clear();
                preparedSpell = null;
                return;
            }

            if (!TryBuildCasterData(out var casterData))
            {
                currentSymbols.Clear();
                preparedSpell = null;
                return;
            }

            var result = spellService.TryPrepareSpell(currentSymbols, casterData, out var spell);
            if (result == CastResult.Success)
            {
                if (spell.CastOnActivation)
                {
                    Debug.Log($"[CastManager] Spell '{spell.name}' casts on activation. Executing immediately.");
                    ExecuteSpellCast(spell, casterData);
                    preparedSpell = null;
                }
                else
                {
                    preparedSpell = spell;
                    Debug.Log($"[CastManager] Prepared spell: {preparedSpell.name}");
                }
            }
            else if (result == CastResult.InvalidSymbol)
            {
                Debug.Log("[CastManager] Unknown spell.");
                preparedSpell = null;
            }
            else
            {
                Debug.LogWarning($"[CastManager] Unable to prepare spell ({result}).");
                preparedSpell = null;
            }

            currentSymbols.Clear();
        }

        private void Update()
        {
            if (preparedSpell != null && IsCastInputTriggered())
            {
                CastPreparedSpell();
            }
        }

        private bool IsCastInputTriggered()
        {
            if (Mouse.current == null)
                return false;

            var keyboard = Keyboard.current;
            var isShiftHeld = keyboard != null && keyboard.leftShiftKey.isPressed;

            return Mouse.current.leftButton.wasPressedThisFrame && !isShiftHeld;
        }

        private void CastPreparedSpell()
        {
            if (preparedSpell == null)
                return;

            if (!TryBuildCasterData(out var casterData))
            {
                preparedSpell = null;
                return;
            }

            ExecuteSpellCast(preparedSpell, casterData);
            preparedSpell = null;
        }

        private bool TryBuildCasterData(out CasterData casterData)
        {
            if (statsController == null)
            {
                statsController = GetComponent<StatsController>();
                if (statsController == null)
                {
                    Debug.LogWarning("[CastManager] StatsController is missing.", this);
                    casterData = null;
                    return false;
                }
            }

            if (castOrigin == null)
            {
                Debug.LogWarning("[CastManager] Cast origin is not assigned.", this);
                casterData = null;
                return false;
            }

            Transform effectiveTarget = target;
            if (targetSelector != null)
            {
                if (preparedSpell != null && preparedSpell.RequiresTarget)
                {
                    // Per-spell targeting constraints
                    effectiveTarget = targetSelector.FindBestTarget(preparedSpell.DesiredTargetRange,
                                                                   preparedSpell.DesiredFovAngle,
                                                                   preparedSpell.PreferCenterRay);
                }
                else
                {
                    effectiveTarget = targetSelector.CurrentTarget;
                }
            }
            Debug.Log($"[CastManager] Target selected: {effectiveTarget}.", this);
            casterData = new CasterData(statsController, castOrigin, effectiveTarget, perkRuntime);
            return true;
        }

        private void ExecuteSpellCast(Spell spell, CasterData casterData)
        {
            if (spell == null || casterData == null)
                return;

            if (spellService == null)
            {
                Debug.LogWarning("[CastManager] Spell service is not initialized.", this);
                return;
            }

            IReadOnlyList<(PerkIntervalTriggerEffect interval, PerkEffectBase reward)> triggeredRewards = null;
            if (perkRuntime != null)
                triggeredRewards = perkRuntime.PrepareForCast(spell);

            var result = spellService.Cast(spell, casterData);
            if (result == CastResult.Success)
            {
                Debug.Log($"Casting {spell.name}!");

                if (triggeredRewards != null)
                {
                    foreach (var pair in triggeredRewards)
                    {
                        var reward = pair.reward;
                        if (reward != null)
                        {
                            Debug.Log($"[CastManager] Perk triggered: {reward.GetEffectString()}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[CastManager] Spell cast failed: {result}");
            }
        }
    }
}
