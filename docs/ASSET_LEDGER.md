# E0 asset ledger

This ledger is the source of truth for temporary vertical-slice assets selected for Session E0. The game design and asset requirements remain canonical in [GDD.md](GDD.md).

**Acquisition date:** 2026-09-03  
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
| Enemy portraits | OpenAI ImageGen built-in tool | Project-generated placeholder | Prompt provenance below | `Enemies/Generated` | None |

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

Status images remain separate UI layers so a gem can display multiple statuses without a combinatorial sprite set.

### Enemies

| Content ID | Selected asset |
| --- | --- |
| `enemy.geode_mite` | `Enemies/Generated/enemy_geode_mite.png` |
| `enemy.frost_oracle` | `Enemies/Generated/enemy_frost_oracle.png` |
| `enemy.geode_mite_elite` | `Enemies/Generated/enemy_geode_mite_elite.png` |
| `enemy.prism_stalker` | `Enemies/Generated/enemy_prism_stalker.png` |
| `enemy.crystal_warden` | `Enemies/Generated/enemy_crystal_warden.png` |

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
| `ui.status_feedback`, `ui.clear_feedback` | `Icons/GameIconsLorc/circle-sparks.png` |

The four level-up branch icons reuse `gem.ember`, `gem.tide`, `gem.venom`, and `gem.volt`. Reward cards reuse the selected skill icon. Cooldown, duration, HP, resource, and reward values are rendered as text.

### UI primitives

All listed files come from Kenney's UI Pack: RPG Expansion.

| UI role | Selected files |
| --- | --- |
| Enemy/HUD/reward panels | `UI/KenneyRpg/panel_*.png`, `panelInset_*.png` |
| Primary/secondary/disabled buttons | `buttonLong_{blue,brown,grey}.png` and `_pressed` variants |
| Active-skill buttons | `buttonSquare_{blue,brown,grey}.png` and `_pressed` variants |
| HP/resource bars | `barBack_horizontal*.png`, `barRed_horizontal*.png`, `barBlue_horizontal*.png`, `barGreen_horizontal*.png`, `barYellow_horizontal*.png` |
| Selected/available/unavailable marks | `iconCheck_*.png`, `iconCircle_*.png`, `iconCross_*.png` |

### Feedback and audio

| UI role | Visual | Audio |
| --- | --- | --- |
| `feedback.swap` | Board tween in Session E | `Audio/KenneyInterface/pluck_001.ogg` |
| `feedback.invalid_swap` | Board shake in Session E | `Audio/KenneyInterface/error_003.ogg` |
| `feedback.clear` | `Vfx/KenneySmoke/WhitePuff/whitePuff00.png` through `whitePuff24.png` | `Audio/KenneyInterface/glass_002.ogg` |
| `feedback.special` | `Vfx/KenneySmoke/Explosion/explosion00.png` through `explosion08.png` | `Audio/KenneyInterface/maximize_006.ogg` |
| `feedback.hit` | `Vfx/KenneySmoke/Flash/flash00.png` through `flash08.png` | `Audio/KenneyRpg/chop.ogg` |
| `feedback.sunder` | Flash sequence | `Audio/KenneyRpg/knifeSlice2.ogg` |
| `feedback.shield` | White Puff sequence, tinted in Unity | `Audio/KenneyRpg/metalClick.ogg` |
| `feedback.status_added` | `Vfx/KenneySmoke/BlackSmoke/blackSmoke00.png` through `blackSmoke24.png`, tinted by status | `Audio/KenneyInterface/drop_002.ogg` |
| `feedback.status_removed` | White Puff sequence | `Audio/KenneyInterface/close_002.ogg` |
| `feedback.victory` | Explosion sequence plus `ui.victory` | `Audio/KenneyInterface/confirmation_004.ogg` |
| `feedback.defeat` | Black Smoke sequence plus `ui.defeat` | `Audio/KenneyInterface/bong_001.ogg` |
| `feedback.ui_select` | Selected button state | `Audio/KenneyInterface/click_003.ogg` |
| `feedback.reward_confirmed` | Check mark and panel pulse | `Audio/KenneyInterface/confirmation_001.ogg` |

## Game-icons file inventory

Every PNG below is by Lorc under CC BY 3.0. Its exact source page is `https://game-icons.net/1x1/lorc/{filename-without-extension}.html`.

`anchor.png`, `big-wave.png`, `bordered-shield.png`, `bubbling-flask.png`, `circle-sparks.png`, `cracked-glass.png`, `crystal-shine.png`, `fire-shield.png`, `frozen-block.png`, `glass-heart.png`, `hammer-drop.png`, `ice-spear.png`, `justice-star.png`, `laurel-crown.png`, `lightning-frequency.png`, `magic-palm.png`, `magic-swirl.png`, `marrow-drain.png`, `poison-bottle.png`, `poison-gas.png`, `power-lightning.png`, `rock.png`, `shattered-sword.png`, `skull-crossed-bones.png`, `small-fire.png`, `snowflake-1.png`, `wave-strike.png`.

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

## Import policy

`Assets/Game/Editor/E0AssetImportSettings.cs` automatically imports E0 PNG files as single sprites with alpha, clamp wrapping, no mipmaps, and mobile-friendly compression. Generated portraits retain a 2048 maximum texture size; other E0 images use 512. E0 sound effects import as mono.

## E0 acceptance handoff

- Every MVP gem, special, board status, enemy, intent telegraph, HUD resource, passive, active skill, reward state, and required feedback state has a mapping above.
- All selected sourced assets have a commercial-compatible license recorded here.
- All imported non-generated assets retain their distributed license text.
- The required Lorc attribution is ready to place in the Session E Settings/Credits screen.
- Device-scale readability and status stacking remain Session E integration checks because no presentation scene exists yet.
