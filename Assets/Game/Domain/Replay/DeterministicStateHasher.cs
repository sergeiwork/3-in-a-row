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
                .Append(state.Player.Health).Append('|').Append(state.Player.Shield).Append('|').Append(state.Player.Focus).Append('|')
                .Append(state.Player.Toxic).Append('|').Append(state.Player.PoisonStacks).Append('|')
                .Append(state.Enemy.DefinitionId).Append('|').Append(state.Enemy.Health).Append('|').Append(state.Enemy.IntentIndex).Append('|');

            foreach (var stream in state.RandomStreams)
                text.Append(stream.Stream).Append(':').Append(stream.State).Append('|');
            foreach (var skill in state.SelectedSkillIds)
                text.Append(skill).Append('|');
            foreach (var item in events.Events)
                text.Append(item.Sequence).Append(':').Append(item.Type).Append(':').Append(item.SourceId).Append(':')
                    .Append(item.Detail).Append(':').Append(item.Amount).Append(':').Append(item.Cell.HasValue ? item.Cell.Value.ToString() : "-").Append('|');

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
