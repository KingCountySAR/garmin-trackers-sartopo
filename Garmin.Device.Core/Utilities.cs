using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garmin.Device.Core
{
  public static class Utilities
  {
    /// <summary>
    /// Returns byte 4&5 of byteArray as a 16-bit signed integer. 
    /// It is presumed that the parameter byteArray is the byte array equivalent of the USB_Packet type.
    /// </summary>
    /// <param name="byteArray">The byte array equivalent of the USB_Packet type</param>
    /// <returns>-1 if an error occurred, or the packetId</returns>
    public static Int16 GetPacketIDFromByteArray(byte[] byteArray)
    {
      if (byteArray.Length < GarminUSBConstants.PACKET_HEADER_SIZE)
      {
        return -1;  //TODO: Need to throw exception
      }
      /// byte 4&5 of the usbpacket
      Int16 theId = -1;

      try
      {
        theId = BitConverter.ToInt16(byteArray, 4);
      }
      catch
      {
        return -1;
      }

      return theId;
    }

    public static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
    {
      return ((deviceType) << 16) | ((access) << 14) | ((function) << 2) | (method);
    }

    public static void CreateUSBPacketByteArray(UsbPacketType packetType, Int16 packetId, Int32 dataSize, byte[] data, ref byte[] destinationArray)
    {

      /// TODO: The size of the array should not exceed device.packetsize. throw exception if it is

      if (destinationArray == null)
      {
        // The USB Packet header (12 bytes) and data amounting to the size of the data buffer
        destinationArray = new byte[GarminUSBConstants.PACKET_HEADER_SIZE + dataSize];
      }
      else
      {
        Array.Clear(destinationArray, 0, destinationArray.Length);
        Array.Resize(ref destinationArray, GarminUSBConstants.PACKET_HEADER_SIZE + dataSize);
      }

      List<byte> bufferList = new List<byte>();

      bufferList.Add((byte)packetType);                           // byte 0  
      bufferList.Add(new byte());                                 // byte 1
      bufferList.AddRange(BitConverter.GetBytes(new Int16()));    // byte 2 & 3
      bufferList.AddRange(BitConverter.GetBytes(packetId));       // byte 4 & 5
      bufferList.AddRange(BitConverter.GetBytes(new Int16()));    // byte 6 & 7
      bufferList.AddRange(BitConverter.GetBytes(dataSize));       // byte 8,9,10,11
      if ((data != null) && (data.Length != 0))
      {
        bufferList.AddRange(data);                              // byte 12+
      }

      //Copy only the amount of bytes specified in dataSize
      Buffer.BlockCopy(bufferList.ToArray(), 0, destinationArray, 0, destinationArray.Length);
    }

    public static void CreateUSBPacketByteArray(UsbPacket usbPacket, ref byte[] destinationArray)
    {
      CreateUSBPacketByteArray(usbPacket.packetType, usbPacket.packetId, usbPacket.dataSize, usbPacket.data, ref destinationArray);
    }

    public static UsbPacket CreateUSBPacketFromByteArray(byte[] byteArray)
    {
      uint offset = 0;

      UsbPacket usbPacket = new UsbPacket();

      usbPacket.packetType = (UsbPacketType)byteArray[offset];
      offset = offset + 1;

      usbPacket.reserved1 = byteArray[offset];
      offset = offset + 1;

      usbPacket.reserved2 = BitConverter.ToInt16(byteArray, (int)offset);
      offset = offset + 2;

      usbPacket.packetId = BitConverter.ToInt16(byteArray, (int)offset);
      offset = offset + 2;

      usbPacket.reserved3 = BitConverter.ToInt16(byteArray, (int)offset);
      offset = offset + 2;

      usbPacket.dataSize = BitConverter.ToInt32(byteArray, (int)offset);
      offset = offset + 4;

      usbPacket.data = new byte[usbPacket.dataSize];

      Array.Copy(byteArray, offset, usbPacket.data, 0, usbPacket.dataSize);

      return usbPacket;
    }
  }
}
