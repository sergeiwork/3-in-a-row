using System;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Harness;
using ThreeInARow.Domain.Replay;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Tests
{
    /// <summary>Zero-dependency Session B acceptance harness. It is callable by later EditMode tests.</summary>
    public static class BoardScenarioHarness
    {
        public static string AssertRepeatableAndPlayable()
        {
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
    }
}
