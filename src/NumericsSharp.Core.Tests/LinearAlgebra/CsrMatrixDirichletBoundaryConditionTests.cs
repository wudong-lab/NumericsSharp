using NumericsSharp.Core.LinearAlgebra;

namespace NumericsSharp.Core.Tests.LinearAlgebra;

[TestClass]
public sealed class CsrMatrixDirichletBoundaryConditionTests
{
    [TestMethod]
    public void ApplyDirichletBoundaryConditions_EliminatesConstrainedRowsAndColumnsAndAdjustsRightHandSide()
    {
        var builder = new SparseMatrixBuilder(3, 3);
        builder.AddSymmetric(0, 0, 2.0);
        builder.AddSymmetric(0, 1, -1.0);
        builder.AddSymmetric(1, 1, 2.0);
        builder.AddSymmetric(1, 2, -1.0);
        builder.AddSymmetric(2, 2, 2.0);

        var matrix = builder.ToCsr();
        var rightHandSide = new[] { 0.0, 0.0, 0.0 };

        var constrainedMatrix = matrix.ApplyDirichletBoundaryConditions(rightHandSide, [0], [10.0]);

        CollectionAssert.AreEqual(new[] {0, 1, 3, 5}, constrainedMatrix.RowOffsets);
        CollectionAssert.AreEqual(new[] {0, 1, 2, 1, 2}, constrainedMatrix.ColumnIndices);
        CollectionAssert.AreEqual(new[] {1.0, 2.0, -1.0, -1.0, 2.0}, constrainedMatrix.Values);
        CollectionAssert.AreEqual(new[] {10.0, 10.0, 0.0}, rightHandSide);
    }
}
