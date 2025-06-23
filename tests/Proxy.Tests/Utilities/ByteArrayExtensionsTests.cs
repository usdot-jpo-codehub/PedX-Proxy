using Proxy.Utilities;

namespace Proxy.Tests.Utilities;

[TestClass]
public class ByteArrayExtensionsTests
{
    [TestMethod]
    public void ConvertToBools_ShouldConvertBytesToBooleans()
    {
        // Arrange
        byte[] bytes = { 0b00000101, 0b10100000 }; // First byte: bits 0 and 2 set, Second byte: bits 5 and 7 set

        // Act
        bool[] result = bytes.ConvertToBools();

        // Assert
        Assert.AreEqual(16, result.Length); // 2 bytes * 8 bits = 16 bits
        
        // First byte (bits 0-7)
        Assert.IsTrue(result[0]); // bit 0 set
        Assert.IsFalse(result[1]); // bit 1 not set
        Assert.IsTrue(result[2]); // bit 2 set
        Assert.IsFalse(result[3]); // bit 3 not set
        Assert.IsFalse(result[4]); // bit 4 not set
        Assert.IsFalse(result[5]); // bit 5 not set
        Assert.IsFalse(result[6]); // bit 6 not set
        Assert.IsFalse(result[7]); // bit 7 not set
        
        // Second byte (bits 8-15)
        Assert.IsFalse(result[8]); // bit 8 not set
        Assert.IsFalse(result[9]); // bit 9 not set
        Assert.IsFalse(result[10]); // bit 10 not set
        Assert.IsFalse(result[11]); // bit 11 not set
        Assert.IsFalse(result[12]); // bit 12 not set
        Assert.IsTrue(result[13]); // bit 13 set
        Assert.IsFalse(result[14]); // bit 14 not set
        Assert.IsTrue(result[15]); // bit 15 set
    }

    [TestMethod]
    public void SetBits_ShouldSetMultipleBits()
    {
        // Arrange
        byte[] bytes = { 0b00000000, 0b00000000 }; // All bits clear
        int[] bitIndices = { 1, 3, 9, 15 }; // Set bits 1, 3 in first byte, and 1, 7 in second byte

        // Act
        byte[] result = bytes.SetBits(bitIndices, true);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0b00001010, result[0]); // Bits 1 and 3 set
        Assert.AreEqual(0b10000010, result[1]); // Bits 1 and 7 set
    }

    [TestMethod]
    public void SetBits_ShouldClearMultipleBits()
    {
        // Arrange
        byte[] bytes = { 0b11111111, 0b11111111 }; // All bits set
        int[] bitIndices = { 1, 3, 9, 15 }; // Clear bits 1, 3 in first byte, and 1, 7 in second byte

        // Act
        byte[] result = bytes.SetBits(bitIndices, false);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0b11110101, result[0]); // Bits 1 and 3 cleared
        Assert.AreEqual(0b01111101, result[1]); // Bits 1 and 7 cleared
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void SetBits_WithOutOfRangeBitIndex_ShouldThrowException()
    {
        // Arrange
        byte[] bytes = { 0b00000000 }; // Single byte
        int[] bitIndices = { 1, 8 }; // Bit 8 is out of range for a single byte

        // Act & Assert - This should throw an exception
        bytes.SetBits(bitIndices, true);
    }

    [TestMethod]
    public void SetBit_ShouldSetSingleBit()
    {
        // Arrange
        byte[] bytes = { 0b00000000, 0b00000000 }; // All bits clear

        // Act
        byte[] result = bytes.SetBit(10, true); // Set bit 2 in second byte

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0b00000000, result[0]); // First byte unchanged
        Assert.AreEqual(0b00000100, result[1]); // Bit 2 set in second byte
    }

    [TestMethod]
    public void SetBit_ShouldClearSingleBit()
    {
        // Arrange
        byte[] bytes = { 0b11111111, 0b11111111 }; // All bits set

        // Act
        byte[] result = bytes.SetBit(10, false); // Clear bit 2 in second byte

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0b11111111, result[0]); // First byte unchanged
        Assert.AreEqual(0b11111011, result[1]); // Bit 2 cleared in second byte
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void SetBit_WithOutOfRangeBitIndex_ShouldThrowException()
    {
        // Arrange
        byte[] bytes = { 0b00000000 }; // Single byte

        // Act & Assert - This should throw an exception
        bytes.SetBit(8, true); // Bit 8 is out of range for a single byte
    }

    [TestMethod]
    public void GetBit_ShouldReturnCorrectBitValue()
    {
        // Arrange
        byte[] bytes = { 0b00001010, 0b10000010 }; // Bits 1 and 3 set in first byte, bits 1 and 7 set in second byte

        // Act & Assert
        Assert.IsFalse(bytes.GetBit(0)); // Bit 0 not set
        Assert.IsTrue(bytes.GetBit(1)); // Bit 1 set
        Assert.IsFalse(bytes.GetBit(2)); // Bit 2 not set
        Assert.IsTrue(bytes.GetBit(3)); // Bit 3 set
        Assert.IsFalse(bytes.GetBit(8)); // Bit 0 of second byte not set
        Assert.IsTrue(bytes.GetBit(9)); // Bit 1 of second byte set
        Assert.IsTrue(bytes.GetBit(15)); // Bit 7 of second byte set
    }

    [TestMethod]
    public void GetBit_WithOutOfRangeBitIndex_WithMessageValidation_ShouldThrowException()
    {
        // Arrange
        byte[] bytes = { 0x01, 0x02 };
        int invalidBitIndex = 16; // Only have 16 bits (0-15), so 16 is out of range

        // Act & Assert
        try
        {
            bytes.GetBit(invalidBitIndex);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StringAssert.Contains(ex.Message, "The bit index is out of the range of the byte array");
        }
    }

    [TestMethod]
    public void GetBits_WithOutOfRangeBitIndex_WithMessageValidation_ShouldThrowException()
    {
        // Arrange
        byte[] bytes = { 0x01, 0x02 };
        int[] bitIndices = { 1, 16 }; // Second index is out of range

        // Act & Assert
        try
        {
            bytes.GetBits(bitIndices);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StringAssert.Contains(ex.Message, "The bit index is out of the range of the byte array");
        }
    }

    [TestMethod]
    public void SetBit_WithOutOfRangeBitIndex_WithMessageValidation_ShouldThrowException()
    {
        // Arrange
        byte[] bytes = { 0x01, 0x02 };
        int invalidBitIndex = 16; // Only have 16 bits (0-15), so 16 is out of range

        // Act & Assert
        try
        {
            bytes.SetBit(invalidBitIndex, true);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StringAssert.Contains(ex.Message, "The bit index is out of the range of the byte array");
        }
    }

    [TestMethod]
    public void SetBits_WithOutOfRangeBitIndex_WithMessageValidation_ShouldThrowException()
    {
        // Arrange
        byte[] bytes = { 0x01, 0x02 };
        int[] bitIndices = { 1, 16 }; // Second index is out of range

        // Act & Assert
        try
        {
            bytes.SetBits(bitIndices, true);
            Assert.Fail("Expected ArgumentOutOfRangeException was not thrown");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StringAssert.Contains(ex.Message, "At least one bit index is out of the range of the byte array");
        }
    }

    [TestMethod]
    public void GetBits_WhenAllBitsAreSet_ShouldReturnTrue()
    {
        // Arrange
        byte[] bytes = { 0b00001010, 0b10000010 }; // Bits 1 and 3 set in first byte, bits 1 and 7 set in second byte
        int[] bitIndices = { 1, 3, 9, 15 }; // All these bits are set

        // Act
        bool result = bytes.GetBits(bitIndices);

        // Assert
        Assert.IsTrue(result, "GetBits should return true when all specified bits are set");
    }

    [TestMethod]
    public void GetBits_WhenAtLeastOneBitIsNotSet_ShouldReturnFalse()
    {
        // Arrange
        byte[] bytes = { 0b00001010, 0b10000010 }; // Bits 1 and 3 set in first byte, bits 1 and 7 set in second byte
        int[] bitIndices = { 1, 2, 9 }; // Bit 2 is not set

        // Act
        bool result = bytes.GetBits(bitIndices);

        // Assert
        Assert.IsFalse(result, "GetBits should return false when at least one specified bit is not set");
    }

    [TestMethod]
    public void GetBits_WhenEmptyBitIndices_ShouldReturnTrue()
    {
        // Arrange
        byte[] bytes = { 0b00000000 }; // All bits clear
        int[] bitIndices = new int[0]; // No bits specified

        // Act
        bool result = bytes.GetBits(bitIndices);

        // Assert
        Assert.IsTrue(result, "GetBits should return true when no bit indices are specified");
    }
}
