using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Mkl.Pardiso;

internal sealed class PardisoCsrMatrix
{
    private const int OneBasedIndexOffset = 1;

    private PardisoCsrMatrix(int order, int[] rowPointers, int[] columns, double[] values)
    {
        Order = order;
        RowPointers = rowPointers;
        Columns = columns;
        Values = values;
    }

    public int Order { get; }

    public int NonZeroCount => Values.Length;

    public int[] RowPointers { get; }

    public int[] Columns { get; }

    public double[] Values { get; }

    public static PardisoCsrMatrix FromCsr(CsrMatrix matrix, PardisoMatrixType matrixType = PardisoMatrixType.RealUnsymmetric)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("PARDISO CSR adapter requires a square matrix.", nameof(matrix));
        }

        return RequiresUpperTriangleOnly(matrixType)
            ? FromUpperTriangleCsr(matrix)
            : FromFullCsr(matrix);
    }

    private static PardisoCsrMatrix FromFullCsr(CsrMatrix matrix)
    {
        var rowPointers = new int[matrix.RowOffsets.Length];
        for (var i = 0; i < rowPointers.Length; i++)
        {
            rowPointers[i] = matrix.RowOffsets[i] + OneBasedIndexOffset;
        }

        var columns = new int[matrix.ColumnIndices.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            columns[i] = matrix.ColumnIndices[i] + OneBasedIndexOffset;
        }

        return new PardisoCsrMatrix(
            matrix.RowCount,
            rowPointers,
            columns,
            (double[])matrix.Values.Clone());
    }

    private static PardisoCsrMatrix FromUpperTriangleCsr(CsrMatrix matrix)
    {
        var rowPointers = new int[matrix.RowCount + 1];
        var columns = new List<int>(matrix.NonZeroCount);
        var values = new List<double>(matrix.NonZeroCount);

        rowPointers[0] = OneBasedIndexOffset;

        for (var row = 0; row < matrix.RowCount; row++)
        {
            var start = matrix.RowOffsets[row];
            var end = matrix.RowOffsets[row + 1];

            for (var index = start; index < end; index++)
            {
                var column = matrix.ColumnIndices[index];
                if (column < row)
                {
                    continue;
                }

                columns.Add(column + OneBasedIndexOffset);
                values.Add(matrix.Values[index]);
            }

            rowPointers[row + 1] = columns.Count + OneBasedIndexOffset;
        }

        return new PardisoCsrMatrix(
            matrix.RowCount,
            rowPointers,
            columns.ToArray(),
            values.ToArray());
    }

    private static bool RequiresUpperTriangleOnly(PardisoMatrixType matrixType)
        => matrixType is PardisoMatrixType.RealStructurallySymmetric
            or PardisoMatrixType.RealSymmetricPositiveDefinite
            or PardisoMatrixType.RealSymmetricIndefinite;
}
