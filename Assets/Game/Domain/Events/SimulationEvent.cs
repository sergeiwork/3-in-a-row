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
        RunEnded,
        BoardInitialized,
        SpecialActivated,
        GemMoved,
        GemSpawned,
        BoardReshuffled
    }

    [Serializable]
    public sealed class SimulationEvent
    {
        public int Sequence;
        public SimulationEventType Type;
        public ContentId SourceId = "system.foundation";
        public string Detail = string.Empty;
        public int Amount;
        public bool HasCell;
        public GridCell Cell;
        public bool HasTargetCell;
        public GridCell TargetCell;
        public ContentId RelatedId = "content.none";
    }

    [Serializable]
    public sealed class EventBatch
    {
        private readonly List<SimulationEvent> _events = new List<SimulationEvent>();

        public IReadOnlyList<SimulationEvent> Events => _events;

        public void Add(
            SimulationEventType type,
            ContentId sourceId,
            string detail,
            int amount = 0,
            GridCell? cell = null,
            GridCell? targetCell = null,
            ContentId? relatedId = null)
        {
            _events.Add(new SimulationEvent
            {
                Sequence = _events.Count,
                Type = type,
                SourceId = sourceId,
                Detail = detail ?? string.Empty,
                Amount = amount,
                HasCell = cell.HasValue,
                Cell = cell.GetValueOrDefault(),
                HasTargetCell = targetCell.HasValue,
                TargetCell = targetCell.GetValueOrDefault(),
                RelatedId = relatedId ?? (ContentId)"content.none"
            });
        }
    }
}
