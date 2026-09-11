using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Map;
using ThreeInARow.Domain.Mastery;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Combat
{
    public sealed class EncounterTurnResult
    {
        public readonly bool Accepted;
        public readonly SwapRejectionReason RejectionReason;
        public readonly EventBatch Events;
        public readonly int CascadeCount;
        public readonly bool EncounterWon;
        public readonly bool RunLost;

        private EncounterTurnResult(
            bool accepted,
            SwapRejectionReason rejectionReason,
            EventBatch events,
            int cascadeCount,
            bool encounterWon,
            bool runLost)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
            CascadeCount = cascadeCount;
            EncounterWon = encounterWon;
            RunLost = runLost;
        }

        public static EncounterTurnResult Reject(SwapRejectionReason reason)
        {
            return new EncounterTurnResult(false, reason, new EventBatch(), 0, false, false);
        }

        public static EncounterTurnResult Accept(
            EventBatch events,
            int cascadeCount,
            bool encounterWon,
            bool runLost)
        {
            return new EncounterTurnResult(true, SwapRejectionReason.None, events, cascadeCount, encounterWon, runLost);
        }
    }

    /// <summary>
    /// Orchestrates one accepted board swap through player effects and exactly one enemy response.
    /// The board remains authoritative for matching; combat consumes its immutable event batch.
    /// </summary>
    public static class CombatSimulation
    {
        private const int FocusThreshold = 3;
        private const int BaseEmberDamage = 3;
        private const int BaseFocusDamage = 9;
        private const int ToxicThreshold = 5;
        private const int ToxicDamage = 13;
        private const int PoisonStackCap = 3;
        private const int BasePoisonDamagePerStack = 3;
        private const int ResourceCap = 9;

        public static EventBatch StartEncounter(
            RunState state,
            int zeroBasedEncounterIndex,
            ICombatContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpCombatContentCatalog.Instance;
            ProgressionSimulation.InitializeRun(state);
            if (state.PendingChoice.IsPending)
                throw new InvalidOperationException("Select the pending level-up reward before starting an encounter.");
            if (state.PendingCombatTurn.AwaitingEnemyResponse)
                throw new InvalidOperationException("Complete the pending combat turn before starting an encounter.");
            var encounter = catalog.GetEncounter(zeroBasedEncounterIndex);
            return StartEncounter(state, encounter.Id, zeroBasedEncounterIndex, catalog);
        }

        public static EventBatch StartEncounter(
            RunState state,
            ContentId encounterId,
            int tuningDepth,
            ICombatContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpCombatContentCatalog.Instance;
            ProgressionSimulation.InitializeRun(state);
            if (state.PendingChoice.IsPending)
                throw new InvalidOperationException("Select the pending reward before starting an encounter.");
            if (state.PendingCombatTurn.AwaitingEnemyResponse)
                throw new InvalidOperationException("Complete the pending combat turn before starting an encounter.");
            var encounter = catalog.GetEncounter(encounterId);
            state.EncounterIndex = tuningDepth;
            state.CurrentEncounterId = encounter.Id;
            state.Enemy = new EnemyState
            {
                DefinitionId = encounter.Enemy.Id,
                Health = encounter.Enemy.MaxHealth,
                IntentIndex = 0,
                PoisonStacks = 0,
                Barrier = 0,
                Phase = 0,
                TelegraphedTargetId = "content.none"
            };
            state.Player.FocusConversionsThisEncounter = 0;
            state.Player.EmpoweredEmberClearDamage = 0;

            var events = new EventBatch();
            events.Append(MapSimulation.ApplyPendingEncounterModifiers(state));
            var difficulty = MasteryRules.For(state);
            if (difficulty.EncounterStartCracked > 0)
            {
                ApplyBoardStatus(state, difficulty.Id,
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, difficulty.EncounterStartCracked), events);
                events.Append(BoardSimulation.EnsurePlayable(state));
            }
            PrepareTelegraph(state, encounter.Enemy.IntentCycle[0]);
            events.Add(
                SimulationEventType.EnemyIntentTelegraphed,
                encounter.Enemy.IntentCycle[0].Id,
                TelegraphDetail(state, encounter.Enemy, 0, encounter.Enemy.IntentCycle[0]),
                0,
                null,
                null,
                encounter.Enemy.Id);
            return events;
        }

        public static EncounterTurnResult ResolveSwap(
            RunState state,
            SwapCommand command,
            IBoardContentCatalog boardCatalog = null,
            ICombatContentCatalog combatCatalog = null)
        {
            var playerResolution = BeginSwap(state, command, boardCatalog, combatCatalog);
            if (!playerResolution.Accepted || playerResolution.EncounterWon)
                return playerResolution;

            var enemyResolution = CompleteTurn(state, boardCatalog, combatCatalog);
            var events = new EventBatch();
            events.Append(playerResolution.Events);
            events.Append(enemyResolution.Events);
            return EncounterTurnResult.Accept(
                events,
                playerResolution.CascadeCount,
                enemyResolution.EncounterWon,
                enemyResolution.RunLost);
        }

        /// <summary>
        /// Resolves the accepted swap and player gem effects, then opens the active-skill window.
        /// Call CompleteTurn when the player declines further active skills.
        /// </summary>
        public static EncounterTurnResult BeginSwap(
            RunState state,
            SwapCommand command,
            IBoardContentCatalog boardCatalog = null,
            ICombatContentCatalog combatCatalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            combatCatalog = combatCatalog ?? MvpCombatContentCatalog.Instance;
            ProgressionSimulation.InitializeRun(state);
            if (state.PendingChoice.IsPending)
                throw new InvalidOperationException("Select the pending level-up reward before resolving another swap.");
            if (state.PendingCombatTurn.AwaitingEnemyResponse)
                throw new InvalidOperationException("Complete the pending combat turn before resolving another swap.");
            if (state.Enemy == null || state.Enemy.Health <= 0)
                throw new InvalidOperationException("Start a living encounter before resolving a combat turn.");

            // Board resolution is transactional. Nothing below runs for a rejected swap.
            var boardResult = BoardSimulation.ResolveSwap(state, command, boardCatalog);
            if (!boardResult.Accepted) return EncounterTurnResult.Reject(boardResult.RejectionReason);

            var events = new EventBatch();

            if (boardResult.Events.Events.Count == 0)
                throw new InvalidOperationException("An accepted board swap must emit SwapAccepted.");
            events.Add(boardResult.Events.Events[0]);

            // Shield lasts through the enemy response that created it and expires on this next accepted swap.
            if (state.Player.Shield > 0)
            {
                var expiredShield = state.Player.Shield;
                var hardLightCap = ProgressionRules.GetModifier(state, PassiveModifierType.ShieldExpiryDamage);
                if (hardLightCap > 0)
                    DamageEnemy(state, ProgressionContentIds.HardLight,
                        Math.Min(hardLightCap, expiredShield / 2), "shield_expiry", events);
                state.Player.Shield = 0;
                events.Add(
                    SimulationEventType.ResourceChanged,
                    CombatContentIds.SystemCombat,
                    "resource=shield;reason=next_valid_swap;current=0",
                    -expiredShield);
            }

            ResolvePlayerEffects(state, boardResult.Events, events, 1);
            ExpireTimedBoardStatuses(state.Board, events);
            ConvertFocusOverflowToShield(state, events);

            if (state.Enemy.Health <= 0)
            {
                ResolveVictory(state, combatCatalog, events);
                FinishAcceptedTurn(state, events);
                return EncounterTurnResult.Accept(events, boardResult.CascadeCount, true, false);
            }

            state.PendingCombatTurn.AwaitingEnemyResponse = true;
            state.PendingCombatTurn.CascadeCount = boardResult.CascadeCount;
            return EncounterTurnResult.Accept(events, boardResult.CascadeCount, false, false);
        }

        public static EncounterTurnResult CompleteTurn(
            RunState state,
            IBoardContentCatalog boardCatalog = null,
            ICombatContentCatalog combatCatalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            combatCatalog = combatCatalog ?? MvpCombatContentCatalog.Instance;
            if (state.PendingCombatTurn == null || !state.PendingCombatTurn.AwaitingEnemyResponse)
                throw new InvalidOperationException("There is no pending enemy response to complete.");

            var cascadeCount = state.PendingCombatTurn.CascadeCount;
            var events = new EventBatch();
            var won = ResolveEnemyResponse(state, combatCatalog, boardCatalog, events);
            var lost = state.Player.Health <= 0;
            FinishAcceptedTurn(state, events);
            return EncounterTurnResult.Accept(events, cascadeCount, won, lost);
        }

        private static void ResolvePlayerEffects(
            RunState state,
            EventBatch boardEvents,
            EventBatch output,
            int startIndex)
        {
            var toxicUndertowCascades = new HashSet<int>();
            var currentCascade = 0;
            var thornDamage = 0;
            for (var index = startIndex; index < boardEvents.Events.Count; index++)
            {
                var item = boardEvents.Events[index];
                output.Add(item);
                var parsedCascade = ParseCascade(item.Detail);
                if (parsedCascade > 0) currentCascade = parsedCascade;
                if (item.Type == SimulationEventType.GemCleared)
                    ResolveGemClear(state, item, output, toxicUndertowCascades, currentCascade, ref thornDamage);
                else if (item.Type == SimulationEventType.SpecialActivated)
                    ResolveSpecial(state, item.SourceId, output, toxicUndertowCascades, currentCascade);
                else if (item.Type == SimulationEventType.GemsMatched &&
                         item.SourceId.Equals(BoardContentIds.Venom) && item.Amount >= 4)
                {
                    var bonus = ProgressionRules.GetModifier(state, PassiveModifierType.LargeVenomMatchToxic);
                    if (bonus > 0) AddToxic(state, bonus, ProgressionContentIds.Contagion, output);
                }
                else if (item.Type == SimulationEventType.GemsMatched && item.SourceId.Equals(BoardContentIds.Ember) &&
                         state.Player.EmpoweredEmberClearDamage > 0)
                {
                    var bonus = state.Player.EmpoweredEmberClearDamage;
                    state.Player.EmpoweredEmberClearDamage = 0;
                    DamageEnemy(state, ProgressionContentIds.ScaldingCurrent, bonus * item.Amount,
                        "empowered_ember_match", output);
                }
            }
        }

        private static void ResolveGemClear(RunState state, SimulationEvent item, EventBatch output,
            HashSet<int> toxicUndertowCascades, int cascade, ref int thornDamage)
        {
            if (Contains(item.StatusIds, BoardContentIds.Thorned) && thornDamage < 6)
            {
                var applied = Math.Min(2, 6 - thornDamage);
                thornDamage += applied;
                DamagePlayer(state, BoardContentIds.Thorned, applied, output);
            }
            if (Contains(item.StatusIds, BoardContentIds.Cracked)) return;
            if (!IsNoSpecial(item.RelatedId)) return;

            if (item.SourceId.Equals(BoardContentIds.Ember))
            {
                var damage = BaseEmberDamage + ProgressionRules.GetModifier(
                    state, PassiveModifierType.EmberClearDamage);
                if (state.Player.EmpoweredEmberClearDamage > 0)
                {
                    damage += state.Player.EmpoweredEmberClearDamage;
                    state.Player.EmpoweredEmberClearDamage = 0;
                }
                DamageEnemy(state, BoardContentIds.Ember, damage, "gem_clear", output);
            }
            else if (item.SourceId.Equals(BoardContentIds.Tide))
            {
                AddFocus(state, 1, BoardContentIds.Tide, output, toxicUndertowCascades, cascade);
            }
            else if (item.SourceId.Equals(BoardContentIds.Venom))
            {
                AddToxic(state, 1, BoardContentIds.Venom, output);
            }
            else if (item.SourceId.Equals(BoardContentIds.Volt))
            {
                DamageEnemy(state, BoardContentIds.Volt, 3, "gem_clear", output);
                AddVoltProgress(state, 1, output);
            }
        }

        private static void ResolveSpecial(RunState state, ContentId specialId, EventBatch output,
            HashSet<int> toxicUndertowCascades, int cascade)
        {
            if (specialId.Equals(BoardContentIds.Spark))
            {
                var firstSparkBonus = CountEvents(output, SimulationEventType.SpecialActivated, BoardContentIds.Spark) <= 1
                    ? ProgressionRules.GetModifier(state, PassiveModifierType.SparkFirstDamage)
                    : 0;
                DamageEnemy(state, specialId, 12 + firstSparkBonus, "special", output);
                var shield = ProgressionRules.GetModifier(state, PassiveModifierType.SparkShield);
                if (shield > 0)
                {
                    state.Player.Shield += shield;
                    output.Add(SimulationEventType.ResourceChanged, specialId,
                        "resource=shield;reason=backdraft;current=" + state.Player.Shield, shield);
                }
                var flashfire = ProgressionRules.GetModifier(state, PassiveModifierType.SparkAllCooldownReduction);
                if (flashfire > 0) ReduceAllCooldowns(state, flashfire, "flashfire", output, true);
            }
            else if (specialId.Equals(BoardContentIds.Current))
            {
                AddFocus(state, 5, specialId, output, toxicUndertowCascades, cascade);
            }
            else if (specialId.Equals(BoardContentIds.Spore))
            {
                AddToxic(state, 5, specialId, output);
            }
            else if (specialId.Equals(BoardContentIds.Charge))
            {
                DamageEnemy(state, specialId, 10, "special", output);
                var reduction = 1 + ProgressionRules.GetModifier(state, PassiveModifierType.ChargeCooldownReduction);
                ReduceAllCooldowns(state, reduction, "charge", output, true);
            }
            // Prism's effect is represented by the color GemCleared events that follow it.
        }

        private static void AddFocus(RunState state, int amount, ContentId sourceId, EventBatch output,
            HashSet<int> toxicUndertowCascades = null, int cascade = 0)
        {
            state.Player.Focus += amount;
            output.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=focus;current=" + state.Player.Focus, amount);
            while (state.Player.Focus >= FocusThreshold)
            {
                state.Player.Focus -= FocusThreshold;
                output.Add(SimulationEventType.ResourceChanged, sourceId,
                    "resource=focus;reason=conversion;current=" + state.Player.Focus, -FocusThreshold);
                var damage = BaseFocusDamage + ProgressionRules.GetModifier(
                    state, PassiveModifierType.FocusConversionDamage);
                DamageEnemy(state, sourceId, damage, "focus_conversion", output);
                state.Player.FocusConversionsThisEncounter++;
                var emberEmpower = ProgressionRules.GetModifier(
                    state, PassiveModifierType.EverySecondFocusEmpowersEmber);
                if (emberEmpower > 0 && state.Player.FocusConversionsThisEncounter % 2 == 0)
                    state.Player.EmpoweredEmberClearDamage = emberEmpower;
                var toxicGain = ProgressionRules.GetModifier(
                    state, PassiveModifierType.FocusConversionToxicPerCascade);
                if (toxicGain > 0 && toxicUndertowCascades != null &&
                    !toxicUndertowCascades.Contains(cascade))
                {
                    toxicUndertowCascades.Add(cascade);
                    AddToxic(state, toxicGain, ProgressionContentIds.ToxicUndertow, output);
                }
                var cooldownReduction = ProgressionRules.GetModifier(
                    state, PassiveModifierType.FocusConversionLeftCooldown);
                if (cooldownReduction > 0)
                    ReduceLeftCooldown(state, cooldownReduction, "undertow", output, true);
                var shield = ProgressionRules.GetModifier(state, PassiveModifierType.FocusConversionShield);
                if (shield > 0)
                {
                    state.Player.Shield += shield;
                    output.Add(SimulationEventType.ResourceChanged, ProgressionContentIds.Reservoir,
                        "resource=shield;reason=focus_conversion;current=" + state.Player.Shield, shield);
                }
            }
        }

        private static void AddToxic(RunState state, int amount, ContentId sourceId, EventBatch output)
        {
            state.Player.Toxic += amount;
            output.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=toxic;current=" + state.Player.Toxic, amount);
            var threshold = Math.Max(1, ToxicThreshold + ProgressionRules.GetModifier(
                state, PassiveModifierType.ToxicThreshold));
            while (state.Player.Toxic >= threshold)
            {
                state.Player.Toxic -= threshold;
                output.Add(SimulationEventType.ResourceChanged, sourceId,
                    "resource=toxic;reason=conversion;current=" + state.Player.Toxic, -threshold);
                DamageEnemy(state, sourceId, ToxicDamage, "toxic_conversion", output);
                AddEnemyPoison(state, sourceId, 1, output);
            }
        }

        private static bool ResolveEnemyResponse(
            RunState state,
            ICombatContentCatalog catalog,
            IBoardContentCatalog boardCatalog,
            EventBatch events)
        {
            if (state.Enemy.PoisonStacks > 0)
            {
                var stacks = state.Enemy.PoisonStacks;
                var damagePerStack = BasePoisonDamagePerStack + ProgressionRules.GetModifier(
                    state, PassiveModifierType.PoisonDamagePerStack);
                events.Add(SimulationEventType.StatusTicked, CombatContentIds.Poison,
                    "target=enemy;stacks=" + stacks, stacks, null, null, state.Enemy.DefinitionId);
                DamageEnemy(state, CombatContentIds.Poison, stacks * damagePerStack,
                    "enemy_response_start;stacks=" + stacks, events);
                var voltProgress = ProgressionRules.GetModifier(state, PassiveModifierType.PoisonTickVoltProgress);
                if (voltProgress > 0) AddVoltProgress(state, voltProgress, events);
                state.Enemy.PoisonStacks = Math.Max(0, stacks - 1);
                if (state.Enemy.PoisonStacks == 0)
                    events.Add(SimulationEventType.StatusRemoved, CombatContentIds.Poison, "expired", 1);
                if (state.Enemy.Health <= 0)
                {
                    ResolveVictory(state, catalog, events);
                    return true;
                }
            }

            var enemy = catalog.GetEnemy(state.Enemy.DefinitionId);
            var cycle = GetIntentCycle(state, enemy);
            var intentIndex = PositiveModulo(state.Enemy.IntentIndex, cycle.Count);
            var intent = cycle[intentIndex];
            events.Add(
                SimulationEventType.EnemyIntentStarted,
                intent.Id,
                "execute;telegraph=" + intent.TelegraphKey,
                0,
                null,
                null,
                enemy.Id);

            var effects = new List<IntentEffectDefinition>(intent.Effects);
            var difficulty = MasteryRules.For(state);
            if (enemy.IsElite && intentIndex == cycle.Count - 1)
                effects.AddRange(difficulty.EliteFinalIntentEffects);
            foreach (var effect in effects)
            {
                if (effect.Type == IntentEffectType.DamagePlayer)
                    DamagePlayer(state, intent.Id, effect.Amount + difficulty.EnemyDirectDamageBonus, events);
                else if (effect.Type == IntentEffectType.ApplyBoardStatus)
                    ApplyBoardStatus(state, intent.Id, effect, events);
                else if (effect.Type == IntentEffectType.DrainResources)
                    DrainResources(state, intent.Id, effect, events);
                else if (effect.Type == IntentEffectType.GainEnemyBarrier)
                    GainEnemyBarrier(state, intent.Id, effect.Amount, events);
                else if (effect.Type == IntentEffectType.JamActiveSkill)
                    JamActiveSkill(state, intent.Id, effect.Amount, events);
            }

            state.Enemy.IntentIndex = (intentIndex + 1) % cycle.Count;
            if (state.Player.Health <= 0)
            {
                events.Add(SimulationEventType.RunEnded, enemy.Id, "defeat", 0);
                return false;
            }
            events.Append(BoardSimulation.EnsurePlayable(state, boardCatalog));
            cycle = GetIntentCycle(state, enemy);
            var nextIntent = cycle[state.Enemy.IntentIndex % cycle.Count];
            PrepareTelegraph(state, nextIntent);
            events.Add(
                SimulationEventType.EnemyIntentTelegraphed,
                nextIntent.Id,
                TelegraphDetail(state, enemy, state.Enemy.IntentIndex, nextIntent),
                0,
                null,
                null,
                enemy.Id);
            return false;
        }

        internal static void ApplySkillDamage(
            RunState state,
            ContentId sourceId,
            int amount,
            EventBatch output)
        {
            DamageEnemy(state, sourceId, amount, "active_skill", output);
        }

        internal static void ApplyBoardMutationEffects(RunState state, EventBatch boardEvents, EventBatch output)
        {
            ResolvePlayerEffects(state, boardEvents, output, 0);
            ConvertFocusOverflowToShield(state, output);
        }

        private static void DamageEnemy(
            RunState state,
            ContentId sourceId,
            int amount,
            string detail,
            EventBatch output)
        {
            if (amount <= 0 || state.Enemy.Health <= 0) return;
            var absorbed = Math.Min(amount, state.Enemy.Barrier);
            if (absorbed > 0)
            {
                state.Enemy.Barrier -= absorbed;
                output.Add(SimulationEventType.EnemyBarrierChanged, sourceId,
                    "reason=absorbed;current=" + state.Enemy.Barrier, -absorbed, null, null, state.Enemy.DefinitionId);
            }
            amount -= absorbed;
            if (amount <= 0) return;
            var beforeHealth = state.Enemy.Health;
            var applied = Math.Min(amount, state.Enemy.Health);
            state.Enemy.Health -= applied;
            output.Add(
                SimulationEventType.DamageApplied,
                sourceId,
                "target=enemy;" + detail,
                applied,
                null,
                null,
                state.Enemy.DefinitionId);
            TryEnterBossPhase(state, beforeHealth, output);
        }

        private static void DamagePlayer(RunState state, ContentId sourceId, int amount, EventBatch events)
        {
            if (amount <= 0 || state.Player.Health <= 0) return;
            var absorbed = Math.Min(state.Player.Shield, amount);
            state.Player.Shield -= absorbed;
            var healthDamage = Math.Min(state.Player.Health, amount - absorbed);
            state.Player.Health -= healthDamage;
            events.Add(
                SimulationEventType.DamageApplied,
                sourceId,
                "target=player;absorbed=" + absorbed,
                healthDamage,
                null,
                null,
                CombatContentIds.SystemCombat);
        }

        private static void ApplyBoardStatus(
            RunState state,
            ContentId intentId,
            IntentEffectDefinition effect,
            EventBatch events)
        {
            if (state.Board == null || state.Board.Gems == null) return;
            var candidates = new List<BoardGemState>();
            foreach (var gem in state.Board.Gems)
            {
                if (gem == null || Contains(gem.StatusIds, effect.StatusId)) continue;
                candidates.Add(gem);
            }

            var random = RandomStreams.Restore(RandomStream.IntentVariation, state.RandomStreams);
            var count = Math.Min(effect.Amount, candidates.Count);
            for (var index = 0; index < count; index++)
            {
                var selectedIndex = random.NextInt(candidates.Count);
                var gem = candidates[selectedIndex];
                candidates.RemoveAt(selectedIndex);
                AddStatus(gem, effect.StatusId, effect.DurationPlayerTurns);
                events.Add(
                    SimulationEventType.StatusAdded,
                    effect.StatusId,
                    "durationPlayerTurns=" + effect.DurationPlayerTurns,
                    1,
                    gem.Cell,
                    null,
                    intentId);
            }
            RandomStreams.Store(RandomStream.IntentVariation, random, state.RandomStreams);
        }

        private static void DrainResources(
            RunState state,
            ContentId intentId,
            IntentEffectDefinition effect,
            EventBatch events)
        {
            var focus = Math.Min(state.Player.Focus, effect.FocusAmount);
            var toxic = Math.Min(state.Player.Toxic, effect.ToxicAmount);
            state.Player.Focus -= focus;
            state.Player.Toxic -= toxic;
            if (focus > 0)
                events.Add(SimulationEventType.ResourceChanged, intentId,
                    "resource=focus;reason=drain;current=" + state.Player.Focus, -focus);
            if (toxic > 0)
                events.Add(SimulationEventType.ResourceChanged, intentId,
                    "resource=toxic;reason=drain;current=" + state.Player.Toxic, -toxic);
        }

        private static void GainEnemyBarrier(RunState state, ContentId intentId, int amount, EventBatch events)
        {
            if (amount <= 0) return;
            state.Enemy.Barrier += amount;
            events.Add(SimulationEventType.EnemyBarrierChanged, intentId,
                "reason=intent;current=" + state.Enemy.Barrier, amount, null, null, state.Enemy.DefinitionId);
        }

        private static void JamActiveSkill(RunState state, ContentId intentId, int amount, EventBatch events)
        {
            if (amount <= 0 || state.Player.EquippedActiveSkillIds == null ||
                state.Player.EquippedActiveSkillIds.Count == 0) return;
            var targetId = state.Enemy.TelegraphedTargetId;
            if (!Contains(state.Player.EquippedActiveSkillIds, targetId))
            {
                var random = RandomStreams.Restore(RandomStream.IntentVariation, state.RandomStreams);
                targetId = state.Player.EquippedActiveSkillIds[random.NextInt(state.Player.EquippedActiveSkillIds.Count)];
                RandomStreams.Store(RandomStream.IntentVariation, random, state.RandomStreams);
            }
            var cooldown = ProgressionRules.FindCooldown(state.Player, targetId);
            if (cooldown == null) return;
            cooldown.RemainingTurns += amount;
            if (state.PendingCombatTurn != null && state.PendingCombatTurn.SkillIdsUsed != null &&
                !Contains(state.PendingCombatTurn.SkillIdsUsed, targetId))
                state.PendingCombatTurn.SkillIdsUsed.Add(targetId);
            events.Add(SimulationEventType.ActiveJammed, intentId,
                "turns=" + amount + ";current=" + cooldown.RemainingTurns, amount, null, null, targetId);
            events.Add(SimulationEventType.CooldownChanged, targetId,
                "reason=jammed;current=" + cooldown.RemainingTurns, amount, null, null, intentId);
        }

        internal static void AddEnemyPoison(
            RunState state,
            ContentId sourceId,
            int amount,
            EventBatch events)
        {
            if (amount <= 0) return;
            var before = state.Enemy.PoisonStacks;
            state.Enemy.PoisonStacks = Math.Min(PoisonStackCap, before + amount);
            var applied = state.Enemy.PoisonStacks - before;
            if (applied <= 0) return;
            events.Add(
                SimulationEventType.StatusAdded,
                CombatContentIds.Poison,
                "target=enemy;reason=" + sourceId + ";stacks=" + state.Enemy.PoisonStacks,
                applied,
                null,
                null,
                state.Enemy.DefinitionId);
        }

        internal static void FinishTurnAfterSkillVictory(
            RunState state,
            ICombatContentCatalog catalog,
            EventBatch events)
        {
            ResolveVictory(state, catalog, events);
            FinishAcceptedTurn(state, events);
        }

        private static void ResolveVictory(RunState state, ICombatContentCatalog catalog, EventBatch events)
        {
            var enemy = catalog.GetEnemy(state.Enemy.DefinitionId);
            state.Enemy.Health = 0;
            events.Add(SimulationEventType.EnemyDefeated, enemy.Id, "victory", 1);
            ProgressionSimulation.GrantExperience(state, enemy.RewardXp, enemy.Id, events);
            if (enemy.IsElite)
                ProgressionSimulation.QueueEliteReward(state, enemy.Id, events);
            var victoryHeal = MasteryRules.For(state).BaseVictoryHealing +
                ProgressionRules.GetModifier(state, PassiveModifierType.VictoryHeal);
            var restored = Math.Min(victoryHeal, PlayerState.MaxHealth - state.Player.Health);
            state.Player.Health += restored;
            if (restored > 0)
                events.Add(SimulationEventType.ResourceChanged, enemy.Id,
                    "resource=health;reason=victory;current=" + state.Player.Health, restored);
        }

        private static void ExpireTimedBoardStatuses(BoardState board, EventBatch events)
        {
            if (board == null || board.Gems == null) return;
            foreach (var gem in board.Gems)
            {
                if (gem == null || gem.StatusDurations == null) continue;
                for (var index = gem.StatusDurations.Count - 1; index >= 0; index--)
                {
                    var duration = gem.StatusDurations[index];
                    if (duration == null || duration.RemainingPlayerTurns <= 0) continue;
                    duration.RemainingPlayerTurns--;
                    if (duration.RemainingPlayerTurns > 0) continue;
                    gem.StatusDurations.RemoveAt(index);
                    Remove(gem.StatusIds, duration.StatusId);
                    events.Add(
                        SimulationEventType.StatusRemoved,
                        duration.StatusId,
                        "duration_expired",
                        1,
                        gem.Cell,
                        null,
                        gem.GemId);
                }
            }
        }

        private static void AddStatus(BoardGemState gem, ContentId statusId, int durationPlayerTurns)
        {
            if (gem.StatusIds == null) gem.StatusIds = new List<ContentId>();
            if (!Contains(gem.StatusIds, statusId)) gem.StatusIds.Add(statusId);
            if (durationPlayerTurns <= 0) return;
            if (gem.StatusDurations == null) gem.StatusDurations = new List<BoardStatusDurationState>();
            gem.StatusDurations.Add(new BoardStatusDurationState
            {
                StatusId = statusId,
                RemainingPlayerTurns = durationPlayerTurns
            });
        }

        private static void ConvertFocusOverflowToShield(RunState state, EventBatch events)
        {
            if (state.Player.Focus <= ResourceCap) return;
            var overflow = state.Player.Focus - ResourceCap;
            state.Player.Focus = ResourceCap;
            state.Player.Shield += overflow;
            events.Add(SimulationEventType.ResourceChanged, CombatContentIds.SystemCombat,
                "resource=focus;reason=overflow;current=" + state.Player.Focus, -overflow);
            events.Add(SimulationEventType.ResourceChanged, CombatContentIds.SystemCombat,
                "resource=shield;reason=focus_overflow;current=" + state.Player.Shield, overflow);
        }

        private static void AddVoltProgress(RunState state, int amount, EventBatch events)
        {
            var player = state.Player;
            player.VoltClearProgress += amount;
            events.Add(SimulationEventType.ResourceChanged, BoardContentIds.Volt,
                "resource=volt_progress;current=" + player.VoltClearProgress, amount);
            var threshold = Math.Max(1, 3 + ProgressionRules.GetModifier(
                state, PassiveModifierType.VoltClearThreshold));
            while (player.VoltClearProgress >= threshold)
            {
                player.VoltClearProgress -= threshold;
                events.Add(SimulationEventType.ResourceChanged, BoardContentIds.Volt,
                    "resource=volt_progress;reason=conversion;current=" + player.VoltClearProgress, -threshold);
                ReduceLeftCooldown(state, 1, "volt_progress", events, true);
            }
        }

        private static void ReduceLeftCooldown(
            RunState state,
            int amount,
            string reason,
            EventBatch events,
            bool grantStaticGuard)
        {
            var player = state.Player;
            if (player.EquippedActiveSkillIds == null || player.EquippedActiveSkillIds.Count == 0) return;
            var cooldown = ProgressionRules.FindCooldown(player, player.EquippedActiveSkillIds[0]);
            if (ReduceCooldown(cooldown, amount, reason, events) && grantStaticGuard)
                GrantStaticGuard(state, reason, events);
        }

        private static void ReduceAllCooldowns(RunState state, int amount, string reason, EventBatch events, bool grantStaticGuard)
        {
            var player = state.Player;
            if (player.EquippedActiveSkillIds == null) return;
            var reduced = false;
            foreach (var skillId in player.EquippedActiveSkillIds)
                reduced |= ReduceCooldown(ProgressionRules.FindCooldown(player, skillId), amount, reason, events);
            if (reduced && grantStaticGuard) GrantStaticGuard(state, reason, events);
        }

        private static bool ReduceCooldown(
            SkillCooldownState cooldown,
            int amount,
            string reason,
            EventBatch events)
        {
            if (cooldown == null) return false;
            var before = cooldown.RemainingTurns;
            cooldown.RemainingTurns = Math.Max(0, before - amount);
            if (events != null && cooldown.RemainingTurns != before)
                events.Add(SimulationEventType.CooldownChanged, cooldown.SkillId,
                    "reason=" + reason + ";current=" + cooldown.RemainingTurns,
                    cooldown.RemainingTurns - before);
            return cooldown.RemainingTurns != before;
        }

        private static void GrantStaticGuard(RunState state, string reason, EventBatch events)
        {
            var shield = ProgressionRules.GetModifier(state, PassiveModifierType.CooldownReductionShield);
            if (shield <= 0) return;
            state.Player.Shield += shield;
            events.Add(SimulationEventType.ResourceChanged, ProgressionContentIds.StaticGuard,
                "resource=shield;reason=cooldown_reduction_" + reason + ";current=" + state.Player.Shield, shield);
        }

        private static void TickSkillCooldowns(
            PlayerState player,
            IEnumerable<ContentId> skillsUsedThisTurn,
            EventBatch events)
        {
            if (player.EquippedActiveSkillIds == null) return;
            foreach (var skillId in player.EquippedActiveSkillIds)
            {
                if (Contains(skillsUsedThisTurn, skillId)) continue;
                ReduceCooldown(ProgressionRules.FindCooldown(player, skillId), 1, "player_turn", events);
            }
        }

        private static void FinishAcceptedTurn(RunState state, EventBatch events)
        {
            var usedSkills = state.PendingCombatTurn == null
                ? null
                : state.PendingCombatTurn.SkillIdsUsed;
            TickSkillCooldowns(state.Player, usedSkills, events);
            state.ResolvedTurnCount++;
            if (state.PendingCombatTurn == null)
                state.PendingCombatTurn = new PendingCombatTurnState();
            state.PendingCombatTurn.AwaitingEnemyResponse = false;
            state.PendingCombatTurn.CascadeCount = 0;
            if (state.PendingCombatTurn.SkillIdsUsed == null)
                state.PendingCombatTurn.SkillIdsUsed = new List<ContentId>();
            else
                state.PendingCombatTurn.SkillIdsUsed.Clear();
        }

        private static IReadOnlyList<IntentDefinition> GetIntentCycle(RunState state, EnemyDefinition enemy)
        {
            if (state.Enemy.Phase > 0 && enemy.SecondPhaseIntentCycle != null && enemy.SecondPhaseIntentCycle.Count > 0)
                return enemy.SecondPhaseIntentCycle;
            return enemy.IntentCycle;
        }

        private static void TryEnterBossPhase(RunState state, int beforeHealth, EventBatch events)
        {
            if (state.Enemy.Phase > 0 || !MasteryRules.For(state).EnableBossSecondPhase || state.Enemy.Health <= 0) return;
            EnemyDefinition enemy;
            try { enemy = MvpCombatContentCatalog.Instance.GetEnemy(state.Enemy.DefinitionId); }
            catch (KeyNotFoundException) { return; }
            if (!enemy.IsBoss || enemy.SecondPhaseIntentCycle == null || enemy.SecondPhaseIntentCycle.Count == 0) return;
            var threshold = enemy.MaxHealth * enemy.SecondPhaseHealthPercent / 100;
            if (beforeHealth <= threshold || state.Enemy.Health > threshold) return;
            state.Enemy.Phase = 1;
            state.Enemy.IntentIndex = 0;
            var next = enemy.SecondPhaseIntentCycle[0];
            PrepareTelegraph(state, next);
            events.Add(SimulationEventType.BossPhaseChanged, enemy.Id,
                "phase=2;thresholdPercent=" + enemy.SecondPhaseHealthPercent, 2);
            events.Add(SimulationEventType.EnemyIntentTelegraphed, next.Id,
                TelegraphDetail(state, enemy, 0, next), 0, null, null, enemy.Id);
        }

        private static string TelegraphDetail(RunState state, EnemyDefinition enemy, int intentIndex,
            IntentDefinition intent)
        {
            var detail = "telegraph=" + intent.TelegraphKey + ";phase=" + (state.Enemy.Phase + 1);
            foreach (var effect in intent.Effects)
                if (effect.Type == IntentEffectType.JamActiveSkill)
                    detail += ";jamTarget=" + state.Enemy.TelegraphedTargetId + ";jamTurns=" + effect.Amount;
            var cycle = GetIntentCycle(state, enemy);
            if (enemy.IsElite && intentIndex == cycle.Count - 1 &&
                MasteryRules.For(state).EliteFinalIntentEffects.Count > 0)
                detail += ";difficultyExtraEffects=" + MasteryRules.For(state).EliteFinalIntentEffects.Count;
            return detail;
        }

        private static void PrepareTelegraph(RunState state, IntentDefinition intent)
        {
            state.Enemy.TelegraphedTargetId = "content.none";
            if (state.Player.EquippedActiveSkillIds == null || state.Player.EquippedActiveSkillIds.Count == 0) return;
            foreach (var effect in intent.Effects)
            {
                if (effect.Type != IntentEffectType.JamActiveSkill) continue;
                var random = RandomStreams.Restore(RandomStream.IntentVariation, state.RandomStreams);
                state.Enemy.TelegraphedTargetId =
                    state.Player.EquippedActiveSkillIds[random.NextInt(state.Player.EquippedActiveSkillIds.Count)];
                RandomStreams.Store(RandomStream.IntentVariation, random, state.RandomStreams);
                return;
            }
        }

        private static int ParseCascade(string detail)
        {
            if (string.IsNullOrEmpty(detail)) return 0;
            const string marker = "cascade=";
            var start = detail.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return 0;
            start += marker.Length;
            var end = detail.IndexOf(';', start);
            var value = end < 0 ? detail.Substring(start) : detail.Substring(start, end - start);
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : 0;
        }

        private static bool IsNoSpecial(ContentId specialId)
        {
            return string.IsNullOrEmpty(specialId.Value) || specialId.Equals(BoardContentIds.NoSpecial);
        }

        private static bool Contains(IEnumerable<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return false;
            foreach (var id in ids)
                if (id.Equals(wanted)) return true;
            return false;
        }

        private static void Remove(List<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return;
            for (var index = ids.Count - 1; index >= 0; index--)
                if (ids[index].Equals(wanted)) ids.RemoveAt(index);
        }

        private static int PositiveModulo(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static int CountEvents(EventBatch events, SimulationEventType type, ContentId sourceId)
        {
            var count = 0;
            foreach (var item in events.Events)
                if (item.Type == type && item.SourceId.Equals(sourceId)) count++;
            return count;
        }
    }
}
