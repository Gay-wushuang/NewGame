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
    public List<CardInstance> Deck = new List<CardInstance>();
    public List<CardInstance> DrawPile = new List<CardInstance>();
    public List<CardInstance> DiscardPile = new List<CardInstance>();
    public List<CardInstance> ExhaustPile = new List<CardInstance>();
    public int MaxHandSizeBase = 10;
    public int CurseHandSizeModifier = 0;
    public int EffectiveMaxHandSize => Mathf.Max(1, MaxHandSizeBase + CurseHandSizeModifier);
    
    public int Shield = 0;
    public int NextTurnEnergyBonus = 0;
    public int EquippedWeaponBonus = 0;
    
    public List<DiceInstance> DicePool = new List<DiceInstance>();
    public DiceRoller DiceRoller;
    
    public Dictionary<StatusType, StatusInstance> Statuses = new Dictionary<StatusType, StatusInstance>();
    public Dictionary<EquipmentSlot, CardData> Equipment = new Dictionary<EquipmentSlot, CardData>()
    {
        { EquipmentSlot.Weapon, null },
        { EquipmentSlot.Armor, null },
        { EquipmentSlot.Accessory, null }
    };
    
    public PlayerState(DiceRoller roller = null)
    {
        DiceRoller = roller ?? new DiceRoller();
    }
    
    public void TakeDamage(int damage)
    {
        int totalDamage = damage;
        
        int shieldDamage = Mathf.Min(totalDamage, Shield);
        Shield -= shieldDamage;
        totalDamage -= shieldDamage;
        
        int energyDamage = Mathf.Min(totalDamage, Energy);
        Energy -= energyDamage;
        int hpDamage = totalDamage - energyDamage;
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
    
    public void ConsumeEnergy(int amount)
    {
        Energy -= amount;
    }
    
    public void PlayCard(CardInstance card)
    {
        Hand.Remove(card);
    }
    
    public void InitDrawPileFromDeck()
    {
        DrawPile.Clear();
        DiscardPile.Clear();
        ExhaustPile.Clear();
        foreach (var card in Deck)
            DrawPile.Add(new CardInstance(card.Data));
        ShuffleDrawPile();
    }
    
    public void ShuffleDrawPile()
    {
        var rng = new System.Random();
        for (int i = DrawPile.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (DrawPile[i], DrawPile[j]) = (DrawPile[j], DrawPile[i]);
        }
    }
    
    public void ShuffleDiscardIntoDraw()
    {
        foreach (var card in DiscardPile)
            DrawPile.Add(card);
        DiscardPile.Clear();
        ShuffleDrawPile();
    }
    
    public int DrawCards(int count)
    {
        int drawn = 0;
        for (int i = 0; i < count; i++)
        {
            if (Hand.Count >= EffectiveMaxHandSize)
                break;

            if (DrawPile.Count == 0)
            {
                if (DiscardPile.Count == 0)
                    break;
                ShuffleDiscardIntoDraw();
            }

            var card = DrawPile[DrawPile.Count - 1];
            DrawPile.RemoveAt(DrawPile.Count - 1);
            Hand.Add(card);
            drawn++;
        }
        return drawn;
    }
    
    public void DiscardHand()
    {
        foreach (var card in Hand)
            DiscardPile.Add(card);
        Hand.Clear();
    }
    
    public void MoveToDiscard(CardInstance card)
    {
        Hand.Remove(card);
        DiscardPile.Add(card);
    }
    
    public void MoveToExhaust(CardInstance card)
    {
        Hand.Remove(card);
        ExhaustPile.Add(card);
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
                dice.RollAndConsume(DiceRoller);
                return dice;
            }
        }
        
        return null;
    }
    
    public bool IsAlive()
    {
        return Hp > 0;
    }
    
    public int GetStatusStacks(StatusType type)
    {
        return Statuses.TryGetValue(type, out StatusInstance status) ? status.Stacks : 0;
    }
    
    public void AddStatus(StatusType type, int stacks, int duration = 0)
    {
        if (!Statuses.ContainsKey(type))
        {
            Statuses[type] = new StatusInstance(type, stacks, duration);
        }
        else
        {
            Statuses[type].Stacks += stacks;
            if (duration > 0)
            {
                Statuses[type].Duration = duration;
            }
        }
    }
    
    public void ReduceStatus(StatusType type, int stacks)
    {
        if (Statuses.ContainsKey(type))
        {
            Statuses[type].Stacks = Mathf.Max(0, Statuses[type].Stacks - stacks);
            if (Statuses[type].Stacks == 0)
            {
                Statuses.Remove(type);
            }
        }
    }
    
    public int GetAttackUpStacks()
    {
        return GetStatusStacks(StatusType.AttackUp);
    }
    
    public void AddAttackUp(int stacks)
    {
        AddStatus(StatusType.AttackUp, stacks, 2);
    }
    
    public int GetDefenseUpStacks()
    {
        return GetStatusStacks(StatusType.DefenseUp);
    }
    
    public void AddDefenseUp(int stacks)
    {
        AddStatus(StatusType.DefenseUp, stacks, 2);
    }
    
    public int GetDiceBonus()
    {
        return GetStatusStacks(StatusType.DiceBonus);
    }
    
    public void AddDiceBonus(int stacks)
    {
        AddStatus(StatusType.DiceBonus, stacks, 1);
    }
    
    public void EquipCard(CardData card)
    {
        if (card.EquipSlot.HasValue)
        {
            Equipment[card.EquipSlot.Value] = card;
        }
    }
    
    public void UnequipSlot(EquipmentSlot slot)
    {
        Equipment[slot] = null;
    }
    
    public void EndTurn()
    {
        List<StatusType> expiredStatuses = new List<StatusType>();
        
        foreach (var pair in Statuses)
        {
            if (pair.Value.Duration > 0)
            {
                pair.Value.Duration--;
                if (pair.Value.Duration <= 0)
                {
                    expiredStatuses.Add(pair.Key);
                }
            }
        }
        
        foreach (var statusType in expiredStatuses)
        {
            if (Statuses.ContainsKey(statusType) && Statuses[statusType].Duration <= 0)
            {
                Statuses.Remove(statusType);
            }
        }
    }
}