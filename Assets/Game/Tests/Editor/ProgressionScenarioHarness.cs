using System;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Harness;
using ThreeInARow.Domain.Progression;

namespace ThreeInARow.Tests
{
    /// <summary>Zero-dependency Session D acceptance harness. It is not automatically executed.</summary>
    public static class ProgressionScenarioHarness
    {
        public static string AssertRepeatableAndPersistent()
        {
            var first = ProgressionScenario.Run();
            var second = ProgressionScenario.Run();
            if (!string.Equals(first.StateHash, second.StateHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Progression scenario is not repeatable.");
            if (first.State.Level != 2 || first.State.Experience != 2)
                throw new InvalidOperationException("XP and level did not persist.");
            if (!ProgressionRules.HasSkill(first.State, first.SelectedReward))
                throw new InvalidOperationException("The selected reward did not persist into combat.");
            if (first.State.PendingChoice.IsPending || first.State.PendingCombatTurn.AwaitingEnemyResponse)
                throw new InvalidOperationException("The scenario ended at an incomplete command boundary.");

            var sawSkillUse = false;
            foreach (var item in first.Events.Events)
                if (item.Type == SimulationEventType.SkillUsed &&
                    item.SourceId.Equals(ProgressionContentIds.Sunder)) sawSkillUse = true;
            if (!sawSkillUse)
                throw new InvalidOperationException("Pre-swap active use did not emit Sunder use.");
            var cooldown = ProgressionRules.FindCooldown(first.State.Player, ProgressionContentIds.Sunder);
            if (cooldown == null || cooldown.RemainingTurns != 4)
                throw new InvalidOperationException("A pre-swap active skill cooled down on its activation turn.");
            return first.StateHash;
        }
    }
}
