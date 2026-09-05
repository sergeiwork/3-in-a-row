using System;
using System.Collections;
using System.Collections.Generic;
using ThreeInARow.Application;
using ThreeInARow.Domain.Board;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.State;
using ThreeInARow.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThreeInARow.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ThreeInARowApp : MonoBehaviour
    {
        private const string ReducedMotionKey = "three_in_a_row.reduced_motion";
        private const float SwapDuration = 0.20f;
        private const float ClearDuration = 0.14f;
        private const float MinimumDropDuration = 0.18f;
        private const float MaximumDropDuration = 0.36f;
        private const float MaximumAnimationFrameDelta = 1f / 30f;
        private static readonly Color Background = Hex("#111827");
        private static readonly Color Panel = Hex("#253247");
        private static readonly Color PanelLight = Hex("#34445E");
        private static readonly Color Gold = Hex("#F6C85F");
        private static readonly Color Cyan = Hex("#67D6E8");
        private static readonly Color TextColor = Hex("#F8FAFC");
        private static readonly Color Muted = Hex("#B8C4D8");
        private static readonly Color Danger = Hex("#F06C75");
        private static readonly Color Success = Hex("#6ED69B");

        private PresentationCatalog _catalog;
        private RunDirector _director;
        private UIDocument _document;
        private AudioSource _audioSource;
        private VisualElement _root;
        private VisualElement _safeArea;
        private VisualElement _board;
        private VisualElement _gemMotionLayer;
        private Label _message;
        private readonly Dictionary<GridCell, VisualElement> _boardCells = new Dictionary<GridCell, VisualElement>();
        private readonly Dictionary<GridCell, VisualElement> _gemVisuals = new Dictionary<GridCell, VisualElement>();
        private readonly Dictionary<GridCell, GemVisualIdentity> _visualGemStates =
            new Dictionary<GridCell, GemVisualIdentity>();
        private GridCell? _selectedCell;
        private GridCell? _pointerCell;
        private Vector2 _pointerStart;
        private bool _inputLocked;
        private bool _reducedMotion;
        private ContentId? _targetingSkill;
        private readonly List<GridCell> _skillTargets = new List<GridCell>();

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            UnityEngine.Application.targetFrameRate = 60;
            _reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            _catalog = Resources.Load<PresentationCatalog>("E0PresentationCatalog");
            _director = new RunDirector(new JsonCheckpointStore());
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _document = GetComponent<UIDocument>();
            if (_document == null) _document = gameObject.AddComponent<UIDocument>();
            var panelSettings = _document.panelSettings ?? Resources.Load<PanelSettings>("PortraitPanelSettings");
            if (panelSettings == null)
            {
                Debug.LogError("Не найден PortraitPanelSettings. Пересоберите ресурсы среды выполнения раздела E.");
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "Резервная портретная панель";
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1080, 1920);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
            }
            _document.panelSettings = panelSettings;
        }

        private void OnEnable()
        {
            StartCoroutine(InitializeNextFrame());
        }

        private IEnumerator InitializeNextFrame()
        {
            yield return null;
            _root = _document.rootVisualElement;
            _root.style.position = Position.Absolute;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.top = 0;
            _root.style.bottom = 0;
            _root.style.backgroundColor = Background;
            _root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
            BuildTitle();
        }

        private void ApplySafeArea()
        {
            if (_safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            var scaleX = _root.resolvedStyle.width / Screen.width;
            var scaleY = _root.resolvedStyle.height / Screen.height;
            _safeArea.style.paddingLeft = safe.xMin * scaleX;
            _safeArea.style.paddingRight = (Screen.width - safe.xMax) * scaleX;
            _safeArea.style.paddingBottom = safe.yMin * scaleY;
            _safeArea.style.paddingTop = (Screen.height - safe.yMax) * scaleY;
        }

        private void BeginScreen()
        {
            StopAllCoroutines();
            _root.Clear();
            _boardCells.Clear();
            _gemVisuals.Clear();
            _visualGemStates.Clear();
            _board = null;
            _gemMotionLayer = null;
            _message = null;
            _inputLocked = false;

            _safeArea = new VisualElement { name = "safe-area" };
            _safeArea.style.flexGrow = 1;
            _safeArea.style.paddingLeft = 24;
            _safeArea.style.paddingRight = 24;
            _safeArea.style.paddingTop = 18;
            _safeArea.style.paddingBottom = 18;
            _root.Add(_safeArea);
            ApplySafeArea();
        }

        private void BuildForCurrentScreen()
        {
            _targetingSkill = null;
            _skillTargets.Clear();
            switch (_director.Screen)
            {
                case RunScreen.Title: BuildTitle(); break;
                case RunScreen.Encounter:
                case RunScreen.SkillWindow: BuildEncounter(); break;
                case RunScreen.Reward: BuildReward(); break;
                case RunScreen.BetweenEncounters: BuildBetweenEncounters(); break;
                case RunScreen.Victory: BuildSummary(true); break;
                case RunScreen.Defeat: BuildSummary(false); break;
            }
        }

        private void BuildTitle()
        {
            BeginScreen();
            _safeArea.style.justifyContent = Justify.Center;
            _safeArea.style.alignItems = Align.Center;

            var crystal = Icon("gem.prism", 190);
            crystal.style.marginBottom = 22;
            _safeArea.Add(crystal);
            _safeArea.Add(Title("ТРИ В РЯД", 54, Gold));
            var subtitle = LabelText("КРИСТАЛЬНЫЙ РОГАЛИК", 24, Cyan, TextAnchor.MiddleCenter);
            subtitle.style.letterSpacing = 4;
            subtitle.style.marginBottom = 54;
            _safeArea.Add(subtitle);

            var start = ActionButton("НАЧАТЬ ЗАБЕГ", StartRun, true);
            start.tooltip = "Начать новый забег из пяти боёв.";
            _safeArea.Add(start);
            if (_director.CanResume)
            {
                var resume = ActionButton("ПРОДОЛЖИТЬ", ResumeRun, false);
                resume.tooltip = "Продолжить с последней сохранённой контрольной точки.";
                _safeArea.Add(resume);
            }
            _safeArea.Add(ActionButton("КАК ИГРАТЬ", () => BuildHelp(BuildTitle), false));
            _safeArea.Add(ActionButton("НАСТРОЙКИ И АВТОРЫ", BuildSettings, false));

            var version = LabelText("Вертикальный срез · v0.6", 18, Muted, TextAnchor.MiddleCenter);
            version.style.marginTop = 36;
            _safeArea.Add(version);
        }

        private void BuildSettings()
        {
            BeginScreen();
            _safeArea.Add(Title("НАСТРОЙКИ", 42, Gold));
            _safeArea.Add(Paragraph("Настройки отображения хранятся на этом устройстве. У каждого состояния на поле есть значок и описание по нажатию."));

            var motion = ActionButton(_reducedMotion ? "УМЕНЬШЕНИЕ ДВИЖЕНИЯ: ВКЛ." : "УМЕНЬШЕНИЕ ДВИЖЕНИЯ: ВЫКЛ.", () =>
            {
                _reducedMotion = !_reducedMotion;
                PlayerPrefs.SetInt(ReducedMotionKey, _reducedMotion ? 1 : 0);
                PlayerPrefs.Save();
                BuildSettings();
            }, true);
            motion.tooltip = "Включить или выключить необязательные движения и задержки анимации.";
            _safeArea.Add(motion);

            _safeArea.Add(ActionButton("КАК ИГРАТЬ", () => BuildHelp(BuildSettings), false));

            _safeArea.Add(SectionHeading("ОБЯЗАТЕЛЬНОЕ УКАЗАНИЕ АВТОРСТВА"));
            _safeArea.Add(Paragraph("Автор значков — Lorc. Опубликованы на game-icons.net по лицензии CC BY 3.0."));
            _safeArea.Add(ActionButton("ОТКРЫТЬ GAME-ICONS.NET", () => UnityEngine.Application.OpenURL("https://game-icons.net/"), false));
            _safeArea.Add(ActionButton("ОТКРЫТЬ CC BY 3.0", () => UnityEngine.Application.OpenURL("https://creativecommons.org/licenses/by/3.0/"), false));
            _safeArea.Add(SectionHeading("ДРУГИЕ АВТОРЫ"));
            _safeArea.Add(Paragraph("Кристаллы: Andrew Tidey · Интерфейс, отклик и звук: Kenney · Портреты врагов: временные материалы проекта."));
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            _safeArea.Add(spacer);
            _safeArea.Add(ActionButton("НАЗАД", BuildTitle, false));
        }

        private void BuildHelp(Action back)
        {
            BeginScreen();
            _safeArea.Add(Title("КАК ИГРАТЬ", 42, Gold));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.marginTop = 8;
            scroll.style.marginBottom = 8;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            scroll.Add(SectionHeading("ХОД БОЯ"));
            scroll.Add(Paragraph("1. До перестановки можно применить готовый активный навык.\n2. Поменяйте местами два соседних подвижных кристалла так, чтобы собрать ряд из трёх или больше. Неверная перестановка не расходует ход.\n3. Все совпадения и каскады срабатывают автоматически.\n4. Если враг выжил, он выполняет действие из панели «Далее»."));
            scroll.Add(Paragraph("Совпадение из четырёх создаёт особый кристалл того же цвета. Совпадение из пяти создаёт Призму. Нажмите на значок состояния прямо на поле, чтобы прочитать его правило."));

            scroll.Add(SectionHeading("КРИСТАЛЛЫ И РЕСУРСЫ"));
            scroll.Add(HelpRow("gem.ember", "ПЛАМЯ", "Каждый убранный кристалл наносит 4 прямого урона. Особая Искра наносит 16 урона."));
            scroll.Add(HelpRow("gem.tide", "ПРИЛИВ И КОНЦЕНТРАЦИЯ", "Каждый кристалл даёт 1 ед. концентрации. Каждые 3 ед. автоматически превращаются в 6 урона. Особый Поток даёт 5 ед."));
            scroll.Add(HelpRow("gem.venom", "ЯД И ТОКСИН", "Каждый кристалл даёт 1 ед. токсина. Каждые 5 ед. наносят 12 урона и дают врагу заряд отравления. Особая Спора даёт 5 ед."));
            scroll.Add(HelpRow("gem.volt", "РАЗРЯД", "Каждый кристалл наносит 2 урона. Каждые 3 убранных Разряда сокращают перезарядку обоих активных навыков на 1. Особый Заряд наносит 8 урона и тоже ускоряет оба навыка."));
            scroll.Add(HelpRow("gem.prism", "ПРИЗМА", "Поменяйте её с обычным кристаллом, чтобы убрать с поля все кристаллы этого цвета и получить их обычные эффекты."));
            scroll.Add(HelpRow("ui.shield", "ЩИТ", "Поглощает входящий урон раньше здоровья. Он защищает от ближайшего ответа врага и исчезает в начале следующей успешной перестановки."));

            scroll.Add(SectionHeading("АКТИВНЫЕ НАВЫКИ"));
            foreach (var skill in MvpProgressionContentCatalog.Instance.Skills)
                if (skill.SlotType == SkillSlotType.Active)
                    scroll.Add(HelpRow(skill.Id.Value, PresentationText.Name(skill.Id).ToUpperInvariant(), PresentationText.SkillDetails(skill)));

            scroll.Add(SectionHeading("ПАССИВНЫЕ УЛУЧШЕНИЯ"));
            scroll.Add(Paragraph("Пассивные улучшения начинают работать сразу после выбора и не требуют нажатия."));
            foreach (var skill in MvpProgressionContentCatalog.Instance.Skills)
                if (skill.SlotType == SkillSlotType.Passive)
                    scroll.Add(HelpRow(skill.Id.Value, PresentationText.Name(skill.Id).ToUpperInvariant(), PresentationText.SkillDetails(skill)));

            scroll.Add(SectionHeading("СОСТОЯНИЯ ПОЛЯ"));
            scroll.Add(HelpRow("status.frozen", "ЗАМОРОЗКА", PresentationText.StatusDescription("status.frozen")));
            scroll.Add(HelpRow("status.cracked", "ТРЕЩИНА", PresentationText.StatusDescription("status.cracked")));
            scroll.Add(HelpRow("status.anchored", "ЯКОРЬ", PresentationText.StatusDescription("status.anchored")));
            scroll.Add(HelpRow("status.poison", "ОТРАВЛЕНИЕ", PresentationText.StatusDescription("status.poison")));

            _safeArea.Add(scroll);
            _safeArea.Add(ActionButton("НАЗАД", back, false));
        }

        private void StartRun()
        {
            var ticks = DateTime.UtcNow.Ticks;
            var seed = unchecked((ulong)ticks ^ ((ulong)Environment.TickCount << 32));
            var result = _director.StartNewRun(seed == 0 ? 1UL : seed);
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void ResumeRun()
        {
            if (_director.Resume()) BuildForCurrentScreen();
            else
            {
                BuildTitle();
                ShowModal("НЕ УДАЛОСЬ ПРОДОЛЖИТЬ", "Контрольная точка отсутствует, повреждена или создана в неподдерживаемой версии.");
            }
        }

        private void BuildEncounter()
        {
            BeginScreen();
            var state = _director.State;
            var encounter = MvpCombatContentCatalog.Instance.GetEncounter(state.EncounterIndex);
            var enemy = encounter.Enemy;

            var top = Row();
            top.style.alignItems = Align.Center;
            var encounterLabel = LabelText("БОЙ " + (state.EncounterIndex + 1) + " / " + RunDirector.EncounterCount, 19, Muted);
            encounterLabel.style.flexGrow = 1;
            top.Add(encounterLabel);
            var settings = SmallButton("⚙", BuildSettingsFromRun);
            settings.tooltip = "Настройки и авторы";
            top.Add(settings);
            var help = SmallButton("?", () =>
            {
                if (!_inputLocked) BuildHelp(BuildForCurrentScreen);
            });
            help.tooltip = "Как играть и что делают навыки";
            top.Add(help);
            _safeArea.Add(top);

            var enemyPanel = Card();
            enemyPanel.style.flexDirection = FlexDirection.Row;
            enemyPanel.style.alignItems = Align.Center;
            enemyPanel.style.paddingTop = 12;
            enemyPanel.style.paddingBottom = 12;
            var portrait = Icon(enemy.Id.Value, 150);
            portrait.style.marginRight = 18;
            enemyPanel.Add(portrait);
            var enemyInfo = new VisualElement();
            enemyInfo.style.flexGrow = 1;
            enemyInfo.Add(Title(PresentationText.Name(enemy.Id), 31, TextColor));
            enemyInfo.Add(Bar("ЗДОРОВЬЕ " + state.Enemy.Health + " / " + enemy.MaxHealth,
                enemy.MaxHealth <= 0 ? 0 : (float)state.Enemy.Health / enemy.MaxHealth, Danger));
            if (state.Enemy.PoisonStacks > 0)
                enemyInfo.Add(InlineIconLabel("status.poison", "Отравление: " + state.Enemy.PoisonStacks, PresentationText.StatusDescription("status.poison")));
            enemyPanel.Add(enemyInfo);
            _safeArea.Add(enemyPanel);

            var intent = enemy.IntentCycle[PositiveModulo(state.Enemy.IntentIndex, enemy.IntentCycle.Count)];
            var intentPanel = Row();
            intentPanel.style.backgroundColor = Hex("#372F4F");
            intentPanel.style.paddingLeft = 12;
            intentPanel.style.paddingRight = 12;
            intentPanel.style.paddingTop = 7;
            intentPanel.style.paddingBottom = 7;
            intentPanel.style.marginTop = 7;
            intentPanel.style.marginBottom = 7;
            intentPanel.style.alignItems = Align.Center;
            foreach (var intentIcon in IntentAssetKeys(intent.TelegraphKey))
                intentPanel.Add(Icon(intentIcon, 54));
            var intentText = LabelText("ДАЛЕЕ: " + PresentationText.Name(intent.TelegraphKey) + "\n" + PresentationText.IntentDescription(intent), 19, TextColor);
            intentText.style.flexGrow = 1;
            intentText.style.marginLeft = 10;
            intentPanel.Add(intentText);
            _safeArea.Add(intentPanel);

            BuildBoard(state.Board);
            BuildResources(state);
            BuildSkills(state);

            _message = LabelText(_director.Screen == RunScreen.SkillWindow
                ? "Враг готовит ответ..."
                : "Нажмите на соседние кристаллы или проведите пальцем, чтобы собрать ряд.", 18, Muted, TextAnchor.MiddleCenter);
            _message.style.minHeight = 30;
            _message.style.marginTop = 4;
            _safeArea.Add(_message);

        }

        private void BuildSettingsFromRun()
        {
            ShowModal("НАСТРОЙКИ", null, modal =>
            {
                modal.Add(ActionButton(_reducedMotion ? "УМЕНЬШЕНИЕ ДВИЖЕНИЯ: ВКЛ." : "УМЕНЬШЕНИЕ ДВИЖЕНИЯ: ВЫКЛ.", () =>
                {
                    _reducedMotion = !_reducedMotion;
                    PlayerPrefs.SetInt(ReducedMotionKey, _reducedMotion ? 1 : 0);
                    PlayerPrefs.Save();
                    BuildForCurrentScreen();
                }, true));
                modal.Add(ActionButton("КАК ИГРАТЬ", () => BuildHelp(BuildForCurrentScreen), false));
                modal.Add(Paragraph("Автор значков — Lorc. Опубликованы на game-icons.net по лицензии CC BY 3.0."));
                modal.Add(ActionButton("GAME-ICONS.NET", () => UnityEngine.Application.OpenURL("https://game-icons.net/"), false));
                modal.Add(ActionButton("CC BY 3.0", () => UnityEngine.Application.OpenURL("https://creativecommons.org/licenses/by/3.0/"), false));
            });
        }

        private void BuildBoard(BoardState boardState)
        {
            _board = new VisualElement { name = "board" };
            _board.style.width = Length.Percent(100);
            _board.style.maxWidth = 720;
            _board.style.alignSelf = Align.Center;
            _board.style.backgroundColor = Hex("#0A1020");
            _board.style.paddingLeft = 5;
            _board.style.paddingRight = 5;
            _board.style.paddingTop = 5;
            _board.style.paddingBottom = 5;
            _board.style.borderTopLeftRadius = 18;
            _board.style.borderTopRightRadius = 18;
            _board.style.borderBottomLeftRadius = 18;
            _board.style.borderBottomRightRadius = 18;
            _board.style.overflow = Overflow.Visible;
            _board.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var size = Mathf.Min(evt.newRect.width, _safeArea.resolvedStyle.height * 0.5f);
                if (size > 10) _board.style.height = size;
            });

            for (var row = BoardState.Height - 1; row >= 0; row--)
            {
                var rowElement = Row();
                rowElement.style.flexGrow = 1;
                for (var column = 0; column < BoardState.Width; column++)
                {
                    var cell = new GridCell(column, row);
                    var gem = FindGem(boardState, cell);
                    var cellElement = BuildCell(gem);
                    rowElement.Add(cellElement);
                    _boardCells[cell] = cellElement;
                    _visualGemStates[cell] = new GemVisualIdentity(gem.GemId, gem.SpecialId);
                }
                _board.Add(rowElement);
            }

            // Falling gems live above the rows while in motion. Keeping them in a foreground layer
            // prevents later cell backgrounds from hiding a gem as it crosses row boundaries.
            _gemMotionLayer = new VisualElement { name = "gem-motion-layer" };
            _gemMotionLayer.pickingMode = PickingMode.Ignore;
            _gemMotionLayer.style.position = Position.Absolute;
            _gemMotionLayer.style.left = 0;
            _gemMotionLayer.style.right = 0;
            _gemMotionLayer.style.top = 0;
            _gemMotionLayer.style.bottom = 0;
            _gemMotionLayer.style.overflow = Overflow.Visible;
            _board.Add(_gemMotionLayer);
            _safeArea.Add(_board);
        }

        private VisualElement BuildCell(BoardGemState gem)
        {
            var cell = gem.Cell;
            var element = new VisualElement { name = "cell-" + cell.Column + "-" + cell.Row };
            element.focusable = true;
            element.tooltip = PresentationText.GemDescription(gem.GemId, gem.SpecialId) + StatusSuffix(gem);
            element.style.flexGrow = 1;
            element.style.marginLeft = 2;
            element.style.marginRight = 2;
            element.style.marginTop = 2;
            element.style.marginBottom = 2;
            element.style.backgroundColor = Hex("#1B2940");
            SetBorder(element, _selectedCell.HasValue && _selectedCell.Value.Equals(cell) ? Gold : Hex("#41516B"), 2);
            element.style.borderTopLeftRadius = 10;
            element.style.borderTopRightRadius = 10;
            element.style.borderBottomLeftRadius = 10;
            element.style.borderBottomRightRadius = 10;

            var assetKey = gem.SpecialId.Value != "special.none" ? gem.SpecialId.Value : gem.GemId.Value;
            var gemVisual = new VisualElement { name = "gem-visual" };
            gemVisual.pickingMode = PickingMode.Ignore;
            gemVisual.style.position = Position.Absolute;
            gemVisual.style.left = 0;
            gemVisual.style.right = 0;
            gemVisual.style.top = 0;
            gemVisual.style.bottom = 0;
            var image = Icon(assetKey, 10);
            image.style.position = Position.Absolute;
            image.style.left = 5;
            image.style.right = 5;
            image.style.top = 5;
            image.style.bottom = 5;
            image.style.width = StyleKeyword.Auto;
            image.style.height = StyleKeyword.Auto;
            gemVisual.Add(image);

            if (gem.StatusIds != null)
            {
                var offset = 1;
                foreach (var statusId in gem.StatusIds)
                {
                    var status = new Button();
                    status.name = "status-" + statusId.Value;
                    status.tooltip = PresentationText.StatusDescription(statusId.Value);
                    status.style.position = Position.Absolute;
                    status.style.width = 27;
                    status.style.height = 27;
                    status.style.right = offset;
                    status.style.top = 1;
                    status.style.paddingLeft = 0;
                    status.style.paddingRight = 0;
                    status.style.paddingTop = 0;
                    status.style.paddingBottom = 0;
                    status.style.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 0.84f);
                    var sprite = _catalog == null ? null : _catalog.GetSprite(statusId.Value);
                    if (sprite != null) status.style.backgroundImage = new StyleBackground(sprite);
                    var capturedStatus = statusId.Value;
                    status.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        ShowModal(PresentationText.Name(capturedStatus), PresentationText.StatusDescription(capturedStatus));
                    });
                    status.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                    status.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
                    gemVisual.Add(status);
                    offset += 23;
                }
            }

            var duration = FindDuration(gem);
            if (duration > 0)
            {
                var badge = LabelText(duration.ToString(), 15, TextColor, TextAnchor.MiddleCenter);
                badge.style.position = Position.Absolute;
                badge.style.right = 2;
                badge.style.bottom = 2;
                badge.style.width = 24;
                badge.style.height = 24;
                badge.style.backgroundColor = Danger;
                badge.style.borderTopLeftRadius = 12;
                badge.style.borderTopRightRadius = 12;
                badge.style.borderBottomLeftRadius = 12;
                badge.style.borderBottomRightRadius = 12;
                gemVisual.Add(badge);
            }

            element.Add(gemVisual);
            _gemVisuals[cell] = gemVisual;

            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_inputLocked || evt.button != 0) return;
                _pointerCell = cell;
                _pointerStart = evt.position;
                element.CapturePointer(evt.pointerId);
            });
            element.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!_pointerCell.HasValue || !_pointerCell.Value.Equals(cell)) return;
                if (element.HasPointerCapture(evt.pointerId)) element.ReleasePointer(evt.pointerId);
                var pointerPosition = new Vector2(evt.position.x, evt.position.y);
                var delta = pointerPosition - _pointerStart;
                _pointerCell = null;
                if (_inputLocked) return;
                if (delta.magnitude >= 24f && !_targetingSkill.HasValue)
                {
                    var horizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
                    var target = horizontal
                        ? new GridCell(cell.Column + (delta.x > 0 ? 1 : -1), cell.Row)
                        : new GridCell(cell.Column, cell.Row + (delta.y > 0 ? -1 : 1));
                    TrySwap(cell, target);
                }
                else
                {
                    CellTapped(cell, gem);
                }
            });
            return element;
        }

        private void CellTapped(GridCell cell, BoardGemState gem)
        {
            if (_targetingSkill.HasValue)
            {
                if (gem.StatusIds == null || gem.StatusIds.Count == 0)
                {
                    SetMessage("Выберите кристалл с заморозкой, трещиной или якорем.", Danger);
                    return;
                }
                var index = _skillTargets.FindIndex(value => value.Equals(cell));
                if (index >= 0) _skillTargets.RemoveAt(index);
                else if (_skillTargets.Count < 3) _skillTargets.Add(cell);
                else SetMessage("Очищение действует максимум на три кристалла.", Danger);
                RefreshCellSelections();
                return;
            }

            if (_director.Screen != RunScreen.Encounter) return;
            if (!_selectedCell.HasValue)
            {
                _selectedCell = cell;
                RefreshCellSelections();
                SetMessage("Выбран кристалл «" + elementAccessibleName(gem) + "». Укажите соседний.", Cyan);
                return;
            }
            var first = _selectedCell.Value;
            _selectedCell = null;
            RefreshCellSelections();
            if (first.Equals(cell)) return;
            TrySwap(first, cell);
        }

        private void TrySwap(GridCell first, GridCell second)
        {
            if (second.Column < 0 || second.Column >= BoardState.Width || second.Row < 0 || second.Row >= BoardState.Height)
            {
                RejectInput("Свайп выходит за границу поля.");
                return;
            }
            _inputLocked = true;
            var result = _director.Swap(first, second);
            if (!result.Accepted)
            {
                _inputLocked = false;
                RejectInput(RejectionText(result.Rejection));
                return;
            }
            PlayBatch(result.Events, ContinueAutomatically);
        }

        private void ContinueAutomatically()
        {
            if (_director.Screen == RunScreen.SkillWindow)
            {
                ContinueTurn();
                return;
            }
            BuildForCurrentScreen();
        }

        private void BuildResources(RunState state)
        {
            var resources = Row();
            resources.style.justifyContent = Justify.SpaceBetween;
            resources.style.marginTop = 7;
            resources.Add(ResourceChip("ui.player_health", "ЗДОР.", state.Player.Health, PlayerState.MaxHealth, Danger));
            resources.Add(ResourceChip("ui.focus", "ФОКУС", state.Player.Focus, 9, Cyan));
            resources.Add(ResourceChip("ui.toxic", "ТОКСИН", state.Player.Toxic, 9, Success));
            resources.Add(ResourceChip("ui.shield", "ЩИТ", state.Player.Shield, -1, Gold));
            _safeArea.Add(resources);
        }

        private void BuildSkills(RunState state)
        {
            var row = Row();
            row.style.marginTop = 5;
            if (state.Player.EquippedActiveSkillIds != null)
            {
                for (var slot = 0; slot < state.Player.EquippedActiveSkillIds.Count; slot++)
                {
                    var skillId = state.Player.EquippedActiveSkillIds[slot];
                    var definition = MvpProgressionContentCatalog.Instance.GetSkill(skillId);
                    var cooldown = FindCooldown(state.Player, skillId);
                    var ready = (_director.Screen == RunScreen.Encounter || _director.Screen == RunScreen.SkillWindow) &&
                                cooldown == 0 && SkillHasEffect(state, definition);
                    var skillPanel = new VisualElement();
                    skillPanel.style.flexGrow = 1;
                    skillPanel.style.marginLeft = 3;
                    skillPanel.style.marginRight = 3;
                    var button = new Button(() => SkillPressed(definition));
                    button.style.width = Length.Percent(100);
                    button.style.height = 70;
                    button.style.flexDirection = FlexDirection.Row;
                    button.style.alignItems = Align.Center;
                    button.style.justifyContent = Justify.Center;
                    button.style.backgroundColor = ready ? Hex("#285A6A") : Hex("#303847");
                    button.SetEnabled(ready);
                    button.tooltip = PresentationText.SkillDescription(definition);
                    button.Add(Icon(skillId.Value, 40));
                    var label = LabelText(PresentationText.Name(skillId) + (cooldown > 0 ? " · " + cooldown : " · ГОТОВО"), 19, TextColor);
                    label.style.marginLeft = 8;
                    button.Add(label);
                    skillPanel.Add(button);
                    var info = SmallButton("О НАВЫКЕ", () => ShowSkillDetails(definition));
                    info.style.width = Length.Percent(100);
                    info.style.height = 34;
                    info.style.marginLeft = 0;
                    info.style.marginRight = 0;
                    info.style.marginTop = 3;
                    info.style.fontSize = 14;
                    info.tooltip = "Открыть полное описание навыка";
                    skillPanel.Add(info);
                    row.Add(skillPanel);
                }
            }
            _safeArea.Add(row);
        }

        private void SkillPressed(SkillDefinition definition)
        {
            if (_inputLocked) return;
            if (definition.TargetPolicy == SkillTargetPolicy.UpToThreeStatusGems)
            {
                _targetingSkill = definition.Id;
                _skillTargets.Clear();
                ShowCleanseTargeting(definition);
                return;
            }
            ExecuteSkill(definition.Id, null);
        }

        private void ShowSkillDetails(SkillDefinition skill)
        {
            ShowModal(PresentationText.Name(skill.Id), null, modal =>
            {
                var category = skill.SlotType == SkillSlotType.Active
                    ? "АКТИВНЫЙ НАВЫК · ПЕРЕЗАРЯДКА " + skill.Cooldown + " ХОДОВ"
                    : "ПАССИВНОЕ УЛУЧШЕНИЕ · РАБОТАЕТ АВТОМАТИЧЕСКИ";
                var categoryLabel = LabelText(category, 17, Cyan, TextAnchor.MiddleCenter);
                categoryLabel.style.whiteSpace = WhiteSpace.Normal;
                categoryLabel.style.marginBottom = 8;
                modal.Add(categoryLabel);

                var icon = Icon(skill.Id.Value, 84);
                icon.style.alignSelf = Align.Center;
                modal.Add(icon);
                modal.Add(Paragraph(PresentationText.SkillDetails(skill)));

                if (skill.HasPrerequisite)
                    modal.Add(LabelText("Требуется: " + PresentationText.Name(skill.PrerequisiteId), 17, Success));

                if (skill.SlotType == SkillSlotType.Active && _director.State != null)
                {
                    var state = _director.State;
                    string current;
                    if (!ProgressionRules.IsEquipped(state.Player, skill.Id))
                        current = "Сейчас не экипирован. Поставить навык в левую или правую ячейку можно между боями.";
                    else
                    {
                        var cooldown = FindCooldown(state.Player, skill.Id);
                        if (cooldown > 0)
                            current = "Сейчас перезаряжается: осталось " + cooldown + " " + RussianTurns(cooldown) + ".";
                        else if (_director.Screen != RunScreen.Encounter && _director.Screen != RunScreen.SkillWindow)
                            current = "Экипирован и будет готов к применению перед перестановкой.";
                        else if (!SkillHasEffect(state, skill))
                            current = "Сейчас применять рано: нет подходящей цели или ресурса для эффекта.";
                        else
                            current = "Сейчас готов к применению перед перестановкой.";
                    }
                    var stateLabel = LabelText(current, 17, Gold);
                    stateLabel.style.whiteSpace = WhiteSpace.Normal;
                    stateLabel.style.marginTop = 8;
                    modal.Add(stateLabel);
                }
            });
        }

        private void ShowCleanseTargeting(SkillDefinition definition)
        {
            var eligible = CountStatusGems(_director.State.Board);
            SetMessage(eligible <= 3
                ? "Выберите цели или подтвердите без выбора, чтобы очистить все: " + eligible + "."
                : "Выберите от одного до трёх кристаллов с состояниями и подтвердите.", Gold);

            var controls = Row();
            controls.name = "targeting-controls";
            var confirm = ActionButton("ПОДТВЕРДИТЬ ОЧИЩЕНИЕ", () =>
            {
                if (_skillTargets.Count == 0 && eligible > 3)
                {
                    SetMessage("Выберите хотя бы один кристалл с состоянием.", Danger);
                    return;
                }
                ExecuteSkill(definition.Id, _skillTargets);
            }, true);
            confirm.style.flexGrow = 1;
            var cancel = ActionButton("ОТМЕНА", () =>
            {
                _targetingSkill = null;
                _skillTargets.Clear();
                BuildEncounter();
            }, false);
            cancel.style.flexGrow = 1;
            controls.Add(confirm);
            controls.Add(cancel);
            _safeArea.Add(controls);
        }

        private void ExecuteSkill(ContentId skillId, IEnumerable<GridCell> targets)
        {
            _inputLocked = true;
            var result = _director.UseSkill(skillId, targets);
            if (!result.Accepted)
            {
                _inputLocked = false;
                SetMessage(RejectionText(result.Rejection), Danger);
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void ContinueTurn()
        {
            if (_inputLocked) return;
            _inputLocked = true;
            var result = _director.ContinueTurn();
            if (!result.Accepted)
            {
                _inputLocked = false;
                SetMessage(result.Rejection, Danger);
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void BuildReward()
        {
            BeginScreen();
            var state = _director.State;
            _safeArea.Add(Icon("ui.level_up", 100));
            _safeArea.Add(Title("УРОВЕНЬ " + state.PendingChoice.Level, 46, Gold));
            _safeArea.Add(LabelText("Нажмите карточку, прочитайте полное описание и выберите одно улучшение.", 20, Muted, TextAnchor.MiddleCenter));

            var cards = new VisualElement();
            cards.style.flexGrow = 1;
            cards.style.justifyContent = Justify.Center;
            foreach (var optionId in state.PendingChoice.OptionIds)
            {
                var skill = MvpProgressionContentCatalog.Instance.GetSkill(optionId);
                var capturedSkill = skill;
                var card = new Button(() => ShowRewardDetails(capturedSkill));
                card.style.minHeight = 170;
                card.style.marginTop = 8;
                card.style.marginBottom = 8;
                card.style.paddingLeft = 18;
                card.style.paddingRight = 18;
                card.style.paddingTop = 14;
                card.style.paddingBottom = 14;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.backgroundColor = Panel;
                card.tooltip = "Открыть полное описание и выбрать улучшение";
                card.Add(Icon(optionId.Value, 100));
                var text = new VisualElement();
                text.style.flexGrow = 1;
                text.style.marginLeft = 20;
                text.Add(Title(PresentationText.Name(optionId), 30, Gold));
                var description = LabelText(PresentationText.SkillDescription(skill), 20, TextColor);
                description.style.whiteSpace = WhiteSpace.Normal;
                text.Add(description);
                if (skill.HasPrerequisite)
                    text.Add(LabelText("Требуется: " + PresentationText.Name(skill.PrerequisiteId) + " ✓", 17, Success));
                card.Add(text);
                cards.Add(card);
            }
            _safeArea.Add(cards);
        }

        private void SelectReward(ContentId rewardId)
        {
            if (_inputLocked) return;
            _inputLocked = true;
            var result = _director.SelectReward(rewardId);
            if (!result.Accepted)
            {
                _inputLocked = false;
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void ShowRewardDetails(SkillDefinition skill)
        {
            ShowModal(PresentationText.Name(skill.Id), null, modal =>
            {
                var category = skill.SlotType == SkillSlotType.Active
                    ? "НОВЫЙ АКТИВНЫЙ НАВЫК"
                    : "ПАССИВНОЕ УЛУЧШЕНИЕ";
                modal.Add(LabelText(category, 18, Cyan, TextAnchor.MiddleCenter));
                var icon = Icon(skill.Id.Value, 92);
                icon.style.alignSelf = Align.Center;
                modal.Add(icon);
                modal.Add(Paragraph(PresentationText.SkillDetails(skill)));
                if (skill.HasPrerequisite)
                    modal.Add(LabelText("Требование выполнено: " + PresentationText.Name(skill.PrerequisiteId) + " ✓", 17, Success));
                var choose = ActionButton("ВЫБРАТЬ «" + PresentationText.Name(skill.Id).ToUpperInvariant() + "»", () =>
                {
                    if (modal.parent != null) modal.parent.RemoveFromHierarchy();
                    SelectReward(skill.Id);
                }, true);
                choose.tooltip = "Подтвердить это улучшение";
                modal.Add(choose);
            });
        }

        private void BuildBetweenEncounters()
        {
            BeginScreen();
            var state = _director.State;
            _safeArea.Add(Icon("ui.victory", 130));
            _safeArea.Add(Title("ВРАГ ПОВЕРЖЕН", 42, Gold));
            _safeArea.Add(LabelText("Восстановлено 4 здоровья · Сейчас " + state.Player.Health + " / " + PlayerState.MaxHealth,
                21, TextColor, TextAnchor.MiddleCenter));
            _safeArea.Add(SectionHeading("АКТИВНЫЕ НАВЫКИ"));
            _safeArea.Add(Paragraph("Изученные активные навыки сохраняют перезарядку после снятия. Перед продолжением выберите левую или правую ячейку."));

            foreach (var skill in MvpProgressionContentCatalog.Instance.Skills)
            {
                if (skill.SlotType != SkillSlotType.Active || !Contains(state.SelectedSkillIds, skill.Id)) continue;
                var line = Card();
                line.style.flexDirection = FlexDirection.Row;
                line.style.alignItems = Align.Center;
                line.Add(Icon(skill.Id.Value, 62));
                var description = LabelText(PresentationText.Name(skill.Id) + "\n" + PresentationText.SkillDescription(skill), 18, TextColor);
                description.style.flexGrow = 1;
                description.style.marginLeft = 10;
                line.Add(description);
                for (var slot = 0; slot < 2; slot++)
                {
                    var capturedSlot = slot;
                    var isCurrent = state.Player.EquippedActiveSkillIds.Count > slot &&
                                    state.Player.EquippedActiveSkillIds[slot].Equals(skill.Id);
                    var button = SmallButton(isCurrent ? (slot == 0 ? "ЛЕВО ✓" : "ПРАВО ✓") : (slot == 0 ? "ЛЕВО" : "ПРАВО"),
                        () => EquipSkill(skill.Id, capturedSlot));
                    button.SetEnabled(!isCurrent);
                    line.Add(button);
                }
                var info = SmallButton("?", () => ShowSkillDetails(skill));
                info.tooltip = "Полное описание навыка";
                line.Add(info);
                _safeArea.Add(line);
            }
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            _safeArea.Add(spacer);
            _safeArea.Add(ActionButton("СЛЕДУЮЩИЙ БОЙ", NextEncounter, true));
        }

        private void EquipSkill(ContentId skillId, int slot)
        {
            var result = _director.EquipSkill(skillId, slot);
            if (result.Accepted) PlayBatch(result.Events, BuildBetweenEncounters);
        }

        private void NextEncounter()
        {
            var result = _director.StartNextEncounter();
            if (result.Accepted) PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void BuildSummary(bool victory)
        {
            BeginScreen();
            var statistics = _director.Statistics ?? new RunStatistics();
            _safeArea.Add(Icon(victory ? "ui.victory" : "ui.defeat", 145));
            _safeArea.Add(Title(victory ? "ЗАБЕГ ЗАВЕРШЁН" : "ЗАБЕГ ОКОНЧЕН", 46, victory ? Gold : Danger));
            _safeArea.Add(LabelText(victory ? "Кристальный страж повержен." : "Кристаллы запомнят эту попытку.",
                22, TextColor, TextAnchor.MiddleCenter));

            var summary = Card();
            summary.Add(StatLine("Побеждено врагов", statistics.EncountersCleared.ToString()));
            summary.Add(StatLine("Самая длинная цепочка", statistics.BiggestCascade.ToString()));
            summary.Add(StatLine("Общий урон", statistics.TotalDamage.ToString()));
            summary.Add(StatLine("Завершено ходов", _director.State.ResolvedTurnCount.ToString()));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            summary.Add(StatLine("Отладочное зерно", _director.State.Seed.ToString()));
#endif
            _safeArea.Add(summary);

            _safeArea.Add(SectionHeading("УРОН ПО ИСТОЧНИКАМ"));
            foreach (var damage in statistics.DamageBySource)
                _safeArea.Add(StatLine(PresentationText.Name(damage.SourceId), damage.Amount.ToString()));
            _safeArea.Add(SectionHeading("ВЫБРАННЫЕ УЛУЧШЕНИЯ"));
            var upgrades = new List<string>();
            foreach (var id in _director.State.SelectedSkillIds)
            {
                if (id.Value == "skill.sunder" || id.Value == "skill.cleanse") continue;
                upgrades.Add(PresentationText.Name(id));
            }
            _safeArea.Add(Paragraph(upgrades.Count == 0 ? "Нет" : string.Join(" · ", upgrades.ToArray())));

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            _safeArea.Add(spacer);
            _safeArea.Add(ActionButton("НОВЫЙ ЗАБЕГ", StartRun, true));
            _safeArea.Add(ActionButton("В ГЛАВНОЕ МЕНЮ", () =>
            {
                _director.ReturnToTitle(true);
                BuildTitle();
            }, false));
        }

        private IEnumerator AnimateBatch(EventBatch events, Action finished)
        {
            _inputLocked = true;
            var played = new HashSet<string>(StringComparer.Ordinal);
            var paced = 0;
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
                        {
                            ShowEventCue(resolutionEvent);
                            PlayEventSound(resolutionEvent, played);
                            clearEvents.Add(resolutionEvent);
                        }
                        eventIndex++;
                    }
                    eventIndex--;

                    yield return AnimateClears(clearEvents);

                    // Combat consequences retain their deterministic order, but no longer hold
                    // gravity back with a separate pause after every cleared gem.
                    foreach (var resolutionEvent in resolutionEvents)
                    {
                        if (resolutionEvent.Type == SimulationEventType.GemCleared) continue;
                        if (resolutionEvent.Type == SimulationEventType.SpecialCreated)
                            ApplySpecialVisual(resolutionEvent);
                        ShowEventCue(resolutionEvent);
                        PlayEventSound(resolutionEvent, played);
                    }
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
                if (_reducedMotion || paced >= 24 || !IsPacedEvent(item.Type)) continue;
                paced++;
                yield return new WaitForSecondsRealtime(item.Type == SimulationEventType.DamageApplied ? 0.10f : 0.045f);
            }
            if (!_reducedMotion) yield return new WaitForSecondsRealtime(0.14f);
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
                    var eased = SmootherStep(progress);
                    var scale = Mathf.Lerp(1f, 0.12f, eased);
                    foreach (var visual in visuals)
                    {
                        visual.style.scale = new Scale(new Vector3(scale, scale, 1f));
                        visual.style.opacity = 1f - eased;
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
            while (elapsed < duration)
            {
                elapsed += AnimationDeltaTime();
                foreach (var motion in motions)
                {
                    var progress = Mathf.Clamp01(elapsed / motion.Duration);
                    var eased = curve == MotionCurve.Drop
                        ? EaseInOutCubic(progress)
                        : SmootherStep(progress);
                    SetTranslation(motion.Visual, Vector3.LerpUnclamped(motion.Start, motion.End, eased));
                    if (motion.FadeIn) motion.Visual.style.opacity = Mathf.Lerp(0.35f, 1f, SmootherStep(progress));
                }
                yield return null;
            }
            foreach (var motion in motions)
            {
                SetTranslation(motion.Visual, motion.End);
                if (motion.FadeIn) motion.Visual.style.opacity = 1f;
            }
        }

        private static float DropDuration(int travelRows)
        {
            // Square-root scaling keeps long falls readable without making large cascades drag.
            return Mathf.Clamp(
                0.12f + Mathf.Sqrt(Mathf.Max(1, travelRows)) * 0.075f,
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
                SetBorder(cell, item.Type == SimulationEventType.StatusAdded ? Danger : Cyan, 5);
                cell.schedule.Execute(() => SetBorder(cell, Hex("#41516B"), 2)).StartingIn(160);
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

            var image = Icon(key, size);
            image.name = "feedback-cue-" + key.Replace('.', '-') + "-" + item.Sequence + "-" + Time.frameCount;
            image.pickingMode = PickingMode.Ignore;
            image.style.position = Position.Absolute;
            image.style.left = center.x - _root.worldBound.xMin - size * 0.5f;
            image.style.top = center.y - _root.worldBound.yMin - size * 0.5f;
            image.style.opacity = 1f;
            image.style.scale = new Scale(new Vector3(0.55f, 0.55f, 1f));
            _root.Add(image);
            StartCoroutine(AnimateFeedbackSprite(image, _reducedMotion ? 0.08f : 0.23f));
        }

        private static IEnumerator AnimateFeedbackSprite(VisualElement image, float duration)
        {
            var elapsed = 0f;
            while (image != null && image.parent != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var scale = Mathf.Lerp(0.55f, 1.25f, EaseOutCubic(progress));
                image.style.scale = new Scale(new Vector3(scale, scale, 1f));
                image.style.opacity = 1f - progress;
                yield return null;
            }
            if (image != null && image.parent != null) image.RemoveFromHierarchy();
        }

        private void PlayEventSound(SimulationEvent item, HashSet<string> played)
        {
            var key = item.Type == SimulationEventType.SwapAccepted ? "feedback.swap"
                : item.Type == SimulationEventType.GemCleared ? "feedback.clear"
                : item.Type == SimulationEventType.SpecialCreated || item.Type == SimulationEventType.SpecialActivated
                    ? "feedback.special"
                : item.Type == SimulationEventType.DamageApplied ? "feedback.hit"
                : item.Type == SimulationEventType.StatusAdded ? "feedback.status_added"
                : item.Type == SimulationEventType.StatusRemoved ? "feedback.status_removed"
                : item.Type == SimulationEventType.EnemyDefeated ? "feedback.victory"
                : item.Type == SimulationEventType.RunEnded ? "feedback.defeat"
                : item.Type == SimulationEventType.SkillChosen ? "feedback.reward_confirmed"
                : item.Type == SimulationEventType.SkillUsed && item.SourceId.Value == "skill.sunder" ? "feedback.sunder"
                : string.Empty;
            if (string.IsNullOrEmpty(key) || !played.Add(key) || _catalog == null) return;
            var clip = _catalog.GetAudio(key);
            if (clip != null) _audioSource.PlayOneShot(clip);
        }

        private void RejectInput(string text)
        {
            SetMessage(text, Danger);
            if (_catalog != null)
            {
                var clip = _catalog.GetAudio("feedback.invalid_swap");
                if (clip != null) _audioSource.PlayOneShot(clip);
            }
            if (_reducedMotion || _board == null) return;
            SetTranslation(_board, new Vector3(-10, 0, 0));
            _board.schedule.Execute(() => SetTranslation(_board, Vector3.zero)).StartingIn(100);
        }

        private void RefreshCellSelections()
        {
            foreach (var entry in _boardCells)
            {
                var selected = (_selectedCell.HasValue && _selectedCell.Value.Equals(entry.Key)) ||
                               _skillTargets.Exists(value => value.Equals(entry.Key));
                SetBorder(entry.Value, selected ? Gold : Hex("#41516B"), selected ? 5 : 2);
            }
        }

        private void ShowModal(string heading, string body, Action<VisualElement> addContent = null)
        {
            var shade = new VisualElement();
            shade.name = "modal-overlay";
            shade.style.position = Position.Absolute;
            shade.style.left = 0;
            shade.style.right = 0;
            shade.style.top = 0;
            shade.style.bottom = 0;
            shade.style.backgroundColor = new Color(0, 0, 0, 0.78f);
            shade.style.justifyContent = Justify.Center;
            shade.style.alignItems = Align.Center;

            var modal = Card();
            modal.style.width = Length.Percent(88);
            modal.style.maxWidth = 720;
            modal.style.paddingLeft = 28;
            modal.style.paddingRight = 28;
            modal.style.paddingTop = 24;
            modal.style.paddingBottom = 24;
            modal.Add(Title(heading, 36, Gold));
            if (!string.IsNullOrEmpty(body)) modal.Add(Paragraph(body));
            addContent?.Invoke(modal);
            modal.Add(ActionButton("ЗАКРЫТЬ", () => shade.RemoveFromHierarchy(), false));
            shade.Add(modal);
            _root.Add(shade);
        }

        private VisualElement Card()
        {
            var element = new VisualElement();
            element.style.backgroundColor = Panel;
            element.style.paddingLeft = 14;
            element.style.paddingRight = 14;
            element.style.paddingTop = 10;
            element.style.paddingBottom = 10;
            element.style.marginTop = 6;
            element.style.marginBottom = 6;
            element.style.borderTopLeftRadius = 14;
            element.style.borderTopRightRadius = 14;
            element.style.borderBottomLeftRadius = 14;
            element.style.borderBottomRightRadius = 14;
            return element;
        }

        private VisualElement HelpRow(string iconKey, string heading, string body)
        {
            var card = Card();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            var icon = Icon(iconKey, 58);
            icon.style.marginRight = 12;
            card.Add(icon);
            var copy = new VisualElement();
            copy.style.flexGrow = 1;
            var title = LabelText(heading, 19, Gold);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.whiteSpace = WhiteSpace.Normal;
            copy.Add(title);
            var description = LabelText(body, 17, TextColor);
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginTop = 3;
            copy.Add(description);
            card.Add(copy);
            return card;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            return row;
        }

        private Button ActionButton(string text, Action action, bool primary)
        {
            var button = new Button(action) { text = text };
            button.style.height = 68;
            button.style.width = Length.Percent(100);
            button.style.maxWidth = 720;
            button.style.alignSelf = Align.Center;
            button.style.marginTop = 7;
            button.style.marginBottom = 7;
            button.style.fontSize = 23;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = TextColor;
            button.style.backgroundColor = primary ? Hex("#28738A") : PanelLight;
            var sprite = _catalog == null ? null : _catalog.GetSprite(primary ? "ui.button.primary" : "ui.button.secondary");
            if (sprite != null) button.style.backgroundImage = new StyleBackground(sprite);
            return button;
        }

        private Button SmallButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.minWidth = 72;
            button.style.height = 46;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.fontSize = 16;
            button.style.color = TextColor;
            button.style.backgroundColor = PanelLight;
            return button;
        }

        private VisualElement Icon(string key, float size)
        {
            var image = new Image();
            image.name = "icon-" + key;
            image.scaleMode = ScaleMode.ScaleToFit;
            image.sprite = _catalog == null ? null : _catalog.GetSprite(key);
            image.style.width = size;
            image.style.height = size;
            image.tooltip = PresentationText.Name(key);
            return image;
        }

        private VisualElement InlineIconLabel(string key, string text, string tooltip)
        {
            var row = Row();
            row.tooltip = tooltip;
            row.style.alignItems = Align.Center;
            row.Add(Icon(key, 30));
            var label = LabelText(text, 17, Success);
            label.style.marginLeft = 6;
            row.Add(label);
            return row;
        }

        private VisualElement ResourceChip(string iconKey, string label, int value, int maximum, Color color)
        {
            var chip = Row();
            chip.style.flexGrow = 1;
            chip.style.marginLeft = 2;
            chip.style.marginRight = 2;
            chip.style.paddingLeft = 5;
            chip.style.paddingRight = 5;
            chip.style.paddingTop = 5;
            chip.style.paddingBottom = 5;
            chip.style.backgroundColor = Panel;
            chip.style.alignItems = Align.Center;
            chip.tooltip = label + ": " + value + (maximum >= 0 ? " из " + maximum : string.Empty);
            chip.Add(Icon(iconKey, 30));
            var text = LabelText(label + "\n" + value + (maximum >= 0 ? "/" + maximum : string.Empty), 15, color, TextAnchor.MiddleCenter);
            text.style.flexGrow = 1;
            chip.Add(text);
            return chip;
        }

        private static VisualElement Bar(string label, float fraction, Color color)
        {
            var back = new VisualElement();
            back.style.height = 34;
            back.style.backgroundColor = Hex("#111827");
            back.style.marginTop = 5;
            var fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 0;
            fill.style.top = 0;
            fill.style.bottom = 0;
            fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
            fill.style.backgroundColor = color;
            back.Add(fill);
            var text = LabelText(label, 17, TextColor, TextAnchor.MiddleCenter);
            text.style.position = Position.Absolute;
            text.style.left = 0;
            text.style.right = 0;
            text.style.top = 0;
            text.style.bottom = 0;
            back.Add(text);
            return back;
        }

        private static Label Title(string text, int size, Color color)
        {
            var label = LabelText(text, size, color, TextAnchor.MiddleCenter);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label SectionHeading(string text)
        {
            var label = Title(text, 24, Gold);
            label.style.marginTop = 22;
            label.style.marginBottom = 7;
            return label;
        }

        private static Label Paragraph(string text)
        {
            var label = LabelText(text, 20, TextColor, TextAnchor.MiddleLeft);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 7;
            label.style.marginBottom = 7;
            return label;
        }

        private static Label LabelText(string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.unityTextAlign = alignment;
            return label;
        }

        private static VisualElement StatLine(string name, string value)
        {
            var row = Row();
            row.style.minHeight = 34;
            var label = LabelText(name, 19, Muted);
            label.style.flexGrow = 1;
            row.Add(label);
            row.Add(LabelText(value, 19, TextColor));
            return row;
        }

        private void SetMessage(string text, Color color)
        {
            if (_message == null) return;
            _message.text = text;
            _message.style.color = color;
        }

        private static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
        }

        private static BoardGemState FindGem(BoardState board, GridCell cell)
        {
            foreach (var gem in board.Gems)
                if (gem != null && gem.Cell.Equals(cell)) return gem;
            throw new InvalidOperationException("Board snapshot is missing cell " + cell + ".");
        }

        private static int FindDuration(BoardGemState gem)
        {
            var result = 0;
            if (gem.StatusDurations == null) return result;
            foreach (var duration in gem.StatusDurations)
                if (duration != null) result = Mathf.Max(result, duration.RemainingPlayerTurns);
            return result;
        }

        private static string StatusSuffix(BoardGemState gem)
        {
            if (gem.StatusIds == null || gem.StatusIds.Count == 0) return string.Empty;
            var names = new List<string>();
            foreach (var status in gem.StatusIds) names.Add(PresentationText.Name(status));
            return " Состояния: " + string.Join(", ", names.ToArray()) + ".";
        }

        private static string elementAccessibleName(BoardGemState gem)
        {
            return PresentationText.Name(gem.SpecialId.Value == "special.none" ? gem.GemId : gem.SpecialId);
        }

        private static int FindCooldown(PlayerState player, ContentId skillId)
        {
            if (player.SkillCooldowns == null) return 0;
            foreach (var cooldown in player.SkillCooldowns)
                if (cooldown != null && cooldown.SkillId.Equals(skillId)) return cooldown.RemainingTurns;
            return 0;
        }

        private static int CountStatusGems(BoardState board)
        {
            var count = 0;
            foreach (var gem in board.Gems)
                if (gem != null && gem.StatusIds != null && gem.StatusIds.Count > 0) count++;
            return count;
        }

        private static bool SkillHasEffect(RunState state, SkillDefinition skill)
        {
            if (skill.Id.Value == "skill.cleanse") return CountStatusGems(state.Board) > 0;
            if (skill.Id.Value == "skill.catalyze")
                return state.Player.Focus > 0 || (state.Player.Toxic >= 2 && state.Enemy.PoisonStacks < 3);
            return true;
        }

        private static bool Contains(IEnumerable<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return false;
            foreach (var id in ids) if (id.Equals(wanted)) return true;
            return false;
        }

        private static string RussianTurns(int value)
        {
            var lastTwo = value % 100;
            if (lastTwo >= 11 && lastTwo <= 14) return "ходов";
            var last = value % 10;
            if (last == 1) return "ход";
            return last >= 2 && last <= 4 ? "хода" : "ходов";
        }

        private static string RejectionText(string rejection)
        {
            if (rejection == "CellsAreNotAdjacent") return "Выберите два соседних кристалла.";
            if (rejection == "SwapCreatesNoMatch") return "Этот ход не создаёт совпадения.";
            if (rejection == "CellIsImmovable") return "Замороженные кристаллы и кристаллы с якорем нельзя менять местами.";
            if (rejection == "SkillOnCooldown") return "Активный навык ещё перезаряжается.";
            if (rejection == "NoEffectAvailable") return "Сейчас этот навык не даст эффекта.";
            if (rejection == "InvalidTargets") return "Выберите от одного до трёх разных кристаллов с состояниями.";
            if (rejection == "InputLocked") return "Дождитесь завершения хода.";
            return "Действие недоступно.";
        }

        private static IEnumerable<string> IntentAssetKeys(string telegraphKey)
        {
            yield return telegraphKey;
            if (telegraphKey == "intent.crush") yield return "status.cracked";
            if (telegraphKey == "intent.freeze_anchor") yield return "status.anchored";
        }

        private static bool IsPacedEvent(SimulationEventType type)
        {
            return type == SimulationEventType.SwapAccepted || type == SimulationEventType.GemCleared ||
                   type == SimulationEventType.SpecialActivated || type == SimulationEventType.DamageApplied ||
                   type == SimulationEventType.StatusAdded || type == SimulationEventType.StatusRemoved ||
                   type == SimulationEventType.EnemyIntentStarted || type == SimulationEventType.EnemyDefeated;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(value, out color) ? color : Color.white;
        }
    }
}
