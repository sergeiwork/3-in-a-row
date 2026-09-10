using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Commands
{
    public interface ISimulationCommand { }

    [Serializable]
    public sealed class SwapCommand : ISimulationCommand
    {
        public GridCell CellA;
        public GridCell CellB;
    }

    [Serializable]
    public sealed class UseSkillCommand : ISimulationCommand
    {
        public ContentId SkillId;
        public List<GridCell> Targets = new List<GridCell>();
        // Optional stable content choice used by active effects such as Transmute.
        public ContentId OptionId = "content.none";
    }

    [Serializable]
    public sealed class SelectRewardCommand : ISimulationCommand
    {
        public ContentId RewardId;
    }

    [Serializable]
    public sealed class EquipSkillCommand : ISimulationCommand
    {
        public ContentId SkillId;
        public int SlotIndex;
    }

    [Serializable]
    public sealed class ContinueCommand : ISimulationCommand { }

    [Serializable]
    public sealed class SelectMapNodeCommand : ISimulationCommand
    {
        public ContentId NodeId;
    }

    [Serializable]
    public sealed class SelectEventChoiceCommand : ISimulationCommand
    {
        public ContentId ChoiceId;
    }

    [Serializable]
    public sealed class PinRouteVowCommand : ISimulationCommand
    {
        public ContentId VowId;
    }
}
