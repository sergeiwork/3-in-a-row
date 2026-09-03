using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ThreeInARow.Application;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;
using UnityEngine;

namespace ThreeInARow.Infrastructure
{
    /// <summary>Versioned, human-readable local checkpoint with an explicit DTO boundary.</summary>
    public sealed class JsonCheckpointStore : ICheckpointStore
    {
        public const int EnvelopeSchemaVersion = 1;
        private const string FileName = "run-checkpoint.json";

        private readonly string _path;

        public JsonCheckpointStore(string directory = null)
        {
            _path = Path.Combine(directory ?? UnityEngine.Application.persistentDataPath, FileName);
        }

        public bool HasCheckpoint => File.Exists(_path);
        public string PathForDiagnostics => _path;

        public void Save(CheckpointSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.State.PendingCombatTurn != null && snapshot.State.PendingCombatTurn.AwaitingEnemyResponse)
                throw new InvalidOperationException("A checkpoint cannot be written during the post-cascade command window.");

            var envelope = new CheckpointEnvelope
            {
                schemaVersion = EnvelopeSchemaVersion,
                savedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                run = RunDto.FromDomain(snapshot.State),
                statistics = snapshot.Statistics
            };
            var json = JsonUtility.ToJson(envelope, true);
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporaryPath, _path);
        }

        public bool TryLoad(out CheckpointSnapshot snapshot)
        {
            snapshot = null;
            if (!File.Exists(_path)) return false;
            try
            {
                var envelope = JsonUtility.FromJson<CheckpointEnvelope>(File.ReadAllText(_path));
                if (envelope == null || envelope.schemaVersion != EnvelopeSchemaVersion || envelope.run == null)
                    return false;
                var state = envelope.run.ToDomain();
                if (state.SchemaVersion != RunState.CurrentSchemaVersion ||
                    state.Board == null || state.Board.Gems == null ||
                    state.Board.Gems.Count != BoardState.Width * BoardState.Height ||
                    (state.PendingCombatTurn != null && state.PendingCombatTurn.AwaitingEnemyResponse))
                    return false;
                snapshot = new CheckpointSnapshot(state, envelope.statistics ?? new RunStatistics());
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Checkpoint could not be loaded: " + exception.Message);
                return false;
            }
        }

        public void Clear()
        {
            if (File.Exists(_path)) File.Delete(_path);
            var temporaryPath = _path + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }

        [Serializable]
        private sealed class CheckpointEnvelope
        {
            public int schemaVersion;
            public string savedUtc;
            public RunDto run;
            public RunStatistics statistics;
        }

        [Serializable]
        private sealed class RunDto
        {
            public int schemaVersion;
            public string contentVersion;
            public string seed;
            public int encounterIndex;
            public int resolvedTurnCount;
            public int experience;
            public int level;
            public PlayerDto player;
            public EnemyDto enemy;
            public List<GemDto> gems = new List<GemDto>();
            public List<string> selectedSkills = new List<string>();
            public List<RandomDto> randomStreams = new List<RandomDto>();
            public ChoiceDto pendingChoice;
            public CombatWindowDto pendingCombatTurn;

            public static RunDto FromDomain(RunState state)
            {
                var dto = new RunDto
                {
                    schemaVersion = state.SchemaVersion,
                    contentVersion = state.ContentVersion,
                    seed = state.Seed.ToString(CultureInfo.InvariantCulture),
                    encounterIndex = state.EncounterIndex,
                    resolvedTurnCount = state.ResolvedTurnCount,
                    experience = state.Experience,
                    level = state.Level,
                    player = PlayerDto.FromDomain(state.Player),
                    enemy = EnemyDto.FromDomain(state.Enemy),
                    pendingChoice = ChoiceDto.FromDomain(state.PendingChoice),
                    pendingCombatTurn = CombatWindowDto.FromDomain(state.PendingCombatTurn)
                };
                if (state.Board != null && state.Board.Gems != null)
                    foreach (var gem in state.Board.Gems) dto.gems.Add(GemDto.FromDomain(gem));
                AddIds(dto.selectedSkills, state.SelectedSkillIds);
                if (state.RandomStreams != null)
                {
                    foreach (var stream in state.RandomStreams)
                    {
                        dto.randomStreams.Add(new RandomDto
                        {
                            stream = (int)stream.Stream,
                            state = stream.State.ToString(CultureInfo.InvariantCulture)
                        });
                    }
                }
                return dto;
            }

            public RunState ToDomain()
            {
                ulong parsedSeed;
                if (!ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out parsedSeed))
                    throw new FormatException("Invalid run seed.");
                var state = new RunState
                {
                    SchemaVersion = schemaVersion,
                    ContentVersion = contentVersion ?? string.Empty,
                    Seed = parsedSeed,
                    EncounterIndex = encounterIndex,
                    ResolvedTurnCount = resolvedTurnCount,
                    Experience = experience,
                    Level = level,
                    Player = player == null ? new PlayerState() : player.ToDomain(),
                    Enemy = enemy == null ? new EnemyState() : enemy.ToDomain(),
                    Board = new BoardState(),
                    SelectedSkillIds = ToIds(selectedSkills),
                    RandomStreams = new List<RandomStreamState>(),
                    PendingChoice = pendingChoice == null ? new PendingChoiceState() : pendingChoice.ToDomain(),
                    PendingCombatTurn = pendingCombatTurn == null
                        ? new PendingCombatTurnState()
                        : pendingCombatTurn.ToDomain()
                };
                if (gems != null)
                    foreach (var gem in gems) state.Board.Gems.Add(gem.ToDomain());
                if (randomStreams != null)
                {
                    foreach (var random in randomStreams)
                    {
                        ulong parsedState;
                        if (!ulong.TryParse(random.state, NumberStyles.None, CultureInfo.InvariantCulture, out parsedState))
                            throw new FormatException("Invalid random stream state.");
                        state.RandomStreams.Add(new RandomStreamState
                        {
                            Stream = (RandomStream)random.stream,
                            State = parsedState
                        });
                    }
                }
                return state;
            }
        }

        [Serializable]
        private sealed class PlayerDto
        {
            public int health;
            public int shield;
            public int focus;
            public int toxic;
            public int voltClearProgress;
            public List<string> equippedActives = new List<string>();
            public List<CooldownDto> cooldowns = new List<CooldownDto>();

            public static PlayerDto FromDomain(PlayerState player)
            {
                player = player ?? new PlayerState();
                var dto = new PlayerDto
                {
                    health = player.Health,
                    shield = player.Shield,
                    focus = player.Focus,
                    toxic = player.Toxic,
                    voltClearProgress = player.VoltClearProgress
                };
                AddIds(dto.equippedActives, player.EquippedActiveSkillIds);
                if (player.SkillCooldowns != null)
                    foreach (var cooldown in player.SkillCooldowns)
                        dto.cooldowns.Add(new CooldownDto
                        {
                            skillId = Id(cooldown.SkillId),
                            remainingTurns = cooldown.RemainingTurns
                        });
                return dto;
            }

            public PlayerState ToDomain()
            {
                var result = new PlayerState
                {
                    Health = health,
                    Shield = shield,
                    Focus = focus,
                    Toxic = toxic,
                    VoltClearProgress = voltClearProgress,
                    EquippedActiveSkillIds = ToIds(equippedActives),
                    SkillCooldowns = new List<SkillCooldownState>()
                };
                if (cooldowns != null)
                    foreach (var cooldown in cooldowns)
                        result.SkillCooldowns.Add(new SkillCooldownState
                        {
                            SkillId = Content(cooldown.skillId),
                            RemainingTurns = cooldown.remainingTurns
                        });
                return result;
            }
        }

        [Serializable]
        private sealed class EnemyDto
        {
            public string definitionId;
            public int health;
            public int intentIndex;
            public int poisonStacks;

            public static EnemyDto FromDomain(EnemyState enemy)
            {
                enemy = enemy ?? new EnemyState();
                return new EnemyDto
                {
                    definitionId = Id(enemy.DefinitionId),
                    health = enemy.Health,
                    intentIndex = enemy.IntentIndex,
                    poisonStacks = enemy.PoisonStacks
                };
            }

            public EnemyState ToDomain()
            {
                return new EnemyState
                {
                    DefinitionId = Content(definitionId),
                    Health = health,
                    IntentIndex = intentIndex,
                    PoisonStacks = poisonStacks
                };
            }
        }

        [Serializable]
        private sealed class GemDto
        {
            public int column;
            public int row;
            public string gemId;
            public string specialId;
            public List<string> statusIds = new List<string>();
            public List<StatusDurationDto> durations = new List<StatusDurationDto>();

            public static GemDto FromDomain(BoardGemState gem)
            {
                if (gem == null) throw new InvalidOperationException("A stable board cannot contain an empty cell.");
                var dto = new GemDto
                {
                    column = gem.Cell.Column,
                    row = gem.Cell.Row,
                    gemId = Id(gem.GemId),
                    specialId = Id(gem.SpecialId)
                };
                AddIds(dto.statusIds, gem.StatusIds);
                if (gem.StatusDurations != null)
                    foreach (var duration in gem.StatusDurations)
                        dto.durations.Add(new StatusDurationDto
                        {
                            statusId = Id(duration.StatusId),
                            remainingPlayerTurns = duration.RemainingPlayerTurns
                        });
                return dto;
            }

            public BoardGemState ToDomain()
            {
                var result = new BoardGemState
                {
                    Cell = new GridCell(column, row),
                    GemId = Content(gemId),
                    SpecialId = Content(specialId),
                    StatusIds = ToIds(statusIds),
                    StatusDurations = new List<BoardStatusDurationState>()
                };
                if (durations != null)
                    foreach (var duration in durations)
                        result.StatusDurations.Add(new BoardStatusDurationState
                        {
                            StatusId = Content(duration.statusId),
                            RemainingPlayerTurns = duration.remainingPlayerTurns
                        });
                return result;
            }
        }

        [Serializable] private sealed class CooldownDto { public string skillId; public int remainingTurns; }
        [Serializable] private sealed class StatusDurationDto { public string statusId; public int remainingPlayerTurns; }
        [Serializable] private sealed class RandomDto { public int stream; public string state; }

        [Serializable]
        private sealed class ChoiceDto
        {
            public string choiceId;
            public int level;
            public List<string> optionIds = new List<string>();

            public static ChoiceDto FromDomain(PendingChoiceState choice)
            {
                choice = choice ?? new PendingChoiceState();
                var dto = new ChoiceDto { choiceId = Id(choice.ChoiceId), level = choice.Level };
                AddIds(dto.optionIds, choice.OptionIds);
                return dto;
            }

            public PendingChoiceState ToDomain()
            {
                return new PendingChoiceState
                {
                    ChoiceId = Content(choiceId),
                    Level = level,
                    OptionIds = ToIds(optionIds)
                };
            }
        }

        [Serializable]
        private sealed class CombatWindowDto
        {
            public bool awaitingEnemyResponse;
            public int cascadeCount;
            public List<string> skillIdsUsed = new List<string>();

            public static CombatWindowDto FromDomain(PendingCombatTurnState window)
            {
                window = window ?? new PendingCombatTurnState();
                var dto = new CombatWindowDto
                {
                    awaitingEnemyResponse = window.AwaitingEnemyResponse,
                    cascadeCount = window.CascadeCount
                };
                AddIds(dto.skillIdsUsed, window.SkillIdsUsed);
                return dto;
            }

            public PendingCombatTurnState ToDomain()
            {
                return new PendingCombatTurnState
                {
                    AwaitingEnemyResponse = awaitingEnemyResponse,
                    CascadeCount = cascadeCount,
                    SkillIdsUsed = ToIds(skillIdsUsed)
                };
            }
        }

        private static string Id(ContentId id)
        {
            return string.IsNullOrEmpty(id.Value) ? "content.none" : id.Value;
        }

        private static ContentId Content(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (ContentId)"content.none" : (ContentId)value;
        }

        private static void AddIds(List<string> destination, IEnumerable<ContentId> source)
        {
            if (source == null) return;
            foreach (var id in source) destination.Add(Id(id));
        }

        private static List<ContentId> ToIds(IEnumerable<string> source)
        {
            var result = new List<ContentId>();
            if (source == null) return result;
            foreach (var id in source) result.Add(Content(id));
            return result;
        }
    }
}
