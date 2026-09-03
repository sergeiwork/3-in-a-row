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
    }

    [Serializable]
    public sealed class SelectRewardCommand : ISimulationCommand
    {
        public ContentId RewardId;
    }

    [Serializable]
    public sealed class ContinueCommand : ISimulationCommand { }
}
