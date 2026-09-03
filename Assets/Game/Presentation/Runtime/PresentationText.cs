using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Combat;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Progression;

namespace ThreeInARow.Presentation
{
    internal static class PresentationText
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "gem.ember", "Пламя" }, { "gem.tide", "Прилив" }, { "gem.venom", "Яд" },
            { "gem.volt", "Разряд" }, { "gem.prism", "Призма" },
            { "special.spark", "Искра" }, { "special.current", "Поток" },
            { "special.spore", "Спора" }, { "special.charge", "Заряд" }, { "special.prism", "Призма" },
            { "status.frozen", "Заморозка" }, { "status.cracked", "Трещина" },
            { "status.anchored", "Якорь" }, { "status.poison", "Отравление" },
            { "enemy.geode_mite", "Геодовый клещ" }, { "enemy.frost_oracle", "Ледяной оракул" },
            { "enemy.geode_mite_elite", "Матёрый геодовый клещ" }, { "enemy.prism_stalker", "Призматический охотник" },
            { "enemy.crystal_warden", "Кристальный страж" },
            { "skill.kindling", "Растопка" }, { "skill.backdraft", "Обратная тяга" },
            { "skill.flow_state", "Состояние потока" }, { "skill.undertow", "Обратное течение" },
            { "skill.corrosive", "Разъедание" }, { "skill.overcharge", "Перегрузка" },
            { "skill.sunder", "Раскол" }, { "skill.cleanse", "Очищение" }, { "skill.catalyze", "Катализ" },
            { "intent.chip", "Скол" }, { "intent.crack", "Трещина" }, { "intent.chill", "Холод" },
            { "intent.needle", "Игла" }, { "intent.crush", "Сокрушение" }, { "intent.bolt", "Разряд" },
            { "intent.drain", "Истощение" }, { "intent.seal", "Печать" },
            { "intent.shardstorm", "Буря осколков" }, { "intent.freeze_anchor", "Заморозка и якорь" },
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
            if (skill.Id.Value == "skill.sunder") return "Наносит 14 прямого урона. Перезарядка: 4 хода.";
            if (skill.Id.Value == "skill.cleanse") return "Снимает состояния с 1–3 выбранных кристаллов. Перезарядка: 5 ходов.";
            if (skill.Id.Value == "skill.catalyze") return "Преобразует до 4 ед. концентрации в урон, а пары токсина — в отравление. Перезарядка: 5 ходов.";
            return Name(skill.Id);
        }

        public static string IntentDescription(IntentDefinition intent)
        {
            var parts = new List<string>();
            foreach (var effect in intent.Effects)
            {
                if (effect.Type == IntentEffectType.DamagePlayer) parts.Add("Нанесёт " + effect.Amount + " урона");
                else if (effect.Type == IntentEffectType.ApplyBoardStatus)
                    parts.Add("Наложит «" + Name(effect.StatusId) + "» на " + effect.Amount + " крист.");
                else if (effect.Type == IntentEffectType.DrainResources)
                    parts.Add("Заберёт до " + effect.FocusAmount + " ед. концентрации и " + effect.ToxicAmount + " ед. токсина");
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
