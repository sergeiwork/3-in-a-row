using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Mastery;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Meta
{
    public enum CodexCategory
    {
        Gem,
        Special,
        Status,
        Enemy,
        Intent,
        Skill,
        Event,
        Elite,
        Boss
    }

    public enum UnlockConditionType
    {
        DefeatCrystalWarden,
        WinWithThreeEmberSkills,
        PoisonTwoStacksInResponse,
        CleanseThreeStatusKinds,
        EliteWithoutHealthDamage,
        ActivateThreeSpecials,
        ActivateThreeSparks,
        ConvertFocusFourTimes,
        FocusAndPoisonSameRun,
        WinWithEveryDominantBranch
    }

    public static class ProfileContentIds
    {
        public static readonly ContentId ChallengeFacetEngine = "challenge.defeat_crystal_warden";
        public static readonly ContentId ChallengeTransmute = "challenge.ember_triad";
        public static readonly ContentId ChallengeGalvanicVenom = "challenge.double_poison";
        public static readonly ContentId ChallengeAdvancedEvent = "challenge.perfect_cleanse";
        public static readonly ContentId ChallengeFlawlessElite = "challenge.flawless_elite";
        public static readonly ContentId ChallengeDetonate = "challenge.specialist";
        public static readonly ContentId ChallengeFlashfire = "challenge.spark_chain";
        public static readonly ContentId ChallengeScaldingCurrent = "challenge.focus_engine";
        public static readonly ContentId ChallengeToxicUndertow = "challenge.crosscurrent";
        public static readonly ContentId ChallengeDifficultyOne = "challenge.four_paths";
        public static readonly ContentId AdvancedEvent = "event.prismatic_archive";
        public static readonly ContentId FlawlessChallengeCard = "challenge_card.flawless_elite";
    }

    [Serializable]
    public sealed class ProfileState
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;
        public string ContentVersion = RunState.CurrentContentVersion;
        public List<ContentId> UnlockedContentIds = new List<ContentId>();
        public List<ContentId> CompletedChallengeIds = new List<ContentId>();
        public List<ContentId> DominantBranchWins = new List<ContentId>();
        public List<CodexEntryState> CodexEntries = new List<CodexEntryState>();
        public List<RunRecordState> Records = new List<RunRecordState>();
        public ProfileAggregateState Aggregate = new ProfileAggregateState();
        public int BestDifficultyUnlocked;
    }

    [Serializable]
    public sealed class CodexEntryState
    {
        public CodexCategory Category;
        public ContentId ContentId = "content.none";
        public ContentId ParentContentId = "content.none";
        public int SeenCount;
    }

    [Serializable]
    public sealed class RunRecordState
    {
        public ContentId BossId = "enemy.unset";
        public int DifficultyTier;
        public int Wins;
        public int BestRemainingHealth;
        public int FastestValidTurnCount;
        public int LargestCascade;
        public ContentId DominantDamageBranchId = "branch.none";
        public ContentId ChallengeId = MasteryContentIds.StandardRun;
        public int BestChallengeTurns;
        public int BestChallengeHealth;
        public int BestChallengeLargestCascade;
        public ContentId BestChallengeDominantBranchId = "branch.none";
    }

    [Serializable]
    public sealed class ProfileAggregateState
    {
        public int RunsStarted;
        public int RunsWon;
        public int EnemiesDefeated;
        public int ElitesDefeated;
        public int BossesDefeated;
    }

    public sealed class UnlockChallengeDefinition
    {
        public readonly ContentId Id;
        public readonly ContentId UnlockContentId;
        public readonly string GoalText;
        public readonly string CategoryText;
        public readonly UnlockConditionType Condition;

        public UnlockChallengeDefinition(ContentId id, ContentId unlockContentId, string goalText,
            string categoryText, UnlockConditionType condition)
        {
            Id = id;
            UnlockContentId = unlockContentId;
            GoalText = goalText ?? string.Empty;
            CategoryText = categoryText ?? string.Empty;
            Condition = condition;
        }
    }

    public sealed class ProfileContentCatalog
    {
        private readonly List<UnlockChallengeDefinition> _challenges;
        public static readonly ProfileContentCatalog Instance = new ProfileContentCatalog();

        private ProfileContentCatalog()
        {
            _challenges = new List<UnlockChallengeDefinition>
            {
                Challenge(ProfileContentIds.ChallengeFacetEngine, "enemy.facet_engine", "Победить Кристального стража.", "Босс", UnlockConditionType.DefeatCrystalWarden),
                Challenge(ProfileContentIds.ChallengeTransmute, "skill.transmute", "Победить, изучив три навыка Пламени.", "Активный навык", UnlockConditionType.WinWithThreeEmberSkills),
                Challenge(ProfileContentIds.ChallengeGalvanicVenom, "skill.galvanic_venom", "Начать ответ врага с двумя или более зарядами отравления.", "Гибридный навык", UnlockConditionType.PoisonTwoStacksInResponse),
                Challenge(ProfileContentIds.ChallengeAdvancedEvent, ProfileContentIds.AdvancedEvent, "Одним Очищением снять три разных состояния.", "Событие", UnlockConditionType.CleanseThreeStatusKinds),
                Challenge(ProfileContentIds.ChallengeFlawlessElite, "skill.reweave", "Победить элитного врага без урона здоровью.", "Активный навык и запись кодекса", UnlockConditionType.EliteWithoutHealthDamage),
                Challenge(ProfileContentIds.ChallengeDetonate, "skill.detonate", "Активировать три особых кристалла за забег.", "Активный навык", UnlockConditionType.ActivateThreeSpecials),
                Challenge(ProfileContentIds.ChallengeFlashfire, "skill.flashfire", "Активировать три Искры за забег.", "Гибридный навык", UnlockConditionType.ActivateThreeSparks),
                Challenge(ProfileContentIds.ChallengeScaldingCurrent, "skill.scalding_current", "Преобразовать концентрацию четыре раза за забег.", "Гибридный навык", UnlockConditionType.ConvertFocusFourTimes),
                Challenge(ProfileContentIds.ChallengeToxicUndertow, "skill.toxic_undertow", "Преобразовать концентрацию и наложить яд в одном забеге.", "Гибридный навык", UnlockConditionType.FocusAndPoisonSameRun),
                Challenge(ProfileContentIds.ChallengeDifficultyOne, MasteryContentIds.Difficulty1, "Победить с каждым из четырёх источников урона как главным.", "Сложность", UnlockConditionType.WinWithEveryDominantBranch)
            };
        }

        public IReadOnlyList<UnlockChallengeDefinition> Challenges => _challenges;

        private static UnlockChallengeDefinition Challenge(ContentId id, ContentId unlock, string goal,
            string category, UnlockConditionType condition)
        {
            return new UnlockChallengeDefinition(id, unlock, goal, category, condition);
        }
    }

    public sealed class RunCompletionSignals
    {
        public bool Victory;
        public ContentId BossId = "enemy.unset";
        public int DifficultyTier;
        public int RemainingHealth;
        public int ValidTurnCount;
        public int LargestCascade;
        public ContentId DominantBranchId = "branch.none";
        public int EmberSkillsLearned;
        public int MaxPoisonStacksInResponse;
        public int MaxCleanseStatusKinds;
        public int FlawlessEliteVictories;
        public int SpecialActivations;
        public int SparkActivations;
        public int FocusConversions;
        public int PoisonApplications;
        public bool IsChallengeRun;
        public ContentId ChallengeId = MasteryContentIds.StandardRun;
    }

    public sealed class ProfileUpdateResult
    {
        public readonly List<ContentId> CompletedChallenges = new List<ContentId>();
        public readonly List<ContentId> UnlockedContent = new List<ContentId>();
    }

    public static class ProfileProgression
    {
        private static readonly ContentId[] Branches =
        {
            "branch.ember", "branch.tide", "branch.venom", "branch.volt"
        };

        public static ProfileState CreateFresh()
        {
            return new ProfileState();
        }

        public static ProfileUpdateResult CompleteRun(ProfileState profile, RunCompletionSignals signals,
            ProfileContentCatalog catalog = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (signals == null) throw new ArgumentNullException(nameof(signals));
            catalog = catalog ?? ProfileContentCatalog.Instance;
            Normalize(profile);
            var result = new ProfileUpdateResult();

            if (signals.Victory)
            {
                profile.Aggregate.RunsWon++;
                if (!signals.DominantBranchId.Equals("branch.none"))
                    AddUnique(profile.DominantBranchWins, signals.DominantBranchId);
                UpdateRecord(profile, signals);
                if (!signals.IsChallengeRun && signals.DifficultyTier > 0 &&
                    signals.DifficultyTier == profile.BestDifficultyUnlocked &&
                    profile.BestDifficultyUnlocked < 5)
                    profile.BestDifficultyUnlocked = Math.Min(5, signals.DifficultyTier + 1);
            }

            foreach (var challenge in catalog.Challenges)
            {
                if (Contains(profile.CompletedChallengeIds, challenge.Id) || !Satisfied(profile, signals, challenge.Condition))
                    continue;
                profile.CompletedChallengeIds.Add(challenge.Id);
                result.CompletedChallenges.Add(challenge.Id);
                if (!Contains(profile.UnlockedContentIds, challenge.UnlockContentId))
                {
                    profile.UnlockedContentIds.Add(challenge.UnlockContentId);
                    result.UnlockedContent.Add(challenge.UnlockContentId);
                }
                if (challenge.Condition == UnlockConditionType.WinWithEveryDominantBranch)
                    profile.BestDifficultyUnlocked = Math.Max(1, profile.BestDifficultyUnlocked);
                if (challenge.Condition == UnlockConditionType.EliteWithoutHealthDamage)
                    Discover(profile, CodexCategory.Elite, ProfileContentIds.FlawlessChallengeCard);
            }
            return result;
        }

        public static void Discover(ProfileState profile, CodexCategory category, ContentId id,
            ContentId? parentContentId = null)
        {
            if (profile == null || string.IsNullOrEmpty(id.Value) || id.Value.EndsWith(".none", StringComparison.Ordinal)) return;
            Normalize(profile);
            foreach (var entry in profile.CodexEntries)
            {
                if (entry.Category != category || !entry.ContentId.Equals(id) ||
                    !entry.ParentContentId.Equals(parentContentId ?? (ContentId)"content.none")) continue;
                entry.SeenCount++;
                return;
            }
            profile.CodexEntries.Add(new CodexEntryState
            {
                Category = category,
                ContentId = id,
                ParentContentId = parentContentId ?? (ContentId)"content.none",
                SeenCount = 1
            });
        }

        public static List<UnlockChallengeDefinition> SuggestedGoals(ProfileState profile, int maximum = 3,
            ProfileContentCatalog catalog = null)
        {
            catalog = catalog ?? ProfileContentCatalog.Instance;
            Normalize(profile);
            var result = new List<UnlockChallengeDefinition>();
            foreach (var challenge in catalog.Challenges)
            {
                if (Contains(profile.CompletedChallengeIds, challenge.Id)) continue;
                result.Add(challenge);
                if (result.Count >= maximum) break;
            }
            return result;
        }

        public static void Normalize(ProfileState profile)
        {
            if (profile.UnlockedContentIds == null) profile.UnlockedContentIds = new List<ContentId>();
            if (profile.CompletedChallengeIds == null) profile.CompletedChallengeIds = new List<ContentId>();
            if (profile.DominantBranchWins == null) profile.DominantBranchWins = new List<ContentId>();
            if (profile.CodexEntries == null) profile.CodexEntries = new List<CodexEntryState>();
            if (profile.Records == null) profile.Records = new List<RunRecordState>();
            if (profile.Aggregate == null) profile.Aggregate = new ProfileAggregateState();
            profile.SchemaVersion = ProfileState.CurrentSchemaVersion;
            profile.ContentVersion = RunState.CurrentContentVersion;
            profile.BestDifficultyUnlocked = Math.Max(0, Math.Min(5, profile.BestDifficultyUnlocked));
        }

        private static bool Satisfied(ProfileState profile, RunCompletionSignals signals, UnlockConditionType condition)
        {
            if (condition == UnlockConditionType.DefeatCrystalWarden)
                return signals.Victory && signals.BossId.Equals("enemy.crystal_warden");
            if (condition == UnlockConditionType.WinWithThreeEmberSkills)
                return signals.Victory && signals.EmberSkillsLearned >= 3;
            if (condition == UnlockConditionType.PoisonTwoStacksInResponse)
                return signals.MaxPoisonStacksInResponse >= 2;
            if (condition == UnlockConditionType.CleanseThreeStatusKinds)
                return signals.MaxCleanseStatusKinds >= 3;
            if (condition == UnlockConditionType.EliteWithoutHealthDamage)
                return signals.FlawlessEliteVictories > 0;
            if (condition == UnlockConditionType.ActivateThreeSpecials)
                return signals.SpecialActivations >= 3;
            if (condition == UnlockConditionType.ActivateThreeSparks)
                return signals.SparkActivations >= 3;
            if (condition == UnlockConditionType.ConvertFocusFourTimes)
                return signals.FocusConversions >= 4;
            if (condition == UnlockConditionType.FocusAndPoisonSameRun)
                return signals.FocusConversions > 0 && signals.PoisonApplications > 0;
            if (condition == UnlockConditionType.WinWithEveryDominantBranch)
            {
                if (!signals.Victory) return false;
                foreach (var branch in Branches) if (!Contains(profile.DominantBranchWins, branch)) return false;
                return true;
            }
            return false;
        }

        private static void UpdateRecord(ProfileState profile, RunCompletionSignals signals)
        {
            RunRecordState record = null;
            foreach (var candidate in profile.Records)
            {
                if (candidate.BossId.Equals(signals.BossId) && candidate.DifficultyTier == signals.DifficultyTier &&
                    candidate.ChallengeId.Equals(signals.ChallengeId)) { record = candidate; break; }
            }
            if (record == null)
            {
                record = new RunRecordState
                {
                    BossId = signals.BossId,
                    DifficultyTier = signals.DifficultyTier,
                    ChallengeId = signals.ChallengeId
                };
                profile.Records.Add(record);
            }
            record.Wins++;
            record.BestRemainingHealth = Math.Max(record.BestRemainingHealth, signals.RemainingHealth);
            if (record.FastestValidTurnCount == 0 || signals.ValidTurnCount < record.FastestValidTurnCount)
                record.FastestValidTurnCount = signals.ValidTurnCount;
            record.LargestCascade = Math.Max(record.LargestCascade, signals.LargestCascade);
            record.DominantDamageBranchId = signals.DominantBranchId;
            var betterChallenge = record.BestChallengeTurns == 0 ||
                signals.ValidTurnCount < record.BestChallengeTurns ||
                (signals.ValidTurnCount == record.BestChallengeTurns && signals.RemainingHealth > record.BestChallengeHealth) ||
                (signals.ValidTurnCount == record.BestChallengeTurns && signals.RemainingHealth == record.BestChallengeHealth &&
                 signals.LargestCascade > record.BestChallengeLargestCascade) ||
                (signals.ValidTurnCount == record.BestChallengeTurns && signals.RemainingHealth == record.BestChallengeHealth &&
                 signals.LargestCascade == record.BestChallengeLargestCascade &&
                 StringComparer.Ordinal.Compare(signals.DominantBranchId.Value,
                     record.BestChallengeDominantBranchId.Value) < 0);
            if (signals.IsChallengeRun && betterChallenge)
            {
                record.BestChallengeTurns = signals.ValidTurnCount;
                record.BestChallengeHealth = signals.RemainingHealth;
                record.BestChallengeLargestCascade = signals.LargestCascade;
                record.BestChallengeDominantBranchId = signals.DominantBranchId;
            }
        }

        private static bool Contains(IEnumerable<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return false;
            foreach (var id in ids) if (id.Equals(wanted)) return true;
            return false;
        }

        private static void AddUnique(List<ContentId> ids, ContentId value)
        {
            if (!Contains(ids, value)) ids.Add(value);
        }
    }
}
