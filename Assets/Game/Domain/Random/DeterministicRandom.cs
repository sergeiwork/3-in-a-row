using System;
using System.Collections.Generic;

namespace ThreeInARow.Domain.Random
{
    public enum RandomStream
    {
        BoardSpawn,
        RewardSampling,
        IntentVariation
    }

    [Serializable]
    public sealed class RandomStreamState
    {
        public RandomStream Stream;
        public ulong State;
    }

    /// <summary>Stable SplitMix64 RNG. Its algorithm and stream names are part of the replay/save contract.</summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed;
        }

        public ulong State => _state;

        public ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            var value = _state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            return (int)(NextUInt64() % (ulong)exclusiveMax);
        }
    }

    public static class RandomStreams
    {
        public static List<RandomStreamState> Create(ulong runSeed)
        {
            var root = new DeterministicRandom(runSeed);
            var result = new List<RandomStreamState>();
            foreach (RandomStream stream in Enum.GetValues(typeof(RandomStream)))
            {
                result.Add(new RandomStreamState { Stream = stream, State = root.NextUInt64() });
            }

            return result;
        }

        public static DeterministicRandom Restore(RandomStream stream, List<RandomStreamState> streams)
        {
            foreach (var streamState in streams)
            {
                if (streamState.Stream == stream) return new DeterministicRandom(streamState.State);
            }

            throw new InvalidOperationException("Missing required random stream: " + stream);
        }

        public static void Store(RandomStream stream, DeterministicRandom random, List<RandomStreamState> streams)
        {
            foreach (var streamState in streams)
            {
                if (streamState.Stream != stream) continue;
                streamState.State = random.State;
                return;
            }

            throw new InvalidOperationException("Missing required random stream: " + stream);
        }
    }
}
