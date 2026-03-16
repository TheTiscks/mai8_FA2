using Arithmetic.BigInt.Interfaces;
using System.Numerics;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const int FftThreshold = 256; // порог FFT

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
        if (digitsA.Length < FftThreshold || digitsB.Length < FftThreshold)
        {
            var karatsuba = new KaratsubaMultiplier();
            return karatsuba.Multiply(a, b);
        }
        uint[] productDigits = FftCore(digitsA, digitsB);
        return new BetterBigInteger(productDigits, resultNegative);
    }

    private static uint[] FftCore(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int size = 1;
        int targetLength = a.Length + b.Length;
        while (size < targetLength)
        {
            size <<= 1;
        }
        var fa = new Complex[size];
        var fb = new Complex[size];
        for (int i = 0; i < a.Length; i++) fa[i] = new Complex(a[i], 0);
        for (int i = 0; i < b.Length; i++) fb[i] = new Complex(b[i], 0);
        Fft(fa, true);
        Fft(fb, true);
        for (int i = 0; i < size; i++) fa[i] *= fb[i];
        Fft(fa, false);
        var result = new uint[targetLength];
        ulong carry = 0;
        for (int i = 0; i < targetLength; i++)
        {
            double value = fa[i].Real / size;
            long ival = (long)Math.Round(value) + (long)carry;
            carry = (ulong)ival >> 32;
            result[i] = (uint)(ival & 0xFFFFFFFF);
        }
        if (carry != 0)
        {
            Array.Resize(ref result, result.Length + 1);
            result[result.Length - 1] = (uint)carry;
        }
        int lastNonZero = result.Length - 1;
        while (lastNonZero > 0 && result[lastNonZero] == 0) lastNonZero--;
        if (lastNonZero != result.Length - 1)
        {
            Array.Resize(ref result, lastNonZero + 1);
        }
        return result;
    }

    private static void Fft(Complex[] buffer, bool inverse)
    {
    }
}