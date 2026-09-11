using System;
using System.Collections.Generic;
using System.IO;
using ThreeInARow.Application;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Meta;
using UnityEngine;

namespace ThreeInARow.Infrastructure
{
    /// <summary>Independent local meta-progression save. It is never cleared when a run is abandoned.</summary>
    public sealed class JsonProfileStore : IProfileStore
    {
        private const string FileName = "profile.json";
        private readonly string _path;

        public JsonProfileStore(string directory = null)
        {
            _path = Path.Combine(directory ?? UnityEngine.Application.persistentDataPath, FileName);
        }

        public ProfileState LoadOrCreate()
        {
            try
            {
                if (!File.Exists(_path)) return ProfileProgression.CreateFresh();
                var dto = JsonUtility.FromJson<ProfileDto>(File.ReadAllText(_path));
                if (dto == null || dto.schemaVersion < 1 || dto.schemaVersion > ProfileState.CurrentSchemaVersion)
                    return ProfileProgression.CreateFresh();
                return dto.ToDomain();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Profile could not be loaded: " + exception.Message);
                return ProfileProgression.CreateFresh();
            }
        }

        public void Save(ProfileState profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            ProfileProgression.Normalize(profile);
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(ProfileDto.FromDomain(profile), true));
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporaryPath, _path);
        }

        [Serializable]
        private sealed class ProfileDto
        {
            public int schemaVersion;
            public string contentVersion;
            public int bestDifficultyUnlocked;
            public List<string> unlockedContent = new List<string>();
            public List<string> completedChallenges = new List<string>();
            public List<string> dominantBranchWins = new List<string>();
            public List<CodexDto> codex = new List<CodexDto>();
            public List<RecordDto> records = new List<RecordDto>();
            public AggregateDto aggregate = new AggregateDto();
            public List<string> completedRouteVows = new List<string>();
            public List<RunHistoryDto> runHistory = new List<RunHistoryDto>();
            public List<PulseRecordDto> pulseRecords = new List<PulseRecordDto>();

            public static ProfileDto FromDomain(ProfileState profile)
            {
                var dto = new ProfileDto
                {
                    schemaVersion = profile.SchemaVersion,
                    contentVersion = profile.ContentVersion,
                    bestDifficultyUnlocked = profile.BestDifficultyUnlocked,
                    aggregate = AggregateDto.FromDomain(profile.Aggregate)
                };
                AddIds(dto.unlockedContent, profile.UnlockedContentIds);
                AddIds(dto.completedChallenges, profile.CompletedChallengeIds);
                AddIds(dto.dominantBranchWins, profile.DominantBranchWins);
                AddIds(dto.completedRouteVows, profile.CompletedRouteVowIds);
                foreach (var entry in profile.CodexEntries)
                    if (entry != null) dto.codex.Add(new CodexDto
                    {
                        category = (int)entry.Category,
                        contentId = entry.ContentId.Value,
                        parentContentId = entry.ParentContentId.Value,
                        seenCount = entry.SeenCount
                    });
                foreach (var record in profile.Records)
                    if (record != null) dto.records.Add(RecordDto.FromDomain(record));
                foreach (var entry in profile.RunHistory)
                    if (entry != null) dto.runHistory.Add(RunHistoryDto.FromDomain(entry));
                foreach (var record in profile.PulseRecords)
                    if (record != null) dto.pulseRecords.Add(PulseRecordDto.FromDomain(record));
                return dto;
            }

            public ProfileState ToDomain()
            {
                var profile = new ProfileState
                {
                    SchemaVersion = schemaVersion,
                    ContentVersion = contentVersion ?? string.Empty,
                    BestDifficultyUnlocked = bestDifficultyUnlocked,
                    UnlockedContentIds = ToIds(unlockedContent),
                    CompletedChallengeIds = ToIds(completedChallenges),
                    DominantBranchWins = ToIds(dominantBranchWins),
                    CodexEntries = new List<CodexEntryState>(),
                    Records = new List<RunRecordState>(),
                    Aggregate = aggregate == null ? new ProfileAggregateState() : aggregate.ToDomain(),
                    CompletedRouteVowIds = ToIds(completedRouteVows),
                    RunHistory = new List<RunHistoryEntryState>(),
                    PulseRecords = new List<PulseRecordState>()
                };
                if (codex != null)
                    foreach (var entry in codex) profile.CodexEntries.Add(new CodexEntryState
                    {
                        Category = (CodexCategory)entry.category,
                        ContentId = Content(entry.contentId),
                        ParentContentId = Content(entry.parentContentId),
                        SeenCount = entry.seenCount
                    });
                if (records != null)
                    foreach (var record in records) profile.Records.Add(record.ToDomain());
                if (runHistory != null)
                    foreach (var entry in runHistory) profile.RunHistory.Add(entry.ToDomain());
                if (pulseRecords != null)
                    foreach (var record in pulseRecords) profile.PulseRecords.Add(record.ToDomain());
                ProfileProgression.Normalize(profile);
                return profile;
            }
        }

        [Serializable]
        private sealed class PulseRecordDto
        {
            public string clockPresetId;
            public string trialId;
            public int runsCompleted;
            public int wins;
            public int bestRemainingHealth;
            public int fewestEnemyPulses;
            public int mostAcceptedSwaps;
            public int mostSurges;
            public int highestFlow;

            public static PulseRecordDto FromDomain(PulseRecordState record)
            {
                return new PulseRecordDto
                {
                    clockPresetId = record.ClockPresetId.Value, trialId = record.TrialId.Value,
                    runsCompleted = record.RunsCompleted, wins = record.Wins,
                    bestRemainingHealth = record.BestRemainingHealth,
                    fewestEnemyPulses = record.FewestEnemyPulses,
                    mostAcceptedSwaps = record.MostAcceptedSwaps,
                    mostSurges = record.MostSurges, highestFlow = record.HighestFlow
                };
            }

            public PulseRecordState ToDomain()
            {
                return new PulseRecordState
                {
                    ClockPresetId = Content(clockPresetId), TrialId = Content(trialId),
                    RunsCompleted = runsCompleted, Wins = wins,
                    BestRemainingHealth = bestRemainingHealth,
                    FewestEnemyPulses = fewestEnemyPulses,
                    MostAcceptedSwaps = mostAcceptedSwaps,
                    MostSurges = mostSurges, HighestFlow = highestFlow
                };
            }
        }

        [Serializable]
        private sealed class CodexDto
        {
            public int category;
            public string contentId;
            public string parentContentId;
            public int seenCount;
        }

        [Serializable]
        private sealed class RunHistoryDto
        {
            public string seed;
            public bool victory;
            public int difficultyTier;
            public int finalRegionIndex;
            public int validTurnCount;
            public int remainingHealth;
            public int largestCascade;
            public string bossId;
            public string challengeId;
            public List<string> skillIds = new List<string>();
            public List<string> routeVowIds = new List<string>();
            public List<string> routeNodeIds = new List<string>();

            public static RunHistoryDto FromDomain(RunHistoryEntryState entry)
            {
                var dto = new RunHistoryDto
                {
                    seed = entry.Seed, victory = entry.Victory, difficultyTier = entry.DifficultyTier,
                    finalRegionIndex = entry.FinalRegionIndex, validTurnCount = entry.ValidTurnCount,
                    remainingHealth = entry.RemainingHealth, largestCascade = entry.LargestCascade,
                    bossId = entry.BossId.Value, challengeId = entry.ChallengeId.Value
                };
                AddIds(dto.skillIds, entry.SkillIds);
                AddIds(dto.routeVowIds, entry.RouteVowIds);
                AddIds(dto.routeNodeIds, entry.RouteNodeIds);
                return dto;
            }

            public RunHistoryEntryState ToDomain()
            {
                return new RunHistoryEntryState
                {
                    Seed = string.IsNullOrEmpty(seed) ? "0" : seed,
                    Victory = victory, DifficultyTier = difficultyTier, FinalRegionIndex = finalRegionIndex,
                    ValidTurnCount = validTurnCount, RemainingHealth = remainingHealth,
                    LargestCascade = largestCascade, BossId = Content(bossId),
                    ChallengeId = Content(challengeId), SkillIds = ToIds(skillIds),
                    RouteVowIds = ToIds(routeVowIds), RouteNodeIds = ToIds(routeNodeIds)
                };
            }
        }

        [Serializable]
        private sealed class RecordDto
        {
            public string bossId;
            public int difficultyTier;
            public int wins;
            public int bestRemainingHealth;
            public int fastestValidTurnCount;
            public int largestCascade;
            public string dominantDamageBranchId;
            public string challengeId;
            public int bestChallengeTurns;
            public int bestChallengeHealth;
            public int bestChallengeLargestCascade;
            public string bestChallengeDominantBranchId;

            public static RecordDto FromDomain(RunRecordState record)
            {
                return new RecordDto
                {
                    bossId = record.BossId.Value,
                    difficultyTier = record.DifficultyTier,
                    wins = record.Wins,
                    bestRemainingHealth = record.BestRemainingHealth,
                    fastestValidTurnCount = record.FastestValidTurnCount,
                    largestCascade = record.LargestCascade,
                    dominantDamageBranchId = record.DominantDamageBranchId.Value,
                    challengeId = record.ChallengeId.Value,
                    bestChallengeTurns = record.BestChallengeTurns,
                    bestChallengeHealth = record.BestChallengeHealth,
                    bestChallengeLargestCascade = record.BestChallengeLargestCascade,
                    bestChallengeDominantBranchId = record.BestChallengeDominantBranchId.Value
                };
            }

            public RunRecordState ToDomain()
            {
                return new RunRecordState
                {
                    BossId = Content(bossId), DifficultyTier = difficultyTier, Wins = wins,
                    BestRemainingHealth = bestRemainingHealth, FastestValidTurnCount = fastestValidTurnCount,
                    LargestCascade = largestCascade, DominantDamageBranchId = Content(dominantDamageBranchId),
                    ChallengeId = Content(challengeId), BestChallengeTurns = bestChallengeTurns,
                    BestChallengeHealth = bestChallengeHealth,
                    BestChallengeLargestCascade = bestChallengeLargestCascade,
                    BestChallengeDominantBranchId = Content(bestChallengeDominantBranchId)
                };
            }
        }

        [Serializable]
        private sealed class AggregateDto
        {
            public int runsStarted;
            public int runsWon;
            public int enemiesDefeated;
            public int elitesDefeated;
            public int bossesDefeated;
            public int specialActivations;
            public int eventChoices;
            public int routeVowsCompleted;
            public int expeditionsCompleted;

            public static AggregateDto FromDomain(ProfileAggregateState value)
            {
                value = value ?? new ProfileAggregateState();
                return new AggregateDto
                {
                    runsStarted = value.RunsStarted, runsWon = value.RunsWon,
                    enemiesDefeated = value.EnemiesDefeated, elitesDefeated = value.ElitesDefeated,
                    bossesDefeated = value.BossesDefeated,
                    specialActivations = value.SpecialActivations,
                    eventChoices = value.EventChoices,
                    routeVowsCompleted = value.RouteVowsCompleted,
                    expeditionsCompleted = value.ExpeditionsCompleted
                };
            }

            public ProfileAggregateState ToDomain()
            {
                return new ProfileAggregateState
                {
                    RunsStarted = runsStarted, RunsWon = runsWon, EnemiesDefeated = enemiesDefeated,
                    ElitesDefeated = elitesDefeated, BossesDefeated = bossesDefeated,
                    SpecialActivations = specialActivations, EventChoices = eventChoices,
                    RouteVowsCompleted = routeVowsCompleted, ExpeditionsCompleted = expeditionsCompleted
                };
            }
        }

        private static void AddIds(List<string> destination, IEnumerable<ContentId> source)
        {
            if (source == null) return;
            foreach (var id in source) destination.Add(id.Value);
        }

        private static List<ContentId> ToIds(IEnumerable<string> source)
        {
            var result = new List<ContentId>();
            if (source == null) return result;
            foreach (var id in source) result.Add(Content(id));
            return result;
        }

        private static ContentId Content(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (ContentId)"content.none" : (ContentId)value;
        }
    }
}
