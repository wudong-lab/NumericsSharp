using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Core.Threading;
using NumericsSharp.Mkl;
using NumericsSharp.Mkl.Native;
using NumericsSharp.Mkl.Tests.Native;
using NumericsSharp.Mkl.Pardiso;
using NumericsSharp.Solvers.LinearSolvers;

namespace NumericsSharp.Mkl.Tests.Pardiso;

public sealed class PardisoSolverTests
{
    [Fact]
    public void Analyze_AcceptsSquareCsrMatrix()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(1, 1, 3.0);

        using var solver = CreateSpdSolver();

        solver.Analyze(builder.ToCsr());

        Assert.True(solver.IsAnalyzed);
        Assert.False(solver.IsFactorized);
    }

    [Fact]
    public void PardisoSolver_ImplementsDirectSparseSolverInterface()
    {
        using IDirectSparseSolver solver = CreateSpdSolver();

        Assert.False(solver.IsAnalyzed);
        Assert.False(solver.IsFactorized);
    }

    [Fact]
    public void Options_UseExplicitMatrixType()
    {
        var options = new PardisoOptions(PardisoMatrixType.RealSymmetricIndefinite);

        Assert.Equal(PardisoMatrixType.RealSymmetricIndefinite, options.MatrixType);
    }

    [Fact]
    public void Constructor_RejectsInvalidThreadingOptions()
    {
        var options = new PardisoOptions(PardisoMatrixType.RealSymmetricPositiveDefinite)
        {
            Threading = new NumericsThreadingOptions
            {
                NativeThreadCount = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new PardisoSolver(options));
    }

    [Fact]
    public void Analyze_AppliesManagedOuterParallelThreadingMode()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var matrix = CreateDiagonalMatrix(2.0, 3.0);
        using var solver = new PardisoSolver(
            new PardisoOptions(PardisoMatrixType.RealSymmetricPositiveDefinite)
            {
                Threading = new NumericsThreadingOptions
                {
                    Mode = ParallelMode.ManagedOuterParallel,
                    MaxDegreeOfParallelism = 2,
                    NativeThreadCount = 8
                }
            });

        solver.Analyze(matrix);

        Assert.True(solver.IsAnalyzed);
    }

    [Fact]
    public void Solve_ComputesSmallSpdSystemWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var solution = new double[2];
        using var solver = CreateSpdSolver();

        try
        {
            var result = solver.Solve(matrix, [1.0, 2.0], solution);

            Assert.True(result.Converged);
            Assert.True(solver.IsAnalyzed);
            Assert.True(solver.IsFactorized);
            Assert.InRange(Math.Abs(solution[0] - 1.0 / 11.0), 0.0, 1e-12);
            Assert.InRange(Math.Abs(solution[1] - 7.0 / 11.0), 0.0, 1e-12);
            Assert.InRange(result.FinalResidualNorm, 0.0, 1e-12);
        }
        catch (MklBackendException exception)
        {
            Assert.Equal((int)MklNativeStatus.MklError, exception.StatusCode);
        }
    }

    [Fact]
    public void Solve_ComputesSmallSpdSystemWithSymmetricPositiveDefiniteMatrixType()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var solution = new double[2];
        using var solver = new PardisoSolver(
            new PardisoOptions(PardisoMatrixType.RealSymmetricPositiveDefinite));

        var result = solver.Solve(matrix, [1.0, 2.0], solution);

        Assert.True(result.Converged);
        Assert.True(solver.IsAnalyzed);
        Assert.True(solver.IsFactorized);
        Assert.InRange(Math.Abs(solution[0] - 1.0 / 11.0), 0.0, 1e-12);
        Assert.InRange(Math.Abs(solution[1] - 7.0 / 11.0), 0.0, 1e-12);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-12);
    }

    [Fact]
    public void Solve_ComputesMultipleRightHandSidesWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 3.0);

        var matrix = builder.ToCsr();
        var rightHandSides = new[] { 1.0, 2.0, 5.0, 8.0 };
        var solutions = new double[4];
        using IDirectSparseSolver solver = CreateSpdSolver();

        var result = solver.Solve(matrix, rightHandSides, solutions, rightHandSideCount: 2);

        Assert.True(result.Converged);
        Assert.True(solver.IsAnalyzed);
        Assert.True(solver.IsFactorized);
        AssertEqual([1.0 / 11.0, 7.0 / 11.0, 7.0 / 11.0, 27.0 / 11.0], solutions, 1e-12);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-12);
    }

    [Fact]
    public void Solve_ThrowsWhenMultipleRightHandSideLengthIsInvalid()
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 4.0);
        builder.AddSymmetric(1, 1, 3.0);

        using IDirectSparseSolver solver = CreateSpdSolver();

        Assert.Throws<ArgumentException>(() => solver.Solve(builder.ToCsr(), [1.0, 2.0, 3.0], new double[4], rightHandSideCount: 2));
    }

    [Fact]
    public void Solve_ComputesLegacySparseSolveCaseWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var matrix = CreateLegacyDirectSparseSolveMatrix();
        var solution = new double[5];
        using var solver = CreateSpdSolver();

        var result = solver.Solve(matrix, [1.0, 2.0, 3.0, 4.0, 5.0], solution);

        Assert.True(result.Converged);
        AssertEqual([-326.3333333, 983.0, 163.4166667, 398.0, 61.5], solution, 1e-5);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-9);
    }

    [Fact]
    public void Solve_ComputesLegacyUnsymmetricDenseCaseWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var matrix = CreateLegacyUnsymmetricDenseMatrix();
        var rightHandSide = new[] { 4.02, 6.19, -8.22, -7.57, -3.03 };
        var solution = new double[5];
        using var solver = new PardisoSolver(
            new PardisoOptions(PardisoMatrixType.RealUnsymmetric));

        var result = solver.Solve(matrix, rightHandSide, solution);

        Assert.True(result.Converged);
        AssertEqual([-0.80071, -0.69524, 0.59391, 1.32173, 0.56576], solution, 1e-5);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-9);
    }

    [Fact]
    public void Factorize_UpdatesStateWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(1, 1, 3.0);

        using var solver = CreateSpdSolver();

        try
        {
            solver.Factorize(builder.ToCsr());

            Assert.True(solver.IsAnalyzed);
            Assert.True(solver.IsFactorized);
        }
        catch (MklBackendException exception)
        {
            Assert.Equal((int)MklNativeStatus.MklError, exception.StatusCode);
            Assert.Equal(nameof(MklNativeStatus.MklError), exception.StatusName);
            Assert.True(solver.IsAnalyzed);
            Assert.False(solver.IsFactorized);
        }
    }

    [Fact]
    public void Factorize_ReusesAnalyzeAndRefreshesValuesWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var firstMatrix = CreateDiagonalMatrix(2.0, 4.0);
        var secondMatrix = CreateDiagonalMatrix(5.0, 10.0);
        using var solver = new PardisoSolver(
            new PardisoOptions(PardisoMatrixType.RealSymmetricPositiveDefinite));

        solver.Analyze(firstMatrix);
        solver.Factorize(firstMatrix);

        var firstSolution = new double[2];
        var firstResult = solver.Solve(firstMatrix, [2.0, 8.0], firstSolution);

        solver.Factorize(secondMatrix);

        var secondSolution = new double[2];
        var secondResult = solver.Solve(secondMatrix, [15.0, 40.0], secondSolution);

        Assert.True(firstResult.Converged);
        Assert.True(secondResult.Converged);
        AssertEqual([1.0, 2.0], firstSolution, 1e-12);
        AssertEqual([3.0, 4.0], secondSolution, 1e-12);
        Assert.InRange(secondResult.FinalResidualNorm, 0.0, 1e-12);
    }

    [Fact]
    public void Factorize_ThrowsWhenAnalyzedStructureDiffers()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var analyzedMatrix = CreateDiagonalMatrix(2.0, 4.0);
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(0, 1, 1.0);
        builder.AddSymmetric(1, 1, 4.0);

        using var solver = CreateSpdSolver();
        solver.Analyze(analyzedMatrix);

        Assert.Throws<ArgumentException>(() => solver.Factorize(builder.ToCsr()));
    }

    [Fact]
    public void Solve_ComputesDirichletConstrainedSpdSystemWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(3, 3);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(0, 1, -1.0);
        builder.AddSymmetric(1, 1, 2.0);
        builder.AddSymmetric(1, 2, -1.0);
        builder.AddSymmetric(2, 2, 2.0);

        var rightHandSide = new[] { 0.0, 0.0, 0.0 };
        var constrainedMatrix = DirichletBoundaryCondition.Apply(builder.ToCsr(), rightHandSide, [0], [10.0]);
        var solution = new double[3];
        using var solver = CreateSpdSolver();

        var result = solver.Solve(constrainedMatrix, rightHandSide, solution);

        Assert.True(result.Converged);
        AssertEqual([10.0, 20.0 / 3.0, 10.0 / 3.0], solution, 1e-12);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-12);
    }

    [Fact]
    public void Solve_ComputesSmallTwoDimensionalTrussAssemblyWhenNativeMklIsAvailable()
    {
        if (!NativeLibraryTestResolver.TryRegister())
        {
            return;
        }

        var builder = new SparseMatrixBuilder(6, 6);
        AddTrussElement(builder, nodeA: 0, nodeB: 1, xA: 0.0, yA: 0.0, xB: 1.0, yB: 0.0, axialStiffness: 100.0);
        AddTrussElement(builder, nodeA: 0, nodeB: 2, xA: 0.0, yA: 0.0, xB: 0.0, yB: 1.0, axialStiffness: 100.0);
        AddTrussElement(builder, nodeA: 1, nodeB: 2, xA: 1.0, yA: 0.0, xB: 0.0, yB: 1.0, axialStiffness: 100.0);

        var rightHandSide = new[] { 0.0, 0.0, 0.0, 0.0, 12.0, -8.0 };
        var constrainedMatrix = DirichletBoundaryCondition.Apply(
            builder.ToCsr(),
            rightHandSide,
            [0, 1, 3],
            [0.0, 0.0, 0.0]);
        var solution = new double[6];
        using var solver = CreateSpdSolver();

        var result = solver.Solve(constrainedMatrix, rightHandSide, solution);

        Assert.True(result.Converged);
        AssertEqual([0.0, 0.0, 0.0], [solution[0], solution[1], solution[3]], 1e-12);
        Assert.InRange(result.FinalResidualNorm, 0.0, 1e-10);
    }

    private static CsrMatrix CreateLegacyDirectSparseSolveMatrix()
    {
        var builder = new SparseMatrixBuilder(5, 5);

        builder.AddSymmetric(0, 0, 9.0);
        builder.AddSymmetric(1, 1, 0.5);
        builder.AddSymmetric(2, 2, 12.0);
        builder.AddSymmetric(3, 3, 0.625);
        builder.AddSymmetric(4, 4, 16.0);

        builder.AddSymmetric(0, 1, 1.5);
        builder.AddSymmetric(0, 2, 6.0);
        builder.AddSymmetric(0, 3, 0.75);
        builder.AddSymmetric(0, 4, 3.0);

        return builder.ToCsr();
    }

    private static PardisoSolver CreateSpdSolver()
    {
        return new PardisoSolver(
            new PardisoOptions(PardisoMatrixType.RealSymmetricPositiveDefinite));
    }

    private static CsrMatrix CreateDiagonalMatrix(double first, double second)
    {
        var builder = new SparseMatrixBuilder(2, 2);
        builder.AddSymmetric(0, 0, first);
        builder.AddSymmetric(1, 1, second);
        return builder.ToCsr();
    }

    private static CsrMatrix CreateLegacyUnsymmetricDenseMatrix()
    {
        var builder = new SparseMatrixBuilder(5, 5);

        AddDenseRow(builder, 0, [6.80, -6.05, -0.45, 8.32, -9.67]);
        AddDenseRow(builder, 1, [-2.11, -3.30, 2.58, 2.71, -5.14]);
        AddDenseRow(builder, 2, [5.66, 5.36, -2.70, 4.35, -7.26]);
        AddDenseRow(builder, 3, [5.97, -4.44, 0.27, -7.17, 6.08]);
        AddDenseRow(builder, 4, [8.23, 1.08, 9.04, 2.14, -6.87]);

        return builder.ToCsr();
    }

    private static void AddDenseRow(SparseMatrixBuilder builder, int row, ReadOnlySpan<double> values)
    {
        for (var column = 0; column < values.Length; column++)
        {
            builder.Add(row, column, values[column]);
        }
    }

    private static void AddTrussElement(
        SparseMatrixBuilder builder,
        int nodeA,
        int nodeB,
        double xA,
        double yA,
        double xB,
        double yB,
        double axialStiffness)
    {
        var dx = xB - xA;
        var dy = yB - yA;
        var length = Math.Sqrt(dx * dx + dy * dy);
        var c = dx / length;
        var s = dy / length;
        var scale = axialStiffness / length;
        var cc = c * c;
        var cs = c * s;
        var ss = s * s;

        Span<int> indices =
        [
            2 * nodeA,
            2 * nodeA + 1,
            2 * nodeB,
            2 * nodeB + 1
        ];

        Span<double> values =
        [
            scale * cc, scale * cs, -scale * cc, -scale * cs,
            scale * cs, scale * ss, -scale * cs, -scale * ss,
            -scale * cc, -scale * cs, scale * cc, scale * cs,
            -scale * cs, -scale * ss, scale * cs, scale * ss
        ];

        builder.AddSymmetricSubmatrix(indices, values);
    }

    private static void AssertEqual(ReadOnlySpan<double> expected, ReadOnlySpan<double> actual, double tolerance)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.InRange(Math.Abs(actual[i] - expected[i]), 0.0, tolerance);
        }
    }
}
