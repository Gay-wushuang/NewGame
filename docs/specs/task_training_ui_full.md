# Trae CN 执行任务：训练场 UI 重构 + 抽牌/弃牌/消耗堆系统 + 卡牌预览重构

## 铁律（必须先读）

1. **严格遵循 `game_initial_concept.md`**：手牌中的卡牌双击 = 尝试打出（消耗资源），绝不是"进入手牌区"。抽牌/弃牌/消耗堆中的牌不可双击打出。
2. **抽牌由系统触发**：玩家不通过双击抽牌。抽牌由 `BattleManager` 回合开始自动调用。抽牌堆和弃牌堆仅显示牌数，不响应任何点击交互。消耗堆也仅显示牌数。
3. **不破坏核心结算**：严禁改动 `BattleManager.TryPlayCard` 内的原始攻击伤害计算（CalculateDamage、敌人 TakeDamage）、原始 `Energy` 扣除、`DiceRoller` 调用、破甲状态结算逻辑。允许改动以下位置（均为新增系统的扩展，不改变原有攻击卡结算）：
   - `DrawInitialHand`：战斗初始化时构建牌组、初始化抽牌堆。只建堆不抽牌。
   - `StartPlayerTurn`：回合开始时从抽牌堆抽牌 + NextTurnEnergyBonus 处理。
   - `EndPlayerTurn`：回合结束时弃掉全部手牌到弃牌堆。
   - `TryPlayCard` 中 `Player.PlayCard(card)` 调用处：替换为 `Player.MoveToDiscard(card)`（非消耗牌）或 `Player.MoveToExhaust(card)`（消耗牌）。
   - `TryPlayCard` 末尾：追加子类型效果处理（防御/增益/减益/消耗品/装备）。
   - `TryPlayCard` 攻击伤害计算后：追加武器加成（仅当 EquippedWeaponBonus > 0 时）。
   - 卡牌预览显示逻辑（`BattleUI.ShowCardPreview`）：可重构格式，不改变预览的内容数据。
   - `BattleUI.UpdateCardUI`：追加诅咒卡 Disabled 处理。
4. **训练场专属 Debug 功能**：浏览面板中"双击堆内牌移入手牌"仅在训练场生效，不要把这个逻辑写进 `BattleManager` 或 `PlayerState`。
5. **消耗堆是设计文档的扩展**：`card_subtype_system_design.md` 定义消耗品"使用后从手牌移除，不进入弃牌堆"，但未定义"消耗堆"概念。本提示词新增 `ExhaustPile` 作为消耗品去向的中间追踪状态，供训练场浏览和调试使用。战斗结束后消耗堆的清理由后续系统设计决定，不在本次任务范围内。

## 现有代码事实

阅读以下事实，避免破坏现有引用：

| 项 | 实际情况 |
|---|---|
| 场景根节点 | `TrainingGroundScene`（Control），脚本 = `TrainingGroundController.cs` |
| 三面板关系 | ConfigPanel、BattleUI、BattleLogPanel 是根节点的**直接子节点**，各自用锚点定位，**没有 HBoxContainer 包裹** |
| BattleUI 未铺满原因 | BattleUI 的 `offset_left = 200, offset_right = -310`，被 ConfigPanel 和 BattleLogPanel 挤在中间 |
| BattleUI 内部结构 | `Background(ColorRect)` → `TopPanel(Control)` → `EnemyArea(Panel)` → `DicePanel(Panel)` → `CardPanel(Panel)` → `EndTurnButton` → `BattleResultLabel` → `CardPreviewBackground(ColorRect)` + `CardPreviewLabel` |
| 手牌容器 | `BattleUI/CardPanel/CardContainer`（HBoxContainer） |
| PlayerState 现有字段 | `Hand`, `DicePool` — 没有 `Deck`/`DrawPile`/`DiscardPile`/`ExhaustPile`，需要新增 |
| CardData 消耗牌判定 | `card.Data.Category == CardCategory.Consumable` 为消耗牌 |
| QuickStrike 的 DiceCost | `DiceCost = 0`，当前 `BattleUI` 第 68 行不管 DiceCost 都会显示"掷出 X"——这是 bug |
| BattleManager 抽牌逻辑 | `DrawInitialHand()` 硬编码 3 张牌；`StartPlayerTurn()` 用 `while (Hand.Count < 3)` 从 5 张卡池循环填充 |
| 打牌后卡牌去向 | `Player.PlayCard(card)` 只做 `Hand.Remove(card)`，卡牌直接消失 |
| BattleLogPanel 方法 | 方法名是 `AddLog(string)` |
| TrainingGroundController 路径引用 | 通过 `GetNode<...>("ConfigPanel/ConfigScroll/ConfigContainer/...")` 引用 ConfigPanel 内控件；通过 `GetNode<BattleLogPanel>("BattleLogPanel")` 引用日志面板 — **移动面板后这些路径必须同步更新** |
| BattleUI 获取 BattleManager | `GetNode<BattleManager>("../BattleManager")` — 相对路径，BattleUI 不移动所以路径不变 |
| 卡牌交互 | BattleUI 动态创建 Button 展示手牌（不是 CardView 实例），单击预览、双击打出的逻辑在 `BattleUI.cs` 的 `OnCardGuiInput` / `OnCardSingleClicked` / `OnCardDoubleClicked` 中 |
| 卡牌预览控件 | `CardPreviewBackground(ColorRect)` + `CardPreviewLabel(Label)`，由 `ShowCardPreview` 方法填充文本并设置 `Visible = true` |

---

## 阶段一：BattleUI 铺满视图 + QuickStrike Bug 修复

### 1.1 BattleUI 铺满

**目标**：移除 ConfigPanel 和 BattleLogPanel 对 BattleUI 的空间挤压，让 BattleUI 撑满整个视口。

**执行步骤**：
1. 打开 `scenes/training/TrainingGroundScene.tscn`。
2. 将 `BattleUI` 的四个锚点偏移归零：`offset_left = 0, offset_top = 0, offset_right = 0, offset_bottom = 0`（当前 `anchors_preset` 已是 15/Full Rect，无需再设）。
3. **此阶段暂不移动 ConfigPanel 和 BattleLogPanel**——它们在阶段二会被移入 Overlay 层。此阶段只需让 BattleUI 的偏移归零，视觉上 BattleUI 会与 ConfigPanel/BattleLogPanel 重叠，这是预期的，阶段二会解决。
4. 确认 BattleUI 内部子节点布局不受影响：
   - `TopPanel`：锚点顶部，水平填充 — 不变。
   - `EnemyArea`：居中偏上 — 不变。
   - `DicePanel`：居中偏下 — 不变。
   - `CardPanel`：锚点底部，水平填充，高度 250px — 不变。
   - `EndTurnButton`：右下角 — 不变。

### 1.2 QuickStrike Bug 修复

**Bug 描述**：`BattleUI.cs` 第 68 行的 `OnCardPlayed` 方法中，`resultText` 不管 `DiceCost` 是多少都会拼接"掷出 {diceResult}"。QuickStrike 的 `DiceCost = 0`，`diceResult = 0`，显示"QuickStrike 掷出 0"。

**修复**：在 `OnCardPlayed` 方法中，根据 `diceResult` 参数判断。由于无骰卡牌 `consumedDice` 为 null，`diceResult` 传的是 `consumedDice?.Value ?? 0`。应改为：在 `BattleManager.TryPlayCard` 的信号发射处，无骰卡牌时 `diceResult` 传 `-1` 作为哨兵值，或在 `OnCardPlayed` 中根据 `cardId` 查 `CardData` 的 `DiceCost`。更简单的方式：**修改 `BattleManager.TryPlayCard` 的信号发射**：

```csharp
// 原代码第 113 行：
int diceResult = consumedDice?.Value ?? 0;
EmitSignal(SignalName.CardPlayed, card.Data.Id, finalDamage, diceResult, vulnerableAdded);

// 改为：
int diceResult = consumedDice?.Value ?? -1;
EmitSignal(SignalName.CardPlayed, card.Data.Id, finalDamage, diceResult, vulnerableAdded);
```

然后在 `BattleUI.OnCardPlayed` 中：
```csharp
string resultText;
if (diceResult < 0)
    resultText = $"{cardId} 造成 {damage} 伤害";
else
    resultText = $"{cardId} 掷出 {diceResult}，造成 {damage} 伤害";
```

这样 QuickStrike 显示"QuickStrike 造成 4 伤害"，无"掷出 0"。

**验收**：
- [ ] BattleUI 的 Background 覆盖整个 1280x720 视口。
- [ ] QuickStrike 打出后显示"QuickStrike 造成 4 伤害"，不显示"掷出"。
- [ ] EnergyStrike 打出后仍正常显示"掷出 X，造成 Y 伤害"。

---

## 阶段二：ConfigPanel 与 BattleLogPanel 转为浮窗

**目标**：两个面板从根节点直接子节点移入 Overlay 层，默认收起，点击按钮切换显隐，展开时阻断 BattleUI 交互。

### 2.1 场景结构调整

1. 在 `TrainingGroundScene` 根节点下新建 `CanvasLayer`，命名 `OverlayLayer`，`layer` 属性设为 10。
2. `OverlayLayer` 的子节点**按以下顺序添加**（Godot 中同层后添加的节点显示在上方）：
   1. `DimMask`（ColorRect）—— 最底层遮罩。
   2. `ConfigFloatingPanel`（原 ConfigPanel 内容移入）。
   3. `LogFloatingPanel`（原 BattleLogPanel 内容移入）。
   4. `DeckFloatingPanel`（PanelContainer）—— 卡包浏览面板，默认 `visible = false`。
   5. `ConfigToggleButton`（Button）—— 最顶层，确保遮罩可见时仍可点击。
   6. `LogToggleButton`（Button）—— 最顶层。
   7. `DeckToggleButton`（Button）—— 最顶层。
3. 将 `ConfigPanel` 节点移入 `OverlayLayer` 下，重命名为 `ConfigFloatingPanel`。内部子节点（ConfigScroll/ConfigContainer 及所有配置行）全部保留不动。
4. 将 `BattleLogPanel` 节点移入 `OverlayLayer` 下，重命名为 `LogFloatingPanel`。内部子节点（LogLabel）保留不动。
5. 两个面板默认 `visible = false`。
6. 切换按钮（`Button` 类型，不用 TextureButton）：
   - `ConfigToggleButton`：文字"配置"，位置在屏幕左上角。
   - `LogToggleButton`：文字"日志"，位置在 ConfigToggleButton 右侧。
   - `DeckToggleButton`：文字"牌包"，位置在 LogToggleButton 右侧。
   - 三个按钮始终 `visible = true`，不受面板显隐影响。

### 2.2 浮窗布局

面板展开时：
- `ConfigFloatingPanel`：锚点左侧，宽度 200px，从顶到底撑满。位置和尺寸与原 ConfigPanel 一致。
- `LogFloatingPanel`：锚点右侧，宽度 290px，从顶到底撑满。位置和尺寸与原 BattleLogPanel 一致。
- `DeckFloatingPanel`：居中，尺寸 600x450，`visible = false`。内部结构见阶段五 5.5。

### 2.3 遮罩阻断逻辑

1. `DimMask`（ColorRect）已在步骤 2.1 中创建为 OverlayLayer 第一个子节点。颜色 `Color(0, 0, 0, 0.5)`，锚点 Full Rect，`mouse_filter = MouseFilterStop`。
2. `DimMask` 默认 `visible = false`。
3. 面板展开时：`DimMask.Visible = true`。DimMask 在 CanvasLayer（layer 10）中，高于 BattleUI（默认 layer 0），且 `MouseFilterStop` 拦截所有对 BattleUI 的点击。面板和切换按钮在 DimMask 之上（场景树中靠后），仍可正常交互。
4. 面板收起时：`DimMask.Visible = false`。
5. 点击 DimMask 也关闭当前展开的面板。
6. **严禁修改 BattleUI 的 `process_mode` 或 `mouse_filter`**。阻断完全由 DimMask 的 MOUSE_FILTER_STOP 实现。

### 2.4 脚本改动

在 `TrainingGroundController.cs` 中：
1. 更新所有 `GetNode` 路径：`"ConfigPanel/..."` → `"OverlayLayer/ConfigFloatingPanel/..."`。
2. 更新 `GetNode<BattleLogPanel>("BattleLogPanel")` → `GetNode<BattleLogPanel>("OverlayLayer/LogFloatingPanel")`。
3. 新增对 `ConfigToggleButton`、`LogToggleButton`、`DeckToggleButton`、`DimMask` 的引用。
4. 实现切换逻辑：
   - 点击 ConfigToggleButton：切换 ConfigFloatingPanel 显隐，同时切换 DimMask。
   - 点击 LogToggleButton：切换 LogFloatingPanel 显隐，同时切换 DimMask。
   - 点击 DeckToggleButton：切换 DeckFloatingPanel 显隐，填充卡包列表，同时切换 DimMask。
   - 点击 DimMask：隐藏所有当前展开的面板和 DimMask。
   - 三个面板互斥：展开一个时自动收起其他两个。

**验收**：
- [ ] 左上角有"配置""日志""牌包"三个按钮，始终可见。
- [ ] 点击"配置"，配置面板从左侧展开，背景出现半透明遮罩，手牌无法点击。
- [ ] 再次点击"配置"或点击遮罩，面板收起，手牌恢复点击。
- [ ] 点击"日志"，日志面板从右侧滑出，同样有遮罩和阻断。
- [ ] 点击"牌包"，居中显示卡包浏览面板，同样有遮罩和阻断。
- [ ] 三个面板互斥：展开一个时自动收起其他两个。
- [ ] ConfigPanel 内所有配置项功能正常。
- [ ] BattleLogPanel 的 `AddLog` 方法被正常调用，日志显示正确。
- [ ] 重置战斗后日志清空并重新写入。

---

## 阶段三：抽牌/弃牌/消耗堆系统

### 3.1 数据模型（PlayerState.cs）

在 `scripts/core/PlayerState.cs` 中新增以下字段和方法：

**新增字段**：
```csharp
public List<CardInstance> Deck = new List<CardInstance>();        // 战斗开始时的完整牌组（静态源）
public List<CardInstance> DrawPile = new List<CardInstance>();    // 抽牌堆
public List<CardInstance> DiscardPile = new List<CardInstance>(); // 弃牌堆
public List<CardInstance> ExhaustPile = new List<CardInstance>(); // 消耗堆
public int MaxHandSize = 10;                                      // 手牌上限

// 以下为测试新卡牌类型的最小支持字段（简化实现，后续可扩展为完整系统）
public int Shield = 0;              // 护盾值，优先于 Energy 承受伤害（EnergyBarrier 用）
public int NextTurnEnergyBonus = 0; // 下回合额外恢复的 Energy（Adrenaline 用，简化 Buff 系统）
public int EquippedWeaponBonus = 0; // 武器伤害加成（IronSword 用，简化装备系统）
```

保留现有 `Hand` 字段不变。

**新增方法**：
```csharp
// 将 Deck 浅拷贝到 DrawPile 并洗牌（战斗初始化调用）
public void InitDrawPileFromDeck()
{
    DrawPile.Clear();
    DiscardPile.Clear();
    ExhaustPile.Clear();
    foreach (var card in Deck)
        DrawPile.Add(new CardInstance(card.Data));
    ShuffleDrawPile();
}

// Fisher-Yates 洗牌
public void ShuffleDrawPile()
{
    var rng = new System.Random();
    for (int i = DrawPile.Count - 1; i > 0; i--)
    {
        int j = rng.Next(i + 1);
        (DrawPile[i], DrawPile[j]) = (DrawPile[j], DrawPile[i]);
    }
}

// 将弃牌堆洗入抽牌堆
public void ShuffleDiscardIntoDraw()
{
    foreach (var card in DiscardPile)
        DrawPile.Add(card);
    DiscardPile.Clear();
    ShuffleDrawPile();
}

// 从抽牌堆抽牌到手牌，返回实际抽出的牌数
public int DrawCards(int count)
{
    int drawn = 0;
    for (int i = 0; i < count; i++)
    {
        if (Hand.Count >= MaxHandSize)
            break;

        if (DrawPile.Count == 0)
        {
            if (DiscardPile.Count == 0)
                break;          // 两堆都空，无法继续抽
            ShuffleDiscardIntoDraw();
        }

        var card = DrawPile[DrawPile.Count - 1];
        DrawPile.RemoveAt(DrawPile.Count - 1);
        Hand.Add(card);
        drawn++;
    }
    return drawn;
}

// 弃掉所有手牌到弃牌堆（回合结束调用）
public void DiscardHand()
{
    foreach (var card in Hand)
        DiscardPile.Add(card);
    Hand.Clear();
}

// 将单张手牌移入弃牌堆（非消耗牌打出后调用）
public void MoveToDiscard(CardInstance card)
{
    Hand.Remove(card);
    DiscardPile.Add(card);
}

// 将单张手牌移入消耗堆（消耗牌打出后调用）
public void MoveToExhaust(CardInstance card)
{
    Hand.Remove(card);
    ExhaustPile.Add(card);
}

// TakeDamage 修改：先扣 Shield，再扣 Energy，最后扣 HP
public void TakeDamage(int damage)
{
    int vulnerable = VulnerableStacks;
    int totalDamage = damage + vulnerable;

    // 先扣 Shield
    int shieldDamage = Mathf.Min(totalDamage, Shield);
    Shield -= shieldDamage;
    totalDamage -= shieldDamage;

    // 再扣 Energy
    int energyDamage = Mathf.Min(totalDamage, Energy);
    Energy -= energyDamage;

    // 最后扣 HP
    int hpDamage = totalDamage - energyDamage;
    Hp -= hpDamage;
    if (Hp < 0) Hp = 0;
}
```

**CardInstance 新增字段**：在 `scripts/core/CardInstance.cs` 中新增：
```csharp
public int RemainingUses;  // 实例级使用次数（消耗品用，构造时从 CardData.UsesPerBattle 复制）
```
构造函数修改为：
```csharp
public CardInstance(CardData data)
{
    Data = data;
    RemainingUses = data.UsesPerBattle;
}
```

保留现有 `PlayCard(CardInstance card)` 方法签名不变（它只做 `Hand.Remove(card)`），但 `BattleManager` 调用处不再使用它。

### 3.1 之一：测试卡牌数据定义

依据 `card_subtype_system_design.md` 的 7 个类别，在 `CardData.cs` 中新增以下 7 张测试卡牌（保留原有 5 张不变）：

```csharp
// 1. 基础卡牌 - 攻击（沿用现有攻击机制，高费高伤）
public static CardData HeavyStrike = new CardData()
{
    Id = "heavy_strike", Name = "HeavyStrike",
    Description = "消耗 2 Energy 和 1 枚默认骰；造成 骰点 + 4 伤害",
    Type = CardType.Attack, Category = CardCategory.Basic, Subtype = CardSubtype.Attack,
    Target = TargetType.Enemy, EnergyCost = 2, DiceCost = 1,
    DamageFormula = (card, dice) => dice.Value.GetValueOrDefault() + 4,
    VisualKey = "attack_hammer", BorderColor = "#FF4444"
};

// 2. 基础卡牌 - 防御（新增最小护盾系统）
public static CardData EnergyBarrier = new CardData()
{
    Id = "energy_barrier", Name = "EnergyBarrier",
    Description = "消耗 2 Energy；获得 5 点护盾，持续 2 回合",
    Type = CardType.Skill, Category = CardCategory.Basic, Subtype = CardSubtype.Defense,
    Target = TargetType.Player, EnergyCost = 2, DiceCost = 0,
    ShieldValue = 5, Duration = 2,
    VisualKey = "defense_shield", BorderColor = "#4444FF"
};

// 3. 技能卡牌 - 增益（简化 Buff 系统）
public static CardData Adrenaline = new CardData()
{
    Id = "adrenaline", Name = "Adrenaline",
    Description = "消耗 1 Energy；下回合开始恢复 3 Energy，持续 2 回合",
    Type = CardType.Skill, Category = CardCategory.Skill, Subtype = CardSubtype.PositiveBuff,
    Target = TargetType.Player, EnergyCost = 1, DiceCost = 0,
    AppliedBuffType = BuffType.EnergyRegen, EffectAmount = 3, Duration = 2,
    VisualKey = "buff_energy", BorderColor = "#44FF44"
};

// 4. 技能卡牌 - 减益（使用现有 Enemy Status 系统）
public static CardData WeakPulse = new CardData()
{
    Id = "weak_pulse", Name = "WeakPulse",
    Description = "消耗 2 Energy 和 1 枚默认骰；造成 3 伤害；施加 2 层 Weak，持续 2 回合",
    Type = CardType.Skill, Category = CardCategory.Skill, Subtype = CardSubtype.NegativeBuff,
    Target = TargetType.Enemy, EnergyCost = 2, DiceCost = 1,
    DamageFormula = (card, dice) => 3,
    AppliedDebuffType = DebuffType.Weak, EffectAmount = 2, Duration = 2,
    VisualKey = "debuff_weak", BorderColor = "#AA44FF"
};

// 5. 战斗级消耗品（消耗品效果 + 使用次数追踪）
public static CardData EnergyPotion = new CardData()
{
    Id = "energy_potion", Name = "EnergyPotion",
    Description = "恢复 5 Energy；每场战斗限用 2 次",
    Type = CardType.Skill, Category = CardCategory.Consumable, Subtype = CardSubtype.BattleLevelConsumable,
    Target = TargetType.Player, EnergyCost = 0, DiceCost = 0,
    UsesPerBattle = 2,
    VisualKey = "consumable_potion", BorderColor = "#FF8800"
};

// 6. 装备（简化装备系统：武器加成）
public static CardData IronSword = new CardData()
{
    Id = "iron_sword", Name = "IronSword",
    Description = "消耗 3 Energy；装备武器，攻击伤害 +2，持续 3 场战斗",
    Type = CardType.Power, Category = CardCategory.Other, Subtype = CardSubtype.Equipment,
    Target = TargetType.Player, EnergyCost = 3, DiceCost = 0,
    EquipSlot = EquipmentSlot.Weapon, EffectAmount = 2, Duration = 3, IsPermanent = false,
    VisualKey = "equip_sword", BorderColor = "#CCCCCC"
};

// 7. 诅咒（简化诅咒系统：抽到后无法打出）
public static CardData Clumsy = new CardData()
{
    Id = "clumsy", Name = "Clumsy",
    Description = "手牌上限 -1；抽到后无法打出；打出 3 张牌后移除",
    Type = CardType.Power, Category = CardCategory.Other, Subtype = CardSubtype.Curse,
    Target = TargetType.Player, EnergyCost = 0, DiceCost = 0,
    AppliedCurseType = CurseType.HandSizeReduction, EffectValue = 1,
    RemovalCondition = "打出 3 张牌", IsRemovedOnDiscard = false,
    VisualKey = "curse_clumsy", BorderColor = "#222222"
};
```

注意：上述卡牌数据需与现有 `CardData` 字段兼容。若 `CardData` 中缺少某些字段（如 `EquipSlot`、`AppliedCurseType` 等），需在 `CardData.cs` 中先新增这些字段（仅作数据占位，不改动已有字段）。

---

### 3.2 回合流程集成（BattleManager.cs）

**仅改动以下五处，严禁触碰 `TryPlayCard` 内的结算逻辑**：

1. **`DrawInitialHand()`**：替换为构建 Deck 并初始化抽牌堆。**不在此处抽牌**——`InitializeBattle` 在 `DrawInitialHand()` 之后立即调用 `StartPlayerTurn()`，而 `StartPlayerTurn()` 会调用 `DrawCards(3)`。
```csharp
private void DrawInitialHand()
{
    // 构建测试牌组：12 张（原有 5 种攻击卡各 1 张 + 新测试卡 7 张各 1 张）
    Player.Deck.Clear();
    var cardPool = new List<CardData> {
        // 原有攻击卡
        CardData.EnergyStrike, CardData.BreakCore,
        CardData.QuickStrike, CardData.VulnerableStrike, CardData.CriticalHit,
        // 新测试卡（7 个类别各 1 张）
        CardData.HeavyStrike, CardData.EnergyBarrier,
        CardData.Adrenaline, CardData.WeakPulse,
        CardData.EnergyPotion, CardData.IronSword,
        CardData.Clumsy
    };
    foreach (var cardData in cardPool)
        Player.Deck.Add(new CardInstance(cardData));
    Player.InitDrawPileFromDeck();
    // 不调用 DrawCards —— 由 StartPlayerTurn 统一抽牌
}
```

2. **`StartPlayerTurn()`**：删除现有的 `while (Player.Hand.Count < 3)` 硬编码填充块，替换为从抽牌堆抽牌。
```csharp
// 删除这段：
//   var cardPool = new List<CardData> { ... };
//   while (Player.Hand.Count < 3) { ... }

// 替换为：
int drawn = Player.DrawCards(3);
if (drawn > 0)
    EmitSignal(SignalName.BattleLog, $"抽牌: {drawn} 张");

保留该位置之前的 `RestoreEnergy`、`RefreshDicePool` 和之后的 `EmitSignal` 不变。

**新增：在 `RestoreEnergy` 调用之前，添加 NextTurnEnergyBonus 处理**：
```csharp
// 在 RestoreEnergy 之前插入：
if (Player.NextTurnEnergyBonus > 0)
{
    Player.RestoreEnergy(Player.NextTurnEnergyBonus);
    EmitSignal(SignalName.BattleLog, $"Adrenaline 触发: 额外恢复 {Player.NextTurnEnergyBonus} Energy");
    Player.NextTurnEnergyBonus = 0;  // 重置
}
```

3. **`EndPlayerTurn()`**：在 `CallDeferred(nameof(ExecuteEnemyTurn))` 之前追加弃牌。
```csharp
public void EndPlayerTurn()
{
    IsPlayerTurn = false;
    Player.DiscardHand();
    EmitSignal(SignalName.PlayerTurnEnded);
    CallDeferred(nameof(ExecuteEnemyTurn));
}
```

4. **`TryPlayCard()`**：将 `Player.PlayCard(card);` 替换为消耗判断 + 对应移动方法。
```csharp
// 原代码：
// Player.PlayCard(card);

// 替换为：
if (card.Data.Category == CardCategory.Consumable)
{
    // 当前阶段只按 Category 判定，不区分子类型。
    // 未来若需区分 GameLevelConsumable（全局使用次数）和 BattleLevelConsumable（本场使用次数），
    // 需按 card.Data.Subtype 分支处理。详见阶段六。
    Player.MoveToExhaust(card);
}
else
    Player.MoveToDiscard(card);
```
`MoveToDiscard` 和 `MoveToExhaust` 内部已包含 `Hand.Remove` + 移入对应堆，效果等价但卡牌进入正确位置。**不要保留原来的 `PlayCard` 调用，也不要再追加 `DiscardPile.Add` 或 `ExhaustPile.Add`**。不得改动该行前后的任何结算代码。

**新增：在移动方法之前，追加子类型效果处理（最小可测系统）**：
```csharp
// 在 "if (card.Data.Category == CardCategory.Consumable)" 判断之前插入：
switch (card.Data.Subtype)
{
    case CardSubtype.Defense:
        Player.Shield += card.Data.ShieldValue;
        EmitSignal(SignalName.BattleLog, $"获得护盾: {card.Data.ShieldValue}");
        break;
    case CardSubtype.PositiveBuff:
        Player.NextTurnEnergyBonus += card.Data.EffectAmount;
        EmitSignal(SignalName.BattleLog, $"增益: 下回合恢复 {card.Data.EffectAmount} Energy");
        break;
    case CardSubtype.NegativeBuff:
        if (card.Data.AppliedDebuffType == DebuffType.Weak)
            Enemy.AddWeak(card.Data.EffectAmount);
        break;
    case CardSubtype.BattleLevelConsumable:
        if (card.Data.Id == "energy_potion" && card.RemainingUses > 0)
        {
            card.RemainingUses--;
            Player.RestoreEnergy(5);
            EmitSignal(SignalName.BattleLog, "恢复 5 Energy");
        }
        break;
    case CardSubtype.Equipment:
        if (card.Data.EquipSlot == EquipmentSlot.Weapon)
        {
            Player.EquippedWeaponBonus = card.Data.EffectAmount;
            EmitSignal(SignalName.BattleLog, $"装备武器: 攻击伤害 +{card.Data.EffectAmount}");
        }
        break;
}
```

**新增：在攻击伤害计算中追加武器加成**：
```csharp
// 在 "int baseDamage = card.CalculateDamage(consumedDice);" 之后插入：
if (Player.EquippedWeaponBonus > 0 && card.Data.Subtype == CardSubtype.Attack)
{
    baseDamage += Player.EquippedWeaponBonus;
    EmitSignal(SignalName.BattleLog, $"武器加成: +{Player.EquippedWeaponBonus}");
}
```

5. **信号发射修复**（同阶段一 1.2）：`diceResult` 传 `-1` 作为无骰哨兵值。

### 3.3 UI 实现（BattleUI.cs + TrainingGroundScene.tscn）

**在 `BattleUI/CardPanel` 内部调整布局**：

当前 CardPanel 内是 `CardLabel` + `CardContainer`。改为：
```
CardPanel (Panel)
├── CardLabel (Label, "手牌")
└── PileRow (HBoxContainer, anchor Full Rect, 内边距 10px)
    ├── DrawPileView (VBoxContainer, custom_minimum_size = 80x100, mouse_filter = Stop)
    │   ├── DrawPileBg (ColorRect, anchor Full Rect, color = Color(0.1, 0.15, 0.4), mouse_filter = Pass)
    │   ├── DrawPileLabel (Label, "抽牌堆", mouse_filter = Pass)
    │   └── DrawPileCount (Label, "0", mouse_filter = Pass)
    ├── CardContainer (HBoxContainer, size_flags_horizontal = Expand+Fill)
    ├── DiscardPileView (VBoxContainer, custom_minimum_size = 80x100, mouse_filter = Stop)
    │   ├── DiscardPileBg (ColorRect, anchor Full Rect, color = Color(0.4, 0.1, 0.1), mouse_filter = Pass)
    │   ├── DiscardPileLabel (Label, "弃牌堆", mouse_filter = Pass)
    │   └── DiscardPileCount (Label, "0", mouse_filter = Pass)
    └── ExhaustPileView (VBoxContainer, custom_minimum_size = 80x100, mouse_filter = Stop)
        ├── ExhaustPileBg (ColorRect, anchor Full Rect, color = Color(1.0, 0.53, 0.0), mouse_filter = Pass)
        ├── ExhaustPileLabel (Label, "消耗堆", mouse_filter = Pass)
        └── ExhaustPileCount (Label, "0", mouse_filter = Pass)
```

**关键结构要求**：
- 每个 PileView 内部的 ColorRect（Bg）必须 `anchor_preset = Full Rect`（或 `anchors_preset = 15`），填满整个 VBoxContainer。
- Bg、Label 的 `mouse_filter = Pass`，让点击事件顺利冒泡到 PileView（VBoxContainer）的 `Stop` 层。
- **严禁**给内部子节点设 `Stop`，否则事件会被子节点吃掉，PileView 收不到点击。

**交互**：三个 PileView 的 `mouse_filter = MouseFilterStop`，响应单击（用于打开浏览面板）。不是 `Ignore`。

**BattleUI.cs 改动**：
1. 新增字段引用 `DrawPileCount`、`DiscardPileCount`、`ExhaustPileCount`。
2. 新增字段引用三个 PileView：`DrawPileView`、`DiscardPileView`、`ExhaustPileView`。
3. 新增信号：
   ```csharp
   [Signal]
   public delegate void PileClickedEventHandler(string pileName);
   ```
4. 在 `_Ready()` 中获取引用（CardContainer 路径更新为 `"CardPanel/PileRow/CardContainer"`），并连接 PileView 的 `GuiInput` 事件：
   ```csharp
   _drawPileView = GetNode<Control>("CardPanel/PileRow/DrawPileView");
   _discardPileView = GetNode<Control>("CardPanel/PileRow/DiscardPileView");
   _exhaustPileView = GetNode<Control>("CardPanel/PileRow/ExhaustPileView");

   _drawPileView.GuiInput += (InputEvent @event) => OnPileGuiInput(@event, "DrawPile");
   _discardPileView.GuiInput += (InputEvent @event) => OnPileGuiInput(@event, "DiscardPile");
   _exhaustPileView.GuiInput += (InputEvent @event) => OnPileGuiInput(@event, "ExhaustPile");
   ```
5. 新增 PileView 点击处理方法：
   ```csharp
   private void OnPileGuiInput(InputEvent @event, string pileName)
   {
       if (@event is InputEventMouseButton mouseEvent &&
           mouseEvent.ButtonIndex == MouseButton.Left &&
           mouseEvent.Pressed)
       {
           EmitSignal(SignalName.PileClicked, pileName);
       }
   }
   ```
6. 在 `UpdateUI()` 中追加调用 `UpdatePileUI()`。
7. 新增方法：
   ```csharp
   private void UpdatePileUI()
   {
       if (_battleManager.Player == null) return;
       _drawPileCount.Text = _battleManager.Player.DrawPile.Count.ToString();
       _discardPileCount.Text = _battleManager.Player.DiscardPile.Count.ToString();
       _exhaustPileCount.Text = _battleManager.Player.ExhaustPile.Count.ToString();
   }
   ```
8. **`UpdateUI()` 改为 `public`**：供外部（TrainingGroundController/CardPileBrowser）在牌堆变动后调用刷新。

**交互不变**：`CardContainer` 内的手牌仍用动态 Button 展示，单击预览、双击打出逻辑完全不变。

**新增：诅咒卡处理（简化）**：
在 `BattleUI.UpdateCardUI()` 中，为每张手牌创建 Button 后，检查卡牌子类型：
```csharp
if (card.Data.Subtype == CardSubtype.Curse)
{
    btn.Disabled = true;  // 诅咒卡无法点击（既不能预览也不能打出）
    btn.Modulate = new Color(0.5f, 0.5f, 0.5f);  // 变灰显示
}
```
在 `BattleUI.OnCardDoubleClicked` 中，开头增加过滤：
```csharp
if (card.Data.Subtype == CardSubtype.Curse)
    return;  // 诅咒卡双击直接返回，不尝试打出
```

---

## 阶段四：卡牌预览三栏格式重构

**目标**：将现有的卡牌预览从单 Label 文本改为三栏结构。

### 4.1 预览面板节点重构

当前：
```
CardPreviewBackground (ColorRect)
CardPreviewLabel (Label)
```

改为：
```
CardPreviewPanel (PanelContainer, 替代 CardPreviewBackground)
└── PreviewVBox (VBoxContainer, 填充内边距 12px)
    ├── CostRow (HBoxContainer)                    // 第1栏：消耗
    │   ├── EnergyCostLabel (Label)
    │   └── DiceCostLabel (Label)
    ├── EffectRow (HBoxContainer)                  // 第2栏：效果范围
    │   ├── DamageRangeLabel (Label)
    │   └── EffectLabel (Label)
    └── DescriptionLabel (Label, autowrap = true)  // 第3栏：说明
```

- `CardPreviewPanel` 替代 `CardPreviewBackground`，位置在 `CardPanel` 上方（`offset_left = 20, offset_top = 280, offset_right = 360, offset_bottom = 470`）。
- **关键：`CardPreviewPanel` 必须作为 `BattleUI` 的直接子节点（不要在 `CardPanel` 内部），且放在场景树最末尾，确保绘制顺序在 `CardPanel` 上方**，不被手牌区遮挡。
- `CardPreviewPanel` 默认 `visible = false`。当 `ShowCardPreview` 时设为 `visible = true`。
- 原 `CardPreviewLabel` 节点删除，不再使用。

### 4.2 三栏内容定义

**第1栏 — 消耗**：显示打出该牌需要消耗的资源。
```
EnergyCostLabel:  "Energy: {EnergyCost}"
DiceCostLabel:    若 DiceCost > 0 显示 "Dice: {DiceCost} {DiceType}"；若 DiceCost == 0 显示 "Dice: 无需"
```

**第2栏 — 效果范围**：按子类型分支显示，覆盖 `card_subtype_system_design.md` 中定义的全部子类型。
```
DamageRangeLabel: 若 DamageFormula != null，显示 "伤害: {minDamage} ~ {maxDamage}"；否则显示 "伤害: 无"

EffectLabel 分支逻辑：
- 若 Subtype == Defense:     显示 "护盾: {ShieldValue} / 持续: {Duration}回合"
- 若 Subtype == Dodge:       显示 "闪避率: {EvasionRate}% / 反击: {CounterDamage} / 持续: {Duration}回合"
- 若 Subtype == PositiveBuff: 显示 "增益: {AppliedBuffType} ({EffectAmount}) / 持续: {Duration}回合"
- 若 Subtype == NegativeBuff: 显示 "减益: {AppliedDebuffType} ({EffectAmount}) / 持续: {Duration}回合"
- 若 Subtype == BattleLevelConsumable:
                              显示 "消耗品效果: {EffectType} / 本场剩余: {UsesPerBattle}次"
- 若 Subtype == GameLevelConsumable:
                              显示 "消耗品效果: {EffectType} / 全局剩余: {MaxUsage}次"
- 若 Subtype == Equipment:   显示 "装备槽: {EquipmentSlot} / 加成: +{EffectValue} / 持续: {Duration}场"
- 若 Subtype == Curse:       显示 "诅咒: {AppliedCurseType} / 移除条件: {RemovalCondition}"
- 否则:                      显示 "效果: 无"
```

**第3栏 — 说明**：显示 `card.Data.Description` 原文。

### 4.3 ShowCardPreview 方法重构

重写 `BattleUI.ShowCardPreview`：
1. 通过 `card.Data` 获取所有需要的数据。
2. 调用 `card.Data.GetDamageRange(_battleManager.Player.DiceSides, out min, out max)` 获取伤害范围。
3. 设置三个 Label 的文本。
4. `CardPreviewPanel.Visible = true`。

**删除** `BattleUI.cs` 中现有的 `ShowCardPreview` 方法内对 `break_core` 的特殊硬编码逻辑（第 281-284 行）。`break_core` 的说明应在 `CardData.Description` 中统一维护，不应在 UI 代码中硬编码。

**删除**原 `CardPreviewBackground` 和 `CardPreviewLabel` 的引用字段，改为引用 `CardPreviewPanel` 及其内部三个 Label。

**更新所有隐藏预览的地方**：`OnPlayerTurnStarted`、`OnCardPlayed`、`OnEnemyAttacked`、`OnBattleWon`、`OnBattleLost`、`HideCardPreview` 中，`CardPreviewPanel.Visible = false`。不再操作 `CardPreviewBackground` 和 `CardPreviewLabel`。

---

## 阶段五：堆内浏览面板 + 训练场 Debug 交互

### 5.1 浏览面板场景

新建场景 `scenes/ui/CardPileBrowser.tscn`，根节点为 `Control`（全屏，锚点 Full Rect）：
```
CardPileBrowser (Control, anchors_preset = 15, Full Rect)
├── BackgroundDim (ColorRect, 全屏, color = Color(0,0,0,0.5), mouse_filter = Stop)
└── BrowserPanel (PanelContainer, 居中, custom_minimum_size = 600x400)
    └── BrowserVBox (VBoxContainer, 填充内边距 12px)
        ├── TitleLabel (Label, 标题如"抽牌堆")
        ├── ContentHBox (HBoxContainer, size_flags_vertical = Expand)
        │   ├── CardScroll (ScrollContainer, size_flags_horizontal = Expand)
        │   │   └── CardList (VBoxContainer)
        │   │       └── [动态生成的 Button]
        │   └── MiniPreviewPanel (PanelContainer, custom_minimum_size = 200x0)
        │       └── MiniPreviewVBox (VBoxContainer, 填充内边距 8px)
        │           ├── MiniCostLabel (Label)
        │           ├── MiniEffectLabel (Label)
        │           └── MiniDescLabel (Label, autowrap = true)
        └── CloseButton (Button, "关闭", size_flags_horizontal = Center)
```

注意：`PanelContainer`（BrowserPanel）只能有一个直接子节点，所以内部必须包一个 `VBoxContainer`（BrowserVBox）。

脚本 `scripts/ui/CardPileBrowser.cs`：

**新增信号**：
```csharp
[Signal]
public delegate void CardMovedEventHandler();  // 牌被移入手牌时发射，通知外部刷新 UI
```

**核心方法**：
- `void OpenPile(string title, PlayerState player, List<CardInstance> pile, bool allowDoubleClickToHand = false)`：打开面板。
  - 设置 `Visible = true`。
  - 清空 `CardList` 并重新生成 Button（显示卡牌名称 + EnergyCost + DiceCost）。
  - 存储 `player` 和 `pile` 为字段供后续使用。
- `void Close()`：设置 `Visible = false`；清空 `CardList`；清空 `MiniPreviewPanel`。
- **单击牌**：填充 `MiniPreviewPanel` 的三栏内容（复用阶段四的格式逻辑）。
- **双击牌**：如果 `allowDoubleClickToHand = true`：
  1. 检查 `player.Hand.Count < player.MaxHandSize`，否则返回。
  2. `pile.Remove(card); player.Hand.Add(card);`
  3. 从 `CardList` 中移除对应 Button。
  4. 发射 `EmitSignal(SignalName.CardMoved)`。
  5. 如果 `CardList` 为空，自动 `Close()`。
- **关闭按钮**：调用 `Close()`。
- **点击 BackgroundDim**：调用 `Close()`。

### 5.2 训练场专属逻辑

在 `TrainingGroundController.cs` 中：

1. 实例化 `CardPileBrowser`（可在 `_Ready` 中 `new CardPileBrowser()` 并 `AddChild`）。
2. 连接 BattleUI 的 `PileClicked` 信号到处理方法：
   ```csharp
   var battleUI = GetNode<BattleUI>("BattleUI");
   battleUI.PileClicked += OnPileClicked;
   ```
3. 实现 `OnPileClicked(string pileName)`：
   ```csharp
   private void OnPileClicked(string pileName)
   {
       switch (pileName)
       {
           case "DrawPile":
               _browser.OpenPile("抽牌堆", _battleManager.Player, _battleManager.Player.DrawPile, true);
               break;
           case "DiscardPile":
               _browser.OpenPile("弃牌堆", _battleManager.Player, _battleManager.Player.DiscardPile, true);
               break;
           case "ExhaustPile":
               _browser.OpenPile("消耗堆", _battleManager.Player, _battleManager.Player.ExhaustPile, true);
               break;
       }
   }
   ```
4. 连接 CardPileBrowser 的 `CardMoved` 信号到 BattleUI 刷新：
   ```csharp
   _browser.CardMoved += () => battleUI.UpdateUI();
   ```
5. 浏览面板中的双击进手牌逻辑，**只在训练场通过 `allowDoubleClickToHand = true` 开启**。

**注意**：`CardPileBrowser` 本身是通用组件，可以在任何场景复用。训练场通过传参 `allowDoubleClickToHand = true` 开启 Debug 功能，正式场景传 `false`。

### 5.3 牌效预览在浏览面板中的复用

已在 5.1 中内嵌 `MiniPreviewPanel`（PanelContainer + MiniPreviewVBox + 三个 Label）。单击 CardList 中的某张牌时，填充三栏内容。不需要复用 BattleUI 的 `ShowCardPreview`，因为浏览器是独立面板。

### 5.4 堆牌移入手牌的边界检查

在 `CardPileBrowser` 的双击处理中：
```csharp
if (allowDoubleClickToHand)
{
    if (_player.Hand.Count >= _player.MaxHandSize)
    {
        // 可选：显示提示"手牌已满"
        return;
    }
    _pile.Remove(card);
    _player.Hand.Add(card);
    RefreshUI();
}
```

**验收**：
- [ ] 手牌区左侧显示抽牌堆（深蓝 + 牌数），右侧依次显示弃牌堆（深红）和消耗堆（紫色）。
- [ ] 战斗开始时抽牌堆有 10 张牌，手牌抽 5 张。
- [ ] 回合开始自动从抽牌堆抽 5 张牌到手牌。
- [ ] 打出非消耗牌后卡牌进入弃牌堆。
- [ ] 打出消耗牌（Category == Consumable）后卡牌进入消耗堆。
- [ ] 回合结束时手牌全部进入弃牌堆。
- [ ] 抽牌堆耗尽时自动将弃牌堆洗入抽牌堆。
- [ ] 消耗堆不参与洗入，永久移出战斗循环。
- [ ] 单击抽牌堆/弃牌堆/消耗堆，弹出浏览面板显示该堆内所有卡牌。
- [ ] 浏览面板内单击某张牌，显示三栏牌效预览。
- [ ] 训练场中：浏览面板内双击某张牌，该牌移入手牌，手牌区实时更新。
- [ ] 手牌已满时双击堆内牌不进手牌（或给出提示）。
- [ ] 浏览面板有"关闭"按钮和点击背景关闭两种方式。
- [ ] 卡牌预览三栏格式正确：消耗栏显示 Energy/Dice，效果栏显示伤害/增益/减益范围，说明栏显示描述。
- [ ] QuickStrike 预览中 DiceCost 显示"无需"，不显示"掷出"。
- [ ] EnergyStrike 预览正常显示伤害范围（3~8，假设 d6）。
- [ ] BreakCore 预览不依赖 BattleUI 内的硬编码逻辑，从 `CardData.Description` 读取。
- [ ] 手牌单击预览、双击打出逻辑完全正常。
- [ ] 训练场原有功能不受影响。
- [ ] 初始牌组包含 12 张牌（5 张原有攻击卡 + 7 张新测试卡）。
- [ ] HeavyStrike（攻击）：消耗 2 Energy + 1 骰，造成骰点 + 4 伤害，可正常打出和预览。
- [ ] EnergyBarrier（防御）：消耗 2 Energy，打出后 Player.Shield 增加 5，日志显示"获得护盾: 5"。
- [ ] 敌人攻击时，优先扣除 Player.Shield，Shield 为 0 后再扣 Energy/HP。
- [ ] Adrenaline（增益）：消耗 1 Energy，打出后下回合开始额外恢复 3 Energy，日志显示触发。
- [ ] WeakPulse（减益）：消耗 2 Energy + 1 骰，造成 3 伤害，敌人获得 2 层 Weak。
- [ ] 敌人 Weak 生效：攻击时伤害减少对应层数。
- [ ] EnergyPotion（消耗品）：消耗 0 Energy，恢复 5 Energy，每场战斗限用 2 次（RemainingUses 递减）。
- [ ] EnergyPotion 使用 2 次后 RemainingUses = 0，再次尝试打出时应失败（或提示次数不足）。
- [ ] IronSword（装备）：消耗 3 Energy，打出后 EquippedWeaponBonus = 2，后续攻击卡伤害 +2。
- [ ] Clumsy（诅咒）：抽到后手牌中显示为灰色 Disabled，单击无反应，双击不尝试打出。
- [ ] 全部 7 张新卡的预览三栏格式正确显示（消耗/效果/说明）。
- [ ] 消耗品卡牌打出后进入消耗堆（ExhaustPile），UI 牌数正确更新。
- [ ] 浏览面板内可查看全部 12 张卡，单击显示正确预览，双击可移入手牌（训练场专属）。
- [ ] 卡牌预览面板在场景树最末尾，z-order 高于手牌区，预览不被手牌 Button 遮挡。
- [ ] 点击"牌包"按钮，居中显示卡包浏览面板，显示牌包中全部 12 张卡牌，标题显示"卡包 (共 12 张)"。
- [ ] 卡包浏览面板内单击牌，显示三栏预览（消耗/效果/说明）。
- [ ] 卡包浏览面板内双击牌不执行任何操作。
- [ ] 点击卡包面板的关闭按钮或点击 DimMask 遮罩，面板关闭。
- [ ] 三个浮窗按钮（配置/日志/牌包）互斥工作正常。

### 5.5 整体卡包浏览面板（DeckFloatingPanel）

**目标**：点击"牌包"按钮，浮窗显示玩家当前牌包（`PlayerState.Deck`）中所有卡牌，供整体查看和测试。

**执行步骤**：

1. 在 `TrainingGroundScene.tscn` 的 `OverlayLayer` 中（已在 2.1 步骤 4 创建）：
   - `DeckFloatingPanel`（PanelContainer），居中，`custom_minimum_size = 600x450`，锚点居中，`visible = false`。
   - 内部结构：
     ```
     DeckFloatingPanel (PanelContainer)
     └── DeckVBox (VBoxContainer, 填充内边距 12px)
         ├── DeckTitleLabel (Label, "卡包 (共 N 张)")
         ├── DeckCardScroll (ScrollContainer, size_flags_vertical = Expand)
         │   └── DeckCardList (VBoxContainer)
         │       └── [动态生成的 Button]
         └── DeckCloseButton (Button, "关闭", size_flags_horizontal = Center)
     ```

2. **DeckFloatingPanel 不需要自己的迷你预览**——单击牌时，复用 `BattleUI.ShowCardPreview` 显示三栏预览，或直接在 DeckFloatingPanel 内嵌预览区（与 CardPileBrowser 的 MiniPreviewPanel 结构相同，包含 MiniCostLabel/MiniEffectLabel/MiniDescLabel）。

3. **交互规则**：
   - 单击牌：显示三栏预览（在 DeckFloatingPanel 内嵌预览区填充）。
   - **双击牌不执行任何操作**（卡包是静态查看，不可移入手牌）。
   - 关闭按钮：隐藏面板。
   - 点击 DimMask：也隐藏面板（复用阶段二的 DimMask 逻辑）。

4. **TrainingGroundController 逻辑**：
   ```csharp
   // 在 OnPileClicked 同级添加：
   private void OnDeckToggleButtonPressed()
   {
       bool isOpen = !DeckFloatingPanel.Visible;
       ConfigFloatingPanel.Visible = false;
       LogFloatingPanel.Visible = false;
       DeckFloatingPanel.Visible = isOpen;
       DimMask.Visible = isOpen;

       if (isOpen)
           PopulateDeckPanel();
   }

   private void PopulateDeckPanel()
   {
       // 清空 DeckCardList
       foreach (var child in DeckCardList.GetChildren())
           child.QueueFree();
       
       // 标题
       DeckTitleLabel.Text = $"卡包 (共 {_battleManager.Player.Deck.Count} 张)";
       
       // 为每张牌创建 Button
       foreach (var card in _battleManager.Player.Deck)
       {
           var btn = new Button();
           btn.Text = $"{card.Data.Name}  [{card.Data.Type}]  Energy:{card.Data.EnergyCost}";
           btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
           // 单击 → 填充预览区
           btn.Connect("pressed", new Callable(this, nameof(OnDeckCardClicked)).Bind(card));
           DeckCardList.AddChild(btn);
       }
   }

   private void OnDeckCardClicked(CardInstance card)
   {
       // 填充 DeckFloatingPanel 内的预览 Label（与 CardPileBrowser 的 MiniPreviewPanel 相同逻辑）
       DeckMiniCostLabel.Text = $"Energy: {card.Data.EnergyCost}  Dice: {(card.Data.DiceCost > 0 ? card.Data.DiceCost.ToString() : "无需")}";
       // ... 填充效果和说明
       DeckMiniEffectLabel.Text = GetEffectText(card.Data);
       DeckMiniDescLabel.Text = card.Data.Description;
   }
   ```

**注**：`DeckFloatingPanel` 与 `CardPileBrowser` 功能不同：
 - `CardPileBrowser` 查看运行时堆（DrawPile/DiscardPile/ExhaustPile），可双击进手牌（训练场专属）
 - `DeckFloatingPanel` 查看静态卡包（Deck），只读，不可双击
 
### 5.6 完整点击流程（事件链对照）

以下是堆点击和卡包点击的完整事件链，供 AI 实现时对照检查，确保不遗漏任何环节：

**堆点击事件链**：
```
玩家单击 DrawPileView/DiscardPileView/ExhaustPileView
  → PileView 的 mouse_filter = Stop 拦截点击
  → GuiInput 事件触发
  → BattleUI.OnPileGuiInput() 捕获
  → EmitSignal 发射 PileClicked(pileName)
  → TrainingGroundController.OnPileClicked() 接收
  → CardPileBrowser.OpenPile() 打开面板
  → 玩家双击牌 → CardMoved 信号 → battleUI.UpdateUI() 刷新手牌
```

**卡包点击事件链**：
```
玩家单击 DeckToggleButton
  → TrainingGroundController.OnDeckToggleButtonPressed()
  → PopulateDeckPanel() 重建卡牌列表
  → DeckFloatingPanel.Visible = true, DimMask.Visible = true
  → 玩家单击牌 → OnDeckCardClicked() 填充预览区
  → 玩家点击关闭/DimMask → 面板隐藏
```

**预览三栏格式调用链**：
```
手牌单击某张牌 → OnCardSingleClicked()
  → ShowCardPreview(card, cardIdx)
  → 填充 CostRow(Energy/Dice) + EffectRow(伤害/效果) + DescriptionLabel(原文)
  → CardPreviewPanel.Visible = true
  → CardPreviewPanel 在场景树最末尾，z-order 高于 CardPanel
```

---

## 文件变更清单

| 操作 | 文件 | 改动摘要 |
|---|---|---|
| 修改 | `scenes/training/TrainingGroundScene.tscn` | BattleUI 偏移归零；新建 OverlayLayer + DimMask；移动 ConfigPanel/BattleLogPanel 入 OverlayLayer 并重命名；新增 DeckFloatingPanel 并重命名；新增三个切换按钮；CardPanel 内新增 PileRow + DrawPileView + DiscardPileView + ExhaustPileView |
| 新增 | `scenes/ui/CardPileBrowser.tscn` | 堆内浏览面板场景 |
| 新增 | `scripts/ui/CardPileBrowser.cs` | 堆内浏览面板逻辑 + 迷你预览 + 双击进手牌（训练场专属） |
| 修改 | `scripts/core/CardData.cs` | 新增 7 张测试卡数据定义（HeavyStrike/EnergyBarrier/Adrenaline/WeakPulse/EnergyPotion/IronSword/Clumsy）；若缺少 EquipSlot/AppliedCurseType 等字段则先新增占位 |
| 修改 | `scripts/core/CardInstance.cs` | 新增 RemainingUses 字段（构造时从 CardData.UsesPerBattle 复制） |
| 修改 | `scripts/core/PlayerState.cs` | 新增 Deck/DrawPile/DiscardPile/ExhaustPile/MaxHandSize/Shield/NextTurnEnergyBonus/EquippedWeaponBonus 字段；新增 InitDrawPileFromDeck/ShuffleDrawPile/ShuffleDiscardIntoDraw/DrawCards/DiscardHand/MoveToDiscard/MoveToExhaust 方法；修改 TakeDamage 先扣 Shield |
| 修改 | `scripts/battle/BattleManager.cs` | DrawInitialHand 构建 12 张测试牌组；StartPlayerTurn 抽 3 张 + NextTurnEnergyBonus 处理；EndPlayerTurn 追加 DiscardHand；TryPlayCard 追加子类型效果处理 + 武器加成 + 消耗品次数检查；信号 diceResult 无骰时传 -1 |
| 修改 | `scripts/ui/BattleUI.cs` | CardContainer GetNode 路径更新；新增三个 PileView 引用和 UpdatePileUI；新增 `PileClicked` 信号 + `OnPileGuiInput` 事件处理；`UpdateUI()` 改为 `public`；重构 CardPreview 为 PanelContainer + 三栏 Label；重写 ShowCardPreview；删除 break_core 硬编码；修复 OnCardPlayed 无骰显示；新增诅咒卡 Disabled + 变灰处理 |
| 修改 | `scripts/training/TrainingGroundController.cs` | 更新 ConfigPanel/BattleLogPanel 的 GetNode 路径；新增浮窗切换按钮和 DimMask 的引用与逻辑；实例化 CardPileBrowser；连接 BattleUI `PileClicked` 信号到浏览器打开；连接浏览器 `CardMoved` 信号到 BattleUI `UpdateUI()` 刷新 |

---

## 设计决策说明（以下为提示词作者的选择，可按需调整）

1. **浮窗互斥**：展开配置面板时自动收起日志面板，反之亦然。
2. **默认收起**：两个浮窗默认 `visible = false`。训练场是 Debug 场景，如果希望日志面板默认展开，将 `LogFloatingPanel` 的 `visible` 改为 `true`。
3. **初始牌组**：5 种卡各 2 张，共 10 张。手牌上限 10。每回合抽 5 张。回合结束弃全部手牌。
4. **消耗堆永久移出**：消耗牌（Category == Consumable）打出后进入消耗堆，不参与洗入抽牌堆。
5. **堆内预览迷你三栏**：浏览面板内嵌独立迷你预览，不直接复用 BattleUI 的预览面板，避免 z-order 和位置冲突。
6. **测试卡牌数量**：初始牌组 12 张（5 张原有 + 7 张新卡），每回合抽 3 张。牌组较小是为了确保每局都能抽到各类型卡进行测试。
7. **最小可测系统 vs 完整系统**：Shield/NextTurnEnergyBonus/EquippedWeaponBonus 使用简化 int 字段而非完整 Buff/装备类，目的是在最小改动下验证各卡牌类型的核心概念。后续应扩展为完整的 BuffInstance/EquipmentSystem/CurseSystem。
8. **消耗堆颜色**：使用橙色 (#FF8800) 匹配战斗级消耗品视觉规范。金色 (#FFD700) 留给未来游戏级消耗品。
