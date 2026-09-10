# Content Expansion and Retention Roadmap

**Status:** R1–R8 implemented in content version `1.0.0`; the experimental shelf remains future-facing
**Depends on:** [GDD.md](GDD.md), especially the deterministic content, command, event, and checkpoint boundaries  
**Planning principle:** Ship the smallest stage that creates a fresh run story, a new board question, or an appealing reason to return.
**Current planning mode:** The `1.0.0` expansion was selected and built by player feel and production leverage, without a KPI or analytics gate.

## 1. Why this order

The current vertical slice can prove that one battle is satisfying, but a second run changes too little:

- the same five enemies appear in the same order;
- the first build choice arrives only after two victories;
- six passives produce shallow and uneven branches: Ember and Tide have two nodes, while Venom and Volt have one each;
- only Catalyze can enter the active-skill loadout during a run;
- there is no route, event, risk/reward, discovery, or long-term mastery layer.

The highest-retention sequence is therefore:

1. **Create an obvious second-run difference cheaply.** Add encounter pools, earlier build identity, and enough skills to form real archetypes.
2. **Add route agency.** A map becomes valuable only when its branches offer meaningfully different content.
3. **Add reasons to return tomorrow.** Use horizontal discoveries and visible goals without permanent stat grinding.
4. **Add mastery.** Difficulty modifiers and challenge seeds serve players who already enjoy the core.
5. **Add breadth last.** A second act, shops, relics, or multi-enemy combat are expensive multipliers, not first fixes.

This plan intentionally does not start with a new normal gem. Four colors already create a readable tactical board; a fifth color would reduce match frequency, destabilize every balance assumption, and make existing builds less reliable.

## 2. Historical R0–R5 retention ladder and measurement

This section records the reasoning used for the implemented roadmap. It was not used as a gate for the vibe-first R6–R8 implementation.

| Retention moment | Player question | Design answer | Primary signal |
| --- | --- | --- | --- |
| First 2 minutes | “Do my choices matter?” | Fast first victory, readable intent, first upgrade after encounter 1 | Encounter-1 completion; time to first upgrade |
| End of first run | “What could I try differently?” | Distinct branch identities, unseen enemies and actives still in the pool | Voluntary second-run start; untried content visible on summary |
| Runs 2–5 / D0 | “Will this run unfold differently?” | Seeded encounter pools, route choices, events, elites, alternate boss | Unique nodes/enemies per run; route-choice distribution |
| D1–D3 | “What am I coming back to unlock or discover?” | Horizontal content unlocks, codex, explicit challenges | D1 return; challenge progress; newly discovered content |
| D7+ | “Can I master this?” | Difficulty tiers, challenge seeds, build records, boss variants | D7 return; difficulty adoption; win rate by tier/build |

Until production analytics exist, use local telemetry plus observed playtests as proxies. Every stage must preserve seed, command log, and state-hash capture so a retention improvement is not purchased with simulation instability.

### Required funnel events before evaluating expansion

- `run_started`, with seed and unlocked-content-set version;
- `encounter_started` and `encounter_ended`, with source node, enemy, depth, turns, HP, and result;
- `reward_offered` and `reward_selected`, with all options, eligibility tags, and selection time;
- `map_node_offered` and `map_node_selected`, with visible alternatives;
- `event_choice_selected`, with event and all offered choices;
- `run_ended`, with route, build, cause, duration, and whether another run starts in the same session;
- `content_discovered` and `challenge_completed` once persistence exists.

Do not use raw D1 or D7 percentages from small internal tests. For Stages 0–2, prioritize observed comprehension, second-run intent, choice distribution, and seeded-run balance. Use real retention cohorts only after distribution and consent-compliant analytics are available.

## 3. Roadmap at a glance

| Stage | Retention target | Main delivery | Relative effort | Start gate |
| --- | --- | --- | --- | --- |
| **R0 — Baseline truth** | Activation | Finish balance/QA, instrument the funnel, remove first-run friction | Small | Current vertical slice is playable end to end |
| **R1 — Build identity and run-two variety** | End of first run / D0 | Earlier first reward, 12-passive tree, 5 actives, depth-based enemy pools | Medium | R0 data identifies no major clarity or pacing failure |
| **R2 — Meaningful paths** | Runs 2–5 / D0–D1 | Compact seeded run map, events, rest, elites, alternate boss | Large | R1 content produces at least three viable build identities |
| **R3 — Reasons to return** | D1–D3 | Horizontal unlock challenges, codex, hybrid skills, discoveries | Medium | Players voluntarily replay and can explain route/build differences |
| **R4 — Mastery** | D7+ | Difficulty ladder, challenge seeds, advanced enemies and bosses | Medium | Normal-mode win rate and dominant builds are understood |
| **R5 — Breadth expansion** | Long tail | Two additional regions; evaluate shop, charms, or multi-enemy combat separately | Very large | Implemented in content `0.9.0` |
| **R6 — Complete journey** | Mid-run renewal | Sanctum, eight evolutions, six regional keystones, route vows | Large | Implemented in content `1.0.0` |
| **R7 — Regional stories** | Run-to-run surprise | Eight regional events, two story links, eight later-region enemies | Large | Implemented in content `1.0.0` |
| **R8 — Return layer** | Short and self-directed replay | Daily expeditions, twelve trials, constellations, archive, lore | Large | Implemented in content `1.0.0` |

## 4. Stage R0 — Baseline truth

**Goal:** Establish whether the current game loses players because of clarity, pacing, difficulty, or lack of variety. Content cannot repair an unclear first turn.

### Deliverables

- Complete Session F balance/QA and ten deterministic clean runs.
- Add the retention funnel events listed above to the existing local telemetry export.
- Record median and spread for encounter turns, damage taken, invalid swaps, active-skill use, upgrade selection, run duration, victory, and defeat.
- Run at least five fresh-player observed sessions and five experienced-player replay sessions.
- Fix only high-severity comprehension and pacing issues before expanding the pool.

### Exit gate

- At least 80% of fresh testers can explain valid swap → cascade → gem effect → enemy intent after encounter 1 without coaching.
- Median first run remains within 8–12 minutes, or a deliberate new target is approved before the map changes run length.
- No enemy, status, skill, or reward has an unexplained effect in the UI.
- A baseline is recorded for first-run completion and voluntary second-run starts.

## 5. Stage R1 — Build identity and run-two variety

**Retention hypothesis:** A player starts another run when the first run reveals credible builds and leaves obvious combinations unexplored.

### 5.1 Progression changes

- Move the three standard reward thresholds from XP `2 / 3 / 4` to `1 / 2 / 4`. The first choice should appear roughly two minutes into play, while the third remains a mid/late-run payoff.
- Keep three offers per reward. Add deterministic offer shaping so, when eligible, an offer contains:
  - at least two different branch tags;
  - no duplicates or ineligible prerequisite nodes;
  - at most one generic active unless the player has fewer than three learned actives.
- Show one-line synergy tags such as **Spark**, **Focus**, **Poison**, **Cooldown**, **Shield**, and **Board control** on reward cards.
- Keep two active slots. More slots would reduce loadout tension and mobile readability.

### 5.2 Complete the four branch identities

The six candidates below bring every branch to three passives. Values are tuning seeds.

| Branch | New skill | Prerequisite | Proposed effect | Purpose |
| --- | --- | --- | --- | --- |
| Ember | **Cinderwake** | Backdraft | The first Spark activated each player turn deals +8 damage | Makes special setup the Ember capstone |
| Tide | **Reservoir** | Flow State | Each Focus damage conversion grants 2 Shield | Turns frequent thresholds into sustain |
| Venom | **Concentrate** | None | Toxic triggers at 4 instead of 5 | Creates an accessible Venom opener |
| Venom | **Contagion** | Concentrate | A 4+ Venom match adds 2 Toxic after its normal clears | Rewards deliberate large matches |
| Volt | **Static Guard** | None | Each active-cooldown reduction grants 2 Shield, once per resolution step | Makes cooldown play defensively viable |
| Volt | **Live Wire** | Static Guard | Charge reduces both active cooldowns by 1 additional turn | Makes Volt special setup the capstone |

The exact wording “once per resolution step” must be resolved into an explicit event trigger before implementation. It exists to prevent one cooldown event affecting two slots from granting Shield twice accidentally.

### 5.3 Expand active skills from three to five

| Active | Cooldown seed | Effect | Contract impact |
| --- | --- | --- | --- |
| **Aegis** | 4 | Gain 10 Shield | Add generic `GainShield` active effect; no targeting work |
| **Infuse** | 6 | Select one movable normal gem and convert it into its color’s match-4 special | Add `OneNormalGem` target policy and generic board transform effect |

Aegis is the low-risk defensive choice. Infuse is the important board-first choice: it changes how the player plans the next swap and validates generic cell-targeting before later map rewards depend on it.

Sunder and Cleanse remain guaranteed starters during R1. Catalyze, Aegis, and Infuse enter the reward pool. Do not add more actives until offer frequency and equip rates are measured.

### 5.4 Replace fixed middle encounters with depth pools

Keep five encounters and the Crystal Warden finale in R1, but select one eligible enemy for each non-boss depth through a new named `EncounterSelection` RNG stream. Persist the four selected encounter IDs at run creation so resume and replays never resample them.

| New enemy | Eligible depths | Intent-cycle seed | Tactical role |
| --- | --- | --- | --- |
| **Crystal Tick** — 56 HP | 1–2 | Drain 1 Focus/1 Toxic → Bite 6 → Crack 2 | Early resource pressure without a hard lock |
| **Rime Moth** — 70 HP | 2–3 | Freeze 1 + hit 4 → Needle 7 → Freeze 2 | Mixed pressure; introduces combined intents gently |
| **Anchor Crab** — 86 HP | 3–4 | Anchor 2 → Claw 8 → hit 5 + Crack 2 | Teaches Anchored before the boss and values Cleanse |
| **Hollow Idol** — 94 HP | 4 | Drain 2/2 → Crack 3 → Bolt 10 | Late build check against resource hoarding |

These enemies deliberately reuse current generic intent effects. R1 should validate variety from new sequences and combinations before adding more debuff types.

Suggested pool shape:

- Depth 1: Geode Mite or Crystal Tick.
- Depth 2: Frost Oracle, Crystal Tick, or Rime Moth.
- Depth 3: Geode Mite Elite, Rime Moth, or Anchor Crab.
- Depth 4: Prism Stalker, Anchor Crab, or Hollow Idol.
- Depth 5: Crystal Warden.

Apply simple anti-repetition rules: do not select the same enemy twice, and do not select more than two encounters whose primary pressure uses the same board status.

### 5.5 R1 contract work

- Add stable IDs for the six passives, two actives, four enemies, intents, and encounter variants.
- Extend passive modifier and active effect enums only with generic behaviors.
- Add persisted selected encounter IDs and `EncounterSelection` RNG state; advance domain and checkpoint schemas with migration or reject older development saves explicitly.
- Make reward offer shaping deterministic and include its decisions in replay/state-hash tests.
- Update Russian presentation text, detail views, content mappings, and the asset ledger for every new item.

### R1 exit gate

- At least three coherent builds—Ember special, Tide sustain, and Venom or Volt engine—can win seeded baseline runs without one exceeding the others by more than 15 percentage points across the test set.
- In observed tests, at least 60% of players voluntarily start a second run or explicitly choose a different build they want to try next.
- No single reward is selected above 60% when offered, except a temporarily documented balance outlier.
- Each run exposes at least two enemies not seen in the immediately previous seed under the standard test-seed suite.

## 6. Stage R2 — Meaningful paths

**Retention hypothesis:** Route choice turns replayable combat content into a run story: recover, take a risk, hunt a build piece, or prepare for the boss.

### 6.1 Compact map scope

Build one region with **seven visited rows** and a 10–15 minute target:

1. mandatory normal combat;
2. choice among normal combat and event;
3. mandatory normal combat;
4. choice among normal combat, elite, and rest;
5. choice among event and rest;
6. mandatory normal combat;
7. one of two visible bosses, selected for the generated map.

The map may generate two or three nodes per row with forward connections. Generate the complete topology and assignments once from a named `MapGeneration` RNG stream, then persist it. Node type, visited state, available connections, and the next boss are visible. Combat nodes show enemy family and dominant pressure icon so a route choice can be informed without revealing the full intent cycle.

Avoid scrolling complexity in the first version: show the full region on one portrait screen, highlight reachable nodes, and allow inspection before selection.

### 6.2 Node types and reward budget

| Node | Result | Reward/power budget |
| --- | --- | --- |
| Normal combat | Standard encounter from depth pool | 1 XP and victory heal |
| Elite combat | Hard enemy with combined pressure | 1 XP plus one choice from a small elite-keystone pool |
| Event | Two or three explicit choices | Expected value below elite but above rest when accepting risk |
| Rest | Safe recovery choice | Heal 12 HP **or** remove all board statuses and reduce equipped cooldowns by 2 |
| Boss | Region finale | Run victory in R2; unlock/milestone credit in R3 |

All routes must contain the same number of mandatory normal combats. Optional combat may offer more power, but a rest/event route must remain a credible survival choice rather than a trap.

### 6.3 Initial event set

Events should use a generic choice/effect grammar and show exact outcomes before confirmation.

| Event | Choice A | Choice B | Design use |
| --- | --- | --- | --- |
| **Faceted Altar** | Lose 8 HP; immediately draft one eligible passive | Leave | Clear health-for-power tradeoff |
| **Quiet Pool** | Heal 10; set Focus and Toxic to 0 | Preserve resources and leave | Tests resource valuation |
| **Static Loom** | Set equipped cooldowns to 0; apply Cracked to 4 eligible gems | Leave | Power now versus board cost |
| **Prism Echo** | Create one Prism on an eligible cell; lose 5 HP | Heal 5 and leave | Visible board payoff versus safety |
| **Frozen Reliquary** | Learn one offered active; apply Frozen to 3 eligible gems | Cleanse all board statuses | Loadout versus recovery |
| **Cracked Cache** | Take a two-option reward draft; next encounter starts with 3 Cracked gems | Gain 6 Shield for the next encounter | Build greed versus tempo |

“Next encounter” effects require an explicit persisted pending-modifier list. Do not implement them as presentation flags.

### 6.4 Elites and alternate boss

| Enemy | HP seed | Intent-cycle concept | Purpose |
| --- | --- | --- | --- |
| **Fracture Golem** | 112 | Hit 7 + Crack 2 → Anchor 2 → hit 11 | Tests cleanup under damage pressure |
| **Stormglass Roc** | 108 | Freeze 2 → hit 6 + drain 2/2 → hit 10 | Tests active timing and resource resilience |
| **Facet Engine** (boss) | 132 | Anchor 2 → hit 9 + Crack 2 → Freeze 2 + drain 2/2 → hit 13 | Alternate finale using known rules in a new cadence |

R2 does not need boss phases. A second readable intent cycle creates more value per implementation hour than a bespoke half-health state. Boss phases can enter R4 after the base boss pool is balanced.

### 6.5 Elite keystone candidates

An elite keystone is a skill-definition subtype or tagged passive, not a separate relic inventory.

- **Tempered Core:** victory healing increases from 4 to 7.
- **Prismatic Start:** the first encounter board refill after entering combat guarantees one eligible match-4 special, with deterministic placement.
- **Rapid Casting:** newly used active skills begin at one less cooldown, minimum 1.
- **Hard Light:** excess Shield at expiry converts to damage at 1 damage per 2 Shield, capped at 8.

Only one elite keystone may be earned in the initial map. Each candidate needs an exact simulation trigger and cap before becoming a locked GDD rule.

### 6.6 R2 contract work

- Add `MapState`, stable node IDs, node definitions, connections, current node, visited state, boss assignment, and `MapGeneration` RNG.
- Replace numeric encounter advancement with a selected combat-node/encounter ID; preserve encounter depth as data for tuning.
- Add generic event definitions, choice definitions, effect definitions, and pending encounter modifiers.
- Add application screens/commands for map selection and event choice. A selected node is checkpointed before its outcome begins.
- Add map/event telemetry and deterministic fixtures for generation, resume, reachability, no dead ends, and event outcomes.

### R2 exit gate

- At least 70% of observed players pause to compare routes and can explain the tradeoff they selected.
- Each non-mandatory node type is selected in at least 20% of eligible offers across the test set; a consistently ignored node is retuned or removed.
- At least 60% of testers describe their second route as materially different from their first.
- Generated maps have no unreachable node, forced elite, duplicate node ID, missing boss, or route with fewer/more mandatory combats than intended across 10,000 seed tests.

## 7. Stage R3 — Reasons to return

**Retention hypothesis:** Players return when the next goal is visible, attainable, and unlocks a new way to play rather than a permanent numerical advantage.

### 7.1 Horizontal discovery, not stat grind

Add a local profile containing content unlocks, codex discoveries, challenge completion, best difficulty, and aggregate records. Do not add permanent HP, damage, currency income, or upgrade levels.

Initial milestone examples:

- defeat the Crystal Warden → unlock the Facet Engine boss in map generation;
- win with three Ember skills → unlock an Ember board-manipulation active;
- trigger Poison twice in one enemy response → unlock a Venom/Volt hybrid passive;
- remove three different board statuses with one Cleanse → unlock an advanced event;
- defeat an elite without taking HP damage → unlock a challenge card and codex entry;
- win once with each branch as the dominant damage source → unlock difficulty tier 1.

Show at most three suggested next goals on the run summary. Hidden content may show its category and unlock condition; do not use unexplained silhouettes as the only motivation.

### 7.2 Hybrid skill pack

Hybrid rewards require one learned prerequisite from each named branch and upgrade the interaction between systems.

| Skill | Requirements | Proposed effect |
| --- | --- | --- |
| **Flashfire** | Ember + Volt | Spark activation reduces both equipped cooldowns by 1 |
| **Galvanic Venom** | Venom + Volt | Each Poison tick contributes one Volt cooldown-progress point |
| **Scalding Current** | Ember + Tide | Every second Focus conversion in an encounter empowers the next Ember clear by +2 damage per gem |
| **Toxic Undertow** | Tide + Venom | When Focus converts, add 1 Toxic; maximum once per cascade |

This stage changes prerequisites from one optional ID to an array or tag expression. Reward UI must show both requirements and the exact trigger cap.

### 7.3 Advanced active pack

- **Transmute:** recolor one selected movable normal gem to a chosen normal color; cooldown 5.
- **Detonate:** activate one selected match-4 special in place; cooldown 6.
- **Reweave:** reroll up to three selected movable normal gems through deterministic board-spawn rules, then ensure a playable stable board; cooldown 5.

These reuse the generic targeting and board-mutation boundary proven by Infuse. They should not bypass `GemCleared`, `SpecialActivated`, or board-playability event contracts.

### 7.4 Codex and records

- Record discovered gems, specials, statuses, enemies, intents, skills, events, elites, and bosses.
- For enemies, show seen intents only after they have been telegraphed at least once.
- Record wins, best remaining HP, fastest valid-turn count, largest cascade, and dominant damage source by boss/difficulty.
- The codex is informational; it does not contain claimable stat bonuses.

### R3 exit gate

- At least half of returning playtesters can name the goal they are pursuing before starting a run.
- Unlock challenges distribute players across at least three build paths; no single mandatory challenge blocks the map or base victory.
- A fresh profile can always complete a valid run, and every unlock sequence is deterministic and save-safe.

## 8. Stage R4 — Mastery

**Retention hypothesis:** Players who can already win need constrained problems and visible mastery, not simply more HP on enemies.

### 8.1 Five-step difficulty ladder

Unlock one tier at a time after a victory on the previous tier. Each tier adds its rule to earlier rules:

1. **Sharp Edges:** enemy direct damage +1.
2. **Unstable Grid:** each encounter begins with two Cracked gems.
3. **Long Road:** victory healing reduced from 4 to 2.
4. **Hostile Pattern:** elites gain one extra effect in their final intent.
5. **Perfect Facet:** boss gains an explicit second intent phase at 50% HP.

Difficulty rules must be stable content definitions included in the seed, save, replay header, summary, and state hash. Never hide a tier modifier in an enemy-specific resolver branch.

### 8.2 Challenge seeds

- Ship one locally generated weekly seed only after platform date handling and version pinning are reliable.
- Pin content version, unlock policy, map, and difficulty so results are comparable.
- Track local best score using turns, HP, and deterministic tie breakers.
- Do not build online leaderboards until cheating, account identity, moderation, and backend cost are intentionally scoped.

### 8.3 Advanced enemy effects

Only now introduce new enemy effect vocabulary, one mechanic at a time:

- **Enemy Barrier:** visible temporary HP that rewards burst timing.
- **Jammed:** add 1 turn to one equipped active cooldown; target/amount telegraphed.
- **Thorned gem:** clearing it deals a small, capped amount of player damage; it remains matchable and can be cleansed.
- **Boss phase:** deterministic intent-deck switch at a telegraphed HP threshold.

Each new effect needs a distinct icon, rule tooltip, event payload, AI/intent telegraph, save field if stateful, and at least one player counter. Add no more than one unfamiliar mechanic per normal encounter and two per boss.

### R4 exit gate

- Difficulty adoption forms a reasonable spread rather than stopping almost entirely at one tier.
- Each tier lowers win rate without sharply increasing early abandonment or unexplained defeats.
- At least four build families win at the highest tested tier.

## 9. Stage R5 — Breadth expansion

This stage shipped in content version `0.9.0`. Future breadth features should still be evaluated as separate investments rather than bundled automatically.

### Implemented in v0.9: two additional regions

- The run now chains three portrait-sized maps: Crystal Spire, Cinderbloom Wilds, and Voidglass Depths. The first two bosses advance the region; Astral Devourer ends the run.
- The four normal gem colors, active slots, progression catalog, board state, resources, HP, and cooldowns carry through the whole run.
- Ten enemies were added: three normal enemies, one elite, and one boss per new region. Cinderbloom makes the existing Thorned mechanic part of normal encounter pressure; Voidglass combines Jam, Barrier, drain, and mobility statuses at higher intensity.
- Encounter and elite pools are region-local. Events currently reuse the established generic pool so the expansion adds combat breadth without duplicating the event or skill systems.
- A continue/stop checkpoint prompt remains a follow-up pacing option if observed full-run sessions make the automatic transition feel too long.

### Evaluate independently

| Feature | Potential value | Main cost/risk | Recommendation |
| --- | --- | --- | --- |
| Shop + currency | Strong route and resource decisions | Economy tuning, UI, save fields, content pricing | Add only if events/rest cannot create enough route tension |
| Charm/relic inventory | Powerful run identity | Large interaction matrix and tooltip burden | Prototype with 6–8 charms before committing to a large pool |
| Multiple enemies | Targeting and encounter depth | Rewrites intents, damage targeting, UI, AI order, skills, saves | Defer until single-enemy mastery plateaus |
| Fifth normal gem | New build family | Lower match rate and full-board rebalance | Avoid; prefer specials or overlays |
| More than two active slots | More tools per run | Lower equip tension and crowded mobile HUD | Avoid unless testing shows learned actives feel unusable |
| Permanent stat progression | Easy short-term compulsion | Undermines deterministic balance and skill mastery | Do not use as the default meta loop |

## 10. Pre-R6 content read (historical)

The game no longer has a raw-content problem in its opening act. It already has a strong board vocabulary, four readable build branches, hybrid skills, route choices, events, elites, bosses, unlock goals, weekly seeds, and five mastery tiers. Adding a fourth region now would make the game longer without fixing where the existing three-region journey becomes thin.

The most important creative gaps are:

- **Build growth is front-loaded.** Standard upgrades arrive at XP `1 / 2 / 4`, so the build is largely formed in Crystal Spire and changes much less through Cinderbloom Wilds and Voidglass Depths.
- **The later regions are narrower.** Each later region has three normal enemies, one elite, and one fixed boss, while Crystal Spire has broader depth pools and alternate finales.
- **Events do not belong to places yet.** All three regions reuse the same generic event pool, so their visual and combat identities are stronger than their stories.
- **Region transitions lack ceremony.** Defeating a boss immediately opens the next map; there is no satisfying chapter break, build reflection, loadout moment, or comfortable save-and-exit invitation.
- **The return layer eventually runs out.** Ten unlock challenges and five difficulty steps are a good foundation, but most goals are one-and-done and the weekly challenge asks for another full three-region commitment.
- **Routes are choices, not journeys.** Every node connects to every node in the next row, so the player chooses the next reward type but rarely commits to a recognizable path.

R6–R8 were selected to **deepen, not lengthen**: power now continues evolving across the current run, each later region tells its own stories, and short expeditions provide reasons to return.

## 11. Stage R6 — A complete three-region journey

**Creative promise:** Every boss should feel like the end of a chapter and the beginning of a newly evolved build.

**Implementation:** Shipped in `1.0.0` with the full eight-evolution set, six regional keystones, three vow definitions, and a checkpoint-safe Sanctum after both intermediate bosses.

### 11.1 Inter-region sanctum

After the Region 1 and Region 2 bosses, open a calm **Sanctum** screen before generating the next map. It should:

- celebrate the defeated boss and summarize the build's strongest interactions;
- offer one build-evolution draft;
- allow active-skill loadout changes with all cooldowns shown;
- preview the next region's dominant pressures and possible boss families;
- offer **Continue** and **Save & Exit** with equal visual weight.

This is a chapter break, not a shop. It needs no currency, inventory grid, or permanent reward.

### 11.2 Branch evolutions

Add **eight evolution definitions**, two for each core branch. A run may take one after the first regional boss and one after the second. Evolutions require at least two learned skills in their branch and should change board valuation rather than simply add damage.

| Branch | Evolution directions | New question created on the board |
| --- | --- | --- |
| Ember | Spark chains; burning disruption into opportunity | “Do I detonate now or prepare a larger special sequence?” |
| Tide | Store versus spend Focus; convert protection back into tempo | “Do I cross the threshold now or preserve the engine?” |
| Venom | Extend or consume Poison; benefit from deliberate large Venom groups | “Do I race the next Poison tick or build a stronger stack?” |
| Volt | Overclock one active slot; turn excess cooldown progress into another resource | “Which tool am I building this turn around?” |

The shipped pair for each branch follows this accessible/expert split: Sparkstorm/Ashen Aegis, Deep Current/Tidal Memory, Virulent Bloom/Patient Venom, and Overclock/Storm Reserve. Exact values and stable IDs are locked in the GDD.

### 11.3 Regional elite keystones

Keep the current four keystones as the generic pool and add three Cinderbloom and three Voidglass keystones. A player may earn at most one keystone per region, so taking an elite can reshape each chapter without creating a separate relic inventory.

Suggested themes:

- **Cinderbloom:** tame Thorned damage, reward clearing Anchored gems, or turn enemy Barrier breaks into aggressive momentum.
- **Voidglass:** exploit Jam instead of merely suffering it, reward exact cooldown timing, or create new Prism decisions from enemy disruption.

The keystones should use generic hooks already exposed by status removal, Barrier changes, cooldown changes, special activation, and board-resolution batches.

### 11.4 Route vows

At the start of each region, show two optional **Route Vows** such as “defeat the elite,” “visit no rest site,” “clear six status gems,” or “win the boss with both actives ready.” The player may pin one vow for that region and earn an extra evolution option, a codex story, or a cosmetic mark.

Vows should never punish failure or weaken the run. Their job is to give a route a personality and encourage a different style of play.

### 11.5 Implemented slice

The shipped slice includes the whole stage rather than a partial prototype: both evolution sets, loadout changes, Continue/Save & Exit, regional keystones, and optional route vows. Completing a vow expands the next evolution draft from three to four options.

## 12. Stage R7 — Regions with stories and surprises

**Creative promise:** Cinderbloom and Voidglass should feel like places with their own mysteries, not later combat skins using Crystal Spire's event deck.

**Implementation:** Shipped in `1.0.0`; every listed event and enemy fantasy is present, with one persisted two-part story flag in each later region and randomized two-boss finales.

### 12.1 Regional event packs

Add four exclusive events to Cinderbloom and four to Voidglass. Keep several generic events in every region so familiar decisions still anchor the run.

| Region | Event concepts | Tone |
| --- | --- | --- |
| Cinderbloom Wilds | Ember Orchard, Ashen Nursery, Stag's Trail, Rootspeaker Shrine | Living danger, bargains with growth, fire used as renewal |
| Voidglass Depths | Mirror Well, Null Observatory, Echo Prison, Broken Constellation | Reflection, stolen possibilities, unstable time and identity |

Each pack should include one recovery event, one build event, one dangerous board transformation, and one event that changes a later encounter.

### 12.2 Linked story chains

Create one two-part story chain per later region. An early choice adds a small persisted story flag; a later event or boss intro pays it off. The payoff may alter an opening board, reveal a boss intent, change a reward draft, or unlock a codex conclusion.

Story chains should not require finding both halves in the same run. If the second half is absent, the first choice must still be worthwhile on its own.

### 12.3 Later-region roster pack

Bring both later regions closer to Crystal Spire's variety by adding:

- two normal enemies per region;
- one alternate elite per region;
- one alternate boss per region after the normal and elite packs feel settled.

Working fantasies:

| Region | Normal enemies | Elite | Alternate boss |
| --- | --- | --- | --- |
| Cinderbloom | **Sootcap Shaman**, **Glassvine Serpent** | **Ashen Dryad** | **Furnace Matriarch** |
| Voidglass | **Shard Leech**, **Orbit Sentinel** | **Parallax Knight** | **Singularity Seraph** |

These enemies should recombine existing Barrier, Jam, Thorned, Frozen, Cracked, Anchored, drain, and direct-damage grammar before introducing another status. Alternate bosses may have unique phase rhythms, but should not require bespoke combat resolvers.

### 12.4 Boss foreshadowing

Once a regional boss is assigned, let one earlier event, map pressure icon, or enemy line foreshadow its signature problem. The finale then feels like the culmination of the path rather than a random content roll.

## 13. Stage R8 — Reasons to return without chores

**Creative promise:** Opening the game should present an inviting possibility, not an obligation or a lost streak.

**Implementation:** Shipped in `1.0.0` with seven recent daily routes, twelve permanent one-region trials, the weekly Grand Expedition, twelve cosmetic constellation goals, a twenty-run seed archive, and codex lore.

### 13.1 Expeditions

Add a short one-region mode using existing maps, enemies, skills, and difficulty rules. The Expedition Board contains:

- one rotating daily seed with a named modifier and a suggested build fantasy;
- the existing weekly three-region challenge as the long-form “Grand Expedition”;
- a permanent library of handcrafted trials that never expires.

The daily expedition should remain playable after its day passes in a small recent-history list. There are no login streaks, energy, or exclusive power rewards.

### 13.2 Handcrafted trial library

Start with twelve trials:

- four branch trials with a curated opening skill and a board problem suited to that branch;
- four board-craft trials centered on specials, cascades, statuses, and active targeting;
- four boss rematches with unusual starting conditions or restricted loadouts.

Trials should be short enough to invite “one more attempt” and strange enough to teach interactions that normal runs may never demand.

### 13.3 Mastery constellations

Expand the current challenge list into visible constellations of milestones:

- **Explorer:** discover regional events, enemies, and story conclusions;
- **Artificer:** complete special, cascade, and active-skill feats;
- **Pathfinder:** finish route vows and unusual node sequences;
- **Conqueror:** defeat bosses with different branch identities and difficulty rules.

Use a mixture of single-run feats and cumulative progress. Rewards are profile emblems, codex illustrations, board frames, and restrained VFX palettes—never permanent HP or damage.

### 13.4 Run archive and seed replay

Keep the latest twenty run summaries with seed, route, skills, keystones, evolutions, boss results, and memorable records. Allow one-tap replay of a seed and a copyable seed code. This turns good and disastrous runs into stories the player can revisit.

### 13.5 Codex story layer

Add short lore entries unlocked by meeting enemies, resolving linked events, defeating alternate bosses, and completing mastery constellations. Lore should connect existing content rather than become random collectible fragments hidden behind grind.

## 14. Experimental shelf

These ideas can be exciting, but they should not displace R6–R8 unless the team actively wants a larger mechanical reinvention.

| Feature | What it could add | Why it stays experimental |
| --- | --- | --- |
| Shop + one-run currency | Stronger route planning and delayed purchases | Adds an economy to every reward decision and competes with clear event choices |
| Six-to-eight charm prototype | Another build layer with broad combinations | Overlaps evolutions and keystones; interaction and tooltip burden grows quickly |
| Boss rush or endless descent | A home for fully mastered builds | Needs scaling rules and risks flattening handcrafted region pacing |
| Multiple enemies | Target priority and area effects | Rewrites targeting, intents, layout, balance, saves, and much of the skill catalog |
| Fourth region | More world and enemy content | The current run is already long; add only with a different run format |
| Online leaderboards/social layer | Competition and shared stories | Requires identity, anti-cheat, moderation, backend, and privacy scope |

The fifth normal gem, more than two active slots, permanent stat progression, energy, and punitive login streaks remain outside the intended identity of the game.

## 15. Vibe-first priority order

| Priority | Addition | Impact | Effort | Why it belongs here |
| --- | --- | --- | --- | --- |
| 1 | Sanctum + two inter-region evolution drafts | Very high | Medium | Fixes the quiet middle of the 30–45-minute run and gives both boss victories a payoff |
| 2 | Eight region-specific events | High | Medium | Adds place, story, and new choices without new combat systems or portraits |
| 3 | Six regional elite keystones | High | Medium | Makes elite routes and late builds much more personal using existing hooks |
| 4 | Two linked event chains | High | Medium | Creates memorable run stories and payoff across a region |
| 5 | One-region Expeditions | High | Medium | Gives returning players a satisfying short session using existing content |
| 6 | Four later-region normal enemies + two elites | High | Medium/large | Reduces the biggest roster repetition without extending the run |
| 7 | Twelve handcrafted trials | Medium/high | Medium | Turns current mechanics into authored puzzles and mastery goals |
| 8 | Two alternate later-region bosses | High | Large | Makes repeated full clears surprising again after the cheaper pools are richer |
| 9 | Mastery constellations + cosmetic marks | Medium/high | Medium/large | Extends the return layer without power creep or obligation |
| 10 | Run archive, seed replay, and codex stories | Medium | Medium | Helps runs become personal stories and supports self-directed mastery |
| 11 | Shop or charm prototype | Unknown/high | Large | Explore only after evolutions, keystones, and events reveal what is still missing |

## 16. Production slicing

For each selected stage:

1. Lock its rule text, stable IDs, event order, RNG usage, and save changes in the GDD.
2. Implement the smallest complete content pack in pure domain code and deterministic fixtures.
3. Add application flow and checkpoint boundaries.
4. Add Russian text, icons, tooltips, reduced-motion behavior, audio/VFX, and asset-ledger entries.
5. Play it from both a fresh profile and an all-content profile, across a complete three-region run.
6. Tune or cut anything that is technically functional but does not create a new story, board question, or build fantasy.

## 17. Locked R1–R8 implementation decisions

- Standard reward thresholds are `1 / 2 / 4`; there is no separate starter draft.
- The six passives ship as proposed. Static Guard triggers once for one explicit non-turn-tick cooldown-reduction operation, even when both slots change.
- Infuse targets one normal, non-special cell without Frozen or Anchored. Cracked is allowed and remains on the transformed gem.
- Selected combat assignments are persisted on map nodes in `MapState`; `SelectedEncounterIds` is the ordered generated assignment ledger used by checkpoints and hashes.
- Domain schema `6` was the R1/R2 boundary and intentionally rejected older development checkpoints; the current domain schema is `10` and content version is `1.0.0`.
- Profile schema `2` is separate from the run checkpoint and migrates schema-1 profiles. Standard runs snapshot profile unlock IDs; weekly runs and expeditions pin `unlock_policy.all_v1.0`.
- Hybrid prerequisites use required branch-tag arrays. Advanced actives resolve board changes through `BoardSpawn` and the ordinary clear/special event pipeline.
- Difficulty tiers are cumulative immutable definitions. Tier 1 requires wins dominated by all four damage branches; tiers 2–5 unlock one at a time after a standard-run win on the preceding highest tier.
- Weekly challenges use a UTC Monday ID, pinned current content, deterministic FNV-1a seed, difficulty 3, and local-only records. Daily expedition IDs use UTC dates and retain the latest seven dates in the selection UI.
- R4 Barrier, Jam, Thorned, and phase state is persisted and hashed. Jam targets are selected and persisted at telegraph time; Thorned damage is 2 per cleared gem and capped at 6 per board-resolution batch.
- Schema `8` was the combined R3/R4 boundary; schema `7` was reserved and never shipped. R5 added three-region state in schema `9` and content version `0.9.0`.
- R6 defers next-map generation until `ContinueFromSanctum`; a completed optional vow gives one extra evolution option and never grants direct power on failure.
- R7 uses two regional story flags and no new combat effect type. Each later region has two normal additions, two elites total, and two bosses total.
- R8 expeditions are one-region all-content runs. Constellation rewards are cosmetic content IDs only, and the run archive stores at most twenty summaries.
