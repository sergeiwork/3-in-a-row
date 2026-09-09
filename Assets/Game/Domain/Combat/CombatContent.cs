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

        public static readonly ContentId BriarWisp = "enemy.briar_wisp";
        public static readonly ContentId AshbackBoar = "enemy.ashback_boar";
        public static readonly ContentId CinderNymph = "enemy.cinder_nymph";
        public static readonly ContentId ThornboundStag = "enemy.thornbound_stag";
        public static readonly ContentId PyreheartTreant = "enemy.pyreheart_treant";

        public static readonly ContentId NullwingBat = "enemy.nullwing_bat";
        public static readonly ContentId MirrorEel = "enemy.mirror_eel";
        public static readonly ContentId RiftWeaver = "enemy.rift_weaver";
        public static readonly ContentId EclipseChimera = "enemy.eclipse_chimera";
        public static readonly ContentId AstralDevourer = "enemy.astral_devourer";

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

        public static readonly ContentId EncounterBriarWisp = "encounter.region2.briar_wisp";
        public static readonly ContentId EncounterAshbackBoar = "encounter.region2.ashback_boar";
        public static readonly ContentId EncounterCinderNymph = "encounter.region2.cinder_nymph";
        public static readonly ContentId EncounterEliteThornboundStag = "encounter.region2.elite.thornbound_stag";
        public static readonly ContentId EncounterBossPyreheartTreant = "encounter.region2.boss.pyreheart_treant";

        public static readonly ContentId EncounterNullwingBat = "encounter.region3.nullwing_bat";
        public static readonly ContentId EncounterMirrorEel = "encounter.region3.mirror_eel";
        public static readonly ContentId EncounterRiftWeaver = "encounter.region3.rift_weaver";
        public static readonly ContentId EncounterEliteEclipseChimera = "encounter.region3.elite.eclipse_chimera";
        public static readonly ContentId EncounterBossAstralDevourer = "encounter.region3.boss.astral_devourer";
    }

    public enum IntentEffectType
    {
        DamagePlayer,
        ApplyBoardStatus,
        DrainResources,
        GainEnemyBarrier,
        JamActiveSkill
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

        public static IntentEffectDefinition Barrier(int amount)
        {
            return new IntentEffectDefinition(IntentEffectType.GainEnemyBarrier, amount, "status.none", 0, 0, 0);
        }

        public static IntentEffectDefinition Jam(int turns)
        {
            return new IntentEffectDefinition(IntentEffectType.JamActiveSkill, turns, "status.none", 0, 0, 0);
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
        public readonly IReadOnlyList<IntentDefinition> SecondPhaseIntentCycle;
        public readonly int SecondPhaseHealthPercent;

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
            SecondPhaseIntentCycle = new IntentDefinition[0];
            SecondPhaseHealthPercent = 0;
        }

        public EnemyDefinition(
            ContentId id,
            string displayKey,
            int maxHealth,
            int rewardXp,
            ContentId dominantPressureId,
            bool isElite,
            bool isBoss,
            int secondPhaseHealthPercent,
            IntentDefinition[] intentCycle,
            IntentDefinition[] secondPhaseIntentCycle)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (intentCycle == null || intentCycle.Length == 0) throw new ArgumentException("An enemy needs at least one intent.", nameof(intentCycle));
            if (secondPhaseIntentCycle == null || secondPhaseIntentCycle.Length == 0) throw new ArgumentException("A phased enemy needs a second intent cycle.", nameof(secondPhaseIntentCycle));
            Id = id;
            DisplayKey = displayKey ?? string.Empty;
            MaxHealth = maxHealth;
            RewardXp = rewardXp;
            DominantPressureId = dominantPressureId;
            IsElite = isElite;
            IsBoss = isBoss;
            IntentCycle = intentCycle;
            SecondPhaseIntentCycle = secondPhaseIntentCycle;
            SecondPhaseHealthPercent = secondPhaseHealthPercent;
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
        IReadOnlyList<EncounterDefinition> GetNormalPool(int regionIndex, int depth);
        IReadOnlyList<EncounterDefinition> GetElitePool(int regionIndex);
        EncounterDefinition GetRegionBossEncounter(int regionIndex);
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
        private readonly Dictionary<int, IReadOnlyList<EncounterDefinition>> _regionalNormalPools;
        private readonly Dictionary<int, IReadOnlyList<EncounterDefinition>> _regionalElitePools;
        private readonly Dictionary<int, EncounterDefinition> _regionalBossEncounters;
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
                CombatContentIds.CrystalWarden, "enemy.crystal_warden.name", 128, 1, "pressure.anchor", false, true, 50,
                new[]
                {
                    Intent("intent.crystal_warden.seal", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                    Intent("intent.crystal_warden.shardstorm_10", "intent.shardstorm", IntentEffectDefinition.Damage(10)),
                    Intent("intent.crystal_warden.freeze_anchor", "intent.freeze_anchor",
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2),
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                    Intent("intent.crystal_warden.shardstorm_12", "intent.shardstorm", IntentEffectDefinition.Damage(12))
                },
                new[]
                {
                    Intent("intent.crystal_warden.phase2.barrier", "intent.barrier", IntentEffectDefinition.Barrier(18)),
                    Intent("intent.crystal_warden.phase2.jam", "intent.jam", IntentEffectDefinition.Damage(8), IntentEffectDefinition.Jam(1)),
                    Intent("intent.crystal_warden.phase2.shardstorm", "intent.shardstorm", IntentEffectDefinition.Damage(13))
                });

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
                CombatContentIds.FacetEngine, "enemy.facet_engine.name", 132, 1, "pressure.mixed", false, true, 50,
                new[]
                {
                    Intent("intent.facet_engine.anchor_2", "intent.seal", IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                    Intent("intent.facet_engine.hit_crack", "intent.crush",
                        IntentEffectDefinition.Damage(9), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                    Intent("intent.facet_engine.freeze_drain", "intent.freeze_anchor",
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2), IntentEffectDefinition.Drain(2, 2)),
                    Intent("intent.facet_engine.hit_13", "intent.shardstorm", IntentEffectDefinition.Damage(13))
                },
                new[]
                {
                    Intent("intent.facet_engine.phase2.thorns", "intent.thorns", IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 3)),
                    Intent("intent.facet_engine.phase2.barrier", "intent.barrier", IntentEffectDefinition.Barrier(16)),
                    Intent("intent.facet_engine.phase2.overload", "intent.bolt", IntentEffectDefinition.Damage(12))
                });

            // Region 2: the Cinderbloom Wilds introduces Thorned as sustained board pressure.
            var briarWisp = new EnemyDefinition(
                CombatContentIds.BriarWisp, "enemy.briar_wisp.name", 112, 1, "pressure.thorns",
                Intent("intent.briar_wisp.thorn_kiss", "intent.thorn_kiss",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 2)),
                Intent("intent.briar_wisp.needleflare", "intent.needleflare",
                    IntentEffectDefinition.Damage(8), IntentEffectDefinition.Jam(1)),
                Intent("intent.briar_wisp.bramble_burst", "intent.bramble_burst",
                    IntentEffectDefinition.Damage(4), IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 3)));
            var ashbackBoar = new EnemyDefinition(
                CombatContentIds.AshbackBoar, "enemy.ashback_boar.name", 126, 1, "pressure.crack",
                Intent("intent.ashback_boar.cinder_charge", "intent.cinder_charge", IntentEffectDefinition.Damage(10)),
                Intent("intent.ashback_boar.faultline", "intent.faultline",
                    IntentEffectDefinition.Damage(7), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                Intent("intent.ashback_boar.magma_hide", "intent.magma_hide", IntentEffectDefinition.Barrier(12)));
            var cinderNymph = new EnemyDefinition(
                CombatContentIds.CinderNymph, "enemy.cinder_nymph.name", 118, 1, "pressure.jam",
                Intent("intent.cinder_nymph.ember_veil", "intent.ember_veil", IntentEffectDefinition.Barrier(10)),
                Intent("intent.cinder_nymph.wildfire", "intent.wildfire",
                    IntentEffectDefinition.Damage(8), IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 2)),
                Intent("intent.cinder_nymph.hexflare", "intent.hexflare",
                    IntentEffectDefinition.Damage(6), IntentEffectDefinition.Jam(1)));
            var thornboundStag = new EnemyDefinition(
                CombatContentIds.ThornboundStag, "enemy.thornbound_stag.name", 154, 2, "pressure.thorns", true, false,
                Intent("intent.thornbound_stag.antler_sweep", "intent.antler_sweep",
                    IntentEffectDefinition.Damage(12), IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 2)),
                Intent("intent.thornbound_stag.root_snare", "intent.root_snare",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                Intent("intent.thornbound_stag.heartfire", "intent.heartfire", IntentEffectDefinition.Damage(15)));
            var pyreheartTreant = new EnemyDefinition(
                CombatContentIds.PyreheartTreant, "enemy.pyreheart_treant.name", 188, 2, "pressure.thorns", false, true, 50,
                new[]
                {
                    Intent("intent.pyreheart_treant.bramble_crown", "intent.bramble_crown",
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 4)),
                    Intent("intent.pyreheart_treant.furnace_roar", "intent.furnace_roar",
                        IntentEffectDefinition.Damage(14), IntentEffectDefinition.Jam(1)),
                    Intent("intent.pyreheart_treant.rootquake", "intent.rootquake",
                        IntentEffectDefinition.Damage(10), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 3)),
                    Intent("intent.pyreheart_treant.ember_bark", "intent.ember_bark", IntentEffectDefinition.Barrier(20))
                },
                new[]
                {
                    Intent("intent.pyreheart_treant.phase2.wildfire", "intent.wildfire",
                        IntentEffectDefinition.Damage(12), IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 3)),
                    Intent("intent.pyreheart_treant.phase2.sapping_flame", "intent.sapping_flame",
                        IntentEffectDefinition.Damage(8), IntentEffectDefinition.Drain(2, 2)),
                    Intent("intent.pyreheart_treant.phase2.inferno", "intent.inferno", IntentEffectDefinition.Damage(18))
                });

            // Region 3: the Voidglass Depths attacks active-skill timing and board mobility.
            var nullwingBat = new EnemyDefinition(
                CombatContentIds.NullwingBat, "enemy.nullwing_bat.name", 142, 1, "pressure.jam",
                Intent("intent.nullwing_bat.null_screech", "intent.null_screech", IntentEffectDefinition.Jam(2)),
                Intent("intent.nullwing_bat.void_bite", "intent.void_bite", IntentEffectDefinition.Damage(12)),
                Intent("intent.nullwing_bat.nightglass", "intent.nightglass",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2), IntentEffectDefinition.Jam(1)));
            var mirrorEel = new EnemyDefinition(
                CombatContentIds.MirrorEel, "enemy.mirror_eel.name", 150, 1, "pressure.barrier",
                Intent("intent.mirror_eel.reflection", "intent.reflection", IntentEffectDefinition.Barrier(16)),
                Intent("intent.mirror_eel.prism_lash", "intent.prism_lash", IntentEffectDefinition.Damage(13)),
                Intent("intent.mirror_eel.siphon_glide", "intent.siphon_glide",
                    IntentEffectDefinition.Damage(7), IntentEffectDefinition.Drain(3, 3)));
            var riftWeaver = new EnemyDefinition(
                CombatContentIds.RiftWeaver, "enemy.rift_weaver.name", 164, 1, "pressure.mixed",
                Intent("intent.rift_weaver.rift_tether", "intent.rift_tether",
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1),
                    IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 2)),
                Intent("intent.rift_weaver.rupture", "intent.rupture",
                    IntentEffectDefinition.Damage(11), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 2)),
                Intent("intent.rift_weaver.entropy_thread", "intent.entropy_thread",
                    IntentEffectDefinition.Drain(2, 2), IntentEffectDefinition.Jam(1)));
            var eclipseChimera = new EnemyDefinition(
                CombatContentIds.EclipseChimera, "enemy.eclipse_chimera.name", 198, 2, "pressure.mixed", true, false,
                Intent("intent.eclipse_chimera.eclipse_veil", "intent.eclipse_veil", IntentEffectDefinition.Barrier(22)),
                Intent("intent.eclipse_chimera.umbra_talon", "intent.umbra_talon",
                    IntentEffectDefinition.Damage(14), IntentEffectDefinition.Jam(1)),
                Intent("intent.eclipse_chimera.gravity_knot", "intent.gravity_knot",
                    IntentEffectDefinition.Damage(8), IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 3, 1)));
            var astralDevourer = new EnemyDefinition(
                CombatContentIds.AstralDevourer, "enemy.astral_devourer.name", 260, 3, "pressure.mixed", false, true, 50,
                new[]
                {
                    Intent("intent.astral_devourer.singularity_drag", "intent.singularity_drag",
                        IntentEffectDefinition.Drain(4, 4), IntentEffectDefinition.Jam(1)),
                    Intent("intent.astral_devourer.event_horizon", "intent.event_horizon",
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 3, 1),
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Thorned, 2)),
                    Intent("intent.astral_devourer.starfall", "intent.starfall", IntentEffectDefinition.Damage(18)),
                    Intent("intent.astral_devourer.void_carapace", "intent.void_carapace", IntentEffectDefinition.Barrier(26))
                },
                new[]
                {
                    Intent("intent.astral_devourer.phase2.collapse", "intent.collapse",
                        IntentEffectDefinition.Damage(14), IntentEffectDefinition.ApplyStatus(BoardContentIds.Cracked, 3)),
                    Intent("intent.astral_devourer.phase2.gravity_lock", "intent.gravity_lock",
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Frozen, 2),
                        IntentEffectDefinition.ApplyStatus(BoardContentIds.Anchored, 2, 1)),
                    Intent("intent.astral_devourer.phase2.devour", "intent.devour", IntentEffectDefinition.Damage(22))
                });

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
                { roc.Id, roc }, { engine.Id, engine },
                { briarWisp.Id, briarWisp }, { ashbackBoar.Id, ashbackBoar },
                { cinderNymph.Id, cinderNymph }, { thornboundStag.Id, thornboundStag },
                { pyreheartTreant.Id, pyreheartTreant }, { nullwingBat.Id, nullwingBat },
                { mirrorEel.Id, mirrorEel }, { riftWeaver.Id, riftWeaver },
                { eclipseChimera.Id, eclipseChimera }, { astralDevourer.Id, astralDevourer }
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
            var encounterBriarWisp = new EncounterDefinition(CombatContentIds.EncounterBriarWisp, briarWisp);
            var encounterAshbackBoar = new EncounterDefinition(CombatContentIds.EncounterAshbackBoar, ashbackBoar);
            var encounterCinderNymph = new EncounterDefinition(CombatContentIds.EncounterCinderNymph, cinderNymph);
            var eliteThornboundStag = new EncounterDefinition(CombatContentIds.EncounterEliteThornboundStag, thornboundStag);
            var bossPyreheartTreant = new EncounterDefinition(CombatContentIds.EncounterBossPyreheartTreant, pyreheartTreant);
            var encounterNullwingBat = new EncounterDefinition(CombatContentIds.EncounterNullwingBat, nullwingBat);
            var encounterMirrorEel = new EncounterDefinition(CombatContentIds.EncounterMirrorEel, mirrorEel);
            var encounterRiftWeaver = new EncounterDefinition(CombatContentIds.EncounterRiftWeaver, riftWeaver);
            var eliteEclipseChimera = new EncounterDefinition(CombatContentIds.EncounterEliteEclipseChimera, eclipseChimera);
            var bossAstralDevourer = new EncounterDefinition(CombatContentIds.EncounterBossAstralDevourer, astralDevourer);
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
                { warden.Id, _encounters[4] }, { engine.Id, bossEngine },
                { pyreheartTreant.Id, bossPyreheartTreant }, { astralDevourer.Id, bossAstralDevourer }
            };
            _regionalNormalPools = new Dictionary<int, IReadOnlyList<EncounterDefinition>>
            {
                { RegionalPoolKey(1, 1), new[] { encounterBriarWisp, encounterAshbackBoar } },
                { RegionalPoolKey(1, 2), new[] { encounterBriarWisp, encounterCinderNymph } },
                { RegionalPoolKey(1, 3), new[] { encounterAshbackBoar, encounterCinderNymph } },
                { RegionalPoolKey(1, 4), new[] { encounterBriarWisp, encounterAshbackBoar, encounterCinderNymph } },
                { RegionalPoolKey(2, 1), new[] { encounterNullwingBat, encounterMirrorEel } },
                { RegionalPoolKey(2, 2), new[] { encounterNullwingBat, encounterRiftWeaver } },
                { RegionalPoolKey(2, 3), new[] { encounterMirrorEel, encounterRiftWeaver } },
                { RegionalPoolKey(2, 4), new[] { encounterNullwingBat, encounterMirrorEel, encounterRiftWeaver } }
            };
            _regionalElitePools = new Dictionary<int, IReadOnlyList<EncounterDefinition>>
            {
                { 0, _eliteEncounters },
                { 1, new[] { eliteThornboundStag } },
                { 2, new[] { eliteEclipseChimera } }
            };
            _regionalBossEncounters = new Dictionary<int, EncounterDefinition>
            {
                { 0, _encounters[4] }, { 1, bossPyreheartTreant }, { 2, bossAstralDevourer }
            };
            _encountersById = new Dictionary<ContentId, EncounterDefinition>();
            foreach (var encounter in _encounters) _encountersById[encounter.Id] = encounter;
            foreach (var pool in _normalPools.Values)
                foreach (var encounter in pool) _encountersById[encounter.Id] = encounter;
            foreach (var pool in _regionalNormalPools.Values)
                foreach (var encounter in pool) _encountersById[encounter.Id] = encounter;
            foreach (var pool in _regionalElitePools.Values)
                foreach (var encounter in pool) _encountersById[encounter.Id] = encounter;
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

        public IReadOnlyList<EncounterDefinition> GetNormalPool(int regionIndex, int depth)
        {
            if (regionIndex == 0) return GetNormalPool(depth);
            IReadOnlyList<EncounterDefinition> pool;
            if (!_regionalNormalPools.TryGetValue(RegionalPoolKey(regionIndex, depth), out pool))
                throw new ArgumentOutOfRangeException(nameof(regionIndex));
            return pool;
        }

        public IReadOnlyList<EncounterDefinition> GetElitePool(int regionIndex)
        {
            IReadOnlyList<EncounterDefinition> pool;
            if (!_regionalElitePools.TryGetValue(regionIndex, out pool))
                throw new ArgumentOutOfRangeException(nameof(regionIndex));
            return pool;
        }

        public EncounterDefinition GetRegionBossEncounter(int regionIndex)
        {
            EncounterDefinition encounter;
            if (!_regionalBossEncounters.TryGetValue(regionIndex, out encounter))
                throw new ArgumentOutOfRangeException(nameof(regionIndex));
            return encounter;
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

        private static int RegionalPoolKey(int regionIndex, int depth)
        {
            return regionIndex * 10 + depth;
        }
    }
}
