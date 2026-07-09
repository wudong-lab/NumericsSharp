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

    public static PardisoCsrMatrix FromCsr(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (matrix.RowCount != matrix.ColumnCount)
        {
            throw new ArgumentException("PARDISO CSR adapter requires a square matrix.", nameof(matrix));
        }

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
}
