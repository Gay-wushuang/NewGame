# Cards v0 Test Matrix

本文件为 cards_v0_design.md 中定义的全部 10 张测试卡牌建立系统化的测试矩阵。

## 测试矩阵说明

每张卡牌的测试覆盖以下维度：

| 维度 | 说明 |
|------|------|
| 最低骰点 | 验证基础效果下限 |
| 最高骰点 | 验证最大效果上限 |
| 触发阈值 | 验证特殊效果触发条件（如破甲、暴击） |
| 边界条件 | 验证不触发特殊效果的情况 |
| 组合测试 | 验证与破甲等状态的联动 |

---

## 1. EnergyStrike（能量打击）

> **当前状态**: ✅ 已实现，可在训练场中测试

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 最低伤害 | Fixed Roll = 1 | 伤害 = 1 + 2 = 3 | P0 |
| 最高伤害 | Fixed Roll = 6 | 伤害 = 6 + 2 = 8 | P0 |
| 能量消耗 | Energy = 12, 打出后 | Energy = 11 | P0 |
| 骰子消耗 | DicePool = 2, 打出后 | DicePool = 1 | P0 |
| 破甲加成 | Enemy Vulnerable = 2, Roll = 1 | 伤害 = 3 + 2 = 5 | P1 |

---

## 2. BreakCore（破核）

> **当前状态**: ✅ 已实现，可在训练场中测试

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 基础伤害（不触发破甲） | Fixed Roll = 4 | 伤害 = 8，不施加破甲 | P0 |
| 触发破甲（下限） | Fixed Roll = 5 | 伤害 = 8，施加 2 层破甲 | P0 |
| 触发破甲（上限） | Fixed Roll = 6 | 伤害 = 8，施加 2 层破甲 | P0 |
| 能量消耗 | Energy = 12, 打出后 | Energy = 9 | P0 |
| 破甲叠加 | Enemy Vulnerable = 1, Roll = 5 | Vulnerable = 3 | P1 |
| 新破甲不影响本次攻击 | Roll = 5, 无初始破甲 | 伤害 = 8（不受新破甲影响） | P1 |

---

## 3. QuickStrike（速击）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 固定伤害 | 任意条件 | 伤害 = 4 | P0 |
| 零能量消耗 | Energy = 0 | 可打出，Energy 不变 | P0 |
| 零骰子消耗 | DicePool = 0 | 可打出，DicePool 不变 | P0 |
| 破甲加成 | Enemy Vulnerable = 3 | 伤害 = 4 + 3 = 7 | P1 |

---

## 4. DoubleStrike（双重打击）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程（需要多骰子支持）

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 最低伤害 | Fixed Roll = 1,1 | 伤害 = floor((1+1)×1.5) = 3 | P0 |
| 最高伤害 | Fixed Roll = 6,6 | 伤害 = floor((6+6)×1.5) = 18 | P0 |
| 中等伤害 | Fixed Roll = 3,4 | 伤害 = floor((3+4)×1.5) = 10 | P1 |
| 能量消耗 | Energy = 12, 打出后 | Energy = 10 | P0 |
| 双骰子消耗 | DicePool = 2, 打出后 | DicePool = 0 | P0 |
| 破甲加成 | Enemy Vulnerable = 2, Roll = 1,1 | 伤害 = 3 + 2 = 5 | P1 |

---

## 5. DefensivePosture（防御姿态）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 能量净变化 | Energy = 5 | 消耗 1，恢复 3 → Energy = 7 | P0 |
| 能量上限 | Energy = 11, MaxEnergy = 12 | 消耗 1 → 10，恢复 3 → 12（上限） | P1 |
| 零骰子消耗 | DicePool = 0 | 可打出 | P0 |

---

## 6. DiceBoost（骰子充能）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程（需要动态骰子池）

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 骰子增加 | DicePool = 2 | 获得 1 枚额外骰子 → DicePool = 3 | P0 |
| 能量消耗 | Energy = 12 | 消耗 2 → Energy = 10 | P0 |
| 零骰子消耗 | DicePool = 0 | 可打出 | P0 |
| 回合限制 | 当前回合使用 | 新增骰子本回合可用 | P1 |

---

## 7. PowerStrike（强力打击）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程（需要多骰子支持）

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 最低伤害 | Fixed Roll = 1,1 | 伤害 = (1+1) + 6 = 8 | P0 |
| 最高伤害（不暴击） | Fixed Roll = 4,4 | 伤害 = (4+4) + 6 = 14 | P1 |
| 触发额外伤害（下限） | Fixed Roll = 4,5 | 和 = 9，额外 +5 → 伤害 = 20 | P0 |
| 触发额外伤害（上限） | Fixed Roll = 6,6 | 和 = 12，额外 +5 → 伤害 = 23 | P0 |
| 不触发额外伤害 | Fixed Roll = 4,4 | 和 = 8，不额外加 → 伤害 = 14 | P0 |
| 能量消耗 | Energy = 12 | 消耗 4 → Energy = 8 | P0 |
| 双骰子消耗 | DicePool = 2 | 消耗 2 → DicePool = 0 | P0 |

---

## 8. VulnerableStrike（破甲打击）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 最低伤害（不触发破甲） | Fixed Roll = 2 | 伤害 = 2 + 1 = 3，不施加破甲 | P0 |
| 触发破甲（下限） | Fixed Roll = 3 | 伤害 = 3 + 1 = 4，施加 1 层破甲 | P0 |
| 最高伤害 | Fixed Roll = 6 | 伤害 = 6 + 1 = 7，施加 1 层破甲 | P0 |
| 能量消耗 | Energy = 12 | 消耗 2 → Energy = 10 | P0 |
| 破甲叠加 | Enemy Vulnerable = 2, Roll = 3 | Vulnerable = 3 | P1 |

---

## 9. EnergySurge（能量涌动）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 最低恢复 | Fixed Roll = 1 | 恢复 1 + 1 = 2 Energy | P0 |
| 最高恢复 | Fixed Roll = 6 | 恢复 6 + 1 = 7 Energy | P0 |
| 能量上限 | Energy = 11, MaxEnergy = 12, Roll = 6 | 恢复到 12（上限） | P1 |
| 零能量消耗 | Energy = 0 | 可打出（无需 Energy） | P0 |
| 骰子消耗 | DicePool = 1 | 消耗 1 → DicePool = 0 | P0 |

---

## 10. CriticalHit（暴击）

> **当前状态**: ⏳ 待实现，阻塞于 cards-v0-design 线程

| 测试项 | 输入条件 | 期望结果 | 测试状态 |
|--------|----------|----------|----------|
| 最低伤害（不暴击） | Fixed Roll = 1 | 伤害 = 1 + 3 = 4 | P0 |
| 不暴击边界 | Fixed Roll = 3 | 伤害 = 3 + 3 = 6 | P0 |
| 暴击触发（下限） | Fixed Roll = 4 | 伤害 = (4 + 3) × 2 = 14 | P0 |
| 最高伤害（暴击） | Fixed Roll = 6 | 伤害 = (6 + 3) × 2 = 18 | P0 |
| 能量消耗 | Energy = 12 | 消耗 2 → Energy = 10 | P0 |
| 破甲加成（不暴击） | Enemy Vulnerable = 2, Roll = 3 | 伤害 = 6 + 2 = 8 | P1 |
| 破甲加成（暴击） | Enemy Vulnerable = 2, Roll = 4 | 伤害 = 14 + 2 = 16 | P1 |

---

## 测试优先级汇总

### P0 - 必须通过

| 卡牌 | 测试项 |
|------|--------|
| EnergyStrike | 最低伤害、最高伤害、能量消耗、骰子消耗 |
| BreakCore | 基础伤害（不触发破甲）、触发破甲（下限）、触发破甲（上限）、能量消耗 |
| QuickStrike | 固定伤害、零能量消耗、零骰子消耗 |
| DoubleStrike | 最低伤害、最高伤害、能量消耗、双骰子消耗 |
| DefensivePosture | 能量净变化、零骰子消耗 |
| DiceBoost | 骰子增加、能量消耗、零骰子消耗 |
| PowerStrike | 最低伤害、触发额外伤害（下限）、触发额外伤害（上限）、不触发额外伤害、能量消耗、双骰子消耗 |
| VulnerableStrike | 最低伤害（不触发破甲）、触发破甲（下限）、最高伤害、能量消耗 |
| EnergySurge | 最低恢复、最高恢复、零能量消耗、骰子消耗 |
| CriticalHit | 最低伤害（不暴击）、不暴击边界、暴击触发（下限）、最高伤害（暴击）、能量消耗 |

### P1 - 高优先级

| 卡牌 | 测试项 |
|------|--------|
| EnergyStrike | 破甲加成 |
| BreakCore | 破甲叠加、新破甲不影响本次攻击 |
| QuickStrike | 破甲加成 |
| DoubleStrike | 中等伤害、破甲加成 |
| DefensivePosture | 能量上限 |
| DiceBoost | 回合限制 |
| PowerStrike | 最高伤害（不暴击） |
| VulnerableStrike | 破甲叠加 |
| EnergySurge | 能量上限 |
| CriticalHit | 破甲加成（不暴击）、破甲加成（暴击） |

---

## 测试执行矩阵

| 卡牌 | 最低骰点 | 最高骰点 | 触发阈值 | 边界条件 | 组合测试 |
|------|----------|----------|----------|----------|----------|
| EnergyStrike | ✅ | ✅ | N/A | N/A | ⏳ |
| BreakCore | N/A | N/A | ✅ | ✅ | ⏳ |
| QuickStrike | N/A | N/A | N/A | N/A | ⏳ |
| DoubleStrike | ✅ | ✅ | N/A | ⏳ | ⏳ |
| DefensivePosture | N/A | N/A | N/A | ⏳ | N/A |
| DiceBoost | N/A | N/A | N/A | N/A | ⏳ |
| PowerStrike | ✅ | N/A | ✅ | ✅ | N/A |
| VulnerableStrike | ✅ | ✅ | ✅ | N/A | ⏳ |
| EnergySurge | ✅ | ✅ | N/A | ⏳ | N/A |
| CriticalHit | ✅ | ✅ | ✅ | ✅ | ⏳ |

**图例**:
- ✅: 已实现并可测试
- ⏳: 待实现或待验证
- N/A: 不适用

---

## 待实现卡牌

当前代码中仅实现了 2 张卡牌（EnergyStrike、BreakCore），以下 8 张卡牌待实现：

| 卡牌 ID | 名称 | 状态 | 依赖 |
|---------|------|------|------|
| quick_strike | QuickStrike | 待实现 | 无 |
| double_strike | DoubleStrike | 待实现 | 需要多骰子支持 |
| defensive_posture | DefensivePosture | 待实现 | 无 |
| dice_boost | DiceBoost | 待实现 | 需要动态骰子池 |
| power_strike | PowerStrike | 待实现 | 需要多骰子支持 |
| vulnerable_strike | VulnerableStrike | 待实现 | 无 |
| energy_surge | EnergySurge | 待实现 | 无 |
| critical_hit | CriticalHit | 待实现 | 无 |

### 实现优先级建议

1. **第一批次**: QuickStrike, DefensivePosture, EnergySurge, VulnerableStrike, CriticalHit（单骰子或无骰子卡牌）
2. **第二批次**: DoubleStrike, PowerStrike（双骰子卡牌）
3. **第三批次**: DiceBoost（动态骰子池）

---

## 测试用例编号映射

| 卡牌 | 关联测试用例 |
|------|-------------|
| EnergyStrike | TC-001, TC-002 |
| BreakCore | TC-003, TC-004, TC-005, TC-006, TC-012 |
| QuickStrike | TC-015（交互验证） |
| 所有卡牌 | TC-015（交互验证） |
