using Godot;
using System;

[Tool]
public partial class CardView : Control
{
    private bool _showBack;
    private string _cardName = "Card Name";
    private string _cardType = "Attack";
    private string _target = "Enemy";
    private string _statLine = "Card Info";
    private string _energyCostText = "N";
    private string _diceCostText = "N";
    private string _rulesText = "Card Effect";
    private string _artNote = "AI art slot\n448 x 320";
    private string _backCoreText = "DICE\nCORE";
    private string _backNoteText = "card back";

    [Export]
    public bool ShowBack
    {
        get => _showBack;
        set { _showBack = value; UpdateViewDeferred(); }
    }

    [Export]
    public string CardName
    {
        get => _cardName;
        set { _cardName = value; UpdateViewDeferred(); }
    }

    [Export]
    public string CardType
    {
        get => _cardType;
        set { _cardType = value; UpdateViewDeferred(); }
    }

    [Export]
    public string Target
    {
        get => _target;
        set { _target = value; UpdateViewDeferred(); }
    }

    [Export]
    public string StatLine
    {
        get => _statLine;
        set { _statLine = value; UpdateViewDeferred(); }
    }

    [Export]
    public string EnergyCostText
    {
        get => _energyCostText;
        set { _energyCostText = value; UpdateViewDeferred(); }
    }

    [Export]
    public string DiceCostText
    {
        get => _diceCostText;
        set { _diceCostText = value; UpdateViewDeferred(); }
    }

    [Export(PropertyHint.MultilineText)]
    public string RulesText
    {
        get => _rulesText;
        set { _rulesText = value; UpdateViewDeferred(); }
    }

    [Export(PropertyHint.MultilineText)]
    public string ArtNote
    {
        get => _artNote;
        set { _artNote = value; UpdateViewDeferred(); }
    }

    [Export(PropertyHint.MultilineText)]
    public string BackCoreText
    {
        get => _backCoreText;
        set { _backCoreText = value; UpdateViewDeferred(); }
    }

    [Export]
    public string BackNoteText
    {
        get => _backNoteText;
        set { _backNoteText = value; UpdateViewDeferred(); }
    }

    public void Setup(CardData data, CardInstance instance, int diceSides, bool showContext, 
        Func<CardData, CardInstance, string> formatStatLine = null,
        Func<CardInstance, string> formatRulesText = null)
    {
        ShowBack = false;
        CardName = CardDisplayFormatter.FormatName(data);
        CardType = CardDisplayFormatter.FormatCardTypeLabel(data);
        
        if (formatStatLine != null)
            StatLine = formatStatLine(data, instance);
        else
            StatLine = CardDisplayFormatter.FormatCardStatLine(data, instance, showContext);
        
        EnergyCostText = data.EnergyCost.ToString();
        DiceCostText = data.DiceCost.ToString();
        ArtNote = !string.IsNullOrEmpty(data.VisualKey) ? $"AI art slot\n{data.VisualKey}" : "AI art slot\n448 x 320";
        
        if (showContext)
        {
            if (formatRulesText != null)
                RulesText = formatRulesText(instance);
            else
                RulesText = CardDisplayFormatter.FormatRuleText(data, instance, diceSides);
        }
        else
        {
            RulesText = "";
        }
    }

    public override void _Ready()
    {
        UpdateView();
    }

    public override void _EnterTree()
    {
        UpdateViewDeferred();
    }

    private void UpdateViewDeferred()
    {
        if (IsInsideTree())
        {
            CallDeferred(nameof(UpdateView));
        }
    }

    private void UpdateView()
    {
        var front = GetNodeOrNull<Control>("Canvas/FrontFace");
        var back = GetNodeOrNull<Control>("Canvas/BackFace");
        if (front != null)
        {
            front.Visible = !ShowBack;
        }
        if (back != null)
        {
            back.Visible = ShowBack;
        }

        SetLabel("Canvas/FrontFace/TopBar/CardNameLabel", CardName);
        SetLabel("Canvas/FrontFace/TopBar/EnergyBadge/EnergyValueLabel", EnergyCostText);
        SetLabel("Canvas/FrontFace/TopBar/DiceBadge/DiceValueLabel", DiceCostText);
        SetLabel("Canvas/FrontFace/ArtFrame/ArtNoteLabel", ArtNote);
        SetLabel("Canvas/FrontFace/StatsBar/StatsLabel", $"{CardType} · {StatLine}");
        SetLabel("Canvas/FrontFace/TextBox/RulesLabel", RulesText);
        SetLabel("Canvas/BackFace/CoreLabel", $"◆\n{BackCoreText}\n◆");
        SetLabel("Canvas/BackFace/BackNoteLabel", BackNoteText);
    }

    private void SetLabel(string path, string text)
    {
        var label = GetNodeOrNull<Label>(path);
        if (label != null)
        {
            label.Text = text;
        }
    }
}
