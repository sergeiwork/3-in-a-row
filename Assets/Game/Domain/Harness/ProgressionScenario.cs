using System;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.Replay;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Harness
{
    /// <summary>
    /// Replayable Session D handoff fixture: grant the first level, select a deterministic offered
    /// reward, carry it into combat, use Sunder in the post-cascade window, and finish the turn.
    /// </summary>
    public static class ProgressionScenario
    {
        public const ulong DefaultSeed = 0xD09E5510UL;

        public static ProgressionScenarioResult Run(ulong seed = DefaultSeed)
        {
            var state = new RunState
            {
                Seed = seed,
                RandomStreams = RandomStreams.Create(seed)
            };
            ProgressionSimulation.InitializeRun(state);

            var events = new EventBatch();
            ProgressionSimulation.GrantExperience(
                state, 2, ProgressionContentIds.SystemProgression, events);
            if (!state.PendingChoice.IsPending || state.PendingChoice.OptionIds.Count != 3)
                throw new InvalidOperationException("The first level did not offer three rewards.");

            var selectedReward = state.PendingChoice.OptionIds[0];
            var selection = ProgressionSimulation.SelectReward(state, new SelectRewardCommand
            {
                RewardId = selectedReward
            });
            if (!selection.Accepted)
                throw new InvalidOperationException("The progression fixture could not select an offered reward.");
            events.Append(selection.Events);

            BoardSimulation.InitializeBoard(state);
            events.Append(CombatSimulation.StartEncounter(state, 0));
            var legalSwaps = BoardSimulation.FindLegalSwaps(state.Board);
            if (legalSwaps.Count == 0)
                throw new InvalidOperationException("The progression fixture board has no legal swap.");

            var swap = legalSwaps[0];
            var playerResolution = CombatSimulation.BeginSwap(state, new SwapCommand
            {
                CellA = swap.CellA,
                CellB = swap.CellB
            });
            if (!playerResolution.Accepted)
                throw new InvalidOperationException("The progression fixture swap was rejected.");
            events.Append(playerResolution.Events);

            if (!playerResolution.EncounterWon)
            {
                var skill = ProgressionSimulation.UseActiveSkill(state, new UseSkillCommand
                {
                    SkillId = ProgressionContentIds.Sunder
                });
                if (!skill.Accepted)
                    throw new InvalidOperationException("Sunder was unavailable in the post-cascade skill window.");
                events.Append(skill.Events);
                if (!skill.EncounterWon)
                    events.Append(CombatSimulation.CompleteTurn(state).Events);
            }

            return new ProgressionScenarioResult(
                state,
                events,
                selectedReward,
                DeterministicStateHasher.Hash(state, events));
        }
    }

    public sealed class ProgressionScenarioResult
    {
        public readonly RunState State;
        public readonly EventBatch Events;
        public readonly ContentId SelectedReward;
        public readonly string StateHash;

        public ProgressionScenarioResult(
            RunState state,
            EventBatch events,
            ContentId selectedReward,
            string stateHash)
        {
            State = state;
            Events = events;
            SelectedReward = selectedReward;
            StateHash = stateHash;
        }
    }
}
