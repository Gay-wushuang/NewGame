# Trae CN 执行任务：诅咒卡子系统（临时诅咒 + 永久诅咒）

## 铁律

1. **不破坏现有系统**：诅咒卡子系统作为新增分支接入，不改变原有攻击卡、防御卡、消耗品、装备的结算逻辑。
2. **诅咒卡可以交互**：与旧版"Disabled + 变灰"不同，诅咒卡在手牌中可以单击预览、双击打出。
3. **概率判定使用 Godot 随机**：`GD.Randf()` 生成 0~1 浮点数，不与 DiceRoller 绑定。
4. **临时诅咒战斗结束即销毁**：从 Hand/DrawPile/DiscardPile 中彻底移除，不进消耗堆。
5. **训练场测试**：新增 Debug 按钮"塞入临时诅咒"，方便直接测试 Wound 卡。

---

## 一、诅咒卡分类定义

| 维度 | 临时诅咒（Temporary） | 永久诅咒（Permanent） |
|------|---------------------|---------------------|
| 获取方式 | 受到过量伤害时自动塞入手牌 | 负面事件获取 |
| 存在周期 | 本场战斗内有效 | 跨战斗持续存在 |
| 战斗结束后 | **自动销毁**，从所有牌堆移除 | 保留在 Deck 中 |
| 主动打出后 | 概率判定（消失/无事/强化） | 概率判定（消失/无事/强化） |
| 打出后去向 | 默认进入 **DrawPile 底部** | 默认进入 **DrawPile 底部** |
| 移除方式 | 只能靠打出概率消失 | 打出概率消失 或 满足特定条件 |
| 视觉标识 | 深红色边框 + "临时"标签 | 紫色边框 |

---

## 二、数据模型

### 2.1 新增枚举

在 `scripts/core/` 下新建 `CurseEnums.cs`：

```csharp
public enum CurseDurationType
{
    Temporary,   // 临时：战斗结束自动销毁
    Permanent    // 永久：跨战斗存在
}

public enum CurseTriggerType
{
    HandSizeReduction,  // 手牌上限减少
    EnergyDrain,        // 每回合扣除 Energy
    SelfDamage,         // 每回合自伤（失去 HP）
    DrawReduction       // 抽牌数量减少
}
```

### 2.2 CardData 新增字段

在 `CardData.cs` 中新增以下字段（不改动已有字段）：

```csharp
public CurseDurationType CurseDuration = CurseDurationType.Permanent;  // 临时/永久
public CurseTriggerType CurseTrigger = CurseTriggerType.SelfDamage;    // 负面效果类型
public int CurseEffectAmount = 1;                                      // 负面效果数值（每层）
public int CurseStrengthenAmount = 1;                                  // 强化时增加的数值
public float CurseDisappearChance = 0.15f;                             // 打出后消失概率（0~1）
public float CurseNothingChance = 0.70f;                               // 打出后无事概率（0~1）
public float CurseStrengthenChance = 0.15f;                            // 打出后强化概率（0~1）
```

**概率校验**：三个概率之和必须等于 1.0。在 `CardData` 构造函数或验证方法中检查：
```csharp
public void ValidateCurseChances()
{
    float total = CurseDisappearChance + CurseNothingChance + CurseStrengthenChance;
    if (Mathf.Abs(total - 1.0f) > 0.001f)
        GD.PushWarning($"诅咒卡 {Name} 的概率之和不为 1.0: {total}");
}
```

### 2.3 CardInstance 新增字段

在 `CardInstance.cs` 中新增：

```csharp
public int CurseStacks = 1;              // 当前诅咒层数（初始1，强化时+CurseStrengthenAmount）
public bool HasTriggeredThisTurn = false; // 本回合是否已触发负面效果（防止一回合多次触发）
```

### 2.4 PlayerState 新增字段

```csharp
public int MaxHandSizeBase = 10;         // 基础手牌上限（诅咒减少的是EffectiveMaxHandSize）
public int CurseHandSizeModifier = 0;    // 诅咒对手牌上限的修正值（负数）

// 计算属性：当前有效手牌上限
public int EffectiveMaxHandSize => Mathf.Max(1, MaxHandSizeBase + CurseHandSizeModifier);
```

修改 `DrawCards` 方法中的手牌上限检查：
```csharp
// 原代码：if (Hand.Count >= MaxHandSize) break;
// 改为：
if (Hand.Count >= EffectiveMaxHandSize) break;
```

---

## 三、负面效果触发（自动，每回合开始）

### 3.1 触发时机

在 `BattleManager.StartPlayerTurn()` 中，**在 RestoreEnergy 之后、DrawCards 之前**插入诅咒触发逻辑。

### 3.2 触发流程

```csharp
private void TriggerCurseEffects()
{
    foreach (var card in Player.Hand.ToList())  // ToList 防止遍历时修改集合
    {
        if (card.Data.Subtype != CardSubtype.Curse)
            continue;
        
        if (card.HasTriggeredThisTurn)
            continue;  // 本回合已触发，跳过
        
        card.HasTriggeredThisTurn = true;
        int totalEffect = card.CurseStacks * card.Data.CurseEffectAmount;
        
        switch (card.Data.CurseTrigger)
        {
            case CurseTriggerType.HandSizeReduction:
                Player.CurseHandSizeModifier -= totalEffect;
                EmitSignal(SignalName.BattleLog, 
                    $"{card.Data.Name} 触发: 手牌上限 -{totalEffect} (当前: {Player.EffectiveMaxHandSize})");
                break;
                
            case CurseTriggerType.EnergyDrain:
                int drain = Mathf.Min(totalEffect, Player.Energy);
                Player.Energy -= drain;
                EmitSignal(SignalName.BattleLog, 
                    $"{card.Data.Name} 触发: 失去 {drain} Energy");
                break;
                
            case CurseTriggerType.SelfDamage:
                Player.Hp -= totalEffect;
                if (Player.Hp < 0) Player.Hp = 0;
                EmitSignal(SignalName.BattleLog, 
                    $"{card.Data.Name} 触发: 失去 {totalEffect} HP");
                break;
                
            case CurseTriggerType.DrawReduction:
                // 抽牌减少在 DrawCards 中处理，这里只记录日志
                EmitSignal(SignalName.BattleLog, 
                    $"{card.Data.Name} 触发: 本回合抽牌 -{totalEffect}");
                break;
        }
    }
}
```

### 3.3 DrawReduction 的特殊处理

`DrawReduction` 不直接修改状态，而是在 `DrawCards` 中减少抽牌数量：

在 `BattleManager.StartPlayerTurn()` 中：
```csharp
int drawCount = 3;  // 基础抽牌数

// 计算 DrawReduction 影响
int drawReduction = 0;
foreach (var card in Player.Hand)
{
    if (card.Data.Subtype == CardSubtype.Curse && 
        card.Data.CurseTrigger == CurseTriggerType.DrawReduction &&
        card.HasTriggeredThisTurn)
    {
        drawReduction += card.CurseStacks * card.Data.CurseEffectAmount;
    }
}
int finalDrawCount = Mathf.Max(0, drawCount - drawReduction);

int drawn = Player.DrawCards(finalDrawCount);
```

### 3.4 回合结束重置

在 `BattleManager.EndPlayerTurn()` 中，重置所有诅咒卡的 `HasTriggeredThisTurn`：
```csharp
foreach (var card in Player.Hand)
    if (card.Data.Subtype == CardSubtype.Curse)
        card.HasTriggeredThisTurn = false;
// 同样清理 DrawPile/DiscardPile 中的诅咒卡（虽然它们不在手牌中不会触发，但保持一致性）
foreach (var card in Player.DrawPile)
    if (card.Data.Subtype == CardSubtype.Curse)
        card.HasTriggeredThisTurn = false;
foreach (var card in Player.DiscardPile)
    if (card.Data.Subtype == CardSubtype.Curse)
        card.HasTriggeredThisTurn = false;
```

---

## 四、主动打出（概率判定）

### 4.1 诅咒卡打出流程

在 `BattleManager.TryPlayCard()` 中，诅咒卡走独立分支（不进入正常的伤害结算）：

```csharp
// 在 "if (card.Data.Category == CardCategory.Consumable)" 判断之前插入：
if (card.Data.Subtype == CardSubtype.Curse)
{
    // 1. 从手牌移除
    Player.Hand.Remove(card);
    
    // 2. 先触发一次负面效果（打出时强制触发）
    int totalEffect = card.CurseStacks * card.Data.CurseEffectAmount;
    switch (card.Data.CurseTrigger)
    {
        case CurseTriggerType.SelfDamage:
            Player.Hp -= totalEffect;
            if (Player.Hp < 0) Player.Hp = 0;
            EmitSignal(SignalName.BattleLog, $"{card.Data.Name} 打出: 失去 {totalEffect} HP");
            break;
        // ... 其他触发类型同理
    }
    
    // 3. 概率判定
    float roll = GD.Randf();
    if (roll < card.Data.CurseDisappearChance)
    {
        // 结果1：消失
        EmitSignal(SignalName.BattleLog, 
            $"{card.Data.Name} 消失了！({card.CurseStacks}层)");
        // 不进入任何牌堆，直接销毁（Godot GC 自动处理）
    }
    else if (roll < card.Data.CurseDisappearChance + card.Data.CurseStrengthenChance)
    {
        // 结果2：强化
        card.CurseStacks += card.Data.CurseStrengthenAmount;
        EmitSignal(SignalName.BattleLog, 
            $"{card.Data.Name} 强化了！当前 {card.CurseStacks} 层");
        // 进入抽牌堆底部
        Player.DrawPile.Insert(0, card);
    }
    else
    {
        // 结果3：无事发生
        EmitSignal(SignalName.BattleLog, 
            $"{card.Data.Name} 无事发生 (当前 {card.CurseStacks} 层)");
        // 进入抽牌堆底部
        Player.DrawPile.Insert(0, card);
    }
    
    // 4. 更新 UI
    UpdateUI();
    
    // 5. 检查战斗结束（诅咒自伤可能导致玩家死亡）
    if (!Player.IsAlive())
    {
        OnBattleLost();
        return true;
    }
    
    return true;  // 诅咒卡打出成功
}
```

### 4.2 关键规则

- 诅咒卡打出**不消耗 Energy**（EnergyCost = 0）。
- 诅咒卡打出**不消耗骰子**（DiceCost = 0）。
- 诅咒卡打出**先触发一次负面效果**，再进行概率判定。
- 概率判定后，**无事/强化 → 进入 DrawPile 底部**；**消失 → 不进入任何牌堆**。
- 进入 DrawPile 底部的诅咒卡，**下一回合抽牌时会抽到**（因为 DrawCards 从 DrawPile 末尾取牌）。

---

## 五、战斗结束清理（临时诅咒销毁）

### 5.1 清理时机

在 `BattleManager.InitializeBattle()` 开始时（新战斗初始化前），或在 `OnBattleWon()`/`OnBattleLost()` 中调用。

推荐放在 `InitializeBattle()` 开头：

```csharp
private void CleanupTemporaryCurses()
{
    int removedCount = 0;
    
    // 从 Hand 中移除临时诅咒
    for (int i = Player.Hand.Count - 1; i >= 0; i--)
    {
        if (Player.Hand[i].Data.CurseDuration == CurseDurationType.Temporary)
        {
            Player.Hand.RemoveAt(i);
            removedCount++;
        }
    }
    
    // 从 DrawPile 中移除临时诅咒
    for (int i = Player.DrawPile.Count - 1; i >= 0; i--)
    {
        if (Player.DrawPile[i].Data.CurseDuration == CurseDurationType.Temporary)
        {
            Player.DrawPile.RemoveAt(i);
            removedCount++;
        }
    }
    
    // 从 DiscardPile 中移除临时诅咒
    for (int i = Player.DiscardPile.Count - 1; i >= 0; i--)
    {
        if (Player.DiscardPile[i].Data.CurseDuration == CurseDurationType.Temporary)
        {
            Player.DiscardPile.RemoveAt(i);
            removedCount++;
        }
    }
    
    // 从 Deck 中移除临时诅咒（Deck 是静态源，通常不应有临时诅咒，但保险起见）
    for (int i = Player.Deck.Count - 1; i >= 0; i--)
    {
        if (Player.Deck[i].Data.CurseDuration == CurseDurationType.Temporary)
        {
            Player.Deck.RemoveAt(i);
            removedCount++;
        }
    }
    
    // 重置诅咒相关状态
    Player.CurseHandSizeModifier = 0;
    
    if (removedCount > 0)
        GD.Print($"清理了 {removedCount} 张临时诅咒卡");
}
```

### 5.2 训练场特殊处理

训练场中"重置战斗"按钮也应调用 `CleanupTemporaryCurses()`，确保每次重置都清理临时诅咒。

---

## 六、训练场 Debug 功能

### 6.1 新增"塞入临时诅咒"按钮

在 `TrainingGroundController` 的 ConfigPanel 中新增一个按钮：

```csharp
// 在 ConfigContainer 中新增：
var addCurseButton = new Button();
addCurseButton.Text = "塞入临时诅咒（Wound）";
addCurseButton.Connect("pressed", new Callable(this, nameof(OnAddTemporaryCurse)));
```

```csharp
private void OnAddTemporaryCurse()
{
    var wound = new CardInstance(CardData.Wound);
    Player.Hand.Add(wound);
    battleUI.UpdateUI();
    battleUI.AddLog("塞入临时诅咒: Wound");
}
```

### 6.2 显示当前诅咒状态

在 BattleLogPanel 或 ConfigPanel 中显示：
- 当前手牌中的诅咒卡列表（名称 + 层数）
- 当前诅咒对手牌上限的影响（`CurseHandSizeModifier`）

---

## 七、测试卡牌

### 7.1 Clumsy（永久诅咒）

修改现有定义：

```csharp
public static CardData Clumsy = new CardData()
{
    Id = "clumsy", Name = "Clumsy",
    Description = "永久诅咒：手牌上限 -1；每回合开始时触发；打出后有概率消失/强化",
    Type = CardType.Power, Category = CardCategory.Other, Subtype = CardSubtype.Curse,
    Target = TargetType.Player, EnergyCost = 0, DiceCost = 0,
    
    // 诅咒专属字段
    CurseDuration = CurseDurationType.Permanent,
    CurseTrigger = CurseTriggerType.HandSizeReduction,
    CurseEffectAmount = 1,
    CurseStrengthenAmount = 1,
    CurseDisappearChance = 0.15f,
    CurseNothingChance = 0.70f,
    CurseStrengthenChance = 0.15f,
    
    VisualKey = "curse_clumsy", BorderColor = "#8A2BE2"  // 紫色
};
```

### 7.2 Wound（临时诅咒，新增）

```csharp
public static CardData Wound = new CardData()
{
    Id = "wound", Name = "Wound",
    Description = "临时诅咒：受到过量伤害的印记；每回合开始时失去 2 HP；战斗结束后消失；打出后有概率消失/强化",
    Type = CardType.Power, Category = CardCategory.Other, Subtype = CardSubtype.Curse,
    Target = TargetType.Player, EnergyCost = 0, DiceCost = 0,
    
    // 诅咒专属字段
    CurseDuration = CurseDurationType.Temporary,
    CurseTrigger = CurseTriggerType.SelfDamage,
    CurseEffectAmount = 2,
    CurseStrengthenAmount = 1,
    CurseDisappearChance = 0.15f,
    CurseNothingChance = 0.70f,
    CurseStrengthenChance = 0.15f,
    
    VisualKey = "curse_wound", BorderColor = "#8B0000"  // 深红色
};
```

### 7.3 初始牌组调整

在 `DrawInitialHand()` 中：
- 移除 Clumsy 从初始牌组（永久诅咒不应在初始牌组中，应由负面事件获取）
- 保留 Wound 在初始牌组中供测试（或者通过 Debug 按钮塞入）

推荐：**初始牌组不包含任何诅咒卡**。诅咒卡通过以下方式获取：
- Wound：通过训练场"塞入临时诅咒"按钮测试
- Clumsy：后续通过"负面事件"系统获取（本次不实现事件系统）

如果必须在初始牌组中放一张用于测试，只放 Wound：
```csharp
var cardPool = new List<CardData> {
    // 原有攻击卡
    CardData.EnergyStrike, CardData.BreakCore,
    CardData.QuickStrike, CardData.VulnerableStrike, CardData.CriticalHit,
    // 新测试卡
    CardData.HeavyStrike, CardData.EnergyBarrier,
    CardData.Adrenaline, CardData.WeakPulse,
    CardData.EnergyPotion, CardData.IronSword,
    // 诅咒卡（仅 Wound 用于测试）
    CardData.Wound
};
```

---

## 八、BattleUI 改动

### 8.1 诅咒卡显示

**移除旧版"Disabled + 变灰"处理**：诅咒卡现在可以正常交互。

在 `UpdateCardUI()` 中，为诅咒卡添加特殊视觉标识：

```csharp
if (card.Data.Subtype == CardSubtype.Curse)
{
    // 诅咒卡边框颜色
    string borderColor = card.Data.CurseDuration == CurseDurationType.Temporary 
        ? "#8B0000"  // 深红色：临时
        : "#8A2BE2"; // 紫色：永久
    
    // 在按钮文本中添加层数标识
    btn.Text = $"{card.Data.Name} [{card.CurseStacks}层]";
    
    // 按钮样式（可选：通过 Theme 或 StyleBox 设置边框颜色）
    // 简化方案：在按钮前加前缀标识
    string prefix = card.Data.CurseDuration == CurseDurationType.Temporary ? "[临时]" : "[永久]";
    btn.Text = $"{prefix} {card.Data.Name} [{card.CurseStacks}层]";
}
```

### 8.2 预览三栏格式

阶段四已有的 EffectLabel 分支：
```
- 若 Subtype == Curse: 显示 "诅咒: {CurseTrigger} ({CurseEffectAmount}x{CurseStacks}层) / 持续: {CurseDuration}"
```

更新为更详细的显示：
```
诅咒卡预览 EffectLabel：
- 诅咒类型: {CurseDuration}（临时/永久）
- 负面效果: {CurseTrigger} {CurseEffectAmount * CurseStacks}/回合
- 当前层数: {CurseStacks}
- 打出后: {CurseDisappearChance*100}%消失 / {CurseNothingChance*100}%无事 / {CurseStrengthenChance*100}%强化+{CurseStrengthenAmount}
```

---

## 九、文件变更清单

| 操作 | 文件 | 改动摘要 |
|---|---|---|
| 新增 | `scripts/core/CurseEnums.cs` | CurseDurationType、CurseTriggerType 枚举 |
| 修改 | `scripts/core/CardData.cs` | 新增诅咒专属字段（CurseDuration/CurseTrigger/CurseEffectAmount/CurseStrengthenAmount/三个概率）；修改 Clumsy 定义；新增 Wound 定义 |
| 修改 | `scripts/core/CardInstance.cs` | 新增 CurseStacks、HasTriggeredThisTurn 字段 |
| 修改 | `scripts/core/PlayerState.cs` | 新增 MaxHandSizeBase、CurseHandSizeModifier、EffectiveMaxHandSize；修改 DrawCards 使用 EffectiveMaxHandSize |
| 修改 | `scripts/battle/BattleManager.cs` | 新增 TriggerCurseEffects、CleanupTemporaryCurses；修改 StartPlayerTurn（插入诅咒触发+DrawReduction处理）；修改 EndPlayerTurn（重置 HasTriggeredThisTurn）；修改 TryPlayCard（诅咒卡独立分支+概率判定）；修改 InitializeBattle（调用 CleanupTemporaryCurses） |
| 修改 | `scripts/ui/BattleUI.cs` | 移除诅咒卡 Disabled 处理；新增诅咒卡视觉标识（前缀+层数）；更新预览三栏格式 |
| 修改 | `scripts/training/TrainingGroundController.cs` | 新增"塞入临时诅咒"按钮；连接按钮到 OnAddTemporaryCurse |

---

## 十、验收标准

### 数据模型
- [ ] `CurseEnums.cs` 包含 `CurseDurationType` 和 `CurseTriggerType` 枚举。
- [ ] `CardData` 包含完整的诅咒专属字段（7个）。
- [ ] `CardInstance` 包含 `CurseStacks` 和 `HasTriggeredThisTurn`。
- [ ] `PlayerState` 的 `EffectiveMaxHandSize` 计算正确（基础10 + 诅咒修正）。

### 负面效果触发
- [ ] 每回合开始时，手牌中的诅咒卡自动触发负面效果。
- [ ] Clumsy（HandSizeReduction）：手牌上限减少，日志显示正确。
- [ ] Wound（SelfDamage）：玩家失去 HP，日志显示正确。
- [ ] 多层诅咒（CurseStacks > 1）：负面效果数值正确乘以层数。
- [ ] 一回合内同一张诅咒卡只触发一次（HasTriggeredThisTurn 防重）。
- [ ] 回合结束时 HasTriggeredThisTurn 重置。

### 主动打出
- [ ] 诅咒卡可以单击预览（三栏格式正确显示诅咒信息）。
- [ ] 诅咒卡可以双击打出（不消耗 Energy/骰子）。
- [ ] 打出时先触发一次负面效果。
- [ ] 概率判定三种结果都有日志输出。
- [ ] 消失结果：诅咒卡不进入任何牌堆。
- [ ] 无事/强化结果：诅咒卡进入 DrawPile 底部。
- [ ] 强化结果：CurseStacks 正确增加。
- [ ] 诅咒自伤导致玩家 HP <= 0 时，战斗失败逻辑正常触发。

### 临时诅咒清理
- [ ] 战斗结束后（InitializeBattle 开头），临时诅咒从 Hand/DrawPile/DiscardPile/Deck 中全部移除。
- [ ] 清理后 Player.CurseHandSizeModifier 重置为 0。
- [ ] 永久诅咒（Clumsy）战斗结束后保留。

### 训练场 Debug
- [ ] 点击"塞入临时诅咒"按钮，Wound 卡进入手牌。
- [ ] 塞入后 UI 正确更新（手牌区显示 Wound）。
- [ ] 重置战斗后临时诅咒被清理。

### 视觉与交互
- [ ] 诅咒卡按钮显示前缀 [临时]/[永久] 和层数。
- [ ] 诅咒卡预览三栏格式正确显示全部诅咒信息。
- [ ] 原有非诅咒卡功能完全不受影响。
