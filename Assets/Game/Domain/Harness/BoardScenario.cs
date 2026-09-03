using System;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.Replay;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Harness
{
    /// <summary>
    /// Replayable Session B handoff fixture. It initializes a board, selects the first legal swap in
    /// row-major order, fully resolves it, and hashes the resulting board, RNG state, and event batch.
    /// </summary>
    public static class BoardScenario
    {
        public const ulong DefaultSeed = 0xB04D5EEDUL;

        public static BoardScenarioResult Run(ulong seed = DefaultSeed)
        {
            var state = new RunState
            {
                Seed = seed,
                RandomStreams = RandomStreams.Create(seed)
            };

            BoardSimulation.InitializeBoard(state);
            var legalSwaps = BoardSimulation.FindLegalSwaps(state.Board);
            if (legalSwaps.Count == 0)
                throw new InvalidOperationException("The initialized board has no legal swap.");

            var selected = legalSwaps[0];
            var resolution = BoardSimulation.ResolveSwap(state, new SwapCommand
            {
                CellA = selected.CellA,
                CellB = selected.CellB
            });
            if (!resolution.Accepted)
                throw new InvalidOperationException("The board rejected a swap returned by its own legal-swap query.");

            return new BoardScenarioResult(
                state,
                resolution.Events,
                selected,
                resolution.CascadeCount,
                resolution.Reshuffled,
                DeterministicStateHasher.Hash(state, resolution.Events));
        }
    }

    public sealed class BoardScenarioResult
    {
        public readonly RunState State;
        public readonly EventBatch Events;
        public readonly LegalSwap SelectedSwap;
        public readonly int CascadeCount;
        public readonly bool Reshuffled;
        public readonly string StateHash;

        public BoardScenarioResult(
            RunState state,
            EventBatch events,
            LegalSwap selectedSwap,
            int cascadeCount,
            bool reshuffled,
            string stateHash)
        {
            State = state;
            Events = events;
            SelectedSwap = selectedSwap;
            CascadeCount = cascadeCount;
            Reshuffled = reshuffled;
            StateHash = stateHash;
        }
    }
}
