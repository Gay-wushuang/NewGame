using Godot;
using System.Collections.Generic;

public partial class CardPileBrowser : Control
{
    [Signal]
    public delegate void CardMovedEventHandler();
    
    private Label _titleLabel;
    private VBoxContainer _cardList;
    private Label _miniCostLabel;
    private Label _miniEffectLabel;
    private Label _miniDescLabel;
    private Button _closeButton;
    private ColorRect _backgroundDim;
    
    private PlayerState _player;
    private List<CardInstance> _pile;
    private bool _allowDoubleClickToHand;
    private CardInstance _previewingCard;
    
    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("BrowserPanel/BrowserVBox/TitleLabel");
        _cardList = GetNode<VBoxContainer>("BrowserPanel/BrowserVBox/ContentHBox/CardScroll/CardList");
        _miniCostLabel = GetNode<Label>("BrowserPanel/BrowserVBox/ContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniCostLabel");
        _miniEffectLabel = GetNode<Label>("BrowserPanel/BrowserVBox/ContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniEffectLabel");
        _miniDescLabel = GetNode<Label>("BrowserPanel/BrowserVBox/ContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniDescLabel");
        _closeButton = GetNode<Button>("BrowserPanel/BrowserVBox/CloseButton");
        _backgroundDim = GetNode<ColorRect>("BackgroundDim");
        
        _closeButton.Pressed += OnClosePressed;
        _backgroundDim.GuiInput += OnBackgroundClicked;
        
        Visible = false;
    }
    
    public void OpenPile(string title, PlayerState player, List<CardInstance> pile, bool allowDoubleClickToHand = false)
    {
        _titleLabel.Text = title;
        _player = player;
        _pile = pile;
        _allowDoubleClickToHand = allowDoubleClickToHand;
        _previewingCard = null;
        
        RefreshUI();
        Visible = true;
    }
    
    public void Close()
    {
        Visible = false;
        _player = null;
        _pile = null;
    }
    
    private void RefreshUI()
    {
        foreach (Node child in _cardList.GetChildren())
        {
            child.QueueFree();
        }
        
        _miniCostLabel.Text = "消耗";
        _miniEffectLabel.Text = "效果";
        _miniDescLabel.Text = "描述";
        
        if (_pile == null || _pile.Count == 0)
        {
            Label emptyLabel = new Label();
            emptyLabel.Text = "堆内无牌";
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _cardList.AddChild(emptyLabel);
            return;
        }
        
        foreach (var card in _pile)
        {
            Button cardBtn = new Button();
            if (card.Data.Subtype == CardSubtype.Curse)
            {
                string prefix = card.Data.CurseDuration == CurseDurationType.Temporary ? "[临时]" : "[永久]";
                cardBtn.Text = $"{prefix} {card.Data.Name} [{card.CurseStacks}层]";
            }
            else
            {
                cardBtn.Text = $"{card.Data.Name}\nE:{card.Data.EnergyCost} D:{card.Data.DiceCost}";
            }
            cardBtn.CustomMinimumSize = new Vector2(300, 40);
            
            cardBtn.GuiInput += (InputEvent @event) => OnCardGuiInput(@event, card);
            
            _cardList.AddChild(cardBtn);
        }
    }
    
    private void OnCardGuiInput(InputEvent @event, CardInstance card)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.IsPressed())
            {
                if (mouseEvent.DoubleClick)
                {
                    OnCardDoubleClicked(card);
                }
                else
                {
                    OnCardSingleClicked(card);
                }
            }
        }
    }
    
    private void OnCardSingleClicked(CardInstance card)
    {
        _previewingCard = card;
        
        string diceText = card.Data.DiceCost > 0 ? card.Data.DiceCost.ToString() : "无需";
        _miniCostLabel.Text = $"Energy: {card.Data.EnergyCost}  Dice: {diceText}";
        
        _miniEffectLabel.Text = card.Data.Description;
        _miniDescLabel.Text = card.Data.EffectExplanation;
    }
    
    private string GetEffectText(CardData data)
    {
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
                return $"消耗品效果: Energy恢复 / 本场剩余: {data.UsesPerBattle}次";
            case CardSubtype.GameLevelConsumable:
                return $"消耗品效果: HP恢复 / 全局剩余: {data.MaxUsage}次";
            case CardSubtype.Equipment:
                return data.EquipSlot.HasValue 
                    ? $"装备槽: {data.EquipSlot.Value} / 加成: +{data.EffectAmount} / 持续: {data.Duration}场" 
                    : "效果: 无";
            case CardSubtype.Curse:
                return data.AppliedCurseType.HasValue 
                    ? $"诅咒: {data.AppliedCurseType.Value} / 移除条件: {data.RemovalCondition}" 
                    : "效果: 无";
            default:
                if (data.AppliedBuffType.HasValue)
                    return $"增益: {data.AppliedBuffType.Value} ({data.EffectAmount})";
                if (data.AppliedDebuffType.HasValue)
                    return $"减益: {data.AppliedDebuffType.Value} ({data.EffectAmount})";
                
                int minDamage, maxDamage;
                data.GetDamageRange(6, out minDamage, out maxDamage);
                if (data.DamageFormula != null)
                    return $"伤害: {minDamage}~{maxDamage}";
                return "效果: 无";
        }
    }
    
    private void OnCardDoubleClicked(CardInstance card)
    {
        if (_allowDoubleClickToHand)
        {
            if (_player.Hand.Count >= _player.EffectiveMaxHandSize)
            {
                return;
            }
            
            _pile.Remove(card);
            _player.Hand.Add(card);
            RefreshUI();
            EmitSignal(SignalName.CardMoved);
            
            if (_pile.Count == 0)
            {
                Close();
            }
        }
    }
    
    private void OnClosePressed()
    {
        Close();
    }
    
    private void OnBackgroundClicked(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            Close();
        }
    }
}