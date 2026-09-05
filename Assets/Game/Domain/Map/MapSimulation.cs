using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Map
{
    public enum MapSelectionRejectionReason
    {
        None,
        NodeNotFound,
        NodeAlreadyVisited,
        NodeNotReachable,
        CurrentNodeIncomplete
    }

    public enum EventChoiceRejectionReason
    {
        None,
        NoPendingEvent,
        ChoiceNotOffered
    }

    public sealed class RouteActionResult
    {
        public readonly bool Accepted;
        public readonly string Rejection;
        public readonly EventBatch Events;

        private RouteActionResult(bool accepted, string rejection, EventBatch events)
        {
            Accepted = accepted;
            Rejection = rejection ?? string.Empty;
            Events = events ?? new EventBatch();
        }

        public static RouteActionResult Accept(EventBatch events)
        {
            return new RouteActionResult(true, string.Empty, events);
        }

        public static RouteActionResult Reject(object reason)
        {
            return new RouteActionResult(false, reason == null ? "Rejected" : reason.ToString(), new EventBatch());
        }
    }

    /// <summary>Owns deterministic map generation, reachability, event outcomes, and pending encounter modifiers.</summary>
    public static class MapSimulation
    {
        private static readonly MapNodeType[][] RowTypes =
        {
            new[] { MapNodeType.NormalCombat },
            new[] { MapNodeType.NormalCombat, MapNodeType.Event },
            new[] { MapNodeType.NormalCombat },
            new[] { MapNodeType.NormalCombat, MapNodeType.EliteCombat, MapNodeType.Rest },
            new[] { MapNodeType.Event, MapNodeType.Rest },
            new[] { MapNodeType.NormalCombat },
            new[] { MapNodeType.Boss }
        };

        public static EventBatch Generate(RunState state, ICombatContentCatalog combatCatalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            combatCatalog = combatCatalog ?? MvpCombatContentCatalog.Instance;
            if (state.RandomStreams == null || state.RandomStreams.Count == 0)
                state.RandomStreams = RandomStreams.Create(state.Seed);

            var mapRandom = RandomStreams.Restore(RandomStream.MapGeneration, state.RandomStreams);
            var encounterRandom = RandomStreams.Restore(RandomStream.EncounterSelection, state.RandomStreams);
            var map = new MapState();
            map.BossEnemyId = mapRandom.NextInt(2) == 0
                ? CombatContentIds.CrystalWarden
                : CombatContentIds.FacetEngine;

            var eventsPool = new List<ContentId>
            {
                MapContentIds.FacetedAltar, MapContentIds.QuietPool, MapContentIds.StaticLoom,
                MapContentIds.PrismEcho, MapContentIds.FrozenReliquary, MapContentIds.CrackedCache
            };
            var usedEnemies = new List<ContentId>();
            var usedPressures = new List<ContentId>();
            state.SelectedEncounterIds = new List<ContentId>();

            for (var row = 0; row < RowTypes.Length; row++)
            {
                for (var column = 0; column < RowTypes[row].Length; column++)
                {
                    var type = RowTypes[row][column];
                    var node = new MapNodeState
                    {
                        Id = (ContentId)("map.node.r" + row + ".c" + column),
                        Row = row,
                        Column = column,
                        Type = type
                    };

                    if (type == MapNodeType.NormalCombat)
                    {
                        var tuningDepth = row == 0 ? 1 : row == 1 ? 2 : row == 2 ? 3 : 4;
                        var encounter = PickEncounter(combatCatalog.GetNormalPool(tuningDepth), encounterRandom, usedEnemies, usedPressures);
                        node.ContentId = encounter.Id;
                        node.PressureId = encounter.Enemy.DominantPressureId;
                        usedEnemies.Add(encounter.Enemy.Id);
                        usedPressures.Add(encounter.Enemy.DominantPressureId);
                        state.SelectedEncounterIds.Add(encounter.Id);
                    }
                    else if (type == MapNodeType.EliteCombat)
                    {
                        var elitePool = combatCatalog.EliteEncounters;
                        var encounter = elitePool[encounterRandom.NextInt(elitePool.Count)];
                        node.ContentId = encounter.Id;
                        node.PressureId = encounter.Enemy.DominantPressureId;
                        state.SelectedEncounterIds.Add(encounter.Id);
                    }
                    else if (type == MapNodeType.Event)
                    {
                        var eventIndex = mapRandom.NextInt(eventsPool.Count);
                        node.ContentId = eventsPool[eventIndex];
                        eventsPool.RemoveAt(eventIndex);
                    }
                    else if (type == MapNodeType.Rest)
                    {
                        node.ContentId = MapContentIds.RestSite;
                    }
                    else
                    {
                        var boss = combatCatalog.GetBossEncounter(map.BossEnemyId);
                        node.ContentId = boss.Id;
                        node.PressureId = boss.Enemy.DominantPressureId;
                        state.SelectedEncounterIds.Add(boss.Id);
                    }
                    map.Nodes.Add(node);
                }
            }

            for (var row = 0; row < RowTypes.Length - 1; row++)
            {
                var current = FindRow(map, row);
                var next = FindRow(map, row + 1);
                foreach (var node in current)
                    foreach (var nextNode in next) node.ConnectionIds.Add(nextNode.Id);
            }

            state.Map = map;
            state.CurrentEncounterId = "encounter.none";
            state.PendingEvent = new PendingEventState();
            RandomStreams.Store(RandomStream.MapGeneration, mapRandom, state.RandomStreams);
            RandomStreams.Store(RandomStream.EncounterSelection, encounterRandom, state.RandomStreams);

            var events = new EventBatch();
            events.Add(SimulationEventType.MapGenerated, MapContentIds.SystemMap,
                "rows=7;boss=" + map.BossEnemyId, map.Nodes.Count, null, null, map.BossEnemyId);
            return events;
        }

        public static RouteActionResult SelectNode(RunState state, SelectMapNodeCommand command)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            var node = GetNode(state.Map, command.NodeId);
            if (node == null) return RouteActionResult.Reject(MapSelectionRejectionReason.NodeNotFound);
            if (node.Visited) return RouteActionResult.Reject(MapSelectionRejectionReason.NodeAlreadyVisited);

            var current = GetNode(state.Map, state.Map.CurrentNodeId);
            if (current != null && !current.Completed)
                return RouteActionResult.Reject(MapSelectionRejectionReason.CurrentNodeIncomplete);
            if (!IsReachable(state.Map, node))
                return RouteActionResult.Reject(MapSelectionRejectionReason.NodeNotReachable);

            node.Visited = true;
            state.Map.CurrentNodeId = node.Id;
            state.Map.FurthestVisitedRow = Math.Max(state.Map.FurthestVisitedRow, node.Row);
            var events = new EventBatch();
            events.Add(SimulationEventType.MapNodeSelected, node.Id,
                "row=" + node.Row + ";type=" + node.Type, node.Row, null, null, node.ContentId);
            return RouteActionResult.Accept(events);
        }

        public static EventBatch BeginNonCombatNode(RunState state, MapContentCatalog catalog = null)
        {
            catalog = catalog ?? MapContentCatalog.Instance;
            var node = GetCurrentNode(state);
            if (node == null || (node.Type != MapNodeType.Event && node.Type != MapNodeType.Rest))
                throw new InvalidOperationException("The current node is not an event or rest node.");
            var definition = catalog.GetEvent(node.ContentId);
            state.PendingEvent = new PendingEventState { EventId = definition.Id };
            foreach (var choice in definition.Choices) state.PendingEvent.ChoiceIds.Add(choice.Id);
            return new EventBatch();
        }

        public static RouteActionResult SelectEventChoice(
            RunState state,
            SelectEventChoiceCommand command,
            MapContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            catalog = catalog ?? MapContentCatalog.Instance;
            if (state.PendingEvent == null || !state.PendingEvent.IsPending)
                return RouteActionResult.Reject(EventChoiceRejectionReason.NoPendingEvent);
            if (!Contains(state.PendingEvent.ChoiceIds, command.ChoiceId))
                return RouteActionResult.Reject(EventChoiceRejectionReason.ChoiceNotOffered);

            var definition = catalog.GetChoice(command.ChoiceId);
            var events = new EventBatch();
            events.Add(SimulationEventType.EventChoiceSelected, command.ChoiceId,
                "event=" + state.PendingEvent.EventId, 1, null, null, state.PendingEvent.EventId);
            foreach (var effect in definition.Effects) ApplyEffect(state, command.ChoiceId, effect, events);
            state.PendingEvent = new PendingEventState();
            CompleteCurrentNode(state, events);
            if (state.Player.Health <= 0)
                events.Add(SimulationEventType.RunEnded, command.ChoiceId, "defeat", 0);
            return RouteActionResult.Accept(events);
        }

        public static void CompleteCurrentNode(RunState state, EventBatch events)
        {
            var node = GetCurrentNode(state);
            if (node == null || node.Completed) return;
            node.Completed = true;
            if (events != null)
                events.Add(SimulationEventType.MapNodeCompleted, node.Id,
                    "row=" + node.Row + ";type=" + node.Type, node.Row, null, null, node.ContentId);
        }

        public static EventBatch ApplyPendingEncounterModifiers(RunState state)
        {
            var events = new EventBatch();
            if (state.PendingEncounterModifiers == null)
                state.PendingEncounterModifiers = new List<PendingEncounterModifierState>();
            foreach (var modifier in state.PendingEncounterModifiers)
            {
                if (modifier == null) continue;
                if (modifier.Id.Equals(MapContentIds.NextCracked))
                    ApplyRandomStatus(state, MapContentIds.SystemMap, BoardContentIds.Cracked, modifier.Amount, events);
                else if (modifier.Id.Equals(MapContentIds.NextShieldModifier))
                    ChangeShield(state, MapContentIds.SystemMap, modifier.Amount, "pending_encounter", events);
            }
            state.PendingEncounterModifiers.Clear();

            var prismStart = ProgressionRules.GetModifier(state, PassiveModifierType.PrismaticStart);
            if (prismStart > 0) CreatePrism(state, ProgressionContentIds.PrismaticStart, events);
            return events;
        }

        public static MapNodeState GetCurrentNode(RunState state)
        {
            return state == null ? null : GetNode(state.Map, state.Map == null ? default(ContentId) : state.Map.CurrentNodeId);
        }

        public static MapNodeState GetNode(MapState map, ContentId id)
        {
            if (map == null || map.Nodes == null) return null;
            foreach (var node in map.Nodes)
                if (node != null && node.Id.Equals(id)) return node;
            return null;
        }

        public static bool IsReachable(MapState map, MapNodeState node)
        {
            if (map == null || node == null || node.Visited) return false;
            var current = GetNode(map, map.CurrentNodeId);
            if (current == null) return node.Row == 0;
            return current.Completed && Contains(current.ConnectionIds, node.Id);
        }

        private static EncounterDefinition PickEncounter(
            IReadOnlyList<EncounterDefinition> pool,
            DeterministicRandom random,
            List<ContentId> usedEnemies,
            List<ContentId> usedPressures)
        {
            var eligible = new List<EncounterDefinition>();
            foreach (var encounter in pool)
                if (!Contains(usedEnemies, encounter.Enemy.Id) &&
                    Count(usedPressures, encounter.Enemy.DominantPressureId) < 2) eligible.Add(encounter);
            if (eligible.Count == 0)
                foreach (var encounter in pool)
                    if (Count(usedPressures, encounter.Enemy.DominantPressureId) < 2) eligible.Add(encounter);
            if (eligible.Count == 0) foreach (var encounter in pool) eligible.Add(encounter);
            return eligible[random.NextInt(eligible.Count)];
        }

        private static List<MapNodeState> FindRow(MapState map, int row)
        {
            var result = new List<MapNodeState>();
            foreach (var node in map.Nodes) if (node.Row == row) result.Add(node);
            return result;
        }

        private static void ApplyEffect(
            RunState state,
            ContentId sourceId,
            EventEffectDefinition effect,
            EventBatch events)
        {
            if (effect.Type == EventEffectType.DamagePlayer)
            {
                var damage = Math.Min(state.Player.Health, effect.Amount);
                state.Player.Health -= damage;
                events.Add(SimulationEventType.DamageApplied, sourceId, "target=player;event", damage);
            }
            else if (effect.Type == EventEffectType.HealPlayer)
            {
                var healed = Math.Min(effect.Amount, PlayerState.MaxHealth - state.Player.Health);
                state.Player.Health += healed;
                if (healed > 0) events.Add(SimulationEventType.ResourceChanged, sourceId,
                    "resource=health;reason=event;current=" + state.Player.Health, healed);
            }
            else if (effect.Type == EventEffectType.ClearResources)
            {
                ChangeResourceToZero(state, sourceId, true, events);
                ChangeResourceToZero(state, sourceId, false, events);
            }
            else if (effect.Type == EventEffectType.SetEquippedCooldowns)
                SetEquippedCooldowns(state, sourceId, 0, events);
            else if (effect.Type == EventEffectType.ReduceEquippedCooldowns)
                ReduceEquippedCooldowns(state, sourceId, effect.Amount, events);
            else if (effect.Type == EventEffectType.ApplyBoardStatus)
                ApplyRandomStatus(state, sourceId, effect.ContentId, effect.Amount, events);
            else if (effect.Type == EventEffectType.CreatePrism)
                CreatePrism(state, sourceId, events);
            else if (effect.Type == EventEffectType.CleanseBoard)
                CleanseBoard(state, sourceId, events);
            else if (effect.Type == EventEffectType.OfferPassiveReward)
                ProgressionSimulation.OfferEventReward(state, sourceId, SkillSlotType.Passive, effect.Amount, events);
            else if (effect.Type == EventEffectType.OfferActiveReward)
                ProgressionSimulation.OfferEventReward(state, sourceId, SkillSlotType.Active, effect.Amount, events);
            else if (effect.Type == EventEffectType.OfferAnyReward)
                ProgressionSimulation.OfferEventReward(state, sourceId, null, effect.Amount, events);
            else if (effect.Type == EventEffectType.AddPendingModifier)
            {
                if (state.PendingEncounterModifiers == null)
                    state.PendingEncounterModifiers = new List<PendingEncounterModifierState>();
                state.PendingEncounterModifiers.Add(new PendingEncounterModifierState
                    { Id = effect.ContentId, Amount = effect.Amount });
                events.Add(SimulationEventType.PendingModifierAdded, effect.ContentId,
                    "amount=" + effect.Amount, effect.Amount, null, null, sourceId);
            }
        }

        private static void ChangeResourceToZero(RunState state, ContentId sourceId, bool focus, EventBatch events)
        {
            var amount = focus ? state.Player.Focus : state.Player.Toxic;
            if (amount <= 0) return;
            if (focus) state.Player.Focus = 0; else state.Player.Toxic = 0;
            events.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=" + (focus ? "focus" : "toxic") + ";reason=event;current=0", -amount);
        }

        private static void SetEquippedCooldowns(RunState state, ContentId sourceId, int value, EventBatch events)
        {
            foreach (var skillId in state.Player.EquippedActiveSkillIds)
            {
                var cooldown = ProgressionRules.FindCooldown(state.Player, skillId);
                if (cooldown == null || cooldown.RemainingTurns == value) continue;
                var delta = value - cooldown.RemainingTurns;
                cooldown.RemainingTurns = value;
                events.Add(SimulationEventType.CooldownChanged, skillId,
                    "reason=event;current=" + value, delta, null, null, sourceId);
            }
        }

        private static void ReduceEquippedCooldowns(RunState state, ContentId sourceId, int amount, EventBatch events)
        {
            foreach (var skillId in state.Player.EquippedActiveSkillIds)
            {
                var cooldown = ProgressionRules.FindCooldown(state.Player, skillId);
                if (cooldown == null) continue;
                var before = cooldown.RemainingTurns;
                cooldown.RemainingTurns = Math.Max(0, before - amount);
                if (before != cooldown.RemainingTurns)
                    events.Add(SimulationEventType.CooldownChanged, skillId,
                        "reason=rest;current=" + cooldown.RemainingTurns,
                        cooldown.RemainingTurns - before, null, null, sourceId);
            }
        }

        private static void ApplyRandomStatus(
            RunState state,
            ContentId sourceId,
            ContentId statusId,
            int count,
            EventBatch events)
        {
            var eligible = new List<BoardGemState>();
            foreach (var gem in state.Board.Gems)
                if (gem != null && !Contains(gem.StatusIds, statusId)) eligible.Add(gem);
            var random = RandomStreams.Restore(RandomStream.IntentVariation, state.RandomStreams);
            count = Math.Min(count, eligible.Count);
            for (var index = 0; index < count; index++)
            {
                var chosen = random.NextInt(eligible.Count);
                var gem = eligible[chosen];
                eligible.RemoveAt(chosen);
                if (gem.StatusIds == null) gem.StatusIds = new List<ContentId>();
                gem.StatusIds.Add(statusId);
                events.Add(SimulationEventType.StatusAdded, statusId, "event", 1, gem.Cell, null, sourceId);
            }
            RandomStreams.Store(RandomStream.IntentVariation, random, state.RandomStreams);
            events.Append(BoardSimulation.EnsurePlayable(state));
        }

        private static void CleanseBoard(RunState state, ContentId sourceId, EventBatch events)
        {
            foreach (var gem in state.Board.Gems)
            {
                if (gem == null || gem.StatusIds == null) continue;
                foreach (var statusId in new List<ContentId>(gem.StatusIds))
                    events.Add(SimulationEventType.StatusRemoved, statusId, "reason=event_cleanse", 1,
                        gem.Cell, null, sourceId);
                gem.StatusIds.Clear();
                if (gem.StatusDurations != null) gem.StatusDurations.Clear();
            }
            events.Append(BoardSimulation.EnsurePlayable(state));
        }

        private static void CreatePrism(RunState state, ContentId sourceId, EventBatch events)
        {
            var eligible = new List<BoardGemState>();
            foreach (var gem in state.Board.Gems)
                if (gem != null && MvpBoardContentCatalog.Instance.IsNormalGem(gem.GemId) &&
                    gem.SpecialId.Equals(BoardContentIds.NoSpecial) && !Contains(gem.StatusIds, BoardContentIds.Anchored))
                    eligible.Add(gem);
            if (eligible.Count == 0) return;
            eligible.Sort((left, right) => left.Cell.CompareTo(right.Cell));
            var random = RandomStreams.Restore(RandomStream.MapGeneration, state.RandomStreams);
            var gemState = eligible[random.NextInt(eligible.Count)];
            RandomStreams.Store(RandomStream.MapGeneration, random, state.RandomStreams);
            gemState.GemId = BoardContentIds.PrismGem;
            gemState.SpecialId = BoardContentIds.Prism;
            events.Add(SimulationEventType.SpecialCreated, BoardContentIds.Prism,
                "event", 1, gemState.Cell, null, sourceId);
            events.Append(BoardSimulation.EnsurePlayable(state));
        }

        private static void ChangeShield(RunState state, ContentId sourceId, int amount, string reason, EventBatch events)
        {
            if (amount <= 0) return;
            state.Player.Shield += amount;
            events.Add(SimulationEventType.ResourceChanged, sourceId,
                "resource=shield;reason=" + reason + ";current=" + state.Player.Shield, amount);
        }

        private static bool Contains(IEnumerable<ContentId> values, ContentId wanted)
        {
            if (values == null) return false;
            foreach (var value in values) if (value.Equals(wanted)) return true;
            return false;
        }

        private static int Count(IEnumerable<ContentId> values, ContentId wanted)
        {
            var count = 0;
            if (values == null) return count;
            foreach (var value in values) if (value.Equals(wanted)) count++;
            return count;
        }
    }
}
