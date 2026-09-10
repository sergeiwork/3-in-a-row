using System;
using System.Collections.Generic;
using System.Globalization;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Mastery
{
    public static class MasteryContentIds
    {
        public static readonly ContentId StandardRun = "challenge.none";
        public static readonly ContentId ProfileUnlockPolicy = "unlock_policy.profile_snapshot";
        public static readonly ContentId AllContentUnlockPolicy = "unlock_policy.all_v1.0";
        public static readonly ContentId Difficulty0 = "difficulty.0.standard";
        public static readonly ContentId Difficulty1 = "difficulty.1.sharp_edges";
        public static readonly ContentId Difficulty2 = "difficulty.2.unstable_grid";
        public static readonly ContentId Difficulty3 = "difficulty.3.long_road";
        public static readonly ContentId Difficulty4 = "difficulty.4.hostile_pattern";
        public static readonly ContentId Difficulty5 = "difficulty.5.perfect_facet";
    }

    public sealed class DifficultyDefinition
    {
        public readonly int Tier;
        public readonly ContentId Id;
        public readonly string DisplayKey;
        public readonly int EnemyDirectDamageBonus;
        public readonly int EncounterStartCracked;
        public readonly int BaseVictoryHealing;
        public readonly IReadOnlyList<IntentEffectDefinition> EliteFinalIntentEffects;
        public readonly bool EnableBossSecondPhase;

        public DifficultyDefinition(
            int tier,
            ContentId id,
            string displayKey,
            int directDamageBonus,
            int encounterStartCracked,
            int baseVictoryHealing,
            bool enableBossSecondPhase,
            params IntentEffectDefinition[] eliteFinalIntentEffects)
        {
            Tier = tier;
            Id = id;
            DisplayKey = displayKey ?? string.Empty;
            EnemyDirectDamageBonus = directDamageBonus;
            EncounterStartCracked = encounterStartCracked;
            BaseVictoryHealing = baseVictoryHealing;
            EnableBossSecondPhase = enableBossSecondPhase;
            EliteFinalIntentEffects = eliteFinalIntentEffects ?? new IntentEffectDefinition[0];
        }
    }

    public sealed class MasteryContentCatalog
    {
        private readonly List<DifficultyDefinition> _difficulties;

        public static readonly MasteryContentCatalog Instance = new MasteryContentCatalog();

        private MasteryContentCatalog()
        {
            _difficulties = new List<DifficultyDefinition>
            {
                new DifficultyDefinition(0, MasteryContentIds.Difficulty0, "difficulty.0.name", 0, 0, 4, false),
                new DifficultyDefinition(1, MasteryContentIds.Difficulty1, "difficulty.1.name", 1, 0, 4, false),
                new DifficultyDefinition(2, MasteryContentIds.Difficulty2, "difficulty.2.name", 1, 2, 4, false),
                new DifficultyDefinition(3, MasteryContentIds.Difficulty3, "difficulty.3.name", 1, 2, 2, false),
                new DifficultyDefinition(4, MasteryContentIds.Difficulty4, "difficulty.4.name", 1, 2, 2, false,
                    IntentEffectDefinition.ApplyStatus("status.thorned", 1)),
                new DifficultyDefinition(5, MasteryContentIds.Difficulty5, "difficulty.5.name", 1, 2, 2, true,
                    IntentEffectDefinition.ApplyStatus("status.thorned", 1))
            };
        }

        public IReadOnlyList<DifficultyDefinition> Difficulties => _difficulties;

        public DifficultyDefinition Get(int tier)
        {
            if (tier < 0 || tier >= _difficulties.Count)
                throw new ArgumentOutOfRangeException(nameof(tier));
            return _difficulties[tier];
        }

        public DifficultyDefinition Get(ContentId id)
        {
            foreach (var definition in _difficulties)
                if (definition.Id.Equals(id)) return definition;
            throw new KeyNotFoundException("Unknown difficulty content ID: " + id);
        }
    }

    [Serializable]
    public sealed class WeeklyChallengeDefinition
    {
        public ContentId Id = MasteryContentIds.StandardRun;
        public ulong Seed;
        public string ContentVersion = RunState.CurrentContentVersion;
        public ContentId UnlockPolicyId = MasteryContentIds.AllContentUnlockPolicy;
        public int DifficultyTier = 3;
        public string WeekStartUtc = string.Empty;
    }

    public static class WeeklyChallenge
    {
        private static readonly DateTime EpochMondayUtc = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        public static WeeklyChallengeDefinition ForUtcDate(DateTime utcDate)
        {
            var utc = utcDate.Kind == DateTimeKind.Utc ? utcDate : utcDate.ToUniversalTime();
            var day = utc.Date;
            var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
            var monday = day.AddDays(-daysSinceMonday);
            var week = (long)Math.Floor((monday - EpochMondayUtc).TotalDays / 7d);
            var id = "challenge.weekly." + monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new WeeklyChallengeDefinition
            {
                Id = (ContentId)id,
                Seed = StableSeed(id + "|" + RunState.CurrentContentVersion + "|" + week),
                ContentVersion = RunState.CurrentContentVersion,
                UnlockPolicyId = MasteryContentIds.AllContentUnlockPolicy,
                DifficultyTier = 3,
                WeekStartUtc = monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }

        private static ulong StableSeed(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= prime;
            }
            return hash == 0 ? 1UL : hash;
        }
    }

    [Serializable]
    public sealed class ExpeditionDefinition
    {
        public ContentId Id = "expedition.none";
        public ulong Seed;
        public string ContentVersion = RunState.CurrentContentVersion;
        public int RegionIndex;
        public int DifficultyTier;
        public ContentId StartingSkillId = "skill.none";
        public ContentId StartingStatusId = "status.none";
        public int StartingStatusCount;
        public string DateLabel = string.Empty;
    }

    public static class DailyExpedition
    {
        public static ExpeditionDefinition ForUtcDate(DateTime utcDate)
        {
            var day = (utcDate.Kind == DateTimeKind.Utc ? utcDate : utcDate.ToUniversalTime()).Date;
            var serial = (long)(day - new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
            var region = (int)((serial % 3 + 3) % 3);
            var id = "expedition.daily." + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var skills = new[] { "skill.kindling", "skill.flow_state", "skill.overcharge" };
            var statuses = new[] { "status.cracked", "status.thorned", "status.frozen" };
            return new ExpeditionDefinition
            {
                Id = id,
                Seed = StableSeed(id + "|" + RunState.CurrentContentVersion),
                RegionIndex = region,
                DifficultyTier = Math.Min(3, region + 1),
                StartingSkillId = skills[region],
                StartingStatusId = statuses[region],
                StartingStatusCount = 2,
                DateLabel = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }

        internal static ulong StableSeed(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            foreach (var character in value) { hash ^= character; hash *= prime; }
            return hash == 0 ? 1UL : hash;
        }
    }

    public static class TrialExpeditions
    {
        private static readonly List<ExpeditionDefinition> Definitions = Build();
        public static IReadOnlyList<ExpeditionDefinition> All => Definitions;

        private static List<ExpeditionDefinition> Build()
        {
            return new List<ExpeditionDefinition>
            {
                Trial("expedition.trial.ember", 0, 1, "skill.kindling", "status.cracked", 3),
                Trial("expedition.trial.tide", 0, 1, "skill.flow_state", "status.frozen", 2),
                Trial("expedition.trial.venom", 1, 1, "skill.concentrate", "status.thorned", 2),
                Trial("expedition.trial.volt", 2, 2, "skill.overcharge", "status.anchored", 2),
                Trial("expedition.trial.sparks", 0, 2, "skill.backdraft", "status.cracked", 4),
                Trial("expedition.trial.cascades", 1, 2, "skill.infuse", "status.thorned", 3),
                Trial("expedition.trial.cleanse", 2, 2, "skill.reweave", "status.frozen", 4),
                Trial("expedition.trial.prism", 0, 3, "skill.infuse", "status.anchored", 3),
                Trial("expedition.trial.warden", 0, 3, "skill.aegis", "status.cracked", 4),
                Trial("expedition.trial.treant", 1, 3, "skill.catalyze", "status.thorned", 4),
                Trial("expedition.trial.devourer", 2, 4, "skill.detonate", "status.frozen", 3),
                Trial("expedition.trial.perfect_facet", 2, 5, "skill.transmute", "status.anchored", 3)
            };
        }

        private static ExpeditionDefinition Trial(string id, int region, int difficulty,
            string skill, string status, int statusCount)
        {
            return new ExpeditionDefinition
            {
                Id = id,
                Seed = DailyExpedition.StableSeed(id + "|" + RunState.CurrentContentVersion),
                RegionIndex = region,
                DifficultyTier = difficulty,
                StartingSkillId = skill,
                StartingStatusId = status,
                StartingStatusCount = statusCount
            };
        }
    }

    public static class MasteryRules
    {
        public static DifficultyDefinition For(RunState state)
        {
            return MasteryContentCatalog.Instance.Get(state == null ? 0 : state.DifficultyTier);
        }

        public static bool HasAvailableContent(RunState state, ContentId id)
        {
            if (state == null) return false;
            if (state.UnlockPolicyId.Equals(MasteryContentIds.AllContentUnlockPolicy)) return true;
            if (state.AvailableContentIds == null) return false;
            foreach (var contentId in state.AvailableContentIds)
                if (contentId.Equals(id)) return true;
            return false;
        }
    }
}
