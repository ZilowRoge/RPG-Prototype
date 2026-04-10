using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Player.Statistics;
using Spells;
using Player.Targeting;
using Systems.Perks;
using Systems.Debugging;

namespace Player.FightSystem.Magic
{
    public class CastManager : MonoBehaviour, ISymbolConsumer
    {
        [SerializeField] private SkillDatabase skillDatabase;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private Transform target;
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private StatsController statsController;
        [SerializeField] private ComponentLogger logger = new ComponentLogger();

        private readonly List<int> currentSymbols = new();
        private Spell preparedSpell = null;
        private SpellCastingService spellService;

        [SerializeField] private Player.Perks.PlayerPerkRuntime perkRuntime;

        private void Awake()
        {
            InitializeLogger();
            if (skillDatabase != null)
            {
                spellService = new SpellCastingService(skillDatabase);
            }
            else
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events, "SkillDatabase is not assigned.");
            }

            if (statsController == null)
            {
                statsController = GetComponent<StatsController>();
                if (statsController == null)
                {
                    logger.LogWarning(ComponentLogger.LogFlag.Events, "StatsController is missing.");
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
                logger.Log(ComponentLogger.LogFlag.Events, "Added symbol: {0}", id);
            }
            else
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events, "Invalid symbolId: {0}", symbolId);
            }
        }

        public void OnSymbolSequenceCommitted()
        {
            if (currentSymbols.Count == 0)
            {
                logger.Log(ComponentLogger.LogFlag.Events, "No symbols drawn.");
                return;
            }

            if (spellService == null)
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Spell service is not initialized.");
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
                    logger.Log(ComponentLogger.LogFlag.Events,
                        "Spell '{0}' casts on activation. Executing immediately.",
                        spell.name);
                    ExecuteSpellCast(spell, casterData);
                    preparedSpell = null;
                }
                else
                {
                    preparedSpell = spell;
                    logger.Log(ComponentLogger.LogFlag.Events,
                        "Prepared spell: {0}",
                        preparedSpell.name);
                }
            }
            else if (result == CastResult.InvalidSymbol)
            {
                logger.Log(ComponentLogger.LogFlag.Events, "Unknown spell.");
                preparedSpell = null;
            }
            else
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Unable to prepare spell ({0}).",
                    result);
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
                    logger.LogWarning(ComponentLogger.LogFlag.Events,
                        "StatsController is missing.");
                    casterData = null;
                    return false;
                }
            }

            if (castOrigin == null)
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Cast origin is not assigned.");
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
                    effectiveTarget = targetSelector.CurrentCombatTarget;
                }
            }
            logger.Log(ComponentLogger.LogFlag.Events,
                "Target selected: {0}.",
                effectiveTarget ? effectiveTarget.name : "null");
            casterData = new CasterData(statsController, castOrigin, effectiveTarget, perkRuntime);
            return true;
        }

        private void ExecuteSpellCast(Spell spell, CasterData casterData)
        {
            if (spell == null || casterData == null)
                return;

            if (spellService == null)
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Spell service is not initialized.");
                return;
            }

            IReadOnlyList<(PerkIntervalTriggerEffect interval, PerkEffectBase reward)> triggeredRewards = null;
            if (perkRuntime != null)
                triggeredRewards = perkRuntime.PrepareForCast(spell);

            var result = spellService.Cast(spell, casterData);
            if (result == CastResult.Success)
            {
                logger.Log(ComponentLogger.LogFlag.Events,
                    "Casting {0}!",
                    spell.name);

                if (triggeredRewards != null)
                {
                    foreach (var pair in triggeredRewards)
                    {
                        var reward = pair.reward;
                        if (reward != null)
                        {
                            logger.Log(ComponentLogger.LogFlag.Events,
                                "Perk triggered: {0}",
                                reward.GetEffectString());
                        }
                    }
                }
            }
            else
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Spell cast failed: {0}",
                    result);
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
