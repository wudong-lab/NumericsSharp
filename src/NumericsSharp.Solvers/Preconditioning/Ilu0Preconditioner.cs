using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Solvers.Preconditioning;

public sealed class Ilu0Preconditioner : IPreconditioner
{
    private readonly int[][] _lowerColumns;
    private readonly double[][] _lowerValues;
    private readonly int[][] _upperColumns;
    private readonly double[][] _upperValues;

    public Ilu0Preconditioner(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("ILU0 preconditioner requires a square matrix.", nameof(matrix));
        }

        Order = matrix.RowCount;
        var factors = Factorize(matrix);
        _lowerColumns = factors.LowerColumns;
        _lowerValues = factors.LowerValues;
        _upperColumns = factors.UpperColumns;
        _upperValues = factors.UpperValues;
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

        var work = new double[Order];

        for (var row = 0; row < Order; row++)
        {
            var sum = residual[row];
            var columns = _lowerColumns[row];
            var values = _lowerValues[row];

            for (var i = 0; i < columns.Length; i++)
            {
                sum -= values[i] * work[columns[i]];
            }

            work[row] = sum;
        }

        for (var row = Order - 1; row >= 0; row--)
        {
            var sum = work[row];
            var diagonal = 0.0;
            var columns = _upperColumns[row];
            var values = _upperValues[row];

            for (var i = columns.Length - 1; i >= 0; i--)
            {
                var column = columns[i];
                if (column == row)
                {
                    diagonal = values[i];
                    break;
                }

                sum -= values[i] * result[column];
            }

            result[row] = sum / diagonal;
        }
    }

    private static Factors Factorize(CsrMatrix matrix)
    {
        var lowerColumns = new List<int>[matrix.RowCount];
        var lowerValues = new List<double>[matrix.RowCount];
        var upperColumns = new List<int>[matrix.RowCount];
        var upperValues = new List<double>[matrix.RowCount];
        var factorValues = new Dictionary<(int Row, int Column), double>();

        for (var row = 0; row < matrix.RowCount; row++)
        {
            lowerColumns[row] = [];
            lowerValues[row] = [];
            upperColumns[row] = [];
            upperValues[row] = [];

            var work = new Dictionary<int, double>();
            for (var index = matrix.RowOffsets[row]; index < matrix.RowOffsets[row + 1]; index++)
            {
                work[matrix.ColumnIndices[index]] = matrix.Values[index];
            }

            foreach (var column in work.Keys.Where(column => column < row).Order())
            {
                var pivot = factorValues[(column, column)];
                if (pivot == 0.0 || !double.IsFinite(pivot))
                {
                    throw new ArgumentException("ILU0 factorization requires nonzero finite pivots.", nameof(matrix));
                }

                work[column] /= pivot;

                foreach (var upperColumn in upperColumns[column])
                {
                    if (upperColumn > column && work.ContainsKey(upperColumn))
                    {
                        work[upperColumn] -= work[column] * factorValues[(column, upperColumn)];
                    }
                }
            }

            foreach (var column in work.Keys.Order())
            {
                var value = work[column];
                factorValues[(row, column)] = value;

                if (column < row)
                {
                    lowerColumns[row].Add(column);
                    lowerValues[row].Add(value);
                }
                else
                {
                    upperColumns[row].Add(column);
                    upperValues[row].Add(value);
                }
            }

            if (!factorValues.TryGetValue((row, row), out var diagonal) || diagonal == 0.0 || !double.IsFinite(diagonal))
            {
                throw new ArgumentException("ILU0 factorization requires nonzero finite diagonal entries.", nameof(matrix));
            }
        }

        return new Factors(
            lowerColumns.Select(columns => columns.ToArray()).ToArray(),
            lowerValues.Select(values => values.ToArray()).ToArray(),
            upperColumns.Select(columns => columns.ToArray()).ToArray(),
            upperValues.Select(values => values.ToArray()).ToArray());
    }

    private sealed record Factors(
        int[][] LowerColumns,
        double[][] LowerValues,
        int[][] UpperColumns,
        double[][] UpperValues);
}
