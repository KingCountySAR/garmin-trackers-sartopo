using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Garmin.Device.Core;

namespace TrackDownloader
{
  public class GarminReader
  {
    private readonly GarminDevice _device;
    private DeviceInformation _deviceInfo = null;

    public GarminReader(GarminDevice device)
    {
      _device = device;
    }

    private bool doReads = true;

    private void ReadThread()
    {
      while (doReads)
      {
        var buffer = _device.GarminRead();
        if (buffer[0] == 0) { Console.WriteLine("End packet"); continue; }

        var packet = Utilities.CreateUSBPacketFromByteArray(buffer);
        Console.WriteLine("Packet id " + packet.packetId);
      }
    }

    public UsbPacket WaitForPacket(short packetId)
    {
      UsbPacket packet;
      do
      {
        var buffer = _device.GarminRead();
        packet = Utilities.CreateUSBPacketFromByteArray(buffer);
      } while (packet.packetId != packetId);

      return packet;
    }

    public DeviceInformation ReadInfo()
    {



      if (_deviceInfo == null)
      {
        var deviceId = StartSession();

        _deviceInfo = new DeviceInformation
        {
          Id = deviceId
        };


        _device.GarminWrite(new UsbPacket(UsbPacketType.APPLICATION_LAYER, (short)L000_packet_id.Pid_Product_Rqst, 0));

        UsbPacket packet;
        var packetFound = false;
        var expectingPacketId = (short)L000_packet_id.Pid_Protocol_Array;

        while (true)
        {
          var buffer = _device.GarminRead();
          if (buffer[0] == 0 && packetFound)
            break;

          packet = Utilities.CreateUSBPacketFromByteArray(buffer);
          if (packet.packetId == expectingPacketId)
            packetFound = true;

          switch (packet.packetId)
          {
            case (short)L000_packet_id.Pid_Product_Data:              
              const int noOfBytesBeforeProductDescription = 4;
              char[] description = new char[packet.dataSize - noOfBytesBeforeProductDescription];

              ASCIIEncoding asciiEncoding = new ASCIIEncoding();

              int noOfCharCopied = asciiEncoding.GetChars(
                                                  packet.data,
                                                  noOfBytesBeforeProductDescription,
                                                  packet.dataSize - noOfBytesBeforeProductDescription,
                                                  description,
                                                  0);

              string temp = new string(description).Replace("\0", "");
              Array.Resize(ref description, temp.Length);
              Array.Copy(temp.ToCharArray(), description, temp.Length);
              _deviceInfo.Description = new string(description);
              break;

            case (short)L000_packet_id.Pid_Ext_Product_Data:
              // no-op
              break;

            case (short)L000_packet_id.Pid_Protocol_Array:
              var list = new List<ProtocolDataType>();
              int sizeOfProtocolDataObject = Marshal.SizeOf(new ProtocolDataType());    //Should be 3.
              byte[] protocolDataTypeAsArray = new byte[sizeOfProtocolDataObject];
              int offset = 0;

              while (offset != packet.dataSize)
              {
                Buffer.BlockCopy(packet.data, offset, protocolDataTypeAsArray, 0, sizeOfProtocolDataObject);

                ProtocolDataType protocolDataType = new ProtocolDataType();
                protocolDataType.tag = protocolDataTypeAsArray[0];
                protocolDataType.data = BitConverter.ToUInt16(protocolDataTypeAsArray, 1);

                list.Add(protocolDataType);

                offset = offset + sizeOfProtocolDataObject;
              }
              _deviceInfo.SupportedProtocols = list.OrderBy(f => f.ToString()).ToList();
              break;
          }
        }


      }
      return _deviceInfo;
    }


    private uint StartSession()
    {

      UsbPacket usbPacket = new UsbPacket(UsbPacketType.USB_PROTOCOL_LAYER, (short)UsbPacketId.Pid_Start_Session, 0);

      _device.GarminWrite(usbPacket);

      UsbPacket returnPacket = _device.GarminReadSingleton();

      // packet returned should be 3.2.3.3 "Pid_Session_Started (6)". 
      // It should also contain the device's unit id. We expect only 1 packet
      if (returnPacket.packetId != (short)UsbPacketId.Pid_Session_Started)
      {
        throw new InvalidOperationException("Unexpected packet id: " + returnPacket.packetId);
      }

      return BitConverter.ToUInt32(returnPacket.data, 0);
    }
  }

  public class DeviceInformation
  {
    public uint Id { get; set; }
    public string Description { get; set; }
    public List<ProtocolDataType> SupportedProtocols { get; set; }
  }
}
