using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;
using System.Runtime.InteropServices;

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
        if (_data != null)
            return new ReadOnlySpan<uint>(_data);
        return MemoryMarshal.CreateReadOnlySpan(ref _smallValue, 1);
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
    
    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        int targetLen = a.GetDigits().Length + 1;
        uint[] twosA = ToTwosComplement(a.GetDigits(), a.IsNegative, targetLen);
        for (int i = 0; i < targetLen; i++)
        {
            twosA[i] = ~twosA[i];
        }
        (uint[] mag, bool neg) = FromTwosComplement(twosA);
        return new BetterBigInteger(mag, neg);
    }
    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        return BitwiseOp(a, b, (x, y) => x & y);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        return BitwiseOp(a, b, (x, y) => x | y);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));
        return BitwiseOp(a, b, (x, y) => x ^ y);
    }
    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift), "Shift must be non-negative");
        }
        if (shift == 0)
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }
        uint[] magnitude = a.GetDigits().ToArray();
        uint[] result = ShiftLeft(magnitude, shift);
        return new BetterBigInteger(result, a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift), "Shift must be non-negative");
        }
        if (shift == 0)
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }
        if (!a.IsNegative)
        {
            uint[] result = ShiftRight(a.GetDigits(), shift);
            return new BetterBigInteger(result, false);
        }
        else
        {
            // для отриц чисел эквив.: a >> k = -(((-a) + (1<<k) - 1) >> k)
            BetterBigInteger absA = new BetterBigInteger(a.GetDigits().ToArray(), false);
            BetterBigInteger power = new BetterBigInteger(new uint[] { 1 }, false) << shift;
            BetterBigInteger numerator = absA + power - new BetterBigInteger(new uint[] { 1 }, false);
            BetterBigInteger shifted = numerator >> shift; // для положительного числа
            return -shifted;
        }
    }
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    
    private static uint[] ToTwosComplement(ReadOnlySpan<uint> magnitude, bool negative, int targetLength)
    {
        uint[] result = new uint[targetLength];
        magnitude.CopyTo(result);
        if (!negative)
        {
            return result;
        }
        for (int i = 0; i < targetLength; i++)
            result[i] = ~result[i];
        ulong carry = 1;
        for (int i = 0; i < targetLength && carry != 0; i++)
        {
            ulong sum = result[i] + carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        return result;
    }

    private static (uint[] magnitude, bool negative) FromTwosComplement(uint[] twos)
    {
        int last = twos.Length - 1;
        bool negative = (twos[last] & 0x80000000) != 0;
        if (!negative)
        {
            Normalize(ref twos);
            return (twos, false);
        }
        else
        {
            uint[] mag = new uint[twos.Length];
            for (int i = 0; i < twos.Length; i++)
                mag[i] = ~twos[i];
            ulong carry = 1;
            for (int i = 0; i < mag.Length && carry != 0; i++)
            {
                ulong sum = mag[i] + carry;
                mag[i] = (uint)sum;
                carry = sum >> 32;
            }
            Normalize(ref mag);
            return (mag, true);
        }
    }
    
    private static BetterBigInteger BitwiseOp(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op)
    {
        int lenA = a.GetDigits().Length;
        int lenB = b.GetDigits().Length;
        int targetLen = Math.Max(lenA, lenB) + 1;

        uint[] twosA = ToTwosComplement(a.GetDigits(), a.IsNegative, targetLen);
        uint[] twosB = ToTwosComplement(b.GetDigits(), b.IsNegative, targetLen);

        uint[] resultTwos = new uint[targetLen];
        for (int i = 0; i < targetLen; i++)
            resultTwos[i] = op(twosA[i], twosB[i]);

        (uint[] mag, bool neg) = FromTwosComplement(resultTwos);
        return new BetterBigInteger(mag, neg);
    }
    
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
    
    private static uint[] DivideByDigit(ReadOnlySpan<uint> a, uint divisor, out uint remainder)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }
        uint[] quotient = new uint[a.Length];
        ulong carry = 0;
        for (int i = a.Length - 1; i >= 0; i--)
        {
            ulong current = (carry << 32) | a[i];
            quotient[i] = (uint)(current / divisor);
            carry = current % divisor;
        }
        remainder = (uint)carry;
        Normalize(ref quotient);
        return quotient;
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
        if (divisor.Length == 0 || (divisor.Length == 1 && divisor[0] == 0))
        {
            throw new DivideByZeroException();
        }
        int cmp = CompareAbs(dividend, divisor);
        if (cmp < 0)
        {
            return new uint[] { 0 };
        }
        if (cmp == 0)
        {
            return new uint[] { 1 };
        }
        int shift = 0;
        uint d = divisor[divisor.Length - 1];
        while (d < 0x80000000)
        {
            d <<= 1;
            shift++;
        }
        uint[] u = ShiftLeft(dividend, shift);
        uint[] v = ShiftLeft(divisor, shift);
        int m0 = dividend.Length - divisor.Length;
        int n = v.Length;
        int expectedULen = n + m0 + 1;
        if (u.Length < expectedULen)
        {
            Array.Resize(ref u, expectedULen);
        }
        if (n == 1)
        {
            uint divisorDigit = v[0];
            uint[] quotient = new uint[u.Length];
            ulong carry = 0;
            for (int i = u.Length - 1; i >= 0; i--)
            {
                ulong current = (carry << 32) | u[i];
                quotient[i] = (uint)(current / divisorDigit);
                carry = current % divisorDigit;
            }
            Normalize(ref quotient);
            return quotient;
        }
        int m = u.Length - n;
        uint[] q = new uint[m + 1];
        for (int j = m - 1; j >= 0; j--)
        {
            ulong uj = ((ulong)u[j + n] << 32) | u[j + n - 1];
            ulong vn = v[n - 1];
            ulong qhat = uj / vn;
            ulong rhat = uj % vn;
            while (qhat >= 0x100000000UL ||
                   (n > 1 && qhat * v[n - 2] > ((rhat << 32) | (j + n - 2 >= 0 ? u[j + n - 2] : 0UL))))
            {
                qhat--;
                rhat += vn;
                if (rhat >= 0x100000000UL)
                {
                    break;
                }
            }
            long borrow = 0;
            for (int i = 0; i < n; i++)
            {
                long diff = u[j + i] - (long)(qhat * v[i]) - borrow;
                if (diff < 0)
                {
                    diff += 1L << 32;
                    borrow = 1;
                }
                else
                {
                    borrow = 0;
                }
                u[j + i] = (uint)diff;
            }
            long diffLast = u[j + n] - borrow;
            if (diffLast < 0)
            {
                ulong carry = 0;
                for (int i = 0; i < n; i++)
                {
                    ulong sum = (ulong)u[j + i] + v[i] + carry;
                    u[j + i] = (uint)sum;
                    carry = sum >> 32;
                }
                u[j + n] = (uint)((ulong)u[j + n] + carry);
                qhat--;
            }
            else
            {
                u[j + n] = (uint)diffLast;
            }
            q[j] = (uint)qhat;
        }

        Normalize(ref q);
        return q;
    }
    
    private static uint[] RemainderAbs(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
        {
            if (divisor.Length == 0 || (divisor.Length == 1 && divisor[0] == 0))
            {
                throw new DivideByZeroException();
            }
            int cmp = CompareAbs(dividend, divisor);
            if (cmp < 0)
            {
                return dividend.ToArray();
            }
            if (cmp == 0)
            {
                return new uint[] { 0 };
            }
            int shift = 0;
            uint d = divisor[divisor.Length - 1];
            while (d < 0x80000000)
            {
                d <<= 1;
                shift++;
            }
            uint[] u = ShiftLeft(dividend, shift);
            uint[] v = ShiftLeft(divisor, shift);
            int m0 = dividend.Length - divisor.Length;
            int n = v.Length;
            int expectedULen = n + m0 + 1;
            if (u.Length < expectedULen)
            {
                Array.Resize(ref u, expectedULen);
            }
            if (n == 1)
            {
                uint divisorDigit = v[0];
                ulong carry = 0;
                for (int i = u.Length - 1; i >= 0; i--)
                {
                    ulong current = (carry << 32) | u[i];
                    carry = current % divisorDigit;
                }
                uint[] remArray = new uint[] { (uint)carry };
                if (shift > 0)
                {
                    remArray = ShiftRight(remArray, shift);
                }
                Normalize(ref remArray);
                return remArray;
            }
            int m = u.Length - n;
            for (int j = m - 1; j >= 0; j--)
            {
                ulong uj = ((ulong)u[j + n] << 32) | u[j + n - 1];
                ulong vn = v[n - 1];
                ulong qhat = uj / vn;
                ulong rhat = uj % vn;
                while (qhat >= 0x100000000UL ||
                       (n > 1 && qhat * v[n - 2] > ((rhat << 32) | (j + n - 2 >= 0 ? u[j + n - 2] : 0UL))))
                {
                    qhat--;
                    rhat += vn;
                    if (rhat >= 0x100000000UL)
                    {
                        break;
                    }
                }
                long borrow = 0;
                for (int i = 0; i < n; i++)
                {
                    long diff = u[j + i] - (long)(qhat * v[i]) - borrow;
                    if (diff < 0)
                    {
                        diff += 1L << 32;
                        borrow = 1;
                    }
                    else
                    {
                        borrow = 0;
                    }
                    u[j + i] = (uint)diff;
                }
                long diffLast = u[j + n] - borrow;
                if (diffLast < 0)
                {
                    ulong carry = 0;
                    for (int i = 0; i < n; i++)
                    {
                        ulong sum = (ulong)u[j + i] + v[i] + carry;
                        u[j + i] = (uint)sum;
                        carry = sum >> 32;
                    }
                    u[j + n] = (uint)((ulong)u[j + n] + carry);
                }
                else
                {
                    u[j + n] = (uint)diffLast;
                }
            }
            uint[] remainderArray = new uint[n];
            Array.Copy(u, 0, remainderArray, 0, n);
            if (shift > 0)
            {
                remainderArray = ShiftRight(remainderArray, shift);
            }
            Normalize(ref remainderArray);
            return remainderArray;
        }
    
    private static uint[] ShiftLeft(ReadOnlySpan<uint> a, int shift)
    {
        if (shift == 0)
        {
            return a.ToArray();
        }
        int wordShift = shift / 32;
        int bitShift = shift % 32;
        int newLength = a.Length + wordShift + 1;
        uint[] result = new uint[newLength];
        for (int i = 0; i < a.Length; i++)
        {
            int pos = i + wordShift;
            ulong val = (ulong)a[i] << bitShift;
            result[pos] |= (uint)val;
            if (pos + 1 < newLength)
                result[pos + 1] |= (uint)(val >> 32);
        }
        Normalize(ref result);
        return result;
    }

    private static uint[] ShiftRight(ReadOnlySpan<uint> a, int shift)
    {
        if (shift == 0)
        {
            return a.ToArray();
        }
        int wordShift = shift / 32;
        int bitShift = shift % 32;
        if (wordShift >= a.Length) return new uint[] { 0 };
        int newLength = a.Length - wordShift;
        uint[] result = new uint[newLength];
        for (int i = 0; i < newLength; i++)
        {
            int srcIdx = i + wordShift;
            ulong val = a[srcIdx];
            if (bitShift > 0 && srcIdx + 1 < a.Length)
                val |= (ulong)a[srcIdx + 1] << 32;
            result[i] = (uint)(val >> bitShift);
        }
        Normalize(ref result);
        return result;
    }
    
    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be between 2 and 36");
        }
        ReadOnlySpan<uint> digits = GetDigits();
        if (digits.Length == 0 || (digits.Length == 1 && digits[0] == 0))
        {
            return "0";
        }
        uint[] temp = digits.ToArray();
        List<char> chars = new List<char>();
        while (!(temp.Length == 1 && temp[0] == 0))
        {
            uint remainder;
            temp = DivideByDigit(temp, (uint)radix, out remainder);
            chars.Add(DigitToChar((int)remainder));
        }
        if (IsNegative)
        {
            chars.Add('-');
        }
        chars.Reverse();
        return new string(chars.ToArray());
    }
}