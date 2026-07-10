using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Mkl.Pardiso;
using NumericsSharp.Solvers.ConjugateGradient;
using NumericsSharp.Solvers.Preconditioning;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class SparseSolverBenchmarks
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
        this._matrix = CreatePoissonMatrix(this.GridSize);
        this._rightHandSide = new double[this._matrix.RowCount];
        Array.Fill(this._rightHandSide, 1.0);

        this._cgOptions = new ConjugateGradientOptions
        {
            MaxIterations = this.GridSize * this.GridSize,
            RelativeTolerance = 1e-10
        };
        this._jacobi = new JacobiPreconditioner(this._matrix);

        this._pardisoFullCsr = new PardisoSolver(
            new PardisoOptions
            {
                MatrixType = PardisoMatrixType.RealUnsymmetric
            });
        this._pardisoFullCsr.Factorize(this._matrix);

        this._pardisoSpdUpperCsr = new PardisoSolver(
            new PardisoOptions
            {
                MatrixType = PardisoMatrixType.RealSymmetricPositiveDefinite
            });
        this._pardisoSpdUpperCsr.Factorize(this._matrix);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        this._pardisoFullCsr.Dispose();
        this._pardisoSpdUpperCsr.Dispose();
    }

    [Benchmark(Baseline = true)]
    public double ConjugateGradient()
    {
        var solution = new double[this._matrix.RowCount];
        var result = new ConjugateGradientSolver().Solve(this._matrix, this._rightHandSide, solution, this._cgOptions);
        return result.FinalResidualNorm;
    }

    [Benchmark]
    public double PreconditionedConjugateGradient()
    {
        var solution = new double[this._matrix.RowCount];
        var result = new PreconditionedConjugateGradientSolver().Solve(this._matrix, this._jacobi, this._rightHandSide,
            solution, this._cgOptions);
        return result.FinalResidualNorm;
    }

    [Benchmark]
    public double PardisoFullCsrSolve()
    {
        var solution = new double[this._matrix.RowCount];
        var result = this._pardisoFullCsr.Solve(this._matrix, this._rightHandSide, solution);
        return result.FinalResidualNorm;
    }

    [Benchmark]
    public double PardisoSpdUpperCsrSolve()
    {
        var solution = new double[this._matrix.RowCount];
        var result = this._pardisoSpdUpperCsr.Solve(this._matrix, this._rightHandSide, solution);
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
