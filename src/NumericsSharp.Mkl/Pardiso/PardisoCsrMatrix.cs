using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Mkl.Pardiso;

internal sealed class PardisoCsrMatrix
{
    private PardisoCsrMatrix(
        int order,
        int[] rowPointers,
        int[] columns,
        double[] values,
        bool upperTriangleOnly)
    {
        this.Order = order;
        this.RowPointers = rowPointers;
        this.Columns = columns;
        this.Values = values;
        this.UpperTriangleOnly = upperTriangleOnly;
    }

    public int Order { get; }
    public int NonZeroCount => this.Values.Length;
    public int[] RowPointers { get; }
    public int[] Columns { get; }
    public double[] Values { get; private set; }

    private bool UpperTriangleOnly { get; }

    public static PardisoCsrMatrix FromCsr(CsrMatrix matrix, PardisoMatrixType matrixType = PardisoMatrixType.RealUnsymmetric)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
            throw new ArgumentException("PARDISO CSR adapter requires a square matrix.", nameof(matrix));

        return RequiresUpperTriangleOnly(matrixType)
            ? FromUpperTriangleCsr(matrix)
            : FromFullCsr(matrix);
    }

    private static PardisoCsrMatrix FromFullCsr(CsrMatrix matrix)
    {
        return new PardisoCsrMatrix(
            matrix.RowCount,
            matrix.RowOffsets,
            matrix.ColumnIndices,
            matrix.Values,
            upperTriangleOnly: false);
    }

    private static PardisoCsrMatrix FromUpperTriangleCsr(CsrMatrix matrix)
    {
        var upperTriangleNonZeroCount = 0;
        for (var row = 0; row < matrix.RowCount; row++)
        {
            var start = matrix.RowOffsets[row];
            var end = matrix.RowOffsets[row + 1];
            for (var index = start; index < end; index++)
            {
                if (matrix.ColumnIndices[index] >= row)
                    upperTriangleNonZeroCount++;
            }
        }

        var rowPointers = new int[matrix.RowCount + 1];
        var columns = new int[upperTriangleNonZeroCount];
        var values = new double[upperTriangleNonZeroCount];
        var destinationIndex = 0;

        rowPointers[0] = 0;

        for (var row = 0; row < matrix.RowCount; row++)
        {
            var start = matrix.RowOffsets[row];
            var end = matrix.RowOffsets[row + 1];

            for (var index = start; index < end; index++)
            {
                var column = matrix.ColumnIndices[index];
                if (column < row) continue;

                columns[destinationIndex] = column;
                values[destinationIndex] = matrix.Values[index];
                destinationIndex++;
            }

            rowPointers[row + 1] = destinationIndex;
        }

        return new PardisoCsrMatrix(
            matrix.RowCount,
            rowPointers,
            columns,
            values,
            upperTriangleOnly: true);
    }

    public bool TryUpdateValues(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != this.Order)
            return false;

        if (!this.UpperTriangleOnly)
        {
            if (!this.RowPointers.AsSpan().SequenceEqual(matrix.RowOffsets)
                || !this.Columns.AsSpan().SequenceEqual(matrix.ColumnIndices))
            {
                return false;
            }

            this.Values = matrix.Values;
            return true;
        }

        var destinationIndex = 0;
        for (var row = 0; row < matrix.RowCount; row++)
        {
            var start = matrix.RowOffsets[row];
            var end = matrix.RowOffsets[row + 1];
            for (var index = start; index < end; index++)
            {
                var column = matrix.ColumnIndices[index];
                if (column < row) continue;

                if (destinationIndex >= this.Columns.Length
                    || this.Columns[destinationIndex] != column)
                {
                    return false;
                }

                destinationIndex++;
            }
        }

        if (destinationIndex != this.Columns.Length)
            return false;

        destinationIndex = 0;
        for (var row = 0; row < matrix.RowCount; row++)
        {
            var start = matrix.RowOffsets[row];
            var end = matrix.RowOffsets[row + 1];
            for (var index = start; index < end; index++)
            {
                if (matrix.ColumnIndices[index] < row) continue;
                this.Values[destinationIndex++] = matrix.Values[index];
            }
        }

        return true;
    }

    private static bool RequiresUpperTriangleOnly(PardisoMatrixType matrixType)
        => matrixType is PardisoMatrixType.RealStructurallySymmetric
            or PardisoMatrixType.RealSymmetricPositiveDefinite
            or PardisoMatrixType.RealSymmetricIndefinite;
}
