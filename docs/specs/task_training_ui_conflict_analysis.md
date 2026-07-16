# 提示词 vs 卡牌子类型设计文档冲突分析

## 对比文档

- 提示词：`docs/specs/task_training_ui_full.md`
- 设计文档：`docs/design/card_subtype_system_design.md`

---

## 问题 1：消耗堆（ExhaustPile）概念扩展未标注（设计层面）

### 现状

设计文档 3.1 节定义消耗品："使用后从手牌移除，不进入弃牌堆"，但没有定义"消耗堆"这个概念。提示词新增了 `ExhaustPile` 字段、`MoveToExhaust` 方法、ExhaustPileView UI 和浏览面板。

### 影响

这不是代码冲突（两者都不进弃牌堆），但属于**未经文档确认的设计扩展**。如果后续设计文档更新消耗品去向（例如"永久从卡组删除"或"战斗结束后回到牌组"），当前实现可能需要返工。

### 修改建议

保留消耗堆（它是合理的中间状态），但必须在提示词中明确标注这是**扩展行为**，并说明消耗堆的边界：
- 消耗堆仅用于追踪本场战斗已使用的消耗品
- 消耗堆不参与洗入抽牌堆
- 战斗结束后消耗堆的清理由后续系统设计决定

---

## 问题 2：ExhaustPileView 颜色与消耗品视觉规范冲突（视觉层面）

### 现状

提示词 3.3 节定义 ExhaustPileView 颜色为紫色 `Color(0.4, 0.1, 0.4)`。

设计文档定义消耗品颜色：
- 游戏级消耗品：金色 (#FFD700)
- 战斗级消耗品：橙色 (#FF8800)

### 影响

玩家通过颜色识别卡牌类型。消耗堆使用紫色（与减益卡同色系）会造成视觉混淆。

### 修改建议

ExhaustPileView 颜色改为消耗品色系。由于当前不区分游戏级/战斗级，使用统一的消耗品色：
- 主色：橙色 `Color(1.0, 0.53, 0.0)` (#FF8800，对应战斗级消耗品)
- 或辅助色：金色 `Color(1.0, 0.84, 0.0)` (#FFD700，对应游戏级消耗品)

推荐统一使用橙色，因为当前测试卡牌池中的消耗品更接近"战斗级消耗品"定位。

---

## 问题 3：卡牌预览 EffectLabel 未覆盖全部子类型（功能层面）

### 现状

提示词 4.2 节定义 EffectLabel：
```
若 AppliedBuffType != null → 显示增益
若 AppliedDebuffType != null → 显示减益
否则 → "效果: 无"
```

设计文档定义了更多子类型，每个子类型的核心参数不同：

| 子类型 | 核心参数 | 当前预览是否覆盖 |
|--------|---------|----------------|
| Attack | DamageFormula | 覆盖（DamageRangeLabel） |
| Defense/Dodge | ShieldValue, EvasionRate, CounterDamage | **未覆盖** |
| PositiveBuff | BuffType, BuffAmount, Duration | 覆盖（EffectLabel） |
| NegativeBuff | DebuffType, DebuffAmount, Duration | 覆盖（EffectLabel） |
| Consumable | MaxUsage/UsesPerBattle, EffectType | **未覆盖** |
| Equipment | EquipmentSlot, EffectType | **未覆盖** |
| Curse | CurseType, RemovalCondition | **未覆盖** |

### 影响

当前只有 5 张 Basic Attack 卡，所以不会触发未覆盖的情况。但一旦引入防御卡、消耗品、装备或诅咒，预览会显示"效果: 无"，造成信息缺失。

### 修改建议

扩展 EffectLabel 的逻辑，按子类型分支显示：

```
if (Subtype == Defense/Dodge):
    显示 "护盾: {ShieldValue} / 闪避: {EvasionRate}% / 反击: {CounterDamage}"
else if (Subtype == Consumable):
    显示 "效果: {EffectType} ({EffectValue}) / 剩余: {UsesPerBattle}次"
else if (Subtype == Equipment):
    显示 "槽位: {EquipmentSlot} / 效果: {EffectType} ({EffectValue})"
else if (Subtype == Curse):
    显示 "诅咒: {CurseType} / 移除条件: {RemovalCondition}"
else if (AppliedBuffType != null):
    显示增益...
else if (AppliedDebuffType != null):
    显示减益...
else:
    "效果: 无"
```

---

## 问题 4：消耗品判定粒度不足（逻辑层面）

### 现状

提示词 3.2 节使用 `card.Data.Category == CardCategory.Consumable` 判定消耗品。

设计文档 3.2/3.3 节将消耗品细分为：
- Game-Level Consumables（游戏级，MaxUsage 全局计数）
- Battle-Level Consumables（战斗级，UsesPerBattle 每场战斗计数）

两者的 Subtype 在 CardData 枚举中对应 `CardSubtype.GameLevelConsumable` 和 `CardSubtype.BattleLevelConsumable`。

### 影响

当前阶段只有 Basic 攻击卡，不影响。但未来增加消耗品后：
- 游戏级消耗品打出后应从全局计数（MaxUsage）扣减
- 战斗级消耗品打出后应从本场计数（UsesPerBattle）扣减
- 两者的使用次数 UI 显示逻辑不同

### 修改建议

在 `BattleManager.TryPlayCard` 的消耗品处理中，增加子类型分支：

```csharp
if (card.Data.Category == CardCategory.Consumable)
{
    if (card.Data.Subtype == CardSubtype.GameLevelConsumable)
    {
        // 扣减全局使用次数，如果 MaxUsage <= 0 则不允许打出
        // 打完进入消耗堆（或永久删除）
    }
    else if (card.Data.Subtype == CardSubtype.BattleLevelConsumable)
    {
        // 扣减本场使用次数，每场战斗重置
        // 打完进入消耗堆
    }
    Player.MoveToExhaust(card);
}
```

当前阶段可先保留简单判定（`Category == Consumable`），但应在提示词中标注"未来需要按 Subtype 细化"。

---

## 汇总

| 问题 | 严重程度 | 是否必须修改 | 修改位置 |
|------|---------|-------------|---------|
| 消耗堆概念未标注 | 中 | 是（标注即可） | 铁律 + 设计决策说明 |
| ExhaustPileView 颜色冲突 | 中 | 是 | 3.3 节占位美术 |
| EffectLabel 未覆盖全部子类型 | 低（当前不影响） | 建议（扩展预览逻辑） | 4.2 节 |
| 消耗品判定粒度不足 | 低（当前不影响） | 建议（标注未来细化） | 3.2 节 + 铁律 |
