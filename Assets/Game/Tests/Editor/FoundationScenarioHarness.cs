using System;
using ThreeInARow.Domain.Harness;

namespace ThreeInARow.Tests
{
    /// <summary>
    /// A zero-dependency harness callable by an EditMode test later. It avoids defining tests in Session A,
    /// in keeping with the requested no-test execution.
    /// </summary>
    public static class FoundationScenarioHarness
    {
        public static string AssertRepeatable()
        {
            var first = FoundationScenario.Run();
            var second = FoundationScenario.Run();
            if (!string.Equals(first.StateHash, second.StateHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Foundation scenario is not repeatable.");
            if (first.Events.Events.Count != 4)
                throw new InvalidOperationException("Foundation scenario event log changed unexpectedly.");
            return first.StateHash;
        }
    }
}
