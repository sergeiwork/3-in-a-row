using System;
using System.Collections;
using System.Collections.Generic;
using ThreeInARow.Application;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.Map;
using ThreeInARow.Domain.Mastery;
using ThreeInARow.Domain.Meta;
using ThreeInARow.Domain.State;
using ThreeInARow.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThreeInARow.Presentation
{
    public sealed partial class ThreeInARowApp
    {
        private const float SwapDuration = 0.22f;
        private const float ClearDuration = 0.21f;
        private const float MinimumDropDuration = 0.23f;
        private const float MaximumDropDuration = 0.42f;
        private const float LandingDuration = 0.13f;
        private const float MaximumAnimationFrameDelta = 1f / 30f;
        private VisualElement _feedbackLayer;

        private IEnumerator AnimateBatch(EventBatch events, Action finished)
        {
            _inputLocked = true;
            InitializePresentedHealth(events);
            _hasPlayerAttackOrigin = false;
            var played = new HashSet<string>(StringComparer.Ordinal);
            var cascadeStep = 0;
            for (var eventIndex = 0; eventIndex < events.Events.Count; eventIndex++)
            {
                var item = events.Events[eventIndex];
                if (item.Type == SimulationEventType.GemMoved || item.Type == SimulationEventType.GemSpawned)
                {
                    var motionEvents = new List<SimulationEvent>();
                    while (eventIndex < events.Events.Count &&
                           (events.Events[eventIndex].Type == SimulationEventType.GemMoved ||
                            events.Events[eventIndex].Type == SimulationEventType.GemSpawned))
                    {
                        var motionEvent = events.Events[eventIndex];
                        ShowEventCue(motionEvent);
                        PlayEventSound(motionEvent, played);
                        motionEvents.Add(motionEvent);
                        eventIndex++;
                    }
                    eventIndex--;
                    yield return AnimateGravity(motionEvents);
                    continue;
                }
                if (item.Type == SimulationEventType.GemCleared)
                {
                    var resolutionEvents = new List<SimulationEvent>();
                    var clearEvents = new List<SimulationEvent>();
                    while (eventIndex < events.Events.Count &&
                           events.Events[eventIndex].Type != SimulationEventType.GemMoved &&
                           events.Events[eventIndex].Type != SimulationEventType.GemSpawned &&
                           events.Events[eventIndex].Type != SimulationEventType.GemsMatched &&
                           events.Events[eventIndex].Type != SimulationEventType.BoardReshuffled)
                    {
                        var resolutionEvent = events.Events[eventIndex];
                        resolutionEvents.Add(resolutionEvent);
                        if (resolutionEvent.Type == SimulationEventType.GemCleared)
                            clearEvents.Add(resolutionEvent);
                        eventIndex++;
                    }
                    eventIndex--;

                    cascadeStep++;
                    PlayClearSound(clearEvents, cascadeStep);
                    if (cascadeStep > 1) LaunchCascadeBanner(cascadeStep);

                    // Present combat consequences at the instant the matched gems fire. The
                    // clear, projectile, and hit effects then overlap instead of making damage
                    // wait for the full disappearance animation.
                    foreach (var resolutionEvent in resolutionEvents)
                    {
                        if (resolutionEvent.Type == SimulationEventType.SpecialCreated)
                            ApplySpecialVisual(resolutionEvent);
                        ShowEventCue(resolutionEvent);
                        PlayEventSound(resolutionEvent, played);
                    }
                    yield return AnimateClears(clearEvents);
                    continue;
                }
                if (item.Type == SimulationEventType.SpecialCreated)
                    ApplySpecialVisual(item);
                ShowEventCue(item);
                PlayEventSound(item, played);
                if (item.Type == SimulationEventType.SwapAccepted)
                {
                    yield return AnimateSwap(item);
                    continue;
                }
                // Pace decisions, not bookkeeping: resources/statuses from one impact are shown
                // together. The response gets one readable wind-up, never one pause per effect.
                if (!_reducedMotion && item.Type == SimulationEventType.EnemyIntentStarted)
                    yield return AnimateEnemyAttack();
                else if (!_reducedMotion && item.Type == SimulationEventType.EnemyDefeated)
                    yield return new WaitForSecondsRealtime(0.18f);
            }
            _inputLocked = false;
            finished?.Invoke();
        }

        private IEnumerator AnimateSwap(SimulationEvent item)
        {
            if (!item.HasCell || !item.HasTargetCell) yield break;
            VisualElement first;
            VisualElement second;
            if (!_gemVisuals.TryGetValue(item.Cell, out first) ||
                !_gemVisuals.TryGetValue(item.TargetCell, out second)) yield break;

            // Move both gems into the shared foreground layer before animating. Leaving translated
            // gems parented to their old cells accumulates offsets over later swaps and can produce
            // a one-frame jump when gravity reparents them.
            if (_gemMotionLayer == null) yield break;
            var firstBounds = first.worldBound;
            var secondBounds = second.worldBound;
            PlaceGemOnMotionLayer(first, firstBounds);
            PlaceGemOnMotionLayer(second, secondBounds);

            var firstDelta = secondBounds.center - firstBounds.center;
            var secondDelta = -firstDelta;
            var motions = new List<GemMotion>
            {
                new GemMotion(first, Vector3.zero, (Vector3)firstDelta, SwapDuration),
                new GemMotion(second, Vector3.zero, (Vector3)secondDelta, SwapDuration)
            };
            if (!_reducedMotion)
                yield return AnimateMotions(motions, MotionCurve.Smooth);
            else
                foreach (var motion in motions) SetTranslation(motion.Visual, motion.End);

            DockGemVisual(first, item.TargetCell);
            DockGemVisual(second, item.Cell);

            _gemVisuals[item.Cell] = second;
            _gemVisuals[item.TargetCell] = first;

            GemVisualIdentity firstIdentity;
            GemVisualIdentity secondIdentity;
            if (_visualGemStates.TryGetValue(item.Cell, out firstIdentity) &&
                _visualGemStates.TryGetValue(item.TargetCell, out secondIdentity))
            {
                _visualGemStates[item.Cell] = secondIdentity;
                _visualGemStates[item.TargetCell] = firstIdentity;
            }
        }

        private IEnumerator AnimateClears(List<SimulationEvent> items)
        {
            var visuals = new List<VisualElement>();
            var clearedCells = new List<GridCell>();
            foreach (var item in items)
            {
                if (!item.HasCell) continue;
                clearedCells.Add(item.Cell);
                VisualElement visual;
                if (_gemVisuals.TryGetValue(item.Cell, out visual)) visuals.Add(visual);
            }

            if (!_reducedMotion && visuals.Count > 0)
            {
                var elapsed = 0f;
                while (elapsed < ClearDuration)
                {
                    elapsed += AnimationDeltaTime();
                    var progress = Mathf.Clamp01(elapsed / ClearDuration);
                    // Keep the anticipation restrained and join both phases at zero velocity.
                    // This preserves the match beat without a visible pop before gravity begins.
                    const float anticipationEnd = 0.32f;
                    var anticipation = SmootherStep(Mathf.Clamp01(progress / anticipationEnd));
                    var release = SmootherStep(Mathf.Clamp01((progress - anticipationEnd) / (1f - anticipationEnd)));
                    var scale = progress < anticipationEnd
                        ? Mathf.Lerp(1f, 1.045f, anticipation)
                        : Mathf.Lerp(1.045f, 0.72f, release);
                    foreach (var visual in visuals)
                    {
                        visual.style.scale = new Scale(new Vector3(scale, scale, 1f));
                        visual.style.opacity = 1f - release;
                    }
                    yield return null;
                }
            }

            foreach (var visual in visuals) visual.RemoveFromHierarchy();
            foreach (var cell in clearedCells)
            {
                _gemVisuals.Remove(cell);
                _visualGemStates.Remove(cell);
            }
        }

        private IEnumerator AnimateGravity(List<SimulationEvent> items)
        {
            var motions = new List<GemMotion>();
            var movedItems = new List<SimulationEvent>();
            var movedVisuals = new List<VisualElement>();
            var spawnedItems = new List<SimulationEvent>();
            var spawnedVisuals = new List<VisualElement>();
            var spawnOrdinals = new Dictionary<int, int>();

            foreach (var item in items)
            {
                if (item.Type == SimulationEventType.GemMoved)
                {
                    if (!item.HasCell || !item.HasTargetCell) continue;
                    VisualElement visual;
                    if (!_gemVisuals.TryGetValue(item.Cell, out visual))
                    {
                        VisualElement sourceCell;
                        if (!_boardCells.TryGetValue(item.Cell, out sourceCell)) continue;
                        visual = CreateMotionGem(item.SourceId, item.RelatedId);
                        sourceCell.Add(visual);
                    }

                    var startCenter = (Vector2)visual.worldBound.center;
                    if (!LiftGemVisual(visual)) continue;
                    var delta = CellCenter(item.TargetCell) - startCenter;
                    var travelRows = Mathf.Abs(item.TargetCell.Row - item.Cell.Row);
                    motions.Add(new GemMotion(
                        visual,
                        Vector3.zero,
                        (Vector3)delta,
                        DropDuration(travelRows)));
                    movedItems.Add(item);
                    movedVisuals.Add(visual);
                    continue;
                }

                if (item.Type != SimulationEventType.GemSpawned || !item.HasTargetCell) continue;
                VisualElement targetCell;
                if (!_boardCells.TryGetValue(item.TargetCell, out targetCell) || _gemMotionLayer == null) continue;

                var spawnVisual = CreateMotionGem(item.SourceId, item.RelatedId);
                PlaceGemOnMotionLayer(spawnVisual, targetCell.worldBound);

                int ordinal;
                spawnOrdinals.TryGetValue(item.TargetCell.Column, out ordinal);
                spawnOrdinals[item.TargetCell.Column] = ordinal + 1;

                // Virtual rows begin immediately above the board and continue upward. This keeps
                // refill gems in a column stacked instead of drawing all of them on one another.
                var dropRows = BoardState.Height + ordinal - item.TargetCell.Row;
                var start = new Vector3(0f, -targetCell.worldBound.height * dropRows, 0f);
                SetTranslation(spawnVisual, start);
                spawnVisual.style.opacity = 0.35f;
                motions.Add(new GemMotion(
                    spawnVisual,
                    start,
                    Vector3.zero,
                    DropDuration(dropRows),
                    true));
                spawnedItems.Add(item);
                spawnedVisuals.Add(spawnVisual);
            }

            if (!_reducedMotion && motions.Count > 0)
                yield return AnimateMotions(motions, MotionCurve.Drop);
            else
                foreach (var motion in motions) SetTranslation(motion.Visual, motion.End);

            // Remove every old location before assigning destinations; chained one-row falls can
            // otherwise overwrite a dictionary entry that is still needed by the next move.
            foreach (var movedItem in movedItems)
            {
                _gemVisuals.Remove(movedItem.Cell);
                _visualGemStates.Remove(movedItem.Cell);
            }
            for (var index = 0; index < movedItems.Count; index++)
            {
                var item = movedItems[index];
                var visual = movedVisuals[index];
                DockGemVisual(visual, item.TargetCell);
                _gemVisuals[item.TargetCell] = visual;
                _visualGemStates[item.TargetCell] = new GemVisualIdentity(item.SourceId, item.RelatedId);
            }
            for (var index = 0; index < spawnedItems.Count; index++)
            {
                var item = spawnedItems[index];
                var visual = spawnedVisuals[index];
                DockGemVisual(visual, item.TargetCell);
                _gemVisuals[item.TargetCell] = visual;
                _visualGemStates[item.TargetCell] = new GemVisualIdentity(item.SourceId, item.RelatedId);
            }

            EnsureVisualBoardComplete();
        }

        private bool LiftGemVisual(VisualElement visual)
        {
            if (visual == null || _gemMotionLayer == null) return false;
            var bounds = visual.worldBound;
            PlaceGemOnMotionLayer(visual, bounds);
            return true;
        }

        private void PlaceGemOnMotionLayer(VisualElement visual, Rect worldBounds)
        {
            var layerBounds = _gemMotionLayer.worldBound;
            visual.RemoveFromHierarchy();
            _gemMotionLayer.Add(visual);
            visual.style.position = Position.Absolute;
            visual.style.left = worldBounds.xMin - layerBounds.xMin;
            visual.style.top = worldBounds.yMin - layerBounds.yMin;
            visual.style.right = StyleKeyword.Auto;
            visual.style.bottom = StyleKeyword.Auto;
            visual.style.width = worldBounds.width;
            visual.style.height = worldBounds.height;
            SetTranslation(visual, Vector3.zero);
        }

        private void DockGemVisual(VisualElement visual, GridCell destination)
        {
            VisualElement cell = null;
            if (visual == null || !_boardCells.TryGetValue(destination, out cell)) return;
            visual.RemoveFromHierarchy();
            cell.Add(visual);
            visual.style.position = Position.Absolute;
            visual.style.left = 0;
            visual.style.right = 0;
            visual.style.top = 0;
            visual.style.bottom = 0;
            visual.style.width = StyleKeyword.Auto;
            visual.style.height = StyleKeyword.Auto;
            visual.style.opacity = 1f;
            SetTranslation(visual, Vector3.zero);
        }

        private void EnsureVisualBoardComplete()
        {
            foreach (var entry in _visualGemStates)
            {
                VisualElement visual;
                if (_gemVisuals.TryGetValue(entry.Key, out visual) && visual != null && visual.parent != null)
                    continue;

                VisualElement cell;
                if (!_boardCells.TryGetValue(entry.Key, out cell)) continue;
                visual = CreateMotionGem(entry.Value.GemId, entry.Value.SpecialId);
                cell.Add(visual);
                _gemVisuals[entry.Key] = visual;
            }
        }

        private VisualElement CreateMotionGem(ContentId gemId, ContentId specialId)
        {
            var visual = new VisualElement { name = "gem-motion" };
            visual.pickingMode = PickingMode.Ignore;
            visual.style.position = Position.Absolute;
            visual.style.left = 0;
            visual.style.right = 0;
            visual.style.top = 0;
            visual.style.bottom = 0;
            var assetKey = !string.IsNullOrEmpty(specialId.Value) && specialId.Value != "special.none"
                ? specialId.Value
                : gemId.Value;
            var image = Icon(assetKey, 10);
            image.style.position = Position.Absolute;
            image.style.left = 5;
            image.style.right = 5;
            image.style.top = 5;
            image.style.bottom = 5;
            image.style.width = StyleKeyword.Auto;
            image.style.height = StyleKeyword.Auto;
            visual.Add(image);
            return visual;
        }

        private IEnumerator AnimateMotions(List<GemMotion> motions, MotionCurve curve)
        {
            var elapsed = 0f;
            var duration = 0f;
            foreach (var motion in motions) duration = Mathf.Max(duration, motion.Duration);
            if (curve == MotionCurve.Drop) duration += LandingDuration;
            while (elapsed < duration)
            {
                elapsed += AnimationDeltaTime();
                foreach (var motion in motions)
                {
                    var progress = Mathf.Clamp01(elapsed / motion.Duration);
                    var eased = curve == MotionCurve.Drop
                        ? EaseGravity(progress)
                        : SmootherStep(progress);
                    SetTranslation(motion.Visual, Vector3.LerpUnclamped(motion.Start, motion.End, eased));
                    if (curve == MotionCurve.Drop)
                    {
                        // Squared sine envelopes give travel and landing zero-rate joins. The
                        // gem arrives exactly on its cell, then settles without snapping scale.
                        var travelEnvelope = Mathf.Sin(progress * Mathf.PI);
                        var stretch = travelEnvelope * travelEnvelope * 0.025f;
                        var landing = Mathf.Clamp01((elapsed - motion.Duration) / LandingDuration);
                        var landingEnvelope = Mathf.Sin(landing * Mathf.PI);
                        var squash = elapsed < motion.Duration ? 0f : landingEnvelope * landingEnvelope * 0.035f;
                        motion.Visual.style.scale = new Scale(new Vector3(1f - stretch + squash, 1f + stretch - squash, 1f));
                    }
                    if (motion.FadeIn)
                        motion.Visual.style.opacity = Mathf.Lerp(0.55f, 1f, SmootherStep(Mathf.Clamp01(progress / 0.48f)));
                }
                yield return null;
            }
            foreach (var motion in motions)
            {
                SetTranslation(motion.Visual, motion.End);
                motion.Visual.style.scale = new Scale(Vector3.one);
                if (motion.FadeIn) motion.Visual.style.opacity = 1f;
            }
        }

        private static float EaseGravity(float progress)
        {
            // Bias a zero-slope S-curve toward the start. Falls gain speed early while still
            // entering and leaving motion continuously at cascade boundaries.
            var value = Mathf.Clamp01(progress);
            return SmootherStep(Mathf.Pow(value, 0.82f));
        }

        private static float DropDuration(int travelRows)
        {
            // Square-root scaling keeps long falls readable without making large cascades drag.
            return Mathf.Clamp(
                0.15f + Mathf.Sqrt(Mathf.Max(1, travelRows)) * 0.09f,
                MinimumDropDuration,
                MaximumDropDuration);
        }

        private void ApplySpecialVisual(SimulationEvent item)
        {
            if (!item.HasCell) return;
            VisualElement visual;
            if (!_gemVisuals.TryGetValue(item.Cell, out visual) || visual == null || visual.parent == null)
            {
                VisualElement cell;
                if (!_boardCells.TryGetValue(item.Cell, out cell)) return;
                visual = CreateMotionGem(item.RelatedId, item.SourceId);
                cell.Add(visual);
                _gemVisuals[item.Cell] = visual;
            }

            _visualGemStates[item.Cell] = new GemVisualIdentity(item.RelatedId, item.SourceId);
            visual.style.opacity = 1f;
            visual.style.scale = new Scale(Vector3.one);
            var image = visual.Q<Image>();
            if (image != null && _catalog != null) image.sprite = _catalog.GetSprite(item.SourceId.Value);
        }

        private Vector2 CellCenter(GridCell cell)
        {
            return _boardCells[cell].worldBound.center;
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value < 0.5f
                ? 4f * value * value * value
                : 1f - Mathf.Pow(-2f * value + 2f, 3f) * 0.5f;
        }

        private static float SmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private static float AnimationDeltaTime()
        {
            // A single slow frame should not skip most of a short board animation.
            return Mathf.Min(Time.unscaledDeltaTime, MaximumAnimationFrameDelta);
        }

        private static Vector3 Translation(VisualElement visual)
        {
            var translate = visual.resolvedStyle.translate;
            return new Vector3(translate.x, translate.y, translate.z);
        }

        private static void SetTranslation(VisualElement visual, Vector3 value)
        {
            visual.style.translate = new Translate(new Length(value.x), new Length(value.y), value.z);
        }

        private sealed class GemMotion
        {
            public readonly VisualElement Visual;
            public readonly Vector3 Start;
            public readonly Vector3 End;
            public readonly float Duration;
            public readonly bool FadeIn;

            public GemMotion(
                VisualElement visual,
                Vector3 start,
                Vector3 end,
                float duration,
                bool fadeIn = false)
            {
                Visual = visual;
                Start = start;
                End = end;
                Duration = Mathf.Max(0.001f, duration);
                FadeIn = fadeIn;
            }
        }

        private enum MotionCurve
        {
            Smooth,
            Drop
        }

        private readonly struct GemVisualIdentity
        {
            public readonly ContentId GemId;
            public readonly ContentId SpecialId;

            public GemVisualIdentity(ContentId gemId, ContentId specialId)
            {
                GemId = gemId;
                SpecialId = specialId;
            }
        }

        private void PlayBatch(EventBatch events, Action finished)
        {
            CapturePulseEventOrder(events);
            if (_root == null)
            {
                finished?.Invoke();
                return;
            }
            StartCoroutine(AnimateBatch(events ?? new EventBatch(), finished));
        }

        private void ShowEventCue(SimulationEvent item)
        {
            VisualElement cell;
            if (item.HasCell && _boardCells.TryGetValue(item.Cell, out cell))
            {
                if (item.Type == SimulationEventType.GemsMatched ||
                    item.Type == SimulationEventType.GemCleared ||
                    item.Type == SimulationEventType.SpecialActivated)
                {
                    _playerAttackOrigin = cell.worldBound.center;
                    _hasPlayerAttackOrigin = true;
                }
                // Keep border geometry constant: thickening it during a clear used to resize
                // the gem's content box just before it moved onto the foreground layer.
                SetBorder(cell, item.Type == SimulationEventType.StatusAdded ? Danger : Cyan, 2);
                cell.schedule.Execute(() => SetBorder(cell, Hex("#29434A"), 2)).StartingIn(160);
            }
            if (item.Type == SimulationEventType.DamageApplied)
            {
                ApplyPresentedDamage(item);
                LaunchDamageParticles(item);
                LaunchFloatingDamageNumber(item);
                if (Targets(item, "enemy")) TriggerEnemyDamageMotion();
            }
            else if (item.Type == SimulationEventType.EnemyBarrierChanged && item.Amount < 0)
            {
                LaunchDamageParticles(item, true);
                LaunchFloatingDamageNumber(item, true);
            }
            if (_message == null) return;
            if (item.Type == SimulationEventType.DamageApplied)
                SetMessage("«" + PresentationText.Name(item.SourceId) + "» наносит " + item.Amount + " урона.", Danger);
            else if (item.Type == SimulationEventType.EnemyIntentStarted)
                SetMessage("Враг применяет «" + PresentationText.Name(item.SourceId) + "».", Danger);
            else if (item.Type == SimulationEventType.SpecialCreated)
                SetMessage("Создан особый кристалл «" + PresentationText.Name(item.SourceId) + "»!", Gold);
            else if (item.Type == SimulationEventType.StatusAdded)
                SetMessage("Наложено состояние «" + PresentationText.Name(item.SourceId) + "».", Danger);
            else if (item.Type == SimulationEventType.StatusRemoved)
                SetMessage("Состояние «" + PresentationText.Name(item.SourceId) + "» снято.", Success);
            else if (item.Type == SimulationEventType.BoardReshuffled)
                SetMessage("Не осталось возможных ходов — поле перемешано.", Gold);
            else if (item.Type == SimulationEventType.GemTransmuted)
                SetMessage("Кристалл изменён: «" + PresentationText.Name(item.SourceId) + "».", Cyan);
            else if (item.Type == SimulationEventType.EnemyBarrierChanged)
                SetMessage(item.Amount > 0 ? "Враг получает барьер: " + item.Amount + "." : "Барьер врага поглощает урон.", Gold);
            else if (item.Type == SimulationEventType.ActiveJammed)
                SetMessage("Помеха увеличивает перезарядку «" + PresentationText.Name(item.RelatedId) + "».", Danger);
            else if (item.Type == SimulationEventType.BossPhaseChanged)
                SetMessage("Босс переходит во вторую фазу!", Danger);
            else if (item.Type == SimulationEventType.StatusTicked)
                SetMessage("Отравление срабатывает: " + item.Amount + " зар.", Success);
            else if (item.Type == SimulationEventType.EnemyPulseQueued)
                SetMessage("Намерение готово и поставлено в очередь.", Danger);
            else if (item.Type == SimulationEventType.EnemyPulseStarted)
                SetMessage("Импульс врага!", Danger);
            else if (item.Type == SimulationEventType.SurgeChanged &&
                     item.Detail.IndexOf("reason=activated", StringComparison.Ordinal) >= 0)
                SetMessage("Натиск активирован: таймер врага заморожен.", Cyan);

            var feedbackKey = item.Type == SimulationEventType.GemCleared ? "feedback.clear"
                : item.Type == SimulationEventType.SpecialCreated || item.Type == SimulationEventType.SpecialActivated
                    ? "feedback.special"
                : item.Type == SimulationEventType.DamageApplied ? "feedback.hit"
                : item.Type == SimulationEventType.StatusAdded ? "feedback.status_added"
                : item.Type == SimulationEventType.EnemyDefeated ? "ui.victory"
                : item.Type == SimulationEventType.RunEnded ? "ui.defeat"
                : string.Empty;
            ShowFeedbackSprite(feedbackKey, item);
        }

        private void InitializePresentedHealth(EventBatch events)
        {
            if (_director.State == null) return;
            _presentedEnemyHealth = _director.State.Enemy == null ? 0 : _director.State.Enemy.Health;
            _presentedPlayerHealth = _director.State.Player == null ? 0 : _director.State.Player.Health;
            foreach (var item in events.Events)
            {
                if (item.Type != SimulationEventType.DamageApplied) continue;
                if (Targets(item, "enemy")) _presentedEnemyHealth += item.Amount;
                else if (Targets(item, "player")) _presentedPlayerHealth += item.Amount;
            }
            _presentedEnemyHealth = Mathf.Clamp(_presentedEnemyHealth, 0, _presentedEnemyMaxHealth);
            _presentedPlayerHealth = Mathf.Clamp(_presentedPlayerHealth, 0, PlayerState.MaxHealth);
            RefreshPresentedHealth();
        }

        private void ApplyPresentedDamage(SimulationEvent item)
        {
            if (Targets(item, "enemy"))
                _presentedEnemyHealth = Mathf.Max(0, _presentedEnemyHealth - item.Amount);
            else if (Targets(item, "player"))
                _presentedPlayerHealth = Mathf.Max(0, _presentedPlayerHealth - item.Amount);
            RefreshPresentedHealth();
        }

        private void RefreshPresentedHealth()
        {
            if (_enemyHealthFill != null)
                _enemyHealthFill.style.width = Length.Percent(_presentedEnemyMaxHealth <= 0
                    ? 0f
                    : Mathf.Clamp01((float)_presentedEnemyHealth / _presentedEnemyMaxHealth) * 100f);
            if (_enemyHealthLabel != null)
                _enemyHealthLabel.text = "ЗДОРОВЬЕ " + _presentedEnemyHealth + " / " + _presentedEnemyMaxHealth;
            if (_playerHealthLabel != null)
                _playerHealthLabel.text = "ЗДОР.\n" + _presentedPlayerHealth + "/" + PlayerState.MaxHealth;
            if (_playerHealthChip != null)
                _playerHealthChip.tooltip = "ЗДОР.: " + _presentedPlayerHealth + " из " + PlayerState.MaxHealth;
        }

        private static bool Targets(SimulationEvent item, string target)
        {
            return item.Detail != null &&
                   item.Detail.IndexOf("target=" + target, StringComparison.Ordinal) >= 0;
        }

        private void StartEnemyIdleMotion()
        {
            if (_reducedMotion || _enemyPortrait == null || _enemyIdleRoutine != null) return;
            _enemyIdleRoutine = StartCoroutine(AnimateEnemyIdle());
        }

        private void StopEnemyIdleMotion()
        {
            if (_enemyIdleRoutine == null) return;
            StopCoroutine(_enemyIdleRoutine);
            _enemyIdleRoutine = null;
        }

        private IEnumerator AnimateEnemyIdle()
        {
            var duration = _enemyMotionProfile == EnemyMotionProfile.Hovering ? 2.6f
                : _enemyMotionProfile == EnemyMotionProfile.Heavy ? 3.1f
                : 2.2f;
            var elapsed = 0f;
            while (_enemyPortrait != null && _enemyPortrait.parent != null)
            {
                elapsed = (elapsed + AnimationDeltaTime()) % duration;
                var wave = Mathf.Sin(elapsed / duration * Mathf.PI * 2f);
                var vertical = _enemyMotionProfile == EnemyMotionProfile.Hovering ? wave * 3.2f
                    : _enemyMotionProfile == EnemyMotionProfile.Heavy ? wave * 0.7f
                    : wave * 1.2f;
                var scaleX = _enemyMotionProfile == EnemyMotionProfile.Grounded ? 1f + wave * 0.008f
                    : 1f + wave * 0.004f;
                var scaleY = _enemyMotionProfile == EnemyMotionProfile.Grounded ? 1f - wave * 0.006f
                    : _enemyMotionProfile == EnemyMotionProfile.Heavy ? 1f + wave * 0.006f
                    : 1f + wave * 0.004f;
                SetTranslation(_enemyPortrait, new Vector3(0f, vertical, 0f));
                _enemyPortrait.style.scale = new Scale(new Vector3(scaleX, scaleY, 1f));
                yield return null;
            }
            _enemyIdleRoutine = null;
        }

        private IEnumerator AnimateEnemyAttack()
        {
            if (_enemyPortrait == null) yield break;
            if (_enemyDamageRoutine != null)
            {
                StopCoroutine(_enemyDamageRoutine);
                _enemyDamageRoutine = null;
            }
            StopEnemyIdleMotion();
            ResetEnemyPortraitVisual();

            yield return AnimateEnemyTransform(
                Vector3.zero, new Vector3(0f, -3f, 0f),
                Vector3.one, new Vector3(0.97f, 1.03f, 1f), 0.10f);

            _enemyPortrait.sprite = _enemyPortraitAttackSprite;
            yield return AnimateEnemyTransform(
                new Vector3(0f, -3f, 0f), new Vector3(0f, 10f, 0f),
                new Vector3(0.97f, 1.03f, 1f), new Vector3(1.055f, 0.96f, 1f), 0.11f);
            yield return AnimateEnemyTransform(
                new Vector3(0f, 10f, 0f), Vector3.zero,
                new Vector3(1.055f, 0.96f, 1f), Vector3.one, 0.13f);

            ResetEnemyPortraitVisual();
            StartEnemyIdleMotion();
        }

        private IEnumerator AnimateEnemyTransform(
            Vector3 startPosition,
            Vector3 endPosition,
            Vector3 startScale,
            Vector3 endScale,
            float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration && _enemyPortrait != null && _enemyPortrait.parent != null)
            {
                elapsed += AnimationDeltaTime();
                var progress = SmootherStep(Mathf.Clamp01(elapsed / duration));
                SetTranslation(_enemyPortrait, Vector3.LerpUnclamped(startPosition, endPosition, progress));
                _enemyPortrait.style.scale = new Scale(Vector3.LerpUnclamped(startScale, endScale, progress));
                yield return null;
            }
        }

        private void TriggerEnemyDamageMotion()
        {
            if (_enemyPortrait == null) return;
            if (_enemyDamageRoutine != null) StopCoroutine(_enemyDamageRoutine);
            StopEnemyIdleMotion();
            ResetEnemyPortraitVisual();
            _enemyDamageRoutine = StartCoroutine(AnimateEnemyDamage());
        }

        private IEnumerator AnimateEnemyDamage()
        {
            var duration = _reducedMotion ? 0.10f : 0.20f;
            var elapsed = 0f;
            var hitTint = new Color(1f, 0.42f, 0.46f, 1f);
            while (elapsed < duration && _enemyPortrait != null && _enemyPortrait.parent != null)
            {
                elapsed += AnimationDeltaTime();
                var progress = Mathf.Clamp01(elapsed / duration);
                var envelope = Mathf.Sin(progress * Mathf.PI);
                envelope *= envelope;
                if (!_reducedMotion)
                {
                    var shake = Mathf.Sin(progress * Mathf.PI * 4f) * envelope * 5f;
                    SetTranslation(_enemyPortrait, new Vector3(shake, 0f, 0f));
                    _enemyPortrait.style.scale = new Scale(new Vector3(
                        1f + envelope * 0.03f,
                        1f - envelope * 0.04f,
                        1f));
                }
                _enemyPortrait.tintColor = Color.Lerp(Color.white, hitTint, envelope);
                yield return null;
            }
            ResetEnemyPortraitVisual();
            _enemyDamageRoutine = null;
            StartEnemyIdleMotion();
        }

        private void ResetEnemyPortraitVisual()
        {
            if (_enemyPortrait == null) return;
            _enemyPortrait.sprite = _enemyPortraitIdleSprite;
            _enemyPortrait.tintColor = Color.white;
            SetTranslation(_enemyPortrait, Vector3.zero);
            _enemyPortrait.style.scale = new Scale(Vector3.one);
        }

        private static EnemyMotionProfile EnemyMotionProfileFor(ContentId enemyId)
        {
            var value = enemyId.Value;
            if (value == "enemy.crystal_warden" || value == "enemy.facet_engine" ||
                value == "enemy.pyreheart_treant" || value == "enemy.furnace_matriarch" ||
                value == "enemy.astral_devourer" || value == "enemy.singularity_seraph")
                return EnemyMotionProfile.Heavy;
            if (value == "enemy.frost_oracle" || value == "enemy.rime_moth" ||
                value == "enemy.stormglass_roc" || value == "enemy.hollow_idol" ||
                value == "enemy.briar_wisp" || value == "enemy.sootcap_shaman" ||
                value == "enemy.cinder_nymph" || value == "enemy.glassvine_serpent" ||
                value == "enemy.nullwing_bat" || value == "enemy.mirror_eel" ||
                value == "enemy.shard_leech" || value == "enemy.rift_weaver" ||
                value == "enemy.orbit_sentinel")
                return EnemyMotionProfile.Hovering;
            return EnemyMotionProfile.Grounded;
        }

        private enum EnemyMotionProfile
        {
            Grounded,
            Hovering,
            Heavy
        }

        private void LaunchDamageParticles(SimulationEvent item, bool forceEnemyTarget = false)
        {
            if (_root == null) return;
            var targetsEnemy = forceEnemyTarget || Targets(item, "enemy");
            var targetAnchor = targetsEnemy ? _enemyFeedbackAnchor : _playerFeedbackAnchor;
            if (targetAnchor == null || targetAnchor.parent == null) return;

            var startAnchor = targetsEnemy ? _playerFeedbackAnchor : _enemyFeedbackAnchor;
            var start = targetsEnemy && _hasPlayerAttackOrigin
                ? _playerAttackOrigin
                : startAnchor != null && startAnchor.parent != null
                    ? startAnchor.worldBound.center
                    : targetAnchor.worldBound.center;
            var end = targetAnchor.worldBound.center;
            var rootOrigin = _root.worldBound.position;
            start -= rootOrigin;
            end -= rootOrigin;
            if (_reducedMotion) start = end;

            var color = DamageParticleColor(item.SourceId, targetsEnemy);
            var count = _reducedMotion ? 1 : 6;
            for (var index = 0; index < count; index++)
            {
                var size = index == 0 ? 18f : Mathf.Lerp(8f, 13f, (index % 3) / 2f);
                var particle = new VisualElement { name = "damage-particle" };
                particle.pickingMode = PickingMode.Ignore;
                particle.style.position = Position.Absolute;
                particle.style.width = size;
                particle.style.height = size;
                particle.style.borderTopLeftRadius = size;
                particle.style.borderTopRightRadius = size;
                particle.style.borderBottomLeftRadius = size;
                particle.style.borderBottomRightRadius = size;
                particle.style.backgroundColor = color;
                particle.style.opacity = 0.95f;
                particle.style.left = start.x - size * 0.5f;
                particle.style.top = start.y - size * 0.5f;
                AddFeedback(particle);
                var delay = _reducedMotion ? 0f : index * 0.018f;
                var arc = _reducedMotion ? 0f : (index % 2 == 0 ? 1f : -1f) * (18f + index * 3f);
                AnimateDamageParticle(particle, start, end,
                    _reducedMotion ? 0.12f : 0.36f, delay, arc, size, _reducedMotion);
            }
        }

        private static void AnimateDamageParticle(
            VisualElement particle,
            Vector2 start,
            Vector2 end,
            float duration,
            float delay,
            float arc,
            float size,
            bool reducedMotion)
        {
            AnimateFeedback(particle, duration, progress =>
            {
                var eased = EaseInOutCubic(progress);
                var position = Vector2.Lerp(start, end, eased);
                var arcEnvelope = Mathf.Sin(progress * Mathf.PI);
                position.y -= arcEnvelope * arcEnvelope * arc;
                SetTranslation(particle, (Vector3)(position - start));
                var fadeIn = SmootherStep(Mathf.Clamp01(progress / 0.16f));
                var fadeOut = SmootherStep(Mathf.Clamp01((progress - 0.68f) / 0.32f));
                particle.style.opacity = fadeIn * (1f - fadeOut);
                var pulse = Mathf.Sin(progress * Mathf.PI);
                pulse *= pulse;
                var scale = reducedMotion ? 1f : Mathf.Lerp(0.82f, 1.06f, pulse);
                particle.style.scale = new Scale(new Vector3(scale, scale, 1f));
            }, delay);
        }

        private static Color DamageParticleColor(ContentId sourceId, bool targetsEnemy)
        {
            var source = sourceId.Value ?? string.Empty;
            if (source.IndexOf("ember", StringComparison.Ordinal) >= 0 ||
                source.IndexOf("spark", StringComparison.Ordinal) >= 0) return Hex("#FF7A59");
            if (source.IndexOf("tide", StringComparison.Ordinal) >= 0 ||
                source.IndexOf("current", StringComparison.Ordinal) >= 0) return Cyan;
            if (source.IndexOf("venom", StringComparison.Ordinal) >= 0 ||
                source.IndexOf("poison", StringComparison.Ordinal) >= 0) return Success;
            if (source.IndexOf("volt", StringComparison.Ordinal) >= 0 ||
                source.IndexOf("charge", StringComparison.Ordinal) >= 0) return Gold;
            return targetsEnemy ? TextColor : Danger;
        }

        private void LaunchFloatingDamageNumber(SimulationEvent item, bool barrierDamage = false)
        {
            if (_root == null || (!barrierDamage && item.Amount <= 0)) return;
            var targetsEnemy = barrierDamage || Targets(item, "enemy");
            var targetAnchor = targetsEnemy ? _enemyFeedbackAnchor : _playerFeedbackAnchor;
            if (targetAnchor == null || targetAnchor.parent == null) return;

            var amount = Mathf.Abs(item.Amount);
            var text = barrierDamage ? "ЩИТ −" + amount : "−" + amount;
            var color = barrierDamage ? Gold : DamageParticleColor(item.SourceId, targetsEnemy);
            var number = LabelText(text, barrierDamage ? 23 : 30, color, TextAnchor.MiddleCenter);
            number.name = "floating-damage-number";
            number.pickingMode = PickingMode.Ignore;
            number.style.position = Position.Absolute;
            number.style.unityFontStyleAndWeight = FontStyle.Bold;
            number.style.backgroundColor = new Color(0.04f, 0.06f, 0.11f, 0.82f);
            number.style.paddingLeft = 8;
            number.style.paddingRight = 8;
            number.style.paddingTop = 3;
            number.style.paddingBottom = 3;
            number.style.borderTopLeftRadius = 9;
            number.style.borderTopRightRadius = 9;
            number.style.borderBottomLeftRadius = 9;
            number.style.borderBottomRightRadius = 9;

            var rootOrigin = _root.worldBound.position;
            var anchorCenter = targetAnchor.worldBound.center - rootOrigin;
            var horizontalOffset = ((item.Sequence % 3) - 1) * 18f;
            var startTop = anchorCenter.y - 24f;
            number.style.left = anchorCenter.x + horizontalOffset - 42f;
            number.style.top = startTop;
            number.style.width = 84f;
            number.style.opacity = 0f;
            number.style.scale = new Scale(_reducedMotion ? Vector3.one : new Vector3(0.86f, 0.86f, 1f));
            AddFeedback(number);
            AnimateFloatingDamageNumber(number, _reducedMotion ? 0f : 48f,
                _reducedMotion ? 0.32f : 0.62f);
        }

        private static void AnimateFloatingDamageNumber(
            VisualElement number,
            float lift,
            float duration)
        {
            AnimateFeedback(number, duration, progress =>
            {
                SetTranslation(number, new Vector3(0f, -SmootherStep(progress) * lift, 0f));
                var scale = lift == 0f ? 1f : Mathf.Lerp(0.86f, 1f, SmootherStep(Mathf.Clamp01(progress / 0.3f)));
                number.style.scale = new Scale(new Vector3(scale, scale, 1f));
                var fadeIn = SmootherStep(Mathf.Clamp01(progress / 0.12f));
                var fadeOut = SmootherStep(Mathf.Clamp01((progress - 0.55f) / 0.45f));
                number.style.opacity = fadeIn * (1f - fadeOut);
            });
        }

        private void ShowFeedbackSprite(string key, SimulationEvent item)
        {
            if (string.IsNullOrEmpty(key) || _root == null || _catalog == null || _catalog.GetSprite(key) == null) return;
            const float defaultSize = 150f;
            var size = defaultSize;
            var center = new Vector2(
                _root.worldBound.xMin + _root.worldBound.width * 0.5f,
                _root.worldBound.yMin + _root.worldBound.height * 0.42f);

            VisualElement cell = null;
            var hasBoardAnchor = item.HasCell && _boardCells.TryGetValue(item.Cell, out cell);
            if (!hasBoardAnchor && item.HasTargetCell)
                hasBoardAnchor = _boardCells.TryGetValue(item.TargetCell, out cell);
            if (hasBoardAnchor)
            {
                center = cell.worldBound.center;
                size = Mathf.Clamp(Mathf.Min(cell.worldBound.width, cell.worldBound.height) * 1.2f, 46f, 110f);
            }
            else if (item.Type == SimulationEventType.DamageApplied)
            {
                var damageAnchor = item.Detail.IndexOf("target=enemy", StringComparison.Ordinal) >= 0
                    ? _enemyFeedbackAnchor
                    : item.Detail.IndexOf("target=player", StringComparison.Ordinal) >= 0
                        ? _playerFeedbackAnchor
                        : null;
                if (damageAnchor != null && damageAnchor.parent != null)
                {
                    center = damageAnchor.worldBound.center;
                    size = Mathf.Clamp(
                        Mathf.Min(damageAnchor.worldBound.width, damageAnchor.worldBound.height) * 0.8f,
                        64f,
                        defaultSize);
                }
            }

            var image = Icon(key, size);
            image.name = "feedback-cue-" + key.Replace('.', '-') + "-" + item.Sequence + "-" + Time.frameCount;
            image.pickingMode = PickingMode.Ignore;
            image.style.position = Position.Absolute;
            image.style.left = center.x - _root.worldBound.xMin - size * 0.5f;
            image.style.top = center.y - _root.worldBound.yMin - size * 0.5f;
            image.style.opacity = 1f;
            image.style.scale = new Scale(_reducedMotion ? Vector3.one : new Vector3(0.82f, 0.82f, 1f));
            AddFeedback(image);
            AnimateFeedbackSprite(image, _reducedMotion ? 0.12f : 0.32f, _reducedMotion);
        }

        private static void AnimateFeedbackSprite(VisualElement image, float duration, bool reducedMotion)
        {
            AnimateFeedback(image, duration, progress =>
            {
                var arrival = SmootherStep(Mathf.Clamp01(progress / 0.42f));
                var settle = SmootherStep(Mathf.Clamp01((progress - 0.42f) / 0.58f));
                var scale = reducedMotion ? 1f : Mathf.Lerp(Mathf.Lerp(0.82f, 1.045f, arrival), 1f, settle);
                image.style.scale = new Scale(new Vector3(scale, scale, 1f));
                var fadeIn = SmootherStep(Mathf.Clamp01(progress / 0.12f));
                var fadeOut = SmootherStep(Mathf.Clamp01((progress - 0.5f) / 0.5f));
                image.style.opacity = fadeIn * (1f - fadeOut);
            });
        }

        private IEnumerator AnimateInvalidNudge()
        {
            if (_board == null) yield break;
            var board = _board;
            const float duration = 0.2f;
            const float amplitude = 6f;
            var elapsed = 0f;
            while (elapsed < duration && board != null && board.parent != null)
            {
                elapsed += AnimationDeltaTime();
                var progress = Mathf.Clamp01(elapsed / duration);
                var envelope = Mathf.Sin(progress * Mathf.PI);
                envelope *= envelope;
                var offset = Mathf.Sin(progress * Mathf.PI * 2f) * envelope * amplitude;
                SetTranslation(board, new Vector3(offset, 0f, 0f));
                yield return null;
            }
            if (board != null && board.parent != null) SetTranslation(board, Vector3.zero);
        }

        private void ClearScreenPreservingFeedback()
        {
            // Cosmetic feedback has its own lifetime and never holds gameplay input hostage.
            // Keeping its attached layer also lets UI schedules finish across a HUD rebuild.
            for (var index = _root.childCount - 1; index >= 0; index--)
                if (_root[index] != _feedbackLayer) _root.RemoveAt(index);
        }

        private void AddFeedback(VisualElement visual)
        {
            if (_feedbackLayer == null || _feedbackLayer.parent != _root)
            {
                _feedbackLayer = new VisualElement { name = "combat-feedback", pickingMode = PickingMode.Ignore };
                _feedbackLayer.style.position = Position.Absolute;
                _feedbackLayer.style.left = 0;
                _feedbackLayer.style.top = 0;
                _feedbackLayer.style.right = 0;
                _feedbackLayer.style.bottom = 0;
                _root.Add(_feedbackLayer);
            }
            _feedbackLayer.BringToFront();
            _feedbackLayer.Add(visual);
        }

        private static void AnimateFeedback(VisualElement visual, float duration, Action<float> update, float delay = 0f)
        {
            var started = Time.realtimeSinceStartupAsDouble + delay;
            if (delay > 0f) visual.style.opacity = 0f;
            else update(0f);
            IVisualElementScheduledItem schedule = null;
            schedule = visual.schedule.Execute(() =>
            {
                var elapsed = (float)(Time.realtimeSinceStartupAsDouble - started);
                if (elapsed < 0f) return;
                var progress = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
                update(progress);
                if (progress < 1f) return;
                schedule.Pause();
                visual.RemoveFromHierarchy();
            }).Every(16);
        }

        private void LaunchCascadeBanner(int cascadeStep)
        {
            if (_board == null || _root == null) return;
            var banner = LabelText("КАСКАД ×" + cascadeStep, 25, Gold, TextAnchor.MiddleCenter);
            banner.name = "cascade-feedback";
            banner.pickingMode = PickingMode.Ignore;
            banner.style.position = Position.Absolute;
            banner.style.unityFontStyleAndWeight = FontStyle.Bold;
            banner.style.width = 220;
            banner.style.left = _board.worldBound.center.x - _root.worldBound.xMin - 110;
            banner.style.top = _board.worldBound.yMin - _root.worldBound.yMin + 12;
            banner.style.backgroundColor = new Color(0.035f, 0.055f, 0.09f, 0.9f);
            banner.style.paddingTop = 5;
            banner.style.paddingBottom = 5;
            AddFeedback(banner);
            AnimateFloatingDamageNumber(banner, _reducedMotion ? 0f : 26f, _reducedMotion ? 0.32f : 0.55f);
        }

    }
}
