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
    [DisallowMultipleComponent]
    public sealed partial class ThreeInARowApp : MonoBehaviour
    {
        private const string ReducedMotionKey = "three_in_a_row.reduced_motion";
        private const string SoundEnabledKey = "three_in_a_row.sound_enabled";
        private const string MusicEnabledKey = "three_in_a_row.music_enabled";
        private const int SfxVoiceCount = 8;
        private static readonly Color Background = Hex("#091419");
        private static readonly Color Panel = Hex("#14282D");
        private static readonly Color PanelLight = Hex("#234049");
        private static readonly Color Gold = Hex("#EBC782");
        private static readonly Color Cyan = Hex("#79E2D2");
        private static readonly Color TextColor = Hex("#F8FAFC");
        private static readonly Color Muted = Hex("#A1B9B9");
        private static readonly Color Danger = Hex("#F06C75");
        private static readonly Color Success = Hex("#6ED69B");

        private PresentationCatalog _catalog;
        private RunDirector _director;
        private UIDocument _document;
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private AudioSource _musicSource;
        private int _nextSfxSource;
        private VisualElement _root;
        private VisualElement _safeArea;
        private VisualElement _board;
        private VisualElement _gemMotionLayer;
        private VisualElement _enemyFeedbackAnchor;
        private Image _enemyPortrait;
        private Sprite _enemyPortraitIdleSprite;
        private Sprite _enemyPortraitAttackSprite;
        private Coroutine _enemyIdleRoutine;
        private Coroutine _enemyDamageRoutine;
        private EnemyMotionProfile _enemyMotionProfile;
        private VisualElement _playerFeedbackAnchor;
        private VisualElement _enemyHealthFill;
        private Label _enemyHealthLabel;
        private VisualElement _playerHealthChip;
        private Label _playerHealthLabel;
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
        private bool _soundEnabled;
        private bool _musicEnabled;
        private ContentId? _targetingSkill;
        private readonly List<GridCell> _skillTargets = new List<GridCell>();
        private ContentId _skillOption = "content.none";
        private int _presentedEnemyHealth;
        private int _presentedEnemyMaxHealth;
        private int _presentedPlayerHealth;
        private bool _hasPlayerAttackOrigin;
        private Vector2 _playerAttackOrigin;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            UnityEngine.Application.targetFrameRate = 60;
            _reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            _soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) != 0;
            _musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            _catalog = Resources.Load<PresentationCatalog>("E0PresentationCatalog");
            _director = new RunDirector(new JsonCheckpointStore(), new JsonProfileStore());
            if (FindAnyObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();
            for (var index = 0; index < SfxVoiceCount; index++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sfxSources.Add(source);
            }
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0.22f;

            _document = GetComponent<UIDocument>();
            if (_document == null) _document = gameObject.AddComponent<UIDocument>();
            var panelSettings = _document.panelSettings ?? Resources.Load<PanelSettings>("PortraitPanelSettings");
            if (panelSettings == null)
            {
                Debug.LogError("Не найден PortraitPanelSettings. Пересоберите ресурсы среды выполнения раздела E.");
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "Резервная портретная панель";
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(720, 1280);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
            }
            else
            {
                panelSettings = Instantiate(panelSettings);
            }
            panelSettings.match = Screen.width > Screen.height ? 1f : 0.5f;
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
            _safeArea.style.paddingLeft = 16 + safe.xMin * scaleX;
            _safeArea.style.paddingRight = 16 + (Screen.width - safe.xMax) * scaleX;
            _safeArea.style.paddingBottom = 12 + safe.yMin * scaleY;
            _safeArea.style.paddingTop = 12 + (Screen.height - safe.yMax) * scaleY;
        }

        private void BeginScreen()
        {
            StopAllCoroutines();
            ApplyMusicForCurrentScreen();
            ClearScreenPreservingFeedback();
            _boardCells.Clear();
            _gemVisuals.Clear();
            _visualGemStates.Clear();
            _board = null;
            _gemMotionLayer = null;
            _enemyFeedbackAnchor = null;
            _enemyPortrait = null;
            _enemyPortraitIdleSprite = null;
            _enemyPortraitAttackSprite = null;
            _enemyIdleRoutine = null;
            _enemyDamageRoutine = null;
            _playerFeedbackAnchor = null;
            _enemyHealthFill = null;
            _enemyHealthLabel = null;
            _playerHealthChip = null;
            _playerHealthLabel = null;
            _message = null;
            _inputLocked = false;
            _hasPlayerAttackOrigin = false;

            AddDungeonBackdrop();
            _safeArea = new VisualElement { name = "safe-area" };
            _safeArea.style.width = Length.Percent(100);
            _safeArea.style.maxWidth = 780;
            _safeArea.style.alignSelf = Align.Center;
            _safeArea.style.flexGrow = 1;
            _safeArea.style.paddingLeft = 24;
            _safeArea.style.paddingRight = 24;
            _safeArea.style.paddingTop = 18;
            _safeArea.style.paddingBottom = 18;
            _root.Add(_safeArea);
            _feedbackLayer?.BringToFront();
            ApplySafeArea();
        }

        private void BuildForCurrentScreen()
        {
            _targetingSkill = null;
            _skillTargets.Clear();
            _skillOption = "content.none";
            switch (_director.Screen)
            {
                case RunScreen.Title: BuildTitle(); break;
                case RunScreen.Encounter:
                case RunScreen.SkillWindow: BuildEncounter(); break;
                case RunScreen.Reward: BuildReward(); break;
                case RunScreen.BetweenEncounters: BuildBetweenEncounters(); break;
                case RunScreen.Map: BuildMap(); break;
                case RunScreen.Event: BuildEvent(false); break;
                case RunScreen.Rest: BuildEvent(true); break;
                case RunScreen.Sanctum: BuildSanctum(); break;
                case RunScreen.Victory: BuildSummary(true); break;
                case RunScreen.Defeat: BuildSummary(false); break;
            }
        }

        private void BuildTitle()
        {
            BeginScreen();
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "title-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.width = Length.Percent(100);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _safeArea.Add(scroll);

            var content = new VisualElement { name = "title-content" };
            content.style.width = Length.Percent(100);
            content.style.minHeight = Length.Percent(100);
            content.style.alignItems = Align.Center;
            content.style.justifyContent = Justify.Center;
            content.style.flexShrink = 0;
            scroll.Add(content);

            var crystal = Icon("gem.prism", 170);
            var crest = new DungeonOrnament(false);
            crest.style.height = 44;
            crest.style.width = 240;
            content.Add(crest);
            crystal.style.marginBottom = 22;
            content.Add(crystal);
            content.Add(Title("ТРИ В РЯД", 54, Gold));
            var subtitle = LabelText("КРИСТАЛЬНЫЙ РОГАЛИК", 24, Cyan, TextAnchor.MiddleCenter);
            subtitle.style.letterSpacing = 4;
            subtitle.style.marginBottom = 54;
            content.Add(subtitle);

            var menu = new VisualElement();
            menu.style.width = Length.Percent(100);
            menu.style.maxWidth = 720;
            menu.style.paddingLeft = 18;
            menu.style.paddingRight = 18;
            menu.style.paddingTop = 16;
            menu.style.paddingBottom = 16;
            var start = ActionButton("НАЧАТЬ ЗАБЕГ", StartRun, true);
            start.tooltip = "Начать новый забег через три региона по семь этапов.";
            menu.Add(start);
            if (_director.CanResume)
            {
                var resume = ActionButton("ПРОДОЛЖИТЬ", ResumeRun, false);
                resume.tooltip = "Продолжить с последней сохранённой контрольной точки.";
                menu.Add(resume);
            }
            menu.Add(DifficultyGoalCard(true));
            menu.Add(ActionButton("ЦЕЛИ, КОДЕКС И РЕКОРДЫ", BuildCodex, false));
            menu.Add(ActionButton("ЭКСПЕДИЦИИ И ИСПЫТАНИЯ", BuildExpeditionBoard, false));
            menu.Add(ActionButton("КАК ИГРАТЬ", () => BuildHelp(BuildTitle), false));
            menu.Add(ActionButton("НАСТРОЙКИ И АВТОРЫ", BuildSettings, false));
            content.Add(menu);

            var profile = _director.Profile;
            var progress = LabelText("Побед: " + profile.Aggregate.RunsWon + " · открыто целей: " +
                profile.CompletedChallengeIds.Count + " · сложность: " + profile.BestDifficultyUnlocked, 18, Muted, TextAnchor.MiddleCenter);
            progress.style.marginTop = 24;
            content.Add(progress);
            var version = LabelText("Контент R1–R8 · v" + RunState.CurrentContentVersion, 18, Muted, TextAnchor.MiddleCenter);
            version.style.marginTop = 36;
            content.Add(version);

            foreach (var child in content.Children()) child.style.flexShrink = 0;
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

            _safeArea.Add(ActionButton(_soundEnabled ? "ЗВУКОВЫЕ ЭФФЕКТЫ: ВКЛ." : "ЗВУКОВЫЕ ЭФФЕКТЫ: ВЫКЛ.", () =>
            {
                _soundEnabled = !_soundEnabled;
                PlayerPrefs.SetInt(SoundEnabledKey, _soundEnabled ? 1 : 0);
                PlayerPrefs.Save();
                BuildSettings();
            }, false));
            _safeArea.Add(ActionButton(_musicEnabled ? "МУЗЫКА: ВКЛ." : "МУЗЫКА: ВЫКЛ.", () =>
            {
                _musicEnabled = !_musicEnabled;
                PlayerPrefs.SetInt(MusicEnabledKey, _musicEnabled ? 1 : 0);
                PlayerPrefs.Save();
                BuildSettings();
            }, false));

            _safeArea.Add(ActionButton("КАК ИГРАТЬ", () => BuildHelp(BuildSettings), false));

            _safeArea.Add(SectionHeading("ОБЯЗАТЕЛЬНОЕ УКАЗАНИЕ АВТОРСТВА"));
            _safeArea.Add(Paragraph("Автор значков — Lorc. Опубликованы на game-icons.net по лицензии CC BY 3.0."));
            _safeArea.Add(ActionButton("ОТКРЫТЬ GAME-ICONS.NET", () => UnityEngine.Application.OpenURL("https://game-icons.net/"), false));
            _safeArea.Add(ActionButton("ОТКРЫТЬ CC BY 3.0", () => UnityEngine.Application.OpenURL("https://creativecommons.org/licenses/by/3.0/"), false));
            _safeArea.Add(SectionHeading("ДРУГИЕ АВТОРЫ"));
            _safeArea.Add(Paragraph("Кристаллы: Andrew Tidey · Интерфейс и часть звуков: Kenney · Звуки: rubberduck, Brian MacIntosh, IgnasD и JaggedStone · Музыка: The Cynic Project и Cleyton Kauffman · Портреты врагов: временные материалы проекта."));
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

            scroll.Add(SectionHeading("МАРШРУТ И СВЯТИЛИЩЕ"));
            scroll.Add(Paragraph("В начале региона можно выбрать один необязательный обет пути. За провал нет штрафа, а выполненный обет добавляет четвёртый вариант в следующем выборе эволюции. После первых двух боссов Святилище позволяет осмотреть сборку, сменить активные навыки, продолжить или сохраниться и выйти."));

            scroll.Add(SectionHeading("КРИСТАЛЛЫ И РЕСУРСЫ"));
            scroll.Add(HelpRow("gem.ember", "ПЛАМЯ", "Каждый убранный кристалл наносит 4 прямого урона. Особая Искра наносит 16 урона."));
            scroll.Add(HelpRow("gem.tide", "ПРИЛИВ И КОНЦЕНТРАЦИЯ", "Каждый кристалл даёт 1 ед. концентрации. Каждые 3 ед. автоматически превращаются в 6 урона. Особый Поток даёт 5 ед."));
            scroll.Add(HelpRow("gem.venom", "ЯД И ТОКСИН", "Каждый кристалл даёт 1 ед. токсина. Каждые 5 ед. наносят 12 урона и дают врагу заряд отравления. Особая Спора даёт 5 ед."));
            scroll.Add(HelpRow("gem.volt", "РАЗРЯД", "Каждый кристалл наносит 2 урона. Каждые 3 убранных Разряда сокращают перезарядку обоих активных навыков на 1. Особый Заряд наносит 8 урона и тоже ускоряет оба навыка."));
            scroll.Add(HelpRow("gem.prism", "ПРИЗМА", "Поменяйте её с обычным кристаллом, чтобы убрать с поля все кристаллы этого цвета и получить их обычные эффекты. Призмы — особые кристаллы: даже три Призмы в ряд не образуют совпадение."));
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
            scroll.Add(HelpRow("status.thorned", "ШИПЫ", PresentationText.StatusDescription("status.thorned")));

            scroll.Add(SectionHeading("СЛОЖНОСТЬ И ИСПЫТАНИЯ"));
            scroll.Add(Paragraph("Уровни складываются: 1 — прямой урон врагов +1; 2 — бой начинается с двух Трещин; 3 — лечение после победы снижено до 2; 4 — финальное намерение элиты получает дополнительный эффект; 5 — боссы переходят во вторую фазу при 50% здоровья."));
            scroll.Add(Paragraph("Ежедневные экспедиции и постоянные испытания — короткие забеги по одному региону с заданным начальным навыком и состояниями поля. Последние семь ежедневных маршрутов не исчезают сразу. Недельный маршрут проходит через все три региона. Серий входов и штрафов за пропуск нет."));

            _safeArea.Add(scroll);
            _safeArea.Add(ActionButton("НАЗАД", back, false));
        }

        private void StartRun()
        {
            ShowModal("ВЫБЕРИТЕ РЕЖИМ", "Каждый уровень сложности добавляет правило к предыдущим.", modal =>
            {
                for (var tier = 0; tier <= _director.Profile.BestDifficultyUnlocked; tier++)
                {
                    var captured = tier;
                    var definition = MasteryContentCatalog.Instance.Get(tier);
                    modal.Add(ActionButton("СЛОЖНОСТЬ " + tier + " · " + PresentationText.Name(definition.Id).ToUpperInvariant(),
                        () => StartStandardRun(captured), tier == _director.Profile.BestDifficultyUnlocked));
                }
                if (_director.Profile.BestDifficultyUnlocked < 5)
                    modal.Add(DifficultyGoalCard(true));
                var weekly = WeeklyChallenge.ForUtcDate(DateTime.UtcNow);
                modal.Add(ActionButton("НЕДЕЛЬНОЕ ИСПЫТАНИЕ · " + weekly.WeekStartUtc,
                    () => StartWeeklyRun(weekly), false));
                modal.Add(LabelText("Недельное испытание использует фиксированную сложность 3 и не открывает следующий уровень сложности.",
                    16, Muted, TextAnchor.MiddleCenter));
            });
        }

        private void StartStandardRun(int difficultyTier)
        {
            var ticks = DateTime.UtcNow.Ticks;
            var seed = unchecked((ulong)ticks ^ ((ulong)Environment.TickCount << 32));
            var result = _director.StartNewRun(seed == 0 ? 1UL : seed, difficultyTier);
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void StartWeeklyRun(WeeklyChallengeDefinition weekly)
        {
            var result = _director.StartWeeklyChallenge(weekly);
            if (!result.Accepted)
            {
                BuildTitle();
                ShowModal("ИСПЫТАНИЕ НЕДОСТУПНО", "Версия локального контента не совпадает с закреплённой версией испытания.");
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void BuildExpeditionBoard()
        {
            BeginScreen();
            _safeArea.Add(Title("ЭКСПЕДИЦИИ", 42, Gold));
            _safeArea.Add(Paragraph("Короткие забеги проходят в одном регионе. Пропущенные ежедневные маршруты ещё семь дней остаются доступными без серии входов и штрафов."));
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.Add(SectionHeading("ЕЖЕДНЕВНЫЕ МАРШРУТЫ"));
            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var expedition = DailyExpedition.ForUtcDate(DateTime.UtcNow.AddDays(-dayOffset));
                var captured = expedition;
                scroll.Add(ExpeditionCard(expedition, () => StartExpedition(captured)));
            }
            scroll.Add(SectionHeading("ПОСТОЯННЫЕ ИСПЫТАНИЯ"));
            foreach (var expedition in TrialExpeditions.All)
            {
                var captured = expedition;
                scroll.Add(ExpeditionCard(expedition, () => StartExpedition(captured)));
            }
            scroll.Add(SectionHeading("БОЛЬШОЙ НЕДЕЛЬНЫЙ МАРШРУТ"));
            var weekly = WeeklyChallenge.ForUtcDate(DateTime.UtcNow);
            scroll.Add(ActionButton("ТРИ РЕГИОНА · " + weekly.WeekStartUtc,
                () => StartWeeklyRun(weekly), true));
            _safeArea.Add(scroll);
            _safeArea.Add(ActionButton("НАЗАД", BuildTitle, false));
        }

        private VisualElement ExpeditionCard(ExpeditionDefinition expedition, Action start)
        {
            var card = Card();
            card.Add(Title(PresentationText.Name(expedition.Id), 23, Gold));
            card.Add(LabelText(PresentationText.RegionName(expedition.RegionIndex) + " · сложность " +
                expedition.DifficultyTier + " · стартовый навык: " + PresentationText.Name(expedition.StartingSkillId),
                17, TextColor));
            card.Add(LabelText("Начальное давление: " + PresentationText.Name(expedition.StartingStatusId) +
                " ×" + expedition.StartingStatusCount, 16, Muted));
            card.Add(ActionButton("НАЧАТЬ", start, true));
            return card;
        }

        private void StartExpedition(ExpeditionDefinition expedition)
        {
            var result = _director.StartExpedition(expedition);
            if (!result.Accepted)
            {
                BuildTitle();
                ShowModal("ЭКСПЕДИЦИЯ НЕДОСТУПНА", "Версия маршрута не совпадает с версией игры.");
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void BuildCodex()
        {
            BeginScreen();
            _safeArea.Add(Title("ЦЕЛИ, КОДЕКС И РЕКОРДЫ", 38, Gold));
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.Add(SectionHeading("СЛЕДУЮЩАЯ СЛОЖНОСТЬ"));
            scroll.Add(DifficultyGoalCard(false));
            scroll.Add(SectionHeading("ДОСТУПНЫЕ ЦЕЛИ"));
            var availableGoals = 0;
            foreach (var goal in ProfileContentCatalog.Instance.Challenges)
            {
                if (goal.Id.Equals(ProfileContentIds.ChallengeDifficultyOne) || IsGoalComplete(goal)) continue;
                scroll.Add(GoalCard(goal, false, false));
                availableGoals++;
            }
            if (availableGoals == 0) scroll.Add(Paragraph("Все цели выполнены."));
            scroll.Add(SectionHeading("ВЫПОЛНЕНО"));
            var completedGoals = 0;
            foreach (var goal in ProfileContentCatalog.Instance.Challenges)
            {
                if (!IsGoalComplete(goal)) continue;
                scroll.Add(GoalCard(goal, true, false));
                completedGoals++;
            }
            if (completedGoals == 0) scroll.Add(Paragraph("Пока выполненных целей нет."));
            scroll.Add(SectionHeading("ОТКРЫТЫЕ ЗАПИСИ"));
            if (_director.Profile.CodexEntries.Count == 0) scroll.Add(Paragraph("Пока записей нет."));
            foreach (var entry in _director.Profile.CodexEntries)
            {
                var line = Card("ui.panel.inset");
                line.Add(StatLine(entry.Category + " · " +
                    (entry.Category == CodexCategory.Intent ? PresentationText.Name(entry.ParentContentId) + " / " : string.Empty) +
                    PresentationText.Name(entry.ContentId), entry.SeenCount.ToString()));
                var lore = PresentationText.CodexLore(entry.ContentId);
                if (!string.IsNullOrEmpty(lore)) line.Add(LabelText(lore, 15, Muted));
                scroll.Add(line);
            }
            scroll.Add(SectionHeading("РЕКОРДЫ"));
            if (_director.Profile.Records.Count == 0) scroll.Add(Paragraph("Первая победа создаст запись."));
            foreach (var record in _director.Profile.Records)
                scroll.Add(Paragraph(PresentationText.Name(record.BossId) + " · сложность " + record.DifficultyTier +
                    " · побед " + record.Wins + " · ходов " + record.FastestValidTurnCount +
                    " · здоровье " + record.BestRemainingHealth + " · каскад " + record.LargestCascade));
            scroll.Add(SectionHeading("АРХИВ ЗАБЕГОВ"));
            if (_director.Profile.RunHistory.Count == 0) scroll.Add(Paragraph("Завершённые забеги появятся здесь."));
            foreach (var history in _director.Profile.RunHistory)
            {
                var captured = history;
                var card = Card("ui.panel.inset");
                card.Add(LabelText((history.Victory ? "ПОБЕДА" : "ПОРАЖЕНИЕ") + " · сложность " +
                    history.DifficultyTier + " · ходов " + history.ValidTurnCount + " · зерно " + history.Seed,
                    16, history.Victory ? Success : Muted));
                card.Add(LabelText("Финал: " + PresentationText.Name(history.BossId) + " · каскад " +
                    history.LargestCascade + " · путь " + (history.RouteNodeIds == null ? 0 : history.RouteNodeIds.Count) +
                    " узлов · навыков " + (history.SkillIds == null ? 0 : history.SkillIds.Count), 15, TextColor));
                if (history.RouteVowIds != null && history.RouteVowIds.Count > 0)
                {
                    var vows = new List<string>();
                    foreach (var vowId in history.RouteVowIds) vows.Add(PresentationText.Name(vowId));
                    card.Add(LabelText("Обеты: " + string.Join(" · ", vows.ToArray()), 15, Gold));
                }
                var actions = Row();
                actions.Add(SmallButton("КОПИРОВАТЬ", () => GUIUtility.systemCopyBuffer = captured.Seed));
                actions.Add(SmallButton("ПОВТОРИТЬ", () => ReplayArchivedRun(captured)));
                card.Add(actions);
                scroll.Add(card);
            }
            _safeArea.Add(scroll);
            _safeArea.Add(ActionButton("НАЗАД", BuildTitle, false));
        }

        private void ReplayArchivedRun(RunHistoryEntryState history)
        {
            ulong seed;
            if (!ulong.TryParse(history.Seed, out seed)) return;
            var difficulty = Math.Min(history.DifficultyTier, _director.Profile.BestDifficultyUnlocked);
            var result = _director.StartNewRun(seed, difficulty);
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private VisualElement DifficultyGoalCard(bool compact)
        {
            var card = Card();
            card.name = "next-difficulty-goal";
            var best = _director.Profile.BestDifficultyUnlocked;
            if (best >= 5)
            {
                card.Add(LabelText("ВСЕ СЛОЖНОСТИ ОТКРЫТЫ", compact ? 17 : 21, Success,
                    TextAnchor.MiddleCenter));
                if (!compact)
                    card.Add(LabelText("Максимум: сложность 5 · " + PresentationText.Name(MasteryContentIds.Difficulty5) + ".",
                        18, TextColor, TextAnchor.MiddleCenter));
                return card;
            }

            var next = best + 1;
            var definition = MasteryContentCatalog.Instance.Get(next);
            var heading = LabelText("СЛЕДУЮЩАЯ: СЛОЖНОСТЬ " + next + " · " +
                PresentationText.Name(definition.Id).ToUpperInvariant(), compact ? 16 : 21, Gold,
                TextAnchor.MiddleCenter);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.whiteSpace = WhiteSpace.Normal;
            card.Add(heading);

            var requirement = best == 0
                ? "Как открыть: побеждайте, нанося больше всего урона каждым из четырёх источников. При равенстве засчитываются все лидирующие источники."
                : "Как открыть: победите в обычном забеге на сложности " + best +
                  ". Недельное испытание не засчитывается.";
            var body = LabelText(requirement, compact ? 15 : 18, TextColor, TextAnchor.MiddleCenter);
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.marginTop = 5;
            card.Add(body);

            if (best == 0)
            {
                var progress = LabelText(DominantBranchProgressText(), compact ? 15 : 18, Cyan,
                    TextAnchor.MiddleCenter);
                progress.style.whiteSpace = WhiteSpace.Normal;
                progress.style.marginTop = 5;
                card.Add(progress);
            }
            else
            {
                var progress = LabelText("Прогресс: 0/1", compact ? 15 : 18, Cyan, TextAnchor.MiddleCenter);
                progress.style.marginTop = 5;
                card.Add(progress);
            }

            if (!compact)
            {
                var reward = LabelText("Откроется правило: " + DifficultyRuleText(next), 17, Muted,
                    TextAnchor.MiddleCenter);
                reward.style.whiteSpace = WhiteSpace.Normal;
                reward.style.marginTop = 5;
                card.Add(reward);
            }
            return card;
        }

        private string DominantBranchProgressText()
        {
            var parts = new List<string>();
            foreach (var branch in ProfileProgression.DamageBranches)
                parts.Add(PresentationText.Name(branch) + " " +
                    (Contains(_director.Profile.DominantBranchWins, branch) ? "✓" : "—"));
            return string.Join(" · ", parts.ToArray()) + "\nПрогресс: " +
                ProfileProgression.DominantBranchWinCount(_director.Profile) + "/4";
        }

        private static string DifficultyRuleText(int tier)
        {
            if (tier == 1) return "враги наносят +1 прямого урона";
            if (tier == 2) return "каждый бой начинается с двух Трещин";
            if (tier == 3) return "лечение после победы снижено до 2";
            if (tier == 4) return "финальное намерение элиты накладывает Шипы";
            if (tier == 5) return "боссы переходят во вторую фазу при 50% здоровья";
            return "без дополнительных правил";
        }

        private bool IsGoalComplete(UnlockChallengeDefinition goal)
        {
            return goal != null && Contains(_director.Profile.CompletedChallengeIds, goal.Id);
        }

        private static UnlockChallengeDefinition FindGoal(ContentId id)
        {
            foreach (var goal in ProfileContentCatalog.Instance.Challenges)
                if (goal.Id.Equals(id)) return goal;
            return null;
        }

        private VisualElement GoalCard(UnlockChallengeDefinition goal, bool completed, bool useCurrentRun)
        {
            var card = Card();
            var heading = LabelText(goal.CategoryText.ToUpperInvariant(), 18, completed ? Success : Gold);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(heading);
            card.Add(Paragraph(goal.GoalText));
            var progress = LabelText(completed ? "Выполнено" : GoalProgressText(goal.Condition, useCurrentRun),
                17, completed ? Success : Cyan);
            progress.style.whiteSpace = WhiteSpace.Normal;
            card.Add(progress);
            var scope = LabelText(GoalScopeText(goal.Condition), 16, Muted);
            scope.style.marginTop = 4;
            card.Add(scope);
            var reward = LabelText("Награда: " + PresentationText.Name(goal.UnlockContentId), 17, TextColor);
            reward.style.marginTop = 4;
            reward.style.whiteSpace = WhiteSpace.Normal;
            card.Add(reward);
            return card;
        }

        private string GoalProgressText(UnlockConditionType condition, bool useCurrentRun)
        {
            var statistics = useCurrentRun ? _director.Statistics : null;
            if (condition == UnlockConditionType.DefeatCrystalWarden) return "Прогресс: 0/1 · требуется победа";
            if (condition == UnlockConditionType.WinWithThreeEmberSkills)
                return "Текущий забег: " + CurrentBranchSkillCount("ember", useCurrentRun) + "/3 · требуется победа";
            if (condition == UnlockConditionType.PoisonTwoStacksInResponse)
                return "Текущий забег: " + Math.Min(2, statistics == null ? 0 : statistics.MaxPoisonStacksInResponse) + "/2";
            if (condition == UnlockConditionType.CleanseThreeStatusKinds)
                return "Текущий забег: " + Math.Min(3, statistics == null ? 0 : statistics.MaxCleanseStatusKinds) + "/3";
            if (condition == UnlockConditionType.EliteWithoutHealthDamage)
                return "Текущий забег: " + Math.Min(1, statistics == null ? 0 : statistics.FlawlessEliteVictories) + "/1";
            if (condition == UnlockConditionType.ActivateThreeSpecials)
                return "Текущий забег: " + Math.Min(3, statistics == null ? 0 : statistics.SpecialActivations) + "/3";
            if (condition == UnlockConditionType.ActivateThreeSparks)
                return "Текущий забег: " + Math.Min(3, statistics == null ? 0 : statistics.SparkActivations) + "/3";
            if (condition == UnlockConditionType.ConvertFocusFourTimes)
                return "Текущий забег: " + Math.Min(4, statistics == null ? 0 : statistics.FocusConversions) + "/4";
            if (condition == UnlockConditionType.FocusAndPoisonSameRun)
                return "Концентрация " + (statistics != null && statistics.FocusConversions > 0 ? "✓" : "—") +
                    " · яд " + (statistics != null && statistics.PoisonApplications > 0 ? "✓" : "—");
            if (condition == UnlockConditionType.WinWithEveryDominantBranch)
                return DominantBranchProgressText();
            if (condition == UnlockConditionType.DefeatTwentyFiveEnemies)
                return "Прогресс: " + Math.Min(25, _director.Profile.Aggregate.EnemiesDefeated) + "/25";
            if (condition == UnlockConditionType.DefeatFiveElites)
                return "Прогресс: " + Math.Min(5, _director.Profile.Aggregate.ElitesDefeated) + "/5";
            if (condition == UnlockConditionType.DefeatFiveBosses)
                return "Прогресс: " + Math.Min(5, _director.Profile.Aggregate.BossesDefeated) + "/5";
            if (condition == UnlockConditionType.WinThreeRuns)
                return "Прогресс: " + Math.Min(3, _director.Profile.Aggregate.RunsWon) + "/3";
            if (condition == UnlockConditionType.WinTenRuns)
                return "Прогресс: " + Math.Min(10, _director.Profile.Aggregate.RunsWon) + "/10";
            if (condition == UnlockConditionType.ReachCascadeFive)
                return "Лучший каскад: " + Math.Min(5, BestRecordedCascade(statistics)) + "/5";
            if (condition == UnlockConditionType.ReachCascadeEight)
                return "Лучший каскад: " + Math.Min(8, BestRecordedCascade(statistics)) + "/8";
            if (condition == UnlockConditionType.ActivateFiftySpecials)
                return "Прогресс: " + Math.Min(50, _director.Profile.Aggregate.SpecialActivations) + "/50";
            if (condition == UnlockConditionType.ChooseTwentyEvents)
                return "Прогресс: " + Math.Min(20, _director.Profile.Aggregate.EventChoices) + "/20";
            if (condition == UnlockConditionType.CompleteThreeRouteVows)
                return "Прогресс: " + Math.Min(3, _director.Profile.Aggregate.RouteVowsCompleted) + "/3";
            if (condition == UnlockConditionType.WinDifficultyThree)
                return "Прогресс: 0/1 · победите на сложности 3+";
            if (condition == UnlockConditionType.CompleteExpedition)
                return "Прогресс: " + Math.Min(1, _director.Profile.Aggregate.ExpeditionsCompleted) + "/1";
            return "Прогресс: 0/1";
        }

        private int BestRecordedCascade(RunStatistics current)
        {
            var best = current == null ? 0 : current.BiggestCascade;
            foreach (var record in _director.Profile.Records)
                best = Math.Max(best, record.LargestCascade);
            return best;
        }

        private int CurrentBranchSkillCount(string branch, bool useCurrentRun)
        {
            if (!useCurrentRun || _director.State == null) return 0;
            var count = 0;
            foreach (var skillId in _director.State.SelectedSkillIds)
            {
                try
                {
                    if (string.Equals(MvpProgressionContentCatalog.Instance.GetSkill(skillId).BranchTag,
                        branch, StringComparison.Ordinal)) count++;
                }
                catch (KeyNotFoundException) { }
            }
            return count;
        }

        private static string GoalScopeText(UnlockConditionType condition)
        {
            if (condition == UnlockConditionType.WinWithEveryDominantBranch ||
                condition == UnlockConditionType.DefeatTwentyFiveEnemies ||
                condition == UnlockConditionType.DefeatFiveElites ||
                condition == UnlockConditionType.DefeatFiveBosses ||
                condition == UnlockConditionType.WinThreeRuns ||
                condition == UnlockConditionType.WinTenRuns ||
                condition == UnlockConditionType.ActivateFiftySpecials ||
                condition == UnlockConditionType.ChooseTwentyEvents ||
                condition == UnlockConditionType.CompleteThreeRouteVows)
                return "Учитывается за несколько забегов.";
            return "Нужно выполнить в одном забеге.";
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

        private void BuildMap()
        {
            BeginScreen();
            var state = _director.State;
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "map-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.width = Length.Percent(100);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _safeArea.Add(scroll);

            var content = new VisualElement { name = "map-content" };
            content.style.width = Length.Percent(100);
            content.style.minHeight = Length.Percent(100);
            content.style.justifyContent = Justify.Center;
            content.style.flexShrink = 0;
            scroll.Add(content);

            var top = Row();
            var heading = Title("КАРТА РЕГИОНА", 38, Gold);
            heading.style.flexGrow = 1;
            top.Add(heading);
            top.Add(SmallButton("?", () => BuildHelp(BuildForCurrentScreen)));
            content.Add(top);
            var mapHint = LabelText(PresentationText.RegionName(state.RegionIndex) + " · " + RegionProgressText(state) + "\n" +
                "Цель: " + PresentationText.Name(state.Map.BossEnemyId) +
                " · сложность " + state.DifficultyTier +
                RunModeSuffix(state) +
                " · выберите доступный путь", 20, Muted, TextAnchor.MiddleCenter);
            mapHint.style.whiteSpace = WhiteSpace.Normal;
            mapHint.style.marginBottom = 6;
            content.Add(mapHint);
            if (state.Map.FurthestVisitedRow < 0 && state.RouteVow != null)
            {
                var vowCard = Card("ui.panel.inset");
                if (state.RouteVow.PinnedId.Value == "vow.none")
                {
                    vowCard.Add(LabelText("ОБЕТ ПУТИ · НЕОБЯЗАТЕЛЬНО", 17, Gold));
                    vowCard.Add(LabelText(state.RegionIndex < state.FinalRegionIndex
                        ? "Исполненный обет добавит четвёртый вариант эволюции после босса."
                        : "Исполненный обет будет отмечен в профиле и созвездиях мастерства.", 15, Muted));
                    foreach (var vowId in state.RouteVow.OfferedIds)
                    {
                        var capturedVow = vowId;
                        var vowButton = ActionButton(PresentationText.Name(vowId).ToUpperInvariant() + "\n" +
                            PresentationText.RouteVowDescription(vowId), () => PinRouteVow(capturedVow), false);
                        vowButton.style.height = StyleKeyword.Auto;
                        vowButton.style.minHeight = 82;
                        vowButton.style.fontSize = 18;
                        vowButton.style.whiteSpace = WhiteSpace.Normal;
                        vowCard.Add(vowButton);
                    }
                }
                else
                {
                    vowCard.Add(LabelText(PresentationText.Name(state.RouteVow.PinnedId).ToUpperInvariant(), 17, Gold));
                    vowCard.Add(LabelText(PresentationText.RouteVowDescription(state.RouteVow.PinnedId), 15, TextColor));
                }
                content.Add(vowCard);
            }

            var mapPanel = new VisualElement();
            mapPanel.style.flexShrink = 0;
            mapPanel.style.justifyContent = Justify.Center;
            mapPanel.style.paddingLeft = 4;
            mapPanel.style.paddingRight = 4;
            for (var rowIndex = 6; rowIndex >= 0; rowIndex--)
            {
                var nodes = MapNodesInRow(state.Map, rowIndex);
                var row = Row();
                row.style.justifyContent = Justify.Center;
                row.style.minHeight = 78;
                foreach (var node in nodes)
                {
                    var captured = node;
                    var reachable = MapSimulation.IsReachable(state.Map, node);
                    var button = new Button(() => SelectMapNode(captured.Id));
                    button.style.minHeight = 76;
                    button.style.flexGrow = 1;
                    button.style.flexBasis = 0;
                    button.style.maxWidth = nodes.Count == 1 ? 430 : 320;
                    button.style.marginLeft = 4;
                    button.style.marginRight = 4;
                    button.style.paddingLeft = 10;
                    button.style.paddingRight = 10;
                    button.style.paddingTop = 8;
                    button.style.paddingBottom = 8;
                    button.style.flexDirection = FlexDirection.Row;
                    button.style.alignItems = Align.Center;
                    SkinButton(button,
                        node.Completed ? "ui.button.secondary" : reachable ? "ui.button.primary" : "ui.button.disabled",
                        node.Completed ? "ui.button.secondary.pressed" : reachable ? "ui.button.primary.pressed" : "ui.button.disabled.pressed");

                    var iconStack = new VisualElement();
                    iconStack.style.width = 52;
                    iconStack.style.height = 52;
                    iconStack.style.marginRight = 8;
                    var nodeIcon = Icon(MapNodeIconKey(node), 48);
                    nodeIcon.style.position = Position.Absolute;
                    nodeIcon.style.left = 2;
                    nodeIcon.style.top = 2;
                    iconStack.Add(nodeIcon);
                    var stateIcon = Icon(node.Completed ? "ui.state.completed" : reachable ? "ui.state.available" : "ui.state.locked", 20);
                    stateIcon.style.position = Position.Absolute;
                    stateIcon.style.right = 0;
                    stateIcon.style.bottom = 0;
                    iconStack.Add(stateIcon);
                    button.Add(iconStack);

                    var copy = new VisualElement();
                    copy.style.flexGrow = 1;
                    var stage = LabelText("ЭТАП " + (node.Row + 1) + " · " + PresentationText.NodeTypeName(node.Type).ToUpperInvariant(),
                        15, node.Completed ? Gold : reachable ? Cyan : Muted);
                    stage.style.unityFontStyleAndWeight = FontStyle.Bold;
                    stage.style.whiteSpace = WhiteSpace.Normal;
                    copy.Add(stage);
                    var detail = LabelText(MapNodeDetail(node), nodes.Count >= 3 ? 14 : 17, TextColor);
                    detail.style.whiteSpace = WhiteSpace.Normal;
                    detail.style.marginTop = 2;
                    copy.Add(detail);
                    button.Add(copy);

                    button.SetEnabled(reachable);
                    button.tooltip = MapNodeTooltip(node);
                    row.Add(button);
                }
                mapPanel.Add(row);
                if (rowIndex > 0)
                {
                    var lowerNodes = MapNodesInRow(state.Map, rowIndex - 1);
                    var connections = new MapConnectionBand(nodes, lowerNodes);
                    connections.tooltip = "Линии показывают доступные переходы между этапами.";
                    mapPanel.Add(connections);
                }
            }
            content.Add(mapPanel);
            var runStatus = Card("ui.panel.inset");
            runStatus.style.flexDirection = FlexDirection.Row;
            runStatus.style.justifyContent = Justify.SpaceAround;
            runStatus.style.alignItems = Align.Center;
            runStatus.style.marginTop = 5;
            runStatus.style.marginBottom = 3;
            runStatus.Add(MapStat("ui.player_health", state.Player.Health + "/" + PlayerState.MaxHealth));
            runStatus.Add(MapStat("ui.shield", state.Player.Shield.ToString()));
            runStatus.Add(MapStat("ui.experience", "УР. " + state.Level));
            content.Add(runStatus);
            content.Add(ActionButton("НАСТРОИТЬ АКТИВНЫЕ НАВЫКИ", BuildLoadoutModal, false));

            foreach (var child in content.Children()) child.style.flexShrink = 0;
        }

        private static List<MapNodeState> MapNodesInRow(MapState map, int rowIndex)
        {
            var result = new List<MapNodeState>();
            if (map == null || map.Nodes == null) return result;
            foreach (var node in map.Nodes)
                if (node != null && node.Row == rowIndex) result.Add(node);
            result.Sort((left, right) => left.Column.CompareTo(right.Column));
            return result;
        }

        private static string RegionProgressText(RunState state)
        {
            return IsExpedition(state)
                ? "ЭКСПЕДИЦИЯ · ОДИН РЕГИОН"
                : "ОБЛАСТЬ " + (state.RegionIndex + 1) + " / " + (state.FinalRegionIndex + 1);
        }

        private static string RunModeSuffix(RunState state)
        {
            if (IsExpedition(state)) return " · короткая экспедиция";
            return state.IsChallengeRun ? " · недельное испытание" : string.Empty;
        }

        private static bool IsExpedition(RunState state)
        {
            return state != null && state.ChallengeId.Value != null &&
                   state.ChallengeId.Value.StartsWith("expedition.", StringComparison.Ordinal);
        }

        private static string MapNodeIconKey(MapNodeState node)
        {
            if (node.Type == MapNodeType.NormalCombat || node.Type == MapNodeType.EliteCombat || node.Type == MapNodeType.Boss)
            {
                var encounter = MvpCombatContentCatalog.Instance.GetEncounter(node.ContentId);
                return encounter.Enemy.Id.Value;
            }
            return node.Type == MapNodeType.Rest ? "ui.player_health" : "gem.prism";
        }

        private static string MapNodeDetail(MapNodeState node)
        {
            if (node.Type == MapNodeType.NormalCombat || node.Type == MapNodeType.EliteCombat || node.Type == MapNodeType.Boss)
            {
                var encounter = MvpCombatContentCatalog.Instance.GetEncounter(node.ContentId);
                return PresentationText.Name(encounter.Enemy.Id) + "\n" + PresentationText.Name(node.PressureId);
            }
            return PresentationText.Name(node.ContentId);
        }

        private VisualElement MapStat(string iconKey, string value)
        {
            var stat = Row();
            stat.style.alignItems = Align.Center;
            stat.Add(Icon(iconKey, 28));
            var label = LabelText(value, 18, TextColor);
            label.style.marginLeft = 5;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            stat.Add(label);
            return stat;
        }

        private string MapNodeTooltip(MapNodeState node)
        {
            if (node.Type == MapNodeType.NormalCombat || node.Type == MapNodeType.EliteCombat || node.Type == MapNodeType.Boss)
                return "Семейство врага и главное давление показаны заранее: " + PresentationText.Name(node.PressureId) + ".";
            return node.Type == MapNodeType.Rest ? "Безопасное восстановление." : PresentationText.EventDescription(node.ContentId);
        }

        private void SelectMapNode(ContentId nodeId)
        {
            if (_inputLocked) return;
            _inputLocked = true;
            var result = _director.SelectMapNode(nodeId);
            if (!result.Accepted)
            {
                _inputLocked = false;
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void PinRouteVow(ContentId vowId)
        {
            var result = _director.PinRouteVow(vowId);
            if (result.Accepted) PlayBatch(result.Events, BuildMap);
        }

        private void BuildEvent(bool rest)
        {
            BeginScreen();
            var pending = _director.State.PendingEvent;
            _safeArea.style.justifyContent = Justify.Center;
            _safeArea.Add(Title(rest ? "ПРИВАЛ" : PresentationText.Name(pending.EventId).ToUpperInvariant(), 42, Gold));
            _safeArea.Add(Paragraph(PresentationText.EventDescription(pending.EventId)));
            foreach (var choiceId in pending.ChoiceIds)
            {
                var captured = choiceId;
                var button = ActionButton(PresentationText.ChoiceDescription(choiceId), () => SelectEventChoice(captured), true);
                button.style.height = StyleKeyword.Auto;
                button.style.minHeight = 82;
                button.style.whiteSpace = WhiteSpace.Normal;
                button.style.paddingLeft = 18;
                button.style.paddingRight = 18;
                _safeArea.Add(button);
            }
        }

        private void SelectEventChoice(ContentId choiceId)
        {
            if (_inputLocked) return;
            _inputLocked = true;
            var result = _director.SelectEventChoice(choiceId);
            if (!result.Accepted)
            {
                _inputLocked = false;
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
        }

        private void BuildLoadoutModal()
        {
            ShowModal("АКТИВНЫЕ НАВЫКИ", "Изученные навыки сохраняют перезарядку при смене ячейки.", modal =>
            {
                var state = _director.State;
                foreach (var skill in MvpProgressionContentCatalog.Instance.Skills)
                {
                    if (skill.SlotType != SkillSlotType.Active || !Contains(state.SelectedSkillIds, skill.Id)) continue;
                    var line = Row();
                    line.style.alignItems = Align.Center;
                    var name = LabelText(PresentationText.Name(skill.Id), 18, TextColor);
                    name.style.flexGrow = 1;
                    line.Add(name);
                    for (var slot = 0; slot < 2; slot++)
                    {
                        var capturedSlot = slot;
                        var isCurrent = state.Player.EquippedActiveSkillIds.Count > slot &&
                                        state.Player.EquippedActiveSkillIds[slot].Equals(skill.Id);
                        var button = SmallButton(isCurrent ? (slot == 0 ? "Л ✓" : "П ✓") : (slot == 0 ? "Л" : "П"),
                            () =>
                            {
                                var result = _director.EquipSkill(skill.Id, capturedSlot);
                                if (result.Accepted) BuildForCurrentScreen();
                            });
                        button.SetEnabled(!isCurrent);
                        line.Add(button);
                    }
                    modal.Add(line);
                }
            });
        }

        private void BuildEncounter()
        {
            BeginScreen();
            var state = _director.State;
            var encounter = MvpCombatContentCatalog.Instance.GetEncounter(state.CurrentEncounterId);
            var enemy = encounter.Enemy;

            var top = Row();
            top.style.alignItems = Align.Center;
            var currentNode = MapSimulation.GetCurrentNode(state);
            var encounterLabel = LabelText(RegionProgressText(state) + " · ЭТАП " +
                (currentNode == null ? 1 : currentNode.Row + 1) + " / 7", 19, Muted);
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

            var enemyPanel = new VisualElement { name = "enemy-stage" };
            enemyPanel.style.flexDirection = FlexDirection.Row;
            enemyPanel.style.alignItems = Align.Center;
            enemyPanel.style.paddingTop = 4;
            enemyPanel.style.paddingBottom = 4;
            var portraitAnchor = new VisualElement { name = "enemy-portrait-anchor" };
            portraitAnchor.style.width = 112;
            portraitAnchor.style.height = 112;
            portraitAnchor.style.flexShrink = 0;
            portraitAnchor.style.marginRight = 18;
            portraitAnchor.tooltip = PresentationText.Name(enemy.Id);
            _enemyPortraitIdleSprite = _catalog == null ? null : _catalog.GetSprite(enemy.Id.Value);
            _enemyPortraitAttackSprite = _catalog == null ? null : _catalog.GetSprite(enemy.Id.Value + ".attack");
            if (_enemyPortraitAttackSprite == null) _enemyPortraitAttackSprite = _enemyPortraitIdleSprite;
            _enemyPortrait = new Image
            {
                name = "enemy-portrait-" + enemy.Id.Value,
                scaleMode = ScaleMode.ScaleToFit,
                sprite = _enemyPortraitIdleSprite
            };
            _enemyPortrait.style.width = 112;
            _enemyPortrait.style.height = 112;
            portraitAnchor.Add(_enemyPortrait);
            enemyPanel.Add(portraitAnchor);
            _enemyFeedbackAnchor = portraitAnchor;
            _enemyMotionProfile = EnemyMotionProfileFor(enemy.Id);
            var enemyInfo = new VisualElement();
            enemyInfo.style.flexGrow = 1;
            enemyInfo.style.minWidth = 0;
            var enemyName = Title(PresentationText.Name(enemy.Id), 29, TextColor);
            enemyName.style.unityTextAlign = TextAnchor.MiddleLeft;
            enemyInfo.Add(enemyName);
            var enemyHealthBar = Bar("ЗДОРОВЬЕ " + state.Enemy.Health + " / " + enemy.MaxHealth,
                enemy.MaxHealth <= 0 ? 0 : (float)state.Enemy.Health / enemy.MaxHealth, Danger);
            _enemyHealthFill = enemyHealthBar.Q<VisualElement>("bar-fill");
            _enemyHealthLabel = enemyHealthBar.Q<Label>("bar-label");
            _presentedEnemyHealth = state.Enemy.Health;
            _presentedEnemyMaxHealth = enemy.MaxHealth;
            enemyInfo.Add(enemyHealthBar);
            if (state.Enemy.Barrier > 0)
                enemyInfo.Add(InlineIconLabel("ui.shield", "Барьер врага: " + state.Enemy.Barrier,
                    "Временное здоровье поглощает урон раньше здоровья врага."));
            if (state.Enemy.Phase > 0)
                enemyInfo.Add(LabelText("ФАЗА 2", 16, Gold));
            if (state.Enemy.PoisonStacks > 0)
                enemyInfo.Add(InlineIconLabel("status.poison", "Отравление: " + state.Enemy.PoisonStacks, PresentationText.StatusDescription("status.poison")));
            enemyPanel.Add(enemyInfo);
            _safeArea.Add(enemyPanel);
            StartEnemyIdleMotion();

            var intentCycle = state.Enemy.Phase > 0 && enemy.SecondPhaseIntentCycle.Count > 0
                ? enemy.SecondPhaseIntentCycle
                : enemy.IntentCycle;
            var intent = intentCycle[PositiveModulo(state.Enemy.IntentIndex, intentCycle.Count)];
            var intentPanel = Row();
            intentPanel.style.backgroundColor = Hex("#2C2028");
            intentPanel.style.borderLeftColor = Danger;
            intentPanel.style.borderLeftWidth = 3;
            intentPanel.style.flexShrink = 0;
            intentPanel.style.paddingLeft = 12;
            intentPanel.style.paddingRight = 12;
            intentPanel.style.paddingTop = 7;
            intentPanel.style.paddingBottom = 7;
            intentPanel.style.marginTop = 7;
            intentPanel.style.marginBottom = 7;
            intentPanel.style.alignItems = Align.Center;
            foreach (var intentIcon in IntentAssetKeys(intent))
                intentPanel.Add(Icon(intentIcon, 36));
            var intentDescription = PresentationText.IntentDescription(intent,
                MasteryContentCatalog.Instance.Get(state.DifficultyTier).EnemyDirectDamageBonus);
            foreach (var effect in intent.Effects)
                if (effect.Type == IntentEffectType.JamActiveSkill)
                    intentDescription += " · Цель: " + PresentationText.Name(state.Enemy.TelegraphedTargetId) +
                                         ", +" + effect.Amount + " ход перезарядки";
            if (enemy.IsElite && state.DifficultyTier >= 4 &&
                state.Enemy.IntentIndex == intentCycle.Count - 1)
                intentDescription += " · Дополнительно наложит Шипы на 1 кристалл";
            var intentText = LabelText("ДАЛЕЕ: " + PresentationText.Name(intent.TelegraphKey) + "\n" + intentDescription, 19, TextColor);
            intentText.style.flexGrow = 1;
            intentText.style.flexShrink = 1;
            intentText.style.whiteSpace = WhiteSpace.Normal;
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
            _message.style.whiteSpace = WhiteSpace.Normal;
            _safeArea.Add(_message);
            foreach (var child in _safeArea.Children()) child.style.flexShrink = 0;
            _safeArea.schedule.Execute(SizeEncounterBoard);

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
                modal.Add(ActionButton(_soundEnabled ? "ЗВУКОВЫЕ ЭФФЕКТЫ: ВКЛ." : "ЗВУКОВЫЕ ЭФФЕКТЫ: ВЫКЛ.", () =>
                {
                    _soundEnabled = !_soundEnabled;
                    PlayerPrefs.SetInt(SoundEnabledKey, _soundEnabled ? 1 : 0);
                    PlayerPrefs.Save();
                    BuildForCurrentScreen();
                }, false));
                modal.Add(ActionButton(_musicEnabled ? "МУЗЫКА: ВКЛ." : "МУЗЫКА: ВЫКЛ.", () =>
                {
                    _musicEnabled = !_musicEnabled;
                    PlayerPrefs.SetInt(MusicEnabledKey, _musicEnabled ? 1 : 0);
                    PlayerPrefs.Save();
                    BuildForCurrentScreen();
                }, false));
                modal.Add(ActionButton("КАК ИГРАТЬ", () => BuildHelp(BuildForCurrentScreen), false));
                modal.Add(Paragraph("Автор значков — Lorc. Опубликованы на game-icons.net по лицензии CC BY 3.0."));
                modal.Add(ActionButton("GAME-ICONS.NET", () => UnityEngine.Application.OpenURL("https://game-icons.net/"), false));
                modal.Add(ActionButton("CC BY 3.0", () => UnityEngine.Application.OpenURL("https://creativecommons.org/licenses/by/3.0/"), false));
            });
        }

        private void BuildBoard(BoardState boardState)
        {
            _board = new VisualElement { name = "board" };
            _board.style.alignSelf = Align.Center;
            _board.style.flexShrink = 0;
            _board.style.backgroundColor = Hex("#060E13");
            _board.style.paddingLeft = 8;
            _board.style.paddingRight = 8;
            _board.style.paddingTop = 8;
            _board.style.paddingBottom = 8;
            SetBorder(_board, Hex("#8B7955"), 2);
            _board.style.overflow = Overflow.Visible;
            // Measure both axes together: a height cap must never stretch the cells sideways.
            _safeArea.RegisterCallback<GeometryChangedEvent>(_ => SizeEncounterBoard());
            _board.RegisterCallback<AttachToPanelEvent>(_ => _board.schedule.Execute(SizeEncounterBoard));

            for (var row = BoardState.Height - 1; row >= 0; row--)
            {
                var rowElement = Row();
                rowElement.style.flexGrow = 1;
                rowElement.style.flexBasis = 0;
                rowElement.style.minHeight = 0;
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
            element.style.flexBasis = 0;
            element.style.minWidth = 0;
            element.style.marginLeft = 2;
            element.style.marginRight = 2;
            element.style.marginTop = 2;
            element.style.marginBottom = 2;
            element.style.backgroundColor = Hex("#11252B");
            SetBorder(element, _selectedCell.HasValue && _selectedCell.Value.Equals(cell) ? Gold : Hex("#29434A"), 2);
            element.style.borderTopLeftRadius = 3;
            element.style.borderTopRightRadius = 3;
            element.style.borderBottomLeftRadius = 3;
            element.style.borderBottomRightRadius = 3;

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
                var targetDefinition = MvpProgressionContentCatalog.Instance.GetSkill(_targetingSkill.Value);
                if (targetDefinition.TargetPolicy == SkillTargetPolicy.OneNormalGem ||
                    targetDefinition.TargetPolicy == SkillTargetPolicy.OneNormalGemAndColor)
                {
                    var valid = MvpBoardContentCatalog.Instance.IsNormalGem(gem.GemId) &&
                                gem.SpecialId.Equals(BoardContentIds.NoSpecial) &&
                                !Contains(gem.StatusIds, BoardContentIds.Anchored) &&
                                !Contains(gem.StatusIds, BoardContentIds.Frozen);
                    if (!valid)
                    {
                        SetMessage("Выберите подвижный обычный кристалл без особого свойства.", Danger);
                        return;
                    }
                    _skillTargets.Clear();
                    _skillTargets.Add(cell);
                    RefreshCellSelections();
                    SetMessage("Цель выбрана. Подтвердите навык.", Gold);
                    return;
                }
                if (targetDefinition.TargetPolicy == SkillTargetPolicy.OneMatchFourSpecial)
                {
                    var valid = IsMatchFourSpecial(gem.SpecialId) &&
                                !Contains(gem.StatusIds, BoardContentIds.Anchored) &&
                                !Contains(gem.StatusIds, BoardContentIds.Frozen);
                    if (!valid)
                    {
                        SetMessage("Выберите подвижную Искру, Поток, Спору или Заряд.", Danger);
                        return;
                    }
                    _skillTargets.Clear();
                    _skillTargets.Add(cell);
                    RefreshCellSelections();
                    SetMessage("Особый кристалл выбран. Подтвердите Детонацию.", Gold);
                    return;
                }
                if (targetDefinition.TargetPolicy == SkillTargetPolicy.UpToThreeNormalGems)
                {
                    var valid = MvpBoardContentCatalog.Instance.IsNormalGem(gem.GemId) &&
                                gem.SpecialId.Equals(BoardContentIds.NoSpecial) &&
                                !Contains(gem.StatusIds, BoardContentIds.Anchored) &&
                                !Contains(gem.StatusIds, BoardContentIds.Frozen);
                    if (!valid)
                    {
                        SetMessage("Выберите подвижный обычный кристалл.", Danger);
                        return;
                    }
                    var selectedIndex = _skillTargets.FindIndex(value => value.Equals(cell));
                    if (selectedIndex >= 0) _skillTargets.RemoveAt(selectedIndex);
                    else if (_skillTargets.Count < 3) _skillTargets.Add(cell);
                    else SetMessage("Переплетение действует максимум на три кристалла.", Danger);
                    RefreshCellSelections();
                    return;
                }
                if (gem.StatusIds == null || gem.StatusIds.Count == 0)
                {
                    SetMessage("Выберите кристалл с заморозкой, трещиной, якорем или шипами.", Danger);
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
            _playerHealthChip = ResourceChip("ui.player_health", "ЗДОР.", state.Player.Health, PlayerState.MaxHealth, Danger);
            _playerHealthLabel = _playerHealthChip.Q<Label>("resource-value");
            _presentedPlayerHealth = state.Player.Health;
            resources.Add(_playerHealthChip);
            resources.Add(ResourceChip("ui.focus", "ФОКУС", state.Player.Focus, 9, Cyan));
            resources.Add(ResourceChip("ui.toxic", "ТОКСИН", state.Player.Toxic, 9, Success));
            resources.Add(ResourceChip("ui.shield", "ЩИТ", state.Player.Shield, -1, Gold));
            _safeArea.Add(resources);
            _playerFeedbackAnchor = resources;
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
                    skillPanel.style.flexBasis = 0;
                    skillPanel.style.minWidth = 0;
                    skillPanel.style.marginLeft = 3;
                    skillPanel.style.marginRight = 3;
                    var button = new Button(() => SkillPressed(definition));
                    button.style.width = Length.Percent(100);
                    button.style.height = 68;
                    StyleGameButton(button, ready);
                    button.style.flexDirection = FlexDirection.Row;
                    button.style.alignItems = Align.Center;
                    button.style.justifyContent = Justify.Center;
                    button.style.backgroundColor = ready ? Hex("#16413F") : Hex("#19272C");
                    button.SetEnabled(ready);
                    button.tooltip = PresentationText.SkillDescription(definition);
                    button.Add(Icon(skillId.Value, 40));
                    var label = LabelText(PresentationText.Name(skillId) + (cooldown > 0 ? "\n" + cooldown + " ХОД." : ready ? "\nГОТОВО" : "\nНЕТ ЦЕЛИ"), 19, TextColor);
                    label.style.marginLeft = 8;
                    label.style.whiteSpace = WhiteSpace.Normal;
                    label.style.flexShrink = 1;
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
            if (_targetingSkill.HasValue)
            {
                var cancel = _targetingSkill.Value.Equals(definition.Id);
                CancelSkillTargeting();
                if (cancel) return;
            }
            _selectedCell = null;
            RefreshCellSelections();
            if (definition.TargetPolicy == SkillTargetPolicy.UpToThreeStatusGems)
            {
                _targetingSkill = definition.Id;
                _skillTargets.Clear();
                ShowCleanseTargeting(definition);
                return;
            }
            if (definition.TargetPolicy == SkillTargetPolicy.OneNormalGem)
            {
                _targetingSkill = definition.Id;
                _skillTargets.Clear();
                ShowInfuseTargeting(definition);
                return;
            }
            if (definition.TargetPolicy == SkillTargetPolicy.OneNormalGemAndColor ||
                definition.TargetPolicy == SkillTargetPolicy.OneMatchFourSpecial ||
                definition.TargetPolicy == SkillTargetPolicy.UpToThreeNormalGems)
            {
                _targetingSkill = definition.Id;
                _skillTargets.Clear();
                _skillOption = "content.none";
                ShowMutationTargeting(definition);
                return;
            }
            ExecuteSkill(definition.Id, null);
        }

        private void CancelSkillTargeting()
        {
            if (_inputLocked) return;
            _targetingSkill = null;
            _skillTargets.Clear();
            _skillOption = "content.none";
            _selectedCell = null;
            BuildEncounter();
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
                if (skill.RequiredBranchTags != null && skill.RequiredBranchTags.Count > 0)
                    modal.Add(LabelText("Требуются ветви: " + string.Join(" + ", new List<string>(skill.RequiredBranchTags).ToArray()), 17, Success));

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
            var cancel = ActionButton("ОТМЕНА", CancelSkillTargeting, false);
            cancel.style.flexGrow = 1;
            controls.Add(confirm);
            controls.Add(cancel);
            _safeArea.Add(controls);
        }

        private void ShowInfuseTargeting(SkillDefinition definition)
        {
            SetMessage("Выберите один подвижный обычный кристалл без особого свойства.", Gold);
            var controls = Row();
            controls.name = "targeting-controls";
            var confirm = ActionButton("ПОДТВЕРДИТЬ НАСЫЩЕНИЕ", () =>
            {
                if (_skillTargets.Count != 1)
                {
                    SetMessage("Сначала выберите один подходящий кристалл.", Danger);
                    return;
                }
                ExecuteSkill(definition.Id, _skillTargets);
            }, true);
            confirm.style.flexGrow = 1;
            var cancel = ActionButton("ОТМЕНА", CancelSkillTargeting, false);
            cancel.style.flexGrow = 1;
            controls.Add(confirm);
            controls.Add(cancel);
            _safeArea.Add(controls);
        }

        private void ShowMutationTargeting(SkillDefinition definition)
        {
            var transmute = definition.TargetPolicy == SkillTargetPolicy.OneNormalGemAndColor;
            var detonate = definition.TargetPolicy == SkillTargetPolicy.OneMatchFourSpecial;
            SetMessage(transmute ? "Выберите один обычный кристалл и новый цвет."
                : detonate ? "Выберите одну Искру, Поток, Спору или Заряд."
                : "Выберите от одного до трёх обычных кристаллов.", Gold);
            if (transmute)
            {
                var colors = Row();
                foreach (var colorId in MvpBoardContentCatalog.Instance.SpawnableGemIds)
                {
                    var captured = colorId;
                    var button = SmallButton(PresentationText.Name(colorId), () =>
                    {
                        _skillOption = captured;
                        SetMessage("Новый цвет: " + PresentationText.Name(captured) + ". Выберите цель и подтвердите.", Gold);
                    });
                    button.style.flexGrow = 1;
                    colors.Add(button);
                }
                _safeArea.Add(colors);
            }
            var controls = Row();
            var confirm = ActionButton("ПОДТВЕРДИТЬ", () =>
            {
                if (_skillTargets.Count < 1 || (detonate && _skillTargets.Count != 1) ||
                    (transmute && (_skillOption.Value == null || _skillOption.Value == "content.none")))
                {
                    SetMessage("Выберите подходящую цель" + (transmute ? " и новый цвет." : "."), Danger);
                    return;
                }
                ExecuteSkill(definition.Id, _skillTargets, _skillOption);
            }, true);
            confirm.style.flexGrow = 1;
            var cancel = ActionButton("ОТМЕНА", CancelSkillTargeting, false);
            cancel.style.flexGrow = 1;
            controls.Add(confirm);
            controls.Add(cancel);
            _safeArea.Add(controls);
        }

        private void ExecuteSkill(ContentId skillId, IEnumerable<GridCell> targets)
        {
            ExecuteSkill(skillId, targets, "content.none");
        }

        private void ExecuteSkill(ContentId skillId, IEnumerable<GridCell> targets, ContentId optionId)
        {
            _inputLocked = true;
            var result = _director.UseSkill(skillId, targets, optionId);
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
            var rewardTitle = state.PendingChoice.ChoiceId.Value.StartsWith("choice.evolution.", StringComparison.Ordinal)
                ? "ЭВОЛЮЦИЯ СБОРКИ"
                : state.PendingChoice.ChoiceId.Value == "choice.elite_keystone"
                    ? "ЭЛИТНОЕ УЛУЧШЕНИЕ"
                    : state.PendingChoice.Level > 0 ? "УРОВЕНЬ " + state.PendingChoice.Level : "НАГРАДА";
            _safeArea.Add(Title(rewardTitle, 46, Gold));
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
                StyleGameButton(card, true);
                card.style.backgroundColor = Panel;
                card.tooltip = "Открыть полное описание и выбрать улучшение";
                card.Add(Icon(optionId.Value, 100));
                var text = new VisualElement();
                text.style.flexGrow = 1;
                text.style.minWidth = 0;
                text.style.marginLeft = 20;
                var active = skill.SlotType == SkillSlotType.Active;
                var category = LabelText(active ? "АКТИВНЫЙ НАВЫК" : "ПАССИВНОЕ УЛУЧШЕНИЕ",
                    17, active ? Cyan : Success);
                category.style.whiteSpace = WhiteSpace.Normal;
                category.style.marginBottom = 4;
                text.Add(category);
                text.Add(Title(PresentationText.Name(optionId), 30, Gold));
                var description = LabelText(PresentationText.SkillDescription(skill), 20, TextColor);
                description.style.whiteSpace = WhiteSpace.Normal;
                text.Add(description);
                if (skill.HasPrerequisite)
                    text.Add(LabelText("Требуется: " + PresentationText.Name(skill.PrerequisiteId) + " ✓", 17, Success));
                if (skill.SynergyTags != null && skill.SynergyTags.Count > 0)
                    text.Add(LabelText(string.Join(" · ", new List<string>(skill.SynergyTags).ToArray()), 16, Cyan));
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

        private void BuildSanctum()
        {
            BeginScreen();
            var state = _director.State;
            _safeArea.style.justifyContent = Justify.Center;
            _safeArea.Add(Icon("ui.level_up", 118));
            _safeArea.Add(Title("СВЯТИЛИЩЕ МЕЖДУ МИРАМИ", 40, Gold));
            _safeArea.Add(LabelText(PresentationText.RegionName(state.Sanctum.CompletedRegionIndex) +
                " пройден. Здесь можно осмотреть сборку и переставить активные навыки перед новым регионом.",
                20, TextColor, TextAnchor.MiddleCenter));

            if (state.Sanctum.ChosenEvolutionId.Value != null &&
                state.Sanctum.ChosenEvolutionId.Value != "skill.none")
            {
                var evolution = Card();
                evolution.Add(LabelText("ВЫБРАННАЯ ЭВОЛЮЦИЯ", 17, Cyan));
                evolution.Add(Title(PresentationText.Name(state.Sanctum.ChosenEvolutionId), 28, Gold));
                evolution.Add(LabelText(PresentationText.SkillDescription(
                    MvpProgressionContentCatalog.Instance.GetSkill(state.Sanctum.ChosenEvolutionId)), 18, TextColor));
                _safeArea.Add(evolution);
            }

            var summary = Card("ui.panel.inset");
            summary.Add(StatLine("Здоровье", state.Player.Health + "/" + PlayerState.MaxHealth));
            summary.Add(StatLine("Изучено навыков", state.SelectedSkillIds.Count.ToString()));
            summary.Add(StatLine("Исполнено обетов", _director.Statistics.CompletedRouteVows.ToString()));
            _safeArea.Add(summary);
            _safeArea.Add(ActionButton("НАСТРОИТЬ АКТИВНЫЕ НАВЫКИ", BuildLoadoutModal, false));
            _safeArea.Add(ActionButton("ВОЙТИ В «" + PresentationText.RegionName(state.RegionIndex + 1).ToUpperInvariant() + "»",
                ContinueFromSanctum, true));
            _safeArea.Add(ActionButton("СОХРАНИТЬСЯ И ВЫЙТИ", () =>
            {
                _director.ReturnToTitle(false);
                BuildTitle();
            }, false));
        }

        private void ContinueFromSanctum()
        {
            if (_inputLocked) return;
            _inputLocked = true;
            var result = _director.ContinueFromSanctum();
            if (!result.Accepted)
            {
                _inputLocked = false;
                return;
            }
            PlayBatch(result.Events, BuildForCurrentScreen);
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
            _safeArea.Add(LabelText(victory ? PresentationText.Name(_director.State.Map.BossEnemyId) + " повержен." : "Кристаллы запомнят эту попытку.",
                22, TextColor, TextAnchor.MiddleCenter));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _safeArea.Add(scroll);

            var summary = Card();
            summary.Add(StatLine("Побеждено врагов", statistics.EncountersCleared.ToString()));
            summary.Add(StatLine("Самая длинная цепочка", statistics.BiggestCascade.ToString()));
            summary.Add(StatLine("Общий урон", statistics.TotalDamage.ToString()));
            summary.Add(StatLine("Завершено ходов", _director.State.ResolvedTurnCount.ToString()));
            summary.Add(StatLine("Посещено узлов", statistics.RouteNodeIds == null ? "0" : statistics.RouteNodeIds.Count.ToString()));
            summary.Add(StatLine("Исполнено обетов", statistics.CompletedRouteVows.ToString()));
            summary.Add(StatLine("Сложность", _director.State.DifficultyTier.ToString()));
            if (_director.State.IsChallengeRun)
                summary.Add(StatLine("Испытание", PresentationText.Name(_director.State.ChallengeId)));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            summary.Add(StatLine("Отладочное зерно", _director.State.Seed.ToString()));
#endif
            scroll.Add(summary);

            scroll.Add(SectionHeading("УРОН ПО ИСТОЧНИКАМ"));
            foreach (var damage in statistics.DamageBySource)
                scroll.Add(StatLine(PresentationText.Name(damage.SourceId), damage.Amount.ToString()));
            scroll.Add(SectionHeading("ВЫБРАННЫЕ УЛУЧШЕНИЯ"));
            var upgrades = new List<string>();
            foreach (var id in _director.State.SelectedSkillIds)
            {
                if (id.Value == "skill.sunder" || id.Value == "skill.cleanse") continue;
                upgrades.Add(PresentationText.Name(id));
            }
            scroll.Add(Paragraph(upgrades.Count == 0 ? "Нет" : string.Join(" · ", upgrades.ToArray())));

            if (_director.LastProfileUpdate != null && _director.LastProfileUpdate.UnlockedContent.Count > 0)
            {
                scroll.Add(SectionHeading("ОТКРЫТО"));
                var unlocked = new List<string>();
                foreach (var id in _director.LastProfileUpdate.UnlockedContent) unlocked.Add(PresentationText.Name(id));
                scroll.Add(Paragraph(string.Join(" · ", unlocked.ToArray())));
            }
            if (_director.LastProfileUpdate != null && _director.LastProfileUpdate.CompletedChallenges.Count > 0)
            {
                scroll.Add(SectionHeading("ВЫПОЛНЕННЫЕ ЦЕЛИ"));
                foreach (var id in _director.LastProfileUpdate.CompletedChallenges)
                {
                    var goal = FindGoal(id);
                    if (goal != null) scroll.Add(GoalCard(goal, true, true));
                }
            }
            if (_director.LastProfileUpdate != null &&
                _director.LastProfileUpdate.DifficultyAfter > _director.LastProfileUpdate.DifficultyBefore)
            {
                var tier = _director.LastProfileUpdate.DifficultyAfter;
                var definition = MasteryContentCatalog.Instance.Get(tier);
                var unlockedDifficulty = Card();
                unlockedDifficulty.Add(Title("ОТКРЫТА СЛОЖНОСТЬ " + tier, 23, Success));
                unlockedDifficulty.Add(LabelText(PresentationText.Name(definition.Id) + ": " +
                    DifficultyRuleText(tier) + ".", 18, TextColor, TextAnchor.MiddleCenter));
                scroll.Add(unlockedDifficulty);
            }
            scroll.Add(SectionHeading("ПРОГРЕСС СЛОЖНОСТИ"));
            if (_director.LastProfileUpdate != null && _director.LastProfileUpdate.CountedDominantBranches.Count > 0)
            {
                var counted = new List<string>();
                foreach (var branch in _director.LastProfileUpdate.CountedDominantBranches)
                    counted.Add(PresentationText.Name(branch));
                scroll.Add(Paragraph("Главные источники этого победного забега: " +
                    string.Join(" · ", counted.ToArray()) + "."));
                if (_director.LastProfileUpdate.NewDominantBranchWins.Count > 0)
                {
                    var gained = new List<string>();
                    foreach (var branch in _director.LastProfileUpdate.NewDominantBranchWins)
                        gained.Add(PresentationText.Name(branch));
                    scroll.Add(LabelText("Новый прогресс: " + string.Join(" · ", gained.ToArray()), 18, Success));
                }
                else
                {
                    scroll.Add(LabelText("Эти источники уже были засчитаны ранее.", 18, Muted));
                }
            }
            scroll.Add(DifficultyGoalCard(true));
            scroll.Add(SectionHeading("СЛЕДУЮЩИЕ ЦЕЛИ"));
            var shownGoals = 0;
            foreach (var goal in ProfileProgression.SuggestedGoals(_director.Profile, 4))
            {
                if (goal.Id.Equals(ProfileContentIds.ChallengeDifficultyOne)) continue;
                scroll.Add(GoalCard(goal, false, true));
                shownGoals++;
                if (shownGoals >= 2) break;
            }
            _safeArea.Add(ActionButton("НОВЫЙ ЗАБЕГ", StartRun, true));
            _safeArea.Add(ActionButton("В ГЛАВНОЕ МЕНЮ", () =>
            {
                _director.ReturnToTitle(true);
                BuildTitle();
            }, false));
        }

        private void PlayEventSound(SimulationEvent item, HashSet<string> played)
        {
            var key = item.Type == SimulationEventType.SwapAccepted ? "feedback.swap"
                : item.Type == SimulationEventType.SpecialCreated || item.Type == SimulationEventType.SpecialActivated
                    ? "feedback.special"
                : item.Type == SimulationEventType.DamageApplied ? "feedback.hit"
                : item.Type == SimulationEventType.EnemyIntentStarted ? "feedback.intent"
                : item.Type == SimulationEventType.StatusAdded ? "feedback.status_added"
                : item.Type == SimulationEventType.StatusRemoved ? "feedback.status_removed"
                : item.Type == SimulationEventType.EnemyDefeated ? "feedback.victory"
                : item.Type == SimulationEventType.RunEnded ? "feedback.defeat"
                : item.Type == SimulationEventType.SkillChosen ? "feedback.reward_confirmed"
                : item.Type == SimulationEventType.SkillUsed && item.SourceId.Value == "skill.sunder" ? "feedback.sunder"
                : string.Empty;
            if (string.IsNullOrEmpty(key) || !played.Add(key) || _catalog == null) return;

            if (key == "feedback.special")
            {
                PlayOneShot(VariantKey("feedback.special.crystal", 3, item.Sequence), 0.78f);
                PlayOneShot(VariantKey("feedback.special.magic", 2, item.Sequence), 0.52f);
                return;
            }
            if (key == "feedback.hit")
            {
                PlayOneShot(VariantKey("feedback.hit.stone", 3, item.Sequence), 0.82f);
                return;
            }
            if (key == "feedback.status_removed" && item.SourceId.Value == "status.frozen")
            {
                PlayOneShot(VariantKey("feedback.status_removed.frozen", 3, item.Sequence), 0.72f);
                return;
            }
            PlayOneShot(key);
        }

        private void PlayClearSound(List<SimulationEvent> clearEvents, int cascadeStep)
        {
            if (clearEvents == null || clearEvents.Count == 0) return;
            var sequence = clearEvents[0].Sequence;
            var variant = Mathf.Min(Mathf.Max(cascadeStep, 1), 5);
            var pitch = 1f + 0.055f * Mathf.Min(cascadeStep - 1, 5);
            PlayOneShot("feedback.clear.crystal." + variant, 0.82f, pitch);

            var gemId = DominantClearedGem(clearEvents);
            if (gemId == "gem.ember")
                PlayOneShot("feedback.clear.ember", 0.34f, pitch);
            else if (gemId == "gem.tide")
                PlayOneShot(VariantKey("feedback.clear.tide", 2, sequence), 0.32f, pitch);
            else if (gemId == "gem.venom")
                PlayOneShot(VariantKey("feedback.clear.venom", 3, sequence), 0.32f, pitch);
            else if (gemId == "gem.volt")
                PlayOneShot("feedback.clear.volt", 0.38f, pitch);
        }

        private static string DominantClearedGem(List<SimulationEvent> clearEvents)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var bestId = string.Empty;
            var bestCount = 0;
            foreach (var item in clearEvents)
            {
                var id = item.SourceId.Value;
                if (string.IsNullOrEmpty(id)) continue;
                int count;
                counts.TryGetValue(id, out count);
                count++;
                counts[id] = count;
                if (count <= bestCount) continue;
                bestId = id;
                bestCount = count;
            }
            return bestId;
        }

        private static string VariantKey(string prefix, int count, long sequence)
        {
            var index = PositiveModulo((int)(sequence % count), count) + 1;
            return prefix + "." + index;
        }

        private void PlayOneShot(string key, float volume = 1f, float pitch = 1f)
        {
            if (!_soundEnabled || _catalog == null || _sfxSources.Count == 0) return;
            var clip = _catalog.GetAudio(key);
            if (clip == null) return;

            AudioSource source = null;
            foreach (var candidate in _sfxSources)
            {
                if (candidate.isPlaying) continue;
                source = candidate;
                break;
            }
            if (source == null)
            {
                source = _sfxSources[_nextSfxSource];
                source.Stop();
            }
            _nextSfxSource = (_nextSfxSource + 1) % _sfxSources.Count;
            source.pitch = Mathf.Clamp(pitch, 0.75f, 1.5f);
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void ApplyMusicForCurrentScreen()
        {
            var key = "music.crystal_cave";
            if (_director != null &&
                (_director.Screen == RunScreen.Encounter || _director.Screen == RunScreen.SkillWindow))
            {
                var currentNode = MapSimulation.GetCurrentNode(_director.State);
                if (currentNode != null && currentNode.Type == MapNodeType.Boss)
                    key = "music.boss_battle";
            }
            PlayMusic(key);
        }

        private void PlayMusic(string key)
        {
            if (_musicSource == null) return;
            if (!_musicEnabled || _catalog == null)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
                return;
            }
            var clip = _catalog.GetAudio(key);
            if (clip == null)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
                return;
            }
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;
            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicSource.Play();
        }

        private void RejectInput(string text)
        {
            SetMessage(text, Danger);
            PlayOneShot("feedback.invalid_swap");
            if (_reducedMotion || _board == null) return;
            StartCoroutine(AnimateInvalidNudge());
        }

        private void RefreshCellSelections()
        {
            foreach (var entry in _boardCells)
            {
                var selected = (_selectedCell.HasValue && _selectedCell.Value.Equals(entry.Key)) ||
                               _skillTargets.Exists(value => value.Equals(entry.Key));
                SetBorder(entry.Value, selected ? Gold : Hex("#29434A"), 2);
                entry.Value.style.backgroundColor = selected ? Hex("#2B453D") : Hex("#11252B");
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
            modal.style.maxHeight = Length.Percent(92);
            modal.style.paddingLeft = 28;
            modal.style.paddingRight = 28;
            modal.style.paddingTop = 24;
            modal.style.paddingBottom = 24;
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.Add(Title(heading, 36, Gold));
            if (!string.IsNullOrEmpty(body)) scroll.Add(Paragraph(body));
            addContent?.Invoke(scroll);
            scroll.Add(ActionButton("ЗАКРЫТЬ", () => shade.RemoveFromHierarchy(), false));
            modal.Add(scroll);
            shade.Add(modal);
            _root.Add(shade);
        }

        private VisualElement Card(string skinKey = "ui.panel.primary")
        {
            var element = new VisualElement();
            element.style.backgroundColor = Panel;
            element.style.paddingLeft = 14;
            element.style.paddingRight = 14;
            element.style.paddingTop = 10;
            element.style.paddingBottom = 10;
            element.style.marginTop = 6;
            element.style.marginBottom = 6;
            SetBorder(element, Hex("#355054"), 1);
            element.style.borderTopWidth = 2;
            element.style.borderTopColor = Hex("#8B7955");
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
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.height = 68;
            button.style.width = Length.Percent(100);
            button.style.maxWidth = 720;
            button.style.alignSelf = Align.Center;
            button.style.marginTop = 7;
            button.style.marginBottom = 7;
            button.style.fontSize = 23;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = TextColor;
            SkinButton(button,
                primary ? "ui.button.primary" : "ui.button.secondary",
                primary ? "ui.button.primary.pressed" : "ui.button.secondary.pressed");
            return button;
        }

        private Button SmallButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.minWidth = 72;
            button.style.height = 46;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.fontSize = 16;
            button.style.color = TextColor;
            var square = text.Length <= 2;
            SkinButton(button,
                square ? "ui.button.square.secondary" : "ui.button.secondary",
                square ? "ui.button.square.secondary.pressed" : "ui.button.secondary.pressed");
            return button;
        }

        private void SkinButton(Button button, string normalKey, string pressedKey)
        {
            StyleGameButton(button, normalKey.Contains("primary"));
        }

        private void SetButtonSprite(Button button, string key)
        {
            var sprite = _catalog == null ? null : _catalog.GetSprite(key);
            if (sprite != null) button.style.backgroundImage = new StyleBackground(sprite);
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
            chip.style.flexBasis = 0;
            chip.style.minWidth = 0;
            chip.style.marginLeft = 2;
            chip.style.marginRight = 2;
            chip.style.paddingLeft = 5;
            chip.style.paddingRight = 5;
            chip.style.paddingTop = 5;
            chip.style.paddingBottom = 5;
            chip.style.backgroundColor = Hex("#102329");
            chip.style.borderBottomWidth = 2;
            chip.style.borderBottomColor = color;
            chip.style.alignItems = Align.Center;
            chip.tooltip = label + ": " + value + (maximum >= 0 ? " из " + maximum : string.Empty);
            chip.Add(Icon(iconKey, 30));
            var text = LabelText(label + "\n" + value + (maximum >= 0 ? "/" + maximum : string.Empty), 15, color, TextAnchor.MiddleCenter);
            text.name = "resource-value";
            text.style.flexGrow = 1;
            chip.Add(text);
            return chip;
        }

        private static VisualElement Bar(string label, float fraction, Color color)
        {
            var back = new VisualElement();
            back.style.height = 28;
            SetBorder(back, Hex("#553C40"), 1);
            back.style.backgroundColor = Hex("#091419");
            back.style.marginTop = 5;
            var fill = new VisualElement();
            fill.name = "bar-fill";
            fill.style.position = Position.Absolute;
            fill.style.left = 0;
            fill.style.top = 0;
            fill.style.bottom = 0;
            fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
            fill.style.backgroundColor = color;
            back.Add(fill);
            var text = LabelText(label, 17, TextColor, TextAnchor.MiddleCenter);
            text.name = "bar-label";
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

        private sealed class MapConnectionBand : VisualElement
        {
            private readonly List<MapNodeState> _upperNodes;
            private readonly List<MapNodeState> _lowerNodes;

            public MapConnectionBand(List<MapNodeState> upperNodes, List<MapNodeState> lowerNodes)
            {
                _upperNodes = upperNodes;
                _lowerNodes = lowerNodes;
                name = "map-connections";
                pickingMode = PickingMode.Ignore;
                style.height = 22;
                style.flexShrink = 0;
                generateVisualContent += DrawConnections;
            }

            private void DrawConnections(MeshGenerationContext context)
            {
                if (_upperNodes.Count == 0 || _lowerNodes.Count == 0) return;
                var painter = context.painter2D;
                painter.lineWidth = 3f;
                painter.strokeColor = new Color(0.40f, 0.84f, 0.91f, 0.48f);
                var width = contentRect.width;
                var height = contentRect.height;
                for (var lowerIndex = 0; lowerIndex < _lowerNodes.Count; lowerIndex++)
                {
                    var lower = _lowerNodes[lowerIndex];
                    for (var upperIndex = 0; upperIndex < _upperNodes.Count; upperIndex++)
                    {
                        var upper = _upperNodes[upperIndex];
                        if (!ThreeInARowApp.Contains(lower.ConnectionIds, upper.Id)) continue;
                        painter.BeginPath();
                        painter.MoveTo(new Vector2((upperIndex + 0.5f) * width / _upperNodes.Count, 0));
                        painter.LineTo(new Vector2((lowerIndex + 0.5f) * width / _lowerNodes.Count, height));
                        painter.Stroke();
                    }
                }
            }
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
            if (skill.Id.Value == "skill.infuse")
            {
                foreach (var gem in state.Board.Gems)
                    if (gem != null && MvpBoardContentCatalog.Instance.IsNormalGem(gem.GemId) &&
                        gem.SpecialId.Equals(BoardContentIds.NoSpecial) &&
                        !Contains(gem.StatusIds, BoardContentIds.Anchored) &&
                        !Contains(gem.StatusIds, BoardContentIds.Frozen)) return true;
                return false;
            }
            if (skill.Id.Value == "skill.transmute" || skill.Id.Value == "skill.reweave")
            {
                foreach (var gem in state.Board.Gems)
                    if (gem != null && MvpBoardContentCatalog.Instance.IsNormalGem(gem.GemId) &&
                        gem.SpecialId.Equals(BoardContentIds.NoSpecial) &&
                        !Contains(gem.StatusIds, BoardContentIds.Anchored) &&
                        !Contains(gem.StatusIds, BoardContentIds.Frozen)) return true;
                return false;
            }
            if (skill.Id.Value == "skill.detonate")
            {
                foreach (var gem in state.Board.Gems)
                    if (gem != null && IsMatchFourSpecial(gem.SpecialId) &&
                        !Contains(gem.StatusIds, BoardContentIds.Anchored) &&
                        !Contains(gem.StatusIds, BoardContentIds.Frozen)) return true;
                return false;
            }
            return true;
        }

        private static bool IsMatchFourSpecial(ContentId id)
        {
            return id.Equals(BoardContentIds.Spark) || id.Equals(BoardContentIds.Current) ||
                   id.Equals(BoardContentIds.Spore) || id.Equals(BoardContentIds.Charge);
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

        private static IEnumerable<string> IntentAssetKeys(IntentDefinition intent)
        {
            var telegraphKey = intent.TelegraphKey;
            if (telegraphKey == "intent.chip" || telegraphKey == "intent.crack" ||
                telegraphKey == "intent.chill" || telegraphKey == "intent.needle" ||
                telegraphKey == "intent.crush" || telegraphKey == "intent.bolt" ||
                telegraphKey == "intent.drain" || telegraphKey == "intent.seal" ||
                telegraphKey == "intent.shardstorm" || telegraphKey == "intent.freeze_anchor" ||
                telegraphKey == "intent.bite" || telegraphKey == "intent.freeze_hit" ||
                telegraphKey == "intent.claw" || telegraphKey == "intent.barrier" ||
                telegraphKey == "intent.jam" || telegraphKey == "intent.thorns")
            {
                yield return telegraphKey;
                if (telegraphKey == "intent.crush") yield return "status.cracked";
                if (telegraphKey == "intent.freeze_anchor") yield return "status.anchored";
                yield break;
            }

            foreach (var effect in intent.Effects)
            {
                if (effect.Type == IntentEffectType.DamagePlayer) yield return "intent.bolt";
                else if (effect.Type == IntentEffectType.ApplyBoardStatus) yield return effect.StatusId.Value;
                else if (effect.Type == IntentEffectType.DrainResources) yield return "intent.drain";
                else if (effect.Type == IntentEffectType.GainEnemyBarrier) yield return "intent.barrier";
                else if (effect.Type == IntentEffectType.JamActiveSkill) yield return "intent.jam";
            }
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
