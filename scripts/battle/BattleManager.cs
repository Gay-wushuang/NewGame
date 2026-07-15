using Godot;
using System.Collections.Generic;

public partial class BattleManager : Node
{
    public PlayerState Player;
    public EnemyState Enemy;
    public int Turn = 1;
    public bool IsPlayerTurn = true;
    
    public override void _Ready()
    {
        InitializeBattle();
    }
    
    public void InitializeBattle()
    {
        Player = new PlayerState();
        Enemy = new EnemyState("TrainingBeast", 20);
        Enemy.CurrentIntent.SetAttack(14);
        
        DrawInitialHand();
        
        StartPlayerTurn();
    }
    
    private void DrawInitialHand()
    {
        for (int i = 0; i < 3; i++)
        {
            Player.Hand.Add(new CardInstance(CardData.EnergyStrike));
        }
    }
    
    public void StartPlayerTurn()
    {
        IsPlayerTurn = true;
        Player.RestoreEnergy(Player.MaxEnergy);
        Player.RefreshDicePool();
        
        while (Player.Hand.Count < 3)
        {
            Player.Hand.Add(new CardInstance(CardData.EnergyStrike));
        }
        
        EmitSignal(SignalName.PlayerTurnStarted, Turn);
    }
    
    public bool TryPlayCard(CardInstance card)
    {
        if (!IsPlayerTurn)
            return false;
        
        if (!Player.CanPlayCard(card))
            return false;
        
        DiceInstance consumedDice = null;
        if (card.Data.DiceCost > 0)
        {
            consumedDice = Player.ConsumeNextDice();
            if (consumedDice == null)
                return false;
        }
        
        if (card.Data.DiceCost > 0 && consumedDice == null)
            return false;
        
        int damage = card.CalculateDamage(consumedDice);
        Enemy.TakeDamage(damage);
        
        Player.PlayCard(card);
        
        int diceResult = consumedDice?.Value ?? 0;
        EmitSignal(SignalName.CardPlayed, card.Data.Id, damage, diceResult);
        
        if (!Enemy.IsAlive())
        {
            EmitSignal(SignalName.BattleWon);
            return true;
        }
        
        return true;
    }
    
    public void EndPlayerTurn()
    {
        IsPlayerTurn = false;
        EmitSignal(SignalName.PlayerTurnEnded);
        
        CallDeferred(nameof(ExecuteEnemyTurn));
    }
    
    public void ExecuteEnemyTurn()
    {
        if (!Enemy.IsAlive())
            return;
        
        int damage = Enemy.CurrentIntent.Value;
        int energyBefore = Player.Energy;
        int hpBefore = Player.Hp;
        Player.TakeDamage(damage);
        
        EmitSignal(SignalName.EnemyAttacked, damage, energyBefore, Player.Energy, hpBefore, Player.Hp);
        
        if (!Player.IsAlive())
        {
            EmitSignal(SignalName.BattleLost);
            return;
        }
        
        Turn++;
        StartPlayerTurn();
    }
    
    public void SkipTurn()
    {
        if (IsPlayerTurn)
        {
            EndPlayerTurn();
        }
    }
    
    [Signal] public delegate void PlayerTurnStartedEventHandler(int turn);
    [Signal] public delegate void PlayerTurnEndedEventHandler();
    [Signal] public delegate void CardPlayedEventHandler(string cardId, int damage, int diceResult);
    [Signal] public delegate void EnemyAttackedEventHandler(int damage, int energyBefore, int energyAfter, int hpBefore, int hpAfter);
    [Signal] public delegate void BattleWonEventHandler();
    [Signal] public delegate void BattleLostEventHandler();
}
