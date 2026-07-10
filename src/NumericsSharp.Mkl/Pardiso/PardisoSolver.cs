using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Core.Threading;
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
        ThrowIfInvalidThreadingOptions(Options.Threading);
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
        ApplyThreadingOptions();

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

        var factorizationMatrix = PardisoCsrMatrix.FromCsr(matrix, Options.MatrixType);
        ThrowIfDifferentStructure(_matrix, factorizationMatrix);
        _matrix = factorizationMatrix;
        ApplyThreadingOptions();

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
        return Solve(matrix, rightHandSide, solution, rightHandSideCount: 1);
    }

    public SolverResult Solve(
        CsrMatrix matrix,
        ReadOnlySpan<double> rightHandSide,
        Span<double> solution,
        int rightHandSideCount)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ThrowIfNotSquare(matrix);
        ArgumentOutOfRangeException.ThrowIfLessThan(rightHandSideCount, 1);

        var expectedRightHandSideLength = checked(matrix.RowCount * rightHandSideCount);
        if (rightHandSide.Length != expectedRightHandSideLength)
        {
            throw new ArgumentException(
                "Right hand side length must equal matrix row count multiplied by right hand side count.",
                nameof(rightHandSide));
        }

        var expectedSolutionLength = checked(matrix.ColumnCount * rightHandSideCount);
        if (solution.Length != expectedSolutionLength)
        {
            throw new ArgumentException(
                "Solution length must equal matrix column count multiplied by right hand side count.",
                nameof(solution));
        }

        var initialResidualNorm = ComputeMaxResidualNorm(matrix, solution, rightHandSide, rightHandSideCount);

        if (!IsFactorized)
        {
            Factorize(matrix);
        }

        if (_handle is null)
        {
            throw new InvalidOperationException("PARDISO matrix must be factorized before solve.");
        }

        ApplyThreadingOptions();

        fixed (double* rightHandSidePointer = rightHandSide)
        fixed (double* solutionPointer = solution)
        {
            MklNativeException.ThrowIfFailed(
                PardisoNativeMethods.Solve(
                    _handle,
                    rightHandSidePointer,
                    solutionPointer,
                    rightHandSideCount));
        }

        var finalResidualNorm = ComputeMaxResidualNorm(matrix, solution, rightHandSide, rightHandSideCount);
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

    private void ApplyThreadingOptions()
    {
        var nativeThreadCount = Options.Threading.Mode == ParallelMode.ManagedOuterParallel
            ? 1
            : Options.Threading.NativeThreadCount;

        MklNativeException.ThrowIfFailed(PardisoNativeMethods.SetThreadCount(nativeThreadCount));
    }

    private static void ThrowIfInvalidThreadingOptions(NumericsThreadingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDegreeOfParallelism, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.NativeThreadCount, 1);
    }

    private static void ThrowIfNotSquare(CsrMatrix matrix)
    {
        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("PARDISO solver requires a square matrix.", nameof(matrix));
        }
    }

    private static void ThrowIfDifferentStructure(PardisoCsrMatrix analyzedMatrix, PardisoCsrMatrix factorizationMatrix)
    {
        if (analyzedMatrix.Order != factorizationMatrix.Order
            || analyzedMatrix.NonZeroCount != factorizationMatrix.NonZeroCount
            || !analyzedMatrix.RowPointers.AsSpan().SequenceEqual(factorizationMatrix.RowPointers)
            || !analyzedMatrix.Columns.AsSpan().SequenceEqual(factorizationMatrix.Columns))
        {
            throw new ArgumentException(
                "PARDISO factorization matrix structure must match the analyzed matrix structure.",
                nameof(factorizationMatrix));
        }
    }

    private static double ComputeMaxResidualNorm(
        CsrMatrix matrix,
        ReadOnlySpan<double> solution,
        ReadOnlySpan<double> rightHandSide,
        int rightHandSideCount)
    {
        var maxResidualNorm = 0.0;
        var order = matrix.RowCount;

        for (var rhsIndex = 0; rhsIndex < rightHandSideCount; rhsIndex++)
        {
            var offset = rhsIndex * order;
            var residualNorm = LinearSystemResidual.ComputeL2Norm(
                matrix,
                solution.Slice(offset, order),
                rightHandSide.Slice(offset, order));

            maxResidualNorm = Math.Max(maxResidualNorm, residualNorm);
        }

        return maxResidualNorm;
    }
}
