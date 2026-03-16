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
        int n = Math.Max(x.Length, y.Length);
        if (n == 0)
        {
            return new uint[] { 0 };
        }
        if (n < 32) {
            return MultiplySimple(x, y);
        }
        int m = n / 2;
        ReadOnlySpan<uint> lowX = x.Length > m ? x.Slice(0, m) : x;
        ReadOnlySpan<uint> highX = x.Length > m ? x.Slice(m) : ReadOnlySpan<uint>.Empty;
        ReadOnlySpan<uint> lowY = y.Length > m ? y.Slice(0, m) : y;
        ReadOnlySpan<uint> highY = y.Length > m ? y.Slice(m) : ReadOnlySpan<uint>.Empty;
        
        uint[] z0 = KaratsubaCore(lowX, lowY); // рекурсия
        uint[] z2 = KaratsubaCore(highX, highY);

        uint[] sumX = Add(lowX, highX);
        uint[] sumY = Add(lowY, highY);
        uint[] prodSum = KaratsubaCore(sumX, sumY);
        uint[] z1 = Subtract(Subtract(prodSum, z0), z2);
        uint[] result = new uint[z0.Length + 2 * m + z2.Length];
        Array.Copy(z0, 0, result, 0, z0.Length);
        AddInPlace(result, z1, m);
        AddInPlace(result, z2, 2 * m);
        int lastNonZero = result.Length - 1;
        while (lastNonZero > 0 && result[lastNonZero] == 0)
        {
            lastNonZero--;
        }
        if (lastNonZero != result.Length - 1)
        {
            Array.Resize(ref result, lastNonZero + 1);
        }
        return result;
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
        {
            result[maxLen] = (uint)carry;
        }
        return result;
    }

    private static uint[] MultiplySimple(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        uint[] result = new uint[a.Length + b.Length];
        for (int i = 0; i < a.Length; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < b.Length; j++)
            {
                int idx = i + j;
                ulong product = (ulong)a[i] * (ulong)b[j] + result[idx] + carry;
                result[idx] = (uint)product;
                carry = product >> 32;
            }
            int pos = i + b.Length;
            while (carry != 0)
            {
                if (pos < result.Length)
                {
                    ulong sum = result[pos] + carry;
                    result[pos] = (uint)sum;
                    carry = sum >> 32;
                    pos++;
                }
                else
                {
                    Array.Resize(ref result, result.Length + 1);
                    result[pos] = (uint)carry;
                    carry >>= 32;
                }
            }
        }
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