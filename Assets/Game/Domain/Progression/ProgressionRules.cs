using System.Collections.Generic;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Progression
{
    public static class ProgressionRules
    {
        public static bool HasSkill(RunState state, ContentId skillId)
        {
            return state != null && Contains(state.SelectedSkillIds, skillId);
        }

        public static int GetModifier(
            RunState state,
            PassiveModifierType modifierType,
            IProgressionContentCatalog catalog = null)
        {
            if (state == null || state.SelectedSkillIds == null) return 0;
            catalog = catalog ?? MvpProgressionContentCatalog.Instance;
            var total = 0;
            foreach (var skillId in state.SelectedSkillIds)
            {
                SkillDefinition definition;
                try
                {
                    definition = catalog.GetSkill(skillId);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }

                foreach (var modifier in definition.PassiveModifiers)
                    if (modifier.Type == modifierType) total += modifier.Amount;
            }
            return total;
        }

        public static bool IsEquipped(PlayerState player, ContentId skillId)
        {
            return player != null && Contains(player.EquippedActiveSkillIds, skillId);
        }

        public static SkillCooldownState FindCooldown(PlayerState player, ContentId skillId)
        {
            if (player == null || player.SkillCooldowns == null) return null;
            foreach (var cooldown in player.SkillCooldowns)
                if (cooldown != null && cooldown.SkillId.Equals(skillId)) return cooldown;
            return null;
        }

        internal static bool Contains(IEnumerable<ContentId> ids, ContentId wanted)
        {
            if (ids == null) return false;
            foreach (var id in ids)
                if (id.Equals(wanted)) return true;
            return false;
        }
    }
}
