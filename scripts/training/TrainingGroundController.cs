using Godot;

public partial class TrainingGroundController : Node
{
    private const float DeckCardScale = 0.74f;
    private static readonly Vector2 DeckCardTileSize = new Vector2(148, 222);

    private BattleManager _battleManager;
    private BattleLogPanel _battleLogPanel;
    private BattleUI _battleUI;

    private LineEdit _playerHpInput;
    private LineEdit _playerEnergyInput;
    private LineEdit _diceCountInput;
    private LineEdit _diceSidesInput;
    private LineEdit _fixedRollInput;

    private Button _rollModeRandomBtn;
    private Button _rollModeFixedBtn;

    private Button _enemyDummyBtn;
    private Button _enemyBeastBtn;

    private LineEdit _enemyHpInput;
    private LineEdit _enemyAttackInput;
    private LineEdit _enemyShieldInput;
    private LineEdit _enemyIntentValueInput;

    private Button _enemyIntentAttackBtn;
    private Button _enemyIntentDefendBtn;
    private Button _enemyIntentBuffBtn;
    private Button _enemyIntentDebuffBtn;

    private Button _resetButton;
    private Button _addCurseButton;

    private Button _configToggleButton;
    private Button _logToggleButton;
    private Button _deckToggleButton;
    private ColorRect _dimMask;
    private Panel _configFloatingPanel;
    private Control _logFloatingPanel;
    private PanelContainer _deckFloatingPanel;

    private Label _deckTitleLabel;
    private GridContainer _deckCardList;
    private Label _deckMiniCostLabel;
    private Label _deckMiniRuleLabel;
    private Label _deckMiniKeywordLabel;

    private TrainingConfig _config = new TrainingConfig();
    private PackedScene _cardViewScene;

    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>("BattleManager");
        _battleLogPanel = GetNode<BattleLogPanel>("OverlayLayer/LogFloatingPanel");
        _battleUI = GetNode<BattleUI>("BattleUI");

        _playerHpInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/PlayerHpRow/PlayerHpInput");
        _playerEnergyInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/PlayerEnergyRow/PlayerEnergyInput");
        _diceCountInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/DiceCountRow/DiceCountInput");
        _diceSidesInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/DiceSidesRow/DiceSidesInput");
        _fixedRollInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/FixedRollRow/FixedRollInput");

        _rollModeRandomBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/RollModeRow/RollModeRandomBtn");
        _rollModeFixedBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/RollModeRow/RollModeFixedBtn");

        _enemyDummyBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyRow/EnemyDummyBtn");
        _enemyBeastBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyRow/EnemyBeastBtn");

        _enemyHpInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyHpRow/EnemyHpInput");
        _enemyAttackInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyAttackRow/EnemyAttackInput");
        _enemyShieldInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyShieldRow/EnemyShieldInput");
        _enemyIntentValueInput = GetNode<LineEdit>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyIntentValueRow/EnemyIntentValueInput");

        _enemyIntentAttackBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyIntentRow1/EnemyIntentAttackBtn");
        _enemyIntentDefendBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyIntentRow1/EnemyIntentDefendBtn");
        _enemyIntentBuffBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyIntentRow2/EnemyIntentBuffBtn");
        _enemyIntentDebuffBtn = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/EnemyIntentRow2/EnemyIntentDebuffBtn");

        _resetButton = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/ResetButton");
        _addCurseButton = GetNode<Button>("OverlayLayer/ConfigFloatingPanel/ConfigScroll/ConfigContainer/AddCurseButton");

        _configToggleButton = GetNode<Button>("OverlayLayer/ConfigToggleButton");
        _logToggleButton = GetNode<Button>("OverlayLayer/LogToggleButton");
        _deckToggleButton = GetNode<Button>("OverlayLayer/DeckToggleButton");
        _dimMask = GetNode<ColorRect>("OverlayLayer/DimMask");
        _configFloatingPanel = GetNode<Panel>("OverlayLayer/ConfigFloatingPanel");
        _logFloatingPanel = GetNode<Control>("OverlayLayer/LogFloatingPanel");
        _deckFloatingPanel = GetNode<PanelContainer>("OverlayLayer/DeckFloatingPanel");

        _deckTitleLabel = GetNode<Label>("OverlayLayer/DeckFloatingPanel/DeckVBox/DeckTitleLabel");
        _deckCardList = GetNode<GridContainer>("OverlayLayer/DeckFloatingPanel/DeckVBox/DeckContentHBox/DeckCardScroll/DeckCardList");
        _deckCardList.AddThemeConstantOverride("h_separation", 8);
        _deckCardList.AddThemeConstantOverride("v_separation", 8);
        _deckMiniCostLabel = GetNode<Label>("OverlayLayer/DeckFloatingPanel/DeckVBox/DeckContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniCostLabel");
        _deckMiniRuleLabel = GetNode<Label>("OverlayLayer/DeckFloatingPanel/DeckVBox/DeckContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniRuleLabel");
        _deckMiniKeywordLabel = GetNode<Label>("OverlayLayer/DeckFloatingPanel/DeckVBox/DeckContentHBox/MiniPreviewPanel/MiniPreviewVBox/MiniKeywordLabel");

        _cardViewScene = GD.Load<PackedScene>("res://scenes/card/CardView.tscn");

        _playerHpInput.Text = _config.PlayerHp.ToString();
        _playerEnergyInput.Text = _config.PlayerEnergy.ToString();
        _diceCountInput.Text = _config.DiceCount.ToString();
        _diceSidesInput.Text = _config.DiceSides.ToString();
        _fixedRollInput.Text = _config.FixedRollValue.ToString();

        _enemyHpInput.Text = _config.EnemyHp.ToString();
        _enemyAttackInput.Text = _config.EnemyAttack.ToString();
        _enemyShieldInput.Text = _config.EnemyShield.ToString();
        _enemyIntentValueInput.Text = _config.EnemyIntentValue.ToString();

        UpdateRollModeButtons();
        UpdateEnemyButtons();
        UpdateEnemyIntentButtons();
        UpdateEnemyControlsVisibility();

        _rollModeRandomBtn.Pressed += () => SetRollMode(RollMode.Random);
        _rollModeFixedBtn.Pressed += () => SetRollMode(RollMode.Fixed);

        _enemyDummyBtn.Pressed += () => SetEnemyType(EnemyType.TrainingDummy);
        _enemyBeastBtn.Pressed += () => SetEnemyType(EnemyType.TrainingBeast);

        _enemyIntentAttackBtn.Pressed += () => SetEnemyIntent(EnemyIntentType.Attack);
        _enemyIntentDefendBtn.Pressed += () => SetEnemyIntent(EnemyIntentType.Defend);
        _enemyIntentBuffBtn.Pressed += () => SetEnemyIntent(EnemyIntentType.Buff);
        _enemyIntentDebuffBtn.Pressed += () => SetEnemyIntent(EnemyIntentType.Debuff);

        _resetButton.Pressed += OnResetBattle;
        _addCurseButton.Pressed += OnAddTemporaryCurse;

        _configToggleButton.Pressed += OnConfigToggle;
        _logToggleButton.Pressed += OnLogToggle;
        _deckToggleButton.Pressed += OnDeckToggle;
        _dimMask.GuiInput += OnDimMaskClicked;



        _battleManager.BattleLog += _battleLogPanel.AddLog;

        ResetBattle();
    }

    private void OnDeckToggle()
    {
        bool isOpen = !_deckFloatingPanel.Visible;
        _configFloatingPanel.Visible = false;
        _logFloatingPanel.Visible = false;
        _deckFloatingPanel.Visible = isOpen;
        _dimMask.Visible = isOpen;

        if (isOpen)
            PopulateDeckPanel();
    }

    private void PopulateDeckPanel()
    {
        foreach (var child in _deckCardList.GetChildren())
            child.QueueFree();

        _deckTitleLabel.Text = $"Deck ({_battleManager.Player.Deck.Count})";

        _deckMiniCostLabel.Text = "Cost";
        _deckMiniRuleLabel.Text = "";
        _deckMiniKeywordLabel.Visible = false;
        _deckMiniKeywordLabel.Text = "";

        foreach (var card in _battleManager.Player.Deck)
        {
            _deckCardList.AddChild(CreateDeckCardTile(card));
        }
    }

    private Control CreateDeckCardTile(CardInstance card)
    {
        var wrapper = new Control();
        wrapper.CustomMinimumSize = DeckCardTileSize;
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        wrapper.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        wrapper.GuiInput += (InputEvent @event) => OnDeckCardGuiInput(@event, card);

        var cardView = (CardView)_cardViewScene.Instantiate();
        cardView.Scale = new Vector2(DeckCardScale, DeckCardScale);
        cardView.Setup(card.Data, card, _battleManager.Player.DiceSides, false);
        SetMouseFilterRecursive(cardView, Control.MouseFilterEnum.Ignore);
        wrapper.AddChild(cardView);

        return wrapper;
    }

    private void OnDeckCardGuiInput(InputEvent @event, CardInstance card)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            string diceText = card.Data.DiceCost > 0 ? card.Data.DiceCost.ToString() : "none";
            _deckMiniCostLabel.Text = $"Energy: {card.Data.EnergyCost}  Dice: {diceText}";

            _deckMiniRuleLabel.Text = CardDisplayFormatter.FormatRuleText(card.Data, card, _battleManager.Player.DiceSides);

            string keywordText = CardDisplayFormatter.FormatKeywordText(card.Data);
            _deckMiniKeywordLabel.Visible = !string.IsNullOrEmpty(keywordText);
            _deckMiniKeywordLabel.Text = keywordText;
        }
    }

    private void OnConfigToggle()
    {
        bool shouldShow = !_configFloatingPanel.Visible;

        if (shouldShow)
        {
            _logFloatingPanel.Visible = false;
            _deckFloatingPanel.Visible = false;
            _configFloatingPanel.Visible = true;
            _dimMask.Visible = true;
        }
        else
        {
            _configFloatingPanel.Visible = false;
            _dimMask.Visible = false;
        }
    }

    private void OnLogToggle()
    {
        bool shouldShow = !_logFloatingPanel.Visible;

        if (shouldShow)
        {
            _configFloatingPanel.Visible = false;
            _deckFloatingPanel.Visible = false;
            _logFloatingPanel.Visible = true;
            _dimMask.Visible = true;
        }
        else
        {
            _logFloatingPanel.Visible = false;
            _dimMask.Visible = false;
        }
    }

    private void OnDimMaskClicked(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            _configFloatingPanel.Visible = false;
            _logFloatingPanel.Visible = false;
            _deckFloatingPanel.Visible = false;
            _dimMask.Visible = false;
        }
    }

    private void SetRollMode(RollMode mode)
    {
        _config.RollMode = mode;
        UpdateRollModeButtons();
    }

    private void SetEnemyType(EnemyType type)
    {
        _config.EnemyType = type;

        if (type == EnemyType.TrainingBeast && _config.EnemyIntent == EnemyIntentType.None)
        {
            _config.EnemyIntent = EnemyIntentType.Attack;
        }

        UpdateEnemyButtons();
        UpdateEnemyControlsVisibility();
    }

    private void UpdateEnemyControlsVisibility()
    {
        bool isDummy = _config.EnemyType == EnemyType.TrainingDummy;

        _enemyAttackInput.Editable = !isDummy;
        _enemyAttackInput.Modulate = isDummy ? Colors.Gray : Colors.White;

        _enemyShieldInput.Editable = !isDummy;
        _enemyShieldInput.Modulate = isDummy ? Colors.Gray : Colors.White;

        if (isDummy)
        {
            _config.EnemyIntent = EnemyIntentType.None;
        }

        UpdateEnemyIntentButtons();
    }

    private void SetEnemyIntent(EnemyIntentType intent)
    {
        _config.EnemyIntent = intent;
        UpdateEnemyIntentButtons();
    }

    private void UpdateRollModeButtons()
    {
        _rollModeRandomBtn.ButtonPressed = _config.RollMode == RollMode.Random;
        _rollModeFixedBtn.ButtonPressed = _config.RollMode == RollMode.Fixed;
        _fixedRollInput.Editable = _config.RollMode == RollMode.Fixed;
        _fixedRollInput.Modulate = _config.RollMode == RollMode.Fixed ? Colors.White : Colors.Gray;
    }

    private void UpdateEnemyButtons()
    {
        _enemyDummyBtn.ButtonPressed = _config.EnemyType == EnemyType.TrainingDummy;
        _enemyBeastBtn.ButtonPressed = _config.EnemyType == EnemyType.TrainingBeast;
    }

    private void UpdateEnemyIntentButtons()
    {
        _enemyIntentAttackBtn.ButtonPressed = _config.EnemyIntent == EnemyIntentType.Attack;
        _enemyIntentDefendBtn.ButtonPressed = _config.EnemyIntent == EnemyIntentType.Defend;
        _enemyIntentBuffBtn.ButtonPressed = _config.EnemyIntent == EnemyIntentType.Buff;
        _enemyIntentDebuffBtn.ButtonPressed = _config.EnemyIntent == EnemyIntentType.Debuff;

        bool hasIntent = _config.EnemyIntent != EnemyIntentType.None;
        _enemyIntentAttackBtn.Visible = hasIntent;
        _enemyIntentDefendBtn.Visible = hasIntent;
        _enemyIntentBuffBtn.Visible = hasIntent;
        _enemyIntentDebuffBtn.Visible = hasIntent;
        _enemyIntentValueInput.Visible = hasIntent;
    }

    private void OnResetBattle()
    {
        ResetBattle();
    }

    public void ResetBattle()
    {
        _battleLogPanel.Clear();

        ParseConfig();

        _battleLogPanel.AddLog("=== Training ground reset ===");
        _battleLogPanel.AddLog($"Player: {_config.PlayerHp} HP, {_config.PlayerEnergy} Energy");
        _battleLogPanel.AddLog($"Dice: {_config.DiceCount}d{_config.DiceSides}");
        _battleLogPanel.AddLog($"Roll mode: {_config.RollMode}");
        if (_config.RollMode == RollMode.Fixed)
        {
            _battleLogPanel.AddLog($"Fixed roll: {_config.FixedRollValue}");
        }
        _battleLogPanel.AddLog($"Enemy: {_config.EnemyType}");
        _battleLogPanel.AddLog($"Enemy HP: {_config.EnemyHp}");
        _battleLogPanel.AddLog($"Enemy attack: {_config.EnemyAttack}");
        _battleLogPanel.AddLog($"Enemy shield: {_config.EnemyShield}");
        _battleLogPanel.AddLog($"Enemy intent: {_config.EnemyIntent} ({_config.EnemyIntentValue})");

        var diceRoller = new DiceRoller
        {
            Mode = _config.RollMode,
            FixedValue = _config.FixedRollValue
        };

        var player = new PlayerState(diceRoller)
        {
            MaxHp = _config.PlayerHp,
            Hp = _config.PlayerHp,
            MaxEnergy = _config.PlayerEnergy,
            Energy = _config.PlayerEnergy,
            DiceCount = _config.DiceCount,
            DiceSides = _config.DiceSides
        };

        var enemy = TrainingEnemyFactory.CreateEnemy(_config.EnemyType, _config);
        _battleManager.InitializeBattle(player, enemy, diceRoller);
    }

    private void ParseConfig()
    {
        if (int.TryParse(_playerHpInput.Text, out int hp))
            _config.PlayerHp = hp;
        if (int.TryParse(_playerEnergyInput.Text, out int energy))
            _config.PlayerEnergy = energy;
        if (int.TryParse(_diceCountInput.Text, out int diceCount))
            _config.DiceCount = diceCount;
        if (int.TryParse(_diceSidesInput.Text, out int diceSides))
            _config.DiceSides = diceSides;
        if (int.TryParse(_fixedRollInput.Text, out int fixedRoll))
            _config.FixedRollValue = fixedRoll;

        if (int.TryParse(_enemyHpInput.Text, out int enemyHp))
            _config.EnemyHp = enemyHp;
        if (int.TryParse(_enemyAttackInput.Text, out int enemyAttack))
            _config.EnemyAttack = enemyAttack;
        if (int.TryParse(_enemyShieldInput.Text, out int enemyShield))
            _config.EnemyShield = enemyShield;
        if (int.TryParse(_enemyIntentValueInput.Text, out int intentValue))
            _config.EnemyIntentValue = intentValue;

        _config.ClampValues();

        _playerHpInput.Text = _config.PlayerHp.ToString();
        _playerEnergyInput.Text = _config.PlayerEnergy.ToString();
        _diceCountInput.Text = _config.DiceCount.ToString();
        _diceSidesInput.Text = _config.DiceSides.ToString();
        _fixedRollInput.Text = _config.FixedRollValue.ToString();

        _enemyHpInput.Text = _config.EnemyHp.ToString();
        _enemyAttackInput.Text = _config.EnemyAttack.ToString();
        _enemyShieldInput.Text = _config.EnemyShield.ToString();
        _enemyIntentValueInput.Text = _config.EnemyIntentValue.ToString();
    }

    private void OnAddTemporaryCurse()
    {
        if (_battleManager.Player == null)
            return;

        var wound = new CardInstance(CardData.Wound);
        _battleManager.Player.Hand.Add(wound);
        _battleUI.UpdateUI();
        _battleLogPanel.AddLog("Added temporary curse: Wound");
    }

    private void SetMouseFilterRecursive(Control control, Control.MouseFilterEnum mouseFilter)
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
