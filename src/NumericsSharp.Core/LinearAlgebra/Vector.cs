namespace NumericsSharp.Core.LinearAlgebra;

public sealed class Vector
{
    private readonly double[] _values;

    public Vector(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        this._values = new double[count];
    }

    private Vector(double[] values)
    {
        this._values = values;
    }

    public int Count => this._values.Length;
    public Span<double> Values => this._values;
    public ReadOnlySpan<double> ReadOnlyValues => this._values;

    public double this[int index]
    {
        get => this._values[index];
        set => this._values[index] = value;
    }

    public static Vector Zero(int count) => new(count);

    public static Vector FromArray(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            throw new ArgumentException("Vector must contain at least one value.", nameof(values));

        return new Vector(values.ToArray());
    }

    public Vector Clone() => new((double[])this._values.Clone());
}
