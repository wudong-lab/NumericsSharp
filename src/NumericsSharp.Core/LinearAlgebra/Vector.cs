namespace NumericsSharp.Core.LinearAlgebra;

/// <summary>
/// 表示由双精度浮点数组存储的稠密向量。
/// </summary>
public sealed class Vector
{
    private readonly double[] _values;

    /// <summary>
    /// 创建指定长度且所有元素为零的向量。
    /// </summary>
    /// <param name="count">向量长度。</param>
    /// <exception cref="ArgumentOutOfRangeException">长度小于 1 时抛出。</exception>
    public Vector(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        this._values = new double[count];
    }

    private Vector(double[] values)
    {
        this._values = values;
    }

    /// <summary>
    /// 获取向量长度。
    /// </summary>
    public int Count => this._values.Length;

    /// <summary>
    /// 获取可读写的向量元素跨度。
    /// </summary>
    public Span<double> Values => this._values;

    /// <summary>
    /// 获取只读的向量元素跨度。
    /// </summary>
    public ReadOnlySpan<double> ReadOnlyValues => this._values;

    /// <summary>
    /// 获取或设置指定索引处的向量元素。
    /// </summary>
    /// <param name="index">元素索引。</param>
    /// <exception cref="IndexOutOfRangeException">索引超出向量范围时抛出。</exception>
    public double this[int index]
    {
        get => this._values[index];
        set => this._values[index] = value;
    }

    /// <summary>
    /// 创建指定长度且所有元素为零的向量。
    /// </summary>
    /// <param name="count">向量长度。</param>
    /// <returns>新建的零向量。</returns>
    /// <exception cref="ArgumentOutOfRangeException">长度小于 1 时抛出。</exception>
    public static Vector Zero(int count) => new(count);

    /// <summary>
    /// 从给定数值复制创建向量。
    /// </summary>
    /// <param name="values">向量元素。</param>
    /// <returns>包含输入数值副本的新向量。</returns>
    /// <exception cref="ArgumentException">输入为空时抛出。</exception>
    public static Vector FromArray(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            throw new ArgumentException("Vector must contain at least one value.", nameof(values));

        return new Vector(values.ToArray());
    }

    /// <summary>
    /// 创建当前向量的独立副本。
    /// </summary>
    /// <returns>与当前向量具有相同元素的新向量。</returns>
    public Vector Clone() => new((double[])this._values.Clone());
}
