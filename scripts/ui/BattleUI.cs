using Godot;
using System.Collections.Generic;

public partial class BattleUI : Control
{
    private Label _playerHpLabel;
    private Label _playerEnergyLabel;
    private Label _playerShieldLabel;
    private Label _enemyHpLabel;
    private Label _enemyIntentLabel;
    private Label _enemyVulnerableLabel;
    private Label _turnLabel;
    private Label _battleResultLabel;
    private PanelContainer _cardPreviewPanel;
    private Label _energyCostLabel;
    private Label _diceCostLabel;
    private Label _damageRangeLabel;
    private Label _effectLabel;
    private Label _descriptionLabel;
    private HBoxContainer _diceContainer;
    private HBoxContainer _cardContainer;
    private Button _endTurnButton;
    private Label _drawPileCount;
    private Label _discardPileCount;
    private Label _exhaustPileCount;
    
    private Control _drawPileView;
    private Control _discardPileView;
    private Control _exhaustPileView;
    
    private BattleManager _battleManager;
    private int _previewingCardIndex = -1;
    
    [Signal]
    public delegate void PileClickedEventHandler(string pileName);
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>("../BattleManager");
        
        _playerHpLabel = GetNode<Label>("TopPanel/PlayerStatus/PlayerHpLabel");
        _playerEnergyLabel = GetNode<Label>("TopPanel/PlayerStatus/PlayerEnergyLabel");
        _playerShieldLabel = GetNode<Label>("TopPanel/PlayerStatus/PlayerShieldLabel");
        _enemyHpLabel = GetNode<Label>("TopPanel/EnemyStatus/EnemyHpLabel");
        _enemyIntentLabel = GetNode<Label>("TopPanel/EnemyStatus/EnemyIntentLabel");
        _enemyVulnerableLabel = GetNode<Label>("TopPanel/EnemyStatus/EnemyVulnerableLabel");
        _turnLabel = GetNode<Label>("TopPanel/TurnLabel");
        _battleResultLabel = GetNode<Label>("BattleResultLabel");
        _cardPreviewPanel = GetNode<PanelContainer>("CardPreviewPanel");
        _energyCostLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/CostRow/EnergyCostLabel");
        _diceCostLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/CostRow/DiceCostLabel");
        _damageRangeLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/EffectRow/DamageRangeLabel");
        _effectLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/EffectRow/EffectLabel");
        _descriptionLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/DescriptionLabel");
        _diceContainer = GetNode<HBoxContainer>("DicePanel/DiceContainer");
        _cardContainer = GetNode<HBoxContainer>("CardPanel/PileRow/CardContainer");
        _endTurnButton = GetNode<Button>("EndTurnButton");
        _drawPileCount = GetNode<Label>("CardPanel/PileRow/DrawPileView/DrawPileCount");
        _discardPileCount = GetNode<Label>("CardPanel/PileRow/DiscardPileView/DiscardPileCount");
        _exhaustPileCount = GetNode<Label>("CardPanel/PileRow/ExhaustPileView/ExhaustPileCount");
        
        _drawPileView = GetNode<Control>("CardPanel/PileRow/DrawPileView");
        _discardPileView = GetNode<Control>("CardPanel/PileRow/DiscardPileView");
        _exhaustPileView = GetNode<Control>("CardPanel/PileRow/ExhaustPileView");
        
        _battleManager.PlayerTurnStarted += OnPlayerTurnStarted;
        _battleManager.PlayerTurnEnded += OnPlayerTurnEnded;
        _battleManager.CardPlayed += OnCardPlayed;
        _battleManager.EnemyAttacked += OnEnemyAttacked;
        _battleManager.BattleWon += OnBattleWon;
        _battleManager.BattleLost += OnBattleLost;
        _endTurnButton.Pressed += OnEndTurnPressed;
        
        _drawPileView.GuiInput += (InputEvent @event) => OnPileGuiInput(@event, "DrawPile");
        _discardPileView.GuiInput += (InputEvent @event) => OnPileGuiInput(@event, "DiscardPile");
        _exhaustPileView.GuiInput += (InputEvent @event) => OnPileGuiInput(@event, "ExhaustPile");
        
        UpdateUI();
    }
    
    private void OnPileGuiInput(InputEvent @event, string pileName)
    {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed)
        {
            EmitSignal(SignalName.PileClicked, pileName);
        }
    }
    
    public void OnPlayerTurnStarted(int turn)
    {
        _turnLabel.Text = $"回合 {turn}";
        _battleResultLabel.Visible = false;
        _cardPreviewPanel.Visible = false;
        _endTurnButton.Visible = true;
        _endTurnButton.Disabled = false;
        UpdateUI();
    }
    
    public void OnPlayerTurnEnded()
    {
        _endTurnButton.Disabled = true;
    }
    
    public void OnCardPlayed(string cardId, int damage, int diceResult, int vulnerableAdded)
    {
        string resultText;
        if (diceResult < 0)
            resultText = $"{cardId} 造成 {damage} 伤害";
        else
            resultText = $"{cardId} 掷出 {diceResult}，造成 {damage} 伤害";
        if (vulnerableAdded > 0)
        {
            resultText += $"\n施加 {vulnerableAdded} 层破甲";
        }
        _battleResultLabel.Text = resultText;
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
        UpdateUI();
    }
    
    public void OnEnemyAttacked(int damage, int energyBefore, int energyAfter, int hpBefore, int hpAfter)
    {
        _battleResultLabel.Text = $"敌人攻击 {damage}\nEnergy: {energyBefore} → {energyAfter}\nHP: {hpBefore} → {hpAfter}";
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
        UpdateUI();
    }
    
    public void OnBattleWon()
    {
        _battleResultLabel.Text = "胜利!";
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
        _endTurnButton.Visible = false;
    }
    
    public void OnBattleLost()
    {
        _battleResultLabel.Text = "失败!";
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
        _endTurnButton.Visible = false;
    }
    
    public void OnEndTurnPressed()
    {
        _battleManager.SkipTurn();
    }
    
    public void UpdateUI()
    {
        if (_battleManager.Player != null)
        {
            _playerHpLabel.Text = $"HP: {_battleManager.Player.Hp}/{_battleManager.Player.MaxHp}";
            _playerEnergyLabel.Text = $"Energy: {_battleManager.Player.Energy}/{_battleManager.Player.MaxEnergy}";
            _playerShieldLabel.Text = _battleManager.Player.Shield > 0 ? $"护盾: {_battleManager.Player.Shield}" : "";
            _playerShieldLabel.Visible = _battleManager.Player.Shield > 0;
        }
        
        if (_battleManager.Enemy != null)
        {
            _enemyHpLabel.Text = $"HP: {_battleManager.Enemy.Hp}/{_battleManager.Enemy.MaxHp}";
            if (_battleManager.Enemy.Shield > 0)
            {
                _enemyHpLabel.Text += $" 护盾: {_battleManager.Enemy.Shield}";
            }
            _enemyIntentLabel.Text = $"意图: {_battleManager.Enemy.CurrentIntent.Description}";
            
            int vulnerable = _battleManager.Enemy.GetVulnerableStacks();
            _enemyVulnerableLabel.Text = vulnerable > 0 ? $"破甲: {vulnerable}" : "";
            _enemyVulnerableLabel.Visible = vulnerable > 0;
        }
        
        UpdateDiceUI();
        UpdateCardUI();
        UpdatePileUI();
    }
    
    private void UpdateDiceUI()
    {
        foreach (Node child in _diceContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        if (_battleManager.Player == null)
            return;
        
        int diceIndex = 0;
        foreach (var dice in _battleManager.Player.DicePool)
        {
            Button diceBtn = new Button();
            diceBtn.Text = dice.IsConsumed
                ? $"d{dice.Sides}\n{dice.Value}"
                : $"d{dice.Sides}\n?";
            diceBtn.Size = new Vector2(60, 60);
            diceBtn.Modulate = dice.IsConsumed ? new Color(0.5f, 0.5f, 0.5f) : new Color(1, 1, 1);
            diceBtn.Disabled = true;
            
            _diceContainer.AddChild(diceBtn);
            diceIndex++;
        }
    }
    
    private void UpdateCardUI()
    {
        foreach (Node child in _cardContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        if (_battleManager.Player == null)
            return;
        
        int cardIndex = 0;
        foreach (var card in _battleManager.Player.Hand)
        {
            Button cardBtn = new Button();
            cardBtn.CustomMinimumSize = new Vector2(150, 72);
            
            if (card.Data.Subtype == CardSubtype.Curse)
            {
                string prefix = card.Data.CurseDuration == CurseDurationType.Temporary ? "[临时]" : "[永久]";
                cardBtn.Text = $"{prefix} {card.Data.Name} [{card.CurseStacks}层]";
                cardBtn.Modulate = new Color(1, 1, 1);
                cardBtn.Disabled = !_battleManager.IsPlayerTurn;
            }
            else
            {
                cardBtn.Text = $"{card.Data.Name}\nE:{card.Data.EnergyCost} D:{card.Data.DiceCost}";
                cardBtn.Modulate = _battleManager.Player.CanPlayCard(card) 
                    ? new Color(1, 1, 1) 
                    : new Color(0.5f, 0.5f, 0.5f);
                cardBtn.Disabled = !_battleManager.IsPlayerTurn;
            }
            
            int index = cardIndex;
            cardBtn.GuiInput += (InputEvent @event) => OnCardGuiInput(@event, index);
            
            _cardContainer.AddChild(cardBtn);
            cardIndex++;
        }
    }
    
    private void UpdatePileUI()
    {
        if (_battleManager.Player == null) return;
        _drawPileCount.Text = _battleManager.Player.DrawPile.Count.ToString();
        _discardPileCount.Text = _battleManager.Player.DiscardPile.Count.ToString();
        _exhaustPileCount.Text = _battleManager.Player.ExhaustPile.Count.ToString();
    }
    
    private void OnCardGuiInput(InputEvent @event, int cardIndex)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.IsPressed())
            {
                if (mouseEvent.DoubleClick)
                {
                    OnCardDoubleClicked(cardIndex);
                }
                else
                {
                    OnCardSingleClicked(cardIndex);
                }
            }
        }
    }
    
    private void OnCardSingleClicked(int cardIndex)
    {
        if (_battleManager.Player != null && cardIndex < _battleManager.Player.Hand.Count)
        {
            CardInstance card = _battleManager.Player.Hand[cardIndex];
            
            if (_previewingCardIndex == cardIndex && _cardPreviewPanel.Visible)
            {
                HideCardPreview();
            }
            else
            {
                ShowCardPreview(card);
                _previewingCardIndex = cardIndex;
            }
        }
    }
    
    private void HideCardPreview()
    {
        _cardPreviewPanel.Visible = false;
        _previewingCardIndex = -1;
    }
    
    private void OnCardDoubleClicked(int cardIndex)
    {
        if (_battleManager.Player != null && cardIndex < _battleManager.Player.Hand.Count)
        {
            CardInstance card = _battleManager.Player.Hand[cardIndex];
            
            if (card.Data.Subtype == CardSubtype.Curse)
            {
                _battleManager.TryPlayCard(card);
                return;
            }
            
            if (!_battleManager.Player.CanPlayCard(card))
                return;
            
            _battleManager.TryPlayCard(card);
        }
    }
    
    private void ShowCardPreview(CardInstance card)
    {
        _energyCostLabel.Text = $"Energy: {card.Data.EnergyCost}";
        
        if (card.Data.DiceCost > 0)
            _diceCostLabel.Text = $"Dice: {card.Data.DiceCost} {card.Data.DiceType}";
        else
            _diceCostLabel.Text = "Dice: 无需";
        
        if (card.Data.Subtype == CardSubtype.Curse)
        {
            _damageRangeLabel.Text = $"当前层数: {card.CurseStacks}";
        }
        else
        {
            int minDamage, maxDamage;
            card.Data.GetDamageRange(_battleManager.Player.DiceSides, out minDamage, out maxDamage);
            _damageRangeLabel.Text = card.Data.DamageFormula != null ? $"伤害: {minDamage} ~ {maxDamage}" : "伤害: 无";
        }
        
        _effectLabel.Text = GetEffectText(card);
        
        _descriptionLabel.Text = card.Data.Description;
        
        _cardPreviewPanel.Visible = true;
        _battleResultLabel.Visible = false;
    }
    
    private string GetEffectText(CardInstance card)
    {
        CardData data = card.Data;
        switch (data.Subtype)
        {
            case CardSubtype.Defense:
                return $"护盾: {data.ShieldValue} / 持续: {data.Duration}回合";
            case CardSubtype.PositiveBuff:
                return data.AppliedBuffType.HasValue 
                    ? $"增益: {data.AppliedBuffType.Value} ({data.EffectAmount}) / 持续: {data.Duration}回合" 
                    : "效果: 无";
            case CardSubtype.NegativeBuff:
                return data.AppliedDebuffType.HasValue 
                    ? $"减益: {data.AppliedDebuffType.Value} ({data.EffectAmount}) / 持续: {data.Duration}回合" 
                    : "效果: 无";
            case CardSubtype.BattleLevelConsumable:
                return $"消耗品效果: Energy恢复 / 本场剩余: {card.RemainingUses}次";
            case CardSubtype.GameLevelConsumable:
                return $"消耗品效果: HP恢复 / 全局剩余: {data.MaxUsage}次";
            case CardSubtype.Equipment:
                return data.EquipSlot.HasValue 
                    ? $"装备槽: {data.EquipSlot.Value} / 加成: +{data.EffectAmount} / 持续: {data.Duration}场" 
                    : "效果: 无";
            case CardSubtype.Curse:
                string duration = data.CurseDuration == CurseDurationType.Temporary ? "临时" : "永久";
                int totalEffect = card.CurseStacks * data.CurseEffectAmount;
                return $"诅咒类型: {duration}\n负面效果: {data.CurseTrigger} {totalEffect}/回合\n当前层数: {card.CurseStacks}\n打出后: {data.CurseDisappearChance * 100}%消失 / {data.CurseNothingChance * 100}%无事 / {data.CurseStrengthenChance * 100}%强化+{data.CurseStrengthenAmount}";
            default:
                if (data.AppliedBuffType.HasValue)
                    return $"增益: {data.AppliedBuffType.Value} ({data.EffectAmount})";
                if (data.AppliedDebuffType.HasValue)
                    return $"减益: {data.AppliedDebuffType.Value} ({data.EffectAmount})";
                return "效果: 无";
        }
    }
}