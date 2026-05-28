namespace Astrion.Network
{
    /// Single source of truth for scene name -> server zone ID mapping.
    /// MonsterNetworkManager and ReconnectSystem both used to carry their
    /// own copy of this switch; the v1 worldmap added 22 zones and keeping
    /// them in sync by hand was a foot-gun. Update both sides here.
    public static class SceneZoneMap
    {
        public static string SceneToZone(string sceneName)
        {
            switch (sceneName)
            {
                // Existing
                case "MainScene":             return "beacon_of_winds";
                case "ForgottenWoodsScene":   return "forgotten_woods";
                case "CitadelOfDawnScene":    return "citadel_of_dawn";

                // v1 worldmap — hub cities
                case "SolariaScene":          return "solaria";
                case "PyresummitScene":       return "pyresummit";
                case "VerdaglenScene":        return "verdaglen";
                case "NightportScene":        return "nightport";
                case "TidehavenScene":        return "tidehaven";

                // Solaria tree
                case "SolariaOutskirtsScene": return "solaria_outskirts";
                case "SunlitPlainsScene":     return "sunlit_plains";
                case "WheatFieldsScene":      return "wheat_fields";
                case "PinewoodTrailScene":    return "pinewood_trail";

                // Pyresummit tree
                case "CinderRidgeScene":      return "cinder_ridge";
                case "AshfallCliffsScene":    return "ashfall_cliffs";
                case "MagmaHollowScene":      return "magma_hollow";

                // Verdaglen tree (forgotten_woods is the terminal node above)
                case "MossgladeScene":        return "mossglade";
                case "WhisperingBoughsScene": return "whispering_boughs";
                case "OldRootsScene":         return "old_roots";

                // Nightport tree
                case "BackalleysScene":       return "backalleys";
                case "SewerTunnelsScene":     return "sewer_tunnels";
                case "UndergroundVaultScene": return "underground_vault";

                // Tidehaven tree (citadel_of_dawn is the terminal node above)
                case "TideDocksScene":        return "tide_docks";
                case "DriftwoodBeachScene":   return "driftwood_beach";
                case "SunkenReefScene":       return "sunken_reef";

                default: return "";
            }
        }

        /// Reverse lookup. Used by teleport-style travel (e.g. the astral
        /// compass): given a server zone id, return the Unity scene name to
        /// load. Keep this in lockstep with SceneToZone above.
        public static string ZoneToScene(string zoneId)
        {
            switch (zoneId)
            {
                case "beacon_of_winds":   return "MainScene";
                case "forgotten_woods":   return "ForgottenWoodsScene";
                case "citadel_of_dawn":   return "CitadelOfDawnScene";

                case "solaria":           return "SolariaScene";
                case "pyresummit":        return "PyresummitScene";
                case "verdaglen":         return "VerdaglenScene";
                case "nightport":         return "NightportScene";
                case "tidehaven":         return "TidehavenScene";

                case "solaria_outskirts": return "SolariaOutskirtsScene";
                case "sunlit_plains":     return "SunlitPlainsScene";
                case "wheat_fields":      return "WheatFieldsScene";
                case "pinewood_trail":    return "PinewoodTrailScene";

                case "cinder_ridge":      return "CinderRidgeScene";
                case "ashfall_cliffs":    return "AshfallCliffsScene";
                case "magma_hollow":      return "MagmaHollowScene";

                case "mossglade":         return "MossgladeScene";
                case "whispering_boughs": return "WhisperingBoughsScene";
                case "old_roots":         return "OldRootsScene";

                case "backalleys":        return "BackalleysScene";
                case "sewer_tunnels":     return "SewerTunnelsScene";
                case "underground_vault": return "UndergroundVaultScene";

                case "tide_docks":        return "TideDocksScene";
                case "driftwood_beach":   return "DriftwoodBeachScene";
                case "sunken_reef":       return "SunkenReefScene";

                default: return "";
            }
        }
    }
}
