using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ThreeInARow.Application;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Map;
using ThreeInARow.Domain.Meta;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.BalanceTests
{
    public enum BalanceAffinity
    {
        Ember,
        Tide,
        Venom,
        Volt,
        Opportunist
    }

    public sealed class BalanceRunResult
    {
        public ulong Seed;
        public BalanceAffinity Affinity;
        public bool Victory;
        public int EncountersCleared;
        public int Turns;
        public int RemainingHealth;
        public int TotalDamage;
        public int HealthDamageTaken;
        public int LargestCascade;
        public int MovesChosen;
        public int MovePreviews;
        public int RewardsChosen;
        public int ActiveSkillsUsed;
        public int AffinitySkillsChosen;
        public string DominantBranches = string.Empty;
        public readonly Dictionary<string, int> BranchDamage = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> SkillUses = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> GemClears = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> SpecialActivations = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> RouteChoices = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> EventChoices = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly List<string> SelectedSkills = new List<string>();

        public string Signature()
        {
            return string.Join("|", Seed, Affinity, Victory, EncountersCleared, Turns, RemainingHealth,
                TotalDamage, HealthDamageTaken, LargestCascade, MovesChosen, RewardsChosen, ActiveSkillsUsed,
                AffinitySkillsChosen, DominantBranches, string.Join(",", SelectedSkills));
        }
    }

    /// <summary>
    /// A deterministic, presentation-free player model. It uses the real application command boundary
    /// and previews legal swaps through the real board resolver on a cloned board/RNG snapshot.
    /// </summary>
    public sealed class BalanceRunSimulator
    {
        private const int MaximumCommands = 4000;
        private readonly BalanceAffinity _affinity;
        private readonly ContentId _gemId;
        private readonly string _branch;
        private readonly MvpProgressionContentCatalog _skills = MvpProgressionContentCatalog.Instance;

        public BalanceRunSimulator(BalanceAffinity affinity)
        {
            _affinity = affinity;
            _gemId = GemFor(affinity);
            _branch = BranchFor(affinity);
        }

        public BalanceRunResult Run(ulong seed)
        {
            var profile = FullyUnlockedProfile();
            var director = new RunDirector(new NullCheckpointStore(), new FixedProfileStore(profile));
            var result = new BalanceRunResult { Seed = seed, Affinity = _affinity };
            Record(Require(director.StartNewRun(seed), "start run").Events, result);

            var commands = 0;
            while (director.Screen != RunScreen.Victory && director.Screen != RunScreen.Defeat)
            {
                if (++commands > MaximumCommands)
                    throw new InvalidOperationException("Balance bot exceeded the command safety limit for seed " + seed + ".");

                if (director.Screen == RunScreen.Map)
                {
                    EquipBestActives(director, result);
                    PinNoRestVow(director, result);
                    var node = ChooseMapNode(director.State);
                    Record(Require(director.SelectMapNode(node.Id), "select map node").Events, result);
                }
                else if (director.Screen == RunScreen.Event || director.Screen == RunScreen.Rest)
                {
                    var choice = ChooseEventChoice(director.State);
                    Record(Require(director.SelectEventChoice(choice), "select event choice").Events, result);
                }
                else if (director.Screen == RunScreen.Reward)
                {
                    var reward = ChooseReward(director.State);
                    Record(Require(director.SelectReward(reward), "select reward").Events, result);
                }
                else if (director.Screen == RunScreen.Sanctum)
                {
                    EquipBestActives(director, result);
                    Record(Require(director.ContinueFromSanctum(), "continue from sanctum").Events, result);
                }
                else if (director.Screen == RunScreen.Encounter)
                {
                    UseReadySkills(director, result);
                    if (director.Screen != RunScreen.Encounter) continue;
                    var swap = ChooseSwap(director.State, result);
                    Record(Require(director.Swap(swap.CellA, swap.CellB), "resolve swap").Events, result);
                    result.MovesChosen++;
                }
                else if (director.Screen == RunScreen.SkillWindow)
                {
                    UseReadySkills(director, result);
                    if (director.Screen == RunScreen.SkillWindow)
                        Record(Require(director.ContinueTurn(), "complete enemy response").Events, result);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported balance simulation screen: " + director.Screen + ".");
                }
            }

            result.Victory = director.Screen == RunScreen.Victory;
            result.EncountersCleared = director.Statistics.EncountersCleared;
            result.Turns = director.State.ResolvedTurnCount;
            result.RemainingHealth = director.State.Player.Health;
            result.TotalDamage = director.Statistics.TotalDamage;
            result.HealthDamageTaken = director.Statistics.HealthDamageTaken;
            result.LargestCascade = director.Statistics.BiggestCascade;
            foreach (var statistic in director.Statistics.DamageBySource)
            {
                var branch = BranchForDamageSource(statistic.SourceId);
                if (branch == null) continue;
                Add(result.BranchDamage, branch, statistic.Amount);
            }
            var dominant = RunDirector.DominantDamageBranches(director.Statistics.DamageBySource);
            result.DominantBranches = string.Join(",", dominant.Select(id => id.Value));
            foreach (var skillId in director.State.SelectedSkillIds)
            {
                result.SelectedSkills.Add(skillId.Value);
                SkillDefinition skill;
                try { skill = _skills.GetSkill(skillId); }
                catch (KeyNotFoundException) { continue; }
                if (_branch.Length > 0 && string.Equals(skill.BranchTag, _branch, StringComparison.Ordinal))
                    result.AffinitySkillsChosen++;
            }
            return result;
        }

        private LegalSwap ChooseSwap(RunState state, BalanceRunResult result)
        {
            var legal = BoardSimulation.FindLegalSwaps(state.Board);
            if (legal.Count == 0) throw new InvalidOperationException("Stable board exposed no legal swap.");
            var best = legal[0];
            var bestScore = int.MinValue;
            foreach (var candidate in legal)
            {
                var previewState = CloneBoardContext(state);
                var preview = BoardSimulation.ResolveSwap(previewState, new ThreeInARow.Domain.Commands.SwapCommand
                {
                    CellA = candidate.CellA,
                    CellB = candidate.CellB
                });
                if (!preview.Accepted) throw new InvalidOperationException("FindLegalSwaps returned a rejected swap.");
                result.MovePreviews++;
                var score = ScorePreview(preview.Events, preview.CascadeCount, state.Player.Health);
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        private int ScorePreview(EventBatch events, int cascades, int health)
        {
            var score = cascades * 3;
            foreach (var item in events.Events)
            {
                if (item.Type == SimulationEventType.GemCleared)
                {
                    var cracked = Contains(item.StatusIds, BoardContentIds.Cracked);
                    var special = !item.RelatedId.Equals(BoardContentIds.NoSpecial);
                    if (!cracked && !special) score += 6;
                    if (_branch.Length > 0 && item.SourceId.Equals(_gemId)) score += cracked ? 2 : 36;
                    if (Contains(item.StatusIds, BoardContentIds.Thorned)) score -= health <= 10 ? 30 : 8;
                }
                else if (item.Type == SimulationEventType.SpecialActivated)
                {
                    score += 24;
                    if (SpecialMatchesAffinity(item.SourceId)) score += 48;
                }
                else if (item.Type == SimulationEventType.SpecialCreated)
                {
                    score += 18;
                    if (SpecialMatchesAffinity(item.SourceId)) score += 30;
                }
            }
            return score;
        }

        private void UseReadySkills(RunDirector director, BalanceRunResult result)
        {
            var equipped = new List<ContentId>(director.State.Player.EquippedActiveSkillIds);
            equipped.Sort((left, right) => ScoreSkill(director.State, right).CompareTo(ScoreSkill(director.State, left)));
            foreach (var skillId in equipped)
            {
                if (director.Screen != RunScreen.Encounter && director.Screen != RunScreen.SkillWindow) break;
                var cooldown = ProgressionRules.FindCooldown(director.State.Player, skillId);
                if (cooldown == null || cooldown.RemainingTurns > 0) continue;
                List<GridCell> targets;
                ContentId optionId;
                if (!TryBuildSkillCommand(director.State, _skills.GetSkill(skillId), out targets, out optionId)) continue;
                var action = director.UseSkill(skillId, targets, optionId);
                if (!action.Accepted)
                    throw new InvalidOperationException("Bot produced invalid command for " + skillId + ": " + action.Rejection + ".");
                Record(action.Events, result);
            }
        }

        private bool TryBuildSkillCommand(RunState state, SkillDefinition skill,
            out List<GridCell> targets, out ContentId optionId)
        {
            targets = new List<GridCell>();
            optionId = "content.none";
            if (skill.TargetPolicy == SkillTargetPolicy.None)
            {
                if (skill.ActiveEffects.Count > 0 && skill.ActiveEffects[0].Type == ActiveEffectType.CatalyzeResources &&
                    state.Player.Focus <= 0 && (state.Player.Toxic < 2 || state.Enemy.PoisonStacks >= 3)) return false;
                if (skill.ActiveEffects.Count > 0 && skill.ActiveEffects[0].Type == ActiveEffectType.GainShield &&
                    state.Player.Shield >= 12) return false;
                return true;
            }

            if (skill.TargetPolicy == SkillTargetPolicy.UpToThreeStatusGems)
            {
                foreach (var gem in state.Board.Gems)
                {
                    if (!HasCleanseableStatus(gem)) continue;
                    targets.Add(gem.Cell);
                    if (targets.Count == 3) break;
                }
                return targets.Count > 0;
            }

            if (skill.TargetPolicy == SkillTargetPolicy.OneMatchFourSpecial)
            {
                var special = state.Board.Gems.FirstOrDefault(gem => IsMovable(gem) && IsMatchFourSpecial(gem.SpecialId));
                if (special == null) return false;
                targets.Add(special.Cell);
                return true;
            }

            if (skill.TargetPolicy == SkillTargetPolicy.OneNormalGem ||
                skill.TargetPolicy == SkillTargetPolicy.OneNormalGemAndColor)
            {
                BoardGemState selected = null;
                if (_branch.Length > 0 && skill.TargetPolicy == SkillTargetPolicy.OneNormalGem)
                    selected = state.Board.Gems.FirstOrDefault(gem => IsMovableNormal(gem) && gem.GemId.Equals(_gemId));
                if (_branch.Length > 0 && skill.TargetPolicy == SkillTargetPolicy.OneNormalGemAndColor)
                    selected = state.Board.Gems.FirstOrDefault(gem => IsMovableNormal(gem) && !gem.GemId.Equals(_gemId));
                selected = selected ?? state.Board.Gems.FirstOrDefault(IsMovableNormal);
                if (selected == null) return false;
                targets.Add(selected.Cell);
                if (skill.TargetPolicy == SkillTargetPolicy.OneNormalGemAndColor)
                {
                    optionId = _branch.Length > 0 && !selected.GemId.Equals(_gemId)
                        ? _gemId
                        : FirstDifferentGem(selected.GemId);
                }
                return true;
            }

            if (skill.TargetPolicy == SkillTargetPolicy.UpToThreeNormalGems)
            {
                foreach (var gem in state.Board.Gems)
                {
                    if (!IsMovableNormal(gem)) continue;
                    if (_branch.Length > 0 && gem.GemId.Equals(_gemId)) continue;
                    targets.Add(gem.Cell);
                    if (targets.Count == 3) break;
                }
                if (targets.Count == 0)
                {
                    var fallback = state.Board.Gems.FirstOrDefault(IsMovableNormal);
                    if (fallback != null) targets.Add(fallback.Cell);
                }
                return targets.Count > 0;
            }
            return false;
        }

        private void EquipBestActives(RunDirector director, BalanceRunResult result)
        {
            var candidates = new List<ContentId>();
            foreach (var id in director.State.SelectedSkillIds)
            {
                SkillDefinition skill;
                try { skill = _skills.GetSkill(id); }
                catch (KeyNotFoundException) { continue; }
                if (skill.SlotType == SkillSlotType.Active) candidates.Add(id);
            }
            candidates.Sort((left, right) => ScoreSkill(director.State, right).CompareTo(ScoreSkill(director.State, left)));
            if (candidates.Count > 2) candidates.RemoveRange(2, candidates.Count - 2);

            for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                var candidate = candidates[candidateIndex];
                if (Contains(director.State.Player.EquippedActiveSkillIds, candidate)) continue;
                var replace = -1;
                for (var slot = 0; slot < director.State.Player.EquippedActiveSkillIds.Count; slot++)
                {
                    if (!Contains(candidates, director.State.Player.EquippedActiveSkillIds[slot]))
                    {
                        replace = slot;
                        break;
                    }
                }
                if (replace < 0) continue;
                Record(Require(director.EquipSkill(candidate, replace), "equip active skill").Events, result);
            }
        }

        private int ScoreSkill(RunState state, ContentId skillId)
        {
            var skill = _skills.GetSkill(skillId);
            var score = 10;
            if (_branch.Length > 0 && string.Equals(skill.BranchTag, _branch, StringComparison.Ordinal)) score += 500;
            if (skill.Id.Value.StartsWith("skill.evolution.", StringComparison.Ordinal)) score += 120;
            if (skill.RequiredBranchTags != null && _branch.Length > 0 && skill.RequiredBranchTags.Contains(_branch)) score += 220;
            if (skill.Id.Equals(ProgressionContentIds.Transmute)) score += _branch.Length > 0 ? 280 : 120;
            else if (skill.Id.Equals(ProgressionContentIds.Catalyze)) score += _affinity == BalanceAffinity.Tide || _affinity == BalanceAffinity.Venom ? 260 : 70;
            else if (skill.Id.Equals(ProgressionContentIds.Infuse)) score += 160;
            else if (skill.Id.Equals(ProgressionContentIds.Detonate)) score += 140;
            else if (skill.Id.Equals(ProgressionContentIds.Reweave)) score += 100;
            else if (skill.Id.Equals(ProgressionContentIds.Sunder)) score += 130;
            else if (skill.Id.Equals(ProgressionContentIds.Cleanse)) score += CountBoardStatuses(state.Board) * 25;
            else if (skill.Id.Equals(ProgressionContentIds.Aegis))
                score += 120 + (PlayerState.MaxHealth - state.Player.Health) * 15;
            if (skill.Id.Equals(ProgressionContentIds.PrismaticStart)) score += 180;
            if (skill.Id.Equals(ProgressionContentIds.RapidCasting)) score += 140;
            if (skill.Id.Equals(ProgressionContentIds.TemperedCore) || skill.Id.Equals(ProgressionContentIds.Briarheart))
                score += 100 + (PlayerState.MaxHealth - state.Player.Health) * 15;
            return score;
        }

        private ContentId ChooseReward(RunState state)
        {
            if (state.PendingChoice == null || !state.PendingChoice.IsPending)
                throw new InvalidOperationException("Reward screen has no offered rewards.");
            var best = state.PendingChoice.OptionIds[0];
            var bestScore = int.MinValue;
            foreach (var option in state.PendingChoice.OptionIds)
            {
                var score = ScoreSkill(state, option);
                if (score <= bestScore) continue;
                best = option;
                bestScore = score;
            }
            return best;
        }

        private static MapNodeState ChooseMapNode(RunState state)
        {
            MapNodeState best = null;
            var bestScore = int.MinValue;
            foreach (var node in state.Map.Nodes)
            {
                if (!MapSimulation.IsReachable(state.Map, node)) continue;
                var score = node.Type == MapNodeType.Boss ? 1000
                    : node.Type == MapNodeType.EliteCombat ? 180
                    : node.Type == MapNodeType.Event ? 145
                    : node.Type == MapNodeType.NormalCombat ? 110
                    : state.Player.Health <= 24 ? 220 : 50;
                if (score <= bestScore) continue;
                best = node;
                bestScore = score;
            }
            if (best == null) throw new InvalidOperationException("Map has no reachable node.");
            return best;
        }

        private static ContentId ChooseEventChoice(RunState state)
        {
            if (state.PendingEvent == null || !state.PendingEvent.IsPending)
                throw new InvalidOperationException("Event screen has no offered choices.");
            var best = state.PendingEvent.ChoiceIds[0];
            var bestScore = int.MinValue;
            foreach (var choiceId in state.PendingEvent.ChoiceIds)
            {
                var choice = MapContentCatalog.Instance.GetChoice(choiceId);
                var score = 0;
                foreach (var effect in choice.Effects)
                {
                    if (effect.Type == EventEffectType.HealPlayer)
                        score += Math.Min(effect.Amount, PlayerState.MaxHealth - state.Player.Health) * 9;
                    else if (effect.Type == EventEffectType.DamagePlayer)
                        score += state.Player.Health <= effect.Amount + 3 ? -10000 : -effect.Amount * 7;
                    else if (effect.Type == EventEffectType.OfferPassiveReward ||
                             effect.Type == EventEffectType.OfferActiveReward || effect.Type == EventEffectType.OfferAnyReward)
                        score += 130;
                    else if (effect.Type == EventEffectType.CreatePrism) score += 60;
                    else if (effect.Type == EventEffectType.CleanseBoard) score += CountBoardStatuses(state.Board) * 18;
                    else if (effect.Type == EventEffectType.SetEquippedCooldowns || effect.Type == EventEffectType.ReduceEquippedCooldowns)
                        score += 35;
                    else if (effect.Type == EventEffectType.AddPendingModifier && effect.ContentId.Equals(MapContentIds.NextShieldModifier))
                        score += effect.Amount * 5;
                    else if (effect.Type == EventEffectType.ApplyBoardStatus) score -= effect.Amount * 9;
                    else if (effect.Type == EventEffectType.ClearResources) score -= state.Player.Focus + state.Player.Toxic;
                }
                if (score <= bestScore) continue;
                best = choiceId;
                bestScore = score;
            }
            return best;
        }

        private static void PinNoRestVow(RunDirector director, BalanceRunResult result)
        {
            if (director.State.Map.FurthestVisitedRow >= 0 || director.State.RouteVow == null ||
                !Contains(director.State.RouteVow.OfferedIds, MapContentIds.VowNoRest)) return;
            Record(Require(director.PinRouteVow(MapContentIds.VowNoRest), "pin route vow").Events, result);
        }

        private static void Record(EventBatch events, BalanceRunResult result)
        {
            if (events == null) return;
            foreach (var item in events.Events)
            {
                if (item.Type == SimulationEventType.GemCleared) Add(result.GemClears, item.SourceId.Value, item.Amount);
                if (item.Type == SimulationEventType.SpecialActivated) Add(result.SpecialActivations, item.SourceId.Value, item.Amount);
                if (item.Type == SimulationEventType.MapNodeSelected) Add(result.RouteChoices, item.RelatedId.Value, 1);
                if (item.Type == SimulationEventType.EventChoiceSelected) Add(result.EventChoices, item.SourceId.Value, 1);
                if (item.Type == SimulationEventType.SkillChosen) result.RewardsChosen++;
                if (item.Type != SimulationEventType.SkillUsed) continue;
                result.ActiveSkillsUsed++;
                Add(result.SkillUses, item.SourceId.Value, 1);
            }
        }

        private bool SpecialMatchesAffinity(ContentId specialId)
        {
            return (_affinity == BalanceAffinity.Ember && specialId.Equals(BoardContentIds.Spark)) ||
                   (_affinity == BalanceAffinity.Tide && specialId.Equals(BoardContentIds.Current)) ||
                   (_affinity == BalanceAffinity.Venom && specialId.Equals(BoardContentIds.Spore)) ||
                   (_affinity == BalanceAffinity.Volt && specialId.Equals(BoardContentIds.Charge));
        }

        private static RunState CloneBoardContext(RunState source)
        {
            var clone = new RunState { Seed = source.Seed, Board = new BoardState(), RandomStreams = new List<RandomStreamState>() };
            foreach (var stream in source.RandomStreams)
                clone.RandomStreams.Add(new RandomStreamState { Stream = stream.Stream, State = stream.State });
            foreach (var gem in source.Board.Gems)
            {
                var copy = new BoardGemState
                {
                    Cell = gem.Cell,
                    GemId = gem.GemId,
                    SpecialId = gem.SpecialId,
                    StatusIds = new List<ContentId>(gem.StatusIds),
                    StatusDurations = new List<BoardStatusDurationState>()
                };
                foreach (var duration in gem.StatusDurations)
                    copy.StatusDurations.Add(new BoardStatusDurationState
                        { StatusId = duration.StatusId, RemainingPlayerTurns = duration.RemainingPlayerTurns });
                clone.Board.Gems.Add(copy);
            }
            return clone;
        }

        private static ProfileState FullyUnlockedProfile()
        {
            var profile = ProfileProgression.CreateFresh();
            profile.BestDifficultyUnlocked = 5;
            foreach (var challenge in ProfileContentCatalog.Instance.Challenges)
                if (!Contains(profile.UnlockedContentIds, challenge.UnlockContentId))
                    profile.UnlockedContentIds.Add(challenge.UnlockContentId);
            return profile;
        }

        private static bool IsMovable(BoardGemState gem)
        {
            return gem != null && !Contains(gem.StatusIds, BoardContentIds.Frozen) &&
                   !Contains(gem.StatusIds, BoardContentIds.Anchored);
        }

        private static bool IsMovableNormal(BoardGemState gem)
        {
            return IsMovable(gem) && MvpBoardContentCatalog.Instance.IsNormalGem(gem.GemId) &&
                   gem.SpecialId.Equals(BoardContentIds.NoSpecial);
        }

        private static bool HasCleanseableStatus(BoardGemState gem)
        {
            return gem != null && (Contains(gem.StatusIds, BoardContentIds.Frozen) ||
                Contains(gem.StatusIds, BoardContentIds.Cracked) || Contains(gem.StatusIds, BoardContentIds.Anchored) ||
                Contains(gem.StatusIds, BoardContentIds.Thorned));
        }

        private static bool IsMatchFourSpecial(ContentId id)
        {
            return id.Equals(BoardContentIds.Spark) || id.Equals(BoardContentIds.Current) ||
                   id.Equals(BoardContentIds.Spore) || id.Equals(BoardContentIds.Charge);
        }

        private static ContentId FirstDifferentGem(ContentId gem)
        {
            foreach (var candidate in new[] { BoardContentIds.Ember, BoardContentIds.Tide, BoardContentIds.Venom, BoardContentIds.Volt })
                if (!candidate.Equals(gem)) return candidate;
            return BoardContentIds.Ember;
        }

        private static int CountBoardStatuses(BoardState board)
        {
            if (board == null || board.Gems == null) return 0;
            var count = 0;
            foreach (var gem in board.Gems) if (HasCleanseableStatus(gem)) count++;
            return count;
        }

        private static ContentId GemFor(BalanceAffinity affinity)
        {
            if (affinity == BalanceAffinity.Ember) return BoardContentIds.Ember;
            if (affinity == BalanceAffinity.Tide) return BoardContentIds.Tide;
            if (affinity == BalanceAffinity.Venom) return BoardContentIds.Venom;
            if (affinity == BalanceAffinity.Volt) return BoardContentIds.Volt;
            return "gem.none";
        }

        private static string BranchFor(BalanceAffinity affinity)
        {
            if (affinity == BalanceAffinity.Ember) return "ember";
            if (affinity == BalanceAffinity.Tide) return "tide";
            if (affinity == BalanceAffinity.Venom) return "venom";
            if (affinity == BalanceAffinity.Volt) return "volt";
            return string.Empty;
        }

        private static string BranchForDamageSource(string sourceId)
        {
            if (sourceId == "gem.ember" || sourceId == "special.spark" || sourceId == "skill.scalding_current") return "ember";
            if (sourceId == "gem.tide" || sourceId == "special.current") return "tide";
            if (sourceId == "gem.venom" || sourceId == "special.spore" || sourceId == "status.poison") return "venom";
            if (sourceId == "gem.volt" || sourceId == "special.charge") return "volt";
            return null;
        }

        private static bool Contains(IEnumerable<ContentId> ids, ContentId expected)
        {
            if (ids == null) return false;
            foreach (var id in ids) if (id.Equals(expected)) return true;
            return false;
        }

        private static void Add(Dictionary<string, int> totals, string key, int amount)
        {
            int current;
            totals.TryGetValue(key, out current);
            totals[key] = current + amount;
        }

        private static RunActionResult Require(RunActionResult action, string operation)
        {
            if (!action.Accepted) throw new InvalidOperationException("Could not " + operation + ": " + action.Rejection + ".");
            return action;
        }

        private sealed class NullCheckpointStore : ICheckpointStore
        {
            public bool HasCheckpoint => false;
            public void Save(CheckpointSnapshot snapshot) { }
            public bool TryLoad(out CheckpointSnapshot snapshot) { snapshot = null; return false; }
            public void Clear() { }
        }

        private sealed class FixedProfileStore : IProfileStore
        {
            private ProfileState _profile;
            public FixedProfileStore(ProfileState profile) { _profile = profile; }
            public ProfileState LoadOrCreate() { return _profile; }
            public void Save(ProfileState profile) { _profile = profile; }
        }
    }

    public static class BalanceReportWriter
    {
        public static string Write(BalanceAffinity affinity, IReadOnlyList<BalanceRunResult> runs)
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "Library", "BalanceReports");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, affinity.ToString().ToLowerInvariant() + ".md");
            var victories = runs.Count(run => run.Victory);
            var sb = new StringBuilder();
            sb.AppendLine("# Balance simulation: " + affinity);
            sb.AppendLine();
            sb.AppendLine("Generated by the explicit local domain-only balance suite.");
            sb.AppendLine();
            var affinityBranch = affinity == BalanceAffinity.Opportunist
                ? string.Empty
                : "branch." + affinity.ToString().ToLowerInvariant();
            var affinityLeads = affinityBranch.Length == 0 ? 0 : runs.Count(run => run.Victory &&
                run.DominantBranches.Split(',').Contains(affinityBranch));
            sb.AppendLine("| Runs | Win rate | Affinity lead in wins | Avg encounters | Avg turns | Turns/encounter | Avg health | Avg damage dealt | Avg health damage taken | Avg previews/move |");
            sb.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            sb.AppendLine("| " + runs.Count + " | " + Percent(victories, runs.Count) + " | " +
                (affinityBranch.Length == 0 ? "n/a" : Percent(affinityLeads, victories)) + " | " +
                Average(runs, run => run.EncountersCleared) + " | " + Average(runs, run => run.Turns) + " | " +
                Ratio(runs.Sum(run => run.Turns), runs.Sum(run => run.EncountersCleared)) + " | " +
                Average(runs, run => run.RemainingHealth) + " | " + Average(runs, run => run.TotalDamage) + " | " +
                Average(runs, run => run.HealthDamageTaken) + " | " +
                Ratio(runs.Sum(run => run.MovePreviews), runs.Sum(run => run.MovesChosen)) + " |");
            sb.AppendLine();
            AppendFrequency(sb, "Dominant branches", runs.Select(run => string.IsNullOrEmpty(run.DominantBranches) ? "none" : run.DominantBranches));
            AppendTotals(sb, "Damage by branch", runs.SelectMany(run => run.BranchDamage));
            AppendTotals(sb, "Gem clears", runs.SelectMany(run => run.GemClears));
            AppendTotals(sb, "Special activations", runs.SelectMany(run => run.SpecialActivations));
            AppendTotals(sb, "Active skill uses", runs.SelectMany(run => run.SkillUses));
            AppendFrequency(sb, "Selected skills", runs.SelectMany(run => run.SelectedSkills));
            AppendTotals(sb, "Chosen route content", runs.SelectMany(run => run.RouteChoices));
            AppendTotals(sb, "Event choices", runs.SelectMany(run => run.EventChoices));
            AppendFrequency(sb, "Losses after encounters", runs.Where(run => !run.Victory)
                .Select(run => run.EncountersCleared.ToString(CultureInfo.InvariantCulture)));
            sb.AppendLine("## Runs");
            sb.AppendLine();
            sb.AppendLine("| Seed | Result | Encounters | Turns | HP | Damage dealt | Health damage taken | Cascade | Rewards | Skill uses | Affinity picks | Dominant |");
            sb.AppendLine("| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var run in runs)
                sb.AppendLine("| " + run.Seed + " | " + (run.Victory ? "win" : "loss") + " | " + run.EncountersCleared +
                    " | " + run.Turns + " | " + run.RemainingHealth + " | " + run.TotalDamage + " | " +
                    run.HealthDamageTaken + " | " + run.LargestCascade + " | " + run.RewardsChosen + " | " + run.ActiveSkillsUsed + " | " +
                    run.AffinitySkillsChosen + " | " + (string.IsNullOrEmpty(run.DominantBranches) ? "none" : run.DominantBranches) + " |");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static void AppendTotals(StringBuilder sb, string heading, IEnumerable<KeyValuePair<string, int>> values)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in values)
            {
                int current;
                totals.TryGetValue(pair.Key, out current);
                totals[pair.Key] = current + pair.Value;
            }
            sb.AppendLine("## " + heading);
            sb.AppendLine();
            sb.AppendLine("| Item | Total |");
            sb.AppendLine("| --- | ---: |");
            foreach (var pair in totals.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal))
                sb.AppendLine("| " + pair.Key + " | " + pair.Value + " |");
            sb.AppendLine();
        }

        private static void AppendFrequency(StringBuilder sb, string heading, IEnumerable<string> values)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                int current;
                totals.TryGetValue(value, out current);
                totals[value] = current + 1;
            }
            AppendTotals(sb, heading, totals);
        }

        private static string Average(IEnumerable<BalanceRunResult> runs, Func<BalanceRunResult, int> selector)
        {
            return runs.Average(selector).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Percent(int numerator, int denominator)
        {
            return (denominator == 0 ? 0d : numerator * 100d / denominator).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string Ratio(int numerator, int denominator)
        {
            return (denominator == 0 ? 0d : (double)numerator / denominator).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
