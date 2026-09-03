using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
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
        public const int EncounterCount = 5;

        private readonly ICheckpointStore _checkpoints;

        public RunState State { get; private set; }
        public RunStatistics Statistics { get; private set; }
        public RunScreen Screen { get; private set; }
        public bool CanResume => _checkpoints.HasCheckpoint;

        public RunDirector(ICheckpointStore checkpoints)
        {
            _checkpoints = checkpoints ?? throw new ArgumentNullException(nameof(checkpoints));
            Screen = RunScreen.Title;
        }

        public RunActionResult StartNewRun(ulong seed)
        {
            _checkpoints.Clear();
            State = new RunState
            {
                Seed = seed,
                RandomStreams = RandomStreams.Create(seed)
            };
            Statistics = new RunStatistics();
            ProgressionSimulation.InitializeRun(State);

            var events = new EventBatch();
            events.Append(BoardSimulation.InitializeBoard(State));
            events.Append(CombatSimulation.StartEncounter(State, 0));
            Screen = RunScreen.Encounter;
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
            Screen = DeriveStableScreen();
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
                ResolvePostActionScreen();
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
            if (Screen != RunScreen.Encounter && Screen != RunScreen.SkillWindow)
                return RunActionResult.Reject("SkillWindowClosed");
            var command = new UseSkillCommand { SkillId = skillId };
            if (targets != null) command.Targets.AddRange(targets);
            var result = ProgressionSimulation.UseActiveSkill(State, command);
            if (!result.Accepted) return RunActionResult.Reject(result.RejectionReason);

            Record(result.Events, 0);
            if (result.EncounterWon)
            {
                ResolvePostActionScreen();
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
            ResolvePostActionScreen();
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult SelectReward(ContentId rewardId)
        {
            if (Screen != RunScreen.Reward) return RunActionResult.Reject("NoPendingChoice");
            var result = ProgressionSimulation.SelectReward(State, new SelectRewardCommand { RewardId = rewardId });
            if (!result.Accepted) return RunActionResult.Reject(result.RejectionReason);
            Record(result.Events, 0);
            ResolvePostActionScreen();
            SaveStableCheckpoint();
            return RunActionResult.Accept(result.Events);
        }

        public RunActionResult EquipSkill(ContentId skillId, int slotIndex)
        {
            if (Screen != RunScreen.BetweenEncounters) return RunActionResult.Reject("LoadoutLocked");
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
            if (Screen != RunScreen.BetweenEncounters) return RunActionResult.Reject("EncounterAdvanceLocked");
            var next = State.EncounterIndex + 1;
            if (next >= EncounterCount) return RunActionResult.Reject("RunComplete");
            var events = CombatSimulation.StartEncounter(State, next);
            Screen = RunScreen.Encounter;
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

        private void ResolvePostActionScreen()
        {
            if (State.Player.Health <= 0)
            {
                Screen = RunScreen.Defeat;
                return;
            }
            if (State.PendingChoice != null && State.PendingChoice.IsPending)
            {
                Screen = RunScreen.Reward;
                return;
            }
            if (State.Enemy.Health <= 0)
            {
                Screen = State.EncounterIndex >= EncounterCount - 1
                    ? RunScreen.Victory
                    : RunScreen.BetweenEncounters;
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
            if (State.Enemy != null && State.Enemy.Health <= 0)
                return State.EncounterIndex >= EncounterCount - 1 ? RunScreen.Victory : RunScreen.BetweenEncounters;
            // Stable checkpoints are never written during this window. Completing it here protects older/debug saves.
            if (State.PendingCombatTurn != null && State.PendingCombatTurn.AwaitingEnemyResponse)
                return RunScreen.SkillWindow;
            return RunScreen.Encounter;
        }

        private void Record(EventBatch events, int cascadeCount)
        {
            if (Statistics == null) Statistics = new RunStatistics();
            Statistics.BiggestCascade = Math.Max(Statistics.BiggestCascade, cascadeCount);
            foreach (var item in events.Events)
            {
                if (item.Type == SimulationEventType.EnemyDefeated)
                    Statistics.EncountersCleared++;
                if (item.Type != SimulationEventType.DamageApplied ||
                    item.Detail.IndexOf("target=enemy", StringComparison.Ordinal) < 0) continue;
                Statistics.TotalDamage += item.Amount;
                AddDamage(item.SourceId.Value, item.Amount);
            }
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

        private void SaveStableCheckpoint()
        {
            if (State == null || (State.PendingCombatTurn != null && State.PendingCombatTurn.AwaitingEnemyResponse)) return;
            _checkpoints.Save(new CheckpointSnapshot(State, Statistics));
        }
    }
}
