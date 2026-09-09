using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Presentation
{
    internal static class PresentationText
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "gem.ember", "Пламя" }, { "gem.tide", "Прилив" }, { "gem.venom", "Яд" },
            { "gem.volt", "Разряд" }, { "gem.prism", "Призма" },
            { "branch.ember", "Пламя" }, { "branch.tide", "Прилив" },
            { "branch.venom", "Яд" }, { "branch.volt", "Разряд" },
            { "special.spark", "Искра" }, { "special.current", "Поток" },
            { "special.spore", "Спора" }, { "special.charge", "Заряд" }, { "special.prism", "Призма" },
            { "status.frozen", "Заморозка" }, { "status.cracked", "Трещина" },
            { "status.anchored", "Якорь" }, { "status.poison", "Отравление" },
            { "status.thorned", "Шипы" },
            { "enemy.geode_mite", "Геодовый клещ" }, { "enemy.frost_oracle", "Ледяной оракул" },
            { "enemy.geode_mite_elite", "Матёрый геодовый клещ" }, { "enemy.prism_stalker", "Призматический охотник" },
            { "enemy.crystal_warden", "Кристальный страж" },
            { "enemy.crystal_tick", "Кристальный клещ" }, { "enemy.rime_moth", "Инейная моль" },
            { "enemy.anchor_crab", "Якорный краб" }, { "enemy.hollow_idol", "Полый идол" },
            { "enemy.fracture_golem", "Голем разлома" }, { "enemy.stormglass_roc", "Громостеклянный рух" },
            { "enemy.facet_engine", "Гранёный механизм" },
            { "enemy.briar_wisp", "Шипастый огонёк" }, { "enemy.ashback_boar", "Пеплоспинный вепрь" },
            { "enemy.cinder_nymph", "Угольная нимфа" }, { "enemy.thornbound_stag", "Терновый олень" },
            { "enemy.pyreheart_treant", "Огнесердный древень" },
            { "enemy.nullwing_bat", "Пустокрылая мышь" }, { "enemy.mirror_eel", "Зеркальный угорь" },
            { "enemy.rift_weaver", "Ткач разлома" }, { "enemy.eclipse_chimera", "Химера затмения" },
            { "enemy.astral_devourer", "Астральный пожиратель" },
            { "skill.kindling", "Растопка" }, { "skill.backdraft", "Обратная тяга" },
            { "skill.flow_state", "Состояние потока" }, { "skill.undertow", "Обратное течение" },
            { "skill.corrosive", "Разъедание" }, { "skill.overcharge", "Перегрузка" },
            { "skill.sunder", "Раскол" }, { "skill.cleanse", "Очищение" }, { "skill.catalyze", "Катализ" },
            { "skill.cinderwake", "Шлейф углей" }, { "skill.reservoir", "Резервуар" },
            { "skill.concentrate", "Концентрат" }, { "skill.contagion", "Заражение" },
            { "skill.static_guard", "Статический щит" }, { "skill.live_wire", "Живой провод" },
            { "skill.aegis", "Эгида" }, { "skill.infuse", "Насыщение" },
            { "skill.transmute", "Трансмутация" }, { "skill.detonate", "Детонация" },
            { "skill.reweave", "Переплетение" }, { "skill.flashfire", "Вспышка" },
            { "skill.galvanic_venom", "Гальванический яд" },
            { "skill.scalding_current", "Обжигающий поток" },
            { "skill.toxic_undertow", "Токсичный отлив" },
            { "skill.keystone.tempered_core", "Закалённое ядро" },
            { "skill.keystone.prismatic_start", "Призматический старт" },
            { "skill.keystone.rapid_casting", "Быстрое сотворение" },
            { "skill.keystone.hard_light", "Твёрдый свет" },
            { "event.faceted_altar", "Гранёный алтарь" }, { "event.quiet_pool", "Тихий омут" },
            { "event.static_loom", "Статический станок" }, { "event.prism_echo", "Эхо призмы" },
            { "event.frozen_reliquary", "Ледяной реликварий" }, { "event.cracked_cache", "Треснувший тайник" },
            { "event.rest_site", "Привал" },
            { "event.prismatic_archive", "Призматический архив" },
            { "pressure.crack", "Трещины" }, { "pressure.freeze", "Заморозка" },
            { "pressure.anchor", "Якоря" }, { "pressure.drain", "Истощение" }, { "pressure.mixed", "Смешанное давление" },
            { "pressure.thorns", "Шипы" }, { "pressure.jam", "Помехи" }, { "pressure.barrier", "Барьеры" },
            { "intent.chip", "Скол" }, { "intent.crack", "Трещина" }, { "intent.chill", "Холод" },
            { "intent.needle", "Игла" }, { "intent.crush", "Сокрушение" }, { "intent.bolt", "Разряд" },
            { "intent.drain", "Истощение" }, { "intent.seal", "Печать" },
            { "intent.shardstorm", "Буря осколков" }, { "intent.freeze_anchor", "Заморозка и якорь" },
            { "intent.bite", "Укус" }, { "intent.freeze_hit", "Морозный удар" }, { "intent.claw", "Клешня" },
            { "intent.barrier", "Барьер" }, { "intent.jam", "Помеха" }, { "intent.thorns", "Шипы" },
            { "intent.thorn_kiss", "Поцелуй шипов" }, { "intent.needleflare", "Игловспышка" },
            { "intent.bramble_burst", "Взрыв терна" }, { "intent.cinder_charge", "Угольный таран" },
            { "intent.faultline", "Линия разлома" }, { "intent.magma_hide", "Магмовая шкура" },
            { "intent.ember_veil", "Угольная завеса" }, { "intent.wildfire", "Дикий огонь" },
            { "intent.hexflare", "Проклятое пламя" }, { "intent.antler_sweep", "Взмах рогов" },
            { "intent.root_snare", "Корневая хватка" }, { "intent.heartfire", "Огонь сердца" },
            { "intent.bramble_crown", "Терновый венец" }, { "intent.furnace_roar", "Рёв горнила" },
            { "intent.rootquake", "Корнелом" }, { "intent.ember_bark", "Угольная кора" },
            { "intent.sapping_flame", "Истощающее пламя" }, { "intent.inferno", "Инферно" },
            { "intent.null_screech", "Нулевой визг" }, { "intent.void_bite", "Укус пустоты" },
            { "intent.nightglass", "Ночное стекло" }, { "intent.reflection", "Отражение" },
            { "intent.prism_lash", "Призматическая плеть" }, { "intent.siphon_glide", "Скользящий сифон" },
            { "intent.rift_tether", "Узы разлома" }, { "intent.rupture", "Разрыв" },
            { "intent.entropy_thread", "Нить энтропии" }, { "intent.eclipse_veil", "Завеса затмения" },
            { "intent.umbra_talon", "Коготь умбры" }, { "intent.gravity_knot", "Узел гравитации" },
            { "intent.singularity_drag", "Тяга сингулярности" }, { "intent.event_horizon", "Горизонт событий" },
            { "intent.starfall", "Звездопад" }, { "intent.void_carapace", "Панцирь пустоты" },
            { "intent.collapse", "Коллапс" }, { "intent.gravity_lock", "Гравитационный замок" },
            { "intent.devour", "Пожирание" },
            { "difficulty.0.standard", "Обычная" }, { "difficulty.1.sharp_edges", "Острые грани" },
            { "difficulty.2.unstable_grid", "Нестабильная сетка" }, { "difficulty.3.long_road", "Долгий путь" },
            { "difficulty.4.hostile_pattern", "Враждебный узор" }, { "difficulty.5.perfect_facet", "Идеальная грань" },
            { "intent.geode_mite.chip_5", "Скол" }, { "intent.geode_mite.chip_6", "Скол" },
            { "intent.geode_mite.crack_3", "Трещина" },
            { "intent.frost_oracle.freeze_2", "Заморозка" }, { "intent.frost_oracle.freeze_3", "Заморозка" },
            { "intent.frost_oracle.needle_7", "Ледяная игла" },
            { "intent.geode_mite_elite.crush", "Сокрушение" },
            { "intent.geode_mite_elite.chip_7", "Скол" }, { "intent.geode_mite_elite.crack_4", "Трещина" },
            { "intent.prism_stalker.bolt_8", "Разряд" }, { "intent.prism_stalker.bolt_10", "Разряд" },
            { "intent.prism_stalker.drain", "Истощение" },
            { "intent.crystal_warden.shardstorm_10", "Буря осколков" },
            { "intent.crystal_warden.shardstorm_12", "Буря осколков" },
            { "intent.crystal_warden.freeze_anchor", "Заморозка и якорь" },
            { "intent.crystal_warden.seal", "Печать" },
            { "ui.player_health", "Здоровье" }, { "ui.enemy_health", "Здоровье врага" },
            { "ui.focus", "Концентрация" }, { "ui.toxic", "Токсин" }, { "ui.shield", "Щит" },
            { "ui.experience", "Опыт" }, { "ui.level_up", "Новый уровень" },
            { "ui.victory", "Победа" }, { "ui.defeat", "Поражение" },
            { "system.board", "Поле" }, { "system.combat", "Бой" },
            { "system.progression", "Развитие" }, { "system.foundation", "Система" }
        };

        public static string Name(ContentId id)
        {
            string value;
            return Names.TryGetValue(id.Value ?? string.Empty, out value) ? value : Humanize(id.Value);
        }

        public static string RegionName(int zeroBasedRegionIndex)
        {
            if (zeroBasedRegionIndex == 0) return "Хрустальный шпиль";
            if (zeroBasedRegionIndex == 1) return "Пеплоцветные дебри";
            return "Глубины пустостекла";
        }

        public static string Name(string id)
        {
            string value;
            return Names.TryGetValue(id ?? string.Empty, out value) ? value : Humanize(id);
        }

        public static string GemDescription(ContentId gem, ContentId special)
        {
            var baseDescription = gem.Value == "gem.ember" ? "При исчезновении наносит 4 прямого урона."
                : gem.Value == "gem.tide" ? "Добавляет концентрацию; каждые 3 ед. наносят урон."
                : gem.Value == "gem.venom" ? "Добавляет токсин; 5 ед. взрываются и накладывают отравление."
                : gem.Value == "gem.volt" ? "Наносит 2 урона и ускоряет перезарядку активных навыков."
                : "Убирает все кристаллы цвета, с которым её поменяли.";
            if (!string.IsNullOrEmpty(special.Value) && special.Value != "special.none")
                return "Особый кристалл «" + Name(special) + "». " + baseDescription;
            return "Кристалл «" + Name(gem) + "». " + baseDescription;
        }

        public static string StatusDescription(string id)
        {
            if (id == "status.frozen") return "Замороженные кристаллы нельзя менять местами, но они складываются в ряды и исчезают как обычно.";
            if (id == "status.cracked") return "Треснувшие кристаллы исчезают как обычно, но не дают свой эффект.";
            if (id == "status.anchored") return "Кристаллы с якорем нельзя двигать или менять местами в этот ход, но их можно убрать совпадением.";
            if (id == "status.poison") return "Отравление наносит врагу урон перед его ответом, затем теряет один заряд.";
            if (id == "status.thorned") return "Уборка шипованного кристалла наносит 2 урона здоровью; не более 6 за одно разрешение поля. Состояние снимается Очищением.";
            return Name(id);
        }

        public static string SkillDescription(SkillDefinition skill)
        {
            if (skill.Id.Value == "skill.kindling") return "Урон Пламени при исчезновении: +1.";
            if (skill.Id.Value == "skill.backdraft") return "Когда исчезает Искра, вы получаете 6 ед. щита.";
            if (skill.Id.Value == "skill.flow_state") return "Преобразование концентрации наносит 7 урона вместо 6.";
            if (skill.Id.Value == "skill.undertow") return "Преобразование концентрации сокращает перезарядку левого активного навыка на 1.";
            if (skill.Id.Value == "skill.corrosive") return "Каждый заряд отравления наносит 4 урона вместо 3.";
            if (skill.Id.Value == "skill.overcharge") return "Для ускорения перезарядки нужно убрать 2 Разряда вместо 3.";
            if (skill.Id.Value == "skill.cinderwake") return "Первая Искра за ход наносит на 8 урона больше.";
            if (skill.Id.Value == "skill.reservoir") return "Каждое преобразование концентрации даёт 2 щита.";
            if (skill.Id.Value == "skill.concentrate") return "Токсин срабатывает при 4 ед. вместо 5.";
            if (skill.Id.Value == "skill.contagion") return "Совпадение из 4+ кристаллов Яда даёт ещё 2 токсина.";
            if (skill.Id.Value == "skill.static_guard") return "Ускорение перезарядки даёт 2 щита один раз за эффект.";
            if (skill.Id.Value == "skill.live_wire") return "Заряд дополнительно сокращает обе перезарядки на 1.";
            if (skill.Id.Value == "skill.sunder") return "Наносит 14 прямого урона. Перезарядка: 4 хода.";
            if (skill.Id.Value == "skill.cleanse") return "Снимает состояния с 1–3 выбранных кристаллов. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.catalyze") return "Преобразует до 4 ед. концентрации в урон, а пары токсина — в отравление. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.aegis") return "Даёт 10 щита. Перезарядка: 4 хода.";
            if (skill.Id.Value == "skill.infuse") return "Превращает выбранный обычный кристалл в его особую версию. Перезарядка: 6 ходов.";
            if (skill.Id.Value == "skill.transmute") return "Меняет цвет одного подвижного обычного кристалла. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.detonate") return "Активирует выбранный особый кристалл на месте. Перезарядка: 6 ходов.";
            if (skill.Id.Value == "skill.reweave") return "Пересоздаёт до трёх обычных кристаллов. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.flashfire") return "Активация Искры сокращает обе экипированные перезарядки на 1.";
            if (skill.Id.Value == "skill.galvanic_venom") return "Каждое срабатывание отравления даёт 1 ед. прогресса Разряда.";
            if (skill.Id.Value == "skill.scalding_current") return "Каждое второе преобразование концентрации усиливает следующую группу Пламени на 2 урона за кристалл.";
            if (skill.Id.Value == "skill.toxic_undertow") return "Преобразование концентрации добавляет 1 токсин, не чаще раза за каскад.";
            if (skill.Id.Value == "skill.keystone.tempered_core") return "Исцеление после победы увеличено с 4 до 7.";
            if (skill.Id.Value == "skill.keystone.prismatic_start") return "В начале каждого боя один подходящий кристалл становится Призмой.";
            if (skill.Id.Value == "skill.keystone.rapid_casting") return "Использованный активный навык начинает перезарядку на 1 ход ниже, минимум 1.";
            if (skill.Id.Value == "skill.keystone.hard_light") return "При исчезновении щит наносит врагу 1 урон за 2 щита, максимум 8.";
            return Name(skill.Id);
        }

        public static string SkillDetails(SkillDefinition skill)
        {
            if (skill.Id.Value == "skill.kindling")
                return "Действует автоматически весь забег. Каждый убранный кристалл Пламени наносит 5 урона вместо 4, в том числе при каскадах и очистке Призмой.";
            if (skill.Id.Value == "skill.backdraft")
                return "Действует автоматически. Когда с поля исчезает особая Искра, вы получаете 6 ед. щита. Искра создаётся совпадением из четырёх кристаллов Пламени.";
            if (skill.Id.Value == "skill.flow_state")
                return "Действует автоматически. Каждые 3 ед. концентрации по-прежнему расходуются вместе, но теперь наносят 7 урона вместо 6.";
            if (skill.Id.Value == "skill.undertow")
                return "Действует автоматически. Каждый раз, когда 3 ед. концентрации превращаются в урон, перезарядка навыка в левой ячейке сокращается ещё на 1 ход.";
            if (skill.Id.Value == "skill.corrosive")
                return "Действует автоматически. Перед ответом врага каждый заряд отравления наносит 4 урона вместо 3, после чего снимается один заряд.";
            if (skill.Id.Value == "skill.overcharge")
                return "Действует автоматически. Достаточно убрать 2 кристалла Разряда вместо 3, чтобы сократить перезарядку экипированных активных навыков на 1 ход.";
            if (skill.Id.Value == "skill.sunder")
                return "Используйте перед перестановкой, чтобы сразу нанести врагу 14 урона. Навык не расходует перестановку, поэтому после него можно сделать обычный ход. Затем он перезаряжается 4 хода.";
            if (skill.Id.Value == "skill.cleanse")
                return "Используйте перед перестановкой. Выберите до трёх кристаллов с Заморозкой, Трещиной или Якорем и подтвердите выбор. Если таких кристаллов не больше трёх, можно подтвердить без выбора и очистить все. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.catalyze")
                return "Используйте перед перестановкой. Навык расходует до 4 ед. концентрации и наносит 3 урона за каждую, затем расходует до 4 ед. токсина парами и даёт 1 заряд отравления за каждую пару. Не тратит ресурс, который не даст эффекта. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.infuse")
                return "Используйте перед перестановкой. Выберите один обычный кристалл без особого свойства, Заморозки или Якоря: он станет Искрой, Потоком, Спорой или Зарядом своего цвета. Трещина сохраняется. Перезарядка: 6 ходов.";
            if (skill.Id.Value == "skill.transmute") return "Выберите один подвижный обычный кристалл и новый цвет. Если возникает совпадение, оно разрешается через обычные события поля. Точный предел: одна цель. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.detonate") return "Выберите Искру, Поток, Спору или Заряд: особый кристалл активируется на месте, поле заполняется и каскады разрешаются как обычно. Перезарядка: 6 ходов.";
            if (skill.Id.Value == "skill.reweave") return "Выберите от одного до трёх подвижных обычных кристаллов. Их новые цвета берутся из потока BoardSpawn; затем поле гарантированно стабильно и играбельно. Перезарядка: 5 ходов.";
            return SkillDescription(skill);
        }

        public static string EventDescription(ContentId eventId)
        {
            if (eventId.Value == "event.faceted_altar") return "Алтарь предлагает силу в обмен на кровь.";
            if (eventId.Value == "event.quiet_pool") return "Тихая вода лечит, но смывает накопленные ресурсы.";
            if (eventId.Value == "event.static_loom") return "Станок мгновенно заряжает навыки и раскалывает поле.";
            if (eventId.Value == "event.prism_echo") return "Эхо может породить Призму — за цену.";
            if (eventId.Value == "event.frozen_reliquary") return "Внутри заключён новый активный навык и древний холод.";
            if (eventId.Value == "event.cracked_cache") return "Можно забрать улучшение сейчас или подготовить защиту.";
            if (eventId.Value == "event.prismatic_archive") return "Открытый архив предлагает редкое знание за часть здоровья.";
            return "Выберите способ восстановиться перед продолжением пути.";
        }

        public static string ChoiceDescription(ContentId choiceId)
        {
            var id = choiceId.Value;
            if (id == "choice.faceted_altar.draft_passive") return "Потерять 8 здоровья; выбрать одно пассивное улучшение.";
            if (id == "choice.faceted_altar.leave") return "Уйти без последствий.";
            if (id == "choice.quiet_pool.heal") return "Восстановить 10 здоровья; концентрация и токсин станут равны 0.";
            if (id == "choice.quiet_pool.leave") return "Сохранить ресурсы и уйти.";
            if (id == "choice.static_loom.ready") return "Обнулить перезарядки; наложить Трещину на 4 кристалла.";
            if (id == "choice.static_loom.leave") return "Уйти без последствий.";
            if (id == "choice.prism_echo.create_prism") return "Создать одну Призму; потерять 5 здоровья.";
            if (id == "choice.prism_echo.heal") return "Восстановить 5 здоровья и уйти.";
            if (id == "choice.frozen_reliquary.draft_active") return "Выбрать один активный навык; заморозить 3 кристалла.";
            if (id == "choice.frozen_reliquary.cleanse") return "Снять все состояния с поля.";
            if (id == "choice.cracked_cache.draft") return "Выбрать одно из 2 улучшений; следующий бой начнётся с 3 Трещинами.";
            if (id == "choice.cracked_cache.shield") return "Следующий бой начнётся с 6 щита.";
            if (id == "choice.rest.heal") return "Восстановить 12 здоровья.";
            if (id == "choice.rest.repair") return "Снять все состояния с поля и сократить обе перезарядки на 2.";
            if (id == "choice.prismatic_archive.study") return "Потерять 6 здоровья; выбрать одно из трёх улучшений.";
            if (id == "choice.prismatic_archive.leave") return "Восстановить 3 здоровья и уйти.";
            return Name(choiceId);
        }

        public static string NodeTypeName(MapNodeType type)
        {
            if (type == MapNodeType.NormalCombat) return "Обычный бой";
            if (type == MapNodeType.EliteCombat) return "Элитный бой";
            if (type == MapNodeType.Event) return "Событие";
            if (type == MapNodeType.Rest) return "Привал";
            return "Босс";
        }

        public static string IntentDescription(IntentDefinition intent, int enemyDirectDamageBonus = 0)
        {
            var parts = new List<string>();
            foreach (var effect in intent.Effects)
            {
                if (effect.Type == IntentEffectType.DamagePlayer) parts.Add("Нанесёт " + (effect.Amount + enemyDirectDamageBonus) + " урона");
                else if (effect.Type == IntentEffectType.ApplyBoardStatus)
                    parts.Add("Наложит «" + Name(effect.StatusId) + "» на " + effect.Amount + " крист.");
                else if (effect.Type == IntentEffectType.DrainResources)
                    parts.Add("Заберёт до " + effect.FocusAmount + " ед. концентрации и " + effect.ToxicAmount + " ед. токсина");
                else if (effect.Type == IntentEffectType.GainEnemyBarrier)
                    parts.Add("Получит " + effect.Amount + " временного барьера");
                else if (effect.Type == IntentEffectType.JamActiveSkill)
                    parts.Add("Добавит " + effect.Amount + " ход перезарядки одному активному навыку");
            }
            return string.Join(" · ", parts.ToArray());
        }

        private static string Humanize(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Неизвестно";
            var dot = id.LastIndexOf('.');
            var value = dot >= 0 ? id.Substring(dot + 1) : id;
            value = value.Replace('_', ' ');
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
