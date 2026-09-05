# Content Expansion and Retention Roadmap

**Status:** R1 and R2 implemented and promoted to [GDD.md](GDD.md); R3–R5 remain proposals  
**Depends on:** [GDD.md](GDD.md), especially the deterministic content, command, event, and checkpoint boundaries  
**Planning principle:** Ship the smallest stage that gives players a new reason to make another run, measure it, and expand only after its retention hypothesis is supported.

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

## 2. Retention ladder and measurement

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
| **R5 — Breadth expansion** | Long tail | Second region/act; evaluate shop, charms, or multi-enemy combat separately | Very large | D7 retention justifies more production breadth |

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

Begin this stage only if D7 behavior shows players want more run length and world variety. Evaluate the following as separate investments rather than one bundle.

### Recommended first: second region

- Add one new visual region after the existing boss, with a checkpointed continue/stop choice if total session length exceeds 20 minutes.
- Reuse the four normal gem colors and established player build.
- Add 4–6 normal enemies, 2 elites, 2 bosses, 6 events, and one new board-status mechanic.
- Use regional encounter/event pools and map visuals; do not duplicate skill systems.

### Evaluate independently

| Feature | Potential value | Main cost/risk | Recommendation |
| --- | --- | --- | --- |
| Shop + currency | Strong route and resource decisions | Economy tuning, UI, save fields, content pricing | Add only if events/rest cannot create enough route tension |
| Charm/relic inventory | Powerful run identity | Large interaction matrix and tooltip burden | Prototype with 6–8 charms before committing to a large pool |
| Multiple enemies | Targeting and encounter depth | Rewrites intents, damage targeting, UI, AI order, skills, saves | Defer until single-enemy mastery plateaus |
| Fifth normal gem | New build family | Lower match rate and full-board rebalance | Avoid; prefer specials or overlays |
| More than two active slots | More tools per run | Lower equip tension and crowded mobile HUD | Avoid unless testing shows learned actives feel unusable |
| Permanent stat progression | Easy short-term compulsion | Undermines deterministic balance and skill mastery | Do not use as the default meta loop |

## 10. Prioritized backlog by impact-to-effort

| Priority | Item | Retention impact | Effort | Why now / why later |
| --- | --- | --- | --- | --- |
| 1 | First reward after encounter 1 | Very high | Small | Players see build agency before deciding whether to continue |
| 2 | Complete 12-passive branch tree | Very high | Medium | Creates credible build promises and fixes uneven branches |
| 3 | Depth-based enemy pools + four recombination enemies | High | Medium | Makes run two different using the current intent grammar |
| 4 | Aegis and Infuse | High | Small/medium | Expands loadout decisions and proves board targeting |
| 5 | Compact map | Very high | Large | Multiplies content value once choices have distinct outcomes |
| 6 | Six events + rest | High | Medium | Adds non-combat stories and health/power tradeoffs |
| 7 | Two elites + alternate boss | High | Medium | Gives route risk a concrete payoff and finale variety |
| 8 | Horizontal challenges + codex | Medium/high | Medium | Creates D1 goals without stat grind |
| 9 | Hybrid and advanced active skills | Medium/high | Medium | Deepens mastery after base branches are understood |
| 10 | Difficulty ladder | Medium | Medium | Valuable only after normal-mode balance is trustworthy |
| 11 | Second region | High for retained users | Very large | Content multiplier justified only by D7 evidence |
| 12 | Shop, relic inventory, or multiple enemies | Unknown/high | Very large | Prototype separately after map behavior is known |

## 11. Production slicing

Each stage should be implemented in this order:

1. Lock the stage’s rule text, stable IDs, event order, RNG usage, and save changes in the GDD.
2. Add pure-domain definitions and deterministic fixtures with placeholder presentation keys.
3. Add application commands, screen derivation, and stable checkpoint boundaries.
4. Add Russian UI text, icons, tooltips, reduced-motion behavior, and asset-ledger entries.
5. Run automated seed sweeps, device layout checks, and observed playtests.
6. Tune values without changing the stage’s rules; record the decision and retention result.

Do not start the next stage merely because the current one is code-complete. Start it when the current retention hypothesis is either supported or clearly falsified and the next experiment addresses what was learned.

## 12. Locked R1/R2 implementation decisions

- Standard reward thresholds are `1 / 2 / 4`; there is no separate starter draft.
- The six passives ship as proposed. Static Guard triggers once for one explicit non-turn-tick cooldown-reduction operation, even when both slots change.
- Infuse targets one normal, non-special cell without Frozen or Anchored. Cracked is allowed and remains on the transformed gem.
- Selected combat assignments are persisted on map nodes in `MapState`; `SelectedEncounterIds` is the ordered generated assignment ledger used by checkpoints and hashes.
- Domain schema `6` intentionally rejects older development checkpoints and the existing Russian Resume error explains incompatibility.
