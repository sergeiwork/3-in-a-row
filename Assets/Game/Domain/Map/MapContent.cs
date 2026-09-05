using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Ids;

namespace ThreeInARow.Domain.Map
{
    public static class MapContentIds
    {
        public static readonly ContentId SystemMap = "system.map";

        public static readonly ContentId FacetedAltar = "event.faceted_altar";
        public static readonly ContentId QuietPool = "event.quiet_pool";
        public static readonly ContentId StaticLoom = "event.static_loom";
        public static readonly ContentId PrismEcho = "event.prism_echo";
        public static readonly ContentId FrozenReliquary = "event.frozen_reliquary";
        public static readonly ContentId CrackedCache = "event.cracked_cache";
        public static readonly ContentId RestSite = "event.rest_site";

        public static readonly ContentId DraftPassive = "choice.faceted_altar.draft_passive";
        public static readonly ContentId LeaveAltar = "choice.faceted_altar.leave";
        public static readonly ContentId HealPool = "choice.quiet_pool.heal";
        public static readonly ContentId LeavePool = "choice.quiet_pool.leave";
        public static readonly ContentId ReadyActives = "choice.static_loom.ready";
        public static readonly ContentId LeaveLoom = "choice.static_loom.leave";
        public static readonly ContentId CreatePrism = "choice.prism_echo.create_prism";
        public static readonly ContentId HealEcho = "choice.prism_echo.heal";
        public static readonly ContentId DraftActive = "choice.frozen_reliquary.draft_active";
        public static readonly ContentId CleanseBoard = "choice.frozen_reliquary.cleanse";
        public static readonly ContentId DraftTwo = "choice.cracked_cache.draft";
        public static readonly ContentId NextShield = "choice.cracked_cache.shield";
        public static readonly ContentId RestHeal = "choice.rest.heal";
        public static readonly ContentId RestRepair = "choice.rest.repair";

        public static readonly ContentId NextCracked = "modifier.next_encounter.cracked";
        public static readonly ContentId NextShieldModifier = "modifier.next_encounter.shield";

        public static readonly ContentId PressureCrack = "pressure.crack";
        public static readonly ContentId PressureFreeze = "pressure.freeze";
        public static readonly ContentId PressureAnchor = "pressure.anchor";
        public static readonly ContentId PressureDrain = "pressure.drain";
        public static readonly ContentId PressureMixed = "pressure.mixed";
    }

    public enum EventEffectType
    {
        DamagePlayer,
        HealPlayer,
        ClearResources,
        SetEquippedCooldowns,
        ApplyBoardStatus,
        CreatePrism,
        CleanseBoard,
        OfferPassiveReward,
        OfferActiveReward,
        OfferAnyReward,
        AddPendingModifier,
        ReduceEquippedCooldowns
    }

    public sealed class EventEffectDefinition
    {
        public readonly EventEffectType Type;
        public readonly int Amount;
        public readonly ContentId ContentId;

        public EventEffectDefinition(EventEffectType type, int amount = 0, ContentId? contentId = null)
        {
            Type = type;
            Amount = amount;
            ContentId = contentId ?? (ContentId)"content.none";
        }
    }

    public sealed class EventChoiceDefinition
    {
        public readonly ContentId Id;
        public readonly string DescriptionKey;
        public readonly IReadOnlyList<EventEffectDefinition> Effects;

        public EventChoiceDefinition(ContentId id, string descriptionKey, params EventEffectDefinition[] effects)
        {
            Id = id;
            DescriptionKey = descriptionKey ?? string.Empty;
            Effects = effects ?? new EventEffectDefinition[0];
        }
    }

    public sealed class EventDefinition
    {
        public readonly ContentId Id;
        public readonly string DisplayKey;
        public readonly IReadOnlyList<EventChoiceDefinition> Choices;

        public EventDefinition(ContentId id, string displayKey, params EventChoiceDefinition[] choices)
        {
            Id = id;
            DisplayKey = displayKey ?? string.Empty;
            Choices = choices ?? throw new ArgumentNullException(nameof(choices));
        }
    }

    public sealed class MapContentCatalog
    {
        private readonly List<EventDefinition> _events;
        private readonly Dictionary<ContentId, EventDefinition> _eventsById;
        private readonly Dictionary<ContentId, EventChoiceDefinition> _choicesById;

        public static readonly MapContentCatalog Instance = new MapContentCatalog();

        private MapContentCatalog()
        {
            _events = new List<EventDefinition>
            {
                Event(MapContentIds.FacetedAltar,
                    Choice(MapContentIds.DraftPassive,
                        new EventEffectDefinition(EventEffectType.DamagePlayer, 8),
                        new EventEffectDefinition(EventEffectType.OfferPassiveReward, 3)),
                    Choice(MapContentIds.LeaveAltar)),
                Event(MapContentIds.QuietPool,
                    Choice(MapContentIds.HealPool,
                        new EventEffectDefinition(EventEffectType.HealPlayer, 10),
                        new EventEffectDefinition(EventEffectType.ClearResources)),
                    Choice(MapContentIds.LeavePool)),
                Event(MapContentIds.StaticLoom,
                    Choice(MapContentIds.ReadyActives,
                        new EventEffectDefinition(EventEffectType.SetEquippedCooldowns),
                        new EventEffectDefinition(EventEffectType.ApplyBoardStatus, 4, BoardContentIds.Cracked)),
                    Choice(MapContentIds.LeaveLoom)),
                Event(MapContentIds.PrismEcho,
                    Choice(MapContentIds.CreatePrism,
                        new EventEffectDefinition(EventEffectType.CreatePrism, 1),
                        new EventEffectDefinition(EventEffectType.DamagePlayer, 5)),
                    Choice(MapContentIds.HealEcho, new EventEffectDefinition(EventEffectType.HealPlayer, 5))),
                Event(MapContentIds.FrozenReliquary,
                    Choice(MapContentIds.DraftActive,
                        new EventEffectDefinition(EventEffectType.OfferActiveReward, 3),
                        new EventEffectDefinition(EventEffectType.ApplyBoardStatus, 3, BoardContentIds.Frozen)),
                    Choice(MapContentIds.CleanseBoard, new EventEffectDefinition(EventEffectType.CleanseBoard))),
                Event(MapContentIds.CrackedCache,
                    Choice(MapContentIds.DraftTwo,
                        new EventEffectDefinition(EventEffectType.OfferAnyReward, 2),
                        new EventEffectDefinition(EventEffectType.AddPendingModifier, 3, MapContentIds.NextCracked)),
                    Choice(MapContentIds.NextShield,
                        new EventEffectDefinition(EventEffectType.AddPendingModifier, 6, MapContentIds.NextShieldModifier))),
                new EventDefinition(MapContentIds.RestSite, "event.rest_site.name",
                    Choice(MapContentIds.RestHeal, new EventEffectDefinition(EventEffectType.HealPlayer, 12)),
                    Choice(MapContentIds.RestRepair,
                        new EventEffectDefinition(EventEffectType.CleanseBoard),
                        new EventEffectDefinition(EventEffectType.ReduceEquippedCooldowns, 2)))
            };

            _eventsById = new Dictionary<ContentId, EventDefinition>();
            _choicesById = new Dictionary<ContentId, EventChoiceDefinition>();
            foreach (var definition in _events)
            {
                _eventsById.Add(definition.Id, definition);
                foreach (var choice in definition.Choices) _choicesById.Add(choice.Id, choice);
            }
        }

        public IReadOnlyList<EventDefinition> Events => _events;

        public EventDefinition GetEvent(ContentId id)
        {
            EventDefinition definition;
            if (!_eventsById.TryGetValue(id, out definition))
                throw new KeyNotFoundException("Unknown event content ID: " + id);
            return definition;
        }

        public EventChoiceDefinition GetChoice(ContentId id)
        {
            EventChoiceDefinition definition;
            if (!_choicesById.TryGetValue(id, out definition))
                throw new KeyNotFoundException("Unknown event choice ID: " + id);
            return definition;
        }

        private static EventDefinition Event(ContentId id, params EventChoiceDefinition[] choices)
        {
            return new EventDefinition(id, id.Value + ".name", choices);
        }

        private static EventChoiceDefinition Choice(ContentId id, params EventEffectDefinition[] effects)
        {
            return new EventChoiceDefinition(id, id.Value + ".description", effects);
        }
    }
}
