using System;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.Replay;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Harness
{
    /// <summary>
    /// Replayable Session C handoff fixture: start encounter one, resolve the first legal row-major
    /// swap through combat, and hash the resulting board, combat state, RNG streams, and event order.
    /// </summary>
    public static class CombatScenario
    {
        public const ulong DefaultSeed = 0xC04BA7EUL;

        public static CombatScenarioResult Run(ulong seed = DefaultSeed)
        {
            var state = new RunState
            {
                Seed = seed,
                RandomStreams = RandomStreams.Create(seed)
            };
            BoardSimulation.InitializeBoard(state);
            CombatSimulation.StartEncounter(state, 0);

            var legalSwaps = BoardSimulation.FindLegalSwaps(state.Board);
            if (legalSwaps.Count == 0)
                throw new InvalidOperationException("The combat fixture board has no legal swap.");

            var selected = legalSwaps[0];
            var turn = CombatSimulation.ResolveSwap(state, new SwapCommand
            {
                CellA = selected.CellA,
                CellB = selected.CellB
            });
            if (!turn.Accepted)
                throw new InvalidOperationException("Combat rejected the board's first reported legal swap.");

            return new CombatScenarioResult(
                state,
                turn.Events,
                selected,
                turn.EncounterWon,
                turn.RunLost,
                DeterministicStateHasher.Hash(state, turn.Events));
        }
    }

    public sealed class CombatScenarioResult
    {
        public readonly RunState State;
        public readonly EventBatch Events;
        public readonly LegalSwap SelectedSwap;
        public readonly bool EncounterWon;
        public readonly bool RunLost;
        public readonly string StateHash;

        public CombatScenarioResult(
            RunState state,
            EventBatch events,
            LegalSwap selectedSwap,
            bool encounterWon,
            bool runLost,
            string stateHash)
        {
            State = state;
            Events = events;
            SelectedSwap = selectedSwap;
            EncounterWon = encounterWon;
            RunLost = runLost;
            StateHash = stateHash;
        }
    }
}
