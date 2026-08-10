using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.WorldLocations
{
    public static class PrototypeLocationRouteDefinitionFactory
    {
        public const string WalkingModeDefinitionId = "travel-mode-definition.prototype.walking";
        public const string RunningModeDefinitionId = "travel-mode-definition.prototype.running";
        public const string CartModeDefinitionId = "travel-mode-definition.prototype.cart";

        public const string StreetSegmentDefinitionId = "route-segment-definition.prototype.street";
        public const string RoadSegmentDefinitionId = "route-segment-definition.prototype.road";
        public const string TrailSegmentDefinitionId = "route-segment-definition.prototype.trail";
        public const string CorridorSegmentDefinitionId = "route-segment-definition.prototype.corridor";
        public const string DungeonRouteSegmentDefinitionId = "route-segment-definition.prototype.dungeon-route";
        public const string BridgeSegmentDefinitionId = "route-segment-definition.prototype.bridge";

        public const string VillageStreetNetworkId = "route-network.prototype.village-streets";
        public const string RegionalTrailNetworkId = "route-network.prototype.regional-trails";

        public const string VillageMarketStreetSegmentId = "route-segment.prototype.village-market-street";
        public const string VillageWildernessTrailSegmentId = "route-segment.prototype.village-wilderness-trail";
        public const string MarketGuildStreetSegmentId = "route-segment.prototype.market-guild-street";

        public static DefinitionRegistry AddMissingPrototypeRouteDefinitions(DefinitionRegistry baseRegistry)
        {
            HashSet<string> ids = new HashSet<string>(baseRegistry?.DefinitionsById.Keys ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            if (baseRegistry != null)
            {
                definitions.AddRange(baseRegistry.DefinitionsById.Values.Where(definition => definition != null));
            }

            definitions.AddRange(CreateMissingTravelModeDefinitions(ids));
            definitions.AddRange(CreateMissingRouteSegmentDefinitions(ids));
            return new DefinitionRegistry(definitions);
        }

        public static IReadOnlyList<TravelModeDefinition> CreateMissingTravelModeDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = new HashSet<string>(existingIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<TravelModeDefinition> definitions = new List<TravelModeDefinition>();
            RouteSegmentCategory[] walkingCategories =
            {
                RouteSegmentCategory.Road,
                RouteSegmentCategory.Street,
                RouteSegmentCategory.Path,
                RouteSegmentCategory.Trail,
                RouteSegmentCategory.Corridor,
                RouteSegmentCategory.Bridge,
                RouteSegmentCategory.Tunnel,
                RouteSegmentCategory.StairRoute,
                RouteSegmentCategory.WildernessRoute,
                RouteSegmentCategory.DungeonRoute,
                RouteSegmentCategory.RegionalRoad,
                RouteSegmentCategory.TradeRoad,
                RouteSegmentCategory.MountainPass,
                RouteSegmentCategory.RiverCrossingPlaceholder,
                RouteSegmentCategory.FerryPlaceholder,
                RouteSegmentCategory.PortalRoutePlaceholder,
                RouteSegmentCategory.Custom
            };

            AddMode(definitions, ids, WalkingModeDefinitionId, "Prototype Walking", TravelModeCategory.Walking, walkingCategories, 1d, 1d);
            AddMode(definitions, ids, RunningModeDefinitionId, "Prototype Running", TravelModeCategory.RunningPlaceholder, walkingCategories, 1d, 0.75d, capabilities: new[] { "capability.prototype.movement.run" });
            AddMode(definitions, ids, CartModeDefinitionId, "Prototype Cart Travel", TravelModeCategory.CartPlaceholder, new[] { RouteSegmentCategory.Road, RouteSegmentCategory.Street, RouteSegmentCategory.RegionalRoad, RouteSegmentCategory.TradeRoad, RouteSegmentCategory.Bridge }, 1d, 1.2d, equipment: new[] { "item.prototype-cart" });
            return definitions;
        }

        public static IReadOnlyList<RouteSegmentDefinition> CreateMissingRouteSegmentDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> ids = new HashSet<string>(existingIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<RouteSegmentDefinition> definitions = new List<RouteSegmentDefinition>();
            AddSegment(definitions, ids, StreetSegmentDefinitionId, "Prototype Street", RouteSegmentCategory.Street, 60d, 60d, new[] { WalkingModeDefinitionId, RunningModeDefinitionId, CartModeDefinitionId }, hidden: false);
            AddSegment(definitions, ids, RoadSegmentDefinitionId, "Prototype Road", RouteSegmentCategory.Road, 100d, 100d, new[] { WalkingModeDefinitionId, RunningModeDefinitionId, CartModeDefinitionId }, hidden: false);
            AddSegment(definitions, ids, TrailSegmentDefinitionId, "Prototype Trail", RouteSegmentCategory.Trail, 120d, 150d, new[] { WalkingModeDefinitionId, RunningModeDefinitionId });
            AddSegment(definitions, ids, CorridorSegmentDefinitionId, "Prototype Corridor Route", RouteSegmentCategory.Corridor, 8d, 8d, new[] { WalkingModeDefinitionId, RunningModeDefinitionId });
            AddSegment(definitions, ids, DungeonRouteSegmentDefinitionId, "Prototype Dungeon Route", RouteSegmentCategory.DungeonRoute, 20d, 30d, new[] { WalkingModeDefinitionId }, visibility: RouteVisibility.Secret);
            AddSegment(definitions, ids, BridgeSegmentDefinitionId, "Prototype Bridge Route", RouteSegmentCategory.Bridge, 20d, 20d, new[] { WalkingModeDefinitionId, RunningModeDefinitionId, CartModeDefinitionId }, hidden: false);
            return definitions;
        }

        public static void SeedPrototypeRoutes(LocationRouteRuntime runtime, DefinitionRegistry registry, LocationRuntime locations, LocationConnectionRuntime connections, string worldId)
        {
            if (runtime == null)
            {
                return;
            }

            string world = string.IsNullOrWhiteSpace(worldId) ? PersistenceService.LocalWorldId : worldId.Trim();
            runtime.Configure(registry, locations, connections, world);
            SeedSegment(runtime, VillageMarketStreetSegmentId, StreetSegmentDefinitionId, "Village Market Street", "location.prototype.village", "location.prototype.market-district", 70d, 70d, RouteVisibility.Public);
            SeedSegment(runtime, MarketGuildStreetSegmentId, StreetSegmentDefinitionId, "Market to Guild Street", "location.prototype.market-district", "location.prototype.adventurers-guild", 85d, 85d, RouteVisibility.Public);
            SeedSegment(runtime, VillageWildernessTrailSegmentId, TrailSegmentDefinitionId, "Village Wilderness Trail", "location.prototype.village", "location.prototype.wilderness-ring", 140d, 170d, RouteVisibility.LocallyKnown);
            runtime.CreateNetwork(new LocationRouteNetworkCreateRequest
            {
                transactionId = $"prototype.seed.{VillageStreetNetworkId}",
                networkId = VillageStreetNetworkId,
                displayName = "Prototype Village Street Network",
                category = RouteNetworkCategory.StreetNetwork,
                segmentIds = new[] { VillageMarketStreetSegmentId, MarketGuildStreetSegmentId },
                visibility = RouteVisibility.Public
            });
            runtime.CreateNetwork(new LocationRouteNetworkCreateRequest
            {
                transactionId = $"prototype.seed.{RegionalTrailNetworkId}",
                networkId = RegionalTrailNetworkId,
                displayName = "Prototype Regional Trail Network",
                category = RouteNetworkCategory.TrailNetwork,
                segmentIds = new[] { VillageWildernessTrailSegmentId },
                visibility = RouteVisibility.LocallyKnown
            });
        }

        private static void SeedSegment(LocationRouteRuntime runtime, string id, string definitionId, string display, string source, string destination, double distance, double cost, RouteVisibility visibility)
        {
            runtime.CreateSegment(new LocationRouteSegmentCreateRequest
            {
                transactionId = $"prototype.seed.{id}",
                segmentId = id,
                segmentDefinitionId = definitionId,
                displayName = display,
                sourceLocationId = source,
                destinationLocationId = destination,
                directionality = LocationConnectionDirectionality.Bidirectional,
                distanceMeters = distance,
                baseCostUnits = cost,
                supportedTravelModeDefinitionIds = new[] { WalkingModeDefinitionId, RunningModeDefinitionId },
                visibility = visibility,
                worldTime = 0d,
                sourceEventId = "event.prototype.world-setup",
                provenanceId = "prototype.location-route.seed"
            });
        }

        private static void AddMode(ICollection<TravelModeDefinition> definitions, ISet<string> ids, string id, string display, TravelModeCategory category, IEnumerable<RouteSegmentCategory> routeCategories, double distanceScale, double costScale, IEnumerable<string> capabilities = null, IEnumerable<string> equipment = null)
        {
            if (ids.Contains(id)) return;
            TravelModeDefinition definition = ScriptableObject.CreateInstance<TravelModeDefinition>();
            definition.name = display;
            definition.DevelopmentConfigure(id, display, category, routeCategories, distanceScale, costScale, capabilities, equipment);
            definitions.Add(definition);
            ids.Add(id);
        }

        private static void AddSegment(ICollection<RouteSegmentDefinition> definitions, ISet<string> ids, string id, string display, RouteSegmentCategory category, double distance, double cost, IEnumerable<string> modes, bool hidden = true, RouteVisibility visibility = RouteVisibility.Public)
        {
            if (ids.Contains(id)) return;
            RouteSegmentDefinition definition = ScriptableObject.CreateInstance<RouteSegmentDefinition>();
            definition.name = display;
            definition.DevelopmentConfigure(id, display, category, LocationConnectionDirectionality.Bidirectional, distance, cost, modes, accessPolicies: true, networkMembership: true, sceneBinding: true, hidden: hidden, visibility: visibility);
            definitions.Add(definition);
            ids.Add(id);
        }
    }
}
