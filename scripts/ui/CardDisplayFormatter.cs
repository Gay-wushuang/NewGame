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
        return FormatCardStatLine(data, card, 6, null, contextual);
    }

    public static string FormatCardStatLine(CardData data, CardInstance card, int diceSides)
    {
        return FormatCardStatLine(data, card, diceSides, null, false);
    }

    public static string FormatCardStatLine(CardData data, CardInstance card, int diceSides, EnemyState enemy, bool contextual)
    {
        if (data.DamageFormula != null)
        {
            GetDamageRange(data, diceSides, enemy, contextual, out int minDamage, out int maxDamage);
            return minDamage == maxDamage ? $"DMG {minDamage}" : $"DMG {minDamage}~{maxDamage}";
        }

        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
            return $"Shield {data.ShieldValue}";

        if (data.Subtype == CardSubtype.Curse)
            return card.CurseStacks > 1 ? $"Stacks {card.CurseStacks}" : "Curse";

        if (data.EffectAmount > 0)
            return $"Effect {data.EffectAmount}";

        return "Effect";
    }

    public static string FormatCost(CardData data)
    {
        var costs = new List<string>();

        if (data.EnergyCost > 0)
            costs.Add($"{data.EnergyCost} Energy");

        if (data.DiceCost > 0)
            costs.Add($"{data.DiceCost} Dice");

        return costs.Count == 0 ? "No cost" : string.Join(", ", costs);
    }

    public static string FormatCardFaceRuleText(CardInstance card, int diceSides, EnemyState enemy, bool contextual)
    {
        var parts = new List<string>();
        CardData data = card.Data;

        if (data.DamageFormula != null)
        {
            GetDamageRange(data, diceSides, enemy, contextual, out int minDamage, out int maxDamage);
            string label = contextual ? "Current damage" : "Base damage";
            parts.Add(minDamage == maxDamage ? $"{label}: {minDamage}" : $"{label}: {minDamage}~{maxDamage}");
        }

        string condition = FormatConditionalEffect(data);
        if (!string.IsNullOrEmpty(condition))
            parts.Add(condition);

        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
            parts.Add($"Shield: {data.ShieldValue}");

        if (data.Subtype == CardSubtype.Curse)
            parts.Add(FormatRuleText(data, card, diceSides));

        return parts.Count > 0 ? string.Join("\n", parts) : "Effect";
    }

    public static string FormatPreviewRuleText(CardInstance card, int diceSides, EnemyState enemy)
    {
        var parts = new List<string>();
        CardData data = card.Data;

        if (data.DamageFormula != null)
        {
            GetDamageRange(data, diceSides, null, false, out int baseMin, out int baseMax);
            GetDamageRange(data, diceSides, enemy, true, out int currentMin, out int currentMax);

            parts.Add(baseMin == baseMax ? $"Base damage: {baseMin}" : $"Base damage: {baseMin}~{baseMax}");
            if (currentMin != baseMin || currentMax != baseMax)
            {
                parts.Add(currentMin == currentMax
                    ? $"Current estimate: {currentMin}"
                    : $"Current estimate: {currentMin}~{currentMax}");
            }
        }

        string condition = FormatConditionalEffect(data);
        if (!string.IsNullOrEmpty(condition))
            parts.Add(condition);

        string ruleText = FormatRuleText(data, card, diceSides);
        if (parts.Count == 0 && !string.IsNullOrEmpty(ruleText))
            parts.Add(ruleText);

        return parts.Count > 0 ? string.Join("\n", parts) : "No extra effect.";
    }

    public static string FormatRuleText(CardData data, CardInstance card, int diceSides)
    {
        var parts = new List<string>();

        if (data.DamageFormula != null)
            parts.Add(FormatDamageEffect(data, diceSides));

        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
            parts.Add($"Gain {data.ShieldValue} shield.");

        if (data.Subtype == CardSubtype.PositiveBuff && data.AppliedBuffType.HasValue)
            parts.Add(FormatBuffEffect(data));

        if (data.Subtype == CardSubtype.NegativeBuff && data.AppliedDebuffType.HasValue)
            parts.Add(FormatDebuffEffect(data));

        if (data.Subtype == CardSubtype.BattleLevelConsumable)
            parts.Add(FormatBattleConsumableEffect(data));

        if (data.Subtype == CardSubtype.GameLevelConsumable)
            parts.Add(FormatGameConsumableEffect(data));

        if (data.Subtype == CardSubtype.Equipment && data.EquipSlot.HasValue)
            parts.Add(FormatEquipmentEffect(data));

        if (data.Subtype == CardSubtype.Curse)
            parts.Add(FormatCurseEffect(data, card));

        if (data.AppliedDebuffType.HasValue &&
            data.Subtype != CardSubtype.NegativeBuff &&
            data.Subtype != CardSubtype.Curse)
        {
            parts.Add(FormatConditionalEffect(data));
        }

        return string.Join("\n", parts);
    }

    public static string FormatKeywordText(CardData data)
    {
        var parts = new List<string>();

        if (data.ShieldValue > 0 || data.Subtype == CardSubtype.Defense)
            parts.Add("Shield: absorbs damage before Energy. Remaining shield clears at the start of the next player turn.");

        if (data.AppliedDebuffType == DebuffType.Vulnerable)
            parts.Add("Vulnerable: each stack increases attack damage taken by 1. Enemy loses 1 stack at end of its turn.");

        if (data.AppliedDebuffType == DebuffType.Weak)
            parts.Add("Weak: reduces enemy attack damage by its stack count.");

        if (data.AppliedBuffType == BuffType.EnergyRegen)
            parts.Add("Energy Regen: restores extra Energy at the start of the next turn.");

        if (data.Subtype == CardSubtype.BattleLevelConsumable)
            parts.Add($"Consumable: usable {data.UsesPerBattle} time(s) per battle.");

        if (data.Subtype == CardSubtype.GameLevelConsumable)
            parts.Add("Consumable: exhausted after use.");

        if (data.Subtype == CardSubtype.Equipment)
            parts.Add("Equipment: grants a lasting bonus for its configured duration.");

        if (data.Subtype == CardSubtype.Curse)
        {
            string duration = data.CurseDuration == CurseDurationType.Temporary ? "temporary" : "permanent";
            parts.Add($"Curse ({duration}): may disappear, strengthen, or stay after being played.");
        }

        return parts.Count > 0 ? string.Join("\n\n", parts) : "";
    }

    public static string FormatConditionalEffect(CardData data)
    {
        if (data.ConditionalDiceThreshold <= 0)
            return "";

        string effect = string.IsNullOrEmpty(data.ConditionalEffectSummary)
            ? "Trigger extra effect"
            : data.ConditionalEffectSummary;
        return $"Dice {data.ConditionalDiceThreshold}+: {effect}.";
    }

    private static void GetDamageRange(CardData data, int diceSides, EnemyState enemy, bool contextual, out int minDamage, out int maxDamage)
    {
        data.GetDamageRange(diceSides, out minDamage, out maxDamage);

        if (!contextual || data.DamageFormula == null || enemy == null || data.Subtype != CardSubtype.Attack)
            return;

        int vulnerable = enemy.GetVulnerableStacks();
        if (vulnerable <= 0)
            return;

        minDamage += vulnerable;
        maxDamage += vulnerable;
    }

    private static string FormatDamageEffect(CardData data, int diceSides)
    {
        data.GetDamageRange(diceSides, out int minDamage, out int maxDamage);

        if (data.DiceCost == 0 || minDamage == maxDamage)
            return $"Deal {minDamage} damage.";

        return $"Deal die + {minDamage - 1} damage. With d{diceSides}: {minDamage}~{maxDamage}.";
    }

    private static string FormatBuffEffect(CardData data)
    {
        string duration = data.Duration > 0 ? $" for {data.Duration} turn(s)" : "";
        return $"Gain {GetBuffName(data.AppliedBuffType.Value)} {data.EffectAmount}{duration}.";
    }

    private static string FormatDebuffEffect(CardData data)
    {
        string duration = data.Duration > 0 ? $" for {data.Duration} turn(s)" : "";
        return $"Apply {data.EffectAmount} {GetDebuffName(data.AppliedDebuffType.Value)}{duration}.";
    }

    private static string FormatBattleConsumableEffect(CardData data)
    {
        return $"Restore {data.EffectAmount} Energy. Uses per battle: {data.UsesPerBattle}.";
    }

    private static string FormatGameConsumableEffect(CardData data)
    {
        return $"Restore {data.EffectAmount} HP.";
    }

    private static string FormatEquipmentEffect(CardData data)
    {
        return $"Equip {GetEquipmentSlotName(data.EquipSlot.Value)}. Attack damage +{data.EffectAmount} for {data.Duration} battle(s).";
    }

    private static string FormatCurseEffect(CardData data, CardInstance card)
    {
        string stacks = card.CurseStacks > 1 ? $" ({card.CurseStacks} stacks)" : "";

        switch (data.CurseTrigger)
        {
            case CurseTriggerType.SelfDamage:
                return $"Lose {data.CurseEffectAmount} HP each turn{stacks}.";
            case CurseTriggerType.HandSizeReduction:
                return $"Hand size -{data.CurseEffectAmount}{stacks}.";
            case CurseTriggerType.DrawReduction:
                return $"Draw {data.CurseEffectAmount} fewer card(s) each turn{stacks}.";
            case CurseTriggerType.EnergyDrain:
                return $"Lose {data.CurseEffectAmount} Energy each turn{stacks}.";
            default:
                return $"Trigger {data.CurseTrigger}{stacks}.";
        }
    }

    private static string GetBuffName(BuffType buffType)
    {
        switch (buffType)
        {
            case BuffType.AttackUp:
                return "Attack Up";
            case BuffType.DefenseUp:
                return "Defense Up";
            case BuffType.EnergyRegen:
                return "Energy Regen";
            case BuffType.DiceBonus:
                return "Dice Bonus";
            case BuffType.CriticalRateUp:
                return "Critical Rate Up";
            default:
                return buffType.ToString();
        }
    }

    private static string GetDebuffName(DebuffType debuffType)
    {
        switch (debuffType)
        {
            case DebuffType.Vulnerable:
                return "Vulnerable";
            case DebuffType.Weak:
                return "Weak";
            case DebuffType.Slow:
                return "Slow";
            case DebuffType.ArmorBreak:
                return "Armor Break";
            case DebuffType.EnergyDrain:
                return "Energy Drain";
            default:
                return debuffType.ToString();
        }
    }

    private static string GetEquipmentSlotName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                return "Weapon";
            case EquipmentSlot.Armor:
                return "Armor";
            case EquipmentSlot.Accessory:
                return "Accessory";
            default:
                return slot.ToString();
        }
    }
}
