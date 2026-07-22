using Godot;

public partial class BattleUI : Control
{
    private const float HandCardScale = 0.78125f;
    private const float HandNormalStep = 158.0f;
    private const float HandFocusedStep = 166.0f;
    private const float HandMinimumStep = 48.0f;
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
    private Control _cardContainer;
    private Button _endTurnButton;
    private PackedScene _cardViewScene;
    private CardPileBrowser _cardPileBrowser;
    private ColorRect _drawPileBg;
    private ColorRect _discardPileBg;
    private ColorRect _exhaustPileBg;
    private Label _drawPileCount;
    private Label _discardPileCount;
    private Label _exhaustPileCount;

    private readonly Color _drawPileColor = new Color(0.898f, 0.784f, 0.0f);
    private readonly Color _discardPileColor = new Color(0.8f, 0.2f, 0.2f);
    private readonly Color _exhaustPileColor = new Color(0.2f, 0.667f, 0.267f);
    private readonly Color _drawPileEmptyColor = new Color(0.541f, 0.478f, 0.267f);
    private readonly Color _discardPileEmptyColor = new Color(0.533f, 0.267f, 0.267f);
    private readonly Color _exhaustPileEmptyColor = new Color(0.267f, 0.467f, 0.267f);

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
        _cardContainer = GetNode<Control>("CardPanel/CardContainer");
        _endTurnButton = GetNode<Button>("EndTurnButton");
        _cardViewScene = GD.Load<PackedScene>("res://scenes/card/CardView.tscn");

        _drawPileBg = GetNode<ColorRect>("PileBar/DrawPilePanel/DrawPileView/DrawPileBg");
        _discardPileBg = GetNode<ColorRect>("PileBar/DiscardPilePanel/DiscardPileView/DiscardPileBg");
        _exhaustPileBg = GetNode<ColorRect>("PileBar/ExhaustPilePanel/ExhaustPileView/ExhaustPileBg");
        _drawPileCount = GetNode<Label>("PileBar/DrawPilePanel/DrawPileView/DrawPileCount");
        _discardPileCount = GetNode<Label>("PileBar/DiscardPilePanel/DiscardPileView/DiscardPileCount");
        _exhaustPileCount = GetNode<Label>("PileBar/ExhaustPilePanel/ExhaustPileView/ExhaustPileCount");

        GetNode<PanelContainer>("PileBar/DrawPilePanel").GuiInput += (InputEvent e) => OnPileClicked(e, "DrawPile");
        GetNode<PanelContainer>("PileBar/DiscardPilePanel").GuiInput += (InputEvent e) => OnPileClicked(e, "DiscardPile");
        GetNode<PanelContainer>("PileBar/ExhaustPilePanel").GuiInput += (InputEvent e) => OnPileClicked(e, "ExhaustPile");

        LoadCardPileBrowser();
        ConnectBattleSignals();
        UpdateUI();
    }

    private void ConnectBattleSignals()
    {
        _battleManager.PlayerTurnStarted += OnPlayerTurnStarted;
        _battleManager.PlayerTurnEnded += OnPlayerTurnEnded;
        _battleManager.CardPlayed += OnCardPlayed;
        _battleManager.CardResolved += OnCardResolved;
        _battleManager.EnemyAttacked += OnEnemyAttacked;
        _battleManager.BattleWon += OnBattleWon;
        _battleManager.BattleLost += OnBattleLost;
        _endTurnButton.Pressed += OnEndTurnPressed;
    }

    public void OnPlayerTurnStarted(int turn)
    {
        _turnLabel.Text = $"Turn {turn}";
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
        string resultText = diceResult < 0
            ? $"{cardId} dealt {damage} damage"
            : $"{cardId} rolled {diceResult}, dealt {damage} damage";

        if (vulnerableAdded > 0)
            resultText += $"\nApplied {vulnerableAdded} Vulnerable";

        _battleResultLabel.Text = resultText;
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
    }

    public void OnCardResolved(string cardId, string subtype)
    {
        _cardPreviewPanel.Visible = false;
        _previewingCardIndex = -1;
        UpdateUI();
    }

    public void OnEnemyAttacked(int damage, int energyBefore, int energyAfter, int hpBefore, int hpAfter)
    {
        _battleResultLabel.Text = $"Enemy attacked for {damage}\nEnergy: {energyBefore} -> {energyAfter}\nHP: {hpBefore} -> {hpAfter}";
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
        UpdateUI();
    }

    public void OnBattleWon()
    {
        _battleResultLabel.Text = "Victory!";
        _battleResultLabel.Visible = true;
        _cardPreviewPanel.Visible = false;
        _endTurnButton.Visible = false;
    }

    public void OnBattleLost()
    {
        _battleResultLabel.Text = "Defeat!";
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
            _playerShieldLabel.Text = _battleManager.Player.Shield > 0 ? $"Shield: {_battleManager.Player.Shield}" : "";
            _playerShieldLabel.Visible = _battleManager.Player.Shield > 0;
        }

        if (_battleManager.Enemy != null)
        {
            _enemyHpLabel.Text = $"HP: {_battleManager.Enemy.Hp}/{_battleManager.Enemy.MaxHp}";
            if (_battleManager.Enemy.Shield > 0)
                _enemyHpLabel.Text += $" Shield: {_battleManager.Enemy.Shield}";

            _enemyIntentLabel.Text = $"Intent: {_battleManager.Enemy.CurrentIntent.Description}";

            int vulnerable = _battleManager.Enemy.GetVulnerableStacks();
            _enemyVulnerableLabel.Text = vulnerable > 0 ? $"Vulnerable: {vulnerable}" : "";
            _enemyVulnerableLabel.Visible = vulnerable > 0;
        }

        UpdateDiceUI();
        UpdateCardUI();
        UpdatePileCounts();
    }

    private void LoadCardPileBrowser()
    {
        var browserScene = GD.Load<PackedScene>("res://scenes/ui/CardPileBrowser.tscn");
        if (browserScene == null)
            return;

        _cardPileBrowser = browserScene.Instantiate<CardPileBrowser>();
        AddChild(_cardPileBrowser);
        _cardPileBrowser.CardMoved += UpdateUI;
    }

    private void OnPileClicked(InputEvent @event, string pileName)
    {
        if (@event is not InputEventMouseButton mouseEvent ||
            mouseEvent.ButtonIndex != MouseButton.Left ||
            !mouseEvent.Pressed)
        {
            return;
        }

        if (!_battleManager.IsBattleActive || _battleManager.Player == null || _cardPileBrowser == null)
            return;

        switch (pileName)
        {
            case "DrawPile":
                _cardPileBrowser.OpenPile("Draw Pile", _battleManager.Player, _battleManager.Player.DrawPile, true);
                break;
            case "DiscardPile":
                _cardPileBrowser.OpenPile("Discard Pile", _battleManager.Player, _battleManager.Player.DiscardPile, true);
                break;
            case "ExhaustPile":
                _cardPileBrowser.OpenPile("Exhaust Pile", _battleManager.Player, _battleManager.Player.ExhaustPile, true);
                break;
        }
    }

    private void UpdatePileCounts()
    {
        if (_battleManager.Player == null)
            return;

        int drawCount = _battleManager.Player.DrawPile.Count;
        int discardCount = _battleManager.Player.DiscardPile.Count;
        int exhaustCount = _battleManager.Player.ExhaustPile.Count;

        _drawPileCount.Text = drawCount.ToString();
        _discardPileCount.Text = discardCount.ToString();
        _exhaustPileCount.Text = exhaustCount.ToString();

        _drawPileBg.Color = drawCount == 0 ? _drawPileEmptyColor : _drawPileColor;
        _discardPileBg.Color = discardCount == 0 ? _discardPileEmptyColor : _discardPileColor;
        _exhaustPileBg.Color = exhaustCount == 0 ? _exhaustPileEmptyColor : _exhaustPileColor;
    }

    private void UpdateDiceUI()
    {
        foreach (Node child in _diceContainer.GetChildren())
            child.QueueFree();

        if (_battleManager.Player == null)
            return;

        foreach (var dice in _battleManager.Player.DicePool)
        {
            Button diceButton = new Button();
            diceButton.Text = dice.IsConsumed ? $"d{dice.Sides}\n{dice.Value}" : $"d{dice.Sides}\n?";
            diceButton.CustomMinimumSize = Vector2.Zero;
            diceButton.Modulate = dice.IsConsumed ? new Color(0.5f, 0.5f, 0.5f) : Colors.White;
            diceButton.Disabled = true;

            _diceContainer.AddChild(diceButton);
        }
    }

    private void UpdateCardUI()
    {
        foreach (Node child in _cardContainer.GetChildren())
            child.QueueFree();

        if (_battleManager.Player == null)
            return;

        float[] cardPositions = CalculateHandCardPositions(_battleManager.Player.Hand.Count);

        for (int cardIndex = 0; cardIndex < _battleManager.Player.Hand.Count; cardIndex++)
        {
            Control cardControl = CreateHandCardView(_battleManager.Player.Hand[cardIndex], cardIndex);
            cardControl.Position = new Vector2(cardPositions[cardIndex], cardIndex == _previewingCardIndex ? -12.0f : 0.0f);
            cardControl.ZIndex = cardIndex == _previewingCardIndex ? 100 : cardIndex;
            _cardContainer.AddChild(cardControl);
        }
    }

    private float[] CalculateHandCardPositions(int cardCount)
    {
        float[] positions = new float[cardCount];
        if (cardCount == 0)
            return positions;

        float availableWidth = _cardContainer.Size.X > 1.0f ? _cardContainer.Size.X : 980.0f;
        if (cardCount == 1)
        {
            positions[0] = Mathf.Max(0.0f, (availableWidth - HandCardSize.X) * 0.5f);
            return positions;
        }

        int gapCount = cardCount - 1;
        float[] steps = new float[gapCount];

        if (_previewingCardIndex < 0)
        {
            float step = Mathf.Clamp((availableWidth - HandCardSize.X) / gapCount, HandMinimumStep, HandNormalStep);
            for (int i = 0; i < gapCount; i++)
                steps[i] = step;
        }
        else
        {
            bool[] focusedGaps = new bool[gapCount];
            if (_previewingCardIndex - 1 >= 0)
                focusedGaps[_previewingCardIndex - 1] = true;
            if (_previewingCardIndex < gapCount)
                focusedGaps[_previewingCardIndex] = true;

            int focusedGapCount = 0;
            for (int i = 0; i < gapCount; i++)
            {
                if (focusedGaps[i])
                    focusedGapCount++;
            }

            int collapsedGapCount = gapCount - focusedGapCount;
            float focusedStep = HandFocusedStep;
            float collapsedStep = collapsedGapCount > 0
                ? (availableWidth - HandCardSize.X - focusedGapCount * focusedStep) / collapsedGapCount
                : focusedStep;

            if (collapsedStep < HandMinimumStep && focusedGapCount > 0)
            {
                collapsedStep = HandMinimumStep;
                focusedStep = (availableWidth - HandCardSize.X - collapsedGapCount * collapsedStep) / focusedGapCount;
                focusedStep = Mathf.Clamp(focusedStep, HandMinimumStep, HandFocusedStep);
            }

            collapsedStep = Mathf.Clamp(collapsedStep, HandMinimumStep, HandNormalStep);
            for (int i = 0; i < gapCount; i++)
                steps[i] = focusedGaps[i] ? focusedStep : collapsedStep;
        }

        float totalWidth = HandCardSize.X;
        for (int i = 0; i < gapCount; i++)
            totalWidth += steps[i];

        float x = Mathf.Max(0.0f, (availableWidth - totalWidth) * 0.5f);
        positions[0] = x;
        for (int i = 1; i < cardCount; i++)
        {
            x += steps[i - 1];
            positions[i] = x;
        }

        return positions;
    }

    private Control CreateHandCardView(CardInstance card, int cardIndex)
    {
        var wrapper = new Control();
        wrapper.CustomMinimumSize = HandCardSize;
        wrapper.Size = HandCardSize;
        wrapper.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        wrapper.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        wrapper.GuiInput += (InputEvent @event) => OnCardGuiInput(@event, cardIndex);

        var cardView = _cardViewScene.Instantiate<CardView>();
        cardView.Scale = new Vector2(HandCardScale, HandCardScale);
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

        if (@event is not InputEventMouseButton mouseEvent ||
            mouseEvent.ButtonIndex != MouseButton.Left ||
            !mouseEvent.IsPressed())
        {
            return;
        }

        if (mouseEvent.DoubleClick)
            OnCardDoubleClicked(cardIndex);
        else
            OnCardSingleClicked(cardIndex);
    }

    private void OnCardSingleClicked(int cardIndex)
    {
        if (_battleManager.Player == null || cardIndex >= _battleManager.Player.Hand.Count)
            return;

        CardInstance card = _battleManager.Player.Hand[cardIndex];
        if (_previewingCardIndex == cardIndex && _cardPreviewPanel.Visible)
        {
            HideCardPreview();
            return;
        }

        _previewingCardIndex = cardIndex;
        ShowCardPreview(card);
    }

    private void HideCardPreview()
    {
        _cardPreviewPanel.Visible = false;
        _previewingCardIndex = -1;
        UpdateCardUI();
    }

    private void OnCardDoubleClicked(int cardIndex)
    {
        if (_battleManager.Player == null || cardIndex >= _battleManager.Player.Hand.Count)
            return;

        CardInstance card = _battleManager.Player.Hand[cardIndex];
        if (card.Data.Subtype != CardSubtype.Curse && !_battleManager.Player.CanPlayCard(card))
            return;

        _battleManager.TryPlayCard(card);
    }

    private void ShowCardPreview(CardInstance card)
    {
        _previewNameLabel.Text = CardDisplayFormatter.FormatName(card.Data);
        _previewSummaryLabel.Text = $"{CardDisplayFormatter.FormatCardTypeLabel(card.Data)} - {CardDisplayFormatter.FormatCardStatLine(card.Data, card, _battleManager.Player.DiceSides, _battleManager.Enemy, true)}";
        _previewRuleLabel.Text = CardDisplayFormatter.FormatPreviewRuleText(card, _battleManager.Player.DiceSides, _battleManager.Enemy);

        string keywordText = CardDisplayFormatter.FormatKeywordText(card.Data);
        _previewKeywordLabel.Visible = !string.IsNullOrEmpty(keywordText);
        _previewKeywordLabel.Text = keywordText;

        _cardPreviewPanel.Visible = true;
        _battleResultLabel.Visible = false;
        UpdateCardUI();
    }

    private void ApplyCardView(CardView view, CardInstance card, bool contextual)
    {
        view.Setup(
            card.Data,
            card,
            _battleManager.Player.DiceSides,
            contextual,
            (data, inst) => CardDisplayFormatter.FormatCardStatLine(data, inst, _battleManager.Player.DiceSides, _battleManager.Enemy, contextual),
            (inst) => CardDisplayFormatter.FormatCardFaceRuleText(inst, _battleManager.Player.DiceSides, _battleManager.Enemy, contextual));
    }

    private void SetMouseFilterRecursive(Control control, MouseFilterEnum mouseFilter)
    {
        control.MouseFilter = mouseFilter;
        foreach (Node child in control.GetChildren())
        {
            if (child is Control childControl)
                SetMouseFilterRecursive(childControl, mouseFilter);
        }
    }
}
