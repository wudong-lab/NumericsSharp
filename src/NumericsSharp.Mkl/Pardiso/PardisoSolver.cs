using NumericsSharp.Core.LinearAlgebra;
using NumericsSharp.Core.Threading;
using NumericsSharp.Mkl.Native;
using NumericsSharp.Solvers.LinearSolvers;

namespace NumericsSharp.Mkl.Pardiso;

public sealed unsafe class PardisoSolver : IDirectSparseSolver
{
    private PardisoCsrMatrix? _matrix;
    private PardisoNativeHandle? _handle;

    public PardisoSolver(PardisoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.Options = options;
        ThrowIfInvalidThreadingOptions(this.Options.Threading);
    }

    public PardisoOptions Options { get; }
    public bool IsAnalyzed { get; private set; }
    public bool IsFactorized { get; private set; }

    public void Analyze(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ThrowIfNotSquare(matrix);

        this._matrix = PardisoCsrMatrix.FromCsr(matrix, this.Options.MatrixType);

        this._handle ??= CreateNativeHandle();
        this.ApplyThreadingOptions();

        fixed (int* rowPointers = this._matrix.RowPointers)
        fixed (int* columns = this._matrix.Columns)
        {
            var status = PardisoNativeMethods.Analyze(this._handle, this._matrix.Order, this._matrix.NonZeroCount,
                rowPointers, columns, this.Options.MatrixType);

            MklBackendException.ThrowIfFailed(
                status,
                operation: "PARDISO analyze",
                phase: 11,
                matrixType: this.Options.MatrixType.ToString(),
                order: this._matrix.Order,
                nonZeroCount: this._matrix.NonZeroCount,
                pardisoErrorCode: null);
        }

        this.IsAnalyzed = true;
        this.IsFactorized = false;
    }

    public void Factorize(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ThrowIfNotSquare(matrix);

        if (!this.IsAnalyzed)
        {
            this.Analyze(matrix);
        }

        if (this._matrix is null || this._handle is null)
            throw new InvalidOperationException("PARDISO matrix must be analyzed before factorization.");

        var factorizationMatrix = PardisoCsrMatrix.FromCsr(matrix, this.Options.MatrixType);
        ThrowIfDifferentStructure(this._matrix, factorizationMatrix);
        this._matrix = factorizationMatrix;
        this.ApplyThreadingOptions();

        fixed (double* values = this._matrix.Values)
        {
            var status = PardisoNativeMethods.Factorize(this._handle, values);
            this.ThrowIfPardisoFailed(status, operation: "PARDISO factorize", expectedPhase: 12);
        }

        this.IsFactorized = true;
    }

    public SolverResult Solve(ILinearOperator matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
    {
        if (matrix is not CsrMatrix csrMatrix)
            throw new ArgumentException("PARDISO solver requires a CSR matrix.", nameof(matrix));

        return this.Solve(csrMatrix, rightHandSide, solution);
    }

    public SolverResult Solve(CsrMatrix matrix, ReadOnlySpan<double> rightHandSide, Span<double> solution)
    {
        return this.Solve(matrix, rightHandSide, solution, rightHandSideCount: 1);
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
            throw new ArgumentException("Right hand side length must equal matrix row count multiplied by right hand side count.", nameof(rightHandSide));
        }

        var expectedSolutionLength = checked(matrix.ColumnCount * rightHandSideCount);
        if (solution.Length != expectedSolutionLength)
        {
            throw new ArgumentException("Solution length must equal matrix column count multiplied by right hand side count.", nameof(solution));
        }

        var initialResidualNorm = ComputeMaxResidualNorm(matrix, solution, rightHandSide, rightHandSideCount);

        if (!this.IsFactorized)
        {
            this.Factorize(matrix);
        }

        if (this._handle is null)
        {
            throw new InvalidOperationException("PARDISO matrix must be factorized before solve.");
        }

        this.ApplyThreadingOptions();

        fixed (double* rightHandSidePointer = rightHandSide)
        fixed (double* solutionPointer = solution)
        {
            var status = PardisoNativeMethods.Solve(this._handle, rightHandSidePointer, solutionPointer, rightHandSideCount);
            this.ThrowIfPardisoFailed(status, operation: "PARDISO solve", expectedPhase: 33);
        }

        var finalResidualNorm = ComputeMaxResidualNorm(matrix, solution, rightHandSide, rightHandSideCount);
        return new SolverResult(SolverStatus.Converged, 0, initialResidualNorm, finalResidualNorm);
    }

    public void Dispose()
    {
        this._handle?.Dispose();
        this._handle = null;
        this._matrix = null;
        this.IsAnalyzed = false;
        this.IsFactorized = false;
    }

    private static PardisoNativeHandle CreateNativeHandle()
    {
        MklBackendException.ThrowIfFailed(PardisoNativeMethods.Create(out var handle));
        return new PardisoNativeHandle(handle, ownsHandle: true);
    }

    private void ApplyThreadingOptions()
    {
        var nativeThreadCount = this.Options.Threading.Mode == ParallelMode.ManagedOuterParallel
            ? 1
            : this.Options.Threading.NativeThreadCount;

        var status = PardisoNativeMethods.SetThreadCount(nativeThreadCount);
        MklBackendException.ThrowIfFailed(status, operation: "MKL set thread count",
            phase: null, matrixType: null, order: null, nonZeroCount: null, pardisoErrorCode: null);
    }

    private void ThrowIfPardisoFailed(MklNativeStatus status, string operation, int expectedPhase)
    {
        if (status == MklNativeStatus.Success) return;

        var phase = expectedPhase;
        int? pardisoErrorCode = null;

        if (this._handle is not null)
        {
            var lastErrorStatus = GetLastPardisoError(this._handle, out var lastPhase, out var lastError);
            if (lastErrorStatus == MklNativeStatus.Success)
            {
                phase = lastPhase == 0 ? expectedPhase : lastPhase;
                pardisoErrorCode = lastError;
            }
        }

        MklBackendException.ThrowIfFailed(status, operation, phase, this.Options.MatrixType.ToString(),
            this._matrix?.Order, this._matrix?.NonZeroCount, pardisoErrorCode);
    }

    private static MklNativeStatus GetLastPardisoError(PardisoNativeHandle handle, out int phase, out int error)
    {
        var phaseBuffer = stackalloc int[1];
        var errorBuffer = stackalloc int[1];
        var status = PardisoNativeMethods.GetLastError(handle, phaseBuffer, errorBuffer);

        phase = phaseBuffer[0];
        error = errorBuffer[0];
        return status;
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

    private static double ComputeMaxResidualNorm(CsrMatrix matrix, ReadOnlySpan<double> solution, ReadOnlySpan<double> rightHandSide, int rightHandSideCount)
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