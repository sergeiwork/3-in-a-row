# Three in a Row: Roguelike Crystals

**Status:** Mobile presentation implementation v0.6
**Engine / platform:** Unity, C#, portrait mobile  
**Canonical format:** This Markdown file is the sole editable source of truth; focused Markdown documents may link back here when this file becomes too large.

## Document purpose

Define a small but complete, extensible MVP for a match-3 roguelike. Later work should deepen a single system at a time without breaking the contracts between board, combat, content, UI, saves, and progression.

## Contents

1. [Product definition](#1-product-definition)
2. [MVP boundary](#2-mvp-boundary-and-success-criteria)
3. [Core loop](#3-core-loop)
4. [Board, gems, and combos](#4-board-gems-and-combos)
5. [Combat model](#5-combat-model)
6. [Enemy roster](#6-enemy-roster-and-encounters)
7. [Progression and skills](#7-progression-and-skills)
8. [Player UX](#8-player-ux-and-rewards)
9. [Unity architecture](#9-extensible-unity-architecture)
10. [Determinism and saves](#10-determinism-saves-and-observability)
11. [Session plan](#11-build-plan-for-separate-sessions)
12. [Test plan](#12-test-plan-and-acceptance-criteria)
13. [Open decisions](#13-open-decisions--current-blockers)

---

## 1. Product definition

A portrait-mobile match-3 roguelike where every cleared crystal becomes a weapon. The player makes a swap, resolves cascades, converts cleared gem types and match patterns into damage and tactical effects, then survives the enemy response. Victories advance through a short chain of enemies; XP, level-up choices, and active skills create distinct builds.

### Design pillars

- **Readable cause and effect.** Players can always tell why damage, shields, cooldown reduction, or a board disruption happened.
- **Fast tactical turns.** One good swap should feel meaningful without requiring a long planning phase.
- **Board-first roguelike variety.** Upgrades change how the player values board states, not just damage numbers.
- **Structured extension.** Adding a gem, enemy, skill, or encounter may be asset authoring or a focused behavior subclass. Either route must plug into stable content and simulation contracts rather than changing central controllers.

### Player fantasy

- Read a dense board, set up a large clear, and turn enemy disruption into an opportunity.
- Feel momentum after every victory through a visible power increase and a consequential choice.
- Win or lose for understandable reasons rather than hidden rules or opaque RNG.

### Non-goals for this MVP

- No persistent/meta progression, economy, crafting, PvP, live ops, social features, ads, or real-money store.
- No procedural map, shops, relic inventory, complex targeting UI, multiple simultaneous enemies, or positional combat.
- No dependency on final art, audio, localization, cloud saves, or backend before validating the core run.

---

## 2. MVP boundary and success criteria

| Area | MVP commitment | Explicitly out of scope |
| --- | --- | --- |
| Run structure | One linear run of five encounters: four normal/elite encounters and a final Warden | Branching map, shops, multiple acts |
| Board | 7×7 board, match-3+, cascades, four normal gem types and one Prism special | Terrain, hex grid, extra blockers |
| Combat | Player HP, enemy HP/intent, one enemy response per valid player turn, typed effects and statuses | Multiple enemies and allied units |
| Progression | Run XP, three upgrade picks, six-node skill tree, two equipped active skills | Persistent account levels and a large trait pool |
| Content | Five encounter enemy definitions, four gem definitions, six passive skills, three active skills, three board statuses | Full launch content volume |
| Technology | ScriptableObject content, deterministic simulation, local run checkpoint | Backend, remote config, production analytics |

### Vertical-slice pass condition

A fresh player can complete a five-encounter run in **8–12 minutes**, understands why the enemy takes damage, makes at least **three meaningful build choices**, and experiences no progression-ending defect in ten consecutive test runs.

---

## 3. Core loop

### Encounter loop

1. Display enemy name, HP, next intent, player HP, resources, active-skill cooldowns, and the board.
2. Before swapping, the player may use any ready active skill. The player then swaps two adjacent movable gems. Invalid swaps revert and consume no turn.
3. Resolve the board: detect matches → create specials → clear gems → emit clear events → gravity/refill → repeat until stable.
4. Resolve player damage, resources, statuses, passives, and skill reactions from the ordered events.
5. If the enemy survived, automatically resolve its telegraphed intent after the board animation: attack and/or apply board status. No confirmation input is required.
6. On victory, grant XP; if an XP threshold was reached, offer an upgrade choice; then start the next encounter.

### Run loop

`Start Run → Encounter 1 → XP / level-up when eligible → … → Encounter 5 boss → run summary`

Defeat ends the run and returns to the title screen. The MVP has no persistent reward.

### Turn invariant

A valid player swap causes exactly one complete board-resolution phase and, unless it kills the enemy, exactly one automatic enemy-response phase. This invariant must hold in simulation, UI lock state, animations, saves, and tests. Presentation keeps input locked across both phases and does not expose an enemy-response confirmation button.

---

## 4. Board, gems, and combos

### Board rules

- Grid is 7 columns × 7 rows. Gravity pulls down; refills spawn from the top using the encounter RNG stream.
- Coordinates are zero-based with `(0,0)` at the bottom-left. Canonical board storage and board-event ordering are row-major: bottom row to top row, then left to right within a row.
- Gems swap only orthogonally. A valid swap creates a horizontal or vertical match of three or more normal gems.
- All gems in a match clear simultaneously. Cascades continue until the board stabilizes.
- Input locks after an accepted swap and unlocks only after combat resolution is complete.
- **Frozen** gems cannot be swapped but can be included in a match. Clearing one removes Frozen before resolving its normal gem effect.
- If a stable board has no legal swap, reshuffle movable gems while preserving locked/frozen states where feasible. Log the reshuffle reason.
- Initial fill cannot contain a pre-existing match and must expose at least one legal swap. A stable dead board first attempts a color/special permutation; a deterministic regeneration of movable normal gems is the fallback when no playable permutation is found.
- Frozen and Anchored gems are immovable for swap validation. Anchored gems also divide a column into independent gravity/refill segments so they never move while the board remains full.

### Gem definitions

| Gem | Base clear effect | Match 4 result | Match 5 / special interaction |
| --- | --- | --- | --- |
| **Ember** (red) | Deal **4 direct damage** per cleared gem | Create **Spark**; clearing it deals 16 direct damage | A 5-match creates Prism; Ember gems cleared by Prism still deal Ember damage |
| **Tide** (blue) | Gain 1 **Focus** per cleared gem; every 3 Focus deals 6 damage and spends 3 Focus | Create **Current**; clearing it grants 5 Focus | Prism may overcap Focus; at turn end, retained excess Focus becomes 1:1 Shield |
| **Venom** (green) | Apply 1 **Toxic** per cleared gem; at 5 Toxic, consume 5, deal 12 damage, apply 1 Poison | Create **Spore**; clearing it adds 5 Toxic | Prism applies Toxic for each green gem cleared |
| **Volt** (yellow) | Deal 2 damage; each three cleared Volt reduces one equipped active cooldown by 1 | Create **Charge**; clearing it deals 8 damage and reduces both active cooldowns by 1 | Prism counts every cleared Volt toward cooldown progress |
| **Prism** (special) | Created only by 5+ match; swap with a normal gem to clear every gem of that color and resolve their normal effects | N/A | Prism + special triggers both special effects, then clears Prism |

### Pattern specials

| Special | Creation | Clear behavior | Purpose |
| --- | --- | --- | --- |
| Spark | Match 4 Ember in a line | 16 direct damage | Teaches delayed burst from a large match |
| Current / Spore / Charge | Match 4 of respective gem color | Use intensified gem-specific effect; no separate targeting pattern in MVP | Preserves gem identity without increasing board-rule complexity |
| Prism | Match 5 or T/L intersection | Board-wide color clear | High-clarity “big turn” payoff |

### Resolution ordering

`Validate swap → detect all matches → create specials (Prism has priority) → clear matched cells and activated-special targets → emit GemCleared events in deterministic row-major order → resolve each effect → gravity/spawn → repeat cascade`

Damage visuals may wait until the cascade stabilizes, but the simulation applies effects immediately in the documented event order.

For a player-created special, the destination cell is preferred, then the source cell, then the first non-special matched cell in row-major order. The creation cell becomes the special and is not also cleared. A T/L intersection or a line of five or more creates Prism; otherwise a line of four creates the color's match-4 special. Simultaneous disconnected matches each resolve as their own row-major match group.

`GemCleared` is a board lifecycle event with amount `1`, not precomputed combat damage. Its `sourceId` is the base gem and `relatedId` is its special ID. A special-bearing gem also emits `SpecialActivated`; its intensified special effect replaces its normal per-gem clear effect. Prism plus a normal or color-special gem clears that base color; the color special activates if present. Prism-to-Prism swaps are not valid in the MVP.

### Balance seed

Initial numbers should result in a normal enemy defeat in **5–7 valid turns** without a high-value special and **3–5 turns** with sensible combo use. Treat this as a playtest target, not a launch value.

---

## 5. Combat model

### Player and combat state

| State | Initial rule |
| --- | --- |
| Player HP | Start at 40. No full healing between fights; victory restores 4 HP, up to max. |
| Focus | 0–9. Every three Focus deals 6 damage; leftover Focus remains during the player turn. |
| Toxic | 0–9. At five, deal 12 and apply one Poison. Can trigger multiple times during a cascade. |
| Poison | Stored on the enemy. At the start of each enemy response, deal 3 per stack then reduce stacks by 1. Cap at 3 stacks. |
| Shield | Absorbs damage before HP; expires at start of the player's next valid swap. |
| Enemy intent | Always visible before player input. Intent may be attack-only, status-only, or both. |

### Damage order

**Player resolution:** `GemCleared effects → created-special effects → skill/passive reactions → player-caused statuses → enemy death check`

**Enemy response:** `start-of-response Poison → enemy death check → intent execution → damage mitigation → board-status application → cooldown/duration updates → player input unlock`

### Enemy board statuses

| Status | Rule | Counterplay |
| --- | --- | --- |
| **Frozen** | Gem cannot be swapped but can still match and clear normally | Match through it, Prism it, or use Cleanse |
| **Cracked** | Gem clears normally but does not produce its normal gem effect; still contributes to a match | Clear it quickly or color-clear it |
| **Anchored** | Gem cannot fall or be swapped for one player turn; it can clear when matched | Plan a match including it |

Every status requires a distinct icon, a tap-tooltipped rule, a duration counter when relevant, and a distinct removal animation. A status must never silently change a gem color or invalidate a legal match.

Enemy status targeting samples unique eligible board cells through the named `IntentVariation` RNG stream. A cell already carrying the requested status is ineligible; if fewer eligible cells remain than requested, the intent affects every eligible cell. Frozen and Cracked persist until cleared or cleansed. Anchored stores `RemainingPlayerTurns = 1`, remains active through the next accepted swap and its cascades, then expires before that turn's enemy response. After status application, the board rechecks legal swaps and deterministically reshuffles through `BoardSpawn` if the new locks caused a dead board.

---

## 6. Enemy roster and encounters

Enemy behavior uses a deterministic intent cycle/deck. No adaptive AI is required for the MVP. The encounter seed and current intent index are saved.

Intent effects execute in definition order. The current intent advances only after its full effect list resolves; the next intent is then telegraphed. Player damage consumes Shield before HP. Poison resolves before `EnemyIntentStarted`, so a Poison defeat prevents the intent and its board statuses entirely.

| Encounter | Enemy / HP | Intent cycle | Teaching goal |
| --- | --- | --- | --- |
| 1 | Geode Mite — 52 HP | Chip 5 → Crack: 3 Cracked gems → Chip 6 | Baseline damage and board disruption |
| 2 | Frost Oracle — 66 HP | Chill: freeze 2 → Needle 7 → Chill: freeze 3 | Frozen gems can still be cleared; learn to read intent |
| 3 | Geode Mite Elite — 84 HP | Crush 8 + 2 Cracked → Chip 7 → Crack: 4 Cracked | Damage race and status cleanup |
| 4 | Prism Stalker — 92 HP | Bolt 8 → Drain: -3 Focus/-3 Toxic (min. 0) → Bolt 10 | Direct damage remains useful when resources are disrupted |
| 5 | Crystal Warden — 128 HP | Seal: 2 Anchored → Shardstorm 10 → Freeze 2 + Anchor 2 → Shardstorm 12 | Boss combines disruption with telegraphed pressure |

### Tuning method

Tune health from recorded median turns-to-kill with a no-upgrade baseline, then recheck every build path. Do not tune only from theoretical gem averages; cascades and Prism availability widen the real result distribution.

---

## 7. Progression and skills

### XP and upgrade picks

Each victory grants 1 XP. The run starts at level 1 and grants level-up choices after **2, 3, and 4 XP**—three upgrade picks total. A level-up presents three unowned eligible rewards sampled without duplicates through the `RewardSampling` RNG stream. The pool contains skill-tree nodes whose prerequisites are met plus active skills marked as level-up rewards; for the MVP, Catalyze is the only such active reward. This keeps Catalyze reachable during the five-encounter run while preserving prerequisite rules.

### Six-node skill tree

| Branch | Node | Prerequisite | Effect |
| --- | --- | --- | --- |
| Ember | Kindling | None | Ember clear damage +1 |
| Ember | Backdraft | Kindling | Whenever Spark clears, gain 6 Shield |
| Tide | Flow State | None | Focus thresholds deal 7 damage instead of 6 |
| Tide | Undertow | Flow State | When Focus converts to damage, reduce the left active cooldown by 1 |
| Venom | Corrosive | None | Poison deals 4 per stack instead of 3 |
| Volt | Overcharge | None | Volt cooldown progress needs two cleared Volt instead of three |

### Active skills

| Skill | Cooldown | Effect | Timing / targeting |
| --- | --- | --- | --- |
| Sunder | 4 player turns | Deal 14 direct damage | Usable before the player's swap; does not consume a swap |
| Cleanse | 5 player turns | Remove Frozen, Cracked, or Anchored from up to three selected gems | Selection is cancelable before confirmation; remove all if fewer than three exist |
| Catalyze | 5 player turns | Convert up to 4 Focus to damage at 3 each and up to 4 Toxic to Poison at 1:2 | Uses current resources to turn near-threshold states into tactical burst |

Run start learns and equips Sunder in the left slot and Cleanse in the right slot. Catalyze is unlockable from a level-up choice but is not equipped automatically; the player may replace either active between encounters. Learned-active cooldowns persist when a skill is unequipped, and only equipped active cooldowns tick or receive generic left/both-slot reductions. An active used before a swap begins at its full listed cooldown and does not tick down when that swap completes. Active-skill UI must be generated from generic timing and targeting policies rather than per-skill UI code.

Cleanse confirmation accepts one to three unique status-bearing cells; if three or fewer eligible cells exist, an empty target list means “cleanse all.” Catalyze spends up to 4 Focus for 3 damage each, then spends Toxic in pairs up to 4 total for 1 Poison per 2 Toxic without consuming resources that cannot produce an effect because Poison is capped.

---

## 8. Player UX and rewards

### Essential screens

- **Title:** Start Run, How to Play, Settings, build/version label.
- **Encounter:** Enemy panel and intent at top; board centered; player HP/resources and two active-skill buttons below.
- **Level-up:** Three large cards showing branch icon, name, exact numeric effect, and prerequisite trail.
- **Run summary / defeat:** Encounters cleared, biggest cascade, damage by gem type, chosen upgrades, and Start New Run. Include the debug seed in development builds.

### Mobile guardrails

- Keep a generous touch target around board cells.
- Support common 16:9 and tall 20:9 portrait layouts.
- Input becomes available as soon as the result is legible; nonessential animation can be fast-forwarded.
- Plan reduced-motion support at the UI-controller level.

---

## 9. Extensible Unity architecture

### Architecture rules

- Definitions are immutable ScriptableObjects (or serialized content assets); runtime state is plain C# data. Never mutate definitions during a run.
- Board and combat simulation has no Unity scene, `MonoBehaviour`, UI, animation, or save dependency.
- UI, VFX, audio, save, and analytics observe emitted events; they do not decide rules or directly mutate combat state.
- New content may be assembled from existing definitions/effects or implemented as a focused C# subclass when it has genuinely distinct behavior. New subclasses must use the same context, result, event, and registration contracts as existing content.

### Layer boundaries

| Layer | Owns | Must not own |
| --- | --- | --- |
| Content | ScriptableObject definitions, IDs, display keys, icon references, tuning | Per-run HP, cooldowns, random state, scene objects |
| Domain simulation | Board validation/refill, match detection, combat, statuses, deterministic RNG, events | Unity UI, animation, content loading, PlayerPrefs |
| Application flow | Run orchestration, transitions, command queue, checkpoints, reward selection | Rendering details and hard-coded content |
| Presentation | Board/HUD/input, animation sequencing, VFX/SFX, accessibility labels | Authoritative combat outcomes |
| Infrastructure | Content repository, JSON save, debug logging, analytics adapter | Game-rule branching |

### Suggested Unity folder map

```text
Assets/Game/
  Domain/          # Pure C#: state, commands, events, rules, deterministic RNG, tests
  Content/         # Gem, Enemy, Intent, Skill, Status, Encounter ScriptableObjects
  Application/     # RunController, EncounterController, command dispatcher, checkpoints
  Presentation/    # MonoBehaviours, board/HUD views, input, VFX/SFX adapters
  Infrastructure/  # content repository, JSON save, logging, analytics adapter
  Tests/           # EditMode domain tests, PlayMode flow tests, deterministic fixtures
```

### Content definition contracts

| Definition | Required fields | Extension behavior |
| --- | --- | --- |
| `GemDefinition` | `id`, display key, icon, spawn weight, `clearEffectId`, `match4EffectId`, element tag | Reference an existing `GemEffect` or a new focused `GemEffect` subclass; spawn filters use tags |
| `EnemyDefinition` | `id`, display key, max HP, `intentCycle[]`, art keys, reward XP | Add enemy by assembling intent definitions; use a custom intent/effect subclass only for behavior that cannot be expressed by the base intent data |
| `IntentDefinition` | `id`, telegraph key, timing, damage formula, status applications, target policy | Use composable effect steps or a subclassed effect/target policy; never put per-enemy branching in the encounter controller |
| `SkillDefinition` | `id`, slot type, cooldown, timing window, target policy, effect steps, upgrade links | Reference an existing `SkillEffect` or a new `SkillEffect` subclass; HUD still derives from generic timing/targeting policies |
| `StatusDefinition` | `id`, owner scope, duration policy, stack cap, hooks, icon/tooltip keys | Hooks subscribe to named simulation moments; use a dedicated status subclass for unique lifecycle behavior |
| `EncounterDefinition` | `id`, enemy ID, seed policy, allowed gems, reward/XP rules | Progression can remain a sequence now and become a map later |

### Extension pattern

Use small, behavior-specific base types at the domain boundary—for example `GemEffect`, `SkillEffect`, `IntentEffect`, `TargetPolicy`, and `StatusBehavior`. A new gem or skill may introduce a subclass of the relevant type.

Each behavior receives an immutable resolution context and returns explicit state changes/events; it must not reach into UI, `MonoBehaviour`, save services, or global managers. The content definition references the behavior, and the central resolver invokes the shared base contract. This makes new mechanics easy to add while keeping order of operations testable and avoiding a growing `switch` statement in `GameManager`, `BoardController`, or `EncounterController`.

**Rule of thumb:** subclass the smallest behavior that owns the rule; do not subclass a controller just to add content. A subclass needs a deterministic unit-test fixture before it is accepted into the content registry.

### Command and event boundary

Presentation may send only commands:

```text
SwapCommand(cellA, cellB)
UseSkillCommand(skillId, targets)
SelectRewardCommand(rewardId)
EquipSkillCommand(skillId, slotIndex)
ContinueCommand()
```

The application layer validates timing, asks the simulation to resolve, records a checkpoint, then publishes an immutable batch of events. Initial event names:

```text
BoardInitialized, SwapAccepted, GemsMatched, GemCleared, SpecialCreated,
SpecialActivated, GemMoved, GemSpawned, BoardReshuffled,
DamageApplied, StatusAdded, EnemyIntentStarted, EnemyDefeated,
XPGranted, LevelUpOffered, SkillChosen, RunEnded, StatusRemoved,
ResourceChanged, CooldownChanged, EnemyIntentTelegraphed,
SkillUsed, SkillEquipped
```

Board events use `cell` as an origin/location and optional `targetCell` for a destination, serialized with explicit `hasCell` / `hasTargetCell` flags. `relatedId` carries the secondary content identity (for example, a gem's special ID). `GemCleared.statusIds` snapshots the statuses present immediately before the gem left the board; this lets Cracked suppression remain deterministic after the board snapshot has already refilled. Clearing a status-bearing gem emits `StatusRemoved` before its `GemCleared` event. A rejected swap returns a typed rejection and an empty event batch, changes neither board nor RNG state, and does not advance `ResolvedTurnCount`. The combat turn resolver advances `ResolvedTurnCount` exactly once after each accepted swap.

For active-skill timing, zero or more valid `UseSkillCommand`s may resolve while the encounter is awaiting the player's swap. `BeginSwap` resolves the board and player effects, then opens the internal pending-response state when the enemy survives. Presentation immediately maps that state to `CompleteTurn` after board animation, executing exactly one enemy response without another player command. The convenience `ResolveSwap` API still performs both simulation phases for non-presentational callers. A pre-swap skill defeat closes the turn without an enemy response. A stable checkpoint may be written after a pre-swap skill, but never while the internal pending-response state is open.

**Integration invariant:** the simulation owns truth. A view can animate a predicted swap but must reconcile to the event batch and resulting snapshot. Never calculate final damage from animation callbacks.

---

## 10. Determinism, saves, and observability

| Concern | MVP decision | Reason |
| --- | --- | --- |
| Randomness | One seeded deterministic RNG per run; named streams for board spawn, reward sampling, and intent variation | Reproducible bugs and balance cases |
| Checkpoints | Save after encounter start, resolved player/enemy turn, victory, and reward/skill choice | Never serialize half-finished animations |
| Save payload | `schemaVersion`, `contentVersion`, seed/RNG states, encounter, player, board, enemy, XP/level, learned and equipped skills, cooldowns, pending choice, pending combat command window | Migration and local debugging |
| Content versioning | Stable string IDs; never Unity asset instance IDs as save keys | Saves and test fixtures remain readable |
| Debug log | Seed, commands, event batches, final state hash in development builds | Makes desyncs and balance reports actionable |

### Named simulation hooks

`OnTurnStarted`, `OnSwapValidated`, `OnMatchDetected`, `OnGemCleared`, `OnCascadeEnded`, `OnPlayerResolutionEnded`, `OnEnemyResponseStarted`, `OnDamageApplied`, `OnStatusApplied`, `OnEntityDefeated`, `OnVictory`, `OnRewardSelected`.

Content may subscribe through a controlled `EffectStep` / `StatusHook` registry only. This prevents each new skill from creating untestable chains of scene callbacks.

---

## 11. Build plan for separate sessions

| Session | Deliverable | Depends on | Definition of done |
| --- | --- | --- | --- |
| A — Foundation | Project shell; domain state, IDs, RNG, event log, test harness | Architecture and save rules | Scripted scenario produces repeatable state hash and event log |
| B — Board | 7×7 simulation: swaps, matching, special creation, cascades, refill, reshuffle | Board rules | Seeded moves resolve deterministically with no soft-lock cases |
| C — Combat/enemies | Damage pipeline, statuses, intent cycle, five encounters | Combat/enemy rules | Enemy behavior works entirely through definitions; ordering tests pass |
| D — Progression | XP, upgrade selection, six nodes, actives, rewards | Progression rules | Eligibility/effects persist across encounters |
| E0 — Stub assets | Source or generate temporary visuals/audio for the vertical slice; record licenses and attribution | UX, status rules, and content IDs | Every MVP gem, special, status, enemy, intent, and HUD state has a readable placeholder asset; asset ledger is complete |
| E — Mobile presentation | Board/HUD/input/animation shell, accessibility labels, save-resume | E0, UX + architecture | One run works on target portrait aspect ratios |
| F — Balance/QA | Fixtures, telemetry export, tuning sheet, playtest revisions | Full MVP | Ten clean runs and an updated balance snapshot |

### Handoff rule

Each session updates its own section and adds a short **Changed contracts** note. Any change to an ID, event ordering, timing window, or save field must be updated here before dependent sessions proceed.

### Session E0 — Stub asset sourcing and generation

Prepare only the temporary assets needed to make the vertical slice readable and playable; final art direction and production audio remain out of scope. E0 must finish before presentation integration begins, so visual readability can be tested on device instead of being deferred to polish.

| Asset group | Minimum temporary coverage | Preferred acquisition route |
| --- | --- | --- |
| Board | Ember, Tide, Venom, Volt, Prism, and clear visual distinctions for Spark, Current, Spore, and Charge | Generate simple original sprites or source permissively licensed icon/sprite assets |
| Board statuses | Frozen, Cracked, Anchored overlays, duration counters, and tooltipped status icons | Generate or create simple original overlays; verify contrast against every gem color |
| Enemies and intents | One readable portrait/silhouette for each of the five encounter entries and an icon for each visible intent | Source permissively licensed placeholders or generate non-final original art |
| HUD and progression | Player/enemy health, Focus, Toxic, Poison, Shield, active-skill, level-up branch, and reward-card icons | Use original vector/UI primitives first; source icons only where needed |
| Feedback | Clear, hit, shield, status-added/removed, victory, and defeat feedback; optional placeholder SFX | Use simple generated/procedural visuals and free-to-use temporary audio where available |

#### Asset sourcing rules

- Search free asset libraries first when they can provide a consistent, readable set. Accept assets only with a license compatible with commercial game distribution (for example CC0, CC-BY with recorded attribution, or an equivalent permissive license).
- Record every non-original asset in an asset ledger with source URL, creator, license, required attribution text, download date, and the in-project path. Do not rely on a marketplace's “free” label without confirming its license.
- When a suitable free asset cannot be found quickly, generate an original placeholder or use a simple shape-based temporary asset. Generated assets must avoid third-party characters, logos, and recognizable franchise styles.
- Keep each temporary asset mapped to a stable content ID or UI role, not to a filename convention alone, so final assets can replace it without changing simulation or save contracts.
- Test gem and status combinations at 16:9 and 20:9 portrait sizes. Placeholder art succeeds only if players can distinguish gem color, special state, and board status at normal play distance.

#### Selected E0 asset kit

The temporary asset selection, stable content/UI-role mappings, generation prompts, licenses, and required attribution are recorded in [ASSET_LEDGER.md](ASSET_LEDGER.md). E0 uses a CC0 match-3 gem set, CC0 Kenney UI/audio/VFX packs, CC-BY Lorc icons from Game-icons.net, and five project-generated enemy portraits. Session E must preserve the ledger mappings and expose the recorded Lorc credit from an accessible Settings or Credits view.

### Session E — Mobile presentation and save-resume

The MVP uses a single portrait UI Toolkit scene generated by `SectionEBuilder`. The scene references a serialized `PortraitPanelSettings` asset and the project-owned `PortraitRuntimeTheme.tss`, so Unity packages its runtime text/ICU dependencies and default control styles; panel settings must not be created only at runtime. `ThreeInARowApp` creates the responsive safe-area layout and derives every screen from the application-owned `RunDirector`; scene views never calculate damage, status outcomes, rewards, or enemy actions.

#### Screen and input flow

- Title exposes Start Run, Resume when a compatible stable checkpoint exists, How to Play, Settings, the reduced-motion toggle, build version, and required asset credits.
- Encounter displays the enemy portrait, live HP, Poison, next-intent icon and exact effects, the 7×7 board, player resources, equipped active skills, cooldowns, and current command-window guidance.
- Board input supports tap-then-tap and directional swipe. The presentation sends only adjacent-cell `SwapCommand` data; rejected swaps retain the current state, explain the rejection, and play invalid feedback.
- Ready active skills are available before a swap. A successful swap locks board input while swap, clear, gravity, and refill movement is animated from the ordered board events; if the enemy survives, presentation dispatches `CompleteTurn` automatically and keeps input locked through the enemy response.
- Cleanse enters a cancelable target mode. Status-bearing cells can be selected or deselected, confirmation accepts one to three unique targets, and an empty selection means cleanse all only when no more than three eligible cells exist.
- Level-up options and between-encounter loadout controls are generated from the generic skill definitions. A newly learned Catalyze can replace either active while preserving every learned active's cooldown.
- Every skill has a tap-accessible detail view that explains whether it is active or passive, its exact trigger, numeric effect, targeting, and cooldown. Level-up cards open this detail view before confirming a reward, and disabled or cooling-down combat skills retain an enabled information control.
- How to Play is available from the title, settings, and encounter header. It summarizes turn flow, resources, crystals, special crystals, board statuses, and every active and passive skill, then returns to the screen that opened it without changing run state.
- Victory and defeat summaries display encounters cleared, largest cascade, resolved turns, total damage, damage by source, and chosen upgrades. Development builds also display the run seed.

#### Responsive and accessible presentation

- The canvas scales from a 1080×1920 reference and applies the device safe area. The square board is capped independently of screen height so both 16:9 and 20:9 portrait layouts retain the enemy intent, resources, active controls, and generous cell hit regions.
- Gem and special identity is carried by both distinct artwork and descriptive focus/tool-tip text. Every board-status overlay is an independent tappable control with its full rule; timed status counters remain live text.
- Enemy intents, health, resources, cooldowns, reward effects, prerequisites, and summary values are live text rather than baked art. Buttons expose descriptive tooltips and visible action labels.
- Reduced Motion removes nonessential event pacing and board shake while preserving state cues, ordered feedback text, and input locks.

#### Checkpoint boundary

The local checkpoint is a versioned JSON envelope at `Application.persistentDataPath/run-checkpoint.json`. It stores the complete domain `RunState` plus run-summary statistics through explicit primitive DTOs, preserving stable content IDs and unsigned RNG values without relying on Unity serialization of readonly structs.

Checkpoints are replaced after run start, encounter start, a fully resolved player/enemy turn, victory, reward selection, and loadout change. No checkpoint is written while `PendingCombatTurn.AwaitingEnemyResponse` is true. Resume validates both the envelope schema and domain schema, requires a complete 49-cell board, restores generic progression invariants, and derives the correct encounter, reward, between-encounter, victory, or defeat screen from authoritative state.

---

## 12. Test plan and acceptance criteria

| Category | Minimum cases |
| --- | --- |
| Board | Invalid swap costs no turn; 3/4/5 matches create correct results; simultaneous matches resolve once; cascades terminate; reshuffle makes a playable board |
| Gem effects | Every per-clear amount, threshold, Prism interaction, special effect, and Volt cooldown rule has deterministic unit tests |
| Combat order | Poison can kill before intent; Shield absorbs before HP; dead enemy never acts; a Frozen gem clears and resolves normally |
| Enemy statuses | Frozen blocks only swap; Cracked suppresses only gem effect; Anchored blocks fall/swap but clears in match; duration UI works |
| Progression | XP thresholds, prerequisites, no duplicate choices, skill persistence, cooldowns, and save/resume all work |
| End-to-end | A seeded run can clear all five encounters; defeat works; each checkpoint resumes to the same final state hash |
| Mobile UX | 16:9 and 20:9 portrait layouts; touch prevents accidental swaps; reduced-motion skips nonessential waits |

### Development telemetry

- Run seed, encounter, turn count, outcome, player HP at start/end, selected upgrades.
- Damage by gem/special/active skill, cascades per turn, largest clear, Prism count, and status applications/removals.
- Invalid swaps, reshuffles, skill timing, and the intent that caused defeat.
- State hash after every resolved turn plus an exportable command/event replay log.

---

## 13. Open decisions & current blockers

There is **no design blocker** for Session E. The unresolved items below should be settled during Session F playtests rather than speculative design.

| Decision | Why it matters | Recommended validation |
| --- | --- | --- |
| Exact damage and health values | Cascades make spreadsheet estimates misleading | Play 30 seeded runs across baseline and each build branch; tune turn-to-kill and survival targets |
| Post-cascade skill window | Changes pacing and whether skill targeting feels strategic or interruptive | Prototype Sunder/Cleanse post-cascade and compare against pre-swap in five usability tests |
| Gem status overlay visual language | Frozen/Cracked/Anchored must be distinguishable on a small screen | Contrast/readability review on device with placeholder art |
| Portrait board cell size vs. HUD density | Touch comfort competes with intent readability for vertical space | Greybox at 16:9 and 20:9; test one-handed reach and accidental swaps |

## Next session

Begin **Session F — Balance/QA**: exercise the finished portrait run on 16:9 and 20:9 targets, capture telemetry from seeded runs, tune combat pacing, and complete the ten-run acceptance pass.

## Changed contracts — Session A

- The initial save schema is version `1`. `RunState` persists `schemaVersion`, `contentVersion`, the run seed, named RNG-stream states (`BoardSpawn`, `RewardSampling`, `IntentVariation`), encounter/turn counters, player/enemy state, board-state placeholders, selected skills, and a pending choice.
- Content/save references use stable ordinal string `ContentId` values (for example, `gem.ember` and `enemy.geode_mite`); Unity asset instance IDs are never persisted.
- The initial command contracts are `SwapCommand`, `UseSkillCommand`, `SelectRewardCommand`, and `ContinueCommand`. The initial event types and their order are represented by an immutable-facing `EventBatch`.
- Session A's scripted foundation scenario is intentionally limited to the contract pipeline. It emits a four-event log and a SHA-256 state hash; Session B replaces the scripted match with authoritative board resolution.

## Changed contracts — Session B

- `BoardGemState` now persists `specialId` separately from its base `gemId`; normal gems use `special.none`, while Prism uses `gem.prism` plus `special.prism`. Board cells remain a complete 49-entry row-major snapshot after every committed operation.
- The save schema advances to version `2` and the default content version to `0.2.0` for the persisted `specialId` field. A schema-1 board gem migrates with `special.none`.
- Board initialization and accepted swaps consume only the named `BoardSpawn` RNG stream. Swap resolution is transactional: validation failures return a typed rejection with no events and do not mutate the board or RNG state.
- Board event payloads now include optional `targetCell` and `relatedId`; cell presence uses Unity-serializable `hasCell` / `hasTargetCell` flags rather than nullable fields. Session B adds `BoardInitialized`, `SpecialActivated`, `GemMoved`, `GemSpawned`, and `BoardReshuffled` to the event contract; `GemCleared.amount` is the cleared-cell count (`1` per event), not damage.
- Match groups, special origin, clear order, gravity/refill events, legal-swap enumeration, and reshuffle attempts all use deterministic row-major ordering. The Session B handoff scenario initializes from a seed, resolves the first legal row-major swap, and hashes the resulting board, RNG state, and event batch.

## Changed contracts — Session C

- The combat resolver consumes the board event batch and emits one combined deterministic batch. Each board event is copied in order and its resulting gem, special, resource, cooldown, or damage events are inserted immediately after it. An accepted swap advances `ResolvedTurnCount` once; a rejected swap still changes no state and emits nothing.
- `GemCleared` now snapshots its pre-clear `statusIds`, and board clears emit `StatusRemoved` before `GemCleared`. Cracked suppresses the cleared gem's normal effect through that snapshot; Frozen still resolves the gem normally. Board status applications select unique eligible cells using `IntentVariation`, then call the board's deterministic `EnsurePlayable` boundary so new locks cannot create a soft-lock.
- `EnemyState` owns Poison stacks. `PlayerState` now persists `VoltClearProgress`. Timed board statuses persist `(statusId, remainingPlayerTurns)` alongside stable status IDs; Anchored uses one player turn while Frozen and Cracked use duration zero for no automatic expiry.
- The save schema advances to version `3` and default content version to `0.3.0` for enemy Poison ownership, Volt progress, and board-status durations. The state hash includes all three fields plus event status snapshots.
- Five immutable encounter/enemy definitions provide HP, reward XP, and ordered intent cycles. Generic intent effects cover player damage, unique-cell board-status application, and Focus/Toxic drain; the resolver contains no per-enemy execution branches.
- Session C adds `StatusRemoved`, `ResourceChanged`, `CooldownChanged`, and `EnemyIntentTelegraphed`. Poison checks before `EnemyIntentStarted`; Shield absorbs before HP; an enemy killed by player resolution or Poison emits `EnemyDefeated` and `XPGranted` and never executes an intent.

## Changed contracts — Session D

- `RunState` now persists `Experience`, `Level`, learned skills, two ordered equipped-active slots, cooldowns for every learned active, the pending level-up choice and its level, and the post-cascade combat command window. The save schema advances to version `4` and default content version to `0.4.0`; all new fields participate in the deterministic state hash.
- Run initialization learns and equips Sunder and Cleanse. Catalyze participates in the deterministic level-up reward pool, becomes learned when selected, and may replace either equipped active only between encounters through `EquipSkillCommand`; an unequipped active retains its cooldown.
- Level-up thresholds are cumulative XP 2, 3, and 4. Eligible unowned rewards include passive nodes with satisfied prerequisites and active definitions flagged for level-up. Three options are sampled without replacement through `RewardSampling` and persisted until a valid `SelectRewardCommand` resolves them.
- The six passive nodes are generic content modifiers consumed by combat: Ember clear damage, Spark Shield, Focus-conversion damage and left cooldown reduction, Poison damage per stack, and Volt clear threshold. The resolver contains no passive-skill ID branches.
- Session D originally introduced a post-cascade active-skill window between `BeginSwap` and `CompleteTurn`; the later automatic-response contract moves player-facing active use before the swap while retaining the same internal simulation boundary. Sunder, Cleanse, and Catalyze resolve through generic target-policy/effect definitions. A newly used skill does not tick on its activation turn.
- Session D adds `SkillUsed` and `SkillEquipped`. `SkillChosen` records the resolved reward and its choice ID; active effects continue to express outcomes through `DamageApplied`, `StatusRemoved`, `StatusAdded`, `ResourceChanged`, and `CooldownChanged`.

## Changed contracts — Session E0

- Temporary presentation assets are selected and mapped to every MVP gem, special, status, enemy, intent telegraph, HUD resource, skill, and feedback role in `docs/ASSET_LEDGER.md`; these mappings do not change simulation or save IDs.
- Composite intents reuse multiple mapped effect icons, and all values, cooldowns, durations, and stack counts remain live text rather than baked artwork.
- Session E must include the ledger's Lorc/Game-icons.net CC-BY 3.0 credit in an accessible Settings or Credits view. All other sourced E0 packs are CC0; generated enemy portraits have recorded prompt provenance.
- E0 texture and audio import defaults are enforced by `E0AssetImportSettings`; runtime presentation code should reference imported sprites and clips through content IDs or explicit UI roles, never raw filename conventions.

## Changed contracts — Session E

- `RunDirector` is the presentation-facing command boundary. It owns run/encounter transitions, derives stable screens from `RunState`, records summary statistics from immutable event batches, and is the only layer that requests checkpoints.
- The local save adds envelope schema `1`, containing domain schema `4` state and run statistics. Content IDs and `ulong` seed/RNG values are serialized through explicit primitive DTOs; incompatible, corrupt, incomplete-board, and mid-command-window checkpoints are rejected instead of partially restored.
- Stable checkpoints are written after run start, encounter start, pre-swap active-skill use, fully completed turns, victories, reward choices, and loadout changes. The internal pending enemy-response state remains deliberately unsaved, preserving the no-half-turn checkpoint invariant.
- The E0 asset ledger mappings are compiled into `E0PresentationCatalog`, keyed only by stable content ID or explicit UI/feedback role. Presentation code never selects an asset by filename.
- Session E introduces one generated build scene, `PortraitGame`, and one runtime UI Toolkit root. It supports portrait safe areas, tap/swipe board commands, generic active targeting, event-paced input locks, reduced motion, status rule controls, and the required Lorc attribution links without changing simulation event ordering.

## Changed contracts — automatic response, board motion, and Russian UI

- Active skills are selected before the player's swap. A used skill remains marked for that turn so its cooldown does not tick when the following swap and automatic enemy response complete.
- The presentation automatically completes the pending enemy response after the player-resolution animation. The former Enemy Response button and player-facing post-cascade skill window are removed.
- `SwapAccepted`, `GemCleared`, `GemMoved`, and `GemSpawned` events drive visible swap, disappearance, gravity, and refill animations. Swaps use a zero-velocity start/end curve; gravity/refill timing scales sublinearly with travel distance and eases into its landing. Moving gems are temporarily hosted by the board foreground layer and docked into their destination cells when motion completes, preventing transform accumulation between cascades. Reduced-motion mode applies the same event reconciliation without travel animation.
- All player-facing runtime labels, descriptions, status rules, errors, tooltips, credits, and combat feedback are Russian. Stable content IDs, save fields, and simulation event details remain language-neutral.
