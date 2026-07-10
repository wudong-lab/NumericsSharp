# NumericsSharp 后续任务列表

本文档记录当前阶段后续开发任务，主要围绕 FEM/CAD 工程软件所需的稀疏线性方程求解能力。

## 近期任务

1. [x] PARDISO 多 RHS 支持

   扩展 native `solve` 和托管 API，支持一次求解多个右端项，服务多工况和批量载荷场景。

2. [x] PARDISO 分解复用

   明确同一矩阵结构下 `Analyze -> 多次 Factorize -> 多次 Solve` 的生命周期，并补充对应测试。

3. [x] 线程配置接入

   将 `NumericsThreadingOptions` 映射到 MKL 线程控制，先支持 `NativeInnerParallel` 和 `ManagedOuterParallel` 两种模式。

4. [x] SPD 默认路径优化

   为 FEM 常用 SPD 系统提供更直接的入口，默认使用 `RealSymmetricPositiveDefinite` 和上三角 CSR。

5. [x] 约束边界条件与 PARDISO 集成测试

   使用 `DirichletBoundaryCondition` 处理后的矩阵直接走 PARDISO，验证 FEM 常见约束流程。

6. [x] 小型 FEM 示例矩阵

   添加一个小型 2D、弹簧或杆单元装配案例，覆盖装配、约束、求解和残差校核。

## 中期任务

1. native DLL 部署整理

   区分 Debug/Release native DLL 复制路径，后续为 NuGet runtime 包做准备。

2. 错误信息增强

   native 返回更具体的 PARDISO error code，托管异常包含 phase、matrix type、order、nnz 等上下文。

3. 求解器性能基准

   增加 BenchmarkDotNet 基准测试，覆盖 CG、PCG、PARDISO full CSR 和 PARDISO SPD 上三角 CSR。

4. 稀疏装配性能优化

   在当前 COO 排序合并方案基础上，评估预分配 pattern、并行分片装配和重复 factorization 场景。

5. 预条件器扩展

   在 Jacobi 之后补充 ILU0、IC0 等更适合 FEM SPD 系统的预条件器。
