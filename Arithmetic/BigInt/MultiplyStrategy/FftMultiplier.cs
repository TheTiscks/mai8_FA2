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

    }
    
}