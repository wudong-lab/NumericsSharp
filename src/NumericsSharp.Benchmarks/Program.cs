using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Mkl.Pardiso;
using NumericsSharp.Solvers.ConjugateGradient;
using NumericsSharp.Solvers.Preconditioning;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public sealed class SparseSolverBenchmarks
{
    private CsrMatrix _matrix = null!;
    private double[] _rightHandSide = null!;
    private ConjugateGradientOptions _cgOptions = null!;
    private JacobiPreconditioner _jacobi = null!;
    private PardisoSolver _pardisoFullCsr = null!;
    private PardisoSolver _pardisoSpdUpperCsr = null!;

    [Params(32, 64)]
    public int GridSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _matrix = CreatePoissonMatrix(GridSize);
        _rightHandSide = new double[_matrix.RowCount];
        Array.Fill(_rightHandSide, 1.0);

        _cgOptions = new ConjugateGradientOptions
        {
            MaxIterations = GridSize * GridSize,
            RelativeTolerance = 1e-10
        };
        _jacobi = new JacobiPreconditioner(_matrix);

        _pardisoFullCsr = new PardisoSolver(
            new PardisoOptions
            {
                MatrixType = PardisoMatrixType.RealUnsymmetric
            });
        _pardisoFullCsr.Factorize(_matrix);

        _pardisoSpdUpperCsr = new PardisoSolver(
            new PardisoOptions
            {
                MatrixType = PardisoMatrixType.RealSymmetricPositiveDefinite
            });
        _pardisoSpdUpperCsr.Factorize(_matrix);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _pardisoFullCsr.Dispose();
        _pardisoSpdUpperCsr.Dispose();
    }

    [Benchmark(Baseline = true)]
    public double ConjugateGradient()
    {
        var solution = new double[_matrix.RowCount];
        var result = new ConjugateGradientSolver().Solve(_matrix, _rightHandSide, solution, _cgOptions);
        return result.FinalResidualNorm;
    }

    [Benchmark]
    public double PreconditionedConjugateGradient()
    {
        var solution = new double[_matrix.RowCount];
        var result = new PreconditionedConjugateGradientSolver().Solve(
            _matrix,
            _jacobi,
            _rightHandSide,
            solution,
            _cgOptions);
        return result.FinalResidualNorm;
    }

    [Benchmark]
    public double PardisoFullCsrSolve()
    {
        var solution = new double[_matrix.RowCount];
        var result = _pardisoFullCsr.Solve(_matrix, _rightHandSide, solution);
        return result.FinalResidualNorm;
    }

    [Benchmark]
    public double PardisoSpdUpperCsrSolve()
    {
        var solution = new double[_matrix.RowCount];
        var result = _pardisoSpdUpperCsr.Solve(_matrix, _rightHandSide, solution);
        return result.FinalResidualNorm;
    }

    private static CsrMatrix CreatePoissonMatrix(int gridSize)
    {
        var order = checked(gridSize * gridSize);
        var builder = new SparseMatrixBuilder(order, order, order * 5);

        for (var y = 0; y < gridSize; y++)
        {
            for (var x = 0; x < gridSize; x++)
            {
                var row = y * gridSize + x;
                builder.AddSymmetric(row, row, 4.0);

                if (x + 1 < gridSize)
                {
                    builder.AddSymmetric(row, row + 1, -1.0);
                }

                if (y + 1 < gridSize)
                {
                    builder.AddSymmetric(row, row + gridSize, -1.0);
                }
            }
        }

        return builder.ToCsr();
    }
}
