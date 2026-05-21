using System.Numerics;

namespace NImpeller;

/// <summary>4x4 transform matrix value used by Impeller drawing APIs.</summary>
public partial struct ImpellerMatrix
{
    /// <summary>The managed matrix backing this Impeller matrix value.</summary>
    public Matrix4x4 Matrix;

    /// <summary>Convert a managed <see cref="Matrix4x4"/> into an Impeller matrix.</summary>
    /// <param name="m">Managed matrix value.</param>
    public static implicit operator ImpellerMatrix(System.Numerics.Matrix4x4 m)
    {
        return new ImpellerMatrix() { Matrix = m };
    }
}
