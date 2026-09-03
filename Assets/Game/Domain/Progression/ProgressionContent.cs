using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Ids;

namespace ThreeInARow.Domain.Progression
{
    public static class ProgressionContentIds
    {
        public static readonly ContentId SystemProgression = "system.progression";

        public static readonly ContentId Kindling = "skill.kindling";
        public static readonly ContentId Backdraft = "skill.backdraft";
        public static readonly ContentId FlowState = "skill.flow_state";
        public static readonly ContentId Undertow = "skill.undertow";
        public static readonly ContentId Corrosive = "skill.corrosive";
        public static readonly ContentId Overcharge = "skill.overcharge";

        public static readonly ContentId Sunder = "skill.sunder";
        public static readonly ContentId Cleanse = "skill.cleanse";
        public static readonly ContentId Catalyze = "skill.catalyze";
    }

    public enum SkillSlotType
    {
        Passive,
        Active
    }

    public enum SkillTargetPolicy
    {
        None,
        UpToThreeStatusGems
    }

    public enum PassiveModifierType
    {
        EmberClearDamage,
        SparkShield,
        FocusConversionDamage,
        FocusConversionLeftCooldown,
        PoisonDamagePerStack,
        VoltClearThreshold
    }

    public enum ActiveEffectType
    {
        DamageEnemy,
        RemoveBoardStatuses,
        CatalyzeResources
    }

    public sealed class PassiveModifierDefinition
    {
        public readonly PassiveModifierType Type;
        public readonly int Amount;

        public PassiveModifierDefinition(PassiveModifierType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    public sealed class ActiveEffectDefinition
    {
        public readonly ActiveEffectType Type;
        public readonly int Amount;

        public ActiveEffectDefinition(ActiveEffectType type, int amount = 0)
        {
            Type = type;
            Amount = amount;
        }
    }

    public sealed class SkillDefinition
    {
        public readonly ContentId Id;
        public readonly string DisplayKey;
        public readonly SkillSlotType SlotType;
        public readonly ContentId PrerequisiteId;
        public readonly bool HasPrerequisite;
        public readonly bool CanBeLevelUpReward;
        public readonly int Cooldown;
        public readonly SkillTargetPolicy TargetPolicy;
        public readonly IReadOnlyList<PassiveModifierDefinition> PassiveModifiers;
        public readonly IReadOnlyList<ActiveEffectDefinition> ActiveEffects;

        private SkillDefinition(
            ContentId id,
            string displayKey,
            SkillSlotType slotType,
            ContentId prerequisiteId,
            bool hasPrerequisite,
            bool canBeLevelUpReward,
            int cooldown,
            SkillTargetPolicy targetPolicy,
            PassiveModifierDefinition[] passiveModifiers,
            ActiveEffectDefinition[] activeEffects)
        {
            Id = id;
            DisplayKey = displayKey ?? string.Empty;
            SlotType = slotType;
            PrerequisiteId = prerequisiteId;
            HasPrerequisite = hasPrerequisite;
            CanBeLevelUpReward = canBeLevelUpReward;
            Cooldown = cooldown;
            TargetPolicy = targetPolicy;
            PassiveModifiers = passiveModifiers ?? new PassiveModifierDefinition[0];
            ActiveEffects = activeEffects ?? new ActiveEffectDefinition[0];
        }

        public static SkillDefinition Passive(
            ContentId id,
            string displayKey,
            PassiveModifierDefinition modifier,
            ContentId? prerequisiteId = null)
        {
            return new SkillDefinition(
                id,
                displayKey,
                SkillSlotType.Passive,
                prerequisiteId ?? default(ContentId),
                prerequisiteId.HasValue,
                true,
                0,
                SkillTargetPolicy.None,
                new[] { modifier },
                null);
        }

        public static SkillDefinition Active(
            ContentId id,
            string displayKey,
            int cooldown,
            SkillTargetPolicy targetPolicy,
            bool canBeLevelUpReward,
            params ActiveEffectDefinition[] effects)
        {
            if (cooldown <= 0) throw new ArgumentOutOfRangeException(nameof(cooldown));
            return new SkillDefinition(
                id,
                displayKey,
                SkillSlotType.Active,
                default(ContentId),
                false,
                canBeLevelUpReward,
                cooldown,
                targetPolicy,
                null,
                effects);
        }
    }

    public interface IProgressionContentCatalog
    {
        IReadOnlyList<SkillDefinition> Skills { get; }
        SkillDefinition GetSkill(ContentId skillId);
    }

    /// <summary>Immutable MVP progression content; resolvers operate on modifier/effect data, not skill IDs.</summary>
    public sealed class MvpProgressionContentCatalog : IProgressionContentCatalog
    {
        private readonly List<SkillDefinition> _skills;
        private readonly Dictionary<ContentId, SkillDefinition> _byId;

        public static readonly MvpProgressionContentCatalog Instance = new MvpProgressionContentCatalog();

        private MvpProgressionContentCatalog()
        {
            _skills = new List<SkillDefinition>
            {
                SkillDefinition.Passive(ProgressionContentIds.Kindling, "skill.kindling.name",
                    new PassiveModifierDefinition(PassiveModifierType.EmberClearDamage, 1)),
                SkillDefinition.Passive(ProgressionContentIds.Backdraft, "skill.backdraft.name",
                    new PassiveModifierDefinition(PassiveModifierType.SparkShield, 6),
                    ProgressionContentIds.Kindling),
                SkillDefinition.Passive(ProgressionContentIds.FlowState, "skill.flow_state.name",
                    new PassiveModifierDefinition(PassiveModifierType.FocusConversionDamage, 1)),
                SkillDefinition.Passive(ProgressionContentIds.Undertow, "skill.undertow.name",
                    new PassiveModifierDefinition(PassiveModifierType.FocusConversionLeftCooldown, 1),
                    ProgressionContentIds.FlowState),
                SkillDefinition.Passive(ProgressionContentIds.Corrosive, "skill.corrosive.name",
                    new PassiveModifierDefinition(PassiveModifierType.PoisonDamagePerStack, 1)),
                SkillDefinition.Passive(ProgressionContentIds.Overcharge, "skill.overcharge.name",
                    new PassiveModifierDefinition(PassiveModifierType.VoltClearThreshold, -1)),

                SkillDefinition.Active(ProgressionContentIds.Sunder, "skill.sunder.name", 4,
                    SkillTargetPolicy.None, false,
                    new ActiveEffectDefinition(ActiveEffectType.DamageEnemy, 14)),
                SkillDefinition.Active(ProgressionContentIds.Cleanse, "skill.cleanse.name", 5,
                    SkillTargetPolicy.UpToThreeStatusGems, false,
                    new ActiveEffectDefinition(ActiveEffectType.RemoveBoardStatuses, 3)),
                SkillDefinition.Active(ProgressionContentIds.Catalyze, "skill.catalyze.name", 5,
                    SkillTargetPolicy.None, true,
                    new ActiveEffectDefinition(ActiveEffectType.CatalyzeResources, 4))
            };

            _byId = new Dictionary<ContentId, SkillDefinition>();
            foreach (var skill in _skills) _byId.Add(skill.Id, skill);
        }

        public IReadOnlyList<SkillDefinition> Skills => _skills;

        public SkillDefinition GetSkill(ContentId skillId)
        {
            SkillDefinition skill;
            if (!_byId.TryGetValue(skillId, out skill))
                throw new KeyNotFoundException("Unknown skill content ID: " + skillId);
            return skill;
        }
    }
}
