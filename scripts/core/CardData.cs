using System;
using System.Collections.Generic;
using Godot;

public enum CardType
{
    Attack,
    Skill,
    Power
}

public enum CardCategory
{
    Basic,
    Skill,
    Consumable,
    Other
}

public enum CardSubtype
{
    Attack,
    Defense,
    PositiveBuff,
    NegativeBuff,
    GameLevelConsumable,
    BattleLevelConsumable,
    Equipment,
    Curse
}

public enum TargetType
{
    Enemy,
    Player,
    AllEnemies
}

public enum BuffType
{
    AttackUp,
    DefenseUp,
    EnergyRegen,
    DiceBonus,
    CriticalRateUp
}

public enum DebuffType
{
    Vulnerable,
    Weak,
    Slow,
    ArmorBreak,
    EnergyDrain
}

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory
}

public enum StackBehavior
{
    RefreshDuration,
    AddStacks,
    Replace
}

public enum CurseType
{
    HandSizeReduction,
    DrawPenalty,
    EnergyDrain,
    DamagePenalty
}

public class CardData
{
    public string Id;
    public string Name;
    public string Description = "";
    public string EffectExplanation = "";
    public CardType Type;
    public CardCategory Category;
    public CardSubtype Subtype;
    public TargetType Target;
    public int EnergyCost;
    public int DiceCost;
    public string DiceType;
    public Func<CardInstance, DiceInstance, int> DamageFormula;
    public Action<CardInstance, DiceInstance, EnemyState> ApplyEffect;
    public Func<CardInstance, DiceInstance, int, int> ModifyDamage;

    public int ShieldValue;
    public float EvasionRate;
    public int CounterDamage;

    public BuffType? AppliedBuffType;
    public DebuffType? AppliedDebuffType;
    public int EffectAmount;
    public int Duration;
    public int StackLimit;
    public StackBehavior StackRule;
    public float ResistChance;
    public int ConditionalDiceThreshold;
    public string ConditionalEffectSummary = "";

    public int MaxUsage;
    public int UsesPerBattle;

    public EquipmentSlot? EquipSlot;
    public bool IsPermanent;

    public CurseType? AppliedCurseType;
    public string RemovalCondition;
    public bool IsRemovedOnDiscard;

    public CurseDurationType CurseDuration = CurseDurationType.Permanent;
    public CurseTriggerType CurseTrigger = CurseTriggerType.SelfDamage;
    public int CurseEffectAmount = 1;
    public int CurseStrengthenAmount = 1;
    public float CurseDisappearChance = 0.15f;
    public float CurseNothingChance = 0.70f;
    public float CurseStrengthenChance = 0.15f;

    public string VisualKey;
    public string BorderColor;

    public Dictionary<string, string> MetaData = new Dictionary<string, string>();

    public void ValidateCurseChances()
    {
        float total = CurseDisappearChance + CurseNothingChance + CurseStrengthenChance;
        if (Mathf.Abs(total - 1.0f) > 0.001f)
            GD.PushWarning($"Curse card {Name} chance total is not 1.0: {total}");
    }

    public static CardData EnergyStrike = new CardData()
    {
        Id = "energy_strike",
        Name = "EnergyStrike",
        Description = "Deal die + 2 damage.",
        EffectExplanation = "",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 1,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 2,
        VisualKey = "attack_sword",
        BorderColor = "#FF4444"
    };

    public static CardData BreakCore = new CardData()
    {
        Id = "break_core",
        Name = "BreakCore",
        Description = "Deal 8 damage. Dice 5+: apply 2 Vulnerable.",
        EffectExplanation = "Vulnerable: attack damage taken +1 per stack, reduced by 1 each enemy turn.",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 3,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => 8,
        AppliedDebuffType = DebuffType.Vulnerable,
        EffectAmount = 2,
        ConditionalDiceThreshold = 5,
        ConditionalEffectSummary = "Apply 2 Vulnerable",
        ApplyEffect = (card, dice, enemy) =>
        {
            if (dice.Value.GetValueOrDefault() >= 5)
            {
                enemy.AddVulnerable(2);
            }
        },
        VisualKey = "attack_axe",
        BorderColor = "#FF4444"
    };

    public static CardData QuickStrike = new CardData()
    {
        Id = "quick_strike",
        Name = "QuickStrike",
        Description = "Deal 4 damage.",
        EffectExplanation = "",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 0,
        DiceCost = 0,
        DiceType = "Any",
        DamageFormula = (card, dice) => 4,
        VisualKey = "attack_fist",
        BorderColor = "#FF4444"
    };

    public static CardData VulnerableStrike = new CardData()
    {
        Id = "vulnerable_strike",
        Name = "VulnerableStrike",
        Description = "Deal die + 1 damage. Dice 3+: apply 1 Vulnerable.",
        EffectExplanation = "Vulnerable: attack damage taken +1 per stack, reduced by 1 each enemy turn.",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 2,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 1,
        AppliedDebuffType = DebuffType.Vulnerable,
        EffectAmount = 1,
        ConditionalDiceThreshold = 3,
        ConditionalEffectSummary = "Apply 1 Vulnerable",
        ApplyEffect = (card, dice, enemy) =>
        {
            if (dice.Value.GetValueOrDefault() >= 3)
            {
                enemy.AddVulnerable(1);
            }
        },
        VisualKey = "attack_pierce",
        BorderColor = "#FF4444"
    };

    public static CardData CriticalHit = new CardData()
    {
        Id = "critical_hit",
        Name = "CriticalHit",
        Description = "Deal die + 3 damage. Dice 4+: double damage.",
        EffectExplanation = "",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 2,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 3,
        ConditionalDiceThreshold = 4,
        ConditionalEffectSummary = "Double damage",
        ModifyDamage = (card, dice, baseDamage) =>
        {
            if (dice.Value.GetValueOrDefault() >= 4)
            {
                return baseDamage * 2;
            }
            return baseDamage;
        },
        VisualKey = "attack_critical",
        BorderColor = "#FF4444"
    };

    public static CardData HeavyStrike = new CardData()
    {
        Id = "heavy_strike",
        Name = "HeavyStrike",
        Description = "Deal die + 4 damage.",
        EffectExplanation = "",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 2,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 4,
        VisualKey = "attack_hammer",
        BorderColor = "#FF4444"
    };

    public static CardData EnergyBarrier = new CardData()
    {
        Id = "energy_barrier",
        Name = "EnergyBarrier",
        Description = "Gain 5 shield.",
        EffectExplanation = "Shield absorbs damage before Energy.",
        Type = CardType.Skill,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Defense,
        Target = TargetType.Player,
        EnergyCost = 2,
        DiceCost = 0,
        ShieldValue = 5,
        Duration = 2,
        VisualKey = "defense_shield",
        BorderColor = "#4444FF"
    };

    public static CardData Adrenaline = new CardData()
    {
        Id = "adrenaline",
        Name = "Adrenaline",
        Description = "Restore extra Energy next turn.",
        EffectExplanation = "Energy Regen restores extra Energy at the start of next turn.",
        Type = CardType.Skill,
        Category = CardCategory.Skill,
        Subtype = CardSubtype.PositiveBuff,
        Target = TargetType.Player,
        EnergyCost = 1,
        DiceCost = 0,
        AppliedBuffType = BuffType.EnergyRegen,
        EffectAmount = 3,
        Duration = 2,
        VisualKey = "buff_energy",
        BorderColor = "#44FF44"
    };

    public static CardData WeakPulse = new CardData()
    {
        Id = "weak_pulse",
        Name = "WeakPulse",
        Description = "Deal 3 damage. Apply 2 Weak.",
        EffectExplanation = "Weak reduces enemy attack damage by its stack count.",
        Type = CardType.Skill,
        Category = CardCategory.Skill,
        Subtype = CardSubtype.NegativeBuff,
        Target = TargetType.Enemy,
        EnergyCost = 2,
        DiceCost = 1,
        DamageFormula = (card, dice) => 3,
        AppliedDebuffType = DebuffType.Weak,
        EffectAmount = 2,
        Duration = 2,
        VisualKey = "debuff_weak",
        BorderColor = "#AA44FF"
    };

    public static CardData EnergyPotion = new CardData()
    {
        Id = "energy_potion",
        Name = "EnergyPotion",
        Description = "Restore 5 Energy.",
        EffectExplanation = "Consumable with limited uses per battle.",
        Type = CardType.Skill,
        Category = CardCategory.Consumable,
        Subtype = CardSubtype.BattleLevelConsumable,
        Target = TargetType.Player,
        EnergyCost = 0,
        DiceCost = 0,
        UsesPerBattle = 2,
        EffectAmount = 5,
        VisualKey = "consumable_potion",
        BorderColor = "#FF8800"
    };

    public static CardData IronSword = new CardData()
    {
        Id = "iron_sword",
        Name = "IronSword",
        Description = "Equip Weapon.",
        EffectExplanation = "Weapon grants attack damage +2.",
        Type = CardType.Power,
        Category = CardCategory.Other,
        Subtype = CardSubtype.Equipment,
        Target = TargetType.Player,
        EnergyCost = 3,
        DiceCost = 0,
        EquipSlot = EquipmentSlot.Weapon,
        EffectAmount = 2,
        Duration = 3,
        IsPermanent = false,
        VisualKey = "equip_sword",
        BorderColor = "#CCCCCC"
    };

    public static CardData Clumsy = new CardData()
    {
        Id = "clumsy",
        Name = "Clumsy",
        Description = "Hand size -1.",
        EffectExplanation = "Curse: may disappear, stay, or strengthen when played.",
        Type = CardType.Power,
        Category = CardCategory.Other,
        Subtype = CardSubtype.Curse,
        Target = TargetType.Player,
        EnergyCost = 0,
        DiceCost = 0,
        CurseDuration = CurseDurationType.Permanent,
        CurseTrigger = CurseTriggerType.HandSizeReduction,
        CurseEffectAmount = 1,
        CurseStrengthenAmount = 1,
        CurseDisappearChance = 0.15f,
        CurseNothingChance = 0.70f,
        CurseStrengthenChance = 0.15f,
        VisualKey = "curse_clumsy",
        BorderColor = "#8A2BE2"
    };

    public static CardData Wound = new CardData()
    {
        Id = "wound",
        Name = "Wound",
        Description = "Lose 2 HP each turn.",
        EffectExplanation = "Temporary curse removed after battle.",
        Type = CardType.Power,
        Category = CardCategory.Other,
        Subtype = CardSubtype.Curse,
        Target = TargetType.Player,
        EnergyCost = 0,
        DiceCost = 0,
        CurseDuration = CurseDurationType.Temporary,
        CurseTrigger = CurseTriggerType.SelfDamage,
        CurseEffectAmount = 2,
        CurseStrengthenAmount = 1,
        CurseDisappearChance = 0.15f,
        CurseNothingChance = 0.70f,
        CurseStrengthenChance = 0.15f,
        VisualKey = "curse_wound",
        BorderColor = "#8B0000"
    };

    public void GetDamageRange(int diceSides, out int min, out int max)
    {
        if (DamageFormula == null)
        {
            min = 0;
            max = 0;
            return;
        }

        var tempCard = new CardInstance(this);

        if (DiceCost == 0)
        {
            min = DamageFormula(tempCard, null);
            max = min;
            return;
        }

        var minDice = new DiceInstance(diceSides);
        minDice.Value = 1;
        var maxDice = new DiceInstance(diceSides);
        maxDice.Value = diceSides;

        min = DamageFormula(tempCard, minDice);
        max = DamageFormula(tempCard, maxDice);

        if (ModifyDamage != null)
        {
            min = ModifyDamage(tempCard, minDice, min);
            max = ModifyDamage(tempCard, maxDice, max);
        }
    }
}
