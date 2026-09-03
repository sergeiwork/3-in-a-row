using System;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Harness;

namespace ThreeInARow.Tests
{
    /// <summary>Zero-dependency Session C acceptance harness. It is not automatically executed.</summary>
    public static class CombatScenarioHarness
    {
        public static string AssertRepeatableAndOrdered()
        {
            var first = CombatScenario.Run();
            var second = CombatScenario.Run();
            if (!string.Equals(first.StateHash, second.StateHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Combat scenario is not repeatable.");
            if (first.State.ResolvedTurnCount != 1)
                throw new InvalidOperationException("An accepted combat swap did not advance exactly one turn.");

            var sawIntent = false;
            for (var index = 0; index < first.Events.Events.Count; index++)
            {
                var item = first.Events.Events[index];
                if (item.Sequence != index)
                    throw new InvalidOperationException("Combat events are not a contiguous ordered batch.");
                if (item.Type == SimulationEventType.EnemyIntentStarted) sawIntent = true;
            }
            if (!first.EncounterWon && !sawIntent)
                throw new InvalidOperationException("A surviving enemy did not take exactly one response opportunity.");
            return first.StateHash;
        }
    }
}
