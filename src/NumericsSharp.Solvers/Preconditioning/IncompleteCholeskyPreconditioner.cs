using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Solvers.Preconditioning;

public sealed class IncompleteCholeskyPreconditioner : IPreconditioner
{
    private readonly int[][] _lowerColumns;
    private readonly double[][] _lowerValues;

    public IncompleteCholeskyPreconditioner(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("Incomplete Cholesky preconditioner requires a square matrix.", nameof(matrix));
        }

        this.Order = matrix.RowCount;
        var lower = Factorize(matrix);
        this._lowerColumns = lower.Columns;
        this._lowerValues = lower.Values;
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

        var work = new double[this.Order];

        for (var row = 0; row < this.Order; row++)
        {
            var sum = residual[row];
            var diagonal = 0.0;
            var columns = this._lowerColumns[row];
            var values = this._lowerValues[row];

            for (var i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                if (column == row)
                {
                    diagonal = values[i];
                    break;
                }

                sum -= values[i] * work[column];
            }

            work[row] = sum / diagonal;
        }

        for (var row = this.Order - 1; row >= 0; row--)
        {
            var sum = work[row];

            for (var lowerRow = row + 1; lowerRow < this.Order; lowerRow++)
            {
                var columns = this._lowerColumns[lowerRow];
                var values = this._lowerValues[lowerRow];

                for (var i = 0; i < columns.Length; i++)
                {
                    var column = columns[i];
                    if (column == row)
                    {
                        sum -= values[i] * result[lowerRow];
                        break;
                    }

                    if (column > row)
                    {
                        break;
                    }
                }
            }

            result[row] = sum / this.GetDiagonal(row);
        }
    }

    private double GetDiagonal(int row)
    {
        var columns = this._lowerColumns[row];
        var values = this._lowerValues[row];

        for (var i = columns.Length - 1; i >= 0; i--)
        {
            if (columns[i] == row)
            {
                return values[i];
            }
        }

        throw new InvalidOperationException("Incomplete Cholesky factor is missing a diagonal entry.");
    }

    private static FactorRows Factorize(CsrMatrix matrix)
    {
        var valuesByEntry = new Dictionary<(int Row, int Column), double>();
        var rowColumns = new List<int>[matrix.RowCount];
        var rowValues = new List<double>[matrix.RowCount];

        for (var row = 0; row < matrix.RowCount; row++)
        {
            rowColumns[row] = [];
            rowValues[row] = [];

            for (var index = matrix.RowOffsets[row]; index < matrix.RowOffsets[row + 1]; index++)
            {
                var column = matrix.ColumnIndices[index];
                if (column <= row)
                {
                    rowColumns[row].Add(column);
                }
            }

            rowColumns[row].Sort();
        }

        for (var row = 0; row < matrix.RowCount; row++)
        {
            foreach (var column in rowColumns[row])
            {
                var value = GetMatrixValue(matrix, row, column);

                if (column < row)
                {
                    foreach (var previousColumn in rowColumns[row])
                    {
                        if (previousColumn >= column)
                        {
                            break;
                        }

                        if (valuesByEntry.TryGetValue((row, previousColumn), out var left)
                            && valuesByEntry.TryGetValue((column, previousColumn), out var right))
                        {
                            value -= left * right;
                        }
                    }

                    value /= valuesByEntry[(column, column)];
                }
                else
                {
                    foreach (var previousColumn in rowColumns[row])
                    {
                        if (previousColumn >= row)
                        {
                            break;
                        }

                        var lowerValue = valuesByEntry[(row, previousColumn)];
                        value -= lowerValue * lowerValue;
                    }

                    if (value <= 0.0 || !double.IsFinite(value))
                    {
                        throw new ArgumentException("Incomplete Cholesky factorization requires positive finite pivots.", nameof(matrix));
                    }

                    value = Math.Sqrt(value);
                }

                valuesByEntry[(row, column)] = value;
                rowValues[row].Add(value);
            }
        }

        return new FactorRows(
            rowColumns.Select(columns => columns.ToArray()).ToArray(),
            rowValues.Select(values => values.ToArray()).ToArray());
    }

    private static double GetMatrixValue(CsrMatrix matrix, int row, int column)
    {
        for (var index = matrix.RowOffsets[row]; index < matrix.RowOffsets[row + 1]; index++)
        {
            if (matrix.ColumnIndices[index] == column)
            {
                return matrix.Values[index];
            }
        }

        return 0.0;
    }

    private sealed record FactorRows(int[][] Columns, double[][] Values);
}
