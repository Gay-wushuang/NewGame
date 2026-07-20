using Godot;
using System.Collections.Generic;

public partial class CardLayoutDemo : Control
{
    private const float CardWidth = 512f;
    private const float CardHeight = 768f;
    private const float PreviewScale = 0.375f;

    private static readonly Color BackgroundColor = new Color("#0b0d13");
    private static readonly Color CardColor = new Color("#171a22");
    private static readonly Color PanelColor = new Color("#10131a");
    private static readonly Color StrokeColor = new Color("#6f7a8f");
    private static readonly Color TextColor = new Color("#e8edf4");
    private static readonly Color MutedTextColor = new Color("#aab3c2");
    private static readonly Color EnergyColor = new Color("#42c7ff");
    private static readonly Color DiceColor = new Color("#f6d36b");
    private static readonly Color AttackColor = new Color("#e85d3f");
    private static readonly Color VulnerableColor = new Color("#ff4b4b");

    private readonly List<CardMock> _cards = new List<CardMock>()
    {
        new CardMock(
            "EnergyStrike",
            "Attack",
            "Enemy",
            "DMG 3-8",
            1,
            1,
            "Roll 1 die.\nDeal dice + 2 damage.",
            "Blue-white energy blade\nbursting from a die.",
            EnergyColor,
            AttackColor),
        new CardMock(
            "BreakCore",
            "Attack",
            "Enemy",
            "DMG 8",
            3,
            1,
            "Deal 8 damage.\nIf die >= 5, apply\n2 Vulnerable.",
            "Red impact cracking\nan armored core.",
            VulnerableColor,
            AttackColor)
    };

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(CreateBackground());
        AddChild(CreateTitle());
        AddChild(CreateCardRow());
    }

    private ColorRect CreateBackground()
    {
        var background = new ColorRect();
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        background.Color = BackgroundColor;
        return background;
    }

    private Label CreateTitle()
    {
        var title = new Label();
        title.Text = "Card Layout Demo - 512x768 master, 192x288 preview";
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", TextColor);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.SetAnchorsPreset(LayoutPreset.TopWide);
        title.OffsetTop = 20;
        title.OffsetBottom = 60;
        return title;
    }

    private HBoxContainer CreateCardRow()
    {
        var row = new HBoxContainer();
        row.SetAnchorsPreset(LayoutPreset.Center);
        row.OffsetLeft = -390;
        row.OffsetTop = -170;
        row.OffsetRight = 390;
        row.OffsetBottom = 170;
        row.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddThemeConstantOverride("separation", 36);

        row.AddChild(CreateScaledCard(_cards[0]));
        row.AddChild(CreateScaledCard(_cards[1]));
        row.AddChild(CreateScaledBack());

        return row;
    }

    private Control CreateScaledCard(CardMock card)
    {
        var wrapper = new Control();
        wrapper.CustomMinimumSize = new Vector2(CardWidth * PreviewScale, CardHeight * PreviewScale);

        var front = CreateCardFront(card);
        front.Scale = new Vector2(PreviewScale, PreviewScale);
        wrapper.AddChild(front);

        return wrapper;
    }

    private Control CreateScaledBack()
    {
        var wrapper = new Control();
        wrapper.CustomMinimumSize = new Vector2(CardWidth * PreviewScale, CardHeight * PreviewScale);

        var back = CreateCardBack();
        back.Scale = new Vector2(PreviewScale, PreviewScale);
        wrapper.AddChild(back);

        return wrapper;
    }

    private Panel CreateCardFront(CardMock card)
    {
        var root = CreatePanel(Vector2.Zero, new Vector2(CardWidth, CardHeight), CardColor, StrokeColor, 6, 8);

        root.AddChild(CreatePanel(new Vector2(24, 24), new Vector2(464, 64), PanelColor, StrokeColor, 3, 4));
        root.AddChild(CreateCostBadge(new Vector2(38, 32), "EN", card.EnergyCost.ToString(), EnergyColor));
        root.AddChild(CreateCostBadge(new Vector2(426, 32), "D", card.DiceCost.ToString(), DiceColor));
        root.AddChild(CreateLabel(card.Name, new Vector2(104, 31), new Vector2(304, 46), 34, TextColor, HorizontalAlignment.Center));

        var artPanel = CreatePanel(new Vector2(32, 104), new Vector2(448, 320), PanelColor, card.AccentColor, 4, 4);
        artPanel.AddChild(CreateArtPlaceholder(card));
        root.AddChild(artPanel);

        root.AddChild(CreatePanel(new Vector2(32, 440), new Vector2(448, 48), PanelColor, StrokeColor, 2, 4));
        root.AddChild(CreateLabel($"{card.Type} - {card.Target} - {card.StatLine}", new Vector2(48, 449), new Vector2(416, 30), 24, MutedTextColor, HorizontalAlignment.Center));

        root.AddChild(CreatePanel(new Vector2(32, 504), new Vector2(448, 216), PanelColor, StrokeColor, 2, 4));
        root.AddChild(CreateLabel(card.RulesText, new Vector2(56, 528), new Vector2(400, 156), 30, TextColor, HorizontalAlignment.Left));

        return root;
    }

    private Panel CreateCardBack()
    {
        var root = CreatePanel(Vector2.Zero, new Vector2(CardWidth, CardHeight), CardColor, EnergyColor, 6, 8);
        root.AddChild(CreatePanel(new Vector2(32, 32), new Vector2(448, 704), new Color("#0f1420"), StrokeColor, 3, 4));

        var core = new Label();
        core.Text = "*\nDICE\nCORE\n*";
        core.AddThemeFontSizeOverride("font_size", 52);
        core.AddThemeColorOverride("font_color", EnergyColor);
        core.HorizontalAlignment = HorizontalAlignment.Center;
        core.VerticalAlignment = VerticalAlignment.Center;
        core.SetPosition(new Vector2(96, 220));
        core.SetSize(new Vector2(320, 260));
        root.AddChild(core);

        root.AddChild(CreateLabel("closed shield pattern", new Vector2(96, 520), new Vector2(320, 44), 24, MutedTextColor, HorizontalAlignment.Center));
        return root;
    }

    private Control CreateArtPlaceholder(CardMock card)
    {
        var art = new Control();
        art.SetPosition(Vector2.Zero);
        art.SetSize(new Vector2(448, 320));

        var fill = new ColorRect();
        fill.Color = card.ArtColor.Darkened(0.55f);
        fill.SetAnchorsPreset(LayoutPreset.FullRect);
        art.AddChild(fill);

        var motif = new Label();
        motif.Text = card.ArtPrompt;
        motif.AddThemeFontSizeOverride("font_size", 28);
        motif.AddThemeColorOverride("font_color", TextColor);
        motif.HorizontalAlignment = HorizontalAlignment.Center;
        motif.VerticalAlignment = VerticalAlignment.Center;
        motif.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        motif.SetAnchorsPreset(LayoutPreset.FullRect);
        motif.OffsetLeft = 32;
        motif.OffsetTop = 32;
        motif.OffsetRight = -32;
        motif.OffsetBottom = -32;
        art.AddChild(motif);

        return art;
    }

    private Panel CreateCostBadge(Vector2 position, string label, string value, Color color)
    {
        var badge = CreatePanel(position, new Vector2(48, 48), color.Darkened(0.55f), color, 3, 24);
        badge.AddChild(CreateLabel(label, new Vector2(0, 4), new Vector2(48, 12), 12, MutedTextColor, HorizontalAlignment.Center));
        badge.AddChild(CreateLabel(value, new Vector2(0, 15), new Vector2(48, 28), 28, TextColor, HorizontalAlignment.Center));
        return badge;
    }

    private Panel CreatePanel(Vector2 position, Vector2 size, Color fill, Color stroke, int borderWidth, int cornerRadius)
    {
        var panel = new Panel();
        panel.SetPosition(position);
        panel.SetSize(size);

        var style = new StyleBoxFlat();
        style.BgColor = fill;
        style.BorderColor = stroke;
        style.BorderWidthLeft = borderWidth;
        style.BorderWidthRight = borderWidth;
        style.BorderWidthTop = borderWidth;
        style.BorderWidthBottom = borderWidth;
        style.CornerRadiusTopLeft = cornerRadius;
        style.CornerRadiusTopRight = cornerRadius;
        style.CornerRadiusBottomLeft = cornerRadius;
        style.CornerRadiusBottomRight = cornerRadius;
        panel.AddThemeStyleboxOverride("panel", style);

        return panel;
    }

    private Label CreateLabel(string text, Vector2 position, Vector2 size, int fontSize, Color color, HorizontalAlignment alignment)
    {
        var label = new Label();
        label.Text = text;
        label.SetPosition(position);
        label.SetSize(size);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.HorizontalAlignment = alignment;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }

    private sealed class CardMock
    {
        public string Name { get; }
        public string Type { get; }
        public string Target { get; }
        public string StatLine { get; }
        public int EnergyCost { get; }
        public int DiceCost { get; }
        public string RulesText { get; }
        public string ArtPrompt { get; }
        public Color ArtColor { get; }
        public Color AccentColor { get; }

        public CardMock(string name, string type, string target, string statLine, int energyCost, int diceCost, string rulesText, string artPrompt, Color artColor, Color accentColor)
        {
            Name = name;
            Type = type;
            Target = target;
            StatLine = statLine;
            EnergyCost = energyCost;
            DiceCost = diceCost;
            RulesText = rulesText;
            ArtPrompt = artPrompt;
            ArtColor = artColor;
            AccentColor = accentColor;
        }
    }
}
