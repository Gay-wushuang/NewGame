using System;

public enum CardType
{
    Attack,
    Skill,
    Power
}

public enum TargetType
{
    Enemy,
    Player,
    AllEnemies
}

public class CardData
{
    public string Id;
    public string Name;
    public string Description;
    public CardType Type;
    public TargetType Target;
    public int EnergyCost;
    public int DiceCost;
    public string DiceType;
    public Func<CardInstance, DiceInstance, int> DamageFormula;
    
    public static CardData EnergyStrike = new CardData()
    {
        Id = "energy_strike",
        Name = "EnergyStrike",
        Description = "消耗 1 Energy 和 1 枚默认骰；打出时掷骰，造成 骰点 + 2 伤害",
        Type = CardType.Attack,
        Target = TargetType.Enemy,
        EnergyCost = 1,
        DiceCost = 1,
        DiceType = "Any",
        DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 2
    };
    
    public void GetDamageRange(int diceSides, out int min, out int max)
    {
        if (DamageFormula == null || DiceCost == 0)
        {
            min = 0;
            max = 0;
            return;
        }
        
        var minDice = new DiceInstance(diceSides);
        minDice.Value = 1;
        var maxDice = new DiceInstance(diceSides);
        maxDice.Value = diceSides;
        
        var tempCard = new CardInstance(this);
        min = DamageFormula(tempCard, minDice);
        max = DamageFormula(tempCard, maxDice);
    }
}
