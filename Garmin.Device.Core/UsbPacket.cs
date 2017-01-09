using System;
using System.Runtime.InteropServices;

namespace Garmin.Device.Core
{
  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct UsbPacket
  {
    public UsbPacketType packetType;   // byte 0    
    public byte reserved1;              // byte 1
    public short reserved2;             // byte 2 & 3
    public short packetId;              // byte 4 & 5
    public short reserved3;             // byte 6 & 7
    public int dataSize;              // byte 8,9,10,11 
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public byte[] data;                 // byte 12+

    public UsbPacket(UsbPacketType packetType, Int16 packetId, Int32 dataSize)
    {
      this.reserved1 = 0;
      this.reserved2 = 0;
      this.reserved3 = 0;

      this.packetType = packetType;
      this.packetId = packetId;
      this.dataSize = dataSize;
      this.data = new byte[dataSize];
    }

    public UsbPacket(UsbPacketType packetType, Int16 packetId, Int32 dataSize, byte[] data)
    {
      this.reserved1 = 0;
      this.reserved2 = 0;
      this.reserved3 = 0;

      this.packetType = packetType;
      this.packetId = packetId;
      this.dataSize = dataSize;
      this.data = new byte[dataSize];
      Buffer.BlockCopy(data, 0, this.data, 0, dataSize);
    }
  }

  public enum UsbPacketType : byte
  {
    USB_PROTOCOL_LAYER = 0,
    APPLICATION_LAYER = 20
  }

  public enum UsbPacketId : short
  {
    Pid_Data_Available = 2, //2
    Pid_Start_Session = 5, //5
    Pid_Session_Started = 6  //6   
  }
}
