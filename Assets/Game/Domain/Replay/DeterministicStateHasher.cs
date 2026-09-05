using System;
using System.Security.Cryptography;
using System.Text;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Replay
{
    /// <summary>Produces a canonical hash for resolved state and event logs; field ordering is intentional.</summary>
    public static class DeterministicStateHasher
    {
        public static string Hash(RunState state, EventBatch events)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (events == null) throw new ArgumentNullException(nameof(events));

            var text = new StringBuilder();
            text.Append(state.SchemaVersion).Append('|').Append(state.ContentVersion).Append('|')
                .Append(state.Seed).Append('|').Append(state.EncounterIndex).Append('|').Append(state.ResolvedTurnCount).Append('|')
                .Append(state.Experience).Append('|').Append(state.Level).Append('|')
                .Append(state.Player.Health).Append('|').Append(state.Player.Shield).Append('|').Append(state.Player.Focus).Append('|')
                .Append(state.Player.Toxic).Append('|')
                .Append(state.Player.VoltClearProgress).Append('|')
                .Append(state.Enemy.DefinitionId).Append('|').Append(state.Enemy.Health).Append('|')
                .Append(state.Enemy.IntentIndex).Append('|').Append(state.Enemy.PoisonStacks).Append('|');

            text.Append("encounter:").Append(state.CurrentEncounterId).Append('|')
                .Append("eliteQueued:").Append(state.PendingEliteReward).Append('|');
            if (state.SelectedEncounterIds != null)
                foreach (var encounterId in state.SelectedEncounterIds)
                    text.Append("planned:").Append(encounterId).Append('|');
            if (state.Map != null)
            {
                text.Append("map:").Append(state.Map.CurrentNodeId).Append(':')
                    .Append(state.Map.BossEnemyId).Append(':').Append(state.Map.FurthestVisitedRow).Append('|');
                if (state.Map.Nodes != null)
                    foreach (var node in state.Map.Nodes)
                    {
                        text.Append("node:").Append(node.Id).Append(':').Append(node.Row).Append(':')
                            .Append(node.Column).Append(':').Append(node.Type).Append(':').Append(node.ContentId)
                            .Append(':').Append(node.PressureId).Append(':').Append(node.Visited).Append(':')
                            .Append(node.Completed).Append('[');
                        if (node.ConnectionIds != null)
                            foreach (var connection in node.ConnectionIds) text.Append(connection).Append(',');
                        text.Append("]|");
                    }
            }
            if (state.PendingEvent != null)
            {
                text.Append("event:").Append(state.PendingEvent.EventId).Append('[');
                if (state.PendingEvent.ChoiceIds != null)
                    foreach (var choice in state.PendingEvent.ChoiceIds) text.Append(choice).Append(',');
                text.Append("]|");
            }
            if (state.PendingEncounterModifiers != null)
                foreach (var modifier in state.PendingEncounterModifiers)
                    if (modifier != null) text.Append("pending:").Append(modifier.Id).Append(':').Append(modifier.Amount).Append('|');

            foreach (var stream in state.RandomStreams)
                text.Append(stream.Stream).Append(':').Append(stream.State).Append('|');
            foreach (var skill in state.SelectedSkillIds)
                text.Append(skill).Append('|');
            if (state.Player.EquippedActiveSkillIds != null)
                foreach (var skill in state.Player.EquippedActiveSkillIds)
                    text.Append("equipped:").Append(skill).Append('|');
            if (state.Player.SkillCooldowns != null)
                foreach (var cooldown in state.Player.SkillCooldowns)
                    if (cooldown != null)
                        text.Append("cooldown:").Append(cooldown.SkillId).Append(':')
                            .Append(cooldown.RemainingTurns).Append('|');
            if (state.PendingChoice != null)
            {
                text.Append("choice:").Append(state.PendingChoice.ChoiceId).Append(':')
                    .Append(state.PendingChoice.Level).Append('[');
                if (state.PendingChoice.OptionIds != null)
                    foreach (var option in state.PendingChoice.OptionIds) text.Append(option).Append(',');
                text.Append("]|");
            }
            if (state.PendingCombatTurn != null)
            {
                text.Append("turn:").Append(state.PendingCombatTurn.AwaitingEnemyResponse).Append(':')
                    .Append(state.PendingCombatTurn.CascadeCount).Append('[');
                if (state.PendingCombatTurn.SkillIdsUsed != null)
                    foreach (var skill in state.PendingCombatTurn.SkillIdsUsed) text.Append(skill).Append(',');
                text.Append("]|");
            }
            foreach (var gem in state.Board.Gems)
            {
                text.Append(gem.Cell).Append(':').Append(gem.GemId).Append(':').Append(gem.SpecialId).Append('[');
                if (gem.StatusIds != null)
                    foreach (var statusId in gem.StatusIds) text.Append(statusId).Append(',');
                text.Append("]{");
                if (gem.StatusDurations != null)
                    foreach (var duration in gem.StatusDurations)
                        if (duration != null)
                            text.Append(duration.StatusId).Append(':').Append(duration.RemainingPlayerTurns).Append(',');
                text.Append("}|");
            }
            foreach (var item in events.Events)
            {
                text.Append(item.Sequence).Append(':').Append(item.Type).Append(':').Append(item.SourceId).Append(':')
                    .Append(item.RelatedId).Append(':').Append(item.Detail).Append(':').Append(item.Amount).Append(':')
                    .Append(item.HasCell ? item.Cell.ToString() : "-").Append(':')
                    .Append(item.HasTargetCell ? item.TargetCell.ToString() : "-").Append('[');
                if (item.StatusIds != null)
                    foreach (var statusId in item.StatusIds) text.Append(statusId).Append(',');
                text.Append("]|");
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var output = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) output.Append(value.ToString("x2"));
                return output.ToString();
            }
        }
    }
}
