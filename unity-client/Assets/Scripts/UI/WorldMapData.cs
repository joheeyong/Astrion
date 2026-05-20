using UnityEngine;

namespace Astrion.UI
{
    /// Static layout for the in-game world map overlay. Mirrors docs/WORLDMAP.md
    /// — keep edits in sync. Pixel coordinates are panel-relative; the panel
    /// itself is sized in WorldMapUI so the numbers below can stay readable.
    public static class WorldMapData
    {
        public struct Node
        {
            public string id;       // server zone id (matches SceneZoneMap)
            public string display;  // human-readable label on the map
            public Vector2 pos;     // panel-relative position in pixels
            public int    minLv, maxLv;
            public bool   isCity;
        }

        // Three horizontal lanes spread under the central Solaria hub.
        //   lane TOP    Solaria → Outskirts → Plains → Fields → Trail → Pyresummit → Cinder → Ashfall → Magma
        //   lane MID    Verdaglen → Mossglade → Whispering → OldRoots → ForgottenWoods
        //   lane BOTTOM Nightport → Backalleys → Sewer → UndergroundVault
        //   lane TIDE   (Nightport→) Tidehaven → TideDocks → DriftwoodBeach → SunkenReef → CitadelOfDawn
        public static readonly Node[] Nodes = new[] {
            // Origin
            new Node { id="beacon_of_winds",   display="Beacon of Winds",  pos=new Vector2(-380,   0), minLv=1,  maxLv=10, isCity=true },
            new Node { id="solaria",           display="Solaria",          pos=new Vector2(-280,   0), minLv=10, maxLv=20, isCity=true },

            // Top lane — Solaria tree → Pyresummit branch
            new Node { id="solaria_outskirts", display="Outskirts",        pos=new Vector2(-180,  90), minLv=10, maxLv=12, isCity=false },
            new Node { id="sunlit_plains",     display="Sunlit Plains",    pos=new Vector2( -90,  90), minLv=12, maxLv=15, isCity=false },
            new Node { id="wheat_fields",      display="Wheat Fields",     pos=new Vector2(   0,  90), minLv=15, maxLv=18, isCity=false },
            new Node { id="pinewood_trail",    display="Pinewood Trail",   pos=new Vector2(  90,  90), minLv=18, maxLv=22, isCity=false },
            new Node { id="pyresummit",        display="Pyresummit",       pos=new Vector2( 180,  90), minLv=20, maxLv=35, isCity=true },
            new Node { id="cinder_ridge",      display="Cinder Ridge",     pos=new Vector2( 260, 140), minLv=20, maxLv=25, isCity=false },
            new Node { id="ashfall_cliffs",    display="Ashfall Cliffs",   pos=new Vector2( 340, 140), minLv=25, maxLv=32, isCity=false },
            new Node { id="magma_hollow",      display="Magma Hollow",     pos=new Vector2( 420, 140), minLv=32, maxLv=40, isCity=false },

            // Middle lane — Verdaglen branch (forgotten_woods terminal)
            new Node { id="verdaglen",         display="Verdaglen",        pos=new Vector2(-180,   0), minLv=15, maxLv=30, isCity=true },
            new Node { id="mossglade",         display="Mossglade",        pos=new Vector2( -90,   0), minLv=15, maxLv=20, isCity=false },
            new Node { id="whispering_boughs", display="Whispering Boughs",pos=new Vector2(   0,   0), minLv=20, maxLv=25, isCity=false },
            new Node { id="old_roots",         display="Old Roots",        pos=new Vector2(  90,   0), minLv=25, maxLv=32, isCity=false },
            new Node { id="forgotten_woods",   display="Forgotten Woods",  pos=new Vector2( 180,   0), minLv=30, maxLv=38, isCity=false },

            // Bottom lane — Nightport branch
            new Node { id="nightport",         display="Nightport",        pos=new Vector2(-180, -90), minLv=25, maxLv=40, isCity=true },
            new Node { id="backalleys",        display="Backalleys",       pos=new Vector2( -90, -90), minLv=25, maxLv=28, isCity=false },
            new Node { id="sewer_tunnels",     display="Sewer Tunnels",    pos=new Vector2(   0, -90), minLv=28, maxLv=35, isCity=false },
            new Node { id="underground_vault", display="Underground Vault",pos=new Vector2(  90, -90), minLv=35, maxLv=45, isCity=false },

            // Tidehaven branch (ferries off Nightport, → Citadel terminal)
            new Node { id="tidehaven",         display="Tidehaven",        pos=new Vector2(-180,-170), minLv=30, maxLv=45, isCity=true },
            new Node { id="tide_docks",        display="Tide Docks",       pos=new Vector2( -90,-170), minLv=30, maxLv=33, isCity=false },
            new Node { id="driftwood_beach",   display="Driftwood Beach",  pos=new Vector2(   0,-170), minLv=33, maxLv=38, isCity=false },
            new Node { id="sunken_reef",       display="Sunken Reef",      pos=new Vector2(  90,-170), minLv=38, maxLv=45, isCity=false },
            new Node { id="citadel_of_dawn",   display="Citadel of Dawn",  pos=new Vector2( 180,-170), minLv=45, maxLv=99, isCity=true },
        };

        /// (from, to) — bidirectional in-game, but a single record on the map.
        public static readonly (string, string)[] Edges = new[] {
            // origin
            ("beacon_of_winds",   "solaria"),
            // Solaria→Pyresummit lane
            ("solaria",           "solaria_outskirts"),
            ("solaria_outskirts", "sunlit_plains"),
            ("sunlit_plains",     "wheat_fields"),
            ("wheat_fields",      "pinewood_trail"),
            ("pinewood_trail",    "pyresummit"),
            ("pyresummit",        "cinder_ridge"),
            ("cinder_ridge",      "ashfall_cliffs"),
            ("ashfall_cliffs",    "magma_hollow"),
            // Solaria→Verdaglen lane
            ("solaria",           "verdaglen"),
            ("verdaglen",         "mossglade"),
            ("mossglade",         "whispering_boughs"),
            ("whispering_boughs", "old_roots"),
            ("old_roots",         "forgotten_woods"),
            // Solaria→Nightport lane
            ("solaria",           "nightport"),
            ("nightport",         "backalleys"),
            ("backalleys",        "sewer_tunnels"),
            ("sewer_tunnels",     "underground_vault"),
            // Nightport→Tidehaven (ferry) lane
            ("nightport",         "tidehaven"),
            ("tidehaven",         "tide_docks"),
            ("tide_docks",        "driftwood_beach"),
            ("driftwood_beach",   "sunken_reef"),
            ("sunken_reef",       "citadel_of_dawn"),
        };

        public static int NodeIndex(string id)
        {
            for (int i = 0; i < Nodes.Length; i++)
                if (Nodes[i].id == id) return i;
            return -1;
        }
    }
}
