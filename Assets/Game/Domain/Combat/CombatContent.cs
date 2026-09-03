using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Ids;

namespace ThreeInARow.Domain.Combat
{
    public static class CombatContentIds
    {
        public static readonly ContentId SystemCombat = "system.combat";
        public static readonly ContentId Poison = "status.poison";

        public static readonly ContentId GeodeMite = "enemy.geode_mite";
        public static readonly ContentId FrostOracle = "enemy.frost_oracle";
        public static readonly ContentId GeodeMiteElite = "enemy.geode_mite_elite";
        public static readonly ContentId PrismStalker = "enemy.prism_stalker";
        public static readonly ContentId CrystalWarden = "enemy.crystal_warden";

        public static readonly ContentId Encounter1 = "encounter.01_geode_mite";
        public static readonly ContentId Encounter2 = "encounter.02_frost_oracle";
        public static readonly ContentId Encounter3 = "encounter.03_geode_mite_elite";
        public static readonly ContentId Encounter4 = "encounter.04_prism_stalker";
        public static readonly ContentId Encounter5 = "encounter.05_crystal_warden";
    }

    public enum IntentEffectType
    {
        DamagePlayer,
        ApplyBoardStatus,
        DrainResources
    }

    public sealed class IntentEffectDefinition
    {
        public readonly IntentEffectType Type;
        public readonly int Amount;
        public readonly ContentId StatusId;
        public readonly int FocusAmount;
        public readonly int ToxicAmount;
        public readonly int DurationPlayerTurns;

        private IntentEffectDefinition(
            IntentEffectType type,
            int amount,
            ContentId statusId,
            int focusAmount,
            int toxicAmount,
            int durationPlayerTurns)
        {
            Type = type;
            Amount = amount;
            StatusId = statusId;
            FocusAmount = focusAmount;
            ToxicAmount = toxicAmount;
            DurationPlayerTurns = durationPlayerTurns;
        }

        public static IntentEffectDefinition Damage(int amount)
        {
            return new IntentEffectDefinition(IntentEffectType.DamagePlayer, amount, "status.none", 0, 0, 0);
        }

        public static IntentEffectDefinition ApplyStatus(ContentId statusId, int count, int durationPlayerTurns = 0)
        {
            return new IntentEffectDefinition(IntentEffectType.ApplyBoardStatus, count, statusId, 0, 0, durationPlayerTurns);
        }

        public static IntentEffectDefinition Drain(int focus, int toxic)
        {
            return new IntentEffectDefinition(IntentEffectType.DrainResources, 0, "status.none", focus, toxic, 0);
        }
    }

    public sealed class IntentDefinition
    {
        public readonly ContentId Id;
        public readonly string TelegraphKey;
        public readonly IReadOnlyList<IntentEffectDefinition> Effects;

        public IntentDefinition(ContentId id, string telegraphKey, params IntentEffectDefinition[] effects)
        {
            Id = id;
            TelegraphKey = telegraphKey ?? string.Empty;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }
    }

    public sealed class EnemyDefinition
    {
        public readonly ContentId Id;
        public readonly string DisplayKey;
        public readonly int MaxHealth;
        public readonly int RewardXp;
        public readonly IReadOnlyList<IntentDefinition> IntentCycle;

        public EnemyDefinition(
            ContentId id,
            string displayKey,
            int maxHealth,
            int rewardXp,
            params IntentDefinition[] intentCycle)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (intentCycle == null || intentCycle.Length == 0)
                throw new ArgumentException("An enemy needs at least one intent.", nameof(intentCycle));
            Id = id;
            DisplayKey = displayKey ?? string.Empty;
            MaxHealth = maxHealth;
            RewardXp = rewardXp;
            IntentCycle = intentCycle;
        }
    }

    public sealed class EncounterDefinition
    {
        public readonly ContentId Id;
        public readonly EnemyDefinition Enemy;

        public EncounterDefinition(ContentId id, EnemyDefinition enemy)
        {
            Id = id;
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
        }
    }

    public interface ICombatContentCatalog
    {
        IReadOnlyList<EncounterDefinition> Encounters { get; }
        EncounterDefinition GetEncounter(int zeroBasedIndex);
        EnemyDefinition GetEnemy(ContentId enemyId);
    }

    /// <summary>Immutable MVP combat content. Enemy execution contains no per-enemy branching.</summary>
    public sealed class MvpCombatContentCatalog : ICombatContentCatalog
    {
        private readonly List<EncounterDefinition> _encounters;
        private readonly Dictionary<ContentId, EnemyDefinition> _enemies;

        public static readonly MvpCombatContentCatalog Instance = new MvpCombatContentCatalog();

        private MvpCombatContentCatalog()
        {
            var mite = new EnemyDefinition(
                CombatContentIds.GeodeMite, "enemy.geode_mite.name", 52, 1,
                Intent("intent.geode_mite.chip_5", "intent.chip", IntentEffectDefinition.Damage(5)),
                Intent("intent.geode_mite.crack_3", "intent.crack", IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 3)),
                Intent("intent.geode_mite.chip_6", "intent.chip", IntentEffectDefinition.Damage(6)));

            var oracle = new EnemyDefinition(
                CombatContentIds.FrostOracle, "enemy.frost_oracle.name", 66, 1,
                Intent("intent.frost_oracle.freeze_2", "intent.chill", IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2)),
                Intent("intent.frost_oracle.needle_7", "intent.needle", IntentEffectDefinition.Damage(7)),
                Intent("intent.frost_oracle.freeze_3", "intent.chill", IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 3)));

            var elite = new EnemyDefinition(
                CombatContentIds.GeodeMiteElite, "enemy.geode_mite_elite.name", 84, 1,
                Intent("intent.geode_mite_elite.crush", "intent.crush",
                    IntentEffectDefinition.Damage(8), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                Intent("intent.geode_mite_elite.chip_7", "intent.chip", IntentEffectDefinition.Damage(7)),
                Intent("intent.geode_mite_elite.crack_4", "intent.crack", IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 4)));

            var stalker = new EnemyDefinition(
                CombatContentIds.PrismStalker, "enemy.prism_stalker.name", 92, 1,
                Intent("intent.prism_stalker.bolt_8", "intent.bolt", IntentEffectDefinition.Damage(8)),
                Intent("intent.prism_stalker.drain", "intent.drain", IntentEffectDefinition.Drain(3, 3)),
                Intent("intent.prism_stalker.bolt_10", "intent.bolt", IntentEffectDefinition.Damage(10)));

            var warden = new EnemyDefinition(
                CombatContentIds.CrystalWarden, "enemy.crystal_warden.name", 128, 1,
                Intent("intent.crystal_warden.seal", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.crystal_warden.shardstorm_10", "intent.shardstorm", IntentEffectDefinition.Damage(10)),
                Intent("intent.crystal_warden.freeze_anchor", "intent.freeze_anchor",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2),
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.crystal_warden.shardstorm_12", "intent.shardstorm", IntentEffectDefinition.Damage(12)));

            _encounters = new List<EncounterDefinition>
            {
                new EncounterDefinition(CombatContentIds.Encounter1, mite),
                new EncounterDefinition(CombatContentIds.Encounter2, oracle),
                new EncounterDefinition(CombatContentIds.Encounter3, elite),
                new EncounterDefinition(CombatContentIds.Encounter4, stalker),
                new EncounterDefinition(CombatContentIds.Encounter5, warden)
            };
            _enemies = new Dictionary<ContentId, EnemyDefinition>
            {
                { mite.Id, mite }, { oracle.Id, oracle }, { elite.Id, elite },
                { stalker.Id, stalker }, { warden.Id, warden }
            };
        }

        public IReadOnlyList<EncounterDefinition> Encounters => _encounters;

        public EncounterDefinition GetEncounter(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= _encounters.Count)
                throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
            return _encounters[zeroBasedIndex];
        }

        public EnemyDefinition GetEnemy(ContentId enemyId)
        {
            EnemyDefinition enemy;
            if (!_enemies.TryGetValue(enemyId, out enemy))
                throw new KeyNotFoundException("Unknown enemy content ID: " + enemyId);
            return enemy;
        }

        private static IntentDefinition Intent(string id, string telegraphKey, params IntentEffectDefinition[] effects)
        {
            return new IntentDefinition(id, telegraphKey, effects);
        }
    }
}
