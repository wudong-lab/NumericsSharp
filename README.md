# NumericsSharp

面向有限元分析的 .NET 数值计算库，提供稀疏线性系统的组装与求解能力。

## 主要功能

- **稀疏矩阵** — CSR 格式存储，支持 COO 风格组装与固定稀疏模式的重复装配
- **直接求解** — 封装 Intel MKL PARDISO，支持 Analyze → Factorize → Solve 三阶段分离与多右端项
- **迭代求解** — 共轭梯度 (CG) 与预优共轭梯度 (PCG)
- **预条件器** — Jacobi、ILU0、Incomplete Cholesky (IC0)
- **边界条件** — Dirichlet 边界条件的消元法处理

## 项目结构

| 项目 | 说明 |
|------|------|
| `NumericsSharp.Core` | 稀疏矩阵 (CSR)、向量、组装器、边界条件 |
| `NumericsSharp.Solvers` | 迭代求解器与预条件器 |
| `NumericsSharp.Mkl` | Intel MKL PARDISO 直接求解器封装 |
| `NumericsSharp.Mkl.Native` | C++ 原生桥接层 |

## 构建

### 前置条件

- .NET 8.0 SDK
- Visual Studio 2022+ (C++ 生成工具)
- CMake ≥ 3.20
- Intel MKL（设置 `MKLROOT` 环境变量；未检测到则构建原生桩）

### 构建命令

```powershell
./tools/build.ps1            # 完整构建（包含原生库）
./tools/build.ps1 -SkipNative # 跳过原生库构建
```

## 运行 Benchmark

```powershell
./tools/benchmarks.ps1
```