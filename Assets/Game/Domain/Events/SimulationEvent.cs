using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Events
{
    public enum SimulationEventType
    {
        SwapAccepted,
        GemsMatched,
        GemCleared,
        SpecialCreated,
        DamageApplied,
        StatusAdded,
        EnemyIntentStarted,
        EnemyDefeated,
        XPGranted,
        LevelUpOffered,
        SkillChosen,
        RunEnded
    }

    [Serializable]
    public sealed class SimulationEvent
    {
        public int Sequence;
        public SimulationEventType Type;
        public ContentId SourceId = "system.foundation";
        public string Detail = string.Empty;
        public int Amount;
        public GridCell? Cell;
    }

    [Serializable]
    public sealed class EventBatch
    {
        private readonly List<SimulationEvent> _events = new List<SimulationEvent>();

        public IReadOnlyList<SimulationEvent> Events => _events;

        public void Add(SimulationEventType type, ContentId sourceId, string detail, int amount = 0, GridCell? cell = null)
        {
            _events.Add(new SimulationEvent
            {
                Sequence = _events.Count,
                Type = type,
                SourceId = sourceId,
                Detail = detail ?? string.Empty,
                Amount = amount,
                Cell = cell
            });
        }
    }
}
