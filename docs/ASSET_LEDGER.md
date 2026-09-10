# E0 asset ledger

This ledger is the source of truth for temporary vertical-slice assets selected for Session E0. The game design and asset requirements remain canonical in [GDD.md](GDD.md).

**Acquisition dates:** 2026-09-03 (visual/E0 placeholders); 2026-09-06 (sound-design pass); 2026-09-09 (regional enemy portraits); 2026-09-10 (enemy attack poses)
**Project asset root:** `Assets/Game/Presentation/Art/E0`

## Required attribution

The shipped game must expose the following credit from its Settings or Credits UI:

> Icons made by Lorc. Available on game-icons.net. Licensed under CC BY 3.0.

Link `game-icons.net` to <https://game-icons.net/> and `CC BY 3.0` to <https://creativecommons.org/licenses/by/3.0/>. No imported Lorc icon was modified; Unity may tint or scale the images at runtime.

The other sourced E0 packs are CC0 and require no attribution. Voluntary credit is still retained in this ledger.

## Source registry

| Source | Creator | License | Source URL | Imported path | Attribution |
| --- | --- | --- | --- | --- | --- |
| Gem Match 3 Set | Andrew Tidey; uploaded by Sylly | CC0 1.0 | <https://opengameart.org/content/gem-match-3-set> | `Board/GemMatch3` | Not required; voluntary credit: Andrew Tidey |
| Game-icons.net Lorc icons | Lorc | CC BY 3.0 | Page pattern: `https://game-icons.net/1x1/lorc/{icon-name}.html`; repository license: <https://github.com/game-icons/icons/blob/master/license.txt> | `Icons/GameIconsLorc` | Required; use the credit above |
| UI Pack: RPG Expansion | Kenney | CC0 1.0 | <https://kenney.nl/assets/ui-pack-rpg-expansion> | `UI/KenneyRpg` | Not required; voluntary credit: Kenney |
| Smoke Particles | Kenney | CC0 1.0 | <https://kenney.nl/assets/smoke-particles> | `Vfx/KenneySmoke` | Not required; voluntary credit: Kenney |
| Interface Sounds | Kenney | CC0 1.0 | <https://kenney.nl/assets/interface-sounds> | `Audio/KenneyInterface` | Not required; voluntary credit: Kenney |
| RPG Audio | Kenney | CC0 1.0 | <https://kenney.nl/assets/rpg-audio> | `Audio/KenneyRpg` | Not required; voluntary credit: Kenney |
| Impact Sounds | Kenney | CC0 1.0 | <https://kenney.nl/assets/impact-sounds> | `Audio/SoundDesign/KenneyImpact` | Not required; voluntary credit: Kenney |
| 80 CC0 RPG SFX | rubberduck | CC0 1.0 | <https://opengameart.org/content/80-cc0-rpg-sfx> | `Audio/SoundDesign/RubberduckRpg` | Not required; voluntary credit: rubberduck |
| 40 CC0 water / splash / slime SFX | rubberduck | CC0 1.0 | <https://opengameart.org/content/40-cc0-water-splash-slime-sfx> | `Audio/SoundDesign/RubberduckWater` | Not required; voluntary credit: rubberduck |
| Electricity Sound Effects | Brian MacIntosh / BMacZero | CC0 1.0 | <https://opengameart.org/content/electricity-sound-effects-0> | `Audio/SoundDesign/BMacElectricity` | Not required; voluntary credit: Brian MacIntosh |
| Ice breaking/shattering | IgnasD | CC0 1.0 | <https://opengameart.org/content/ice-breakingshattering> | `Audio/SoundDesign/IgnasIce` | Not required; voluntary credit: IgnasD |
| Magic Spell SFX | JaggedStone | CC0 1.0 | <https://opengameart.org/content/magic-spell-sfx> | `Audio/SoundDesign/JaggedStoneMagic` | Not required; voluntary credit: JaggedStone |
| Crystal Cave (song18) | The Cynic Project / cynicmusic | CC0 1.0 | <https://opengameart.org/content/crystal-cave-song18> | `Audio/Music/CynicMusic` | Not required by CC0; requested voluntary credit retained |
| Battle RPG Theme, loop variation | Cleyton Kauffman | CC0 1.0 | <https://opengameart.org/content/boss-battle-theme> | `Audio/Music/CleytonKauffman` | Not required; voluntary credit: Cleyton Kauffman |
| Enemy portraits | OpenAI ImageGen built-in tool | Project-generated placeholder | Prompt provenance below | `Enemies/Generated` | None |
| Application icon | OpenAI ImageGen built-in tool | Project-generated original | Prompt provenance below | `../app_icon.png` | None |

Each sourced directory contains the license text distributed with its source pack.

## Stable content mappings

Paths below are relative to `Assets/Game/Presentation/Art/E0`.

### Board gems and specials

| Content ID | Selected asset | Source file in pack |
| --- | --- | --- |
| `gem.ember` | `Board/GemMatch3/gem_ember.png` | `PNG/Large/Gem Type1 Red.png` |
| `gem.tide` | `Board/GemMatch3/gem_tide.png` | `PNG/Large/Gem Type2 Blue.png` |
| `gem.venom` | `Board/GemMatch3/gem_venom.png` | `PNG/Large/Gem Type3 Green.png` |
| `gem.volt` | `Board/GemMatch3/gem_volt.png` | `PNG/Large/Gem Type4 Yellow.png` |
| `gem.prism` / `special.prism` | `Board/GemMatch3/gem_prism.png` | `PNG/Large/Gem Type1 Purple.png` |
| `special.spark` | `Board/GemMatch3/special_spark.png` | `PNG/Large/Gem Type2 Red.png` |
| `special.current` | `Board/GemMatch3/special_current.png` | `PNG/Large/Gem Type3 Blue.png` |
| `special.spore` | `Board/GemMatch3/special_spore.png` | `PNG/Large/Gem Type4 Green.png` |
| `special.charge` | `Board/GemMatch3/special_charge.png` | `PNG/Large/Gem Type1 Yellow.png` |

### Board and combat statuses

| Content ID | Selected asset | Usage |
| --- | --- | --- |
| `status.frozen` | `Icons/GameIconsLorc/frozen-block.png` | Independent board overlay and tooltip icon |
| `status.cracked` | `Icons/GameIconsLorc/cracked-glass.png` | Independent board overlay and tooltip icon |
| `status.anchored` | `Icons/GameIconsLorc/anchor.png` | Independent board overlay and tooltip icon; duration is live text |
| `status.poison` | `Icons/GameIconsLorc/poison-gas.png` | Enemy status icon; stack count is live text |
| `status.thorned` | `Icons/GameIconsLorc/shattered-sword.png` | R4 board overlay and tooltip icon; per-clear damage and cap are live text |

Status images remain separate UI layers so a gem can display multiple statuses without a combinatorial sprite set.

### Enemies

| Content ID | Selected asset |
| --- | --- |
| `enemy.geode_mite` | `Enemies/Generated/enemy_geode_mite.png` |
| `enemy.frost_oracle` | `Enemies/Generated/enemy_frost_oracle.png` |
| `enemy.geode_mite_elite` | `Enemies/Generated/enemy_geode_mite_elite.png` |
| `enemy.prism_stalker` | `Enemies/Generated/enemy_prism_stalker.png` |
| `enemy.crystal_warden` | `Enemies/Generated/enemy_crystal_warden.png` |
| `enemy.crystal_tick` | Temporary reuse: `Enemies/Generated/enemy_geode_mite.png` |
| `enemy.rime_moth` | Temporary reuse: `Enemies/Generated/enemy_frost_oracle.png` |
| `enemy.anchor_crab` | Temporary reuse: `Enemies/Generated/enemy_geode_mite_elite.png` |
| `enemy.hollow_idol` | Temporary reuse: `Enemies/Generated/enemy_prism_stalker.png` |
| `enemy.fracture_golem` | Temporary reuse: `Enemies/Generated/enemy_geode_mite_elite.png` |
| `enemy.stormglass_roc` | Temporary reuse: `Enemies/Generated/enemy_frost_oracle.png` |
| `enemy.facet_engine` | Temporary reuse: `Enemies/Generated/enemy_crystal_warden.png` |
| `enemy.briar_wisp` | `Enemies/Generated/enemy_briar_wisp.png` |
| `enemy.ashback_boar` | `Enemies/Generated/enemy_ashback_boar.png` |
| `enemy.cinder_nymph` | `Enemies/Generated/enemy_cinder_nymph.png` |
| `enemy.thornbound_stag` | `Enemies/Generated/enemy_thornbound_stag.png` |
| `enemy.pyreheart_treant` | `Enemies/Generated/enemy_pyreheart_treant.png` |
| `enemy.sootcap_shaman` | Temporary reuse: `Enemies/Generated/enemy_briar_wisp.png` |
| `enemy.glassvine_serpent` | Temporary reuse: `Enemies/Generated/enemy_cinder_nymph.png` |
| `enemy.ashen_dryad` | Temporary reuse: `Enemies/Generated/enemy_thornbound_stag.png` |
| `enemy.furnace_matriarch` | Temporary reuse: `Enemies/Generated/enemy_pyreheart_treant.png` |
| `enemy.nullwing_bat` | `Enemies/Generated/enemy_nullwing_bat.png` |
| `enemy.mirror_eel` | `Enemies/Generated/enemy_mirror_eel.png` |
| `enemy.rift_weaver` | `Enemies/Generated/enemy_rift_weaver.png` |
| `enemy.eclipse_chimera` | `Enemies/Generated/enemy_eclipse_chimera.png` |
| `enemy.astral_devourer` | `Enemies/Generated/enemy_astral_devourer.png` |
| `enemy.shard_leech` | Temporary reuse: `Enemies/Generated/enemy_mirror_eel.png` |
| `enemy.orbit_sentinel` | Temporary reuse: `Enemies/Generated/enemy_rift_weaver.png` |
| `enemy.parallax_knight` | Temporary reuse: `Enemies/Generated/enemy_eclipse_chimera.png` |
| `enemy.singularity_seraph` | Temporary reuse: `Enemies/Generated/enemy_astral_devourer.png` |

Every unique portrait also has one generated attack pose at `Enemies/Generated/<base-name>_attack.png`. Its catalog key is `{enemyId}.attack`; enemy IDs that reuse a base portrait also reuse its matching attack pose. The procedural animation system falls back to the base portrait if an attack asset is absent.

### Intent telegraphs

Intent damage/status amounts are live text. Composite intents display multiple icons rather than requiring new artwork.

| Telegraph key | Selected asset(s) |
| --- | --- |
| `intent.chip` | `Icons/GameIconsLorc/rock.png` |
| `intent.crack` | `Icons/GameIconsLorc/cracked-glass.png` |
| `intent.chill` | `Icons/GameIconsLorc/snowflake-1.png` |
| `intent.needle` | `Icons/GameIconsLorc/ice-spear.png` |
| `intent.crush` | `Icons/GameIconsLorc/hammer-drop.png` + `cracked-glass.png` |
| `intent.bolt` | `Icons/GameIconsLorc/lightning-frequency.png` |
| `intent.drain` | `Icons/GameIconsLorc/marrow-drain.png` |
| `intent.seal` | `Icons/GameIconsLorc/anchor.png` |
| `intent.shardstorm` | `Icons/GameIconsLorc/crystal-shine.png` |
| `intent.freeze_anchor` | `Icons/GameIconsLorc/snowflake-1.png` + `anchor.png` |
| `intent.bite`, `intent.claw` | `Icons/GameIconsLorc/shattered-sword.png` |
| `intent.freeze_hit` | `Icons/GameIconsLorc/snowflake-1.png` |
| `intent.barrier` | `Icons/GameIconsLorc/bordered-shield.png` |
| `intent.jam` | `Icons/GameIconsLorc/magic-palm.png` |
| `intent.thorns` | `Icons/GameIconsLorc/shattered-sword.png` |

### HUD and progression

| Content ID or UI role | Selected asset |
| --- | --- |
| `ui.player_health`, `ui.enemy_health` | `Icons/GameIconsLorc/glass-heart.png` |
| `ui.focus` | `Icons/GameIconsLorc/magic-swirl.png` |
| `ui.toxic` | `Icons/GameIconsLorc/poison-bottle.png` |
| `ui.shield` | `Icons/GameIconsLorc/bordered-shield.png` |
| `ui.experience`, `ui.level_up` | `Icons/GameIconsLorc/justice-star.png` |
| `ui.victory` | `Icons/GameIconsLorc/laurel-crown.png` |
| `ui.defeat` | `Icons/GameIconsLorc/skull-crossed-bones.png` |
| `skill.kindling` | `Icons/GameIconsLorc/small-fire.png` |
| `skill.backdraft` | `Icons/GameIconsLorc/fire-shield.png` |
| `skill.flow_state` | `Icons/GameIconsLorc/big-wave.png` |
| `skill.undertow` | `Icons/GameIconsLorc/wave-strike.png` |
| `skill.corrosive` | `Icons/GameIconsLorc/poison-gas.png` |
| `skill.overcharge` | `Icons/GameIconsLorc/power-lightning.png` |
| `skill.sunder` | `Icons/GameIconsLorc/shattered-sword.png` |
| `skill.cleanse` | `Icons/GameIconsLorc/magic-palm.png` |
| `skill.catalyze` | `Icons/GameIconsLorc/bubbling-flask.png` |
| `skill.cinderwake` | `Icons/GameIconsLorc/small-fire.png` |
| `skill.reservoir` | `Icons/GameIconsLorc/bordered-shield.png` |
| `skill.concentrate` | `Icons/GameIconsLorc/poison-bottle.png` |
| `skill.contagion` | `Icons/GameIconsLorc/poison-gas.png` |
| `skill.static_guard` | `Icons/GameIconsLorc/lightning-frequency.png` |
| `skill.live_wire` | `Icons/GameIconsLorc/power-lightning.png` |
| `skill.aegis` | `Icons/GameIconsLorc/bordered-shield.png` |
| `skill.infuse` | `Icons/GameIconsLorc/crystal-shine.png` |
| `skill.transmute` | `Icons/GameIconsLorc/magic-swirl.png` |
| `skill.detonate` | `Icons/GameIconsLorc/circle-sparks.png` |
| `skill.reweave` | `Icons/GameIconsLorc/crystal-shine.png` |
| `skill.flashfire` | `Icons/GameIconsLorc/small-fire.png` |
| `skill.galvanic_venom` | `Icons/GameIconsLorc/power-lightning.png` |
| `skill.scalding_current` | `Icons/GameIconsLorc/wave-strike.png` |
| `skill.toxic_undertow` | `Icons/GameIconsLorc/poison-bottle.png` |
| `skill.keystone.tempered_core` | `Icons/GameIconsLorc/glass-heart.png` |
| `skill.keystone.prismatic_start` | `Icons/GameIconsLorc/crystal-shine.png` |
| `skill.keystone.rapid_casting` | `Icons/GameIconsLorc/magic-swirl.png` |
| `skill.keystone.hard_light` | `Icons/GameIconsLorc/fire-shield.png` |
| `skill.evolution.sparkstorm`, `skill.evolution.ashen_aegis` | `Icons/GameIconsLorc/small-fire.png`, `bordered-shield.png` |
| `skill.evolution.deep_current`, `skill.evolution.tidal_memory` | `Icons/GameIconsLorc/big-wave.png`, `bordered-shield.png` |
| `skill.evolution.virulent_bloom`, `skill.evolution.patient_venom` | `Icons/GameIconsLorc/poison-gas.png` |
| `skill.evolution.overclock`, `skill.evolution.storm_reserve` | `Icons/GameIconsLorc/power-lightning.png`, `lightning-frequency.png` |
| `skill.keystone.briarheart`, `skill.keystone.rootbreaker`, `skill.keystone.emberseed` | `Icons/GameIconsLorc/glass-heart.png`, `fire-shield.png`, `crystal-shine.png` |
| `skill.keystone.null_coil`, `skill.keystone.mirror_shard`, `skill.keystone.rift_lens` | `Icons/GameIconsLorc/magic-swirl.png`, `bordered-shield.png`, `fire-shield.png` |
| `ui.status_feedback`, `ui.clear_feedback` | `Icons/GameIconsLorc/circle-sparks.png` |

The four level-up branch icons reuse `gem.ember`, `gem.tide`, `gem.venom`, and `gem.volt`. Reward cards reuse the selected skill icon. Cooldown, duration, HP, resource, and reward values are rendered as text.

### UI primitives

All listed files come from Kenney's UI Pack: RPG Expansion.

| UI role | Selected files |
| --- | --- |
| Enemy/HUD/reward panels | `UI/KenneyRpg/panel_*.png`, `panelInset_*.png` |
| Primary/secondary/disabled buttons | `buttonLong_{blue,brown,grey}.png` and `_pressed` variants; all runtime states are mapped separately |
| Active-skill buttons | `buttonSquare_{blue,brown,grey}.png` and `_pressed` variants |
| HP/resource bars | `barBack_horizontal*.png`, `barRed_horizontal*.png`, `barBlue_horizontal*.png`, `barGreen_horizontal*.png`, `barYellow_horizontal*.png` |
| Selected/available/unavailable marks | `iconCheck_*.png`, `iconCircle_*.png`, `iconCross_*.png` |
| Menu, modal, card, and inset surfaces | `panel_*.png`, `panelInset_*.png` |

### Feedback and audio

| UI role | Visual | Audio |
| --- | --- | --- |
| `feedback.swap` | Board tween in Session E | `Audio/KenneyInterface/pluck_001.ogg` |
| `feedback.invalid_swap` | Board shake in Session E | `Audio/KenneyInterface/error_003.ogg` |
| `feedback.clear.crystal.1`–`.5` | `Vfx/KenneySmoke/WhitePuff/whitePuff00.png` through `whitePuff24.png` | `Audio/SoundDesign/KenneyImpact/impactGlass_light_000.ogg` through `_004.ogg` |
| `feedback.clear.ember` | Clear feedback tinted red | `Audio/SoundDesign/RubberduckRpg/spell_fire_07.ogg` |
| `feedback.clear.tide.1`–`.2` | Clear feedback tinted blue | `Audio/SoundDesign/RubberduckWater/splash_02.ogg`, `splash_03.ogg` |
| `feedback.clear.venom.1`–`.3` | Clear feedback tinted green | `Audio/SoundDesign/RubberduckWater/slime_01.ogg` through `slime_03.ogg` |
| `feedback.clear.volt` | Clear feedback tinted yellow | `Audio/SoundDesign/BMacElectricity/spark.wav` |
| `feedback.special.crystal.1`–`.3` | `Vfx/KenneySmoke/Explosion/explosion00.png` through `explosion08.png` | `Audio/SoundDesign/KenneyImpact/impactGlass_medium_000.ogg` through `_002.ogg` |
| `feedback.special.magic.1`–`.2` | Special explosion sequence | `Audio/SoundDesign/JaggedStoneMagic/magical_1.ogg`, `magical_4.ogg` |
| `feedback.hit.stone.1`–`.3` | `Vfx/KenneySmoke/Flash/flash00.png` through `flash08.png` | `Audio/SoundDesign/KenneyImpact/impactMining_000.ogg` through `_002.ogg` |
| `feedback.intent` | Intent panel emphasis | `Audio/SoundDesign/KenneyImpact/impactBell_heavy_000.ogg` |
| `feedback.sunder` | Flash sequence | `Audio/KenneyRpg/knifeSlice2.ogg` |
| `feedback.shield` | White Puff sequence, tinted in Unity | `Audio/KenneyRpg/metalClick.ogg` |
| `feedback.status_added` | `Vfx/KenneySmoke/BlackSmoke/blackSmoke00.png` through `blackSmoke24.png`, tinted by status | `Audio/KenneyInterface/drop_002.ogg` |
| `feedback.status_removed` | White Puff sequence | `Audio/KenneyInterface/close_002.ogg` |
| `feedback.status_removed.frozen.1`–`.3` | White Puff sequence, ice tint | `Audio/SoundDesign/IgnasIce/LedasLuzta.ogg`, `LedasLuzta2.ogg`, `LedasLuzta33.ogg` |
| `feedback.victory` | Explosion sequence plus `ui.victory` | `Audio/KenneyInterface/confirmation_004.ogg` |
| `feedback.defeat` | Black Smoke sequence plus `ui.defeat` | `Audio/KenneyInterface/bong_001.ogg` |
| `feedback.ui_select` | Selected button state | `Audio/KenneyInterface/click_003.ogg` |
| `feedback.reward_confirmed` | Check mark and panel pulse | `Audio/KenneyInterface/confirmation_001.ogg` |

Clear audio plays once per resolved clear wave rather than once per gem. It combines one short crystal transient with a lower-volume layer for the dominant gem type. Crystal variants and pitch rise across cascade steps and cap at the fifth variant. Special feedback combines a medium glass impact with a magic layer. Variant choice is derived from presentation event sequence and never consumes simulation RNG.

### Music

| Music role | Selected asset | Runtime use |
| --- | --- | --- |
| `music.crystal_cave` | `Audio/Music/CynicMusic/crystal_cave.mp3` | Title, map, events, rest, rewards, summaries, and non-boss combat |
| `music.boss_battle` | `Audio/Music/CleytonKauffman/boss_battle.ogg` | Boss encounter and skill window |

Sound effects import as mono Vorbis and decompress on load. Music retains stereo, streams from storage, loops, and uses a lower Vorbis quality suitable for mobile. Device-local settings independently enable sound effects and music.

## Game-icons file inventory

Every PNG below is by Lorc under CC BY 3.0. Its exact source page is `https://game-icons.net/1x1/lorc/{filename-without-extension}.html`.

`anchor.png`, `big-wave.png`, `bordered-shield.png`, `bubbling-flask.png`, `circle-sparks.png`, `cracked-glass.png`, `crystal-shine.png`, `fire-shield.png`, `frozen-block.png`, `glass-heart.png`, `hammer-drop.png`, `ice-spear.png`, `justice-star.png`, `laurel-crown.png`, `lightning-frequency.png`, `magic-palm.png`, `magic-swirl.png`, `marrow-drain.png`, `poison-bottle.png`, `poison-gas.png`, `power-lightning.png`, `rock.png`, `shattered-sword.png`, `skull-crossed-bones.png`, `small-fire.png`, `snowflake-1.png`, `wave-strike.png`.

## Generated application icon provenance

The 1024×1024 application icon was generated with the built-in OpenAI ImageGen tool on 2026-09-06. It uses no third-party character, franchise, logo, or source image.

```text
Use case: logo-brand
Asset type: production mobile game app icon, square 1024x1024
Primary request: Create a bold, instantly readable icon for a portrait match-3 roguelike where every matched crystal becomes a weapon. The hero mark is one large radiant faceted prism crystal, with three smaller aligned gems behind it suggesting a three-in-a-row match and a subtle upward blade/spear silhouette formed by the crystal facets.
Scene/backdrop: deep midnight-navy magical cavern glow, full-bleed square background with a polished dark indigo vignette; no transparent corners and no baked-in rounded-square border because app stores apply their own mask.
Subject: central luminous diamond-shaped prism crystal, magenta-violet core shifting to cyan highlights; three small gem echoes arranged in a tight diagonal/row behind it; restrained gold-bronze framing accents that hint at roguelike fantasy armor.
Style/medium: premium stylized mobile game icon, crisp painterly 3D illustration, chunky faceted geometry, clean silhouette, high polish, consistent with colorful match-3 gems and dramatic fantasy crystal enemies.
Composition/framing: centered, symmetrical, extreme close-up, single dominant shape filling about 72% of canvas; important details inside central 80% safe area; readable at 48px; controlled depth, not a scene.
Lighting/mood: energetic magical glow, bright cyan and hot magenta rim light, heroic and tactical rather than cute.
Color palette: midnight navy, electric cyan, prism magenta, small warm ember-orange/gold accents.
Materials/textures: glossy cut gemstone, subtle stone/metal frame, sharp specular highlights, clean edges.
Constraints: no text, no letters, no numbers, no character face, no UI, no grid board, no clutter, no watermark; one iconic focal point; strong contrast; avoid overly thin details and photorealism.
```

The full-resolution source is stored at `Assets/Game/Presentation/Art/app_icon.png`; platform-specific masks and downscaling should be applied by Unity or the target store pipeline.

## Generated portrait provenance

All portraits were generated with the built-in OpenAI ImageGen tool on 2026-09-03. No third-party character, franchise, logo, or source image was requested. The prompts below are retained so temporary portraits can be regenerated.

### Geode Mite

```text
Use case: stylized-concept
Asset type: square mobile-game enemy portrait with transparent background
Primary request: Geode Mite, a small hostile fantasy creature made from rough gray geode rock, low squat insect body, six short legs, jagged crystal growths, glowing amber eyes
Subject: one creature only, recognizable silhouette, front three-quarter view, whole creature visible
Style/medium: polished stylized 2D game illustration, chunky readable forms, moderate detail suitable for an MVP mobile encounter portrait
Composition/framing: centered, generous transparent padding, no ground plane
Lighting/mood: dramatic cool rim light with warm crystal glow, threatening but not gruesome
Constraints: genuinely transparent background; no text; no frame; no logo; no watermark; no extra creatures; no weapons; no cast shadow beyond the creature edge
```

### Frost Oracle

```text
Use case: stylized-concept
Asset type: square mobile-game enemy portrait with transparent background
Primary request: Frost Oracle, a floating mysterious fantasy seer formed from pale ice and blue crystal, hooded upper-body silhouette, narrow faceless mask with cold cyan glow, crystalline staff-like shapes integrated into the body
Subject: one enemy only, front three-quarter view, full floating figure visible
Style/medium: polished stylized 2D game illustration, chunky readable forms, moderate detail suitable for an MVP mobile encounter portrait
Composition/framing: centered, generous transparent padding, no ground plane
Lighting/mood: cold cyan internal glow and cool rim light, ominous and magical
Constraints: genuinely transparent background; no text; no frame; no logo; no watermark; no extra characters; no recognizable franchise styling
```

### Geode Mite Elite

```text
Use case: identity-preserve
Asset type: square mobile-game enemy portrait with transparent background
Input images: Image 1: Geode Mite character anchor and edit target
Primary request: turn the same Geode Mite into the Geode Mite Elite encounter variant
Changes: increase its apparent size and armor, add thicker darker stone plates, longer amber crystal spikes, brighter orange fissures, and two small bronze armor bands on the front legs
Composition/framing: preserve the same front three-quarter viewpoint and keep the whole creature visible
Constraints: preserve the creature species, anatomy, face, eye arrangement, core silhouette, illustration style, and transparent background; change only the elite upgrades; no text; no frame; no logo; no watermark; no extra creatures
```

The elite output received a second background-extraction pass: remove the generated checkerboard and replace it with genuine alpha while preserving the creature exactly.

### Prism Stalker

```text
Use case: stylized-concept
Asset type: square mobile-game enemy portrait with transparent background
Primary request: Prism Stalker, a lean predatory fantasy beast built from dark obsidian plates and sharp iridescent prism crystals, feline-reptilian posture, one bright prismatic eye, long angular limbs
Subject: one enemy only, stalking pose, front three-quarter view, full creature visible
Style/medium: polished stylized 2D game illustration, chunky readable silhouette, moderate detail suitable for an MVP mobile encounter portrait
Composition/framing: centered, generous transparent padding, no ground plane
Lighting/mood: dark body with restrained rainbow refractions and violet rim light, dangerous and elusive
Constraints: genuinely transparent background; no text; no frame; no logo; no watermark; no extra creatures; no recognizable franchise styling
```

### Crystal Warden

```text
Use case: stylized-concept
Asset type: square mobile-game boss portrait with transparent background
Primary request: Crystal Warden, a massive ancient guardian made from dark stone armor and pale blue crystal, broad humanoid torso, imposing crown-like crystal formation, heavy symmetrical arms, glowing core in the chest
Subject: one boss only, powerful frontal three-quarter stance, upper body and arms fully visible
Style/medium: polished stylized 2D game illustration, chunky readable silhouette, moderate detail suitable for an MVP mobile encounter portrait
Composition/framing: centered, generous transparent padding, no ground plane
Lighting/mood: cold blue internal glow, dramatic rim light, monumental and threatening
Constraints: genuinely transparent background; no text; no frame; no logo; no watermark; no weapon; no extra characters; no recognizable franchise styling
```

### Cinderbloom Wilds and Voidglass Depths

The ten regional portraits were generated with the built-in OpenAI ImageGen tool on 2026-09-09. They are project-generated originals: no third-party character, franchise, logo, or source image was requested, and no external attribution is required. Every generation used this shared production prompt, with the subject lines below.

```text
Use case: stylized-concept
Asset type: square mobile-game enemy portrait with genuinely transparent background
Style/medium: polished stylized 2D fantasy game illustration, chunky readable silhouette, moderate detail, matching a colorful match-3 roguelike
Composition/framing: one enemy only, centered, front three-quarter view, whole creature visible, generous transparent padding, no ground plane
Lighting/mood: dramatic game lighting, threatening but not gruesome
Constraints: actual transparent alpha; no text; no frame; no logo; no watermark; no extra creatures; no recognizable franchise styling; no checkerboard background
```

| File | Subject request |
| --- | --- |
| `enemy_briar_wisp.png` | Hovering seed-spirit wrapped in dark thorn vines, emerald crystal face, three ember-orange eyes, leaf-like fins |
| `enemy_ashback_boar.png` | Stocky boar of volcanic bark and cracked charcoal stone, amber crystal tusks, smoldering back |
| `enemy_cinder_nymph.png` | Floating fire-and-flower spirit with faceted crimson mask, petal flame mantle, thornlike limbs |
| `enemy_thornbound_stag.png` | Corrupted forest guardian with dark bark body, branching crystal-thorn antlers, molten chest core |
| `enemy_pyreheart_treant.png` | Colossal tree monster split by a furnace heart, burning crystal crown, root arms and thorn claws |
| `enemy_nullwing_bat.png` | Obsidian and violet-glass cave bat, angular wings, hollow circular face with one cyan eye |
| `enemy_mirror_eel.png` | Floating serpentine eel with silver-blue mirror scales, translucent fins, prism eyes, S silhouette |
| `enemy_rift_weaver.png` | Alien six-legged black-glass crystal construct with magenta energy threads |
| `enemy_eclipse_chimera.png` | Leonine obsidian elite with crescent crystal horns, dark-glass wings, violet chest core |
| `enemy_astral_devourer.png` | Cosmic leviathan of black crystal around a star-filled void, singularity jaws, four heavy claws |

The elite and boss prompts additionally requested a more imposing full stance and preserved the same transparent-background and single-subject constraints.

### Enemy attack poses

The fifteen attack poses were generated with the built-in OpenAI ImageGen tool on 2026-09-10 as identity-preserving edits of the corresponding base portraits. A second background-extraction pass replaced the generated preview checkerboard with genuine alpha; every final PNG was validated as 32-bit ARGB with transparent corner pixels.

```text
Use case: identity-preserve
Asset type: alternate attack-pose frame for a mobile-game enemy portrait
Input images: Image 1: edit target and exact character identity anchor
Primary request: Repose the same enemy into a readable attack directed toward the viewer and lower edge; make its existing internal glow slightly brighter.
Style/medium: preserve the same polished stylized 2D fantasy game illustration and rendering quality
Composition/framing: preserve the square canvas, front three-quarter viewpoint, character scale, visual center, and generous transparent padding; keep the whole creature visible
Constraints: preserve exact identity, anatomy, materials, palette, lighting direction, and detail; change only pose and attack energy; genuine transparent alpha; no checkerboard, background pixels, text, frame, ground, cast shadow, halo, logo, watermark, extra creature, added weapon, or cropping
```

The pose direction was adapted to each silhouette: mites lunge with front claws; the Oracle casts through its hand and staff; the Stalker and Chimera pounce; the Warden punches; the Wisp and Nymph sweep their limbs; the Boar and Stag charge; the Treant swings its root claws; the Bat dives; the Eel snaps; the Weaver thrusts its front legs; and the Devourer opens its singularity jaws and pulls with its claws.

## Import policy

`Assets/Game/Editor/E0AssetImportSettings.cs` automatically imports E0 PNG files as single sprites with alpha, clamp wrapping, no mipmaps, and mobile-friendly compression. Generated portraits retain a 2048 maximum texture size; other E0 images use 512. E0 sound effects import as mono.

## E0 acceptance handoff

- Every MVP gem, special, board status, enemy, intent telegraph, HUD resource, passive, active skill, reward state, and required feedback state has a mapping above.
- All selected sourced assets have a commercial-compatible license recorded here.
- All imported non-generated assets retain their distributed license text.
- The required Lorc attribution is ready to place in the Session E Settings/Credits screen.
- Device-scale readability and status stacking remain Session E integration checks because no presentation scene exists yet.
