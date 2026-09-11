# Real-time mode exploration: Pulse Run

**Status:** Implementation baseline present across slices 0–3 as `run.pulse_v1`; playtest and acceptance gates remain open; Phase 4 remains exploratory and gated  
**Working name:** Pulse Run  
**Relationship to the main mode:** An additional mode. The existing turn-by-turn Standard Run remains unchanged.

## 1. Recommendation

Prototype a one-region **Pulse Run** in which the enemy's visible intent meter charges while the board is stable and accepting input. The player may resolve several swaps before the meter fills. When it fills, the current swap and cascade finish, then the enemy executes exactly one telegraphed intent.

The clock pauses during board animation, menus, tutorials, application suspension, and other non-interactive states. This measures decision time rather than device or animation speed and preserves readable cause and effect.

Do not begin with fully concurrent falling gems, live enemy attacks during cascades, or a full three-region campaign. Those versions multiply input, determinism, accessibility, and balance problems before the new mode has proved fun.

## 2. Player fantasy and design goals

Pulse Run should feel like reading a dangerous rhythm rather than playing Standard Run faster.

- **See the threat coming.** The exact next intent and its countdown are always visible.
- **Find one more move.** A nearly full enemy meter creates an understandable push-your-luck moment.
- **Build momentum.** Quick, valid decisions create a short arc of rising audiovisual intensity between enemy pulses.
- **Earn relief.** Large clears and specials build toward a brief player-controlled respite.
- **Stay readable.** Simulation still resolves one board mutation at a time; damage and statuses remain attributable to ordered events.

The mode should reward fluency, not demand twitch precision. A thoughtful player who averages one good swap per pulse must remain viable on the base difficulty, while faster players can turn spare time into advantage.

## 3. Core encounter loop

1. Show the enemy intent, exact effects, and a circular or horizontal charge meter.
2. Start the charge only when the board is stable and input is enabled.
3. The player may use a ready active skill or make an adjacent swap.
4. On an accepted swap, pause the charge and resolve the complete board event batch.
5. Restore input and resume the charge from its previous value.
6. If the charge reaches zero while the board is stable, close board input and execute the telegraphed enemy intent.
7. If it reaches zero after an input was accepted, mark one enemy pulse pending; finish that swap and all cascades before executing the intent.
8. Telegraph the next intent, reset its duration, and resume play when its presentation is complete.

Only one enemy pulse may be pending. An enemy cannot attack in the middle of a swap, cascade, active-skill resolution, or board reconciliation.

### Recommended first tuning

| Encounter class | Base decision time | Purpose |
| --- | ---: | --- |
| Normal | 7 seconds | Supports one careful swap or two fluent swaps |
| Elite | 6 seconds | Raises pressure without changing input rules |
| Boss | 6 seconds | Uses intent rhythm and phases for difficulty |

Difficulty modifiers may adjust these values later, but the first prototype should tune enemy health and damage before shortening the clock below six seconds.

## 4. Signature entertainment systems

### 4.1 Flow

Flow is the lightweight streak layer for the prototype.

- Each accepted swap before the next enemy pulse adds one Flow, to a maximum of three.
- Flow resets immediately after the enemy intent resolves.
- Flow does not directly multiply all damage in the first prototype. Instead, it increases presentation intensity and adds **one extra Surge charge** at Flow three.
- Invalid swaps add no Flow; the enemy clock simply continues.

This makes speed exciting without making rapid play the only viable damage build. It also gives the mode a visible beginning, middle, and climax between enemy actions.

### 4.2 Surge

Surge is the player's earned moment of control.

- Clearing gems grants one Surge charge per gem; specials grant an additional four.
- The meter fills at 24 charges. Excess charge does not carry past a full meter in the prototype.
- When full, a **Surge** button becomes available. Activating it freezes the enemy decision clock for four seconds of active board-input time.
- Surge activation itself does not mutate the board, deal damage, or consume a swap.
- Only one full Surge may be stored.

Surge turns skilled board play into breathing room and gives large cascades a payoff unique to the mode. Because its duration counts only while the player can act, it behaves consistently across motion settings and devices.

### 4.3 Intent rhythm

Existing enemy intent decks remain the content foundation, but their sequence becomes a rhythm:

- attacks create urgency;
- status intents create a board problem for the next pulse;
- resource drains discourage waiting for the perfect conversion;
- boss phase changes can alter the charge duration by a clearly telegraphed amount.

The prototype should first use three representative enemies: one direct attacker, one status controller, and one boss with alternating pressure. New real-time-only enemies are unnecessary until this rhythm is proven.

## 5. Rule translation from Standard Run

Shared content must declare its timing in semantic units rather than silently treating every accepted swap as a full turn.

| Existing rule | Pulse Run rule |
| --- | --- |
| Enemy responds after every accepted swap | Enemy responds when its decision clock fills |
| Poison ticks at start of enemy response | Poison ticks at start of each enemy pulse |
| Shield expires at the next accepted swap | Shield expires immediately after it has protected through the next enemy pulse; shield gained before that pulse is useful |
| Active cooldown loses one turn after a completed player turn | Active cooldown loses one step after each accepted swap; a just-used skill skips the next cooldown step as it does now |
| Volt reduces cooldown turns | Volt reduces cooldown steps with the same numeric values |
| Jam adds cooldown turns | Jam adds cooldown steps |
| Anchored lasts one player turn | Anchored lasts until the next enemy pulse finishes |
| “First Spark each player turn” | First Spark between enemy pulses |
| Thorned damage cap per board-resolution batch | Unchanged |
| Victory healing, Focus, Toxic, Poison caps | Unchanged initially |

These translations need explicit mode-aware timing policies. Content IDs should remain stable where the player-facing effect is genuinely equivalent; any materially different effect should receive Pulse-specific copy or a separate definition.

## 6. Actives and pausing

- Untargeted skills resolve immediately and pause the enemy clock during their event presentation.
- Entering a targeted skill pauses the enemy clock. The targeting overlay clearly says that time is paused.
- Canceling targeting has no cost and resumes the same clock value.
- Opening How to Play, Settings, or the pause menu pauses active decision time and obscures the board enough that pause is not an optimal board-analysis tool.
- Backgrounding the app pauses immediately. Returning shows a three-second resume countdown before the enemy clock restarts.

Pulse Run is not intended as a ranked esport. Predictable pausing and accessibility are more valuable than policing every opportunity to think.

## 7. Run format and progression

### Prototype format

Use a deterministic one-region route lasting about 8–12 minutes:

`Normal combat → choice of event or rest → normal combat → upgrade → elite or normal → boss`

- Start with the usual two active skills.
- Grant an opening three-card passive draft so a build exists early enough to test.
- Grant one additional draft before the boss.
- Use one region and a compact enemy pool to make repeated playtests comparable.
- Keep Pulse results separate from Standard Run difficulty unlocks and weekly records during experimentation.

### Expansion gate

Consider a three-region Pulse campaign only if the prototype meets its acceptance targets and players still want a longer session. A better long-term home may remain short expeditions, where real-time intensity complements rather than replaces the deliberate campaign.

## 8. Balance approach

Do not copy Standard Run health and damage unchanged. In Pulse Run, a faster player may make multiple swaps per enemy action, so turns-to-kill no longer predicts danger.

Track and tune around:

- accepted swaps per enemy pulse;
- player damage per active decision second;
- enemy pulses survived per encounter;
- Flow-three frequency;
- Surge activations per encounter;
- damage taken by intent family;
- pause and targeting time separately from active decision time.

Initial targets:

| Measure | Target |
| --- | --- |
| New-player swaps per pulse | 1.0–1.4 |
| Experienced-player swaps per pulse | 1.7–2.3 |
| Normal encounter length | 4–6 enemy pulses |
| Boss length | 7–10 enemy pulses |
| Surge frequency | 1–2 activations per normal encounter, 2–4 per boss |
| Base-mode win rate after onboarding | 55–70% |

Tune enemy health upward enough that speed creates advantage without deleting intent cycles. Tune attack damage downward if slow but accurate players are eliminated before they can learn the rhythm.

## 9. UX, feedback, and accessibility

### Encounter HUD

- Replace the static intent strip with the same icon and exact effect text plus a large, high-contrast charge meter.
- Show the remaining time numerically in Settings as an optional accessibility preference; the visual meter is always present.
- Place Flow near the board edge and Surge beside the active skills. Neither may cover the intent or board statuses.
- At two seconds remaining, shift the intent meter's shape and sound cadence, not color alone.
- When a pulse becomes pending during an accepted input, show “Intent queued” and stop the meter at zero rather than attacking during animation.

### Feel

- Use faster board presentation than Standard Run, but keep simulation and lock boundaries identical.
- Raise crystal pitch and add a restrained musical layer as Flow rises.
- Surge briefly drops the enemy pulse stem, widens the board ambience, and makes the frozen clock unmistakable.
- The enemy pulse lands on a consistent audio beat after its short wind-up.

### Accessibility

- Offer Relaxed, Standard, and Intense clock presets; only Standard is used for comparable records.
- Reduced Motion changes animation, never available decision time.
- A visible pause control is always available while the board accepts input.
- The first Pulse encounter teaches one mechanic at a time and begins with a 10-second clock for its first two pulses.
- Haptics and urgent audio remain independently optional.

## 10. Technical shape

### Separate mode policy

Add a run ruleset ID rather than branching on presentation state:

```text
run.standard_turns_v1
run.pulse_v1
```

A mode policy should own:

- what schedules an enemy response;
- which semantic unit advances cooldowns and durations;
- base enemy decision durations;
- whether Flow and Surge state is enabled;
- mode-specific scoring and telemetry labels.

Board matching, ordered clear events, gem effects, intent effect definitions, RNG streams, map selection, and reward sampling remain shared.

### Domain state

The exploratory state additions are:

```text
RunRulesetId
AcceptedSwapCount
EnemyPulseCount
PulseRemainingMilliseconds
EnemyPulsePending
Flow
SurgeCharge
SurgeRemainingMilliseconds
```

Use integer milliseconds or fixed simulation ticks, never floating-point wall time in the deterministic state. `ResolvedTurnCount` may remain as a legacy Standard Run statistic, but new code should not overload it with two meanings.

### Commands and events

Potential additions:

```text
AdvanceDecisionTimeCommand(elapsedMilliseconds)
ActivateSurgeCommand()

EnemyPulseQueued
EnemyPulseStarted
FlowChanged
SurgeChanged
DecisionClockChanged
```

Presentation reports elapsed active-input time in bounded, quantized slices. The application validates and applies it before any swap command received in the same frame. Replays record these time-advance commands alongside player commands.

No domain state may depend directly on `Time.deltaTime`, animation completion time, frame count, or platform clock.

### Checkpoints

- A stable checkpoint may be written only when the board is stable, no enemy pulse is pending, and no skill targeting operation is open.
- Save the current decision-clock value so resume cannot reset an imminent attack.
- Resume enters a paused state and requires an explicit three-second presentation countdown.
- Pulse Run requires a schema/content boundary only when implementation begins; this exploratory document does not change the current schema.

## 11. Delivery slices

### Slice 0 — Timing sandbox

- One fixed board, one enemy, no map or rewards.
- Visible clock that runs only during stable input.
- Multiple swaps per pulse and one queued pulse after an in-flight swap.
- Development overlay for active time, swaps per pulse, pending state, and event order.

Exit when no device speed, animation setting, pause, or frame-rate variation changes the resulting command/event replay.

### Slice 1 — Fun prototype

- Three representative enemies and one boss.
- Flow, Surge, actives, Poison, Shield, Frozen, Cracked, Anchored, and Jam translations.
- One short deterministic region and two upgrade drafts.
- Temporary but clear sound and UI treatment.

Exit after at least ten players complete two runs and the second run shows improved swaps-per-pulse without a rise in unexplained damage.

### Slice 2 — Production candidate

- Relaxed/Standard/Intense presets, onboarding, reduced-motion validation, save/resume, run summary, and balance simulation.
- Russian player-facing text and final presentation assets.
- Pulse-specific profile records, with no effect on Standard Run unlock progression yet.

Exit only if the acceptance criteria below are met.

### Slice 3 — Optional expansion

- More regional pools and Pulse-specific enemy timing profiles.
- Handcrafted Pulse trials.
- Consider a three-region run, a daily seed, or unlock integration independently.

### Phase 4 — True continuous real-time (separate gated investment)

This phase is intentionally outside the Pulse Run commitment. Begin it only after Pulse proves that players want more urgency **and** playtests identify waiting for complete board settlement as a specific limitation. It is a major simulation project, not an option added to the existing timer.

#### What “true continuous” means

- The enemy clock continues while gems swap, clear, fall, and refill.
- The player may start another swap using two eligible settled gems while unrelated parts of the board are still resolving.
- Several local board-resolution fronts may therefore be active at once.
- An enemy intent may become due during a player cascade and resolves at its authoritative simulation tick rather than waiting for the entire board to stabilize.
- Presentation observes a continuously advancing simulation; animation callbacks never decide game outcomes.

This does **not** require multithreaded gameplay code or free-form physics. The recommended implementation is one deterministic fixed-step scheduler on the main thread.

#### Product gate

Approve Phase 4 only when all of the following are true:

- Pulse Run meets its production acceptance criteria.
- A meaningful share of returning testers try to interact during cascades and describe the lock as unwanted waiting.
- A greybox with overlapping column settlement tests better than Pulse on excitement without materially lowering comprehension.
- The team accepts that this work is comparable to replacing the board/combat orchestration core, including saves, replay, AI test drivers, and much of encounter presentation.

Do not approve it merely because “real-time” sounds more marketable. If Pulse already delivers the desired rhythm, keep its much simpler and more legible architecture.

#### Required simulation model

Replace whole-turn batch resolution for this ruleset with a deterministic fixed-step scheduler. A starting tick size of 50 milliseconds is recommended for the prototype; rendering may interpolate between ticks.

Each board cell needs an explicit phase such as:

```text
Settled
ReservedForSwap
Clearing
Falling
Spawning
Blocked
```

The continuous state also needs:

```text
CurrentSimulationTick
NextCommandSequence
ScheduledBoardTransitions
ScheduledCombatEffects
CellReservations
EnemyIntentDueTick
ActiveMatchGroups
```

A swap is legal only when both cells are settled, adjacent, movable, and unreserved in the authoritative tick snapshot. Moving or clearing cells cannot be targeted. Independent columns or settled board islands remain available.

Use a stable conflict key for every simultaneous event:

```text
due tick → event priority → originating command sequence → row-major cell → stable content ID
```

The exact priority table must be locked before production. The recommended same-tick principle is that a player action accepted before the deadline may complete its immediately scheduled match effects before an enemy intent due on that tick; later cascade steps do not receive the same protection. This creates a readable last-moment save without allowing an arbitrarily long cascade to postpone danger.

#### Board and combat refactor

The existing `BoardSimulation.ResolveSwap` resolves an entire cascade transaction immediately, and `CombatSimulation.BeginSwap` then consumes that completed batch. True continuous play needs separate operations:

```text
TryStartSwap(command, tick)
AdvanceBoardTo(tick)
AdvanceCombatTo(tick)
DrainDueEvents(tick)
```

- Swap, clear, fall, and spawn become scheduled domain transitions with fixed integer durations.
- Match detection runs when affected gems land or a swap completes, not once after a global board settle.
- Cells participating in a detected match are reserved atomically so overlapping match groups cannot clear the same gem twice.
- Gravity operates per column segment and respects Anchored cells and other reservations.
- Dead-board detection runs only at a quiescent barrier: no eligible swap, no scheduled spawn/fall, and no unresolved match. A deterministic reshuffle reserves the full board.
- Combat effects remain ordered events but can interleave with board transitions and enemy intents by scheduler priority.
- Player and enemy death checks occur at documented tick boundaries. A same-tick double defeat needs an explicit rule; the recommended rule is that the player wins if their pre-deadline action reduced the enemy to zero before the enemy-intent priority step.

Standard Run and Pulse Run should keep their current batch resolver. Build a separate continuous orchestrator over shared board rules, content definitions, effect primitives, and event types rather than adding pervasive continuous-mode branches inside the proven turn resolver.

#### Content and balance migration

Every piece of content must be classified as one of:

- **Event-counted:** triggers after accepted swaps, clears, specials, conversions, or enemy intents.
- **Clock-counted:** measured in fixed ticks or milliseconds.
- **Until-condition:** persists until the next enemy intent, next damage event, cleanse, or explicit board event.

Avoid translating cooldowns directly into seconds without playtesting; that would weaken the existing relationship between Volt, board skill, and active reuse. The recommended first continuous prototype keeps active cooldown progress tied to accepted swaps, while enemy scheduling is clock-based.

Enemy timings require new tuning profiles because long attack wind-ups, status application, and telegraph replacement can overlap board action. Bosses need authored safe and dangerous rhythm changes rather than simply shorter timers. Existing HP and damage values should be treated as placeholders.

#### Input and presentation work

- Hit testing must use the authoritative cell snapshot for the command tick, not the gem's interpolated visual position.
- Settled selectable gems, moving gems, and reserved gems need distinct but restrained feedback.
- A rejected gesture caused by a just-reserved cell should feel different from an invalid match and should not produce a punitive lock.
- The player needs clear ownership cues when two cascades overlap: command-origin highlights, local clear waves, and merged rather than competing cascade banners.
- Enemy attacks that land amid board action must remain legible without obscuring the next legal swap.
- Reduced Motion removes interpolation and flourish but advances the same fixed ticks; it cannot create earlier cell availability.
- Targeted skill behavior must be redesigned. Pausing the whole simulation during targeting is acceptable for the first prototype; production may instead give each skill an instant, gesture-friendly targeting policy.

The first prototype should permit at most one outstanding player swap command at a time. It may accept a new command as soon as that swap's two cells become settled, even while other columns continue falling. Do not add an input queue until direct manipulation is proven reliable.

#### Save, replay, and suspension

True continuous replay records commands with their target simulation tick and monotonic command sequence. Frame timestamps and animation durations are never replay inputs.

Production-quality mid-encounter resume must serialize the scheduler, cell phases and reservations, all due events, enemy deadline, and active match groups. That is substantially larger than the current stable-board checkpoint. The feasibility prototype may save only at encounter boundaries, but the mode cannot ship on mobile without safe application-suspension behavior.

Before shipping, verify deterministic equivalence across:

- 30, 60, and 120 Hz rendering;
- frames that advance zero, one, or several simulation ticks;
- foreground/background boundaries;
- normal and reduced motion;
- replay with commands submitted on deadline ticks;
- save/resume during swap, clear, fall, spawn, overlapping cascades, and an enemy wind-up.

#### Phase 4 delivery steps

1. **4A — Interaction greybox:** two independently settling columns, one enemy deadline, fixed board, no progression or saves. Determine whether acting during cascades is understandable and more fun.
2. **4B — Deterministic scheduler:** cell phases, reservations, fixed-step event queue, command timestamps, stable priorities, replay, and hash coverage.
3. **4C — Shared mechanics:** specials, statuses, resources, actives, enemy intents, death ordering, reshuffle barriers, and three representative encounters.
4. **4D — Mobile presentation:** authoritative hit testing, interpolation, overlapping feedback, pause/targeting, audio mixing, tutorial, and accessibility.
5. **4E — Production hardening:** mid-resolution checkpoints, app suspension, automated fuzzing, balance agents, full content audit, device performance, and localization.

#### Phase 4 exit criteria

- Players can correctly identify which gems are currently actionable during overlapping settlement.
- Input during cascades creates deliberate choices rather than rapid random swiping.
- At least 75% of damage events are correctly attributed by testers immediately after a busy encounter.
- The same timestamped command log produces the same event order and state hash at all tested render rates.
- No gem clears twice, occupies two cells, disappears without an event, or remains permanently reserved in long randomized simulations.
- The continuous version scores materially higher than Pulse on excitement and no more than one point lower on clarity using the same five-point survey.
- Mobile pause, interruption, and resume never grant extra time, lose accepted input, or change event order.

If the greybox fails the excitement-versus-clarity test, stop after 4A and retain Pulse Run. That is a successful validation result, not an incomplete implementation.

## 12. Acceptance criteria

- At least 80% of testers can explain when the enemy will act after the tutorial encounter.
- At least 70% intentionally activate Surge by their second run.
- Median swaps per pulse increases between a tester's first and second run.
- Relaxed players can win through accurate single-swap pulses; Standard players gain meaningful but nonessential advantage from additional swaps.
- No enemy action interrupts board resolution or produces an unattributable damage event.
- Reduced Motion, 30/60/120 Hz rendering, pausing, and resume produce equal active decision budgets for the same recorded commands.
- Testers describe the mode as a distinct rhythm or pressure experience, not merely “the same game with a timer.”

## 13. Main risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Fast players dominate balance | Flow grants control rather than a universal damage multiplier; base tuning supports one good swap per pulse |
| Timers undermine tactical reading | Seven-second starting clock, Relaxed preset, full pause, clear intent copy, tutorial grace pulses |
| Long cascades feel like lost time | Clock runs only during stable input |
| Targeted actives become unusable | Targeting explicitly pauses the clock |
| Shared content gains ambiguous “turn” wording | Introduce semantic timing policies and mode-specific copy where effects differ |
| Replays or saves become nondeterministic | Record quantized time commands and persist fixed-point clock state |
| Full campaign becomes exhausting | Validate as an 8–12 minute one-region run first |
| Real-time implementation destabilizes Standard Run | Select a ruleset at run creation; keep Standard resolver and balance unchanged |

## 14. Decisions to make after the timing sandbox

1. Is seven seconds the right base pulse, or does eight seconds produce better deliberate play?
2. Does Flow need a small mechanical reward beyond extra Surge charge?
3. Is a four-second Surge long enough to feel liberating without trivializing boss rhythms?
4. Should bosses vary pulse duration by intent, or keep one learnable beat for the whole encounter?
5. Should Pulse remain an expedition family or graduate to a full campaign option?

The recommended defaults are: seven seconds, no direct Flow damage bonus, four-second Surge, one stable beat per enemy phase, and a short expedition identity until playtests prove demand for more.
