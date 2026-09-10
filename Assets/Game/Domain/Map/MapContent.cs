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
        public static readonly ContentId PrismaticArchive = "event.prismatic_archive";
        public static readonly ContentId EmberOrchard = "event.cinder.ember_orchard";
        public static readonly ContentId AshenNursery = "event.cinder.ashen_nursery";
        public static readonly ContentId StagTrail = "event.cinder.stag_trail";
        public static readonly ContentId RootspeakerShrine = "event.cinder.rootspeaker_shrine";
        public static readonly ContentId MirrorWell = "event.void.mirror_well";
        public static readonly ContentId NullObservatory = "event.void.null_observatory";
        public static readonly ContentId EchoPrison = "event.void.echo_prison";
        public static readonly ContentId BrokenConstellation = "event.void.broken_constellation";

        public static readonly ContentId CinderSeedFlag = "story.cinder.seed";
        public static readonly ContentId CapturedEchoFlag = "story.void.echo";

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
        public static readonly ContentId StudyArchive = "choice.prismatic_archive.study";
        public static readonly ContentId LeaveArchive = "choice.prismatic_archive.leave";
        public static readonly ContentId TakeCinderSeed = "choice.cinder.ember_orchard.seed";
        public static readonly ContentId RestInOrchard = "choice.cinder.ember_orchard.rest";
        public static readonly ContentId RaidNursery = "choice.cinder.ashen_nursery.raid";
        public static readonly ContentId SootheNursery = "choice.cinder.ashen_nursery.soothe";
        public static readonly ContentId FollowStag = "choice.cinder.stag_trail.follow";
        public static readonly ContentId AvoidStag = "choice.cinder.stag_trail.avoid";
        public static readonly ContentId AwakenRootspeaker = "choice.cinder.rootspeaker_shrine.awaken";
        public static readonly ContentId ListenRootspeaker = "choice.cinder.rootspeaker_shrine.listen";
        public static readonly ContentId DrinkMirror = "choice.void.mirror_well.drink";
        public static readonly ContentId ShatterMirror = "choice.void.mirror_well.shatter";
        public static readonly ContentId CaptureEcho = "choice.void.null_observatory.capture";
        public static readonly ContentId LeaveObservatory = "choice.void.null_observatory.leave";
        public static readonly ContentId FreeEcho = "choice.void.echo_prison.free";
        public static readonly ContentId DrainEcho = "choice.void.echo_prison.drain";
        public static readonly ContentId AlignConstellation = "choice.void.broken_constellation.align";
        public static readonly ContentId ScatterConstellation = "choice.void.broken_constellation.scatter";

        public static readonly ContentId NextCracked = "modifier.next_encounter.cracked";
        public static readonly ContentId NextShieldModifier = "modifier.next_encounter.shield";

        public static readonly ContentId PressureCrack = "pressure.crack";
        public static readonly ContentId PressureFreeze = "pressure.freeze";
        public static readonly ContentId PressureAnchor = "pressure.anchor";
        public static readonly ContentId PressureDrain = "pressure.drain";
        public static readonly ContentId PressureMixed = "pressure.mixed";

        public static readonly ContentId VowEliteHunter = "vow.elite_hunter";
        public static readonly ContentId VowNoRest = "vow.no_rest";
        public static readonly ContentId VowEventSeeker = "vow.event_seeker";
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
        ReduceEquippedCooldowns,
        AddStoryFlag
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
        public readonly ContentId RequiredStoryFlagId;

        public EventChoiceDefinition(ContentId id, string descriptionKey, ContentId requiredStoryFlagId,
            params EventEffectDefinition[] effects)
        {
            Id = id;
            DescriptionKey = descriptionKey ?? string.Empty;
            Effects = effects ?? new EventEffectDefinition[0];
            RequiredStoryFlagId = requiredStoryFlagId;
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
                        new EventEffectDefinition(EventEffectType.ReduceEquippedCooldowns, 2))),
                Event(MapContentIds.PrismaticArchive,
                    Choice(MapContentIds.StudyArchive,
                        new EventEffectDefinition(EventEffectType.DamagePlayer, 6),
                        new EventEffectDefinition(EventEffectType.OfferAnyReward, 3)),
                    Choice(MapContentIds.LeaveArchive,
                        new EventEffectDefinition(EventEffectType.HealPlayer, 3))),
                Event(MapContentIds.EmberOrchard,
                    Choice(MapContentIds.TakeCinderSeed,
                        new EventEffectDefinition(EventEffectType.AddStoryFlag, 1, MapContentIds.CinderSeedFlag),
                        new EventEffectDefinition(EventEffectType.CreatePrism, 1),
                        new EventEffectDefinition(EventEffectType.ApplyBoardStatus, 2, BoardContentIds.Thorned)),
                    Choice(MapContentIds.RestInOrchard, new EventEffectDefinition(EventEffectType.HealPlayer, 7))),
                Event(MapContentIds.AshenNursery,
                    Choice(MapContentIds.RaidNursery,
                        new EventEffectDefinition(EventEffectType.DamagePlayer, 6),
                        new EventEffectDefinition(EventEffectType.OfferAnyReward, 2)),
                    Choice(MapContentIds.SootheNursery,
                        new EventEffectDefinition(EventEffectType.CleanseBoard))),
                Event(MapContentIds.StagTrail,
                    Choice(MapContentIds.FollowStag,
                        new EventEffectDefinition(EventEffectType.AddPendingModifier, 12, MapContentIds.NextShieldModifier)),
                    Choice(MapContentIds.AvoidStag,
                        new EventEffectDefinition(EventEffectType.HealPlayer, 5))),
                Event(MapContentIds.RootspeakerShrine,
                    ConditionalChoice(MapContentIds.AwakenRootspeaker, MapContentIds.CinderSeedFlag,
                        new EventEffectDefinition(EventEffectType.OfferPassiveReward, 3),
                        new EventEffectDefinition(EventEffectType.HealPlayer, 4)),
                    Choice(MapContentIds.ListenRootspeaker,
                        new EventEffectDefinition(EventEffectType.ReduceEquippedCooldowns, 2))),
                Event(MapContentIds.MirrorWell,
                    Choice(MapContentIds.DrinkMirror,
                        new EventEffectDefinition(EventEffectType.HealPlayer, 8),
                        new EventEffectDefinition(EventEffectType.ClearResources)),
                    Choice(MapContentIds.ShatterMirror,
                        new EventEffectDefinition(EventEffectType.CreatePrism, 1),
                        new EventEffectDefinition(EventEffectType.DamagePlayer, 4))),
                Event(MapContentIds.NullObservatory,
                    Choice(MapContentIds.CaptureEcho,
                        new EventEffectDefinition(EventEffectType.AddStoryFlag, 1, MapContentIds.CapturedEchoFlag),
                        new EventEffectDefinition(EventEffectType.SetEquippedCooldowns),
                        new EventEffectDefinition(EventEffectType.ApplyBoardStatus, 2, BoardContentIds.Frozen)),
                    Choice(MapContentIds.LeaveObservatory)),
                Event(MapContentIds.EchoPrison,
                    ConditionalChoice(MapContentIds.FreeEcho, MapContentIds.CapturedEchoFlag,
                        new EventEffectDefinition(EventEffectType.OfferActiveReward, 3),
                        new EventEffectDefinition(EventEffectType.CleanseBoard)),
                    Choice(MapContentIds.DrainEcho,
                        new EventEffectDefinition(EventEffectType.AddPendingModifier, 10, MapContentIds.NextShieldModifier))),
                Event(MapContentIds.BrokenConstellation,
                    Choice(MapContentIds.AlignConstellation,
                        new EventEffectDefinition(EventEffectType.CreatePrism, 1),
                        new EventEffectDefinition(EventEffectType.ApplyBoardStatus, 3, BoardContentIds.Cracked)),
                    Choice(MapContentIds.ScatterConstellation,
                        new EventEffectDefinition(EventEffectType.OfferAnyReward, 2),
                        new EventEffectDefinition(EventEffectType.DamagePlayer, 5)))
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
            return new EventChoiceDefinition(id, id.Value + ".description", "story.none", effects);
        }

        private static EventChoiceDefinition ConditionalChoice(ContentId id, ContentId requiredStoryFlagId,
            params EventEffectDefinition[] effects)
        {
            return new EventChoiceDefinition(id, id.Value + ".description", requiredStoryFlagId, effects);
        }
    }

    public enum RouteVowCondition
    {
        DefeatElite,
        VisitNoRest,
        VisitBothEvents
    }

    public sealed class RouteVowDefinition
    {
        public readonly ContentId Id;
        public readonly RouteVowCondition Condition;

        public RouteVowDefinition(ContentId id, RouteVowCondition condition)
        {
            Id = id;
            Condition = condition;
        }
    }

    public static class RouteVowContent
    {
        public static readonly RouteVowDefinition EliteHunter =
            new RouteVowDefinition(MapContentIds.VowEliteHunter, RouteVowCondition.DefeatElite);
        public static readonly RouteVowDefinition NoRest =
            new RouteVowDefinition(MapContentIds.VowNoRest, RouteVowCondition.VisitNoRest);
        public static readonly RouteVowDefinition EventSeeker =
            new RouteVowDefinition(MapContentIds.VowEventSeeker, RouteVowCondition.VisitBothEvents);

        public static RouteVowDefinition Get(ContentId id)
        {
            if (id.Equals(EliteHunter.Id)) return EliteHunter;
            if (id.Equals(NoRest.Id)) return NoRest;
            if (id.Equals(EventSeeker.Id)) return EventSeeker;
            throw new KeyNotFoundException("Unknown route vow ID: " + id);
        }

        public static IReadOnlyList<ContentId> ForRegion(int regionIndex)
        {
            if (regionIndex == 0) return new[] { EliteHunter.Id, NoRest.Id };
            if (regionIndex == 1) return new[] { NoRest.Id, EventSeeker.Id };
            return new[] { EliteHunter.Id, EventSeeker.Id };
        }
    }
}
