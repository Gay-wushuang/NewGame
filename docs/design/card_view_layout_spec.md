# CardView Layout Spec v0.1

`CardView.tscn` 是所有卡牌正反面的唯一通用模板。手牌、抽牌堆、弃牌堆、消耗堆、牌包和奖励预览都应实例化这个场景，不为单张卡牌另做一套布局。

## 母版尺寸

- 设计母版：`512x768`
- 显示基准：`192x288`
- 比例：`2:3`
- 手牌显示：约 `150x225`
- 弹窗显示：约 `148x222`

## 正面布局

```text
512x768

24,24   - 488,88    TopBar
32,104  - 480,424   ArtFrame
32,440  - 480,488   StatsBar
32,504  - 480,720   TextBox
```

### TopBar

TopBar 只放通用识别信息：

- 左上：Energy 消耗，显示 `N`
- 中间：卡牌名称
- 右上：Dice 消耗，显示 `N`

不要把卡牌效果、目标、伤害范围塞进 TopBar。

### ArtFrame

ArtFrame 是卡牌主图槽位：

- 槽位尺寸：`448x320`
- 像素占位建议：`224x160` 后 2 倍 Nearest 放大
- AI 概念图建议先裁到 `448x320`

ArtFrame 只显示图，不显示规则文字。

### StatsBar

StatsBar 只放一行简短分类/数值：

```text
Attack - DMG 3-8
Skill - Shield 5
Buff - Effect 3
```

卡面未被选中时显示基础数值；被单击预览后，允许根据敌我状态、骰子类型、破甲等上下文显示当前预测值。

### TextBox

TextBox 放通用规则说明：

- 不重复 Energy / Dice 消耗
- 不写长句式条件，例如“若……则……”
- 优先写短句：`Deal dice + 2 damage.`
- 复杂变化放到单击预览面板中解释

## 反面布局

反面仍在同一个 `CardView.tscn` 内，通过 `ShowBack` 切换：

- `FrontFace`：卡牌正面
- `BackFace`：卡牌背面

牌背用于抽牌堆、未揭示卡、转场动画和未来隐藏信息展示，不需要为正反面拆两个场景。

## 后续扩展

后续接入正式卡图时，应优先给 `CardView.cs` 增加 `Texture2D ArtTexture` 或通过 `CardData.VisualKey` 映射到卡图资源，再让 `ArtFrame` 内部显示 TextureRect。不要在每张卡牌实例里手摆图片节点。
