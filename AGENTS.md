# AGENTS.md

本文件定义本项目中 AI 助手、代码代理与人工协作者的工作边界、文件规范、开发循环和验收标准。  
所有自动化修改、跨文件重构、代码审查、规格核对和内容落地，都应优先遵守本文。

## 0. 项目原则

这是一个从 0 开始搭建的 Godot 游戏项目。当前阶段优先级如下：

1. 先做可运行、可验证、可迭代的最小原型。
2. 任何功能都必须能回到清晰的规格、状态变量和验收条件。
3. Godot 是唯一游戏运行时和最终整合中心。
4. 叙事、交互、资产、音频和工程文件分层管理，不把临时想法直接塞进运行时代码。
5. 小步提交、可回滚、可审查；避免一次性大改。

## 0.1 当前游戏核心

当前项目是一个卡牌 + 骰子 + 回合制策略 + 爬塔原型。

核心机制：

1. Energy 同时是出牌资源和护盾。
2. 玩家每回合获得未揭示骰池；打出需要骰子的卡牌时，系统自动消耗骰子并当场掷骰结算。
3. 卡牌通过能量消耗、骰子消耗、骰点触发和状态效果产生战斗结果。
4. 敌人使用可预判 Intent 行动，不在当前阶段实现完整敌方抽卡系统。
5. 队友不是独立角色，而是提供额外骰子、专属卡牌和被动效果。
6. 当前卡牌交互为单击预览、双击打出。
7. 当前阶段以一场可玩的战斗闭环为最高优先级。

当前核心体验：

玩家每次出牌都在消耗自己的防御资源，并可能消耗未知骰子；需要在输出、状态构筑、保留能量防御和赌骰收益之间做取舍。

## 1. 最终工作栈

### 1.1 设计与叙事

- Arrow：用于剧情分支、地图节点、事件流转和复杂状态流转的可视化设计；普通战斗功能不强制使用。
- Markdown：用于世界观、场景真相、系统规则、功能规格和验收标准。
- CSV/XLSX：用于卡牌、敌人、状态、队友、奖励、资源清单、测试用例和内容表。

### 1.2 游戏开发

- Godot 4.x .NET：唯一游戏运行时和最终整合中心。
- 当前项目使用 C#。
- 当前项目使用 Compatibility 渲染器。
- 当前阶段以 2D UI、Sprite2D、AnimatedSprite2D 和处理后的像素序列图为主。
- Trae CN：日常代码生产、局部功能实现和调试工作台。
- Codex：代码审查、跨文件修补、重构、测试、规格核对和工程一致性检查。

### 1.3 视觉生产

- Blender 5.1：场景灰盒、空间比例、镜头、复杂机械结构和透视参考。
- Aseprite：角色形象、场景像素资产、UI、图标和逐帧动画母版。
- Pixel Composer：故障、扫描、数据流、时间回写和像素化演出效果。

### 1.4 音频生产

- Studio One：音频工程中心，负责作曲、编曲、剪辑、混音和导出。
- Serum 2：合成音乐层、机械音、UI 音、数据音和科幻音效设计。
- Melodyne 5：离线音符级修正、扒谱辅助、人声与器乐音高校正。
- FabFilter：默认精准 EQ、压缩、动态和母带工具。
- oeksound：针对刺耳共振、瞬态和音色失衡的专项修复。
- Plugin Alliance：模拟硬件、染色、总线质感和特色处理。
- Waves：仅在需要特定经典插件或特殊后期功能时启用。
- Antares：仅在正式制作演唱、人声校音或风格化角色声音时启用。

### 1.5 工程基础设施

- Git / GitHub：版本控制、分支、代码审查和任务追踪。
- Git LFS：管理 `.blend`、`.aseprite`、大型 WAV、DAW 工程等二进制文件。

## 2. 最低工作循环

所有可进入开发的功能，默认遵守以下循环：

1. Markdown 写明功能规格和验收条件。
2. 若功能涉及剧情分支、地图节点、事件流转，再使用 Arrow。
3. 创建 Git 功能分支。
4. Trae CN 实现。
5. 本地运行 Godot 验证。
6. 提交代码差异。
7. Codex 审查和修补。
8. 再次运行与测试。
9. 合并到 `develop`。

如果某一步暂时无法完成，必须在对应 Markdown 规格或提交说明中写明原因、风险和后续补齐方式。

## 3. 推荐目录结构

项目可以逐步演化为以下结构。没有实际内容前，不必空建所有目录。

```text
res://
  scenes/
    main/
    battle/
    card/
    dice/
    enemy/
    reward/
    tavern/
    ui/
  scripts/
    core/
    battle/
    cards/
    dice/
    enemies/
    statuses/
    allies/
    rewards/
    tavern/
    ui/
    debug/
  assets/
    art/
      sprites/
        player/
        enemies/
        allies/
        cards/
        dice/
        vfx/
      ui/
      backgrounds/
    audio/
      music/
      sfx/
      voice/
  data/
    cards/
    enemies/
    statuses/
    allies/
    encounters/
    rewards/
  docs/
    design/
    specs/
    tests/
    balance/
  source_assets/
    blender/
    aseprite/
    pixel_composer/
    audio_projects/
  addons/
```

`source_assets/` 用于保存 Blender、Aseprite、Pixel Composer 和音频工程源文件。  
Godot 运行时真正使用的导入资产放在 `assets/` 下。

如果 `source_assets/` 位于 Godot 项目根目录内，必须放置 `.gdignore`，避免 Godot 导入 Blender、Aseprite、Pixel Composer、DAW 工程和大型临时渲染序列。

更推荐的仓库结构是：

```text
repo_root/
  game/
    project.godot
    scenes/
    scripts/
    assets/
    data/
    docs/
    addons/
  source_assets/
    blender/
    aseprite/
    pixel_composer/
    audio_projects/
```

其中 `game/` 是 Godot 项目根目录，`source_assets/` 是外部源资产区。

## 4. 文档规范

### 4.1 Markdown 规格

每个准备实现的功能，至少应包含：

- 功能目的：这个功能解决什么玩家体验问题。
- 玩家输入：玩家能做什么。
- 系统反馈：游戏如何响应。
- 状态变量：新增或修改哪些变量。
- 失败状态：哪些情况算失败、异常或边界。
- 验收标准：怎样确认它做完了。
- 测试步骤：在 Godot 中如何复现和验证。

建议路径：

```text
docs/specs/功能名.md
docs/narrative/章节或场景名.md
docs/tests/功能名_test.md
```

### 4.2 表格数据

CSV/XLSX 用于稳定、可批量编辑的数据，例如：

- 卡牌表
- 骰子表
- 状态表
- 敌人表
- 敌人 Intent 表
- 队友表
- 奖励表
- 遭遇表
- 场景资源清单
- 测试用例表

运行时代码不应依赖未整理的临时表格。进入游戏前，应转换为 Godot 可稳定读取的数据格式，或明确记录读取方式。

## 5. Godot 开发规则

### 5.1 运行时边界

- Godot 4.x .NET 是唯一运行时。
- 当前项目使用 C#。
- 不引入额外游戏引擎或并行运行时。
- 外部工具产物最终都必须能被 Godot 导入、加载或引用。

### 5.2 场景与脚本

- 场景文件使用 `.tscn`。
- 脚本使用 C#，除非已有明确理由，不混用 GDScript 承担核心战斗逻辑。
- 节点命名应表达职责，例如 `BattleScene`、`CardView`、`DiceSlot`、`EnemyIntentView`。
- 避免把大量逻辑写在 `Main` 场景中；核心逻辑应拆到明确脚本或组件。

### 5.3 状态管理

涉及战斗结果、卡牌效果、骰子结算、状态变化、奖励选择或队友效果的功能，必须能追溯到：

- Markdown 中的规格说明。
- 表格或数据文件中的变量定义。
- 必要时，Arrow 中的剧情、节点或事件流转。

不要在脚本中散落“魔法字符串”表示卡牌 ID、状态 ID、敌人 ID、意图 ID 或剧情状态。需要复用的 ID 应集中定义或由数据源提供。

### 5.4 验证

每次功能修改后，至少做一种验证：

- 在 Godot 编辑器中运行主场景或测试场景。
- 运行相关单元测试、脚本检查或项目导入检查。
- 手动按规格中的测试步骤复现。

如果无法运行 Godot，必须在回复或提交说明中明确说明“未运行”的原因。

### 5.5 像素风与渲染设置

- 当前项目使用 Compatibility 渲染器。
- 当前阶段以 2D UI 和像素序列图为主。
- 像素资源默认关闭平滑过滤，优先使用 Nearest。
- UI 文字应保证可读性，不强制全部低分辨率像素化。
- 不在核心玩法未验证前投入复杂 3D 后处理、实时描边或像素化 shader。

### 5.6 数据模型规范

当前 demo 至少包含以下运行时概念：

- `PlayerState`：玩家 HP、Energy、DicePool、Deck、Hand、Statuses。
- `EnemyState`：敌人 HP、Shield、Intent、Statuses。
- `CardData`：卡牌静态数据。
- `CardInstance`：战斗中的卡牌实例。
- `DiceData`：骰子静态数据。
- `DiceInstance`：本回合获得的未揭示骰子实例，出牌时才掷出并消耗。
- `StatusInstance`：状态类型、层数、持续时间。
- `EnemyIntent`：敌人下一步行动说明。
- `AllyData`：队友提供的骰子、卡牌和被动。

第一版可以在 C# 中写死数据库；当卡牌、敌人、状态数量增加后，再迁移到 JSON、CSV 或 Godot Resource。

### 5.7 当前战斗交互规则

当前 demo 的卡牌交互规则：

- 单击卡牌：显示卡牌预览，不结算效果。
- 再次单击当前已预览卡牌或执行取消预览操作时，必须同时隐藏预览文字和预览深色背景面板。
- 双击卡牌：尝试打出卡牌。
- 打出卡牌时，系统先检查 Energy 是否足够。
- 需要骰子的卡牌还要检查是否有未消耗骰子。
- 检查通过后，系统消耗 Energy，并自动消耗最左侧可用骰子。
- 被消耗的骰子在打出瞬间掷出，骰点用于结算卡牌效果。
- 骰点影响结算结果，不应导致卡牌完全无效果；低点应有基础效果，高点可触发额外奖励。
- 当前 demo 不做手动选骰、拖拽绑骰或提前显示骰点。
- 不能打出的卡牌仍应允许单击预览，但双击时不应结算。

当前卡牌预览至少显示：

- 卡牌名称。
- 类型。
- 目标。
- Energy 消耗。
- Dice 消耗。
- 伤害或效果范围。
- 效果描述。

实现预览 UI 时，文字节点和背景节点必须视为同一个显示状态：显示时一起显示，隐藏时一起隐藏。出牌、回合切换、战斗胜利、战斗失败、取消预览等场景都不能只隐藏其中一个。

## 6. Git 工作流

### 6.1 分支

建议分支模型：

- `main`：稳定版本。
- `develop`：日常整合分支。
- `feature/<功能名>`：单个功能。
- `fix/<问题名>`：缺陷修复。
- `docs/<主题名>`：文档修改。
- `art/<资产名>`：资产导入或整理。
- `audio/<资产名>`：音频导入或整理。

### 6.2 提交

提交应尽量小而清晰。推荐格式：

```text
type(scope): summary
```

示例：

```text
feat(battle): add turn start draw and dice roll
feat(cards): add basic attack card resolution
fix(energy): prevent enemy attack from reducing hp before energy
docs(spec): define minimum battle loop
art(cards): import placeholder card icons
audio(sfx): add placeholder card confirm sound
```

常用 type：

- `feat`：新功能
- `fix`：修复
- `docs`：文档
- `refactor`：重构
- `test`：测试
- `art`：美术资产
- `audio`：音频资产
- `chore`：工程杂项

### 6.3 Git LFS

以下类型应通过 Git LFS 管理：

- `.blend`
- `.aseprite`
- `.psd`
- `.kra`
- `.wav`
- `.flac`
- `.studioone`
- 大型视频、参考图、渲染序列和 DAW 工程文件

修改 LFS 配置前，先检查 `.gitattributes`，避免重复或冲突规则。

### 6.4 不要提交

不要提交：

- `.godot/`
- `.import/` 旧版本缓存目录
- 临时导出包
- Godot 自动生成的本地编辑器缓存
- `.mono/`
- `.vs/`
- `.idea/`
- `bin/`
- `obj/`
- 未经整理的大型参考图和临时渲染序列

Godot .NET 项目的 `.csproj` 和 `.sln` 属于工程文件，默认应提交；`bin/`、`obj/`、IDE 缓存不提交。

## 7. AI 协作角色

### 7.1 Trae CN

Trae CN 主要负责：

- 日常代码生产。
- 局部功能实现。
- 快速调试。
- 根据明确规格完成单点任务。

Trae CN 输出代码时，应尽量同步更新相关规格或测试说明。

### 7.2 Codex

Codex 主要负责：

- 代码审查。
- 跨文件修补。
- 重构。
- 测试与验证。
- 规格核对。
- 检查实现是否偏离 Markdown / Arrow / 表格定义。

Codex 修改项目时应：

1. 先检查当前文件结构和已有约定。
2. 尽量只改与任务相关的文件。
3. 避免覆盖用户未提交的修改。
4. 修改后说明改了什么、如何验证、还有什么风险。

## 8. 资产生产与导入规则

### 8.1 美术

- Blender 用于灰盒、空间、镜头和复杂结构参考。
- Aseprite 是像素资产母版来源。
- Pixel Composer 用于特效演出和后期像素效果。
- 导入 Godot 的资源应有清晰命名和用途说明。

当前阶段美术策略：

- Blender 主要作为建模、摆姿、渲染参考和预渲染工具。
- Godot 中优先使用处理后的 PNG 序列图、SpriteSheet、Sprite2D、AnimatedSprite2D。
- 当前 demo 不使用实时 3D 角色作为主要战斗对象。
- 正式美术前，允许使用色块、临时图标和占位 Sprite。
- 不因缺少正式美术阻塞核心战斗系统开发。

建议命名：

```text
player_core_idle_v001.png
player_blade_attack_v001.png
enemy_beast_idle_v001.png
enemy_mage_attack_v001.png
ally_mech_swordsman_portrait_v001.png
card_energy_strike_icon_v001.png
dice_player_d6_v001.png
fx_energy_shield_break_v001.png
fx_burn_loop_v001.png
```

### 8.2 音频

- Studio One 是音频工程中心。
- Serum 2、Melodyne 5 和各类插件用于制作流程，不应成为 Godot 运行时依赖。
- 导出到 Godot 的音频应优先使用清晰的最终资产格式。

建议分类：

```text
assets/audio/music/
assets/audio/sfx/
assets/audio/voice/
```

建议命名：

```text
music_area_lab_loop_v001.wav
sfx_ui_confirm_v001.wav
voice_character_lineid_v001.wav
```

## 9. 验收标准模板

每个功能的验收条件建议使用以下格式：

```markdown
## 验收标准

- [ ] 玩家可以在指定场景中触发该功能。
- [ ] 功能结果符合 Markdown 规格中定义的战斗、卡牌、骰子或状态结算。
- [ ] 若涉及剧情、地图节点或事件流转，结果符合 Arrow 中定义的状态流转。
- [ ] 涉及的状态变量有明确初始值、变化条件和结果。
- [ ] 失败或边界情况不会导致游戏卡死。
- [ ] Godot 本地运行通过。
- [ ] 相关文档、数据表或测试说明已更新。
```

## 10. 当前阶段建议

当前阶段目标不是完整爬塔，而是跑通一个最小战斗 demo。

优先完成以下闭环：

1. 一个可运行主场景。
2. 一个可进入的 `BattleScene`。
3. 玩家拥有 HP、Energy、DicePool、Deck、Hand。
4. 敌人拥有 HP、Shield、Intent、Statuses。
5. 每回合开始恢复能量、抽牌，并获得未揭示骰池。
6. 玩家单击卡牌显示预览，双击卡牌尝试打出。
7. 需要骰子的卡牌会自动消耗骰子并当场掷骰结算。
8. 出牌会消耗能量。
9. 卡牌可以造成伤害、施加状态或恢复能量。
10. 敌人攻击优先扣除玩家 Energy，Energy 不足时扣 HP。
11. 敌人死亡后进入 `RewardScene`。
12. 奖励选择的卡牌能加入卡组。
13. `TavernScene` 中可以招募一个队友。
14. 队友能增加一个骰子、加入卡牌或提供被动。
15. 下一场战斗能体现队友带来的变化。

当前 demo 暂不实现：

- 完整爬塔地图。
- 完整职业系统。
- 复杂元素反应。
- 普通怪部位破坏。
- 队友独立血条。
- 敌人完整抽卡系统。
- 正式美术资源。
- 实时 3D 角色渲染。

任何宏大系统都应先压缩成这个最小闭环，再逐步扩展。

当前第一阶段最小实现顺序：

1. 先只实现一张测试卡：`EnergyStrike`。
2. `EnergyStrike`：消耗 1 Energy 和 1 枚默认骰；双击卡牌即打出，当场掷骰，造成 `骰点 + 2` 伤害。
3. 先只实现一个敌人：`TrainingBeast`。
4. `TrainingBeast`：拥有 20 HP；当前调试阶段每回合 Intent 为造成 14 点伤害，方便验证 Energy 不足时 HP 会正常减少，以及失败状态能触发。
5. 先只实现玩家 30 HP、12 Energy、2d6。
6. 先不实现完整洗牌、奖励、酒馆、队友。
7. 当单张卡牌、自动消耗未揭示骰、单个敌人的战斗能跑通后，再扩展 Deck、Status、Reward、Tavern。

## 11. 禁止或谨慎事项

- 不要在没有规格的情况下直接扩展大系统。
- 不要一次性重构大量无关文件。
- 不要把源资产、导出资产和运行时代码混在同一层级。
- 不要手动编辑 Godot 自动生成的 `.import` 文件，除非明确知道原因。
- 不要提交 `.godot/` 缓存目录。
- 不要在战斗闭环验证前优先投入完整爬塔、完整职业、实时 3D 角色或复杂 shader。
- 不要把大型二进制文件直接放入普通 Git 历史，应使用 Git LFS。
- 不要删除或覆盖用户未确认的工作成果。

## 12. 交接说明

当一个 AI 助手完成任务时，交接回复应包含：

- 改动摘要。
- 修改文件列表。
- 验证方式。
- 未完成事项或风险。
- 建议的下一步。

如果只是审查而未修改，也应明确说明“未修改文件”。
