using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }
        bool resultNegative = a.IsNegative ^ b.IsNegative;
        ReadOnlySpan<uint> digitsA = a.GetDigits();
        ReadOnlySpan<uint> digitsB = b.GetDigits();
        uint[] productDigits = MultiplyArrays(digitsA, digitsB);
        return new BetterBigInteger(productDigits, resultNegative);
    }
    
    private static uint[] MultiplyArrays(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return new uint[] { 0 };
        }
        int lenA = a.Length;
        int lenB = b.Length;
        List<uint> result = new List<uint>(lenA + lenB);
        for (int i = 0; i < lenA + lenB; i++)
        {
            result.Add(0);
        }
        for (int i = 0; i < lenA; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < lenB; j++)
            {
                int idx = i + j;
                ulong product = (ulong)a[i] * (ulong)b[j] + result[idx] + carry;
                result[idx] = (uint)product;
                carry = product >> 32;
            }

            if (carry != 0)
            {
                // perenosa obrabotka
            }
        }
        while (result.Count > 1 && result[result.Count - 1] == 0) // remove lead 0
        {
            result.RemoveAt(result.Count - 1);
        }
        return result.ToArray();
    }
}