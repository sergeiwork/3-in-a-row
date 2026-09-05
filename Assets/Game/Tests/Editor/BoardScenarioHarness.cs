using System;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Harness;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.Replay;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Tests
{
    /// <summary>Zero-dependency Session B acceptance harness. It is callable by later EditMode tests.</summary>
    public static class BoardScenarioHarness
    {
        public static string AssertRepeatableAndPlayable()
        {
            AssertInitialBoardsAreStable();
            var first = BoardScenario.Run();
            var second = BoardScenario.Run();
            if (!string.Equals(first.StateHash, second.StateHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Board scenario is not repeatable.");
            if (first.State.Board.Gems.Count != BoardState.Width * BoardState.Height)
                throw new InvalidOperationException("Resolved board is not full.");
            if (BoardSimulation.FindLegalSwaps(first.State.Board).Count == 0)
                throw new InvalidOperationException("Resolved board is soft-locked.");

            var beforeRejectedSwap = DeterministicStateHasher.Hash(first.State, new EventBatch());
            var rejected = BoardSimulation.ResolveSwap(first.State, new SwapCommand
            {
                CellA = new GridCell(0, 0),
                CellB = new GridCell(0, 0)
            });
            var afterRejectedSwap = DeterministicStateHasher.Hash(first.State, new EventBatch());
            if (rejected.Accepted || rejected.Events.Events.Count != 0 || beforeRejectedSwap != afterRejectedSwap)
                throw new InvalidOperationException("A rejected swap changed board or RNG state.");
            return first.StateHash;
        }

        private static void AssertInitialBoardsAreStable()
        {
            for (ulong seed = 1; seed <= 512; seed++)
            {
                var state = new RunState
                {
                    Seed = seed,
                    RandomStreams = RandomStreams.Create(seed)
                };
                BoardSimulation.InitializeBoard(state);
                if (BoardSimulation.HasPreExistingMatch(state.Board))
                    throw new InvalidOperationException("An initialized board contains a pre-existing match for seed " + seed + ".");
                if (BoardSimulation.FindLegalSwaps(state.Board).Count == 0)
                    throw new InvalidOperationException("An initialized board is soft-locked for seed " + seed + ".");
            }

            var repaired = new RunState
            {
                Seed = 513,
                RandomStreams = RandomStreams.Create(513)
            };
            BoardSimulation.InitializeBoard(repaired);
            repaired.Board.Gems[0].GemId = BoardContentIds.Ember;
            repaired.Board.Gems[1].GemId = BoardContentIds.Ember;
            repaired.Board.Gems[2].GemId = BoardContentIds.Ember;
            if (!BoardSimulation.HasPreExistingMatch(repaired.Board))
                throw new InvalidOperationException("The pre-existing-match fixture is invalid.");

            BoardSimulation.EnsurePlayable(repaired);
            if (BoardSimulation.HasPreExistingMatch(repaired.Board) ||
                BoardSimulation.FindLegalSwaps(repaired.Board).Count == 0)
                throw new InvalidOperationException("Board startup recovery did not produce a stable, playable board.");
        }
    }
}
