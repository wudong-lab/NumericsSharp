namespace NumericsSharp.Core.LinearAlgebra;

public sealed class SparseMatrixAssembler
{
    private readonly double[] _values;

    public SparseMatrixAssembler(CsrMatrixPattern pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _values = pattern.CreateValueBuffer();
    }

    public CsrMatrixPattern Pattern { get; }

    public void Clear() => Array.Clear(_values);

    public void Add(int row, int column, double value)
    {
        if (value == 0.0)
        {
            return;
        }

        _values[Pattern.FindEntryIndex(row, column)] += value;
    }

    public void AddSymmetric(int row, int column, double value)
    {
        Add(row, column, value);

        if (row != column)
        {
            Add(column, row, value);
        }
    }

    public void AddSubmatrix(ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
        => AddSubmatrix(indices, indices, values);

    public void AddSubmatrix(ReadOnlySpan<int> rowIndices, ReadOnlySpan<int> columnIndices, ReadOnlySpan<double> values)
    {
        if (values.Length != rowIndices.Length * columnIndices.Length)
        {
            throw new ArgumentException("Submatrix value count must equal rowIndices.Length * columnIndices.Length.", nameof(values));
        }

        for (var localRow = 0; localRow < rowIndices.Length; localRow++)
        {
            var row = rowIndices[localRow];

            for (var localColumn = 0; localColumn < columnIndices.Length; localColumn++)
            {
                Add(row, columnIndices[localColumn], values[localRow * columnIndices.Length + localColumn]);
            }
        }
    }

    public void AddSymmetricSubmatrix(ReadOnlySpan<int> indices, ReadOnlySpan<double> values)
    {
        if (values.Length != indices.Length * indices.Length)
        {
            throw new ArgumentException("Submatrix value count must equal indices.Length squared.", nameof(values));
        }

        for (var localRow = 0; localRow < indices.Length; localRow++)
        {
            var row = indices[localRow];

            for (var localColumn = localRow; localColumn < indices.Length; localColumn++)
            {
                AddSymmetric(row, indices[localColumn], values[localRow * indices.Length + localColumn]);
            }
        }
    }

    public CsrMatrix ToCsr() => Pattern.ToCsr(_values);
}
