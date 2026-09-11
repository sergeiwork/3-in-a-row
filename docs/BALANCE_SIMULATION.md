# Local balance simulation suite

The balance suite exercises the current board, combat, progression, map, event, mastery, and run-orchestration logic without loading scenes or referencing the Presentation assembly. It is an Editor-only NUnit assembly and every statistical case is marked `Explicit`, so it runs only when deliberately selected on a local workstation.

## Strategies and decisions

Each of the Ember, Tide, Venom, and Volt strategies values clears and specials of its chosen element, prioritizes matching branch upgrades and evolutions, equips the most relevant learned active skills, and supplies valid targets for every active-skill target policy. The Opportunist strategy supplies a neutral comparison that values the strongest immediate board result and generally useful upgrades.

The player model also compares every legal swap by resolving it against a cloned board and cloned `BoardSpawn` RNG state, chooses among reachable combat/event/rest routes, evaluates event effects against current health/resources/statuses, pins the no-rest vow, and continues through all three regions until victory or defeat. Previewed states never mutate the authoritative run.

## Sample and output

Each explicit test case runs 200 distinct seeds plus one deterministic replay check: 1,000 full runs across all five strategies. Reports contain win rate, average encounters, turns, remaining health, damage dealt and taken, previews per move, loss-stage distribution, dominant-branch frequency, damage by branch, chosen upgrades, active-skill use, and a row for every simulated run.

Reports are written to the ignored local directory `Library/BalanceReports/` as Markdown. They are intentionally not committed because they describe one local run of the current code and are meant to be regenerated after balance changes.

## Run locally

Close the Unity Editor, then run the explicit balance assembly in Edit Mode:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath (Get-Location).Path `
  -runTests -testPlatform EditMode `
  -testFilter ThreeInARow.BalanceTests.BalanceSimulationTests.FocusedStrategy_SimulatesFullRunsAndWritesBalanceReport `
  -testResults "Logs\balance-tests.xml" `
  -logFile "Logs\balance-tests.log"
```

The XML/log files and generated balance reports stay under ignored local directories. A failed command decision, non-deterministic replay, missing move comparison, failure to exercise upgrades and active skills, or a strategy winning every sampled run fails the suite. Other balance outcomes are recorded rather than asserted against arbitrary thresholds.
