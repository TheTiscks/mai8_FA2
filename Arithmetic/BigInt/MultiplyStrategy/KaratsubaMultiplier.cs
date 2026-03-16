using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
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
        if (digitsA.Length < 32 || digitsB.Length < 32) // для малых чисел, хватит рекурсии
        {
            var simple = new SimpleMultiplier();
            return simple.Multiply(a, b);
        }
        uint[] productDigits = KaratsubaCore(digitsA, digitsB);
        return new BetterBigInteger(productDigits, resultNegative);
    }

    private static uint[] KaratsubaCore(ReadOnlySpan<uint> x, ReadOnlySpan<uint> y)
    {
        
    }
    
    private static uint[] Add(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        var result = new uint[maxLen + 1];
        ulong carry = 0;
        for (int i = 0; i < maxLen; i++)
        {
            ulong sum = carry;
            if (i < a.Length) sum += a[i];
            if (i < b.Length) sum += b[i];
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        if (carry != 0)
            result[maxLen] = (uint)carry;
        return result;
    }

    private static uint[] Subtract(uint[] a, uint[] b) // a >= b
    {
        var result = new uint[a.Length];
        long borrow = 0;
        for (int i = 0; i < a.Length; i++)
        {
            long diff = a[i] - (i < b.Length ? b[i] : 0L) - borrow;
            if (diff < 0)
            {
                diff += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }
            result[i] = (uint)diff;
        }
        int last = result.Length - 1;
        while (last > 0 && result[last] == 0)
        {
            last--;
        }
        if (last != result.Length - 1)
        {
            Array.Resize(ref result, last + 1);
        }
        return result;
    }
    
    private static void AddInPlace(uint[] target, uint[] source, int shift)
    {
        ulong carry = 0;
        int i = 0;
        for (; i < source.Length || carry != 0; i++)
        {
            int pos = i + shift;
            if (pos >= target.Length)
            {
                Array.Resize(ref target, pos + 1);
            }
            ulong sum = target[pos] + (i < source.Length ? source[i] : 0UL) + carry;
            target[pos] = (uint)sum;
            carry = sum >> 32;
        }
    }
}