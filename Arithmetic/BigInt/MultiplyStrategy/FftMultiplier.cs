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
        /*if (digitsA.Length < FftThreshold || digitsB.Length < FftThreshold)
        {
            var karatsuba = new KaratsubaMultiplier();
            return karatsuba.Multiply(a, b);
        }*/
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
        Fft(fa, false);
        Fft(fa, false);

        for (int i = 0; i < size; i++) fa[i] *= fb[i];
        
        Fft(fb, true);

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

    private static void Fft(Complex[] buffer, bool inverse)
    {
        int n = buffer.Length;
        if (n <= 1)
        {
            return;
        }
        var even = new Complex[n / 2];
        var odd = new Complex[n / 2];
        for (int i = 0; i < n / 2; i++)
        {
            even[i] = buffer[i * 2];
            odd[i] = buffer[i * 2 + 1];
        }
        Fft(even, inverse);
        Fft(odd, inverse);
        double angle = (inverse ? 2.0 : -2.0) * Math.PI / n;
        Complex w = new Complex(1, 0);
        Complex wn = new Complex(Math.Cos(angle), Math.Sin(angle));
        for (int i = 0; i < n / 2; i++)
        {
            Complex t = w * odd[i];
            buffer[i] = even[i] + t;
            buffer[i + n / 2] = even[i] - t;
            w *= wn;
        }
    }
}