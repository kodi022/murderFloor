namespace MurderFloor;

public static class Compression
{
    public const string ArithmeticBase64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+/";

    public static string IntToArithmeticBase64(int value)
    {
        var negative = value < 0;
        var number = (uint)Math.Abs((long)value);

        if (number == 0)
            return "0";

        Span<char> buffer = stackalloc char[6];
        var index = buffer.Length;

        while (number > 0)
        {
            buffer[--index] = ArithmeticBase64[(int)(number % 64)];
            number /= 64;
        }

        return negative ? "-" + new string(buffer[index..]) : new string(buffer[index..]);
    }

    public static int ArithmeticBase64ToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            GD.PushError("ArithmeticBase64ToInt value cannot be empty");
            return 0;
        }

        var negative = value[0] == '-';
        var start = negative ? 1 : 0;

        if (start == value.Length)
        {
            GD.PushError("ArithmeticBase64ToInt invalid value");
            return 0;
        }

        var number = 0;
        for (var i = start; i < value.Length; i++)
        {
            var digit = ArithmeticBase64.IndexOf(value[i]);

            if (digit < 0)
            {
                GD.PushError($"ArithmeticBase64ToInt invalid characer: {value[i]}");
                return 0;
            }

            number = checked(number * 64 + digit);
        }

        return negative ? -number : number;
    }
}