using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Mkl.Native;
using NumericsSharp.Solvers.LinearSolvers;

namespace NumericsSharp.Mkl.Pardiso;

public sealed unsafe class PardisoSolver : IDirectSparseSolver
{
    private PardisoCsrMatrix? _matrix;
    private PardisoNativeHandle? _handle;

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

        _matrix = PardisoCsrMatrix.FromCsr(matrix, Options.MatrixType);

        _handle ??= CreateNativeHandle();

        fixed (int* rowPointers = _matrix.RowPointers)
        fixed (int* columns = _matrix.Columns)
        {
            MklNativeException.ThrowIfFailed(
                PardisoNativeMethods.Analyze(
                    _handle,
                    _matrix.Order,
                    _matrix.NonZeroCount,
                    rowPointers,
                    columns,
                    Options.MatrixType));
        }

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

        if (_matrix is null || _handle is null)
        {
            throw new InvalidOperationException("PARDISO matrix must be analyzed before factorization.");
        }

        fixed (double* values = _matrix.Values)
        {
            MklNativeException.ThrowIfFailed(PardisoNativeMethods.Factorize(_handle, values));
        }

        IsFactorized = true;
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

        var initialResidualNorm = LinearSystemResidual.ComputeL2Norm(matrix, solution, rightHandSide);

        if (!IsFactorized)
        {
            Factorize(matrix);
        }

        if (_handle is null)
        {
            throw new InvalidOperationException("PARDISO matrix must be factorized before solve.");
        }

        fixed (double* rightHandSidePointer = rightHandSide)
        fixed (double* solutionPointer = solution)
        {
            MklNativeException.ThrowIfFailed(
                PardisoNativeMethods.Solve(
                    _handle,
                    rightHandSidePointer,
                    solutionPointer,
                    rightHandSideCount: 1));
        }

        var finalResidualNorm = LinearSystemResidual.ComputeL2Norm(matrix, solution, rightHandSide);
        return new SolverResult(SolverStatus.Converged, 0, initialResidualNorm, finalResidualNorm);
    }

    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
        _matrix = null;
        IsAnalyzed = false;
        IsFactorized = false;
    }

    private static PardisoNativeHandle CreateNativeHandle()
    {
        MklNativeException.ThrowIfFailed(PardisoNativeMethods.Create(out var handle));
        return new PardisoNativeHandle(handle, ownsHandle: true);
    }

    private static void ThrowIfNotSquare(CsrMatrix matrix)
    {
        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("PARDISO solver requires a square matrix.", nameof(matrix));
        }
    }
}
