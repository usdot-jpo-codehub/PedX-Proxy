namespace Proxy.Utilities;

public static class ByteArrayExtensions
{
    public static bool[] ConvertToBools(this byte[] byteArray)
    {
        bool[] boolArray = new bool[byteArray.Length * 8]; // Each byte has 8 bits
        for (int byteIndex = 0; byteIndex < byteArray.Length; byteIndex++)
        {
            byte currentByte = byteArray[byteIndex];
            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                // Shift the bit to the least significant position and mask with 1 to isolate it
                boolArray[byteIndex * 8 + bitIndex] = (currentByte & (1 << bitIndex)) != 0;
            }
        }

        return boolArray;
    }

    public static byte[] SetBits(this byte[] byteArray, int[] bitIndices, bool value)
    {
        foreach (int bitIndex in bitIndices)
        {
            int byteIndex = bitIndex / 8;
            int bitPosition = bitIndex % 8;

            if (byteIndex >= byteArray.Length)
                throw new ArgumentOutOfRangeException(nameof(bitIndex),
                    "At least one bit index is out of the range of the byte array.");

            byteArray[byteIndex] = value
                ? (byte)(byteArray[byteIndex] | (1 << bitPosition))
                : (byte)(byteArray[byteIndex] & ~(1 << bitPosition));
        }

        return byteArray;
    }

    public static byte[] SetBit(this byte[] byteArray, int bitIndex, bool value)
    {
        int byteIndex = bitIndex / 8;
        int bitPosition = bitIndex % 8;

        if (byteIndex >= byteArray.Length)
            throw new ArgumentOutOfRangeException(nameof(bitIndex),
                "The bit index is out of the range of the byte array.");

        byteArray[byteIndex] = value
            ? (byte)(byteArray[byteIndex] | (1 << bitPosition))
            : (byte)(byteArray[byteIndex] & ~(1 << bitPosition));

        return byteArray;
    }


    public static bool GetBits(this byte[] byteArray, int[] bitIndices)
    {
        bool result = true;
        foreach (var bitIndex in bitIndices)
        {
            int byteIndex = bitIndex / 8; // Calculate the index of the byte in the array
            int bitPosition = bitIndex % 8; // Calculate the position of the bit within the byte

            if (byteIndex >= byteArray.Length)
                throw new ArgumentOutOfRangeException(nameof(bitIndex),
                    "The bit index is out of the range of the byte array.");

            if ((byteArray[byteIndex] & (1 << bitPosition)) == 0)
                result = false;
        }

        return result;
    }

    public static bool GetBit(this byte[] byteArray, int bitIndex)
    {
        int byteIndex = bitIndex / 8; // Calculate the index of the byte in the array
        int bitPosition = bitIndex % 8; // Calculate the position of the bit within the byte

        if (byteIndex >= byteArray.Length)
            throw new ArgumentOutOfRangeException(nameof(bitIndex),
                "The bit index is out of the range of the byte array.");

        return (byteArray[byteIndex] & (1 << bitPosition)) != 0;
    }
}