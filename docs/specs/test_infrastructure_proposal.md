# Test Infrastructure Proposal

本文件调研并提议适用于当前 Godot .NET 项目的 C# 单元测试方案。

> **跨线程协调说明**：本文档中的 DiceRoller 依赖注入（DI）重构和 Mathf 替换方案涉及 `scripts/battle/` 和 `scripts/core/` 的代码修改，属于 **battle-core 线程**的管辖范围。本文档仅作提议和技术参考，实际代码修改需与 battle-core 线程正式协调后实施。

---

## 现状分析

### 当前项目特点

| 维度 | 现状 |
|------|------|
| 语言 | C# |
| 框架 | Godot 4.7-stable Mono |
| 核心模块 | PlayerState, EnemyState, CardData, CardInstance, DiceRoller, BattleManager |
| 测试方式 | 人工在训练场中验证 |
| 测试文档 | 已编写 battle_test_spec.md 和 cards_test_matrix.md |

### 核心模块可测试性评估

| 模块 | 是否依赖 Godot 场景 | 是否依赖 Godot API | 可测试性 |
|------|---------------------|---------------------|----------|
| PlayerState | 否 | 是（Mathf） | 高 |
| EnemyState | 否 | 是（Mathf） | 高 |
| CardData | 否 | 否 | 高 |
| CardInstance | 否 | 否 | 高 |
| DiceRoller | 否 | 是（RandomNumberGenerator, Mathf） | 高 |
| BattleManager | 是（Node, Signal） | 是（Signal） | 中 |
| BattleLogPanel | 是（Control, RichTextLabel） | 是（Control, RichTextLabel, Time） | 低 |
| TrainingGroundController | 是（场景节点） | 是（多个 UI 节点） | 低 |

---

## 测试框架选择

### 方案对比

| 方案 | 优点 | 缺点 | 适用场景 |
|------|------|------|----------|
| xUnit | .NET 生态标准，轻量级，并行执行 | 需要适配 Godot API | 核心逻辑单元测试 |
| NUnit | 成熟稳定，功能丰富 | 比 xUnit 重 | 大型项目 |
| Godot 内置测试 | 直接集成，支持场景测试 | 文档较少，学习成本 | 场景集成测试 |
| 自定义测试框架 | 完全可控 | 重复造轮子 | 不推荐 |

### 推荐方案

**推荐使用 xUnit**，理由如下：

1. **.NET 标准**: xUnit 是 .NET 官方推荐的测试框架，与 Godot .NET 集成良好
2. **轻量级**: 核心逻辑单元测试不需要复杂的断言库
3. **并行执行**: 支持并行测试，提高执行效率
4. **社区支持**: 大量文档和资源

---

## 架构设计

### 分层测试策略

```text
┌─────────────────────────────────────────────────────────────┐
│                    集成测试层                                │
│  TrainingGroundScene 场景测试、完整战斗流程测试               │
├─────────────────────────────────────────────────────────────┤
│                    单元测试层                                │
│  PlayerState、EnemyState、CardData、DiceRoller 纯逻辑测试    │
└─────────────────────────────────────────────────────────────┘
```

### 项目结构

```text
game/
├── NewGame.csproj              # 主项目
├── scripts/                    # 核心逻辑
│   ├── core/                   # 状态和数据模型
│   ├── battle/                 # 战斗逻辑
│   ├── training/               # 训练场逻辑
│   └── ui/                     # UI 逻辑
├── tests/                      # 测试项目
│   ├── NewGame.Tests.csproj    # 测试项目
│   ├── core/                   # 核心模块测试
│   │   ├── PlayerStateTests.cs
│   │   ├── EnemyStateTests.cs
│   │   ├── CardDataTests.cs
│   │   └── CardInstanceTests.cs
│   ├── battle/                 # 战斗模块测试
│   │   ├── DiceRollerTests.cs
│   │   └── BattleManagerTests.cs
│   └── GlobalUsings.cs         # 全局 using
└── docs/
	└── tests/                  # 测试文档
```

---

## Godot API 隔离方案

### 问题

核心模块依赖 Godot API（如 `Mathf`、`RandomNumberGenerator`、`Signal`），直接在单元测试中运行会导致：
- 需要启动 Godot 运行时
- 测试速度慢
- 难以隔离

### 解决方案

#### 1. 依赖注入（DI）

为 `DiceRoller` 抽象随机数生成器接口：

```csharp
public interface IRandomNumberGenerator
{
	int RandiRange(int min, int max);
	void Randomize();
}

public class GodotRNG : IRandomNumberGenerator
{
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	
	public int RandiRange(int min, int max) => _rng.RandiRange(min, max);
	public void Randomize() => _rng.Randomize();
}

public class MockRNG : IRandomNumberGenerator
{
	public int NextValue { get; set; } = 1;
	
	public int RandiRange(int min, int max) => NextValue;
	public void Randomize() { }
}
```

#### 2. Mathf 替换

使用 `System.Math` 替换 `Godot.Mathf`：

| Godot.Mathf | System.Math | 说明 |
|-------------|-------------|------|
| `Mathf.Min(a, b)` | `Math.Min(a, b)` | 最小值 |
| `Mathf.Max(a, b)` | `Math.Max(a, b)` | 最大值 |
| `Mathf.Clamp(v, min, max)` | 自定义方法 | 范围限制 |

```csharp
public static class MathExtensions
{
	public static int Clamp(this int value, int min, int max)
	{
		return Math.Max(min, Math.Min(max, value));
	}
}
```

#### 3. Signal 替换

对于 `BattleManager` 的测试，使用接口或回调替代 Signal：

```csharp
public interface IBattleLogger
{
	void Log(string message);
}

public class BattleManager
{
	public IBattleLogger Logger;
	
	public void EmitBattleLog(string message)
	{
		Logger?.Log(message);
	}
}
```

---

## 测试项目配置

### NewGame.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
	<TargetFramework>net8.0</TargetFramework>
	<ImplicitUsings>enable</ImplicitUsings>
	<Nullable>enable</Nullable>
	
	<IsPackable>false</IsPackable>
	<IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
	<PackageReference Include="xunit" Version="2.7.2" />
	<PackageReference Include="xunit.runner.visualstudio" Version="2.7.2" />
	<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  </ItemGroup>

  <ItemGroup>
	<ProjectReference Include="..\NewGame.csproj" />
  </ItemGroup>

</Project>
```

### GlobalUsings.cs

```csharp
global using Xunit;
global using FluentAssertions; // 可选，增强断言
```

---

## 测试用例示例

### PlayerStateTests.cs

```csharp
public class PlayerStateTests
{
	[Fact]
	public void TakeDamage_DeductsEnergyFirst()
	{
		// Arrange
		var player = new PlayerState
		{
			Hp = 30,
			MaxHp = 30,
			Energy = 12,
			MaxEnergy = 12
		};
		
		// Act
		player.TakeDamage(14);
		
		// Assert
		player.Energy.Should().Be(0);
		player.Hp.Should().Be(30);
	}
	
	[Fact]
	public void TakeDamage_DeductsHpWhenEnergyDepleted()
	{
		// Arrange
		var player = new PlayerState
		{
			Hp = 30,
			MaxHp = 30,
			Energy = 5,
			MaxEnergy = 12
		};
		
		// Act
		player.TakeDamage(14);
		
		// Assert
		player.Energy.Should().Be(0);
		player.Hp.Should().Be(21);
	}
	
	[Fact]
	public void RestoreEnergy_RespectsMaxEnergy()
	{
		// Arrange
		var player = new PlayerState
		{
			Energy = 10,
			MaxEnergy = 12
		};
		
		// Act
		player.RestoreEnergy(5);
		
		// Assert
		player.Energy.Should().Be(12);
	}
}
```

### DiceRollerTests.cs

```csharp
public class DiceRollerTests
{
	[Fact]
	public void Roll_FixedMode_ReturnsFixedValue()
	{
		// Arrange
		var roller = new DiceRoller
		{
			Mode = RollMode.Fixed,
			FixedValue = 3
		};
		
		// Act
		var result = roller.Roll(6);
		
		// Assert
		result.Should().Be(3);
	}
	
	[Fact]
	public void Roll_FixedMode_ClampsToValidRange()
	{
		// Arrange
		var roller = new DiceRoller
		{
			Mode = RollMode.Fixed,
			FixedValue = 10
		};
		
		// Act
		var result = roller.Roll(6);
		
		// Assert
		result.Should().Be(6);
	}
	
	[Fact]
	public void Roll_RandomMode_ReturnsValidRange()
	{
		// Arrange
		var roller = new DiceRoller
		{
			Mode = RollMode.Random
		};
		
		// Act
		var results = Enumerable.Range(0, 100)
			.Select(_ => roller.Roll(6))
			.ToList();
		
		// Assert
		results.All(r => r >= 1 && r <= 6).Should().BeTrue();
	}
}
```

### CardDataTests.cs

```csharp
public class CardDataTests
{
	[Fact]
	public void EnergyStrike_CalculatesDamageCorrectly()
	{
		// Arrange
		var card = new CardInstance(CardData.EnergyStrike);
		var dice = new DiceInstance(6) { Value = 5 };
		
		// Act
		var damage = card.CalculateDamage(dice);
		
		// Assert
		damage.Should().Be(7); // 5 + 2
	}
	
	[Fact]
	public void BreakCore_AppliesVulnerableWhenDiceHigh()
	{
		// Arrange
		var card = new CardInstance(CardData.BreakCore);
		var dice = new DiceInstance(6) { Value = 5 };
		var enemy = new EnemyState("TestEnemy", 100);
		
		// Act
		card.Data.ApplyEffect?.Invoke(card, dice, enemy);
		
		// Assert
		enemy.GetVulnerableStacks().Should().Be(2);
	}
	
	[Fact]
	public void BreakCore_DoesNotApplyVulnerableWhenDiceLow()
	{
		// Arrange
		var card = new CardInstance(CardData.BreakCore);
		var dice = new DiceInstance(6) { Value = 4 };
		var enemy = new EnemyState("TestEnemy", 100);
		
		// Act
		card.Data.ApplyEffect?.Invoke(card, dice, enemy);
		
		// Assert
		enemy.GetVulnerableStacks().Should().Be(0);
	}
}
```

---

## 测试执行流程

### 本地执行

```bash
# 进入项目目录
cd game

# 运行所有测试
dotnet test --no-restore

# 运行特定测试文件
dotnet test --no-restore --filter FullyQualifiedName~PlayerStateTests

# 生成测试报告
dotnet test --no-restore --logger "html;LogFileName=test-report.html"
```

### CI/CD 集成

在 GitHub Actions 中添加测试步骤：

```yaml
name: Test

on:
  push:
	branches: [ develop, main ]
  pull_request:
	branches: [ develop ]

jobs:
  test:
	runs-on: ubuntu-latest
	
	steps:
	- uses: actions/checkout@v4
	
	- name: Setup .NET
	  uses: actions/setup-dotnet@v4
	  with:
		dotnet-version: '8.0.x'
	
	- name: Restore dependencies
	  run: dotnet restore
	
	- name: Build
	  run: dotnet build --no-restore
	
	- name: Test
	  run: dotnet test --no-build --verbosity normal
```

---

## 测试覆盖目标

### 核心模块覆盖率

| 模块 | 目标覆盖率 | 优先测试 |
|------|------------|----------|
| PlayerState | 80% | TakeDamage, RestoreEnergy, CanPlayCard |
| EnemyState | 80% | TakeDamage, CalculateDamage, AddVulnerable |
| CardData | 90% | 所有卡牌的 DamageFormula 和 ApplyEffect |
| CardInstance | 80% | CalculateDamage |
| DiceRoller | 90% | Roll（Fixed 和 Random 模式） |
| BattleManager | 60% | TryPlayCard, ExecuteEnemyTurn |

### 测试用例数量目标

| 阶段 | 测试用例数 | 时间 |
|------|------------|------|
| 第一阶段 | 20+ | 卡牌 v0 实现前 |
| 第二阶段 | 50+ | 训练场稳定后 |
| 第三阶段 | 100+ | 正式战斗前 |

---

## 实施步骤

### 第一步：搭建测试项目结构

1. 创建 `tests/NewGame.Tests.csproj`
2. 创建 `tests/GlobalUsings.cs`
3. 添加 xUnit 包引用

### 第二步：隔离 Godot API

1. 创建 `IRandomNumberGenerator` 接口
2. 创建 `MathExtensions` 静态类
3. 修改 `DiceRoller` 使用接口注入

### 第三步：编写核心测试

1. `PlayerStateTests.cs`
2. `EnemyStateTests.cs`
3. `DiceRollerTests.cs`
4. `CardDataTests.cs`

### 第四步：运行和验证

1. 运行 `dotnet test`
2. 检查测试通过情况
3. 修复失败的测试

### 第五步：扩展测试覆盖

1. 添加 `BattleManagerTests.cs`
2. 添加 `CardInstanceTests.cs`
3. 集成测试报告

---

## 风险与注意事项

### 技术风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Godot API 依赖 | 难以在单元测试中运行 | 使用依赖注入隔离 |
| 测试与运行时代码不一致 | 测试通过但实际运行失败 | 定期在训练场验证 |
| 测试执行速度 | 大量测试导致 CI 慢 | 并行执行，分层测试 |

### 管理风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 测试维护成本 | 代码变更需要同步更新测试 | 先写测试再实现（TDD） |
| 测试覆盖率不足 | 遗漏重要逻辑 | 定期审查覆盖率报告 |
| 测试与规格不一致 | 测试验证错误的行为 | 测试用例基于 battle_test_spec.md |

---

## 总结

推荐使用 **xUnit** 作为测试框架，通过 **依赖注入** 隔离 Godot API，实现核心逻辑的纯单元测试。

**核心优势**:
1. 无需启动 Godot 即可运行测试，执行速度快
2. 测试隔离性好，便于定位问题
3. 与 .NET 生态无缝集成，支持 CI/CD
4. 为后续功能扩展提供可靠的回归测试保障

**下一步行动**:
1. battle-core 线程修复 ISSUE-002 和 ISSUE-003（无骰卡牌缺陷和 static RNG）
2. battle-log-and-tests 线程搭建测试项目结构
3. 编写第一批核心模块测试用例
