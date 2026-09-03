using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Progression
{
    public enum RewardSelectionRejectionReason
    {
        None,
        NoPendingChoice,
        RewardNotOffered
    }

    public enum EquipSkillRejectionReason
    {
        None,
        ChoicePending,
        CombatTurnPending,
        EncounterInProgress,
        InvalidSlot,
        SkillNotLearned,
        SkillIsNotActive
    }

    public enum ActiveSkillRejectionReason
    {
        None,
        SkillWindowClosed,
        SkillNotLearned,
        SkillNotEquipped,
        SkillOnCooldown,
        InvalidTargets,
        NoEffectAvailable,
        UnknownSkill
    }

    public sealed class RewardSelectionResult
    {
        public readonly bool Accepted;
        public readonly RewardSelectionRejectionReason RejectionReason;
        public readonly EventBatch Events;

        private RewardSelectionResult(bool accepted, RewardSelectionRejectionReason rejectionReason, EventBatch events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
        }

        public static RewardSelectionResult Reject(RewardSelectionRejectionReason reason)
        {
            return new RewardSelectionResult(false, reason, new EventBatch());
        }

        public static RewardSelectionResult Accept(EventBatch events)
        {
            return new RewardSelectionResult(true, RewardSelectionRejectionReason.None, events);
        }
    }

    public sealed class EquipSkillResult
    {
        public readonly bool Accepted;
        public readonly EquipSkillRejectionReason RejectionReason;
        public readonly EventBatch Events;

        private EquipSkillResult(bool accepted, EquipSkillRejectionReason rejectionReason, EventBatch events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
        }

        public static EquipSkillResult Reject(EquipSkillRejectionReason reason)
        {
            return new EquipSkillResult(false, reason, new EventBatch());
        }

        public static EquipSkillResult Accept(EventBatch events)
        {
            return new EquipSkillResult(true, EquipSkillRejectionReason.None, events);
        }
    }

    public sealed class ActiveSkillResult
    {
        public readonly bool Accepted;
        public readonly ActiveSkillRejectionReason RejectionReason;
        public readonly EventBatch Events;
        public readonly bool EncounterWon;

        private ActiveSkillResult(
            bool accepted,
            ActiveSkillRejectionReason rejectionReason,
            EventBatch events,
            bool encounterWon)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
            EncounterWon = encounterWon;
        }

        public static ActiveSkillResult Reject(ActiveSkillRejectionReason reason)
        {
            return new ActiveSkillResult(false, reason, new EventBatch(), false);
        }

        public static ActiveSkillResult Accept(EventBatch events, bool encounterWon)
        {
            return new ActiveSkillResult(true, ActiveSkillRejectionReason.None, events, encounterWon);
        }
    }

    /// <summary>
    /// Owns run-level XP, deterministic reward choices, loadout changes between encounters, and
    /// active-skill command validation. Combat effects are applied through CombatSimulation helpers.
    /// </summary>
    public static class ProgressionSimulation
    {
        private static readonly int[] LevelThresholds = { 2, 3, 4 };
        private const int ActiveSlotCount = 2;

        public static void InitializeRun(RunState state, IProgressionContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpProgressionContentCatalog.Instance;
            if (state.Level < 1) state.Level = 1;
            if (state.SelectedSkillIds == null) state.SelectedSkillIds = new List<ContentId>();
            if (state.Player == null) state.Player = new PlayerState();
            if (state.Player.EquippedActiveSkillIds == null)
                state.Player.EquippedActiveSkillIds = new List<ContentId>();
            if (state.Player.SkillCooldowns == null)
                state.Player.SkillCooldowns = new List<SkillCooldownState>();
            if (state.PendingChoice == null) state.PendingChoice = new PendingChoiceState();
            if (state.PendingCombatTurn == null) state.PendingCombatTurn = new PendingCombatTurnState();
            if (state.PendingCombatTurn.SkillIdsUsed == null)
                state.PendingCombatTurn.SkillIdsUsed = new List<ContentId>();
            if (state.RandomStreams == null || state.RandomStreams.Count == 0)
                state.RandomStreams = RandomStreams.Create(state.Seed);

            LearnStarter(state, ProgressionContentIds.Sunder);
            LearnStarter(state, ProgressionContentIds.Cleanse);
            EnsureCooldown(state.Player, ProgressionContentIds.Sunder);
            EnsureCooldown(state.Player, ProgressionContentIds.Cleanse);
            EnsureEquipped(state.Player, ProgressionContentIds.Sunder);
            EnsureEquipped(state.Player, ProgressionContentIds.Cleanse);

            if (state.Player.EquippedActiveSkillIds.Count > ActiveSlotCount)
                state.Player.EquippedActiveSkillIds.RemoveRange(
                    ActiveSlotCount,
                    state.Player.EquippedActiveSkillIds.Count - ActiveSlotCount);
        }

        public static void GrantExperience(
            RunState state,
            int amount,
            ContentId sourceId,
            EventBatch events,
            IProgressionContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (amount <= 0) return;
            catalog = catalog ?? MvpProgressionContentCatalog.Instance;
            InitializeRun(state, catalog);
            state.Experience += amount;
            events.Add(
                SimulationEventType.XPGranted,
                sourceId,
                "current=" + state.Experience,
                amount);
            OfferNextLevelIfEligible(state, events, catalog);
        }

        public static RewardSelectionResult SelectReward(
            RunState state,
            SelectRewardCommand command,
            IProgressionContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            catalog = catalog ?? MvpProgressionContentCatalog.Instance;
            InitializeRun(state, catalog);
            if (!state.PendingChoice.IsPending)
                return RewardSelectionResult.Reject(RewardSelectionRejectionReason.NoPendingChoice);
            if (!ProgressionRules.Contains(state.PendingChoice.OptionIds, command.RewardId))
                return RewardSelectionResult.Reject(RewardSelectionRejectionReason.RewardNotOffered);

            var definition = catalog.GetSkill(command.RewardId);
            state.SelectedSkillIds.Add(definition.Id);
            if (definition.SlotType == SkillSlotType.Active)
                EnsureCooldown(state.Player, definition.Id);

            var choiceId = state.PendingChoice.ChoiceId;
            var events = new EventBatch();
            events.Add(
                SimulationEventType.SkillChosen,
                definition.Id,
                "level=" + state.PendingChoice.Level,
                1,
                null,
                null,
                choiceId);
            ClearPendingChoice(state.PendingChoice);
            OfferNextLevelIfEligible(state, events, catalog);
            return RewardSelectionResult.Accept(events);
        }

        public static EquipSkillResult EquipActiveSkill(
            RunState state,
            EquipSkillCommand command,
            IProgressionContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            catalog = catalog ?? MvpProgressionContentCatalog.Instance;
            InitializeRun(state, catalog);
            if (state.PendingChoice.IsPending)
                return EquipSkillResult.Reject(EquipSkillRejectionReason.ChoicePending);
            if (state.PendingCombatTurn.AwaitingEnemyResponse)
                return EquipSkillResult.Reject(EquipSkillRejectionReason.CombatTurnPending);
            if (state.Enemy != null && state.Enemy.Health > 0)
                return EquipSkillResult.Reject(EquipSkillRejectionReason.EncounterInProgress);
            if (command.SlotIndex < 0 || command.SlotIndex >= ActiveSlotCount)
                return EquipSkillResult.Reject(EquipSkillRejectionReason.InvalidSlot);
            if (!ProgressionRules.HasSkill(state, command.SkillId))
                return EquipSkillResult.Reject(EquipSkillRejectionReason.SkillNotLearned);

            SkillDefinition definition;
            try
            {
                definition = catalog.GetSkill(command.SkillId);
            }
            catch (KeyNotFoundException)
            {
                return EquipSkillResult.Reject(EquipSkillRejectionReason.SkillNotLearned);
            }
            if (definition.SlotType != SkillSlotType.Active)
                return EquipSkillResult.Reject(EquipSkillRejectionReason.SkillIsNotActive);

            var otherSlot = command.SlotIndex == 0 ? 1 : 0;
            if (state.Player.EquippedActiveSkillIds.Count > otherSlot &&
                state.Player.EquippedActiveSkillIds[otherSlot].Equals(command.SkillId))
                return EquipSkillResult.Reject(EquipSkillRejectionReason.InvalidSlot);

            while (state.Player.EquippedActiveSkillIds.Count < ActiveSlotCount)
                state.Player.EquippedActiveSkillIds.Add(command.SkillId);
            var previous = state.Player.EquippedActiveSkillIds[command.SlotIndex];
            state.Player.EquippedActiveSkillIds[command.SlotIndex] = command.SkillId;
            EnsureCooldown(state.Player, command.SkillId);

            var events = new EventBatch();
            events.Add(
                SimulationEventType.SkillEquipped,
                command.SkillId,
                "slot=" + command.SlotIndex,
                command.SlotIndex,
                null,
                null,
                previous);
            return EquipSkillResult.Accept(events);
        }

        public static ActiveSkillResult UseActiveSkill(
            RunState state,
            UseSkillCommand command,
            IProgressionContentCatalog progressionCatalog = null,
            ICombatContentCatalog combatCatalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            progressionCatalog = progressionCatalog ?? MvpProgressionContentCatalog.Instance;
            combatCatalog = combatCatalog ?? MvpCombatContentCatalog.Instance;
            InitializeRun(state, progressionCatalog);
            if (!state.PendingCombatTurn.AwaitingEnemyResponse || state.Enemy == null || state.Enemy.Health <= 0)
                return ActiveSkillResult.Reject(ActiveSkillRejectionReason.SkillWindowClosed);

            SkillDefinition definition;
            try
            {
                definition = progressionCatalog.GetSkill(command.SkillId);
            }
            catch (KeyNotFoundException)
            {
                return ActiveSkillResult.Reject(ActiveSkillRejectionReason.UnknownSkill);
            }
            if (definition.SlotType != SkillSlotType.Active || !ProgressionRules.HasSkill(state, definition.Id))
                return ActiveSkillResult.Reject(ActiveSkillRejectionReason.SkillNotLearned);
            if (!ProgressionRules.IsEquipped(state.Player, definition.Id))
                return ActiveSkillResult.Reject(ActiveSkillRejectionReason.SkillNotEquipped);

            var cooldown = ProgressionRules.FindCooldown(state.Player, definition.Id);
            if (cooldown == null || cooldown.RemainingTurns > 0)
                return ActiveSkillResult.Reject(ActiveSkillRejectionReason.SkillOnCooldown);

            List<BoardGemState> cleanseTargets;
            var targetValidation = ValidateTargets(state, command, definition, out cleanseTargets);
            if (targetValidation != ActiveSkillRejectionReason.None)
                return ActiveSkillResult.Reject(targetValidation);

            var events = new EventBatch();
            events.Add(SimulationEventType.SkillUsed, definition.Id,
                "targetPolicy=" + definition.TargetPolicy, 1);
            cooldown.RemainingTurns = definition.Cooldown;
            events.Add(SimulationEventType.CooldownChanged, definition.Id,
                "reason=skill_used;current=" + cooldown.RemainingTurns,
                definition.Cooldown);
            state.PendingCombatTurn.SkillIdsUsed.Add(definition.Id);

            foreach (var effect in definition.ActiveEffects)
            {
                if (effect.Type == ActiveEffectType.DamageEnemy)
                    CombatSimulation.ApplySkillDamage(state, definition.Id, effect.Amount, events);
                else if (effect.Type == ActiveEffectType.RemoveBoardStatuses)
                    RemoveBoardStatuses(cleanseTargets, definition.Id, events);
                else if (effect.Type == ActiveEffectType.CatalyzeResources)
                    CatalyzeResources(state, definition.Id, effect.Amount, events);
            }

            var won = state.Enemy.Health <= 0;
            if (won) CombatSimulation.FinishTurnAfterSkillVictory(state, combatCatalog, events);
            return ActiveSkillResult.Accept(events, won);
        }

        private static void OfferNextLevelIfEligible(
            RunState state,
            EventBatch events,
            IProgressionContentCatalog catalog)
        {
            if (state.PendingChoice.IsPending) return;
            var nextLevel = state.Level + 1;
            var thresholdIndex = nextLevel - 2;
            if (thresholdIndex < 0 || thresholdIndex >= LevelThresholds.Length ||
                state.Experience < LevelThresholds[thresholdIndex]) return;

            state.Level = nextLevel;
            var candidates = GetEligibleRewards(state, catalog);
            var options = SampleWithoutReplacement(state, candidates, 3);
            if (options.Count == 0) return;

            state.PendingChoice.ChoiceId = (ContentId)("choice.level_up." + state.Level);
            state.PendingChoice.Level = state.Level;
            state.PendingChoice.OptionIds = options;
            events.Add(
                SimulationEventType.LevelUpOffered,
                state.PendingChoice.ChoiceId,
                "level=" + state.Level,
                options.Count);
        }

        private static List<SkillDefinition> GetEligibleRewards(
            RunState state,
            IProgressionContentCatalog catalog)
        {
            var result = new List<SkillDefinition>();
            foreach (var skill in catalog.Skills)
            {
                if (!skill.CanBeLevelUpReward || ProgressionRules.HasSkill(state, skill.Id)) continue;
                if (skill.HasPrerequisite && !ProgressionRules.HasSkill(state, skill.PrerequisiteId)) continue;
                result.Add(skill);
            }
            return result;
        }

        private static List<ContentId> SampleWithoutReplacement(
            RunState state,
            List<SkillDefinition> candidates,
            int maximum)
        {
            var result = new List<ContentId>();
            if (candidates.Count == 0) return result;
            var random = RandomStreams.Restore(RandomStream.RewardSampling, state.RandomStreams);
            while (result.Count < maximum && candidates.Count > 0)
            {
                var index = random.NextInt(candidates.Count);
                result.Add(candidates[index].Id);
                candidates.RemoveAt(index);
            }
            RandomStreams.Store(RandomStream.RewardSampling, random, state.RandomStreams);
            return result;
        }

        private static ActiveSkillRejectionReason ValidateTargets(
            RunState state,
            UseSkillCommand command,
            SkillDefinition definition,
            out List<BoardGemState> selected)
        {
            selected = new List<BoardGemState>();
            if (definition.TargetPolicy == SkillTargetPolicy.None)
            {
                if (command.Targets != null && command.Targets.Count > 0)
                    return ActiveSkillRejectionReason.InvalidTargets;
                if (definition.ActiveEffects.Count > 0 &&
                    definition.ActiveEffects[0].Type == ActiveEffectType.CatalyzeResources &&
                    state.Player.Focus <= 0 && (state.Player.Toxic < 2 || state.Enemy.PoisonStacks >= 3))
                    return ActiveSkillRejectionReason.NoEffectAvailable;
                return ActiveSkillRejectionReason.None;
            }

            var eligible = FindStatusGems(state.Board);
            if (eligible.Count == 0) return ActiveSkillRejectionReason.NoEffectAvailable;
            if ((command.Targets == null || command.Targets.Count == 0) && eligible.Count <= 3)
            {
                selected.AddRange(eligible);
                return ActiveSkillRejectionReason.None;
            }
            if (command.Targets == null || command.Targets.Count == 0 || command.Targets.Count > 3)
                return ActiveSkillRejectionReason.InvalidTargets;

            foreach (var cell in command.Targets)
            {
                BoardGemState match = null;
                foreach (var gem in eligible)
                    if (gem.Cell.Equals(cell)) { match = gem; break; }
                if (match == null || selected.Contains(match))
                    return ActiveSkillRejectionReason.InvalidTargets;
                selected.Add(match);
            }
            return ActiveSkillRejectionReason.None;
        }

        private static List<BoardGemState> FindStatusGems(BoardState board)
        {
            var result = new List<BoardGemState>();
            if (board == null || board.Gems == null) return result;
            foreach (var gem in board.Gems)
            {
                if (gem == null) continue;
                if (ProgressionRules.Contains(gem.StatusIds, BoardContentIds.Frozen) ||
                    ProgressionRules.Contains(gem.StatusIds, BoardContentIds.Cracked) ||
                    ProgressionRules.Contains(gem.StatusIds, BoardContentIds.Anchored))
                    result.Add(gem);
            }
            return result;
        }

        private static void RemoveBoardStatuses(
            List<BoardGemState> targets,
            ContentId sourceId,
            EventBatch events)
        {
            foreach (var gem in targets)
            {
                RemoveBoardStatus(gem, BoardContentIds.Frozen, sourceId, events);
                RemoveBoardStatus(gem, BoardContentIds.Cracked, sourceId, events);
                RemoveBoardStatus(gem, BoardContentIds.Anchored, sourceId, events);
            }
        }

        private static void RemoveBoardStatus(
            BoardGemState gem,
            ContentId statusId,
            ContentId sourceId,
            EventBatch events)
        {
            if (!Remove(gem.StatusIds, statusId)) return;
            if (gem.StatusDurations != null)
            {
                for (var index = gem.StatusDurations.Count - 1; index >= 0; index--)
                {
                    var duration = gem.StatusDurations[index];
                    if (duration != null && duration.StatusId.Equals(statusId))
                        gem.StatusDurations.RemoveAt(index);
                }
            }
            events.Add(SimulationEventType.StatusRemoved, statusId,
                "reason=cleanse", 1, gem.Cell, null, sourceId);
        }

        private static void CatalyzeResources(
            RunState state,
            ContentId sourceId,
            int maximumPerResource,
            EventBatch events)
        {
            var focusSpent = Math.Min(maximumPerResource, state.Player.Focus);
            if (focusSpent > 0)
            {
                state.Player.Focus -= focusSpent;
                events.Add(SimulationEventType.ResourceChanged, sourceId,
                    "resource=focus;reason=catalyze;current=" + state.Player.Focus, -focusSpent);
                CombatSimulation.ApplySkillDamage(state, sourceId, focusSpent * 3, events);
            }

            var poisonCapacity = Math.Max(0, 3 - state.Enemy.PoisonStacks);
            var toxicSpent = Math.Min(maximumPerResource, state.Player.Toxic);
            toxicSpent = Math.Min(toxicSpent - toxicSpent % 2, poisonCapacity * 2);
            if (toxicSpent <= 0) return;
            state.Player.Toxic -= toxicSpent;
            events.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=toxic;reason=catalyze;current=" + state.Player.Toxic, -toxicSpent);
            CombatSimulation.AddEnemyPoison(state, sourceId, toxicSpent / 2, events);
        }

        private static void LearnStarter(RunState state, ContentId skillId)
        {
            if (!ProgressionRules.HasSkill(state, skillId)) state.SelectedSkillIds.Add(skillId);
        }

        private static void EnsureEquipped(PlayerState player, ContentId skillId)
        {
            if (player.EquippedActiveSkillIds.Count >= ActiveSlotCount ||
                ProgressionRules.Contains(player.EquippedActiveSkillIds, skillId)) return;
            player.EquippedActiveSkillIds.Add(skillId);
        }

        private static void EnsureCooldown(PlayerState player, ContentId skillId)
        {
            if (ProgressionRules.FindCooldown(player, skillId) != null) return;
            player.SkillCooldowns.Add(new SkillCooldownState { SkillId = skillId, RemainingTurns = 0 });
        }

        private static void ClearPendingChoice(PendingChoiceState choice)
        {
            choice.ChoiceId = "choice.none";
            choice.Level = 0;
            choice.OptionIds = new List<ContentId>();
        }

        private static bool Remove(List<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return false;
            for (var index = ids.Count - 1; index >= 0; index--)
            {
                if (!ids[index].Equals(wanted)) continue;
                ids.RemoveAt(index);
                return true;
            }
            return false;
        }
    }
}
