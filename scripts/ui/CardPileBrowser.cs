using Godot;
using System.Collections.Generic;

public partial class CardPileBrowser : Control
{
	[Signal]
	public delegate void CardMovedEventHandler();
	
	private Label _titleLabel;
	private VBoxContainer _cardList;
	private Label _miniCostLabel;
	private Label _miniRuleLabel;
	private Label _miniKeywordLabel;
	private Button _closeButton;
	private ColorRect _backgroundDim;
	
	private PlayerState _player;
	private List<CardInstance> _pile;
	private bool _allowDoubleClickToHand;
	private CardInstance _previewingCard;
	private PackedScene _cardViewScene;
	
	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("BrowserPanel/BrowserVBox/TitleLabel");
		_cardList = GetNode<VBoxContainer>("BrowserPanel/BrowserVBox/ContentHBox/CardScroll/CardList");
		_miniCostLabel = GetNode<Label>("BrowserPanel/BrowserVBox/ContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniCostLabel");
		_miniRuleLabel = GetNode<Label>("BrowserPanel/BrowserVBox/ContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniRuleLabel");
		_miniKeywordLabel = GetNode<Label>("BrowserPanel/BrowserVBox/ContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniKeywordLabel");
		_closeButton = GetNode<Button>("BrowserPanel/BrowserVBox/CloseButton");
		_backgroundDim = GetNode<ColorRect>("BackgroundDim");
		
		_cardViewScene = GD.Load<PackedScene>("res://scenes/card/CardView.tscn");
		
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
		_miniRuleLabel.Text = "";
		_miniKeywordLabel.Visible = false;
		_miniKeywordLabel.Text = "";
		
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
			var cardView = (CardView)_cardViewScene.Instantiate();
			cardView.Setup(card.Data, card, _player.DiceSides, false);
			cardView.GuiInput += (InputEvent @event) => OnCardGuiInput(@event, card);
			
			_cardList.AddChild(cardView);
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
		
		_miniCostLabel.Text = $"消耗：{CardDisplayFormatter.FormatCost(card.Data)}";
		_miniRuleLabel.Text = CardDisplayFormatter.FormatRuleText(card.Data, card, _player.DiceSides);
		
		string keywordText = CardDisplayFormatter.FormatKeywordText(card.Data);
		_miniKeywordLabel.Visible = !string.IsNullOrEmpty(keywordText);
		_miniKeywordLabel.Text = keywordText;
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
