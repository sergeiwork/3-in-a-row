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
        public static readonly ContentId CrystalTick = "enemy.crystal_tick";
        public static readonly ContentId RimeMoth = "enemy.rime_moth";
        public static readonly ContentId AnchorCrab = "enemy.anchor_crab";
        public static readonly ContentId HollowIdol = "enemy.hollow_idol";
        public static readonly ContentId FractureGolem = "enemy.fracture_golem";
        public static readonly ContentId StormglassRoc = "enemy.stormglass_roc";
        public static readonly ContentId FacetEngine = "enemy.facet_engine";

        public static readonly ContentId Encounter1 = "encounter.01_geode_mite";
        public static readonly ContentId Encounter2 = "encounter.02_frost_oracle";
        public static readonly ContentId Encounter3 = "encounter.03_geode_mite_elite";
        public static readonly ContentId Encounter4 = "encounter.04_prism_stalker";
        public static readonly ContentId Encounter5 = "encounter.05_crystal_warden";
        public static readonly ContentId EncounterDepth1CrystalTick = "encounter.depth1.crystal_tick";
        public static readonly ContentId EncounterDepth2CrystalTick = "encounter.depth2.crystal_tick";
        public static readonly ContentId EncounterDepth2RimeMoth = "encounter.depth2.rime_moth";
        public static readonly ContentId EncounterDepth3RimeMoth = "encounter.depth3.rime_moth";
        public static readonly ContentId EncounterDepth3AnchorCrab = "encounter.depth3.anchor_crab";
        public static readonly ContentId EncounterDepth4AnchorCrab = "encounter.depth4.anchor_crab";
        public static readonly ContentId EncounterDepth4HollowIdol = "encounter.depth4.hollow_idol";
        public static readonly ContentId EncounterEliteFractureGolem = "encounter.elite.fracture_golem";
        public static readonly ContentId EncounterEliteStormglassRoc = "encounter.elite.stormglass_roc";
        public static readonly ContentId EncounterBossFacetEngine = "encounter.boss.facet_engine";
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
        public readonly ContentId DominantPressureId;
        public readonly bool IsElite;
        public readonly bool IsBoss;

        public EnemyDefinition(
            ContentId id,
            string displayKey,
            int maxHealth,
            int rewardXp,
            ContentId dominantPressureId,
            bool isElite,
            bool isBoss,
            params IntentDefinition[] intentCycle)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (intentCycle == null || intentCycle.Length == 0)
                throw new ArgumentException("An enemy needs at least one intent.", nameof(intentCycle));
            Id = id;
            DisplayKey = displayKey ?? string.Empty;
            MaxHealth = maxHealth;
            RewardXp = rewardXp;
            DominantPressureId = dominantPressureId;
            IsElite = isElite;
            IsBoss = isBoss;
            IntentCycle = intentCycle;
        }

        public EnemyDefinition(
            ContentId id,
            string displayKey,
            int maxHealth,
            int rewardXp,
            ContentId dominantPressureId,
            params IntentDefinition[] intentCycle)
            : this(id, displayKey, maxHealth, rewardXp, dominantPressureId, false, false, intentCycle) { }
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
        IReadOnlyList<EncounterDefinition> EliteEncounters { get; }
        EncounterDefinition GetEncounter(int zeroBasedIndex);
        EncounterDefinition GetEncounter(ContentId encounterId);
        IReadOnlyList<EncounterDefinition> GetNormalPool(int depth);
        EncounterDefinition GetBossEncounter(ContentId enemyId);
        EnemyDefinition GetEnemy(ContentId enemyId);
    }

    /// <summary>Immutable MVP combat content. Enemy execution contains no per-enemy branching.</summary>
    public sealed class MvpCombatContentCatalog : ICombatContentCatalog
    {
        private readonly List<EncounterDefinition> _encounters;
        private readonly Dictionary<ContentId, EnemyDefinition> _enemies;
        private readonly Dictionary<ContentId, EncounterDefinition> _encountersById;
        private readonly Dictionary<int, IReadOnlyList<EncounterDefinition>> _normalPools;
        private readonly List<EncounterDefinition> _eliteEncounters;
        private readonly Dictionary<ContentId, EncounterDefinition> _bossEncounters;

        public static readonly MvpCombatContentCatalog Instance = new MvpCombatContentCatalog();

        private MvpCombatContentCatalog()
        {
            var mite = new EnemyDefinition(
                CombatContentIds.GeodeMite, "enemy.geode_mite.name", 52, 1, "pressure.crack",
                Intent("intent.geode_mite.chip_5", "intent.chip", IntentEffectDefinition.Damage(5)),
                Intent("intent.geode_mite.crack_3", "intent.crack", IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 3)),
                Intent("intent.geode_mite.chip_6", "intent.chip", IntentEffectDefinition.Damage(6)));

            var oracle = new EnemyDefinition(
                CombatContentIds.FrostOracle, "enemy.frost_oracle.name", 66, 1, "pressure.freeze",
                Intent("intent.frost_oracle.freeze_2", "intent.chill", IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2)),
                Intent("intent.frost_oracle.needle_7", "intent.needle", IntentEffectDefinition.Damage(7)),
                Intent("intent.frost_oracle.freeze_3", "intent.chill", IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 3)));

            var elite = new EnemyDefinition(
                CombatContentIds.GeodeMiteElite, "enemy.geode_mite_elite.name", 84, 1, "pressure.crack",
                Intent("intent.geode_mite_elite.crush", "intent.crush",
                    IntentEffectDefinition.Damage(8), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                Intent("intent.geode_mite_elite.chip_7", "intent.chip", IntentEffectDefinition.Damage(7)),
                Intent("intent.geode_mite_elite.crack_4", "intent.crack", IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 4)));

            var stalker = new EnemyDefinition(
                CombatContentIds.PrismStalker, "enemy.prism_stalker.name", 92, 1, "pressure.drain",
                Intent("intent.prism_stalker.bolt_8", "intent.bolt", IntentEffectDefinition.Damage(8)),
                Intent("intent.prism_stalker.drain", "intent.drain", IntentEffectDefinition.Drain(3, 3)),
                Intent("intent.prism_stalker.bolt_10", "intent.bolt", IntentEffectDefinition.Damage(10)));

            var warden = new EnemyDefinition(
                CombatContentIds.CrystalWarden, "enemy.crystal_warden.name", 128, 1, "pressure.anchor", false, true,
                Intent("intent.crystal_warden.seal", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.crystal_warden.shardstorm_10", "intent.shardstorm", IntentEffectDefinition.Damage(10)),
                Intent("intent.crystal_warden.freeze_anchor", "intent.freeze_anchor",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2),
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.crystal_warden.shardstorm_12", "intent.shardstorm", IntentEffectDefinition.Damage(12)));

            var tick = new EnemyDefinition(
                CombatContentIds.CrystalTick, "enemy.crystal_tick.name", 56, 1, "pressure.drain",
                Intent("intent.crystal_tick.drain_1", "intent.drain", IntentEffectDefinition.Drain(1, 1)),
                Intent("intent.crystal_tick.bite_6", "intent.bite", IntentEffectDefinition.Damage(6)),
                Intent("intent.crystal_tick.crack_2", "intent.crack", IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)));
            var moth = new EnemyDefinition(
                CombatContentIds.RimeMoth, "enemy.rime_moth.name", 70, 1, "pressure.freeze",
                Intent("intent.rime_moth.freeze_hit", "intent.freeze_hit",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 1), IntentEffectDefinition.Damage(4)),
                Intent("intent.rime_moth.needle_7", "intent.needle", IntentEffectDefinition.Damage(7)),
                Intent("intent.rime_moth.freeze_2", "intent.chill", IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2)));
            var crab = new EnemyDefinition(
                CombatContentIds.AnchorCrab, "enemy.anchor_crab.name", 86, 1, "pressure.anchor",
                Intent("intent.anchor_crab.anchor_2", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.anchor_crab.claw_8", "intent.claw", IntentEffectDefinition.Damage(8)),
                Intent("intent.anchor_crab.hit_crack", "intent.crush",
                    IntentEffectDefinition.Damage(5), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)));
            var idol = new EnemyDefinition(
                CombatContentIds.HollowIdol, "enemy.hollow_idol.name", 94, 1, "pressure.drain",
                Intent("intent.hollow_idol.drain_2", "intent.drain", IntentEffectDefinition.Drain(2, 2)),
                Intent("intent.hollow_idol.crack_3", "intent.crack", IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 3)),
                Intent("intent.hollow_idol.bolt_10", "intent.bolt", IntentEffectDefinition.Damage(10)));
            var golem = new EnemyDefinition(
                CombatContentIds.FractureGolem, "enemy.fracture_golem.name", 112, 1, "pressure.crack", true, false,
                Intent("intent.fracture_golem.hit_crack", "intent.crush",
                    IntentEffectDefinition.Damage(7), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                Intent("intent.fracture_golem.anchor_2", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.fracture_golem.hit_11", "intent.crush", IntentEffectDefinition.Damage(11)));
            var roc = new EnemyDefinition(
                CombatContentIds.StormglassRoc, "enemy.stormglass_roc.name", 108, 1, "pressure.mixed", true, false,
                Intent("intent.stormglass_roc.freeze_2", "intent.chill", IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2)),
                Intent("intent.stormglass_roc.hit_drain", "intent.drain",
                    IntentEffectDefinition.Damage(6), IntentEffectDefinition.Drain(2, 2)),
                Intent("intent.stormglass_roc.hit_10", "intent.bolt", IntentEffectDefinition.Damage(10)));
            var engine = new EnemyDefinition(
                CombatContentIds.FacetEngine, "enemy.facet_engine.name", 132, 1, "pressure.mixed", false, true,
                Intent("intent.facet_engine.anchor_2", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.facet_engine.hit_crack", "intent.crush",
                    IntentEffectDefinition.Damage(9), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                Intent("intent.facet_engine.freeze_drain", "intent.freeze_anchor",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2), IntentEffectDefinition.Drain(2, 2)),
                Intent("intent.facet_engine.hit_13", "intent.shardstorm", IntentEffectDefinition.Damage(13)));

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
                { stalker.Id, stalker }, { warden.Id, warden }, { tick.Id, tick },
                { moth.Id, moth }, { crab.Id, crab }, { idol.Id, idol }, { golem.Id, golem },
                { roc.Id, roc }, { engine.Id, engine }
            };

            var depth1Tick = new EncounterDefinition(CombatContentIds.EncounterDepth1CrystalTick, tick);
            var depth2Tick = new EncounterDefinition(CombatContentIds.EncounterDepth2CrystalTick, tick);
            var depth2Moth = new EncounterDefinition(CombatContentIds.EncounterDepth2RimeMoth, moth);
            var depth3Moth = new EncounterDefinition(CombatContentIds.EncounterDepth3RimeMoth, moth);
            var depth3Crab = new EncounterDefinition(CombatContentIds.EncounterDepth3AnchorCrab, crab);
            var depth4Crab = new EncounterDefinition(CombatContentIds.EncounterDepth4AnchorCrab, crab);
            var depth4Idol = new EncounterDefinition(CombatContentIds.EncounterDepth4HollowIdol, idol);
            var eliteGolem = new EncounterDefinition(CombatContentIds.EncounterEliteFractureGolem, golem);
            var eliteRoc = new EncounterDefinition(CombatContentIds.EncounterEliteStormglassRoc, roc);
            var bossEngine = new EncounterDefinition(CombatContentIds.EncounterBossFacetEngine, engine);
            _normalPools = new Dictionary<int, IReadOnlyList<EncounterDefinition>>
            {
                { 1, new[] { _encounters[0], depth1Tick } },
                { 2, new[] { _encounters[1], depth2Tick, depth2Moth } },
                { 3, new[] { _encounters[2], depth3Moth, depth3Crab } },
                { 4, new[] { _encounters[3], depth4Crab, depth4Idol } }
            };
            _eliteEncounters = new List<EncounterDefinition> { eliteGolem, eliteRoc };
            _bossEncounters = new Dictionary<ContentId, EncounterDefinition>
            {
                { warden.Id, _encounters[4] }, { engine.Id, bossEngine }
            };
            _encountersById = new Dictionary<ContentId, EncounterDefinition>();
            foreach (var encounter in _encounters) _encountersById[encounter.Id] = encounter;
            foreach (var pool in _normalPools.Values)
                foreach (var encounter in pool) _encountersById[encounter.Id] = encounter;
            foreach (var encounter in _eliteEncounters) _encountersById[encounter.Id] = encounter;
            foreach (var encounter in _bossEncounters.Values) _encountersById[encounter.Id] = encounter;
        }

        public IReadOnlyList<EncounterDefinition> Encounters => _encounters;
        public IReadOnlyList<EncounterDefinition> EliteEncounters => _eliteEncounters;

        public EncounterDefinition GetEncounter(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= _encounters.Count)
                throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
            return _encounters[zeroBasedIndex];
        }

        public EncounterDefinition GetEncounter(ContentId encounterId)
        {
            EncounterDefinition encounter;
            if (!_encountersById.TryGetValue(encounterId, out encounter))
                throw new KeyNotFoundException("Unknown encounter content ID: " + encounterId);
            return encounter;
        }

        public IReadOnlyList<EncounterDefinition> GetNormalPool(int depth)
        {
            IReadOnlyList<EncounterDefinition> pool;
            if (!_normalPools.TryGetValue(depth, out pool))
                throw new ArgumentOutOfRangeException(nameof(depth));
            return pool;
        }

        public EncounterDefinition GetBossEncounter(ContentId enemyId)
        {
            EncounterDefinition encounter;
            if (!_bossEncounters.TryGetValue(enemyId, out encounter))
                throw new KeyNotFoundException("Unknown boss enemy ID: " + enemyId);
            return encounter;
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
