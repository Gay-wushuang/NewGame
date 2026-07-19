using System.Collections.Generic;

public static class CardDisplayFormatter
{
    public static string FormatName(CardData data)
    {
        return data.Name;
    }

    public static string FormatCardTypeLabel(CardData data)
    {
        switch (data.Subtype)
        {
            case CardSubtype.Attack:
                return "Attack";
            case CardSubtype.Defense:
                return "Defense";
            case CardSubtype.PositiveBuff:
                return "Buff";
            case CardSubtype.NegativeBuff:
                return "Debuff";
            case CardSubtype.BattleLevelConsumable:
            case CardSubtype.GameLevelConsumable:
                return "Item";
            case CardSubtype.Equipment:
                return "Equip";
            case CardSubtype.Curse:
                return "Curse";
            default:
                return data.Type.ToString();
        }
    }

    public static string FormatCardStatLine(CardData data, CardInstance card, bool contextual)
    {
        if (data.DamageFormula != null)
        {
            int minDamage, maxDamage;
            data.GetDamageRange(contextual ? 6 : 6, out minDamage, out maxDamage);
            return minDamage == maxDamage ? $"DMG {minDamage}" : $"DMG {minDamage}~{maxDamage}";
        }

        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
        {
            return $"Shield {data.ShieldValue}";
        }

        if (data.Subtype == CardSubtype.Curse)
        {
            return card.CurseStacks > 1 ? $"Stacks {card.CurseStacks}" : "Curse";
        }

        if (data.EffectAmount > 0)
        {
            return $"Effect {data.EffectAmount}";
        }

        return "Effect";
    }

    public static string FormatCost(CardData data)
    {
        var costs = new List<string>();
        
        if (data.EnergyCost > 0)
        {
            costs.Add($"{data.EnergyCost} Energy");
        }
        
        if (data.DiceCost > 0)
        {
            costs.Add($"{data.DiceCost} 枚骰子");
        }
        
        if (costs.Count == 0)
        {
            return "无消耗";
        }
        
        return string.Join("、", costs);
    }

    public static string FormatRuleText(CardData data, CardInstance card, int diceSides)
    {
        var parts = new List<string>();
        
        if (data.DamageFormula != null)
        {
            parts.Add(FormatDamageEffect(data, diceSides));
        }
        
        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
        {
            parts.Add($"获得 {data.ShieldValue} 点护盾。");
        }
        
        if (data.Subtype == CardSubtype.PositiveBuff && data.AppliedBuffType.HasValue)
        {
            parts.Add(FormatBuffEffect(data));
        }
        
        if (data.Subtype == CardSubtype.NegativeBuff && data.AppliedDebuffType.HasValue)
        {
            parts.Add(FormatDebuffEffect(data));
        }
        
        if (data.Subtype == CardSubtype.BattleLevelConsumable)
        {
            parts.Add(FormatBattleConsumableEffect(data));
        }
        
        if (data.Subtype == CardSubtype.GameLevelConsumable)
        {
            parts.Add(FormatGameConsumableEffect(data));
        }
        
        if (data.Subtype == CardSubtype.Equipment && data.EquipSlot.HasValue)
        {
            parts.Add(FormatEquipmentEffect(data));
        }
        
        if (data.Subtype == CardSubtype.Curse)
        {
            parts.Add(FormatCurseEffect(data, card));
        }
        
        if (data.AppliedDebuffType.HasValue && data.Subtype != CardSubtype.NegativeBuff && data.Subtype != CardSubtype.Curse)
        {
            parts.Add(FormatConditionalDebuff(data));
        }
        
        return string.Join("\n", parts);
    }

    public static string FormatKeywordText(CardData data)
    {
        var parts = new List<string>();
        
        if (data.ShieldValue > 0 || data.Subtype == CardSubtype.Defense)
        {
            parts.Add("护盾：受到攻击时优先于 Energy 承受伤害。\n未消耗的护盾在下一玩家回合开始时消散。");
        }
        
        if (data.AppliedDebuffType == DebuffType.Vulnerable || 
            (data.AppliedDebuffType.HasValue && data.AppliedDebuffType.Value == DebuffType.Vulnerable))
        {
            parts.Add("破甲：每层使敌人受到的攻击伤害增加 1。\n敌人回合结束时减少 1 层。");
        }
        
        if (data.AppliedDebuffType == DebuffType.Weak)
        {
            parts.Add("Weak：敌人攻击伤害减少对应层数，持续 2 回合。");
        }
        
        if (data.AppliedBuffType == BuffType.EnergyRegen)
        {
            parts.Add("能量回复：下回合开始额外恢复对应数量的 Energy。");
        }
        
        if (data.Subtype == CardSubtype.BattleLevelConsumable)
        {
            parts.Add($"消耗品：每场战斗限用 {data.UsesPerBattle} 次，使用后进入消耗堆。");
        }
        
        if (data.Subtype == CardSubtype.GameLevelConsumable)
        {
            parts.Add("消耗品：全局可用，使用后进入消耗堆。");
        }
        
        if (data.Subtype == CardSubtype.Equipment)
        {
            parts.Add("装备：装备后持续生效，持续对应场次后移除。");
        }
        
        if (data.Subtype == CardSubtype.Curse)
        {
            string duration = data.CurseDuration == CurseDurationType.Temporary ? "临时" : "永久";
            parts.Add($"诅咒（{duration}）：打出后有 {data.CurseDisappearChance * 100}% 概率消失，{data.CurseStrengthenChance * 100}% 概率强化，其余概率无事发生。");
        }
        
        return parts.Count > 0 ? string.Join("\n\n", parts) : "";
    }

    private static string FormatDamageEffect(CardData data, int diceSides)
    {
        int minDamage, maxDamage;
        data.GetDamageRange(diceSides, out minDamage, out maxDamage);
        
        if (data.DiceCost == 0)
        {
            return $"造成 {minDamage} 点伤害。";
        }
        
        if (minDamage == maxDamage)
        {
            return $"造成 {minDamage} 点伤害。";
        }
        
        return $"造成“骰点 + {minDamage - 1}”的伤害。\n使用 d{diceSides} 时，伤害范围为 {minDamage}～{maxDamage}。";
    }

    private static string FormatBuffEffect(CardData data)
    {
        string buffName = GetBuffName(data.AppliedBuffType.Value);
        string duration = data.Duration > 0 ? $"，持续 {data.Duration} 回合" : "";
        return $"获得 {buffName}（{data.EffectAmount}）{duration}。";
    }

    private static string FormatDebuffEffect(CardData data)
    {
        string debuffName = GetDebuffName(data.AppliedDebuffType.Value);
        string duration = data.Duration > 0 ? $"，持续 {data.Duration} 回合" : "";
        return $"施加 {data.EffectAmount} 层 {debuffName}{duration}。";
    }

    private static string FormatConditionalDebuff(CardData data)
    {
        if (data.AppliedDebuffType != DebuffType.Vulnerable)
            return "";
            
        return $"如果骰点不低于 {data.EffectAmount + 3}，施加 {data.EffectAmount} 层破甲。";
    }

    private static string FormatBattleConsumableEffect(CardData data)
    {
        return $"恢复 {data.EffectAmount} Energy。\n每场战斗可以使用 {data.UsesPerBattle} 次。";
    }

    private static string FormatGameConsumableEffect(CardData data)
    {
        return $"恢复 {data.EffectAmount} HP。";
    }

    private static string FormatEquipmentEffect(CardData data)
    {
        string slotName = GetEquipmentSlotName(data.EquipSlot.Value);
        return $"装备 {slotName}，攻击伤害 +{data.EffectAmount}。\n持续 {data.Duration} 场战斗。";
    }

    private static string FormatCurseEffect(CardData data, CardInstance card)
    {
        string triggerName = GetCurseTriggerName(data.CurseTrigger);
        string stacks = card.CurseStacks > 1 ? $"（{card.CurseStacks} 层）" : "";
        
        switch (data.CurseTrigger)
        {
            case CurseTriggerType.SelfDamage:
                return $"每回合失去 {data.CurseEffectAmount} HP{stacks}。";
            case CurseTriggerType.HandSizeReduction:
                return $"手牌上限 -1{stacks}。";
            case CurseTriggerType.DrawReduction:
                return $"每回合少抽 {data.CurseEffectAmount} 张牌{stacks}。";
            case CurseTriggerType.EnergyDrain:
                return $"每回合失去 {data.CurseEffectAmount} Energy{stacks}。";
            default:
                return $"触发 {triggerName} 效果{stacks}。";
        }
    }

    private static string GetBuffName(BuffType buffType)
    {
        switch (buffType)
        {
            case BuffType.AttackUp: return "攻击力提升";
            case BuffType.DefenseUp: return "防御力提升";
            case BuffType.EnergyRegen: return "能量回复";
            case BuffType.DiceBonus: return "骰子加成";
            case BuffType.CriticalRateUp: return "暴击率提升";
            default: return buffType.ToString();
        }
    }

    private static string GetDebuffName(DebuffType debuffType)
    {
        switch (debuffType)
        {
            case DebuffType.Vulnerable: return "破甲";
            case DebuffType.Weak: return "Weak";
            case DebuffType.Slow: return "减速";
            case DebuffType.ArmorBreak: return "护甲破坏";
            case DebuffType.EnergyDrain: return "能量流失";
            default: return debuffType.ToString();
        }
    }

    private static string GetEquipmentSlotName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return "武器";
            case EquipmentSlot.Armor: return "护甲";
            case EquipmentSlot.Accessory: return "饰品";
            default: return slot.ToString();
        }
    }

    private static string GetCurseTriggerName(CurseTriggerType trigger)
    {
        switch (trigger)
        {
            case CurseTriggerType.SelfDamage: return "自伤";
            case CurseTriggerType.HandSizeReduction: return "手牌上限减少";
            case CurseTriggerType.DrawReduction: return "抽牌惩罚";
            case CurseTriggerType.EnergyDrain: return "能量流失";
            default: return trigger.ToString();
        }
    }
}
