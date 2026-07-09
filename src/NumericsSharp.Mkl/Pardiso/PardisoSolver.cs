using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Solvers.LinearSolvers;

namespace NumericsSharp.Mkl.Pardiso;

public sealed class PardisoSolver : IDirectSparseSolver
{
    private PardisoCsrMatrix? _matrix;

    public PardisoSolver(PardisoOptions? options = null)
    {
        Options = options ?? new PardisoOptions();
    }

    public PardisoOptions Options { get; }

    public bool IsAnalyzed { get; private set; }

    public bool IsFactorized { get; private set; }

    public void Analyze(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ThrowIfNotSquare(matrix);

        _matrix = PardisoCsrMatrix.FromCsr(matrix);
        IsAnalyzed = true;
        IsFactorized = false;
    }

    public void Factorize(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ThrowIfNotSquare(matrix);

        if (!IsAnalyzed)
        {
            Analyze(matrix);
        }

        throw new PlatformNotSupportedException("MKL PARDISO native backend is not implemented yet.");
    }

    public SolverResult Solve(ILinearOperator matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
    {
        if (matrix is not CsrMatrix csrMatrix)
        {
            throw new ArgumentException("PARDISO solver requires a CSR matrix.", nameof(matrix));
        }

        return Solve(csrMatrix, rightHandSide, solution);
    }

    public SolverResult Solve(CsrMatrix matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ThrowIfNotSquare(matrix);

        if (rightHandSide.Length != matrix.RowCount)
        {
            throw new ArgumentException("Right hand side length must equal matrix row count.", nameof(rightHandSide));
        }

        if (solution.Length != matrix.ColumnCount)
        {
            throw new ArgumentException("Solution length must equal matrix column count.", nameof(solution));
        }

        throw new PlatformNotSupportedException("MKL PARDISO native backend is not implemented yet.");
    }

    public void Dispose()
    {
        _matrix = null;
        IsAnalyzed = false;
        IsFactorized = false;
    }

    private static void ThrowIfNotSquare(CsrMatrix matrix)
    {
        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("PARDISO solver requires a square matrix.", nameof(matrix));
        }
    }
}
