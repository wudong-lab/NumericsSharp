# NumericsSharp 底层数值库需求

本文记录截面特性求解器项目对 `NumericsSharp` 的依赖边界和补强需求。

## 定位

`NumericsSharp` 作为本项目的底层线性代数与线性方程求解库使用，主要承担：

- CSR 稀疏矩阵存储。
- 有限元总体矩阵装配。
- 稀疏线性方程组求解。
- 直接求解器的分析、分解、求解阶段复用。
- 迭代求解器和预条件器的可选支持。

以下内容不应放入 `NumericsSharp`，应由截面特性求解器项目自行实现：

- 截面几何模型。
- CAD 输入清理。
- 线圈截面和线宽截面拓扑。
- 曲线离散。
- 网格生成。
- 单元形函数和高斯积分。
- 几何、扭转、剪切截面特性求解流程。
- 截面结果模型和工程单位处理。

## 当前可满足的能力

当前 `NumericsSharp` 已具备以下基础能力，可作为本项目第一版数值底座：

- `CsrMatrix` 可表达有限元装配后的稀疏矩阵。
- `SparseMatrixBuilder` 支持 COO 风格累加并转换为 CSR。
- `CsrMatrixPattern` 和 `SparseMatrixAssembler` 支持固定稀疏模式下的重复装配。
- `ILinearOperator` 和 `ILinearSolver` 提供了基础求解器抽象。
- `PardisoSolver` 支持 `Analyze -> Factorize -> Solve` 三阶段流程。
- `PardisoSolver` 当前已支持多右端项求解。
- PARDISO 矩阵类型已覆盖：
  - `RealSymmetricPositiveDefinite`
  - `RealSymmetricIndefinite`
  - `RealUnsymmetric`
  - `RealStructurallySymmetric`

## 截面求解器的关键数值特征

截面扭转和剪切 FEM 常见线性系统具有以下特点：

- 基础刚度矩阵通常来自 Poisson 型问题。
- 翘曲函数存在常数不唯一性，原始系统可能是半正定或奇异的。
- 若通过固定单个自由度消除零能模态，系统可构造为近似 SPD。
- 若通过拉格朗日乘子施加平均值为零约束，系统会变成对称不定鞍点系统。
- 同一个刚度矩阵通常需要求解多个右端项，例如扭转函数、剪切函数 `psi`、剪切函数 `phi`。

因此，截面求解器不能默认所有系统都是 SPD。矩阵类型和约束处理方式必须显式配置。

## 必需补强需求

### 1. 多右端项求解提升到接口层

当前 `PardisoSolver` 已支持多右端项，但能力只暴露在具体类型上。建议将多右端项求解提升到 `IDirectSparseSolver`：

```csharp
SolverResult Solve(
    CsrMatrix matrix,
    ReadOnlySpan<double> rightHandSides,
    Span<double> solutions,
    int rightHandSideCount);
```

理由：

- 截面剪切特性通常需要对同一矩阵求解多个右端项。
- 截面求解器不应依赖 `PardisoSolver` 具体类型来使用多右端项能力。
- 便于后续接入其他直接稀疏求解器。

### 2. 显式矩阵类型配置

截面求解器侧必须能够显式指定矩阵类型：

- 固定自由度后的正定系统：可使用 `RealSymmetricPositiveDefinite`。
- 拉格朗日乘子约束系统：应使用 `RealSymmetricIndefinite`。
- 不确定对称性或调试阶段：可使用 `RealUnsymmetric` 作为保守选项。

`PardisoOptions.MatrixType` 当前已有该能力，但截面求解器集成时必须禁止隐式依赖默认值。

## 建议补强需求

### 1. 固定稀疏模式装配示例和测试

建议在 `NumericsSharp` 中补充一个小型 FEM 风格测试，覆盖：

- 初次构造 CSR pattern。
- 基于 pattern 重复装配不同数值。
- 复用直接求解器的分析阶段。
- 对多个右端项求解。

该测试不需要包含截面业务，只需验证有限元装配和求解器复用模式。

### 2. 求解结果诊断信息

当前 `SolverResult` 已包含残差信息。后续可考虑补充：

- 矩阵阶数。
- 非零元数量。
- 右端项数量。
- 求解器使用的矩阵类型。
- 分析、分解、求解耗时。

这些信息有助于截面求解器输出诊断报告，定位网格质量、奇异约束和病态矩阵问题。

### 3. 非 SPD 迭代求解器预留

当前 CG/PCG 主要适用于 SPD 系统。若后续希望在无 MKL 环境下求解拉格朗日乘子系统，需要考虑：

- MINRES：适合对称不定系统。
- GMRES / BiCGSTAB：适合一般非对称系统。

第一版不强制要求实现。主力方案仍建议使用 PARDISO 直接求解器。

## 第一版集成建议

第一版截面 FEM 求解器建议采用以下集成方式：

- 矩阵装配使用 `SparseMatrixBuilder` 或 `CsrMatrixPattern + SparseMatrixAssembler`。
- 对单次求解或原型阶段，可使用 `SparseMatrixBuilder` 简化实现。
- 对固定网格、多次求解场景，使用 `CsrMatrixPattern + SparseMatrixAssembler`。
- 直接求解优先使用 `IDirectSparseSolver`。
- 扭转和剪切共用同一个刚度矩阵时，复用 `Analyze` 和 `Factorize`。
- 使用拉格朗日乘子约束时，矩阵类型显式设置为 `RealSymmetricIndefinite`。
- 只有在确认系统为 SPD 时，才使用 CG/PCG 或 `RealSymmetricPositiveDefinite`。

## 结论

`NumericsSharp` 当前功能和接口基本满足截面特性求解器第一版原型需求，但作为正式依赖前，建议至少完成：

1. 将多右端项求解提升到 `IDirectSparseSolver` 接口。
2. 在截面求解器集成层显式配置矩阵类型，避免默认使用 SPD。

完成以上两点后，`NumericsSharp` 可以作为本项目稳定的底层数值计算基础。
