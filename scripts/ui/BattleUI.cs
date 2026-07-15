using Godot;
using System.Collections.Generic;

public partial class BattleUI : Control
{
    private Label _playerHpLabel;
    private Label _playerEnergyLabel;
    private Label _enemyHpLabel;
    private Label _enemyIntentLabel;
    private Label _turnLabel;
    private Label _battleResultLabel;
    private ColorRect _cardPreviewBackground;
    private Label _cardPreviewLabel;
    private HBoxContainer _diceContainer;
    private HBoxContainer _cardContainer;
    private Button _endTurnButton;
    
    private BattleManager _battleManager;
    private int _previewingCardIndex = -1;
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>("../BattleManager");
        
        _playerHpLabel = GetNode<Label>("TopPanel/PlayerStatus/PlayerHpLabel");
        _playerEnergyLabel = GetNode<Label>("TopPanel/PlayerStatus/PlayerEnergyLabel");
        _enemyHpLabel = GetNode<Label>("TopPanel/EnemyStatus/EnemyHpLabel");
        _enemyIntentLabel = GetNode<Label>("TopPanel/EnemyStatus/EnemyIntentLabel");
        _turnLabel = GetNode<Label>("TopPanel/TurnLabel");
        _battleResultLabel = GetNode<Label>("BattleResultLabel");
        _cardPreviewBackground = GetNode<ColorRect>("CardPreviewBackground");
        _cardPreviewLabel = GetNode<Label>("CardPreviewLabel");
        _diceContainer = GetNode<HBoxContainer>("DicePanel/DiceContainer");
        _cardContainer = GetNode<HBoxContainer>("CardPanel/CardContainer");
        _endTurnButton = GetNode<Button>("EndTurnButton");
        
        _battleManager.PlayerTurnStarted += OnPlayerTurnStarted;
        _battleManager.PlayerTurnEnded += OnPlayerTurnEnded;
        _battleManager.CardPlayed += OnCardPlayed;
        _battleManager.EnemyAttacked += OnEnemyAttacked;
        _battleManager.BattleWon += OnBattleWon;
        _battleManager.BattleLost += OnBattleLost;
        _endTurnButton.Pressed += OnEndTurnPressed;
        
        UpdateUI();
    }
    
    public void OnPlayerTurnStarted(int turn)
    {
        _turnLabel.Text = $"回合 {turn}";
        _battleResultLabel.Visible = false;
        _cardPreviewBackground.Visible = false;
        _cardPreviewLabel.Visible = false;
        _endTurnButton.Visible = true;
        _endTurnButton.Disabled = false;
        UpdateUI();
    }
    
    public void OnPlayerTurnEnded()
    {
        _endTurnButton.Disabled = true;
    }
    
    public void OnCardPlayed(string cardId, int damage, int diceResult)
    {
        _battleResultLabel.Text = $"{cardId} 掷出 {diceResult}，造成 {damage} 伤害";
        _battleResultLabel.Visible = true;
        _cardPreviewBackground.Visible = false;
        _cardPreviewLabel.Visible = false;
        UpdateUI();
    }
    
    public void OnEnemyAttacked(int damage, int energyBefore, int energyAfter, int hpBefore, int hpAfter)
    {
        _battleResultLabel.Text = $"敌人攻击 {damage}\nEnergy: {energyBefore} → {energyAfter}\nHP: {hpBefore} → {hpAfter}";
        _battleResultLabel.Visible = true;
        _cardPreviewBackground.Visible = false;
        _cardPreviewLabel.Visible = false;
        UpdateUI();
    }
    
    public void OnBattleWon()
    {
        _battleResultLabel.Text = "胜利!";
        _battleResultLabel.Visible = true;
        _cardPreviewBackground.Visible = false;
        _cardPreviewLabel.Visible = false;
        _endTurnButton.Visible = false;
    }
    
    public void OnBattleLost()
    {
        _battleResultLabel.Text = "失败!";
        _battleResultLabel.Visible = true;
        _cardPreviewBackground.Visible = false;
        _cardPreviewLabel.Visible = false;
        _endTurnButton.Visible = false;
    }
    
    public void OnEndTurnPressed()
    {
        _battleManager.SkipTurn();
    }
    
    private void UpdateUI()
    {
        if (_battleManager.Player != null)
        {
            _playerHpLabel.Text = $"HP: {_battleManager.Player.Hp}/{_battleManager.Player.MaxHp}";
            _playerEnergyLabel.Text = $"Energy: {_battleManager.Player.Energy}/{_battleManager.Player.MaxEnergy}";
        }
        
        if (_battleManager.Enemy != null)
        {
            _enemyHpLabel.Text = $"HP: {_battleManager.Enemy.Hp}/{_battleManager.Enemy.MaxHp}";
            _enemyIntentLabel.Text = $"意图: {_battleManager.Enemy.CurrentIntent.Description}";
        }
        
        UpdateDiceUI();
        UpdateCardUI();
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
            cardBtn.Text = $"{card.Data.Name}\nEnergy: {card.Data.EnergyCost}  Dice: {card.Data.DiceCost}\n{card.Data.Description}";
            cardBtn.Size = new Vector2(120, 80);
            cardBtn.Modulate = _battleManager.Player.CanPlayCard(card) 
                ? new Color(1, 1, 1) 
                : new Color(0.5f, 0.5f, 0.5f);
            cardBtn.Disabled = !_battleManager.IsPlayerTurn;
            
            int index = cardIndex;
            cardBtn.GuiInput += (InputEvent @event) => OnCardGuiInput(@event, index);
            
            _cardContainer.AddChild(cardBtn);
            cardIndex++;
        }
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
            if (_previewingCardIndex == cardIndex && _cardPreviewLabel.Visible)
            {
                HideCardPreview();
            }
            else
            {
                CardInstance card = _battleManager.Player.Hand[cardIndex];
                ShowCardPreview(card);
                _previewingCardIndex = cardIndex;
            }
        }
    }
    
    private void HideCardPreview()
    {
        _cardPreviewLabel.Visible = false;
        _cardPreviewBackground.Visible = false;
        _previewingCardIndex = -1;
    }
    
    private void OnCardDoubleClicked(int cardIndex)
    {
        if (_battleManager.Player != null && cardIndex < _battleManager.Player.Hand.Count)
        {
            CardInstance card = _battleManager.Player.Hand[cardIndex];
            if (!_battleManager.Player.CanPlayCard(card))
                return;
            
            _battleManager.TryPlayCard(card);
        }
    }
    
    private void ShowCardPreview(CardInstance card)
    {
        int minDamage, maxDamage;
        card.Data.GetDamageRange(_battleManager.Player.DiceSides, out minDamage, out maxDamage);
        
        string cardTypeText = "";
        switch (card.Data.Type)
        {
            case CardType.Attack:
                cardTypeText = "攻击牌";
                break;
            case CardType.Skill:
                cardTypeText = "技能牌";
                break;
            case CardType.Power:
                cardTypeText = "能力牌";
                break;
        }
        
        string targetText = "";
        switch (card.Data.Target)
        {
            case TargetType.Enemy:
                targetText = "目标: 单个敌人";
                break;
            case TargetType.Player:
                targetText = "目标: 自己";
                break;
            case TargetType.AllEnemies:
                targetText = "目标: 所有敌人";
                break;
        }
        
        _cardPreviewLabel.Text = $"【{card.Data.Name}】\n" +
            $"类型: {cardTypeText}\n" +
            $"{targetText}\n" +
            $"消耗: {card.Data.EnergyCost} Energy, {card.Data.DiceCost} 骰子\n" +
            $"伤害范围: {minDamage} ~ {maxDamage}\n" +
            $"效果: {card.Data.Description}";
        
        _cardPreviewBackground.Visible = true;
        _cardPreviewLabel.Visible = true;
        _battleResultLabel.Visible = false;
    }
}
