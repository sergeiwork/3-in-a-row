using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Random;

namespace ThreeInARow.Domain.State
{
    /// <summary>
    /// Pure runtime state. It is deliberately independent of ScriptableObjects, scenes, and views so it can be saved and replayed.
    /// Fields are public for Unity's serializer and are only changed by domain/application commands.
    /// </summary>
    [Serializable]
    public sealed class RunState
    {
        public const int CurrentSchemaVersion = 4;

        public int SchemaVersion = CurrentSchemaVersion;
        public string ContentVersion = "0.4.0";
        public ulong Seed;
        public int EncounterIndex;
        public int ResolvedTurnCount;
        public int Experience;
        public int Level = 1;
        public PlayerState Player = new PlayerState();
        public EnemyState Enemy = new EnemyState();
        public BoardState Board = new BoardState();
        public List<ContentId> SelectedSkillIds = new List<ContentId>();
        public List<RandomStreamState> RandomStreams = new List<RandomStreamState>();
        public PendingChoiceState PendingChoice = new PendingChoiceState();
        public PendingCombatTurnState PendingCombatTurn = new PendingCombatTurnState();
    }

    [Serializable]
    public sealed class PlayerState
    {
        public const int MaxHealth = 40;

        public int Health = MaxHealth;
        public int Shield;
        public int Focus;
        public int Toxic;
        // Counts cleared Volt gems toward the next deterministic cooldown reduction.
        public int VoltClearProgress;
        // Stable left-to-right slot order. Learned active skills remain in SelectedSkillIds.
        public List<ContentId> EquippedActiveSkillIds = new List<ContentId>();
        public List<SkillCooldownState> SkillCooldowns = new List<SkillCooldownState>();
    }

    [Serializable]
    public sealed class EnemyState
    {
        public ContentId DefinitionId = "enemy.unset";
        public int Health;
        public int IntentIndex;
        public int PoisonStacks;
    }

    [Serializable]
    public sealed class BoardState
    {
        public const int Width = 7;
        public const int Height = 7;

        // Row-major order. A full board must contain Width * Height entries.
        public List<BoardGemState> Gems = new List<BoardGemState>();
    }

    [Serializable]
    public sealed class BoardGemState
    {
        public GridCell Cell;
        public ContentId GemId = "gem.unset";
        public ContentId SpecialId = "special.none";
        public List<ContentId> StatusIds = new List<ContentId>();
        // Kept separate from status IDs so existing content IDs remain stable in saves.
        // A duration of zero means the status has no automatic expiry.
        public List<BoardStatusDurationState> StatusDurations = new List<BoardStatusDurationState>();
    }

    [Serializable]
    public sealed class BoardStatusDurationState
    {
        public ContentId StatusId = "status.unset";
        public int RemainingPlayerTurns;
    }

    [Serializable]
    public sealed class SkillCooldownState
    {
        public ContentId SkillId = "skill.unset";
        public int RemainingTurns;
    }

    [Serializable]
    public sealed class PendingChoiceState
    {
        public ContentId ChoiceId = "choice.none";
        public int Level;
        public List<ContentId> OptionIds = new List<ContentId>();

        public bool IsPending => OptionIds != null && OptionIds.Count > 0;
    }

    [Serializable]
    public sealed class PendingCombatTurnState
    {
        // This is authoritative command timing state, not an animation checkpoint.
        public bool AwaitingEnemyResponse;
        public int CascadeCount;
        public List<ContentId> SkillIdsUsed = new List<ContentId>();
    }
}
