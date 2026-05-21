# ASTRION World Map v1

Concept hybrid — MapleStory v1 (2003–2005) **structure** (tutorial island
+ five hub cities, each gating a tree of hunting grounds, leveling 1→50)
with **original naming and lore** so we're not riding directly on Maple's
visual identity.

Total v1 footprint: **6 hub zones + 18 hunting zones + 1 end-game zone = 25 zones**.
That's small next to Maple v1's ~100 maps, but it's the minimum that
supports a complete Lv 1→50 leveling arc with class-thematic regions, and
lets us cleanly add more later without retconning the world.

## World shape

```
                              [ Beacon of Winds ]   Lv 1–10  (tutorial island)
                                       │
                                       │  ship
                                       ▼
                               [ Solaria ]          Lv 10–20  (sun-drenched plains city)
                                ├ solaria_outskirts            Lv 10–12  snails
                                ├ sunlit_plains                Lv 12–15  slimes
                                ├ wheat_fields                 Lv 15–18  mushrooms
                                └ pinewood_trail               Lv 18–22  wolves
                                       │
            ┌──────────────────────────┼─────────────────────────────┐
            │ north                    │ west                        │ south
            ▼                          ▼                             ▼
    [ Pyresummit ]            [ Verdaglen ]                  [ Nightport ]
       Lv 20–35                  Lv 15–30                       Lv 25–40
    (volcanic mountain,       (forest grove,                 (rooftop city,
     warrior-themed)           mage-themed)                   thief-themed)
        ├ cinder_ridge           ├ mossglade                     ├ backalleys
        ├ ashfall_cliffs         ├ whispering_boughs             ├ sewer_tunnels
        └ magma_hollow           ├ old_roots                     └ underground_vault
                                 └ forgotten_woods*                       │
                                   (existing — Shadow Hulk boss)          │  ferry
                                                                          ▼
                                                                  [ Tidehaven ]       Lv 30–45
                                                                  (port, archer-themed)
                                                                     ├ tide_docks
                                                                     ├ driftwood_beach
                                                                     └ sunken_reef
                                                                          │
                                                                          ▼
                                                                  [ Citadel of Dawn ]* Lv 45+
                                                                  (existing — end-game dungeon)
```

* = zone already implemented before v1 worldmap effort.

## Cities — naming & inspiration

| City             | Maple analogue   | Concept                                          | Class flavour |
|------------------|------------------|--------------------------------------------------|---------------|
| Beacon of Winds  | Maple Island     | starter island, gentle slopes, snails            | all           |
| Solaria          | Henesys          | central plains city, sunlit, fountain in plaza   | bow / archer  |
| Pyresummit       | Perion           | mountain crag, lava cracks, weapon-smith city    | warrior       |
| Verdaglen        | Ellinia          | giant treetop city, glowing leaves, library      | mage          |
| Nightport        | Kerning City     | densely-built urban rooftops, neon, alley vibes  | thief         |
| Tidehaven        | Lith Harbor      | wooden boardwalks, fishing piers, lighthouse     | archer (fish) |
| Citadel of Dawn  | Sleepywood/El Nath| crumbling temple ruins, hard mobs, group content | endgame       |

Each hub city is a "safe zone": no aggressive monsters, contains an inn /
shop / class-specific NPC / portal to its hunting trail.

## Hunting zone catalog

Stats below are server-side starters; tune from `/metrics` once players
are killing. HP / EXP rough scaling: HP ≈ Lv × 8, EXP ≈ Lv × 2.

### Beacon of Winds (existing — keep)
- snails, slimes (low HP, no aggro)

### Solaria tree (Lv 10–22)

| Zone               | Lv    | Mobs                       | HP   | EXP |
|--------------------|-------|----------------------------|------|-----|
| solaria_outskirts  | 10–12 | snail                      | 30   | 8   |
| sunlit_plains      | 12–15 | slime                      | 60   | 14  |
| wheat_fields       | 15–18 | mushroom                   | 100  | 22  |
| pinewood_trail     | 18–22 | wolf                       | 140  | 32  |

### Pyresummit tree (Lv 20–40)

| Zone               | Lv    | Mobs                       | HP   | EXP |
|--------------------|-------|----------------------------|------|-----|
| cinder_ridge       | 20–25 | fire_imp                   | 180  | 42  |
| ashfall_cliffs     | 25–32 | gargoyle                   | 280  | 60  |
| magma_hollow       | 32–40 | lava_slime + magma_golem   | 420  | 95  |

### Verdaglen tree (Lv 15–38)

| Zone               | Lv    | Mobs                       | HP   | EXP |
|--------------------|-------|----------------------------|------|-----|
| mossglade          | 15–20 | sprite                     | 80   | 20  |
| whispering_boughs  | 20–25 | faerie                     | 160  | 38  |
| old_roots          | 25–32 | ent                        | 260  | 58  |
| forgotten_woods    | 30–38 | shadow_hulk (existing boss)| 500  | 250 |

### Nightport tree (Lv 25–45)

| Zone               | Lv    | Mobs                       | HP   | EXP |
|--------------------|-------|----------------------------|------|-----|
| backalleys         | 25–28 | alley_cat                  | 220  | 50  |
| sewer_tunnels      | 28–35 | rat_king                   | 320  | 72  |
| underground_vault  | 35–45 | golem                      | 480  | 110 |

### Tidehaven tree (Lv 30–45)

| Zone               | Lv    | Mobs                       | HP   | EXP |
|--------------------|-------|----------------------------|------|-----|
| tide_docks         | 30–33 | jellyfish                  | 280  | 65  |
| driftwood_beach    | 33–38 | crab                       | 360  | 80  |
| sunken_reef        | 38–45 | kraken_spawn               | 520  | 120 |

### Citadel of Dawn (existing — endgame, keep)
- Lv 45+ group content.

## Portal graph

Each pair below is a bidirectional portal (Unity collider trigger →
ZONE_ENTER packet).

```
beacon_of_winds   ↔ solaria
solaria           ↔ solaria_outskirts
solaria_outskirts ↔ sunlit_plains
sunlit_plains     ↔ wheat_fields
wheat_fields      ↔ pinewood_trail
pinewood_trail    ↔ pyresummit
solaria           ↔ verdaglen
verdaglen         ↔ mossglade
mossglade         ↔ whispering_boughs
whispering_boughs ↔ old_roots
old_roots         ↔ forgotten_woods
solaria           ↔ nightport
nightport         ↔ backalleys
backalleys        ↔ sewer_tunnels
sewer_tunnels     ↔ underground_vault
pyresummit        ↔ cinder_ridge
cinder_ridge      ↔ ashfall_cliffs
ashfall_cliffs    ↔ magma_hollow
nightport         ↔ tidehaven   (ferry)
tidehaven         ↔ tide_docks
tide_docks        ↔ driftwood_beach
driftwood_beach   ↔ sunken_reef
sunken_reef       ↔ citadel_of_dawn
```

The graph is a *tree from the player's perspective* (one-way leveling
flow) but the portals themselves are bidirectional so you can backtrack
to the previous hunting zone or the city.

## Implementation phases

| Phase | Work                                                                                             | Status        |
|-------|--------------------------------------------------------------------------------------------------|---------------|
| 1     | This design document                                                                             | ✅ done       |
| 2     | Server-side `MonsterManager.spawnInitial` extended: spawn entries for all 18 hunting zones       | ✅ done       |
| 3     | Unity scene auto-generation (ProjectSetup) for 22 new scenes (5 cities + 16 huntings)            | ✅ done       |
| 3.5   | In-game World Map overlay (M key) + LoginPanel theme unification                                 | ✅ done       |
| 4     | Portal labels + pulse glow (zone-transition trigger components were already in place)            | ✅ done       |
| 5     | NPC catalog per city: innkeeper, shopkeeper, class-trainer, ferry/ship master                    | next          |
| 6     | Mob sprite work — 13 new monster types now referenced (snail, mushroom, wolf, fire_imp, etc.)    | later (art)   |
| 7     | Quest threading across the city tree (Lv 10 intro → Lv 20 hub → Lv 35 region boss → endgame)     | later         |

Phase 6 is the longest-running because each monster type needs sprite
work. Until then the client falls back to a generic placeholder for
unknown monster types, which is fine for shape testing — the wire and
the zone graph work without finished art.
