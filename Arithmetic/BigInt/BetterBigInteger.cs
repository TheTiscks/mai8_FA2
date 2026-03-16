using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit; // 0 – полож, 1 – отрицательное
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null)
        {
            throw new ArgumentNullException(nameof(digits));
        }
        InitializeFromDigits(digits, isNegative);
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        if (digits == null)
        {
            throw new ArgumentNullException(nameof(digits));
        }
        InitializeFromDigits(digits.ToArray(), isNegative);
    }
    
    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty");
        }
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be between 2 and 36");
        }
        int start = 0;
        bool isNegative = false;
        if (value[0] == '-')
        {
            isNegative = true;
            start = 1;
        }
        else if (value[0] == '+')
        {
            start = 1;
        }
        if (start >= value.Length)
        {
            throw new FormatException("Invalid number format");
        }
        uint[] result = new uint[] { 0 };
        for (int i = start; i < value.Length; i++)
        {
            char c = value[i];
            int digit = CharToDigit(c, radix);
            if (digit < 0 || digit >= radix)
            {
                throw new FormatException($"Invalid character '{c}' for radix {radix}");
            }
            result = MultiplyByDigit(result, (uint)radix);
            result = AddDigit(result, (uint)digit);
        }
        InitializeFromDigits(result, isNegative);
    }
    
    private void InitializeFromDigits(uint[] digits, bool isNegative)
    {
        Normalize(ref digits);
        if (digits.Length == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }
        if (digits.Length == 1)
        {
            _smallValue = digits[0];
            _data = null;
        }
        else
        {
            _data = digits;
        }
        _signBit = isNegative ? 1 : 0;
    }
    
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }
        bool thisNeg = this.IsNegative;
        bool otherNeg = other.IsNegative;
        if (thisNeg != otherNeg)
        {
            return thisNeg ? -1 : 1;
        }
        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();
        int lenCompare = thisDigits.Length.CompareTo(otherDigits.Length);
        if (lenCompare != 0)
        {
            return thisNeg ? -lenCompare : lenCompare;
        }
        for (int i = thisDigits.Length - 1; i >= 0; i--)
        {
            if (thisDigits[i] != otherDigits[i])
            {
                int cmp = thisDigits[i].CompareTo(otherDigits[i]);
                return thisNeg ? -cmp : cmp;
            }
        }
        return 0;
    }
    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        foreach (uint d in GetDigits())
        {
            hash.Add(d);
        }
        hash.Add(IsNegative);
        return hash.ToHashCode();
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }
        if (a.IsNegative == b.IsNegative)
        {
            uint[] sum = AddAbs(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(sum, a.IsNegative);
        }
        else
        {
            int cmp = CompareAbs(a.GetDigits(), b.GetDigits());
            if (cmp == 0)
            {
                return new BetterBigInteger(new uint[] { 0 }, false);
            }
            if (cmp > 0)
            {
                uint[] diff = SubtractAbs(a.GetDigits(), b.GetDigits());
                return new BetterBigInteger(diff, a.IsNegative);
            }
            else
            {
                uint[] diff = SubtractAbs(b.GetDigits(), a.GetDigits());
                return new BetterBigInteger(diff, b.IsNegative);
            }
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        ReadOnlySpan<uint> digits = a.GetDigits();
        if (digits.Length == 1 && digits[0] == 0)
        {
            return new BetterBigInteger(new uint[] { 0 }, false);
        }
        return new BetterBigInteger(digits.ToArray(), !a.IsNegative);
    }
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }
        int digitsA = a.GetDigits().Length;
        int digitsB = b.GetDigits().Length;
        int maxDigits = Math.Max(digitsA, digitsB);
        IMultiplier multiplier = maxDigits switch
        {
            < 32 => new SimpleMultiplier(),
            < 256 => new KaratsubaMultiplier(),
            _ => new FftMultiplier()
        };
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }
        if (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0)
        {
            throw new DivideByZeroException();
        }
        int cmp = CompareAbs(a.GetDigits(), b.GetDigits());
        if (cmp < 0)
        {
            return new BetterBigInteger(new uint[] { 0 }, false);
        }
        uint[] quotient = DivideAbs(a.GetDigits(), b.GetDigits());
        bool resultNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(quotient, resultNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }
        if (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0)
        {
            throw new DivideByZeroException();
        }
        int cmp = CompareAbs(a.GetDigits(), b.GetDigits());
        if (cmp < 0)
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }
        uint[] remainder = RemainderAbs(a.GetDigits(), b.GetDigits());
        return new BetterBigInteger(remainder, a.IsNegative);
    }
    
    public static BetterBigInteger operator ~(BetterBigInteger a) => throw new NotImplementedException();
    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => throw new NotImplementedException();
    public static BetterBigInteger operator >> (BetterBigInteger a, int shift) => throw new NotImplementedException();
    
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    private static void Normalize(ref uint[] arr)
    {
        int lastNonZero = arr.Length - 1;
        while (lastNonZero > 0 && arr[lastNonZero] == 0)
        {
            lastNonZero--;
        }
        if (lastNonZero != arr.Length - 1)
        {
            Array.Resize(ref arr, lastNonZero + 1);
        }
    }
    
    private static uint[] MultiplyByDigit(ReadOnlySpan<uint> a, uint digit)
    {
        if (digit == 0)
        {
            return new uint[] { 0 };
        }
        uint[] result = new uint[a.Length + 1];
        ulong carry = 0;
        for (int i = 0; i < a.Length; i++)
        {
            ulong product = (ulong)a[i] * digit + carry;
            result[i] = (uint)product;
            carry = product >> 32;
        }
        if (carry != 0)
        {
            result[a.Length] = (uint)carry;
        }
        Normalize(ref result);
        return result;
    }
    
    private static int CharToDigit(char c, int radix)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }
        if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 10;
        }
        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 10;
        }
        return -1;
    }
    
    private static uint[] AddDigit(ReadOnlySpan<uint> a, uint digit)
    {
        uint[] result = a.ToArray();
        ulong carry = digit;
        for (int i = 0; i < result.Length && carry != 0; i++)
        {
            ulong sum = result[i] + carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        if (carry != 0)
        {
            Array.Resize(ref result, result.Length + 1);
            result[result.Length - 1] = (uint)carry;
        }
        return result;
    }
    
    private static char DigitToChar(int digit)
    {
        if (digit < 10)
        {
            return (char)('0' + digit);
        }
        return (char)('a' + digit - 10);
    }
    
    private static int CompareAbs(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length != b.Length)
        {
            return a.Length.CompareTo(b.Length);
        }
        for (int i = a.Length - 1; i >= 0; i--)
        {
            if (a[i] != b[i])
            {
                return a[i].CompareTo(b[i]);
            }
        }
        return 0;
    }
    
    private static uint[] AddAbs(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        uint[] result = new uint[maxLen + 1];
        ulong carry = 0;
        for (int i = 0; i < maxLen; i++)
        {
            ulong sum = carry;
            if (i < a.Length)
            {
                sum += a[i];
            }

            if (i < b.Length)
            {
                sum += b[i];
            }
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        if (carry != 0)
        {
            result[maxLen] = (uint)carry;
        }
        Normalize(ref result);
        return result;
    }

    private static uint[] SubtractAbs(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b) // a >= b
    {
        uint[] result = new uint[a.Length];
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
        Normalize(ref result);
        return result;
    }

    private static uint[] DivideAbs(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
    {
        
    }
    
    private static uint[] RemainderAbs(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
    {
        
    }
    
    public override string ToString() => ToString(10);
    public string ToString(int radix) => throw new NotImplementedException();
    
}