using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
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
        private const int FocusDamage = 6;
        private const int ToxicThreshold = 5;
        private const int ToxicDamage = 12;
        private const int PoisonStackCap = 3;
        private const int PoisonDamagePerStack = 3;
        private const int ResourceCap = 9;

        public static EventBatch StartEncounter(
            RunState state,
            int zeroBasedEncounterIndex,
            ICombatContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpCombatContentCatalog.Instance;
            var encounter = catalog.GetEncounter(zeroBasedEncounterIndex);
            state.EncounterIndex = zeroBasedEncounterIndex;
            state.Enemy = new EnemyState
            {
                DefinitionId = encounter.Enemy.Id,
                Health = encounter.Enemy.MaxHealth,
                IntentIndex = 0,
                PoisonStacks = 0
            };

            var events = new EventBatch();
            events.Add(
                SimulationEventType.EnemyIntentTelegraphed,
                encounter.Enemy.IntentCycle[0].Id,
                "telegraph=" + encounter.Enemy.IntentCycle[0].TelegraphKey,
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
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            combatCatalog = combatCatalog ?? MvpCombatContentCatalog.Instance;
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

            var won = false;
            var lost = false;
            if (state.Enemy.Health <= 0)
            {
                ResolveVictory(state, combatCatalog, events);
                won = true;
            }
            else
            {
                won = ResolveEnemyResponse(state, combatCatalog, boardCatalog, events);
                lost = state.Player.Health <= 0;
            }

            TickSkillCooldowns(state.Player, events);
            state.ResolvedTurnCount++;
            return EncounterTurnResult.Accept(events, boardResult.CascadeCount, won, lost);
        }

        private static void ResolvePlayerEffects(
            RunState state,
            EventBatch boardEvents,
            EventBatch output,
            int startIndex)
        {
            for (var index = startIndex; index < boardEvents.Events.Count; index++)
            {
                var item = boardEvents.Events[index];
                output.Add(item);
                if (item.Type == SimulationEventType.GemCleared)
                    ResolveGemClear(state, item, output);
                else if (item.Type == SimulationEventType.SpecialActivated)
                    ResolveSpecial(state, item.SourceId, output);
            }
        }

        private static void ResolveGemClear(RunState state, SimulationEvent item, EventBatch output)
        {
            if (Contains(item.StatusIds, BoardContentIds.Cracked)) return;
            if (!IsNoSpecial(item.RelatedId)) return;

            if (item.SourceId.Equals(BoardContentIds.Ember))
            {
                DamageEnemy(state, BoardContentIds.Ember, 4, "gem_clear", output);
            }
            else if (item.SourceId.Equals(BoardContentIds.Tide))
            {
                AddFocus(state, 1, BoardContentIds.Tide, output);
            }
            else if (item.SourceId.Equals(BoardContentIds.Venom))
            {
                AddToxic(state, 1, BoardContentIds.Venom, output);
            }
            else if (item.SourceId.Equals(BoardContentIds.Volt))
            {
                DamageEnemy(state, BoardContentIds.Volt, 2, "gem_clear", output);
                AddVoltProgress(state.Player, 1, output);
            }
        }

        private static void ResolveSpecial(RunState state, ContentId specialId, EventBatch output)
        {
            if (specialId.Equals(BoardContentIds.Spark))
            {
                DamageEnemy(state, specialId, 16, "special", output);
            }
            else if (specialId.Equals(BoardContentIds.Current))
            {
                AddFocus(state, 5, specialId, output);
            }
            else if (specialId.Equals(BoardContentIds.Spore))
            {
                AddToxic(state, 5, specialId, output);
            }
            else if (specialId.Equals(BoardContentIds.Charge))
            {
                DamageEnemy(state, specialId, 8, "special", output);
                ReduceAllCooldowns(state.Player, 1, "charge", output);
            }
            // Prism's effect is represented by the color GemCleared events that follow it.
        }

        private static void AddFocus(RunState state, int amount, ContentId sourceId, EventBatch output)
        {
            state.Player.Focus += amount;
            output.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=focus;current=" + state.Player.Focus, amount);
            while (state.Player.Focus >= FocusThreshold)
            {
                state.Player.Focus -= FocusThreshold;
                output.Add(SimulationEventType.ResourceChanged, sourceId,
                    "resource=focus;reason=conversion;current=" + state.Player.Focus, -FocusThreshold);
                DamageEnemy(state, sourceId, FocusDamage, "focus_conversion", output);
            }
        }

        private static void AddToxic(RunState state, int amount, ContentId sourceId, EventBatch output)
        {
            state.Player.Toxic += amount;
            output.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=toxic;current=" + state.Player.Toxic, amount);
            while (state.Player.Toxic >= ToxicThreshold)
            {
                state.Player.Toxic -= ToxicThreshold;
                output.Add(SimulationEventType.ResourceChanged, sourceId,
                    "resource=toxic;reason=conversion;current=" + state.Player.Toxic, -ToxicThreshold);
                DamageEnemy(state, sourceId, ToxicDamage, "toxic_conversion", output);
                var before = state.Enemy.PoisonStacks;
                state.Enemy.PoisonStacks = Math.Min(PoisonStackCap, state.Enemy.PoisonStacks + 1);
                if (state.Enemy.PoisonStacks > before)
                {
                    output.Add(
                        SimulationEventType.StatusAdded,
                        CombatContentIds.Poison,
                        "target=enemy;stacks=" + state.Enemy.PoisonStacks,
                        1,
                        null,
                        null,
                        state.Enemy.DefinitionId);
                }
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
                DamageEnemy(state, CombatContentIds.Poison, stacks * PoisonDamagePerStack, "enemy_response_start", events);
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
            var intentIndex = PositiveModulo(state.Enemy.IntentIndex, enemy.IntentCycle.Count);
            var intent = enemy.IntentCycle[intentIndex];
            events.Add(
                SimulationEventType.EnemyIntentStarted,
                intent.Id,
                "execute;telegraph=" + intent.TelegraphKey,
                0,
                null,
                null,
                enemy.Id);

            foreach (var effect in intent.Effects)
            {
                if (effect.Type == IntentEffectType.DamagePlayer)
                    DamagePlayer(state, intent.Id, effect.Amount, events);
                else if (effect.Type == IntentEffectType.ApplyBoardStatus)
                    ApplyBoardStatus(state, intent.Id, effect, events);
                else if (effect.Type == IntentEffectType.DrainResources)
                    DrainResources(state, intent.Id, effect, events);
            }

            state.Enemy.IntentIndex = (intentIndex + 1) % enemy.IntentCycle.Count;
            if (state.Player.Health <= 0)
            {
                events.Add(SimulationEventType.RunEnded, enemy.Id, "defeat", 0);
                return false;
            }
            events.Append(BoardSimulation.EnsurePlayable(state, boardCatalog));
            var nextIntent = enemy.IntentCycle[state.Enemy.IntentIndex];
            events.Add(
                SimulationEventType.EnemyIntentTelegraphed,
                nextIntent.Id,
                "telegraph=" + nextIntent.TelegraphKey,
                0,
                null,
                null,
                enemy.Id);
            return false;
        }

        private static void DamageEnemy(
            RunState state,
            ContentId sourceId,
            int amount,
            string detail,
            EventBatch output)
        {
            if (amount <= 0 || state.Enemy.Health <= 0) return;
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

        private static void ResolveVictory(RunState state, ICombatContentCatalog catalog, EventBatch events)
        {
            var enemy = catalog.GetEnemy(state.Enemy.DefinitionId);
            state.Enemy.Health = 0;
            events.Add(SimulationEventType.EnemyDefeated, enemy.Id, "victory", 1);
            events.Add(SimulationEventType.XPGranted, enemy.Id, "encounter_reward", enemy.RewardXp);
            var restored = Math.Min(4, PlayerState.MaxHealth - state.Player.Health);
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

        private static void AddVoltProgress(PlayerState player, int amount, EventBatch events)
        {
            player.VoltClearProgress += amount;
            events.Add(SimulationEventType.ResourceChanged, BoardContentIds.Volt,
                "resource=volt_progress;current=" + player.VoltClearProgress, amount);
            while (player.VoltClearProgress >= 3)
            {
                player.VoltClearProgress -= 3;
                events.Add(SimulationEventType.ResourceChanged, BoardContentIds.Volt,
                    "resource=volt_progress;reason=conversion;current=" + player.VoltClearProgress, -3);
                ReduceLeftCooldown(player, 1, events);
            }
        }

        private static void ReduceLeftCooldown(PlayerState player, int amount, EventBatch events)
        {
            if (player.SkillCooldowns == null || player.SkillCooldowns.Count == 0) return;
            ReduceCooldown(player.SkillCooldowns[0], amount, "volt_progress", events);
        }

        private static void ReduceAllCooldowns(PlayerState player, int amount, string reason, EventBatch events)
        {
            if (player.SkillCooldowns == null) return;
            foreach (var cooldown in player.SkillCooldowns)
                ReduceCooldown(cooldown, amount, reason, events);
        }

        private static void ReduceCooldown(
            SkillCooldownState cooldown,
            int amount,
            string reason,
            EventBatch events)
        {
            if (cooldown == null) return;
            var before = cooldown.RemainingTurns;
            cooldown.RemainingTurns = Math.Max(0, before - amount);
            if (events != null && cooldown.RemainingTurns != before)
                events.Add(SimulationEventType.CooldownChanged, cooldown.SkillId,
                    "reason=" + reason + ";current=" + cooldown.RemainingTurns,
                    cooldown.RemainingTurns - before);
        }

        private static void TickSkillCooldowns(PlayerState player, EventBatch events)
        {
            ReduceAllCooldowns(player, 1, "player_turn", events);
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
    }
}
