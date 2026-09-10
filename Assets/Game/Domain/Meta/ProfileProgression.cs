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
        WinWithEveryDominantBranch,
        DefeatTwentyFiveEnemies,
        DefeatFiveElites,
        DefeatFiveBosses,
        WinThreeRuns,
        WinTenRuns,
        ReachCascadeFive,
        ReachCascadeEight,
        ActivateFiftySpecials,
        ChooseTwentyEvents,
        CompleteThreeRouteVows,
        WinDifficultyThree,
        CompleteExpedition
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
        public static readonly ContentId EmblemExplorer = "emblem.explorer";
        public static readonly ContentId EmblemEliteHunter = "emblem.elite_hunter";
        public static readonly ContentId EmblemBossbreaker = "emblem.bossbreaker";
        public static readonly ContentId EmblemWayfarer = "emblem.wayfarer";
        public static readonly ContentId EmblemVeteran = "emblem.veteran";
        public static readonly ContentId EmblemCascade = "emblem.cascade";
        public static readonly ContentId EmblemAvalanche = "emblem.avalanche";
        public static readonly ContentId EmblemArtificer = "emblem.artificer";
        public static readonly ContentId EmblemStoryseeker = "emblem.storyseeker";
        public static readonly ContentId EmblemOathkeeper = "emblem.oathkeeper";
        public static readonly ContentId EmblemConqueror = "emblem.conqueror";
        public static readonly ContentId EmblemExpeditioner = "emblem.expeditioner";
    }

    [Serializable]
    public sealed class ProfileState
    {
        public const int CurrentSchemaVersion = 2;
        public int SchemaVersion = CurrentSchemaVersion;
        public string ContentVersion = RunState.CurrentContentVersion;
        public List<ContentId> UnlockedContentIds = new List<ContentId>();
        public List<ContentId> CompletedChallengeIds = new List<ContentId>();
        public List<ContentId> DominantBranchWins = new List<ContentId>();
        public List<CodexEntryState> CodexEntries = new List<CodexEntryState>();
        public List<RunRecordState> Records = new List<RunRecordState>();
        public ProfileAggregateState Aggregate = new ProfileAggregateState();
        public int BestDifficultyUnlocked;
        public List<ContentId> CompletedRouteVowIds = new List<ContentId>();
        public List<RunHistoryEntryState> RunHistory = new List<RunHistoryEntryState>();
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
        public int SpecialActivations;
        public int EventChoices;
        public int RouteVowsCompleted;
        public int ExpeditionsCompleted;
    }

    [Serializable]
    public sealed class RunHistoryEntryState
    {
        public string Seed = "0";
        public bool Victory;
        public int DifficultyTier;
        public int FinalRegionIndex;
        public int ValidTurnCount;
        public int RemainingHealth;
        public int LargestCascade;
        public ContentId BossId = "enemy.unset";
        public ContentId ChallengeId = MasteryContentIds.StandardRun;
        public List<ContentId> SkillIds = new List<ContentId>();
        public List<ContentId> RouteVowIds = new List<ContentId>();
        public List<ContentId> RouteNodeIds = new List<ContentId>();
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
                Challenge(ProfileContentIds.ChallengeDifficultyOne, MasteryContentIds.Difficulty1, "Победить с каждым из четырёх источников урона как главным.", "Сложность", UnlockConditionType.WinWithEveryDominantBranch),
                Challenge("challenge.constellation.explorer", ProfileContentIds.EmblemExplorer, "Победить 25 врагов.", "Созвездие: Исследователь", UnlockConditionType.DefeatTwentyFiveEnemies),
                Challenge("challenge.constellation.elites", ProfileContentIds.EmblemEliteHunter, "Победить 5 элитных врагов.", "Созвездие: Покоритель", UnlockConditionType.DefeatFiveElites),
                Challenge("challenge.constellation.bosses", ProfileContentIds.EmblemBossbreaker, "Победить 5 боссов.", "Созвездие: Покоритель", UnlockConditionType.DefeatFiveBosses),
                Challenge("challenge.constellation.wayfarer", ProfileContentIds.EmblemWayfarer, "Завершить 3 победных забега.", "Созвездие: Следопыт", UnlockConditionType.WinThreeRuns),
                Challenge("challenge.constellation.veteran", ProfileContentIds.EmblemVeteran, "Завершить 10 победных забегов.", "Созвездие: Следопыт", UnlockConditionType.WinTenRuns),
                Challenge("challenge.constellation.cascade", ProfileContentIds.EmblemCascade, "Собрать каскад длиной 5.", "Созвездие: Мастер кристаллов", UnlockConditionType.ReachCascadeFive),
                Challenge("challenge.constellation.avalanche", ProfileContentIds.EmblemAvalanche, "Собрать каскад длиной 8.", "Созвездие: Мастер кристаллов", UnlockConditionType.ReachCascadeEight),
                Challenge("challenge.constellation.artificer", ProfileContentIds.EmblemArtificer, "Активировать 50 особых кристаллов.", "Созвездие: Мастер кристаллов", UnlockConditionType.ActivateFiftySpecials),
                Challenge("challenge.constellation.stories", ProfileContentIds.EmblemStoryseeker, "Сделать 20 выборов в событиях.", "Созвездие: Исследователь", UnlockConditionType.ChooseTwentyEvents),
                Challenge("challenge.constellation.vows", ProfileContentIds.EmblemOathkeeper, "Исполнить 3 обета пути.", "Созвездие: Следопыт", UnlockConditionType.CompleteThreeRouteVows),
                Challenge("challenge.constellation.difficulty3", ProfileContentIds.EmblemConqueror, "Победить на сложности 3 или выше.", "Созвездие: Покоритель", UnlockConditionType.WinDifficultyThree),
                Challenge("challenge.constellation.expedition", ProfileContentIds.EmblemExpeditioner, "Завершить экспедицию.", "Созвездие: Исследователь", UnlockConditionType.CompleteExpedition)
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
        public List<ContentId> DominantBranchIds = new List<ContentId>();
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
        public ulong Seed;
        public int FinalRegionIndex;
        public int EventChoices;
        public int CompletedRouteVows;
        public bool IsExpedition;
        public List<ContentId> SkillIds = new List<ContentId>();
        public List<ContentId> RouteVowIds = new List<ContentId>();
        public List<ContentId> RouteNodeIds = new List<ContentId>();
    }

    public sealed class ProfileUpdateResult
    {
        public readonly List<ContentId> CompletedChallenges = new List<ContentId>();
        public readonly List<ContentId> UnlockedContent = new List<ContentId>();
        public readonly List<ContentId> CountedDominantBranches = new List<ContentId>();
        public readonly List<ContentId> NewDominantBranchWins = new List<ContentId>();
        public int DifficultyBefore;
        public int DifficultyAfter;
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

        public static IReadOnlyList<ContentId> DamageBranches => Branches;

        public static int DominantBranchWinCount(ProfileState profile)
        {
            Normalize(profile);
            var count = 0;
            foreach (var branch in Branches)
                if (Contains(profile.DominantBranchWins, branch)) count++;
            return count;
        }

        public static ProfileUpdateResult CompleteRun(ProfileState profile, RunCompletionSignals signals,
            ProfileContentCatalog catalog = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (signals == null) throw new ArgumentNullException(nameof(signals));
            catalog = catalog ?? ProfileContentCatalog.Instance;
            Normalize(profile);
            profile.Aggregate.SpecialActivations += Math.Max(0, signals.SpecialActivations);
            profile.Aggregate.EventChoices += Math.Max(0, signals.EventChoices);
            profile.Aggregate.RouteVowsCompleted += Math.Max(0, signals.CompletedRouteVows);
            if (signals.IsExpedition && signals.Victory) profile.Aggregate.ExpeditionsCompleted++;
            if (signals.RouteVowIds != null)
                foreach (var vowId in signals.RouteVowIds) AddUnique(profile.CompletedRouteVowIds, vowId);
            AddRunHistory(profile, signals);
            var result = new ProfileUpdateResult
            {
                DifficultyBefore = profile.BestDifficultyUnlocked,
                DifficultyAfter = profile.BestDifficultyUnlocked
            };

            if (signals.Victory)
            {
                profile.Aggregate.RunsWon++;
                var countedBranches = signals.DominantBranchIds != null && signals.DominantBranchIds.Count > 0
                    ? signals.DominantBranchIds
                    : new List<ContentId> { signals.DominantBranchId };
                foreach (var branch in countedBranches)
                {
                    if (signals.IsChallengeRun || !Contains(Branches, branch)) continue;
                    AddUnique(result.CountedDominantBranches, branch);
                    if (!Contains(profile.DominantBranchWins, branch))
                    {
                        profile.DominantBranchWins.Add(branch);
                        result.NewDominantBranchWins.Add(branch);
                    }
                }
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
            result.DifficultyAfter = profile.BestDifficultyUnlocked;
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
            if (profile.CompletedRouteVowIds == null) profile.CompletedRouteVowIds = new List<ContentId>();
            if (profile.RunHistory == null) profile.RunHistory = new List<RunHistoryEntryState>();
            foreach (var entry in profile.RunHistory)
            {
                if (entry == null) continue;
                if (entry.SkillIds == null) entry.SkillIds = new List<ContentId>();
                if (entry.RouteVowIds == null) entry.RouteVowIds = new List<ContentId>();
                if (entry.RouteNodeIds == null) entry.RouteNodeIds = new List<ContentId>();
            }
            if (profile.RunHistory.Count > 20)
                profile.RunHistory.RemoveRange(20, profile.RunHistory.Count - 20);
            profile.SchemaVersion = ProfileState.CurrentSchemaVersion;
            profile.ContentVersion = RunState.CurrentContentVersion;
            profile.BestDifficultyUnlocked = Math.Max(0, Math.Min(5, profile.BestDifficultyUnlocked));
        }

        private static bool Satisfied(ProfileState profile, RunCompletionSignals signals, UnlockConditionType condition)
        {
            if (condition == UnlockConditionType.DefeatCrystalWarden)
                // A fresh standard run cannot roll Facet Engine until this goal is complete,
                // so reaching the new final boss proves Crystal Warden was defeated in Region 1.
                return signals.Victory && !signals.IsChallengeRun;
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
            if (condition == UnlockConditionType.DefeatTwentyFiveEnemies) return profile.Aggregate.EnemiesDefeated >= 25;
            if (condition == UnlockConditionType.DefeatFiveElites) return profile.Aggregate.ElitesDefeated >= 5;
            if (condition == UnlockConditionType.DefeatFiveBosses) return profile.Aggregate.BossesDefeated >= 5;
            if (condition == UnlockConditionType.WinThreeRuns) return profile.Aggregate.RunsWon >= 3;
            if (condition == UnlockConditionType.WinTenRuns) return profile.Aggregate.RunsWon >= 10;
            if (condition == UnlockConditionType.ReachCascadeFive) return signals.LargestCascade >= 5;
            if (condition == UnlockConditionType.ReachCascadeEight) return signals.LargestCascade >= 8;
            if (condition == UnlockConditionType.ActivateFiftySpecials) return profile.Aggregate.SpecialActivations >= 50;
            if (condition == UnlockConditionType.ChooseTwentyEvents) return profile.Aggregate.EventChoices >= 20;
            if (condition == UnlockConditionType.CompleteThreeRouteVows) return profile.Aggregate.RouteVowsCompleted >= 3;
            if (condition == UnlockConditionType.WinDifficultyThree) return signals.Victory && signals.DifficultyTier >= 3;
            if (condition == UnlockConditionType.CompleteExpedition) return signals.Victory && signals.IsExpedition;
            return false;
        }

        private static void AddRunHistory(ProfileState profile, RunCompletionSignals signals)
        {
            var entry = new RunHistoryEntryState
            {
                Seed = signals.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Victory = signals.Victory,
                DifficultyTier = signals.DifficultyTier,
                FinalRegionIndex = signals.FinalRegionIndex,
                ValidTurnCount = signals.ValidTurnCount,
                RemainingHealth = signals.RemainingHealth,
                LargestCascade = signals.LargestCascade,
                BossId = signals.BossId,
                ChallengeId = signals.ChallengeId,
                SkillIds = signals.SkillIds == null ? new List<ContentId>() : new List<ContentId>(signals.SkillIds),
                RouteVowIds = signals.RouteVowIds == null ? new List<ContentId>() : new List<ContentId>(signals.RouteVowIds),
                RouteNodeIds = signals.RouteNodeIds == null ? new List<ContentId>() : new List<ContentId>(signals.RouteNodeIds)
            };
            profile.RunHistory.Insert(0, entry);
            if (profile.RunHistory.Count > 20) profile.RunHistory.RemoveRange(20, profile.RunHistory.Count - 20);
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
