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
        public static readonly ContentId Cinderwake = "skill.cinderwake";
        public static readonly ContentId Reservoir = "skill.reservoir";
        public static readonly ContentId Concentrate = "skill.concentrate";
        public static readonly ContentId Contagion = "skill.contagion";
        public static readonly ContentId StaticGuard = "skill.static_guard";
        public static readonly ContentId LiveWire = "skill.live_wire";

        public static readonly ContentId Sunder = "skill.sunder";
        public static readonly ContentId Cleanse = "skill.cleanse";
        public static readonly ContentId Catalyze = "skill.catalyze";
        public static readonly ContentId Aegis = "skill.aegis";
        public static readonly ContentId Infuse = "skill.infuse";

        public static readonly ContentId TemperedCore = "skill.keystone.tempered_core";
        public static readonly ContentId PrismaticStart = "skill.keystone.prismatic_start";
        public static readonly ContentId RapidCasting = "skill.keystone.rapid_casting";
        public static readonly ContentId HardLight = "skill.keystone.hard_light";
    }

    public enum SkillSlotType
    {
        Passive,
        Active
    }

    public enum SkillTargetPolicy
    {
        None,
        UpToThreeStatusGems,
        OneNormalGem
    }

    public enum PassiveModifierType
    {
        EmberClearDamage,
        SparkShield,
        FocusConversionDamage,
        FocusConversionLeftCooldown,
        PoisonDamagePerStack,
        VoltClearThreshold,
        SparkFirstDamage,
        FocusConversionShield,
        ToxicThreshold,
        LargeVenomMatchToxic,
        CooldownReductionShield,
        ChargeCooldownReduction,
        VictoryHeal,
        PrismaticStart,
        ActiveCooldownStartReduction,
        ShieldExpiryDamage
    }

    public enum ActiveEffectType
    {
        DamageEnemy,
        RemoveBoardStatuses,
        CatalyzeResources,
        GainShield,
        InfuseNormalGem
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
        public readonly string BranchTag;
        public readonly IReadOnlyList<string> SynergyTags;
        public readonly bool IsEliteKeystone;

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
            : this(id, displayKey, slotType, prerequisiteId, hasPrerequisite, canBeLevelUpReward,
                cooldown, targetPolicy, passiveModifiers, activeEffects, string.Empty, null, false) { }

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
            ActiveEffectDefinition[] activeEffects,
            string branchTag,
            string[] synergyTags,
            bool isEliteKeystone)
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
            BranchTag = branchTag ?? string.Empty;
            SynergyTags = synergyTags ?? new string[0];
            IsEliteKeystone = isEliteKeystone;
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

        public static SkillDefinition BranchPassive(
            ContentId id,
            string displayKey,
            string branchTag,
            string[] synergyTags,
            PassiveModifierDefinition modifier,
            ContentId? prerequisiteId = null)
        {
            return new SkillDefinition(id, displayKey, SkillSlotType.Passive,
                prerequisiteId ?? default(ContentId), prerequisiteId.HasValue, true, 0,
                SkillTargetPolicy.None, new[] { modifier }, null, branchTag, synergyTags, false);
        }

        public static SkillDefinition RewardActive(
            ContentId id,
            string displayKey,
            int cooldown,
            SkillTargetPolicy targetPolicy,
            string[] synergyTags,
            params ActiveEffectDefinition[] effects)
        {
            return new SkillDefinition(id, displayKey, SkillSlotType.Active, default(ContentId), false,
                true, cooldown, targetPolicy, null, effects, "generic", synergyTags, false);
        }

        public static SkillDefinition EliteKeystone(
            ContentId id,
            string displayKey,
            PassiveModifierDefinition modifier,
            params string[] synergyTags)
        {
            return new SkillDefinition(id, displayKey, SkillSlotType.Passive, default(ContentId), false,
                false, 0, SkillTargetPolicy.None, new[] { modifier }, null, "keystone", synergyTags, true);
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
                SkillDefinition.BranchPassive(ProgressionContentIds.Kindling, "skill.kindling.name", "ember", new[] { "Искра" },
                    new PassiveModifierDefinition(PassiveModifierType.EmberClearDamage, 1)),
                SkillDefinition.BranchPassive(ProgressionContentIds.Backdraft, "skill.backdraft.name", "ember", new[] { "Искра", "Щит" },
                    new PassiveModifierDefinition(PassiveModifierType.SparkShield, 6),
                    ProgressionContentIds.Kindling),
                SkillDefinition.BranchPassive(ProgressionContentIds.Cinderwake, "skill.cinderwake.name", "ember", new[] { "Искра" },
                    new PassiveModifierDefinition(PassiveModifierType.SparkFirstDamage, 8), ProgressionContentIds.Backdraft),
                SkillDefinition.BranchPassive(ProgressionContentIds.FlowState, "skill.flow_state.name", "tide", new[] { "Концентрация" },
                    new PassiveModifierDefinition(PassiveModifierType.FocusConversionDamage, 1)),
                SkillDefinition.BranchPassive(ProgressionContentIds.Undertow, "skill.undertow.name", "tide", new[] { "Концентрация", "Перезарядка" },
                    new PassiveModifierDefinition(PassiveModifierType.FocusConversionLeftCooldown, 1),
                    ProgressionContentIds.FlowState),
                SkillDefinition.BranchPassive(ProgressionContentIds.Reservoir, "skill.reservoir.name", "tide", new[] { "Концентрация", "Щит" },
                    new PassiveModifierDefinition(PassiveModifierType.FocusConversionShield, 2), ProgressionContentIds.FlowState),
                SkillDefinition.BranchPassive(ProgressionContentIds.Corrosive, "skill.corrosive.name", "venom", new[] { "Яд" },
                    new PassiveModifierDefinition(PassiveModifierType.PoisonDamagePerStack, 1)),
                SkillDefinition.BranchPassive(ProgressionContentIds.Concentrate, "skill.concentrate.name", "venom", new[] { "Яд" },
                    new PassiveModifierDefinition(PassiveModifierType.ToxicThreshold, -1)),
                SkillDefinition.BranchPassive(ProgressionContentIds.Contagion, "skill.contagion.name", "venom", new[] { "Яд" },
                    new PassiveModifierDefinition(PassiveModifierType.LargeVenomMatchToxic, 2), ProgressionContentIds.Concentrate),
                SkillDefinition.BranchPassive(ProgressionContentIds.Overcharge, "skill.overcharge.name", "volt", new[] { "Перезарядка" },
                    new PassiveModifierDefinition(PassiveModifierType.VoltClearThreshold, -1)),
                SkillDefinition.BranchPassive(ProgressionContentIds.StaticGuard, "skill.static_guard.name", "volt", new[] { "Перезарядка", "Щит" },
                    new PassiveModifierDefinition(PassiveModifierType.CooldownReductionShield, 2)),
                SkillDefinition.BranchPassive(ProgressionContentIds.LiveWire, "skill.live_wire.name", "volt", new[] { "Перезарядка" },
                    new PassiveModifierDefinition(PassiveModifierType.ChargeCooldownReduction, 1), ProgressionContentIds.StaticGuard),

                SkillDefinition.Active(ProgressionContentIds.Sunder, "skill.sunder.name", 4,
                    SkillTargetPolicy.None, false,
                    new ActiveEffectDefinition(ActiveEffectType.DamageEnemy, 14)),
                SkillDefinition.Active(ProgressionContentIds.Cleanse, "skill.cleanse.name", 5,
                    SkillTargetPolicy.UpToThreeStatusGems, false,
                    new ActiveEffectDefinition(ActiveEffectType.RemoveBoardStatuses, 3)),
                SkillDefinition.RewardActive(ProgressionContentIds.Catalyze, "skill.catalyze.name", 5,
                    SkillTargetPolicy.None, new[] { "Концентрация", "Яд" },
                    new ActiveEffectDefinition(ActiveEffectType.CatalyzeResources, 4)),
                SkillDefinition.RewardActive(ProgressionContentIds.Aegis, "skill.aegis.name", 4,
                    SkillTargetPolicy.None, new[] { "Щит" },
                    new ActiveEffectDefinition(ActiveEffectType.GainShield, 10)),
                SkillDefinition.RewardActive(ProgressionContentIds.Infuse, "skill.infuse.name", 6,
                    SkillTargetPolicy.OneNormalGem, new[] { "Контроль поля" },
                    new ActiveEffectDefinition(ActiveEffectType.InfuseNormalGem, 1)),

                SkillDefinition.EliteKeystone(ProgressionContentIds.TemperedCore, "skill.keystone.tempered_core.name",
                    new PassiveModifierDefinition(PassiveModifierType.VictoryHeal, 3), "Исцеление"),
                SkillDefinition.EliteKeystone(ProgressionContentIds.PrismaticStart, "skill.keystone.prismatic_start.name",
                    new PassiveModifierDefinition(PassiveModifierType.PrismaticStart, 1), "Контроль поля"),
                SkillDefinition.EliteKeystone(ProgressionContentIds.RapidCasting, "skill.keystone.rapid_casting.name",
                    new PassiveModifierDefinition(PassiveModifierType.ActiveCooldownStartReduction, 1), "Перезарядка"),
                SkillDefinition.EliteKeystone(ProgressionContentIds.HardLight, "skill.keystone.hard_light.name",
                    new PassiveModifierDefinition(PassiveModifierType.ShieldExpiryDamage, 8), "Щит")
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
