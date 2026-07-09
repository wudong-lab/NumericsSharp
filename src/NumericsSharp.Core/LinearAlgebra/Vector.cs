namespace NumericsSharp.Core.LinearAlgebra;

public sealed class Vector
{
    private readonly double[] _values;

    public Vector(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        _values = new double[count];
    }

    private Vector(double[] values)
    {
        _values = values;
    }

    public int Count => _values.Length;

    public Span<double> Values => _values;

    public ReadOnlySpan<double> ReadOnlyValues => _values;

    public double this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    public static Vector Zero(int count) => new(count);

    public static Vector FromArray(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
        {
            throw new ArgumentException("Vector must contain at least one value.", nameof(values));
        }

        return new Vector(values.ToArray());
    }

    public Vector Clone() => new((double[])_values.Clone());
}
