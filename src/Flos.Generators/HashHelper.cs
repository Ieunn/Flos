namespace Flos.Generators;

/// <summary>
/// FNV-1a 32-bit hash for generating deterministic type IDs.
/// </summary>
internal static class HashHelper
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    public static int ComputeFnv1a(string input)
    {
        uint hash = FnvOffsetBasis;
        for (int i = 0; i < input.Length; i++)
        {
            hash ^= input[i];
            hash *= FnvPrime;
        }
        return unchecked((int)hash);
    }
}
