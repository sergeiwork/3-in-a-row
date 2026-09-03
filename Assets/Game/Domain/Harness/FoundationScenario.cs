using System;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.Replay;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Harness
{
    /// <summary>Small deterministic replay fixture for Session A; it intentionally does not implement board or combat rules.</summary>
    public static class FoundationScenario
    {
        public const ulong DefaultSeed = 0xC0FFEE42UL;

        public static FoundationScenarioResult Run(ulong seed = DefaultSeed)
        {
            var state = new RunState
            {
                Seed = seed,
                EncounterIndex = 0,
                Enemy = new EnemyState { DefinitionId = "enemy.geode_mite", Health = 52, IntentIndex = 0 },
                RandomStreams = RandomStreams.Create(seed)
            };
            state.SelectedSkillIds.Add("skill.sunder");
            state.SelectedSkillIds.Add("skill.cleanse");

            var events = new EventBatch();
            var boardRandom = RandomStreams.Restore(RandomStream.BoardSpawn, state.RandomStreams);
            var scriptedRoll = boardRandom.NextInt(49);
            RandomStreams.Store(RandomStream.BoardSpawn, boardRandom, state.RandomStreams);

            events.Add(SimulationEventType.SwapAccepted, "system.foundation", "scripted swap accepted", cell: new GridCell(3, 3));
            events.Add(SimulationEventType.GemsMatched, "gem.ember", "scripted three-match", 3);
            events.Add(SimulationEventType.GemCleared, "gem.ember", "scripted clear", 1, new GridCell(scriptedRoll % 7, scriptedRoll / 7));
            state.Enemy.Health -= 4;
            state.ResolvedTurnCount = 1;
            events.Add(SimulationEventType.DamageApplied, "gem.ember", "enemy direct damage", 4);

            return new FoundationScenarioResult(state, events, DeterministicStateHasher.Hash(state, events));
        }
    }

    public sealed class FoundationScenarioResult
    {
        public readonly RunState State;
        public readonly EventBatch Events;
        public readonly string StateHash;

        public FoundationScenarioResult(RunState state, EventBatch events, string stateHash)
        {
            State = state;
            Events = events;
            StateHash = stateHash;
        }
    }
}
