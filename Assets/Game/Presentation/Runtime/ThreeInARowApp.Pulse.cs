using System;
using ThreeInARow.Application;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThreeInARow.Presentation
{
    public sealed partial class ThreeInARowApp
    {
        private VisualElement _pulseClockFill;
        private VisualElement _pulseClockBack;
        private Label _pulseClockLabel;
        private Label _pulseFlowLabel;
        private Label _pulseSurgeLabel;
        private Button _pulseSurgeButton;
        private Label _pulseDevelopmentLabel;
        private float _pulseAccumulatorMilliseconds;
        private float _pulseResumeCountdown;
        private bool _pulseWasUnfocused;
        private int _pulseUrgentTickBucket = -1;
        private string _pulseLastEventOrder = "—";

        private void Update()
        {
            if (_director == null || !PulseSimulation.IsPulse(_director.State)) return;
            if (_pulseResumeCountdown > 0f)
            {
                _pulseResumeCountdown = Mathf.Max(0f, _pulseResumeCountdown - Time.unscaledDeltaTime);
                RefreshPulseHud();
                return;
            }
            if (!CanAdvancePulseClock()) return;
            _pulseAccumulatorMilliseconds += Time.unscaledDeltaTime * 1000f;
            AdvanceAvailablePulseTime();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!PulseSimulation.IsPulse(_director == null ? null : _director.State)) return;
            _pulseAccumulatorMilliseconds = 0f;
            if (paused) _director.SaveCurrentCheckpoint();
            else BeginPulseResumeCountdown();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!PulseSimulation.IsPulse(_director == null ? null : _director.State)) return;
            if (!focused)
            {
                _pulseWasUnfocused = true;
                _pulseAccumulatorMilliseconds = 0f;
                _director.SaveCurrentCheckpoint();
            }
            else if (_pulseWasUnfocused)
            {
                _pulseWasUnfocused = false;
                BeginPulseResumeCountdown();
            }
        }

        private bool CanAdvancePulseClock()
        {
            return _director.Screen == RunScreen.Encounter && !_inputLocked &&
                   !_targetingSkill.HasValue && _root != null && _root.Q("modal-overlay") == null;
        }

        private void AdvanceAvailablePulseTime()
        {
            while (_pulseAccumulatorMilliseconds >= PulseSimulation.TimeQuantumMilliseconds &&
                   _director.Screen == RunScreen.Encounter)
            {
                _pulseAccumulatorMilliseconds -= PulseSimulation.TimeQuantumMilliseconds;
                var result = _director.AdvanceDecisionTime(PulseSimulation.TimeQuantumMilliseconds);
                if (!result.Accepted) return;
                if (_director.State.Pulse.EnemyPulsePending)
                {
                    _pulseAccumulatorMilliseconds = 0f;
                    RefreshPulseHud();
                    PresentEnemyPulse(result.Events);
                    return;
                }
            }
            RefreshPulseHud();
        }

        private bool FlushPulseClockBeforeInput()
        {
            if (!PulseSimulation.IsPulse(_director == null ? null : _director.State)) return true;
            AdvanceAvailablePulseTime();
            return !_director.State.Pulse.EnemyPulsePending;
        }

        private void PresentEnemyPulse(EventBatch queuedEvents)
        {
            _inputLocked = true;
            PlayBatch(queuedEvents, () =>
            {
                var response = _director.ResolveEnemyPulse();
                if (!response.Accepted)
                {
                    _inputLocked = false;
                    BuildForCurrentScreen();
                    return;
                }
                PlayBatch(response.Events, BuildForCurrentScreen);
            });
        }

        private void BeginPulseResumeCountdown()
        {
            if (!PulseSimulation.IsPulse(_director == null ? null : _director.State)) return;
            _pulseAccumulatorMilliseconds = 0f;
            _pulseResumeCountdown = 3f;
        }

        private void BuildPulseHud(VisualElement parent, RunState state)
        {
            var pulse = state.Pulse;
            var clock = Bar(string.Empty, PulseFraction(pulse), Danger);
            _pulseClockBack = clock;
            clock.name = "pulse-clock";
            clock.style.height = 34;
            clock.style.marginTop = 1;
            clock.style.marginBottom = 5;
            _pulseClockFill = clock.Q<VisualElement>("bar-fill");
            _pulseClockLabel = clock.Q<Label>("bar-label");
            parent.Add(clock);

            var row = Row();
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;
            _pulseFlowLabel = LabelText(string.Empty, 17, Cyan, TextAnchor.MiddleLeft);
            _pulseFlowLabel.style.flexGrow = 1;
            row.Add(_pulseFlowLabel);

            _pulseSurgeButton = ActionButton("НАТИСК", ActivatePulseSurge, false);
            _pulseSurgeButton.style.width = 190;
            _pulseSurgeButton.style.height = 46;
            _pulseSurgeLabel = _pulseSurgeButton.Q<Label>();
            row.Add(_pulseSurgeButton);

            var pause = SmallButton("Ⅱ ПАУЗА", () => ShowModal("ПАУЗА",
                "Таймер врага остановлен. Поле затемнено; закройте окно, когда будете готовы продолжить."));
            pause.style.marginLeft = 6;
            row.Add(pause);
            parent.Add(row);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _pulseDevelopmentLabel = LabelText(string.Empty, 13, Muted, TextAnchor.MiddleCenter);
            _pulseDevelopmentLabel.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(_pulseDevelopmentLabel);
#endif
            RefreshPulseHud();
        }

        private void RefreshPulseHud()
        {
            if (_director == null || !PulseSimulation.IsPulse(_director.State) || _director.State.Pulse == null) return;
            var pulse = _director.State.Pulse;
            if (_pulseClockFill != null)
            {
                _pulseClockFill.style.width = Length.Percent(PulseFraction(pulse) * 100f);
                _pulseClockFill.style.backgroundColor = pulse.PulseRemainingMilliseconds <= 2000 ? Danger : Gold;
            }
            if (_pulseClockBack != null)
            {
                var urgent = pulse.PulseRemainingMilliseconds <= 2000 && !pulse.EnemyPulsePending;
                _pulseClockBack.style.borderTopWidth = urgent ? 3 : 1;
                _pulseClockBack.style.borderBottomWidth = urgent ? 3 : 1;
                var bucket = urgent ? pulse.PulseRemainingMilliseconds / 500 : -1;
                if (urgent && bucket != _pulseUrgentTickBucket)
                {
                    _pulseUrgentTickBucket = bucket;
                    PlayOneShot("feedback.clear.volt", 0.18f, 0.72f + (3 - bucket) * 0.08f);
                }
                else if (!urgent) _pulseUrgentTickBucket = -1;
            }
            if (_pulseClockLabel != null)
            {
                if (_pulseResumeCountdown > 0f)
                    _pulseClockLabel.text = "ВОЗВРАТ: " + Mathf.CeilToInt(_pulseResumeCountdown);
                else if (pulse.EnemyPulsePending)
                    _pulseClockLabel.text = "НАМЕРЕНИЕ В ОЧЕРЕДИ";
                else if (pulse.SurgeRemainingMilliseconds > 0)
                    _pulseClockLabel.text = "НАТИСК · ВРЕМЯ ЗАМОРОЖЕНО";
                else
                    _pulseClockLabel.text = (pulse.PulseRemainingMilliseconds <= 2000 ? "◆ " : string.Empty) +
                        "СЛЕДУЮЩИЙ ИМПУЛЬС" +
                        (_pulseNumericTimer ? " · " + (pulse.PulseRemainingMilliseconds / 1000f).ToString("0.0") + " с" : string.Empty);
                _pulseClockLabel.style.unityFontStyleAndWeight = pulse.PulseRemainingMilliseconds <= 2000
                    ? FontStyle.Bold : FontStyle.Normal;
            }
            if (_pulseFlowLabel != null)
                _pulseFlowLabel.text = "ПОТОК " + pulse.Flow + "/" + PulseSimulation.MaximumFlow +
                                       " · ходов до импульса: " + pulse.SwapsSincePulse;
            if (_pulseSurgeButton != null)
            {
                var ready = pulse.SurgeCharge >= PulseSimulation.SurgeCapacity &&
                            pulse.SurgeRemainingMilliseconds <= 0 && _director.Screen == RunScreen.Encounter;
                _pulseSurgeButton.SetEnabled(ready && !_inputLocked);
                _pulseSurgeButton.text = pulse.SurgeRemainingMilliseconds > 0
                    ? "НАТИСК " + (pulse.SurgeRemainingMilliseconds / 1000f).ToString("0.0") + " с"
                    : "НАТИСК " + pulse.SurgeCharge + "/" + PulseSimulation.SurgeCapacity;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_pulseDevelopmentLabel != null)
                _pulseDevelopmentLabel.text = "PULSE DEV · active=" + pulse.ActiveDecisionMilliseconds +
                    " ms · swaps=" + pulse.AcceptedSwapCount + " · pulses=" + pulse.EnemyPulseCount +
                    " · pending=" + pulse.EnemyPulsePending + " · intent=" + _director.State.Enemy.IntentIndex +
                    "\nlast: " + _pulseLastEventOrder;
#endif
        }

        private void CapturePulseEventOrder(EventBatch events)
        {
            if (!PulseSimulation.IsPulse(_director == null ? null : _director.State) || events == null) return;
            var text = string.Empty;
            var start = Math.Max(0, events.Events.Count - 8);
            for (var index = start; index < events.Events.Count; index++)
            {
                if (text.Length > 0) text += " → ";
                text += events.Events[index].Type.ToString();
            }
            _pulseLastEventOrder = text.Length == 0 ? "—" : text;
        }

        private void ActivatePulseSurge()
        {
            if (_inputLocked || !FlushPulseClockBeforeInput()) return;
            var result = _director.ActivateSurge();
            if (!result.Accepted)
            {
                SetMessage("Натиск ещё не готов.", Danger);
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void ShowPulseRunOptions()
        {
            ShowModal("РЕЖИМ «ПУЛЬС»", "Время идёт только на стабильном поле. Выберите темп или особое испытание.", modal =>
            {
                modal.Add(ActionButton("СПОКОЙНЫЙ · +35% ВРЕМЕНИ", () => StartPulseRun(PulseContentIds.Relaxed), false));
                modal.Add(ActionButton("СТАНДАРТНЫЙ · РЕКОМЕНДУЕТСЯ", () => StartPulseRun(PulseContentIds.Standard), true));
                modal.Add(ActionButton("НАПРЯЖЁННЫЙ · −20% ВРЕМЕНИ", () => StartPulseRun(PulseContentIds.Intense), false));
                modal.Add(SectionHeading("ИСПЫТАНИЯ ПУЛЬСА"));
                modal.Add(ActionButton("ЛЕДЯНОЙ СТАРТ", () => StartPulseTrial(PulseContentIds.FrozenOpening), false));
                modal.Add(ActionButton("ЯДОВИТЫЙ РЫВОК", () => StartPulseTrial(PulseContentIds.VenomRush), false));
                modal.Add(ActionButton("ТРИ РЕГИОНА", () => StartPulseTrial(PulseContentIds.ThreeRegions), false));
                var completed = 0;
                var wins = 0;
                foreach (var record in _director.Profile.PulseRecords)
                {
                    completed += record.RunsCompleted;
                    wins += record.Wins;
                }
                modal.Add(LabelText("Личные записи: " + wins + " побед из " + completed + " завершённых забегов.",
                    16, Muted, TextAnchor.MiddleCenter));
            });
        }

        private void StartPulseRun(ContentId presetId)
        {
            var ticks = DateTime.UtcNow.Ticks;
            var seed = unchecked((ulong)ticks ^ ((ulong)Environment.TickCount << 32));
            var result = _director.StartPulseRun(seed == 0 ? 1UL : seed, presetId);
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void StartPulseTrial(ContentId trialId)
        {
            RunActionResult result;
            if (trialId.Equals(PulseContentIds.FrozenOpening))
                result = _director.StartPulseRun(0xF20A9UL, PulseContentIds.Standard, 0, 0, trialId,
                    default(ContentId), BoardContentIds.Frozen, 4);
            else if (trialId.Equals(PulseContentIds.VenomRush))
                result = _director.StartPulseRun(0xBEE771UL, PulseContentIds.Intense, 1, 1, trialId,
                    ProgressionContentIds.GalvanicVenom, BoardContentIds.Thorned, 3);
            else
                result = _director.StartPulseRun(0x3A117UL, PulseContentIds.Standard, 0, 2,
                    PulseContentIds.ThreeRegions);
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private static float PulseFraction(PulseState pulse)
        {
            return pulse == null || pulse.PulseDurationMilliseconds <= 0 ? 0f :
                Mathf.Clamp01((float)pulse.PulseRemainingMilliseconds / pulse.PulseDurationMilliseconds);
        }

        private static string FormatPulseTime(int milliseconds)
        {
            var span = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00") + "." +
                   (span.Milliseconds / 100).ToString();
        }
    }
}
