using Godot;
using System.Collections.Generic;

public class PlayerState
{
    public int MaxHp = 30;
    public int Hp = 30;
    
    public int MaxEnergy = 12;
    public int Energy = 12;
    
    public int DiceCount = 2;
    public int DiceSides = 6;
    
    public List<CardInstance> Hand = new List<CardInstance>();
    public List<DiceInstance> DicePool = new List<DiceInstance>();
    
    public void TakeDamage(int damage)
    {
        int energyDamage = Mathf.Min(damage, Energy);
        Energy -= energyDamage;
        int hpDamage = damage - energyDamage;
        Hp -= hpDamage;
    }
    
    public void RestoreEnergy(int amount)
    {
        Energy = Mathf.Min(Energy + amount, MaxEnergy);
    }
    
    public void RefreshDicePool()
    {
        DicePool.Clear();
        for (int i = 0; i < DiceCount; i++)
        {
            DicePool.Add(new DiceInstance(DiceSides));
        }
    }
    
    public bool CanPlayCard(CardInstance card)
    {
        return Energy >= card.Data.EnergyCost && AvailableDiceCount() >= card.Data.DiceCost;
    }
    
    public void PlayCard(CardInstance card)
    {
        Energy -= card.Data.EnergyCost;
        Hand.Remove(card);
    }
    
    public int AvailableDiceCount()
    {
        int count = 0;
        foreach (var dice in DicePool)
        {
            if (!dice.IsConsumed)
                count++;
        }
        return count;
    }
    
    public DiceInstance ConsumeNextDice()
    {
        foreach (var dice in DicePool)
        {
            if (!dice.IsConsumed)
            {
                dice.RollAndConsume();
                return dice;
            }
        }
        
        return null;
    }
    
    public bool IsAlive()
    {
        return Hp > 0;
    }
}
