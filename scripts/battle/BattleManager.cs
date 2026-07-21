using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BattleManager : Node
{
    public PlayerState Player;
    public EnemyState Enemy;
    public int Turn = 1;
    public bool IsPlayerTurn = true;
    public bool IsBattleActive { get; private set; } = true;
    public DiceRoller DiceRoller;

    public override void _Ready()
    {
        DiceRoller = new DiceRoller();
        if (Player == null || Enemy == null)
        {
            InitializeBattle();
        }
    }

    public void InitializeBattle()
    {
        var player = new PlayerState(DiceRoller);
        var enemy = new EnemyState("TrainingBeast", 20);
        enemy.CurrentIntent.SetAttack(14);

        InitializeBattle(player, enemy, DiceRoller);
    }

    public void InitializeBattle(PlayerState player, EnemyState enemy, DiceRoller diceRoller = null)
    {
        DiceRoller = diceRoller ?? DiceRoller ?? new DiceRoller();
        Player = player;
        Enemy = enemy;
        Turn = 1;
        IsPlayerTurn = true;
        IsBattleActive = true;
        Player.DiceRoller = DiceRoller;
        EnsureDefaultDeck();
        CleanupTemporaryCurses();
        Player.Hand.Clear();
        Player.DicePool.Clear();
        Player.DrawPile.Clear();
        Player.DiscardPile.Clear();
        Player.ExhaustPile.Clear();

        Player.InitDrawPileFromDeck();
        StartPlayerTurn();
    }

    private void EnsureDefaultDeck()
    {
        if (Player.Deck.Count > 0)
            return;

        var cardPool = new List<CardData> {
            CardData.EnergyStrike, CardData.BreakCore,
            CardData.QuickStrike, CardData.VulnerableStrike, CardData.CriticalHit,
            CardData.HeavyStrike, CardData.EnergyBarrier,
            CardData.Adrenaline, CardData.WeakPulse,
            CardData.EnergyPotion, CardData.IronSword
        };
        foreach (var cardData in cardPool)
        {
            Player.Deck.Add(new CardInstance(cardData));
        }
    }

    public void StartPlayerTurn()
    {
        IsPlayerTurn = true;

        if (Player.Shield > 0)
        {
            EmitSignal(SignalName.BattleLog, $"Shield cleared: {Player.Shield}");
            Player.Shield = 0;
        }

        if (Player.NextTurnEnergyBonus > 0)
        {
            Player.RestoreEnergy(Player.NextTurnEnergyBonus);
            EmitSignal(SignalName.BattleLog, $"Adrenaline triggered: restore {Player.NextTurnEnergyBonus} extra Energy");
            Player.NextTurnEnergyBonus = 0;
        }

        int energyBefore = Player.Energy;
        Player.RestoreEnergy(Player.MaxEnergy);

        TriggerCurseEffects();

        Player.RefreshDicePool();

        int drawCount = 3;
        int drawReduction = 0;
        foreach (var card in Player.Hand)
        {
            if (card.Data.Subtype == CardSubtype.Curse &&
                card.Data.CurseTrigger == CurseTriggerType.DrawReduction &&
                card.HasTriggeredThisTurn)
            {
                drawReduction += card.CurseStacks * card.Data.CurseEffectAmount;
            }
        }
        int finalDrawCount = Mathf.Max(0, drawCount - drawReduction);

        int drawn = Player.DrawCards(finalDrawCount);
        if (drawn > 0)
            EmitSignal(SignalName.BattleLog, $"Draw cards: {drawn}");

        EmitSignal(SignalName.PlayerTurnStarted, Turn);
        EmitSignal(SignalName.BattleLog, $"Turn {Turn} started");
        EmitSignal(SignalName.BattleLog, $"Restore Energy: {energyBefore} -> {Player.Energy}");
        EmitSignal(SignalName.BattleLog, $"Gain dice pool: {Player.DiceCount}d{Player.DiceSides}");
    }

    public bool TryPlayCard(CardInstance card)
    {
        if (!IsPlayerTurn)
            return false;

        if (card.Data.Subtype == CardSubtype.Curse)
        {
            Player.Hand.Remove(card);

            int totalEffect = card.CurseStacks * card.Data.CurseEffectAmount;
            switch (card.Data.CurseTrigger)
            {
                case CurseTriggerType.HandSizeReduction:
                    Player.CurseHandSizeModifier -= totalEffect;
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: hand size -{totalEffect} (current: {Player.EffectiveMaxHandSize})");
                    break;
                case CurseTriggerType.EnergyDrain:
                    int drain = Mathf.Min(totalEffect, Player.Energy);
                    Player.Energy -= drain;
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: lose {drain} Energy");
                    break;
                case CurseTriggerType.SelfDamage:
                    Player.Hp -= totalEffect;
                    if (Player.Hp < 0) Player.Hp = 0;
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: lose {totalEffect} HP");
                    break;
                case CurseTriggerType.DrawReduction:
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: draw reduction {totalEffect}");
                    break;
            }

            float roll = GD.Randf();
            if (roll < card.Data.CurseDisappearChance)
            {
                EmitSignal(SignalName.BattleLog, $"{card.Data.Name} disappeared ({card.CurseStacks} stacks)");
            }
            else if (roll < card.Data.CurseDisappearChance + card.Data.CurseStrengthenChance)
            {
                card.CurseStacks += card.Data.CurseStrengthenAmount;
                EmitSignal(SignalName.BattleLog, $"{card.Data.Name} strengthened: {card.CurseStacks} stacks");
                Player.DrawPile.Insert(0, card);
            }
            else
            {
                EmitSignal(SignalName.BattleLog, $"{card.Data.Name} stayed ({card.CurseStacks} stacks)");
                Player.DrawPile.Insert(0, card);
            }

            if (!Player.IsAlive())
            {
                IsBattleActive = false;
                EmitSignal(SignalName.BattleLost);
                EmitSignal(SignalName.BattleLog, "Defeat!");
                EmitSignal(SignalName.CardResolved, card.Data.Id, card.Data.Subtype.ToString());
                return true;
            }

            EmitSignal(SignalName.CardResolved, card.Data.Id, card.Data.Subtype.ToString());
            return true;
        }

        if (!Player.CanPlayCard(card))
            return false;

        int energyBeforePlay = Player.Energy;
        List<DiceInstance> consumedDiceList = null;
        DiceInstance consumedDice = null;
        if (card.Data.DiceCost > 0)
        {
            consumedDiceList = Player.ConsumeDice(card.Data.DiceCost);
            if (consumedDiceList == null || consumedDiceList.Count < card.Data.DiceCost)
                return false;
            consumedDice = consumedDiceList[0];
        }

        Player.ConsumeEnergy(card.Data.EnergyCost);

        EmitSignal(SignalName.BattleLog, $"Play {card.Data.Name}");
        EmitSignal(SignalName.BattleLog, $"Spend Energy: {card.Data.EnergyCost}");
        if (card.Data.DiceCost > 0)
        {
            EmitSignal(SignalName.BattleLog, $"Dice result: {consumedDice?.Value ?? 0}");
        }

        bool success = ProcessCardBySubtype(card, consumedDice);

        if (!success)
        {
            Player.Energy = energyBeforePlay;
            Player.RestoreDice(consumedDiceList);
            return false;
        }

        if (!Enemy.IsAlive())
        {
            IsBattleActive = false;
            EmitSignal(SignalName.BattleWon);
            EmitSignal(SignalName.BattleLog, "Victory!");
            return true;
        }

        return true;
    }

    private bool ProcessCardBySubtype(CardInstance card, DiceInstance consumedDice)
    {
        bool success;
        switch (card.Data.Subtype)
        {
            case CardSubtype.Attack:
                success = ProcessAttackCard(card, consumedDice);
                break;
            case CardSubtype.Defense:
                success = ProcessDefenseCard(card, consumedDice);
                break;
            case CardSubtype.PositiveBuff:
                success = ProcessPositiveBuffCard(card, consumedDice);
                break;
            case CardSubtype.NegativeBuff:
                success = ProcessNegativeBuffCard(card, consumedDice);
                break;
            case CardSubtype.BattleLevelConsumable:
                success = ProcessBattleConsumableCard(card, consumedDice);
                break;
            case CardSubtype.GameLevelConsumable:
                success = ProcessGameConsumableCard(card, consumedDice);
                break;
            case CardSubtype.Equipment:
                success = ProcessEquipmentCard(card);
                break;
            default:
                success = ProcessAttackCard(card, consumedDice);
                break;
        }

        if (success)
        {
            EmitSignal(SignalName.CardResolved, card.Data.Id, card.Data.Subtype.ToString());
        }

        return success;
    }

    private bool ProcessAttackCard(CardInstance card, DiceInstance consumedDice)
    {
        if (card.Data.Category == CardCategory.Consumable)
            Player.MoveToExhaust(card);
        else
            Player.MoveToDiscard(card);

        int baseDamage = card.CalculateDamage(consumedDice);

        if (Player.EquippedWeaponBonus > 0 && card.Data.Subtype == CardSubtype.Attack)
        {
            baseDamage += Player.EquippedWeaponBonus;
            EmitSignal(SignalName.BattleLog, $"Weapon bonus: +{Player.EquippedWeaponBonus}");
        }

        if (card.Data.ModifyDamage != null && consumedDice != null)
        {
            baseDamage = card.Data.ModifyDamage(card, consumedDice, baseDamage);
        }

        int finalDamage = Enemy.TakeDamage(baseDamage);
        int vulnerableBeforeEffect = Enemy.GetVulnerableStacks();

        if (card.Data.ApplyEffect != null)
        {
            card.Data.ApplyEffect(card, consumedDice, Enemy);
        }

        int vulnerableAdded = Mathf.Max(0, Enemy.GetVulnerableStacks() - vulnerableBeforeEffect);
        int diceResult = consumedDice?.Value ?? -1;

        EmitSignal(SignalName.CardPlayed, card.Data.Id, finalDamage, diceResult, vulnerableAdded);
        EmitSignal(SignalName.BattleLog, $"Deal damage: {finalDamage}");

        if (vulnerableAdded > 0)
        {
            EmitSignal(SignalName.BattleLog, $"Apply Vulnerable: {vulnerableAdded}");
        }

        return true;
    }

    private bool ProcessDefenseCard(CardInstance card, DiceInstance consumedDice)
    {
        if (card.Data.Category == CardCategory.Consumable)
            Player.MoveToExhaust(card);
        else
            Player.MoveToDiscard(card);

        int shieldAmount = card.Data.ShieldValue;
        if (consumedDice != null && consumedDice.Value.HasValue)
        {
            shieldAmount += consumedDice.Value.Value;
        }

        Player.Shield += shieldAmount;
        EmitSignal(SignalName.BattleLog, $"Gain shield: {shieldAmount}");

        if (card.Data.CounterDamage > 0)
        {
            int counterDamage = card.Data.CounterDamage;
            if (consumedDice != null && consumedDice.Value.HasValue)
            {
                counterDamage += consumedDice.Value.Value;
            }
            Enemy.TakeDamage(counterDamage);
            EmitSignal(SignalName.BattleLog, $"Counter damage: {counterDamage}");
        }

        return true;
    }

    private bool ProcessPositiveBuffCard(CardInstance card, DiceInstance consumedDice)
    {
        if (card.Data.Category == CardCategory.Consumable)
            Player.MoveToExhaust(card);
        else
            Player.MoveToDiscard(card);

        if (card.Data.AppliedBuffType.HasValue)
        {
            switch (card.Data.AppliedBuffType.Value)
            {
                case BuffType.AttackUp:
                    Player.AddAttackUp(card.Data.EffectAmount);
                    EmitSignal(SignalName.BattleLog, $"Attack Up: {card.Data.EffectAmount}");
                    break;
                case BuffType.DefenseUp:
                    Player.AddDefenseUp(card.Data.EffectAmount);
                    EmitSignal(SignalName.BattleLog, $"Defense Up: {card.Data.EffectAmount}");
                    break;
                case BuffType.DiceBonus:
                    Player.AddDiceBonus(card.Data.EffectAmount);
                    EmitSignal(SignalName.BattleLog, $"Dice Bonus: {card.Data.EffectAmount}");
                    break;
                case BuffType.EnergyRegen:
                    Player.NextTurnEnergyBonus += card.Data.EffectAmount;
                    EmitSignal(SignalName.BattleLog, $"Next turn Energy bonus: {card.Data.EffectAmount}");
                    break;
            }
        }

        return true;
    }

    private bool ProcessNegativeBuffCard(CardInstance card, DiceInstance consumedDice)
    {
        if (card.Data.Category == CardCategory.Consumable)
            Player.MoveToExhaust(card);
        else
            Player.MoveToDiscard(card);

        if (card.Data.DamageFormula != null)
        {
            int baseDamage = card.CalculateDamage(consumedDice);
            int finalDamage = Enemy.TakeDamage(baseDamage);
            EmitSignal(SignalName.BattleLog, $"Deal damage: {finalDamage}");
        }

        if (card.Data.AppliedDebuffType.HasValue)
        {
            switch (card.Data.AppliedDebuffType.Value)
            {
                case DebuffType.Vulnerable:
                    Enemy.AddVulnerable(card.Data.EffectAmount);
                    EmitSignal(SignalName.BattleLog, $"Apply Vulnerable: {card.Data.EffectAmount}");
                    break;
                case DebuffType.Weak:
                    Enemy.AddWeak(card.Data.EffectAmount);
                    EmitSignal(SignalName.BattleLog, $"Apply Weak: {card.Data.EffectAmount}");
                    break;
                case DebuffType.Slow:
                    Enemy.AddSlow(card.Data.EffectAmount);
                    EmitSignal(SignalName.BattleLog, $"Apply Slow: {card.Data.EffectAmount}");
                    break;
            }
        }

        return true;
    }

    private bool ProcessBattleConsumableCard(CardInstance card, DiceInstance consumedDice)
    {
        if (card.RemainingUses <= 0)
        {
            EmitSignal(SignalName.BattleLog, $"{card.Data.Name} has no uses left");
            return false;
        }

        card.RemainingUses--;
        int restoreAmount = card.Data.EffectAmount + (consumedDice?.Value.GetValueOrDefault() ?? 0);
        Player.RestoreEnergy(restoreAmount);
        EmitSignal(SignalName.BattleLog, $"Restore Energy: {restoreAmount}");

        if (card.RemainingUses <= 0)
        {
            Player.MoveToExhaust(card);
            EmitSignal(SignalName.BattleLog, $"{card.Data.Name} exhausted");
        }
        if (Player.Hand.Contains(card))
            Player.MoveToDiscard(card);

        return true;
    }

    private bool ProcessGameConsumableCard(CardInstance card, DiceInstance consumedDice)
    {
        Player.Hp = Mathf.Min(Player.Hp + card.Data.EffectAmount, Player.MaxHp);
        Player.MoveToExhaust(card);
        EmitSignal(SignalName.BattleLog, $"Restore HP: {card.Data.EffectAmount}");

        return true;
    }

    private bool ProcessEquipmentCard(CardInstance card)
    {
        if (card.Data.EquipSlot.HasValue)
        {
            Player.MoveToDiscard(card);
            Player.EquipCard(card.Data);
            if (card.Data.EquipSlot == EquipmentSlot.Weapon)
            {
                Player.EquippedWeaponBonus = card.Data.EffectAmount;
            }
            EmitSignal(SignalName.BattleLog, $"Equip weapon: attack damage +{card.Data.EffectAmount}");
        }
        else
        {
            Player.MoveToDiscard(card);
        }

        return true;
    }

    private void TriggerCurseEffects()
    {
        foreach (var card in Player.Hand.ToList())
        {
            if (card.Data.Subtype != CardSubtype.Curse)
                continue;

            if (card.HasTriggeredThisTurn)
                continue;

            card.HasTriggeredThisTurn = true;
            int totalEffect = card.CurseStacks * card.Data.CurseEffectAmount;

            switch (card.Data.CurseTrigger)
            {
                case CurseTriggerType.HandSizeReduction:
                    Player.CurseHandSizeModifier -= totalEffect;
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: hand size -{totalEffect} (current: {Player.EffectiveMaxHandSize})");
                    break;
                case CurseTriggerType.EnergyDrain:
                    int drain = Mathf.Min(totalEffect, Player.Energy);
                    Player.Energy -= drain;
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: lose {drain} Energy");
                    break;
                case CurseTriggerType.SelfDamage:
                    Player.Hp -= totalEffect;
                    if (Player.Hp < 0) Player.Hp = 0;
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: lose {totalEffect} HP");
                    break;
                case CurseTriggerType.DrawReduction:
                    EmitSignal(SignalName.BattleLog, $"{card.Data.Name} played: draw reduction {totalEffect}");
                    break;
            }
        }
    }

    private void CleanupTemporaryCurses()
    {
        int removedCount = 0;

        for (int i = Player.Hand.Count - 1; i >= 0; i--)
        {
            if (Player.Hand[i].Data.CurseDuration == CurseDurationType.Temporary)
            {
                Player.Hand.RemoveAt(i);
                removedCount++;
            }
        }

        for (int i = Player.DrawPile.Count - 1; i >= 0; i--)
        {
            if (Player.DrawPile[i].Data.CurseDuration == CurseDurationType.Temporary)
            {
                Player.DrawPile.RemoveAt(i);
                removedCount++;
            }
        }

        for (int i = Player.DiscardPile.Count - 1; i >= 0; i--)
        {
            if (Player.DiscardPile[i].Data.CurseDuration == CurseDurationType.Temporary)
            {
                Player.DiscardPile.RemoveAt(i);
                removedCount++;
            }
        }

        for (int i = Player.Deck.Count - 1; i >= 0; i--)
        {
            if (Player.Deck[i].Data.CurseDuration == CurseDurationType.Temporary)
            {
                Player.Deck.RemoveAt(i);
                removedCount++;
            }
        }

        Player.CurseHandSizeModifier = 0;

        if (removedCount > 0)
            GD.Print($"Cleaned temporary curses: {removedCount}");
    }

    public void EndPlayerTurn()
    {
        IsPlayerTurn = false;

        foreach (var card in Player.Hand)
            if (card.Data.Subtype == CardSubtype.Curse)
                card.HasTriggeredThisTurn = false;
        foreach (var card in Player.DrawPile)
            if (card.Data.Subtype == CardSubtype.Curse)
                card.HasTriggeredThisTurn = false;
        foreach (var card in Player.DiscardPile)
            if (card.Data.Subtype == CardSubtype.Curse)
                card.HasTriggeredThisTurn = false;

        Player.EndTurn();
        Player.DiscardHand();
        EmitSignal(SignalName.PlayerTurnEnded);

        CallDeferred(nameof(ExecuteEnemyTurn));
    }

    public void ExecuteEnemyTurn()
    {
        if (!Enemy.IsAlive())
            return;

        switch (Enemy.CurrentIntent.Type)
        {
            case EnemyIntent.IntentType.Attack:
                ExecuteEnemyAttack();
                if (!Player.IsAlive())
                    return;
                break;
            case EnemyIntent.IntentType.Defend:
                Enemy.Shield += Enemy.CurrentIntent.Value;
                EmitSignal(SignalName.BattleLog, $"{Enemy.Name} gains shield: {Enemy.CurrentIntent.Value}");
                break;
            case EnemyIntent.IntentType.Buff:
                EmitSignal(SignalName.BattleLog, $"{Enemy.Name} uses buff intent: {Enemy.CurrentIntent.Value} (not implemented)");
                break;
            case EnemyIntent.IntentType.Debuff:
                EmitSignal(SignalName.BattleLog, $"{Enemy.Name} uses debuff intent: {Enemy.CurrentIntent.Value} (not implemented)");
                break;
        }

        int vulnerableBefore = Enemy.GetVulnerableStacks();
        Enemy.EndTurn();
        int vulnerableAfter = Enemy.GetVulnerableStacks();

        if (vulnerableBefore > vulnerableAfter)
        {
            EmitSignal(SignalName.BattleLog, $"Vulnerable reduced: {vulnerableBefore} -> {vulnerableAfter}");
        }

        Turn++;
        StartPlayerTurn();
    }

    private void ExecuteEnemyAttack()
    {
        int damage = Enemy.CurrentIntent.Value;
        if (damage <= 0)
        {
            EmitSignal(SignalName.BattleLog, $"{Enemy.Name} does nothing");
            return;
        }

        int weakReduction = Enemy.GetWeakStacks();
        int finalDamage = Mathf.Max(0, damage - weakReduction);

        if (weakReduction > 0)
        {
            EmitSignal(SignalName.BattleLog, $"Weak reduced damage: {damage} -> {finalDamage}");
        }

        int energyBefore = Player.Energy;
        int hpBefore = Player.Hp;
        int shieldAbsorbed = Player.TakeDamage(finalDamage);

        if (shieldAbsorbed > 0)
        {
            EmitSignal(SignalName.BattleLog, $"Shield absorbed: {shieldAbsorbed}, remaining shield: {Player.Shield}");
        }

        EmitSignal(SignalName.EnemyAttacked, finalDamage, energyBefore, Player.Energy, hpBefore, Player.Hp);
        EmitSignal(SignalName.BattleLog, $"{Enemy.Name} attacks: {finalDamage}");
        EmitSignal(SignalName.BattleLog, $"Energy: {energyBefore} -> {Player.Energy}");
        EmitSignal(SignalName.BattleLog, $"HP: {hpBefore} -> {Player.Hp}");

        if (!Player.IsAlive())
        {
            IsBattleActive = false;
            EmitSignal(SignalName.BattleLost);
            EmitSignal(SignalName.BattleLog, "Defeat!");
        }
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
    [Signal] public delegate void CardPlayedEventHandler(string cardId, int damage, int diceResult, int vulnerableAdded);
    [Signal] public delegate void CardResolvedEventHandler(string cardId, string subtype);
    [Signal] public delegate void EnemyAttackedEventHandler(int damage, int energyBefore, int energyAfter, int hpBefore, int hpAfter);
    [Signal] public delegate void BattleWonEventHandler();
    [Signal] public delegate void BattleLostEventHandler();
    [Signal] public delegate void BattleLogEventHandler(string message);
}
