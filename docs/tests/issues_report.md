# Battle Core Issues Report

本文件记录 battle-log-and-tests 线程在代码审查中发现的问题，提交给 battle-core 线程处理。

---

## 问题列表

### ISSUE-001: BattleManager.TryPlayCard 冗余死代码

**文件**: [BattleManager.cs](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/battle/BattleManager.cs#L88-L96)

**问题描述**:
```csharp
if (card.Data.DiceCost > 0)
{
	consumedDice = Player.ConsumeNextDice();
	if (consumedDice == null)
		return false;  // ← 第 92 行已返回
}

if (card.Data.DiceCost > 0 && consumedDice == null)  // ← 第 95-96 行永远不会执行
	return false;
```

第 95-96 行的检查永远不会命中，因为：
- 如果 `DiceCost > 0`，第 92 行已经在 `consumedDice == null` 时返回
- 如果 `DiceCost <= 0`，条件 `DiceCost > 0` 为 false，整个条件为 false

**严重程度**: 低（死代码）

**建议修复**: 删除第 95-96 行的冗余检查。

---

### ISSUE-002: CardInstance.CalculateDamage 对无骰卡牌的缺陷

**文件**: [CardInstance.cs](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/core/CardInstance.cs#L10-L16)

**问题描述**:
```csharp
public int CalculateDamage(DiceInstance dice)
{
	if (dice == null || Data.DamageFormula == null)
		return 0;  // ← 问题：dice==null 时直接返回 0
	
	return Data.DamageFormula(this, dice);
}
```

当前实现中，当 `dice == null` 时直接返回 0。这意味着未来实现的无骰子卡牌（如设计文档中的 `QuickStrike`、`DefensivePosture`）如果设置了 `DamageFormula`，伤害会错误地归零。

**严重程度**: 高（功能缺陷，影响未来卡牌扩展）

**设计文档中的受影响卡牌**:
- `QuickStrike`: DiceCost=0, 固定伤害 4
- `DefensivePosture`: DiceCost=0, 无伤害（技能卡）
- `EnergySurge`: DiceCost=1, 需要骰子
- `DiceBoost`: DiceCost=0, 无伤害（技能卡）

**建议修复**:
1. 修改 `CalculateDamage`，允许 `dice == null` 时调用 `DamageFormula`
2. 或者为无骰子卡牌使用单独的效果机制，不依赖 `DamageFormula`

**推荐方案**:
```csharp
public int CalculateDamage(DiceInstance dice)
{
	if (Data.DamageFormula == null)
		return 0;
	
	return Data.DamageFormula(this, dice);
}
```

同时需要确保 `DamageFormula` 的实现能够处理 `dice == null` 的情况：
```csharp
// QuickStrike 的 DamageFormula 示例
DamageFormula = (card, dice) => 4
```

**关联问题**: [CardData.GetDamageRange](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/core/CardData.cs#L62-L80) 在 `DiceCost == 0` 时直接返回 `min=0, max=0`，导致无骰子卡牌（如 QuickStrike）的伤害范围显示错误。

```csharp
public void GetDamageRange(int diceSides, out int min, out int max)
{
	if (DamageFormula == null || DiceCost == 0)  // ← 问题：DiceCost==0 时直接返回 0/0
	{
		min = 0;
		max = 0;
		return;
	}
	// ...
}
```

**建议修复**: 修改条件，仅在 `DamageFormula == null` 时返回 0/0：
```csharp
if (DamageFormula == null)
{
	min = 0;
	max = 0;
	return;
}
```

---

### ISSUE-003: DiceRoller 使用 static RNG 导致测试隔离问题

**文件**: [DiceRoller.cs](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/battle/DiceRoller.cs#L5-L15)

**问题描述**:
```csharp
private static RandomNumberGenerator _rng = new RandomNumberGenerator();

public DiceRoller()
{
	Mode = RollMode.Random;
	FixedValue = 1;
	_rng.Randomize();  // ← 所有实例共享同一个 RNG，Randomize 会影响其他实例
}
```

`_rng` 是静态字段，所有 `DiceRoller` 实例共享同一个随机数生成器。这会导致：
1. 在单元测试中，无法隔离不同测试用例的随机状态
2. 创建多个 `DiceRoller` 实例时，`Randomize()` 调用会相互干扰

**严重程度**: 中（测试隔离问题）

**建议修复**: 将 `_rng` 改为实例字段：
```csharp
private RandomNumberGenerator _rng;

public DiceRoller()
{
	Mode = RollMode.Random;
	FixedValue = 1;
	_rng = new RandomNumberGenerator();
	_rng.Randomize();
}
```

---

### ISSUE-004: Player.PlayCard 执行顺序脆弱

**文件**: [BattleManager.cs](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/battle/BattleManager.cs#L98-L109)

**问题描述**:
```csharp
int baseDamage = card.CalculateDamage(consumedDice);
int finalDamage = Enemy.TakeDamage(baseDamage);
int vulnerableBeforeEffect = Enemy.GetVulnerableStacks();

if (card.Data.ApplyEffect != null)
{
	card.Data.ApplyEffect(card, consumedDice, Enemy);  // ← 效果施加
}

int vulnerableAdded = Mathf.Max(0, Enemy.GetVulnerableStacks() - vulnerableBeforeEffect);

Player.PlayCard(card);  // ← 能量扣除和手牌移除在最后
```

当前执行顺序：
1. 计算伤害
2. 敌人承受伤害
3. 施加卡牌效果
4. 扣除能量和移除手牌

**问题**: `Player.PlayCard` 在伤害结算和效果施加之后才执行。虽然当前效果只影响 Enemy 不会出错，但顺序脆弱：
- 如果未来有卡牌效果依赖玩家当前 Energy，会出现逻辑错误
- 如果未来有卡牌效果需要检查手牌状态，也会出错

**严重程度**: 中（架构设计问题，影响未来扩展）

**建议修复**: 将能量消耗和手牌移除提前到伤害结算之前：
```csharp
Player.PlayCard(card);  // ← 提前执行

int baseDamage = card.CalculateDamage(consumedDice);
int finalDamage = Enemy.TakeDamage(baseDamage);
// ... 后续效果施加
```

注意：这需要确保 `PlayCard` 只扣除能量和移除手牌，不影响其他状态。当前 `PlayCard` 实现：
```csharp
public void PlayCard(CardInstance card)
{
	Energy -= card.Data.EnergyCost;
	Hand.Remove(card);
}
```

当前实现可以安全提前。

---

### ISSUE-005: 战斗日志格式不一致

**文件**: [BattleManager.cs](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/battle/BattleManager.cs)

**问题描述**: 战斗日志的格式和信息密度不一致：

| 场景 | 日志示例 | 问题 |
|------|----------|------|
| 回合开始 | `回合 1 开始` | 缺少时间戳（BattleLogPanel 添加） |
| 能量恢复 | `恢复 Energy: 10 → 12` | 使用 "→" 符号 |
| 骰子获得 | `获得骰池: 2d6` | 格式简洁 |
| 出牌 | `打出 EnergyStrike` | 缺少消耗信息 |
| 掷骰 | `掷骰结果: 5` | 单独一行 |
| 伤害 | `造成伤害: 8` | 单独一行 |
| 破甲施加 | `施加破甲: 2 层` | 单独一行 |
| 敌人攻击 | `TrainingBeast 攻击: 14` | 包含敌人名称 |
| 能量变化 | `Energy: 12 → 0` | 使用 "→" 符号 |
| HP 变化 | `HP: 30 → 21` | 使用 "→" 符号 |

**严重程度**: 低（体验问题）

**建议修复**: 统一日志格式，使用更结构化的输出：
- 出牌时包含消耗：`打出 EnergyStrike (消耗: 1 Energy, 1 骰子)`
- 将相关日志合并：`掷骰结果: 5 → 造成伤害: 8（含破甲+2）`

---

### ISSUE-006: DiceRoller.FixedValue 未验证范围

**文件**: [DiceRoller.cs](file:///e:/Godot_v4.7-stable_mono_win64/Documents/NewGame/scripts/battle/DiceRoller.cs#L18-L25)

**问题描述**:
```csharp
public int Roll(int sides)
{
	if (Mode == RollMode.Fixed)
	{
		return Mathf.Clamp(FixedValue, 1, sides);  // ← 运行时才 clamp
	}
	
	return _rng.RandiRange(1, sides);
}
```

虽然 `Roll` 方法在运行时会 clamp `FixedValue`，但 `FixedValue` 属性本身没有验证机制。`TrainingConfig.ClampValues()` 会验证，但如果直接设置 `DiceRoller.FixedValue` 则没有保护。

**严重程度**: 低（防御性编程问题）

**当前状态**: ✅ 当前设计接受，依赖 `Roll` 方法的 clamp 防护。`Roll(int sides)` 在返回前会通过 `Mathf.Clamp(FixedValue, 1, sides)` 确保结果始终在有效范围内。由于 `TrainingConfig.ClampValues()` 已提供前端保护，且 `Roll` 方法已提供运行时保护，当前设计足够安全。

---

## 问题汇总

| 编号 | 问题 | 文件 | 严重程度 | 状态 |
|------|------|------|----------|------|
| ISSUE-001 | TryPlayCard 冗余死代码 | BattleManager.cs | 低 | ✅ 已修复 |
| ISSUE-002 | CalculateDamage 对无骰卡牌缺陷 | CardInstance.cs | 高 | ✅ 已修复 |
| ISSUE-003 | DiceRoller static RNG | DiceRoller.cs | 中 | ✅ 已修复 |
| ISSUE-004 | PlayCard 执行顺序 | BattleManager.cs | 中 | ✅ 已修复 |
| ISSUE-005 | 战斗日志格式不一致 | BattleManager.cs | 低 | 待改进 |
| ISSUE-006 | FixedValue 未验证 | DiceRoller.cs | 低 | ✅ 当前设计接受 |

---

## 修复优先级建议

### 必须修复（影响核心功能）

1. **ISSUE-002**: 必须在实现无骰子卡牌前修复，否则 `QuickStrike` 等卡牌无法正常工作

### 建议修复（影响测试和架构）

2. **ISSUE-003**: 建议在搭建单元测试框架前修复
3. **ISSUE-004**: 建议在扩展卡牌效果前修复

### 可以延后（不影响核心功能）

4. **ISSUE-001**: 死代码，不影响运行
5. **ISSUE-005**: 日志格式，可在后续迭代中改进
6. **ISSUE-006**: 防御性编程，当前有 `ClampValues` 保护

---

## 修复验证

每个问题修复后，应通过以下验证：

| 问题 | 验证方式 |
|------|----------|
| ISSUE-001 | 代码编译通过，功能不变 |
| ISSUE-002 | 实现 QuickStrike 后，训练场测试 Fixed Roll 任意值均造成 4 伤害 |
| ISSUE-003 | 创建多个 DiceRoller 实例，设置不同 FixedValue，各自独立工作 |
| ISSUE-004 | 创建依赖 Energy 的卡牌效果，验证效果计算使用消耗前的 Energy 值 |
| ISSUE-005 | 训练场战斗日志格式统一，信息完整 |
| ISSUE-006 | 设置 FixedValue 超出范围，运行时自动 clamp |

---

## 版本记录

| 日期 | 版本 | 修改内容 |
|------|------|----------|
| 2026-07-15 | v0.1 | 初始版本，记录 6 个问题 |
