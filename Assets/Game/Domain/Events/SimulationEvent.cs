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
        BoardReshuffled,
        StatusRemoved,
        ResourceChanged,
        CooldownChanged,
        EnemyIntentTelegraphed,
        SkillUsed,
        SkillEquipped
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
        // A clear carries the board statuses that existed immediately before removal.
        // This lets combat resolve Cracked without reading a post-resolution board snapshot.
        public List<ContentId> StatusIds = new List<ContentId>();
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
            ContentId? relatedId = null,
            IEnumerable<ContentId> statusIds = null)
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
                RelatedId = relatedId ?? (ContentId)"content.none",
                StatusIds = statusIds == null ? new List<ContentId>() : new List<ContentId>(statusIds)
            });
        }

        public void Append(EventBatch source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var item in source.Events)
            {
                Add(item.Type, item.SourceId, item.Detail, item.Amount,
                    item.HasCell ? item.Cell : (GridCell?)null,
                    item.HasTargetCell ? item.TargetCell : (GridCell?)null,
                    item.RelatedId,
                    item.StatusIds);
            }
        }

        public void Add(SimulationEvent item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            Add(item.Type, item.SourceId, item.Detail, item.Amount,
                item.HasCell ? item.Cell : (GridCell?)null,
                item.HasTargetCell ? item.TargetCell : (GridCell?)null,
                item.RelatedId,
                item.StatusIds);
        }
    }
}
