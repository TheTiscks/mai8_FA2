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
    
    public int CompareTo(IBigInteger? other) => throw new NotImplementedException();
    public bool Equals(IBigInteger? other) => throw new NotImplementedException();
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode() => throw new NotImplementedException();
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator -(BetterBigInteger a) => throw new NotImplementedException();
    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) => throw new NotImplementedException();
    
    
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
       => throw new NotImplementedException("Умножение делегируется стратегии, выбирать необходимо в зависимости от размеров чисел");
    
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
    
    public override string ToString() => ToString(10);
    public string ToString(int radix) => throw new NotImplementedException();
    
}