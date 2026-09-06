using System;
using System.Collections.Generic;
using NUnit.Framework;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Mastery;
using ThreeInARow.Domain.Meta;
using ThreeInARow.Domain.Map;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Tests
{
    public sealed class R3R4AcceptanceTests
    {
        [Test]
        public void WeeklyChallenge_IsPinnedForTheWholeUtcWeek()
        {
            var monday = WeeklyChallenge.ForUtcDate(new DateTime(2026, 9, 7, 1, 0, 0, DateTimeKind.Utc));
            var sunday = WeeklyChallenge.ForUtcDate(new DateTime(2026, 9, 13, 23, 59, 0, DateTimeKind.Utc));
            Assert.That(sunday.Id, Is.EqualTo(monday.Id));
            Assert.That(sunday.Seed, Is.EqualTo(monday.Seed));
            Assert.That(monday.ContentVersion, Is.EqualTo(RunState.CurrentContentVersion));
            Assert.That(monday.UnlockPolicyId, Is.EqualTo(MasteryContentIds.AllContentUnlockPolicy));
        }

        [Test]
        public void ProfileUnlocks_AreHorizontalAndDeterministic()
        {
            var first = ProfileProgression.CreateFresh();
            var second = ProfileProgression.CreateFresh();
            foreach (var profile in new[] { first, second })
            {
                profile.DominantBranchWins.Add("branch.ember");
                profile.DominantBranchWins.Add("branch.tide");
                profile.DominantBranchWins.Add("branch.venom");
            }
            var signals = new RunCompletionSignals
            {
                Victory = true,
                BossId = CombatContentIds.CrystalWarden,
                DifficultyTier = 0,
                RemainingHealth = 17,
                ValidTurnCount = 31,
                LargestCascade = 4,
                DominantBranchId = "branch.volt",
                EmberSkillsLearned = 3,
                MaxPoisonStacksInResponse = 2,
                MaxCleanseStatusKinds = 3,
                FlawlessEliteVictories = 1,
                SpecialActivations = 3,
                SparkActivations = 3,
                FocusConversions = 4,
                PoisonApplications = 1
            };

            var updateA = ProfileProgression.CompleteRun(first, signals);
            var updateB = ProfileProgression.CompleteRun(second, signals);
            Assert.That(Ids(updateA.UnlockedContent), Is.EqualTo(Ids(updateB.UnlockedContent)));
            Assert.That(first.BestDifficultyUnlocked, Is.EqualTo(1));
            Assert.That(Contains(first.UnlockedContentIds, CombatContentIds.FacetEngine), Is.True);
            Assert.That(Contains(first.UnlockedContentIds, ProgressionContentIds.Transmute), Is.True);
            Assert.That(first.Aggregate.RunsWon, Is.EqualTo(1));
        }

        [Test]
        public void DifficultyRules_ApplyDamageAndUnstableGrid()
        {
            var sharp = NewRun(1);
            CombatSimulation.StartEncounter(sharp, 0);
            sharp.PendingCombatTurn.AwaitingEnemyResponse = true;
            CombatSimulation.CompleteTurn(sharp);
            Assert.That(sharp.Player.Health, Is.EqualTo(PlayerState.MaxHealth - 6));

            var unstable = NewRun(2);
            CombatSimulation.StartEncounter(unstable, 0);
            var cracked = 0;
            foreach (var gem in unstable.Board.Gems)
                if (Contains(gem.StatusIds, BoardContentIds.Cracked)) cracked++;
            Assert.That(cracked, Is.EqualTo(2));
        }

        [Test]
        public void PerfectFacet_EntersTelegraphedSecondPhaseAndUsesBarrier()
        {
            var state = NewRun(5);
            CombatSimulation.StartEncounter(state, CombatContentIds.Encounter5, 4);
            state.Enemy.Health = 70;
            var skill = ProgressionSimulation.UseActiveSkill(state,
                new ThreeInARow.Domain.Commands.UseSkillCommand { SkillId = ProgressionContentIds.Sunder });
            Assert.That(skill.Accepted, Is.True);
            Assert.That(state.Enemy.Phase, Is.EqualTo(1));
            Assert.That(HasEvent(skill.Events, SimulationEventType.BossPhaseChanged), Is.True);
            Assert.That(HasEvent(skill.Events, SimulationEventType.EnemyIntentTelegraphed), Is.True);

            state.PendingCombatTurn.AwaitingEnemyResponse = true;
            state.Enemy.IntentIndex = 0;
            CombatSimulation.CompleteTurn(state);
            Assert.That(state.Enemy.Barrier, Is.EqualTo(18));

            var jamTarget = state.Enemy.TelegraphedTargetId;
            state.PendingCombatTurn.AwaitingEnemyResponse = true;
            var jam = CombatSimulation.CompleteTurn(state);
            Assert.That(HasEvent(jam.Events, SimulationEventType.ActiveJammed), Is.True);
            Assert.That(ProgressionRules.FindCooldown(state.Player, jamTarget).RemainingTurns, Is.EqualTo(1));
        }

        [Test]
        public void HostilePattern_AddsTelegraphedThornsAndThornDamageIsCapped()
        {
            var state = NewRun(4);
            CombatSimulation.StartEncounter(state, CombatContentIds.EncounterEliteFractureGolem, 3);
            state.Enemy.IntentIndex = 2;
            state.PendingCombatTurn.AwaitingEnemyResponse = true;
            var hostile = CombatSimulation.CompleteTurn(state);
            Assert.That(HasEvent(hostile.Events, SimulationEventType.StatusAdded), Is.True);

            foreach (var gem in state.Board.Gems)
                if (!Contains(gem.StatusIds, BoardContentIds.Thorned)) gem.StatusIds.Add(BoardContentIds.Thorned);
            var health = state.Player.Health;
            var swap = BoardSimulation.FindLegalSwaps(state.Board)[0];
            var result = CombatSimulation.BeginSwap(state, new ThreeInARow.Domain.Commands.SwapCommand
            {
                CellA = swap.CellA,
                CellB = swap.CellB
            });
            Assert.That(result.Accepted, Is.True);
            Assert.That(health - state.Player.Health, Is.EqualTo(6));
        }

        [Test]
        public void Reweave_UsesBoardSpawnAndLeavesAStablePlayableBoard()
        {
            var state = NewRun(0);
            var targets = new List<GridCell>();
            foreach (var gem in state.Board.Gems)
            {
                if (gem.SpecialId.Equals(BoardContentIds.NoSpecial) && gem.StatusIds.Count == 0)
                    targets.Add(gem.Cell);
                if (targets.Count == 3) break;
            }
            var events = BoardSimulation.Reweave(state, targets, ProgressionContentIds.Reweave);
            Assert.That(HasEvent(events, SimulationEventType.GemTransmuted), Is.True);
            Assert.That(BoardSimulation.HasPreExistingMatch(state.Board), Is.False);
            Assert.That(BoardSimulation.FindLegalSwaps(state.Board), Is.Not.Empty);
        }

        [Test]
        public void HybridRewards_RequireProfileUnlockAndBothRunBranches()
        {
            var state = NewRun(0);
            state.AvailableContentIds.Add(ProgressionContentIds.Flashfire);
            state.SelectedSkillIds.Add(ProgressionContentIds.Kindling);
            var events = new EventBatch();
            ProgressionSimulation.OfferEventReward(state, "test.hybrid", null, 50, events);
            Assert.That(Contains(state.PendingChoice.OptionIds, ProgressionContentIds.Flashfire), Is.False);

            state.PendingChoice = new PendingChoiceState();
            state.SelectedSkillIds.Add(ProgressionContentIds.Overcharge);
            ProgressionSimulation.OfferEventReward(state, "test.hybrid", null, 50, events);
            Assert.That(Contains(state.PendingChoice.OptionIds, ProgressionContentIds.Flashfire), Is.True);
        }

        [Test]
        public void MapGeneration_UsesThePinnedUnlockSnapshot()
        {
            var sawUnlockedBoss = false;
            var sawUnlockedEvent = false;
            for (ulong seed = 1; seed <= 64; seed++)
            {
                var fresh = new RunState { Seed = seed, RandomStreams = RandomStreams.Create(seed) };
                MapSimulation.Generate(fresh);
                Assert.That(fresh.Map.BossEnemyId, Is.EqualTo(CombatContentIds.CrystalWarden));
                foreach (var node in fresh.Map.Nodes)
                    Assert.That(node.ContentId, Is.Not.EqualTo(MapContentIds.PrismaticArchive));

                var unlocked = new RunState
                {
                    Seed = seed,
                    UnlockPolicyId = MasteryContentIds.AllContentUnlockPolicy,
                    RandomStreams = RandomStreams.Create(seed)
                };
                MapSimulation.Generate(unlocked);
                sawUnlockedBoss |= unlocked.Map.BossEnemyId.Equals(CombatContentIds.FacetEngine);
                foreach (var node in unlocked.Map.Nodes)
                    sawUnlockedEvent |= node.ContentId.Equals(MapContentIds.PrismaticArchive);
            }
            Assert.That(sawUnlockedBoss, Is.True);
            Assert.That(sawUnlockedEvent, Is.True);
        }

        private static RunState NewRun(int difficultyTier)
        {
            var state = new RunState
            {
                Seed = 0xA11CE55UL,
                DifficultyTier = difficultyTier,
                DifficultyId = MasteryContentCatalog.Instance.Get(difficultyTier).Id,
                UnlockPolicyId = MasteryContentIds.AllContentUnlockPolicy,
                RandomStreams = RandomStreams.Create(0xA11CE55UL)
            };
            ProgressionSimulation.InitializeRun(state);
            BoardSimulation.InitializeBoard(state);
            return state;
        }

        private static bool HasEvent(EventBatch events, SimulationEventType type)
        {
            foreach (var item in events.Events) if (item.Type == type) return true;
            return false;
        }

        private static bool Contains(IEnumerable<ContentId> ids, ContentId wanted)
        {
            foreach (var id in ids) if (id.Equals(wanted)) return true;
            return false;
        }

        private static string Ids(IEnumerable<ContentId> ids)
        {
            var values = new List<string>();
            foreach (var id in ids) values.Add(id.Value);
            return string.Join("|", values.ToArray());
        }
    }
}
