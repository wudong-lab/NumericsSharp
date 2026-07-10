namespace NumericsSharp.Core.LinearAlgebra;

public interface ILinearOperator
{
    int RowCount { get; }
    int ColumnCount { get; }

    void Multiply(ReadOnlySpan<double> x, Span<double> y);
}
