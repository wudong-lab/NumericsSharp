namespace NumericsSharp.Core.LinearAlgebra;

public static class DirichletBoundaryCondition
{
    public static CsrMatrix Apply(CsrMatrix matrix, Span<double> rightHandSide, ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
            throw new ArgumentException("Dirichlet boundary conditions require a square matrix.", nameof(matrix));

        if (rightHandSide.Length != matrix.RowCount)
            throw new ArgumentException("Right hand side length must equal matrix order.", nameof(rightHandSide));

        if (indices.Length != values.Length)
            throw new ArgumentException("Constrained index count must equal constrained value count.", nameof(values));

        var order = matrix.RowCount;
        var constrainedValues = new double[order];
        var isConstrained = new bool[order];

        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            if ((uint)index >= (uint)order)
                throw new ArgumentOutOfRangeException(nameof(indices), "Constrained index is out of range.");

            if (isConstrained[index])
                throw new ArgumentException("Duplicate constrained index is not supported.", nameof(indices));

            isConstrained[index] = true;
            constrainedValues[index] = values[i];
        }

        var builder = new SparseMatrixBuilder(order, order, matrix.NonZeroCount + indices.Length);

        for (var row = 0; row < order; row++)
        {
            var start = matrix.RowOffsets[row];
            var end = matrix.RowOffsets[row + 1];

            for (var entryIndex = start; entryIndex < end; entryIndex++)
            {
                var column = matrix.ColumnIndices[entryIndex];
                var value = matrix.Values[entryIndex];

                if (isConstrained[column])
                {
                    rightHandSide[row] -= value * constrainedValues[column];
                }

                if (!isConstrained[row] && !isConstrained[column])
                {
                    builder.Add(row, column, value);
                }
            }
        }

        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            builder.Add(index, index, 1.0);
            rightHandSide[index] = values[i];
        }

        return builder.ToCsr();
    }
}