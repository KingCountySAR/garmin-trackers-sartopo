using System.Runtime.InteropServices;

namespace Garmin.Device.Core
{
  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct ProtocolDataType
  {
    public byte tag;
    public ushort data;

    public ProtocolDataType(byte tag, ushort data)
    {
      this.tag = tag;
      this.data = data;
    }

    public ProtocolDataType(ProtocolDataTypeId tag, ushort data)
    {
      this.tag = (byte)tag;
      this.data = data;
    }

    public override string ToString()
    {
      return string.Format("{0}{1:000}", (char)tag, data);
    }
  }

  public enum ProtocolDataTypeId : byte
  {
    Tag_Phys_Prot_Id = (byte)'P',  /* tag for Physical protocol ID */
    Tag_Link_Prot_Id = (byte)'L',  /* tag for Link protocol ID */
    Tag_Appl_Prot_Id = (byte)'A',  /* tag for Application protocol ID */
    Tag_Data_Type_Id = (byte)'D'   /* tag for Data Type ID */
  }
}
