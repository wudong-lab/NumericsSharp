using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Solvers.Preconditioning;

public sealed class JacobiPreconditioner : IPreconditioner
{
    private readonly double[] _inverseDiagonal;

    public JacobiPreconditioner(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("Jacobi preconditioner requires a square matrix.", nameof(matrix));
        }

        Order = matrix.RowCount;
        var diagonal = new double[Order];
        matrix.CopyDiagonalTo(diagonal);

        _inverseDiagonal = new double[Order];
        for (var i = 0; i < diagonal.Length; i++)
        {
            if (diagonal[i] <= 0.0 || !double.IsFinite(diagonal[i]))
            {
                throw new ArgumentException("Jacobi preconditioner requires positive finite diagonal entries.", nameof(matrix));
            }

            _inverseDiagonal[i] = 1.0 / diagonal[i];
        }
    }

    public int Order { get; }

    public void Apply(ReadOnlySpan<double> residual, Span<double> result)
    {
        if (residual.Length != Order)
        {
            throw new ArgumentException("Residual length must equal preconditioner order.", nameof(residual));
        }

        if (result.Length != Order)
        {
            throw new ArgumentException("Result length must equal preconditioner order.", nameof(result));
        }

        for (var i = 0; i < Order; i++)
        {
            result[i] = _inverseDiagonal[i] * residual[i];
        }
    }
}
