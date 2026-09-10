using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Map;
using ThreeInARow.Domain.Mastery;
using ThreeInARow.Domain.Meta;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Application
{
    public enum RunScreen
    {
        Title,
        Encounter,
        SkillWindow,
        Reward,
        BetweenEncounters,
        Map,
        Event,
        Rest,
        Sanctum,
        Victory,
        Defeat
    }

    [Serializable]
    public sealed class RunStatistics
    {
        public int EncountersCleared;
        public int BiggestCascade;
        public int TotalDamage;
        public List<DamageStatistic> DamageBySource = new List<DamageStatistic>();
        public List<string> RouteNodeIds = new List<string>();
        public List<string> EventChoiceIds = new List<string>();
        public int HealthDamageTaken;
        public int CurrentEncounterHealthDamageTaken;
        public int MaxPoisonStacksInResponse;
        public int MaxCleanseStatusKinds;
        public int FlawlessEliteVictories;
        public int SpecialActivations;
        public int SparkActivations;
        public int FocusConversions;
        public int PoisonApplications;
        public int CompletedRouteVows;
        public List<string> CompletedRouteVowIds = new List<string>();
    }

    [Serializable]
    public sealed class DamageStatistic
    {
        public string SourceId = string.Empty;
        public int Amount;
    }

    public sealed class CheckpointSnapshot
    {
        public readonly RunState State;
        public readonly RunStatistics Statistics;

        public CheckpointSnapshot(RunState state, RunStatistics statistics)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Statistics = statistics ?? new RunStatistics();
        }
    }

    public interface ICheckpointStore
    {
        bool HasCheckpoint { get; }
        void Save(CheckpointSnapshot snapshot);
        bool TryLoad(out CheckpointSnapshot snapshot);
        void Clear();
    }

    public sealed class RunActionResult
    {
        public readonly bool Accepted;
        public readonly string Rejection;
        public readonly EventBatch Events;

        private RunActionResult(bool accepted, string rejection, EventBatch events)
        {
            Accepted = accepted;
            Rejection = rejection ?? string.Empty;
            Events = events ?? new EventBatch();
        }

        public static RunActionResult Accept(EventBatch events)
        {
            return new RunActionResult(true, string.Empty, events);
        }

        public static RunActionResult Reject(object reason)
        {
            return new RunActionResult(false, reason == null ? "Rejected" : reason.ToString(), new EventBatch());
        }
    }

    /// <summary>
    /// The application boundary for one local run. It is the only presentation-facing type that invokes
    /// domain commands, advances encounters, and decides when a checkpoint is safe to write.
    /// </summary>
    public sealed class RunDirector
    {
        public const int EncounterCount = 21;

        private readonly ICheckpointStore _checkpoints;
        private readonly IProfileStore _profiles;
        private bool _profileRunCompleted;

        public RunState State { get; private set; }
        public RunStatistics Statistics { get; private set; }
        public RunScreen Screen { get; private set; }
        public bool CanResume => _checkpoints.HasCheckpoint;
        public ProfileState Profile { get; private set; }
        public ProfileUpdateResult LastProfileUpdate { get; private set; }

        public RunDirector(ICheckpointStore checkpoints, IProfileStore profiles = null)
        {
            _checkpoints = checkpoints ?? throw new ArgumentNullException(nameof(checkpoints));
            _profiles = profiles ?? new MemoryProfileStore();
            Profile = _profiles.LoadOrCreate() ?? ProfileProgression.CreateFresh();
            ProfileProgression.Normalize(Profile);
            Screen = RunScreen.Title;
        }

        public RunActionResult StartNewRun(ulong seed)
        {
            return StartNewRun(seed, 0);
        }

        public RunActionResult StartNewRun(ulong seed, int difficultyTier)
        {
            if (difficultyTier < 0 || difficultyTier > Profile.BestDifficultyUnlocked)
                return RunActionResult.Reject("DifficultyLocked");
            return StartRun(seed, difficultyTier, false, MasteryContentIds.StandardRun,
                MasteryContentIds.ProfileUnlockPolicy, RunState.CurrentContentVersion);
        }

        public RunActionResult StartWeeklyChallenge(WeeklyChallengeDefinition challenge)
        {
            if (challenge == null) throw new ArgumentNullException(nameof(challenge));
            if (!string.Equals(challenge.ContentVersion, RunState.CurrentContentVersion, StringComparison.Ordinal))
                return RunActionResult.Reject("ChallengeContentVersionMismatch");
            return StartRun(challenge.Seed, challenge.DifficultyTier, true, challenge.Id,
                challenge.UnlockPolicyId, challenge.ContentVersion);
        }

        public RunActionResult StartExpedition(ExpeditionDefinition expedition)
        {
            if (expedition == null) throw new ArgumentNullException(nameof(expedition));
            if (!string.Equals(expedition.ContentVersion, RunState.CurrentContentVersion, StringComparison.Ordinal))
                return RunActionResult.Reject("ChallengeContentVersionMismatch");
            return StartRun(expedition.Seed, expedition.DifficultyTier, true, expedition.Id,
                MasteryContentIds.AllContentUnlockPolicy, expedition.ContentVersion,
                expedition.RegionIndex, expedition.RegionIndex, expedition.StartingSkillId,
                expedition.StartingStatusId, expedition.StartingStatusCount);
        }

        private RunActionResult StartRun(ulong seed, int difficultyTier, bool isChallenge,
            ContentId challengeId, ContentId unlockPolicyId, string challengeContentVersion,
            int startRegionIndex = 0, int finalRegionIndex = 2,
            ContentId startingSkillId = default(ContentId), ContentId startingStatusId = default(ContentId),
            int startingStatusCount = 0)
        {
            _checkpoints.Clear();
            var difficulty = MasteryContentCatalog.Instance.Get(difficultyTier);
            State = new RunState
            {
                Seed = seed,
                RandomStreams = RandomStreams.Create(seed),
                DifficultyTier = difficultyTier,
                DifficultyId = difficulty.Id,
                IsChallengeRun = isChallenge,
                ChallengeId = challengeId,
                UnlockPolicyId = unlockPolicyId,
                ChallengeContentVersion = challengeContentVersion,
                AvailableContentIds = new List<ContentId>(),
                RegionIndex = startRegionIndex,
                FinalRegionIndex = finalRegionIndex
            };
            if (unlockPolicyId.Equals(MasteryContentIds.AllContentUnlockPolicy))
            {
                foreach (var challenge in ProfileContentCatalog.Instance.Challenges)
                    if (!Contains(State.AvailableContentIds, challenge.UnlockContentId))
                        State.AvailableContentIds.Add(challenge.UnlockContentId);
            }
            else if (Profile.UnlockedContentIds != null)
            {
                State.AvailableContentIds.AddRange(Profile.UnlockedContentIds);
            }
            Statistics = new RunStatistics();
            LastProfileUpdate = new ProfileUpdateResult();
            _profileRunCompleted = false;
            Profile.Aggregate.RunsStarted++;
            _profiles.Save(Profile);
            ProgressionSimulation.InitializeRun(State);
            var events = new EventBatch();
            if (!string.IsNullOrEmpty(startingSkillId.Value))
            {
                ProgressionSimulation.LearnBonusSkill(State, startingSkillId);
                var startingSkill = MvpProgressionContentCatalog.Instance.GetSkill(startingSkillId);
                if (startingSkill.SlotType == SkillSlotType.Active)
                {
                    var equip = ProgressionSimulation.EquipActiveSkill(State,
                        new EquipSkillCommand { SkillId = startingSkillId, SlotIndex = 0 });
                    if (equip.Accepted) events.Append(equip.Events);
                }
            }

            events.Append(BoardSimulation.InitializeBoard(State));
            if (startingStatusCount > 0 && !string.IsNullOrEmpty(startingStatusId.Value))
                events.Append(MapSimulation.ApplyStartingStatus(State, startingStatusId, startingStatusCount));
            events.Append(MapSimulation.Generate(State));
            Screen = RunScreen.Map;
            Record(events, 0);
            SaveStableCheckpoint();
            return RunActionResult.Accept(events);
        }

        public bool Resume()
        {
            CheckpointSnapshot snapshot;
            if (!_checkpoints.TryLoad(out snapshot)) return false;
            State = snapshot.State;
            Statistics = snapshot.Statistics;
            ProgressionSimulation.InitializeRun(State);
            BoardSimulation.EnsurePlayable(State);
            Screen = DeriveStableScreen();
            _profileRunCompleted = Screen == RunScreen.Victory || Screen == RunScreen.Defeat;
            SaveStableCheckpoint();
            return true;
        }

        public RunActionResult Swap(GridCell first, GridCell second)
        {
            if (Screen != RunScreen.Encounter) return RunActionResult.Reject("InputLocked");
            var result = CombatSimulation.BeginSwap(State, new SwapCommand { CellA = first, CellB = second });
            if (!result.Accepted) return RunActionResult.Reject(result.RejectionReason);

            Record(result.Events, result.CascadeCount);
            if (result.EncounterWon)
            {
                ResolvePostActionScreen(result.Events);
                SaveStableCheckpoint();
            }
            else
            {
                Screen = RunScreen.SkillWindow;
            }
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult UseSkill(ContentId skillId, IEnumerable<GridCell> targets)
        {
            return UseSkill(skillId, targets, "content.none");
        }

        public RunActionResult UseSkill(ContentId skillId, IEnumerable<GridCell> targets, ContentId optionId)
        {
            if (Screen != RunScreen.Encounter && Screen != RunScreen.SkillWindow)
                return RunActionResult.Reject("SkillWindowClosed");
            var command = new UseSkillCommand { SkillId = skillId };
            if (targets != null) command.Targets.AddRange(targets);
            command.OptionId = optionId;
            var result = ProgressionSimulation.UseActiveSkill(State, command);
            if (!result.Accepted) return RunActionResult.Reject(result.RejectionReason);

            Record(result.Events, 0);
            if (result.EncounterWon)
            {
                ResolvePostActionScreen(result.Events);
                SaveStableCheckpoint();
            }
            else if (Screen == RunScreen.Encounter)
            {
                SaveStableCheckpoint();
            }
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult ContinueTurn()
        {
            if (Screen != RunScreen.SkillWindow) return RunActionResult.Reject("NoPendingEnemyResponse");
            var result = CombatSimulation.CompleteTurn(State);
            Record(result.Events, result.CascadeCount);
            ResolvePostActionScreen(result.Events);
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult SelectReward(ContentId rewardId)
        {
            if (Screen != RunScreen.Reward) return RunActionResult.Reject("NoPendingChoice");
            var result = ProgressionSimulation.SelectReward(State, new SelectRewardCommand { RewardId = rewardId });
            if (!result.Accepted) return RunActionResult.Reject(result.RejectionReason);
            Record(result.Events, 0);
            ResolvePostActionScreen(result.Events);
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult EquipSkill(ContentId skillId, int slotIndex)
        {
            if (Screen != RunScreen.Map && Screen != RunScreen.BetweenEncounters && Screen != RunScreen.Sanctum)
                return RunActionResult.Reject("LoadoutLocked");
            var result = ProgressionSimulation.EquipActiveSkill(
                State,
                new EquipSkillCommand { SkillId = skillId, SlotIndex = slotIndex });
            if (!result.Accepted) return RunActionResult.Reject(result.RejectionReason);
            Record(result.Events, 0);
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult StartNextEncounter()
        {
            return RunActionResult.Reject("SelectMapNode");
        }

        public RunActionResult SelectMapNode(ContentId nodeId)
        {
            if (Screen != RunScreen.Map) return RunActionResult.Reject("MapSelectionLocked");
            var selection = MapSimulation.SelectNode(State, new SelectMapNodeCommand { NodeId = nodeId });
            if (!selection.Accepted) return RunActionResult.Reject(selection.Rejection);

            Record(selection.Events, 0);
            SaveStableCheckpoint();
            var events = new EventBatch();
            events.Append(selection.Events);
            var node = MapSimulation.GetCurrentNode(State);
            if (node.Type == MapNodeType.NormalCombat || node.Type == MapNodeType.EliteCombat || node.Type == MapNodeType.Boss)
            {
                Statistics.CurrentEncounterHealthDamageTaken = 0;
                var tuningDepth = Math.Max(0, Math.Min(4, node.Row));
                var encounterEvents = CombatSimulation.StartEncounter(State, node.ContentId, tuningDepth);
                events.Append(encounterEvents);
                Screen = RunScreen.Encounter;
                Record(encounterEvents, 0);
            }
            else
            {
                events.Append(MapSimulation.BeginNonCombatNode(State));
                Screen = node.Type == MapNodeType.Rest ? RunScreen.Rest : RunScreen.Event;
            }
            SaveStableCheckpoint();
            return RunActionResult.Accept(events);
        }

        public RunActionResult SelectEventChoice(ContentId choiceId)
        {
            if (Screen != RunScreen.Event && Screen != RunScreen.Rest)
                return RunActionResult.Reject("NoPendingEvent");
            var result = MapSimulation.SelectEventChoice(
                State, new SelectEventChoiceCommand { ChoiceId = choiceId });
            if (!result.Accepted) return RunActionResult.Reject(result.Rejection);
            Record(result.Events, 0);
            if (State.Player.Health <= 0)
            {
                Screen = RunScreen.Defeat;
                CompleteProfileRun(false);
            }
            else if (State.PendingChoice != null && State.PendingChoice.IsPending) Screen = RunScreen.Reward;
            else Screen = RunScreen.Map;
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult PinRouteVow(ContentId vowId)
        {
            if (Screen != RunScreen.Map) return RunActionResult.Reject("VowUnavailable");
            var result = MapSimulation.PinRouteVow(State, new PinRouteVowCommand { VowId = vowId });
            if (!result.Accepted) return RunActionResult.Reject(result.Rejection);
            Record(result.Events, 0);
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult ContinueFromSanctum()
        {
            if (Screen != RunScreen.Sanctum || State.Sanctum == null || !State.Sanctum.Active)
                return RunActionResult.Reject("NoSanctum");
            if (State.RegionIndex >= State.FinalRegionIndex) return RunActionResult.Reject("FinalRegionReached");
            State.RegionIndex++;
            State.Sanctum = new SanctumState();
            var events = MapSimulation.Generate(State);
            events.Add(SimulationEventType.RegionAdvanced, MapContentIds.SystemMap,
                "region=" + State.RegionIndex, State.RegionIndex);
            Screen = RunScreen.Map;
            Record(events, 0);
            SaveStableCheckpoint();
            return RunActionResult.Accept(events);
        }

        public void ReturnToTitle(bool abandonRun)
        {
            if (abandonRun) _checkpoints.Clear();
            State = null;
            Statistics = null;
            Screen = RunScreen.Title;
        }

        private void ResolvePostActionScreen(EventBatch events)
        {
            if (State.Player.Health <= 0)
            {
                Screen = RunScreen.Defeat;
                CompleteProfileRun(false);
                return;
            }
            if (State.Sanctum != null && State.Sanctum.Active)
            {
                if ((State.PendingChoice == null || !State.PendingChoice.IsPending) &&
                    (State.Sanctum.ChosenEvolutionId.Value == null ||
                     State.Sanctum.ChosenEvolutionId.Value == "skill.none"))
                {
                    var evolutionOptions = State.RouteVow != null && State.RouteVow.Completed ? 4 : 3;
                    ProgressionSimulation.OfferEvolutionReward(State, evolutionOptions, events);
                }
                Screen = State.PendingChoice != null && State.PendingChoice.IsPending
                    ? RunScreen.Reward
                    : RunScreen.Sanctum;
                return;
            }
            if (State.Enemy.Health <= 0)
            {
                var node = MapSimulation.GetCurrentNode(State);
                MapSimulation.CompleteCurrentNode(State, events);
                if (node != null && node.Type == MapNodeType.Boss)
                {
                    if (MapSimulation.CompleteRouteVowAtBoss(State, events))
                    {
                        Statistics.CompletedRouteVows++;
                        var completedVowId = State.RouteVow.PinnedId.Value;
                        if (!Statistics.CompletedRouteVowIds.Contains(completedVowId))
                            Statistics.CompletedRouteVowIds.Add(completedVowId);
                    }
                    if (State.RegionIndex < State.FinalRegionIndex)
                    {
                        State.Sanctum = new SanctumState
                        {
                            Active = true,
                            CompletedRegionIndex = State.RegionIndex
                        };
                        var evolutionOptions = State.RouteVow != null && State.RouteVow.Completed ? 4 : 3;
                        ProgressionSimulation.OfferEvolutionReward(State, evolutionOptions, events);
                        events.Add(SimulationEventType.SanctumEntered, node.ContentId,
                            "region=" + State.RegionIndex, State.RegionIndex + 1);
                        Screen = State.PendingChoice != null && State.PendingChoice.IsPending
                            ? RunScreen.Reward
                            : RunScreen.Sanctum;
                    }
                    else
                    {
                        Screen = RunScreen.Victory;
                        CompleteProfileRun(true);
                    }
                }
                else if (State.PendingChoice != null && State.PendingChoice.IsPending)
                    Screen = RunScreen.Reward;
                else
                    Screen = RunScreen.Map;
                return;
            }
            if (State.PendingChoice != null && State.PendingChoice.IsPending)
            {
                Screen = RunScreen.Reward;
                return;
            }
            Screen = State.PendingCombatTurn != null && State.PendingCombatTurn.AwaitingEnemyResponse
                ? RunScreen.SkillWindow
                : RunScreen.Encounter;
        }

        private RunScreen DeriveStableScreen()
        {
            if (State.Player.Health <= 0) return RunScreen.Defeat;
            if (State.PendingChoice != null && State.PendingChoice.IsPending) return RunScreen.Reward;
            if (State.Sanctum != null && State.Sanctum.Active) return RunScreen.Sanctum;
            if (State.PendingEvent != null && State.PendingEvent.IsPending)
            {
                var pendingNode = MapSimulation.GetCurrentNode(State);
                return pendingNode != null && pendingNode.Type == MapNodeType.Rest ? RunScreen.Rest : RunScreen.Event;
            }
            if (State.Enemy != null && State.Enemy.Health <= 0)
            {
                var completedNode = MapSimulation.GetCurrentNode(State);
                if (completedNode != null && completedNode.Type == MapNodeType.Boss &&
                    State.RegionIndex >= State.FinalRegionIndex) return RunScreen.Victory;
                return RunScreen.Map;
            }
            // Stable checkpoints are never written during this window. Completing it here protects older/debug saves.
            if (State.PendingCombatTurn != null && State.PendingCombatTurn.AwaitingEnemyResponse)
                return RunScreen.SkillWindow;
            var node = MapSimulation.GetCurrentNode(State);
            if (node == null || node.Completed) return RunScreen.Map;
            return RunScreen.Encounter;
        }

        private void Record(EventBatch events, int cascadeCount)
        {
            if (Statistics == null) Statistics = new RunStatistics();
            Statistics.BiggestCascade = Math.Max(Statistics.BiggestCascade, cascadeCount);
            var cleanseKinds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in events.Events)
            {
                if (item.Type == SimulationEventType.EnemyDefeated)
                {
                    Statistics.EncountersCleared++;
                    Profile.Aggregate.EnemiesDefeated++;
                    var enemy = MvpCombatContentCatalog.Instance.GetEnemy(item.SourceId);
                    if (enemy.IsElite)
                    {
                        Profile.Aggregate.ElitesDefeated++;
                        if (Statistics.CurrentEncounterHealthDamageTaken == 0)
                            Statistics.FlawlessEliteVictories++;
                    }
                    if (enemy.IsBoss) Profile.Aggregate.BossesDefeated++;
                }
                if (item.Type == SimulationEventType.MapNodeSelected)
                    Statistics.RouteNodeIds.Add(item.SourceId.Value);
                if (item.Type == SimulationEventType.EventChoiceSelected)
                    Statistics.EventChoiceIds.Add(item.SourceId.Value);
                if (item.Type == SimulationEventType.RouteVowCompleted)
                {
                    Statistics.CompletedRouteVows++;
                    if (!Statistics.CompletedRouteVowIds.Contains(item.SourceId.Value))
                        Statistics.CompletedRouteVowIds.Add(item.SourceId.Value);
                }
                if (item.Type == SimulationEventType.SpecialActivated)
                {
                    Statistics.SpecialActivations++;
                    if (item.SourceId.Value == "special.spark") Statistics.SparkActivations++;
                }
                if (item.Type == SimulationEventType.ResourceChanged &&
                    item.Detail.IndexOf("resource=focus;reason=conversion", StringComparison.Ordinal) >= 0)
                    Statistics.FocusConversions++;
                if (item.Type == SimulationEventType.StatusAdded && item.SourceId.Value == "status.poison")
                    Statistics.PoisonApplications += item.Amount;
                if (item.Type == SimulationEventType.StatusRemoved && item.RelatedId.Value == "skill.cleanse")
                    cleanseKinds.Add(item.SourceId.Value);
                if (item.Type == SimulationEventType.StatusTicked && item.SourceId.Value == "status.poison")
                    Statistics.MaxPoisonStacksInResponse = Math.Max(
                        Statistics.MaxPoisonStacksInResponse, ParseDetailInt(item.Detail, "stacks="));
                if (item.Type == SimulationEventType.DamageApplied &&
                    item.Detail.IndexOf("target=player", StringComparison.Ordinal) >= 0)
                {
                    Statistics.HealthDamageTaken += item.Amount;
                    Statistics.CurrentEncounterHealthDamageTaken += item.Amount;
                }
                if (item.Type == SimulationEventType.DamageApplied &&
                    item.Detail.IndexOf("target=enemy", StringComparison.Ordinal) >= 0)
                {
                    Statistics.TotalDamage += item.Amount;
                    AddDamage(item.SourceId.Value, item.Amount);
                }
                Discover(item);
            }
            Statistics.MaxCleanseStatusKinds = Math.Max(Statistics.MaxCleanseStatusKinds, cleanseKinds.Count);
            _profiles.Save(Profile);
        }

        private void AddDamage(string sourceId, int amount)
        {
            foreach (var statistic in Statistics.DamageBySource)
            {
                if (!string.Equals(statistic.SourceId, sourceId, StringComparison.Ordinal)) continue;
                statistic.Amount += amount;
                return;
            }
            Statistics.DamageBySource.Add(new DamageStatistic { SourceId = sourceId, Amount = amount });
        }

        private void CompleteProfileRun(bool victory)
        {
            if (_profileRunCompleted || State == null || Statistics == null) return;
            _profileRunCompleted = true;
            var dominantBranches = DominantDamageBranches(Statistics.DamageBySource);
            var signals = new RunCompletionSignals
            {
                Victory = victory,
                BossId = State.Map == null ? (ContentId)"enemy.unset" : State.Map.BossEnemyId,
                DifficultyTier = State.DifficultyTier,
                RemainingHealth = State.Player.Health,
                ValidTurnCount = State.ResolvedTurnCount,
                LargestCascade = Statistics.BiggestCascade,
                DominantBranchId = dominantBranches.Count == 0 ? (ContentId)"branch.none" : dominantBranches[0],
                DominantBranchIds = dominantBranches,
                EmberSkillsLearned = CountLearnedBranch("ember"),
                MaxPoisonStacksInResponse = Statistics.MaxPoisonStacksInResponse,
                MaxCleanseStatusKinds = Statistics.MaxCleanseStatusKinds,
                FlawlessEliteVictories = Statistics.FlawlessEliteVictories,
                SpecialActivations = Statistics.SpecialActivations,
                SparkActivations = Statistics.SparkActivations,
                FocusConversions = Statistics.FocusConversions,
                PoisonApplications = Statistics.PoisonApplications,
                IsChallengeRun = State.IsChallengeRun,
                ChallengeId = State.ChallengeId,
                Seed = State.Seed,
                FinalRegionIndex = State.FinalRegionIndex,
                EventChoices = Statistics.EventChoiceIds == null ? 0 : Statistics.EventChoiceIds.Count,
                CompletedRouteVows = Statistics.CompletedRouteVows,
                IsExpedition = State.ChallengeId.Value != null && State.ChallengeId.Value.StartsWith("expedition.", StringComparison.Ordinal),
                SkillIds = new List<ContentId>(State.SelectedSkillIds)
            };
            if (Statistics.CompletedRouteVowIds != null)
                foreach (var id in Statistics.CompletedRouteVowIds) signals.RouteVowIds.Add((ContentId)id);
            if (Statistics.RouteNodeIds != null)
                foreach (var id in Statistics.RouteNodeIds) signals.RouteNodeIds.Add((ContentId)id);
            LastProfileUpdate = ProfileProgression.CompleteRun(Profile, signals);
            _profiles.Save(Profile);
        }

        private int CountLearnedBranch(string branch)
        {
            var count = 0;
            foreach (var skillId in State.SelectedSkillIds)
            {
                try
                {
                    if (string.Equals(MvpProgressionContentCatalog.Instance.GetSkill(skillId).BranchTag,
                        branch, StringComparison.Ordinal)) count++;
                }
                catch (KeyNotFoundException) { }
            }
            return count;
        }

        public static List<ContentId> DominantDamageBranches(IEnumerable<DamageStatistic> damageBySource)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "branch.ember", 0 }, { "branch.tide", 0 }, { "branch.venom", 0 }, { "branch.volt", 0 }
            };
            if (damageBySource != null)
            {
                foreach (var damage in damageBySource)
                {
                    var branch = BranchForDamageSource(damage.SourceId);
                    if (branch != null) totals[branch] += damage.Amount;
                }
            }
            var maximum = 0;
            foreach (var amount in totals.Values) maximum = Math.Max(maximum, amount);
            var result = new List<ContentId>();
            if (maximum <= 0) return result;
            foreach (var branch in new[] { "branch.ember", "branch.tide", "branch.venom", "branch.volt" })
            {
                if (totals[branch] == maximum) result.Add((ContentId)branch);
            }
            return result;
        }

        private static string BranchForDamageSource(string sourceId)
        {
            if (sourceId == "gem.ember" || sourceId == "special.spark" || sourceId == "skill.scalding_current") return "branch.ember";
            if (sourceId == "gem.tide" || sourceId == "special.current") return "branch.tide";
            if (sourceId == "gem.venom" || sourceId == "special.spore" || sourceId == "status.poison") return "branch.venom";
            if (sourceId == "gem.volt" || sourceId == "special.charge") return "branch.volt";
            return null;
        }

        private void Discover(SimulationEvent item)
        {
            DiscoverId(item.SourceId);
            DiscoverId(item.RelatedId);
            if (item.Type == SimulationEventType.EnemyIntentTelegraphed)
                ProfileProgression.Discover(Profile, CodexCategory.Intent, item.SourceId, item.RelatedId);
            if (item.Type == SimulationEventType.MapNodeSelected && item.RelatedId.Value != null &&
                item.RelatedId.Value.StartsWith("event.", StringComparison.Ordinal))
                ProfileProgression.Discover(Profile, CodexCategory.Event, item.RelatedId);
        }

        private void DiscoverId(ContentId id)
        {
            var value = id.Value ?? string.Empty;
            if (value.StartsWith("gem.", StringComparison.Ordinal)) ProfileProgression.Discover(Profile, CodexCategory.Gem, id);
            else if (value.StartsWith("special.", StringComparison.Ordinal) && value != "special.none") ProfileProgression.Discover(Profile, CodexCategory.Special, id);
            else if (value.StartsWith("status.", StringComparison.Ordinal) && value != "status.none") ProfileProgression.Discover(Profile, CodexCategory.Status, id);
            else if (value.StartsWith("enemy.", StringComparison.Ordinal) && value != "enemy.unset")
            {
                var category = CodexCategory.Enemy;
                try
                {
                    var enemy = MvpCombatContentCatalog.Instance.GetEnemy(id);
                    if (enemy.IsBoss) category = CodexCategory.Boss;
                    else if (enemy.IsElite) category = CodexCategory.Elite;
                }
                catch (KeyNotFoundException) { }
                ProfileProgression.Discover(Profile, category, id);
            }
            else if (value.StartsWith("skill.", StringComparison.Ordinal)) ProfileProgression.Discover(Profile, CodexCategory.Skill, id);
            else if (value.StartsWith("event.", StringComparison.Ordinal)) ProfileProgression.Discover(Profile, CodexCategory.Event, id);
        }

        private static int ParseDetailInt(string detail, string marker)
        {
            if (string.IsNullOrEmpty(detail)) return 0;
            var start = detail.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return 0;
            start += marker.Length;
            var end = detail.IndexOf(';', start);
            var text = end < 0 ? detail.Substring(start) : detail.Substring(start, end - start);
            int value;
            return int.TryParse(text, style: System.Globalization.NumberStyles.Integer,
                provider: System.Globalization.CultureInfo.InvariantCulture, result: out value) ? value : 0;
        }

        private static bool Contains(IEnumerable<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return false;
            foreach (var id in ids) if (id.Equals(wanted)) return true;
            return false;
        }

        private void SaveStableCheckpoint()
        {
            if (State == null || (State.PendingCombatTurn != null && State.PendingCombatTurn.AwaitingEnemyResponse)) return;
            _checkpoints.Save(new CheckpointSnapshot(State, Statistics));
        }
    }
}
