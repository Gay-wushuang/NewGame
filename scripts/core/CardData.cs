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
    public string Description;
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
            GD.PushWarning($"诅咒卡 {Name} 的概率之和不为 1.0: {total}");
    }
    
    public static CardData EnergyStrike = new CardData()
    {
        Id = "energy_strike",
        Name = "EnergyStrike",
        Description = "消耗 1 Energy 和 1 枚默认骰；打出时掷骰，造成 骰点 + 2 伤害",
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
        Description = "消耗 3 Energy 和 1 枚默认骰；造成 8 伤害；骰点 >= 5 时施加 2 层破甲",
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
        Description = "无需消耗；造成 4 点固定伤害",
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
        Description = "消耗 2 Energy 和 1 枚默认骰；造成 骰点 + 1 伤害；骰点 >= 3 时施加 1 层破甲",
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
        Description = "消耗 2 Energy 和 1 枚默认骰；造成 骰点 + 3 伤害；骰点 >= 4 时伤害翻倍",
        Type = CardType.Attack,
        Category = CardCategory.Basic,
        Subtype = CardSubtype.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 2,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 3,
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
        Description = "消耗 2 Energy 和 1 枚默认骰；造成 骰点 + 4 伤害",
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
        Description = "消耗 2 Energy；获得 5 点护盾，持续 2 回合",
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
        Description = "消耗 1 Energy；下回合开始恢复 3 Energy，持续 2 回合",
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
        Description = "消耗 2 Energy 和 1 枚默认骰；造成 3 伤害；施加 2 层 Weak，持续 2 回合",
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
        Description = "恢复 5 Energy；每场战斗限用 2 次",
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
        Description = "消耗 3 Energy；装备武器，攻击伤害 +2，持续 3 场战斗",
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
        Description = "永久诅咒：手牌上限 -1；每回合开始时触发；打出后有概率消失/强化",
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
        Description = "临时诅咒：受到过量伤害的印记；每回合开始时失去 2 HP；战斗结束后消失；打出后有概率消失/强化",
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
            max = ModifyDamage(tempCard, maxDice, max);
        }
    }
}