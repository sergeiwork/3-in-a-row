using System;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Combat
{
    public static class RunRulesetIds
    {
        public static readonly ContentId Standard = "run.standard_turns_v1";
        public static readonly ContentId Pulse = "run.pulse_v1";
    }

    public static class PulseContentIds
    {
        public static readonly ContentId Relaxed = "pulse.clock.relaxed";
        public static readonly ContentId Standard = "pulse.clock.standard";
        public static readonly ContentId Intense = "pulse.clock.intense";
        public static readonly ContentId NoTrial = "pulse.trial.none";
        public static readonly ContentId FrozenOpening = "pulse.trial.frozen_opening";
        public static readonly ContentId VenomRush = "pulse.trial.venom_rush";
        public static readonly ContentId ThreeRegions = "pulse.trial.three_regions";
        public static readonly ContentId SystemPulse = "system.pulse";
    }

    /// <summary>Pure deterministic timing policy for Pulse Run. It never reads a platform clock.</summary>
    public static class PulseSimulation
    {
        public const int TimeQuantumMilliseconds = 50;
        public const int MaximumAdvanceMilliseconds = 250;
        public const int SurgeCapacity = 24;
        public const int SurgeDurationMilliseconds = 4000;
        public const int MaximumFlow = 3;

        public static bool IsPulse(RunState state)
        {
            return state != null && state.RunRulesetId.Equals(RunRulesetIds.Pulse);
        }

        public static void Normalize(RunState state)
        {
            if (state.Pulse == null) state.Pulse = new PulseState();
            if (string.IsNullOrEmpty(state.Pulse.ClockPresetId.Value))
                state.Pulse.ClockPresetId = PulseContentIds.Standard;
            if (string.IsNullOrEmpty(state.Pulse.TrialId.Value))
                state.Pulse.TrialId = PulseContentIds.NoTrial;
            state.Pulse.Flow = Math.Max(0, Math.Min(MaximumFlow, state.Pulse.Flow));
            state.Pulse.SurgeCharge = Math.Max(0, Math.Min(SurgeCapacity, state.Pulse.SurgeCharge));
            state.Pulse.SurgeRemainingMilliseconds = Math.Max(0, state.Pulse.SurgeRemainingMilliseconds);
        }

        public static EventBatch BeginEncounter(RunState state, ICombatContentCatalog catalog = null)
        {
            if (!IsPulse(state)) return new EventBatch();
            catalog = catalog ?? MvpCombatContentCatalog.Instance;
            Normalize(state);
            var enemy = catalog.GetEnemy(state.Enemy.DefinitionId);
            state.Pulse.Flow = 0;
            state.Pulse.SwapsSincePulse = 0;
            state.Pulse.FirstSparkConsumed = false;
            state.Pulse.EnemyPulsePending = false;
            state.Pulse.SurgeRemainingMilliseconds = 0;
            state.Pulse.OnboardingPulsesRemaining = state.EncounterIndex <= 1 && state.Pulse.EnemyPulseCount == 0 ? 2 : 0;
            ResetClock(state, enemy);
            var events = new EventBatch();
            events.Add(SimulationEventType.DecisionClockChanged, PulseContentIds.SystemPulse,
                ClockDetail(state, "encounter_start"), state.Pulse.PulseRemainingMilliseconds);
            return events;
        }

        public static EventBatch Advance(RunState state, AdvanceDecisionTimeCommand command)
        {
            if (!IsPulse(state)) throw new InvalidOperationException("Decision time exists only in Pulse Run.");
            if (command == null) throw new ArgumentNullException(nameof(command));
            Normalize(state);
            var elapsed = command.ElapsedMilliseconds;
            if (elapsed <= 0 || elapsed > MaximumAdvanceMilliseconds || elapsed % TimeQuantumMilliseconds != 0)
                throw new ArgumentOutOfRangeException(nameof(command.ElapsedMilliseconds));
            if (state.Pulse.EnemyPulsePending) return new EventBatch();

            state.Pulse.ActiveDecisionMilliseconds += elapsed;
            var remaining = elapsed;
            var events = new EventBatch();
            if (state.Pulse.SurgeRemainingMilliseconds > 0)
            {
                var consumed = Math.Min(remaining, state.Pulse.SurgeRemainingMilliseconds);
                state.Pulse.SurgeRemainingMilliseconds -= consumed;
                remaining -= consumed;
                events.Add(SimulationEventType.SurgeChanged, PulseContentIds.SystemPulse,
                    "reason=active_time;remaining=" + state.Pulse.SurgeRemainingMilliseconds,
                    -consumed);
            }
            if (remaining > 0)
            {
                var before = state.Pulse.PulseRemainingMilliseconds;
                state.Pulse.PulseRemainingMilliseconds = Math.Max(0, before - remaining);
                events.Add(SimulationEventType.DecisionClockChanged, PulseContentIds.SystemPulse,
                    ClockDetail(state, "active_time"), state.Pulse.PulseRemainingMilliseconds - before);
                if (state.Pulse.PulseRemainingMilliseconds == 0)
                {
                    state.Pulse.EnemyPulsePending = true;
                    events.Add(SimulationEventType.EnemyPulseQueued, PulseContentIds.SystemPulse,
                        "swaps=" + state.Pulse.SwapsSincePulse + ";activeMs=" + state.Pulse.ActiveDecisionMilliseconds,
                        state.Pulse.EnemyPulseCount + 1);
                }
            }
            return events;
        }

        public static EventBatch ActivateSurge(RunState state, ActivateSurgeCommand command)
        {
            if (!IsPulse(state)) throw new InvalidOperationException("Surge exists only in Pulse Run.");
            if (command == null) throw new ArgumentNullException(nameof(command));
            Normalize(state);
            if (state.Pulse.SurgeCharge < SurgeCapacity || state.Pulse.SurgeRemainingMilliseconds > 0)
                throw new InvalidOperationException("SurgeUnavailable");
            state.Pulse.SurgeCharge = 0;
            state.Pulse.SurgeRemainingMilliseconds = SurgeDurationMilliseconds;
            var events = new EventBatch();
            events.Add(SimulationEventType.SurgeChanged, PulseContentIds.SystemPulse,
                "reason=activated;charge=0;remaining=" + SurgeDurationMilliseconds,
                SurgeDurationMilliseconds);
            return events;
        }

        public static void RecordAcceptedSwap(RunState state, EventBatch resolvedEvents, EventBatch output)
        {
            if (!IsPulse(state)) return;
            Normalize(state);
            state.Pulse.AcceptedSwapCount++;
            state.Pulse.SwapsSincePulse++;
            var previousFlow = state.Pulse.Flow;
            state.Pulse.Flow = Math.Min(MaximumFlow, state.Pulse.Flow + 1);
            if (state.Pulse.Flow != previousFlow)
                output.Add(SimulationEventType.FlowChanged, PulseContentIds.SystemPulse,
                    "reason=accepted_swap;current=" + state.Pulse.Flow, state.Pulse.Flow - previousFlow);

            var flowBonus = previousFlow < MaximumFlow && state.Pulse.Flow == MaximumFlow ? 1 : 0;
            RecordResolvedEventsCharge(state, resolvedEvents, output, flowBonus);
        }

        public static void RecordResolvedEventsCharge(RunState state, EventBatch resolvedEvents,
            EventBatch output, int bonus = 0)
        {
            if (!IsPulse(state) || resolvedEvents == null || output == null) return;
            Normalize(state);
            var charge = Math.Max(0, bonus);
            foreach (var item in resolvedEvents.Events)
            {
                if (item.Type == SimulationEventType.GemCleared) charge++;
                else if (item.Type == SimulationEventType.SpecialActivated) charge += 4;
            }
            if (charge <= 0 || state.Pulse.SurgeCharge >= SurgeCapacity) return;
            var before = state.Pulse.SurgeCharge;
            state.Pulse.SurgeCharge = Math.Min(SurgeCapacity, before + charge);
            output.Add(SimulationEventType.SurgeChanged, PulseContentIds.SystemPulse,
                "reason=board_clear;charge=" + state.Pulse.SurgeCharge,
                state.Pulse.SurgeCharge - before);
        }

        public static void CompletePulse(RunState state, EventBatch events, ICombatContentCatalog catalog = null)
        {
            if (!IsPulse(state)) return;
            catalog = catalog ?? MvpCombatContentCatalog.Instance;
            Normalize(state);
            state.Pulse.EnemyPulseCount++;
            state.Pulse.EnemyPulsePending = false;
            state.Pulse.SwapsSincePulse = 0;
            state.Pulse.FirstSparkConsumed = false;
            if (state.Pulse.Flow != 0)
            {
                var expiredFlow = state.Pulse.Flow;
                state.Pulse.Flow = 0;
                events.Add(SimulationEventType.FlowChanged, PulseContentIds.SystemPulse,
                    "reason=enemy_pulse;current=0", -expiredFlow);
            }
            if (state.Pulse.OnboardingPulsesRemaining > 0) state.Pulse.OnboardingPulsesRemaining--;
            if (state.Enemy.Health > 0 && state.Player.Health > 0)
            {
                ResetClock(state, catalog.GetEnemy(state.Enemy.DefinitionId));
                events.Add(SimulationEventType.DecisionClockChanged, PulseContentIds.SystemPulse,
                    ClockDetail(state, "pulse_complete"), state.Pulse.PulseRemainingMilliseconds);
            }
        }

        private static void ResetClock(RunState state, EnemyDefinition enemy)
        {
            var duration = state.Pulse.OnboardingPulsesRemaining > 0 ? 10000 : BaseDuration(enemy, state.RegionIndex);
            if (state.Pulse.ClockPresetId.Equals(PulseContentIds.Relaxed)) duration = duration * 135 / 100;
            else if (state.Pulse.ClockPresetId.Equals(PulseContentIds.Intense)) duration = duration * 80 / 100;
            duration = Math.Max(4000, duration / TimeQuantumMilliseconds * TimeQuantumMilliseconds);
            state.Pulse.PulseDurationMilliseconds = duration;
            state.Pulse.PulseRemainingMilliseconds = duration;
        }

        private static int BaseDuration(EnemyDefinition enemy, int regionIndex)
        {
            var duration = enemy.IsBoss || enemy.IsElite ? 6000 : 7000;
            if (enemy.DominantPressureId.Value == "pressure.freeze" || enemy.DominantPressureId.Value == "pressure.anchor" ||
                enemy.DominantPressureId.Value == "pressure.jam") duration += 500;
            duration -= Math.Max(0, Math.Min(2, regionIndex)) * 250;
            return duration;
        }

        private static string ClockDetail(RunState state, string reason)
        {
            return "reason=" + reason + ";remaining=" + state.Pulse.PulseRemainingMilliseconds +
                   ";duration=" + state.Pulse.PulseDurationMilliseconds;
        }
    }
}
