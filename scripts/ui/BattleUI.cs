using Godot;
using System.Collections.Generic;

public partial class BattleUI : Control
{
    private const float HandCardScale = 0.78125f;
    private static readonly Vector2 HandCardSize = new Vector2(150, 225);

    private Label _playerHpLabel;
    private Label _playerEnergyLabel;
    private Label _playerShieldLabel;
    private Label _enemyHpLabel;
    private Label _enemyIntentLabel;
    private Label _enemyVulnerableLabel;
    private Label _turnLabel;
    private Label _battleResultLabel;
    private PanelContainer _cardPreviewPanel;
    private Label _previewNameLabel;
    private Label _previewSummaryLabel;
    private Label _previewRuleLabel;
    private Label _previewKeywordLabel;
    private HBoxContainer _diceContainer;
    private HBoxContainer _cardContainer;
    private Button _endTurnButton;
    private PackedScene _cardViewScene;
    
    private BattleManager _battleManager;
    private int _previewingCardIndex = -1;
    
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
        _previewNameLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/PreviewNameLabel");
        _previewSummaryLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/PreviewSummaryLabel");
        _previewRuleLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/PreviewRuleLabel");
        _previewKeywordLabel = GetNode<Label>("CardPreviewPanel/PreviewVBox/PreviewKeywordLabel");
        _diceContainer = GetNode<HBoxContainer>("DicePanel/DiceContainer");
        _cardContainer = GetNode<HBoxContainer>("CardPanel/CardContainer");
        _endTurnButton = GetNode<Button>("EndTurnButton");
        _cardViewScene = GD.Load<PackedScene>("res://scenes/card/CardView.tscn");
        
        _battleManager.PlayerTurnStarted += OnPlayerTurnStarted;
        _battleManager.PlayerTurnEnded += OnPlayerTurnEnded;
        _battleManager.CardPlayed += OnCardPlayed;
        _battleManager.CardResolved += OnCardResolved;
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
    }
    
    public void OnCardResolved(string cardId, string subtype)
    {
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
            diceBtn.CustomMinimumSize = new Vector2(0, 0);
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
            int index = cardIndex;
            Control cardControl = CreateHandCardView(card, index);
            
            _cardContainer.AddChild(cardControl);
            cardIndex++;
        }
    }

    private Control CreateHandCardView(CardInstance card, int cardIndex)
    {
        var wrapper = new Control();
        wrapper.CustomMinimumSize = HandCardSize;
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        wrapper.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        wrapper.GuiInput += (InputEvent @event) => OnCardGuiInput(@event, cardIndex);

        var cardView = _cardViewScene.Instantiate<CardView>();
        cardView.Scale = new Vector2(HandCardScale, HandCardScale);
        cardView.MouseFilter = MouseFilterEnum.Ignore;
        ApplyCardView(cardView, card, _previewingCardIndex == cardIndex);
        SetMouseFilterRecursive(cardView, MouseFilterEnum.Ignore);
        wrapper.AddChild(cardView);

        bool canUse = card.Data.Subtype == CardSubtype.Curse || _battleManager.Player.CanPlayCard(card);
        wrapper.Modulate = canUse ? Colors.White : new Color(0.55f, 0.55f, 0.55f);

        return wrapper;
    }
    
    private void OnCardGuiInput(InputEvent @event, int cardIndex)
    {
        if (!_battleManager.IsBattleActive)
            return;
            
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
        UpdateCardUI();
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
        _previewNameLabel.Text = CardDisplayFormatter.FormatName(card.Data);
        _previewSummaryLabel.Text = $"{FormatCardTypeLabel(card.Data)} · {FormatCardStatLine(card.Data, card, true)}";
        _previewRuleLabel.Text = FormatPreviewRuleText(card);

        string keywordText = CardDisplayFormatter.FormatKeywordText(card.Data);
        _previewKeywordLabel.Visible = !string.IsNullOrEmpty(keywordText);
        _previewKeywordLabel.Text = keywordText;
        
        _cardPreviewPanel.Visible = true;
        _battleResultLabel.Visible = false;
        UpdateCardUI();
    }

    private string FormatCardTypeLabel(CardData data)
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

    private void ApplyCardView(CardView view, CardInstance card, bool contextual)
    {
        view.Setup(card.Data, card, _battleManager.Player.DiceSides, contextual,
            (data, inst) => FormatCardStatLine(data, inst, contextual),
            (inst) => FormatCardFaceRuleText(inst, contextual));
    }

    private string FormatCardStatLine(CardData data, CardInstance card, bool contextual)
    {
        if (data.DamageFormula != null)
        {
            GetDamageRange(data, contextual, out int minDamage, out int maxDamage);
            return minDamage == maxDamage ? $"DMG {minDamage}" : $"DMG {minDamage}~{maxDamage}";
        }

        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
        {
            return $"Shield {data.ShieldValue}";
        }

        if (data.Subtype == CardSubtype.Curse)
        {
            return card.CurseStacks > 1 ? $"Stacks {card.CurseStacks}" : "Curse";
        }

        if (data.EffectAmount > 0)
        {
            return $"Effect {data.EffectAmount}";
        }

        return "Effect";
    }

    private string FormatCardFaceRuleText(CardInstance card, bool contextual)
    {
        var parts = new List<string>();
        CardData data = card.Data;

        if (data.DamageFormula != null)
        {
            GetDamageRange(data, contextual, out int minDamage, out int maxDamage);
            string label = contextual ? "当前伤害" : "基础伤害";
            parts.Add(minDamage == maxDamage ? $"{label}: {minDamage}" : $"{label}: {minDamage}~{maxDamage}");
        }

        string condition = FormatConditionLine(data);
        if (!string.IsNullOrEmpty(condition))
        {
            parts.Add(condition);
        }

        if (data.Subtype == CardSubtype.Defense && data.ShieldValue > 0)
        {
            parts.Add($"护盾: {data.ShieldValue}");
        }

        if (data.Subtype == CardSubtype.Curse)
        {
            parts.Add(CardDisplayFormatter.FormatRuleText(data, card, _battleManager.Player.DiceSides));
        }

        return parts.Count > 0 ? string.Join("\n", parts) : "效果";
    }

    private string FormatPreviewRuleText(CardInstance card)
    {
        var parts = new List<string>();
        CardData data = card.Data;

        if (data.DamageFormula != null)
        {
            data.GetDamageRange(_battleManager.Player.DiceSides, out int baseMin, out int baseMax);
            GetDamageRange(data, true, out int currentMin, out int currentMax);

            parts.Add(baseMin == baseMax ? $"基础伤害: {baseMin}" : $"基础伤害: {baseMin}~{baseMax}");
            if (currentMin != baseMin || currentMax != baseMax)
            {
                parts.Add(currentMin == currentMax ? $"当前预计: {currentMin}" : $"当前预计: {currentMin}~{currentMax}");
            }
        }

        string condition = FormatConditionLine(data);
        if (!string.IsNullOrEmpty(condition))
        {
            parts.Add(condition);
        }

        string ruleText = CardDisplayFormatter.FormatRuleText(data, card, _battleManager.Player.DiceSides);
        if (parts.Count == 0 && !string.IsNullOrEmpty(ruleText))
        {
            parts.Add(ruleText);
        }

        return parts.Count > 0 ? string.Join("\n", parts) : "无额外效果。";
    }

    private void GetDamageRange(CardData data, bool contextual, out int minDamage, out int maxDamage)
    {
        data.GetDamageRange(_battleManager.Player.DiceSides, out minDamage, out maxDamage);

        if (!contextual || data.DamageFormula == null || _battleManager.Enemy == null)
        {
            return;
        }

        int vulnerable = _battleManager.Enemy.GetVulnerableStacks();
        if (vulnerable > 0 && data.Subtype == CardSubtype.Attack)
        {
            minDamage += vulnerable;
            maxDamage += vulnerable;
        }
    }

    private string FormatConditionLine(CardData data)
    {
        switch (data.Id)
        {
            case "break_core":
                return "骰点 5+: 施加 2 层破甲";
            case "vulnerable_strike":
                return "骰点 3+: 施加 1 层破甲";
            case "critical_hit":
                return "骰点 4+: 伤害翻倍";
            default:
                return "";
        }
    }

    private string FormatArtNote(CardData data)
    {
        if (!string.IsNullOrEmpty(data.VisualKey))
        {
            return $"AI art slot\n{data.VisualKey}";
        }

        return "AI art slot\n448 x 320";
    }

    private void SetMouseFilterRecursive(Control control, MouseFilterEnum mouseFilter)
    {
        control.MouseFilter = mouseFilter;
        foreach (Node child in control.GetChildren())
        {
            if (child is Control childControl)
            {
                SetMouseFilterRecursive(childControl, mouseFilter);
            }
        }
    }
}
