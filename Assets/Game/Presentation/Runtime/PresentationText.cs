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
            { "enemy.sootcap_shaman", "Сажешляпый шаман" }, { "enemy.glassvine_serpent", "Стеклолозный змей" },
            { "enemy.ashen_dryad", "Пепельная дриада" }, { "enemy.furnace_matriarch", "Матриарх горнила" },
            { "enemy.shard_leech", "Осколочная пиявка" }, { "enemy.orbit_sentinel", "Орбитальный страж" },
            { "enemy.parallax_knight", "Рыцарь параллакса" }, { "enemy.singularity_seraph", "Серафим сингулярности" },
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
            { "skill.evolution.sparkstorm", "Искровая буря" }, { "skill.evolution.ashen_aegis", "Пепельная эгида" },
            { "skill.evolution.deep_current", "Глубинный поток" }, { "skill.evolution.tidal_memory", "Память прилива" },
            { "skill.evolution.virulent_bloom", "Ядовитое цветение" }, { "skill.evolution.patient_venom", "Терпеливый яд" },
            { "skill.evolution.overclock", "Разгон" }, { "skill.evolution.storm_reserve", "Грозовой запас" },
            { "skill.keystone.briarheart", "Сердце терновника" }, { "skill.keystone.rootbreaker", "Корнелом" },
            { "skill.keystone.emberseed", "Угольное семя" }, { "skill.keystone.null_coil", "Нулевая катушка" },
            { "skill.keystone.mirror_shard", "Зеркальный осколок" }, { "skill.keystone.rift_lens", "Линза разлома" },
            { "event.faceted_altar", "Гранёный алтарь" }, { "event.quiet_pool", "Тихий омут" },
            { "event.static_loom", "Статический станок" }, { "event.prism_echo", "Эхо призмы" },
            { "event.frozen_reliquary", "Ледяной реликварий" }, { "event.cracked_cache", "Треснувший тайник" },
            { "event.rest_site", "Привал" },
            { "event.prismatic_archive", "Призматический архив" },
            { "event.cinder.ember_orchard", "Угольный сад" }, { "event.cinder.ashen_nursery", "Пепельный питомник" },
            { "event.cinder.stag_trail", "След оленя" }, { "event.cinder.rootspeaker_shrine", "Святилище корнегласа" },
            { "event.void.mirror_well", "Зеркальный колодец" }, { "event.void.null_observatory", "Нулевая обсерватория" },
            { "event.void.echo_prison", "Темница эха" }, { "event.void.broken_constellation", "Разбитое созвездие" },
            { "vow.elite_hunter", "Обет охотника" }, { "vow.no_rest", "Обет неутомимого" },
            { "vow.event_seeker", "Обет искателя" },
            { "emblem.explorer", "Знак исследователя" }, { "emblem.elite_hunter", "Знак охотника" },
            { "emblem.bossbreaker", "Знак крушителя" }, { "emblem.wayfarer", "Знак странника" },
            { "emblem.veteran", "Знак ветерана" }, { "emblem.cascade", "Знак каскада" },
            { "emblem.avalanche", "Знак лавины" }, { "emblem.artificer", "Знак мастера" },
            { "emblem.storyseeker", "Знак сказителя" }, { "emblem.oathkeeper", "Знак хранителя клятв" },
            { "emblem.conqueror", "Знак покорителя" }, { "emblem.expeditioner", "Знак экспедиции" },
            { "expedition.trial.ember", "Печь искр" }, { "expedition.trial.tide", "Холодное течение" },
            { "expedition.trial.venom", "Терновая настойка" }, { "expedition.trial.volt", "Замкнутый контур" },
            { "expedition.trial.sparks", "Искровой ливень" }, { "expedition.trial.cascades", "Неутихающий каскад" },
            { "expedition.trial.cleanse", "Чистая грань" }, { "expedition.trial.prism", "Призматический долг" },
            { "expedition.trial.warden", "Возвращение стража" }, { "expedition.trial.treant", "Сердце древня" },
            { "expedition.trial.devourer", "Голод пустоты" }, { "expedition.trial.perfect_facet", "Идеальная экспедиция" },
            { "status.none", "нет" }, { "skill.none", "нет" },
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
            { "intent.spore_haze", "Споровая дымка" }, { "intent.ash_sip", "Глоток пепла" },
            { "intent.flare_hex", "Пламенное проклятие" }, { "intent.glass_skin", "Стеклянная кожа" },
            { "intent.coiling_roots", "Кольца корней" }, { "intent.venomous_glare", "Ядовитый взгляд" },
            { "intent.cinder_bark", "Угольная кора" }, { "intent.ashfall", "Пеплопад" },
            { "intent.dryad_wail", "Плач дриады" }, { "intent.brood_barrier", "Барьер выводка" },
            { "intent.cinder_web", "Угольная сеть" }, { "intent.furnace_bite", "Укус горнила" },
            { "intent.hatch", "Вылупление" }, { "intent.immolate", "Самосожжение" }, { "intent.feast", "Пир" },
            { "intent.essence_siphon", "Сифон сущности" }, { "intent.shard_bite", "Осколочный укус" },
            { "intent.crystal_gorge", "Кристальная сытость" }, { "intent.gravity_ring", "Кольцо тяготения" },
            { "intent.orbit_shell", "Орбитальная оболочка" }, { "intent.comet_lance", "Кометное копьё" },
            { "intent.mirror_guard", "Зеркальная защита" }, { "intent.split_horizon", "Раскол горизонта" },
            { "intent.parallax_cut", "Разрез параллакса" }, { "intent.halo_lock", "Замок нимба" },
            { "intent.dark_grace", "Тёмная благодать" }, { "intent.star_spear", "Звёздное копьё" },
            { "intent.black_hymn", "Чёрный гимн" }, { "intent.fallen_halo", "Падший нимб" },
            { "intent.annihilation", "Аннигиляция" },
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
            return ResolveName(id.Value);
        }

        public static string RegionName(int zeroBasedRegionIndex)
        {
            if (zeroBasedRegionIndex == 0) return "Хрустальный шпиль";
            if (zeroBasedRegionIndex == 1) return "Пеплоцветные дебри";
            return "Глубины пустостекла";
        }

        public static string Name(string id)
        {
            return ResolveName(id);
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
            if (skill.Id.Value == "skill.evolution.sparkstorm") return "Первая Искра за ход наносит ещё 12 урона.";
            if (skill.Id.Value == "skill.evolution.ashen_aegis") return "Каждая сработавшая Искра даёт ещё 8 щита.";
            if (skill.Id.Value == "skill.evolution.deep_current") return "Каждое преобразование концентрации наносит ещё 2 урона.";
            if (skill.Id.Value == "skill.evolution.tidal_memory") return "Каждое преобразование концентрации даёт ещё 4 щита.";
            if (skill.Id.Value == "skill.evolution.virulent_bloom") return "Каждый заряд отравления наносит ещё 2 урона.";
            if (skill.Id.Value == "skill.evolution.patient_venom") return "Большие совпадения Яда дают ещё 3 токсина.";
            if (skill.Id.Value == "skill.evolution.overclock") return "Заряд дополнительно сокращает обе перезарядки ещё на 2.";
            if (skill.Id.Value == "skill.evolution.storm_reserve") return "Любое ускорение перезарядки даёт ещё 4 щита.";
            if (skill.Id.Value == "skill.keystone.briarheart") return "Победа восстанавливает ещё 2 здоровья.";
            if (skill.Id.Value == "skill.keystone.rootbreaker") return "При исчезновении щита наносит врагу до 12 дополнительного урона.";
            if (skill.Id.Value == "skill.keystone.emberseed") return "В начале каждого боя появляется дополнительная Призма.";
            if (skill.Id.Value == "skill.keystone.null_coil") return "Использованный навык начинает перезарядку ещё на 2 хода ниже.";
            if (skill.Id.Value == "skill.keystone.mirror_shard") return "Преобразование концентрации даёт ещё 3 щита.";
            if (skill.Id.Value == "skill.keystone.rift_lens") return "При исчезновении щита наносит врагу до 14 дополнительного урона.";
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
            if (eventId.Value == "event.cinder.ember_orchard") return "Живой сад хранит угольное семя, которое может отозваться глубже в чаще.";
            if (eventId.Value == "event.cinder.ashen_nursery") return "Под слоем пепла дремлют новые силы и старые корни.";
            if (eventId.Value == "event.cinder.stag_trail") return "Светящийся след обещает защиту, если решиться пойти за ним.";
            if (eventId.Value == "event.cinder.rootspeaker_shrine") return "Корни слушают путника; принесённое семя заставит их заговорить.";
            if (eventId.Value == "event.void.mirror_well") return "Колодец отражает не лицо, а возможный исход следующего боя.";
            if (eventId.Value == "event.void.null_observatory") return "В пустом телескопе застряло эхо ещё не сделанного выбора.";
            if (eventId.Value == "event.void.echo_prison") return "За стеклом томится отражение, если вы сумели поймать его раньше.";
            if (eventId.Value == "event.void.broken_constellation") return "Осколки звёзд можно собрать в Призму или рассыпать ради знания.";
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
            if (id == "choice.cinder.ember_orchard.seed") return "Взять угольное семя и Призму; наложить Шипы на 2 кристалла.";
            if (id == "choice.cinder.ember_orchard.rest") return "Отдохнуть в тепле и восстановить 7 здоровья.";
            if (id == "choice.cinder.ashen_nursery.raid") return "Потерять 6 здоровья; выбрать одно из 2 улучшений.";
            if (id == "choice.cinder.ashen_nursery.soothe") return "Успокоить корни и снять все состояния с поля.";
            if (id == "choice.cinder.stag_trail.follow") return "Пойти по следу; следующий бой начнётся с 12 щита.";
            if (id == "choice.cinder.stag_trail.avoid") return "Не рисковать и восстановить 5 здоровья.";
            if (id == "choice.cinder.rootspeaker_shrine.awaken") return "Отдать угольное семя; выбрать пассивное улучшение и восстановить 4 здоровья.";
            if (id == "choice.cinder.rootspeaker_shrine.listen") return "Выслушать корни и сократить обе перезарядки на 2.";
            if (id == "choice.void.mirror_well.drink") return "Восстановить 8 здоровья, потеряв концентрацию и токсин.";
            if (id == "choice.void.mirror_well.shatter") return "Создать Призму и потерять 4 здоровья.";
            if (id == "choice.void.null_observatory.capture") return "Поймать эхо: зарядить навыки и заморозить 2 кристалла.";
            if (id == "choice.void.null_observatory.leave") return "Оставить эхо в покое.";
            if (id == "choice.void.echo_prison.free") return "Освободить пойманное эхо: выбрать активный навык и очистить поле.";
            if (id == "choice.void.echo_prison.drain") return "Поглотить отражение; следующий бой начнётся с 10 щита.";
            if (id == "choice.void.broken_constellation.align") return "Собрать Призму; наложить Трещину на 3 кристалла.";
            if (id == "choice.void.broken_constellation.scatter") return "Потерять 5 здоровья; выбрать одно из 2 улучшений.";
            return Name(choiceId);
        }

        public static string RouteVowDescription(ContentId vowId)
        {
            if (vowId.Value == "vow.elite_hunter") return "Победить элитного врага в этом регионе.";
            if (vowId.Value == "vow.no_rest") return "Дойти до босса, не посещая привал.";
            if (vowId.Value == "vow.event_seeker") return "Завершить оба события региона.";
            return Name(vowId);
        }

        public static string CodexLore(ContentId id)
        {
            if (id.Value == "enemy.sootcap_shaman") return "Сажешляпые слушают треск спор и считают его голосом будущего пожара.";
            if (id.Value == "enemy.glassvine_serpent") return "Стеклолоза растёт вокруг добычи кольцами, пока та не становится частью прозрачного сада.";
            if (id.Value == "enemy.ashen_dryad") return "Пепельная дриада помнит каждый лес, который сгорел, чтобы нынешний смог прорасти.";
            if (id.Value == "enemy.furnace_matriarch") return "Матриарх высиживает в горниле не потомство, а новые формы живого огня.";
            if (id.Value == "enemy.shard_leech") return "Осколочная пиявка пьёт не кровь, а возможности, оставляя жертве единственный путь.";
            if (id.Value == "enemy.orbit_sentinel") return "Орбитальный страж вращает вокруг себя обломки миров, которых больше нет.";
            if (id.Value == "enemy.parallax_knight") return "Рыцарь наносит удар из того положения, которое наблюдатель не выбрал.";
            if (id.Value == "enemy.singularity_seraph") return "Падший нимб Серафима удерживает песню звёзд на самой границе тишины.";
            if (id.Value == "event.cinder.ember_orchard") return "Угольное семя переживает любой пожар, но прорастает лишь у того, кто согласился нести его дальше.";
            if (id.Value == "event.cinder.rootspeaker_shrine") return "Корнеглас отвечает только путникам с живым семенем; остальным достаётся шёпот прежних лесов.";
            if (id.Value == "event.void.null_observatory") return "Нулевая обсерватория наблюдает не звёзды, а решения, исчезнувшие в момент выбора.";
            if (id.Value == "event.void.echo_prison") return "Пойманное эхо может стать союзником, но оно всегда помнит жизнь, которую у него отняли.";
            if (id.Value != null && id.Value.StartsWith("enemy.", StringComparison.Ordinal))
                return "Существо впитало силу своего региона; его намерения раскрывают лучший способ пережить встречу.";
            if (id.Value != null && id.Value.StartsWith("event.cinder.", StringComparison.Ordinal))
                return "Часть истории Пеплоцветных дебрей — места, где огонь служит не только разрушению, но и росту.";
            if (id.Value != null && id.Value.StartsWith("event.void.", StringComparison.Ordinal))
                return "Фрагмент истории Глубин пустостекла, где отражения помнят варианты, от которых отказались.";
            if (id.Value != null && id.Value.StartsWith("emblem.", StringComparison.Ordinal))
                return "Личный знак мастерства, не дающий боевого преимущества.";
            return string.Empty;
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

        private static string ResolveName(string id)
        {
            var key = id ?? string.Empty;
            string value;
            if (Names.TryGetValue(key, out value)) return value;
            if (key.StartsWith("expedition.daily.", StringComparison.Ordinal))
                return "Ежедневный маршрут · " + key.Substring("expedition.daily.".Length);
            if (key.StartsWith("intent.", StringComparison.Ordinal))
            {
                var dot = key.LastIndexOf('.');
                if (dot >= 0 && Names.TryGetValue("intent." + key.Substring(dot + 1), out value))
                    return value;
            }
            return Humanize(key);
        }
    }
}
