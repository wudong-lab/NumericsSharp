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

        this.Order = matrix.RowCount;
        var diagonal = new double[this.Order];
        matrix.CopyDiagonalTo(diagonal);

        this._inverseDiagonal = new double[this.Order];
        for (var i = 0; i < diagonal.Length; i++)
        {
            if (diagonal[i] <= 0.0 || !double.IsFinite(diagonal[i]))
            {
                throw new ArgumentException("Jacobi preconditioner requires positive finite diagonal entries.", nameof(matrix));
            }

            this._inverseDiagonal[i] = 1.0 / diagonal[i];
        }
    }

    public int Order { get; }

    public void Apply(ReadOnlySpan<double> residual, Span<double> result)
    {
        if (residual.Length != this.Order)
        {
            throw new ArgumentException("Residual length must equal preconditioner order.", nameof(residual));
        }

        if (result.Length != this.Order)
        {
            throw new ArgumentException("Result length must equal preconditioner order.", nameof(result));
        }

        for (var i = 0; i < this.Order; i++)
        {
            result[i] = this._inverseDiagonal[i] * residual[i];
        }
    }
}
