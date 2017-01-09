using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Garmin.Device.Core
{
  public static class UnsafeNativeMethods
  {
    #region General USBConstants

    public const Int64 INVALID_HANDLE_VALUE = -1;
    public const int ERROR_NO_MORE_ITEMS = 259;
    public const int BUFFER_SIZE = 256;

    public const uint FILE_DEVICE_UNKNOWN = 0x00000022;
    public const uint METHOD_BUFFERED = 0;
    public const uint FILE_ANY_ACCESS = 0;

    #endregion

    #region SetupDiGetClassDevs

    /// <summary>
    /// //Retrieve a device information set for the devices in a specified class.
    /// http://msdn.microsoft.com/en-us/library/ff551069.aspx
    /// </summary>
    /// <param name="ClassGuid"></param>
    /// <param name="Enumerator"></param>
    /// <param name="hwndParent"></param>
    /// <param name="Flags"></param>
    /// <returns></returns>
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs
    (
       ref Guid ClassGuid,
       IntPtr Enumerator,
       IntPtr hwndParent,
       DiGetClassFlags Flags
    );

    /* Alternative signatures for SetupDiGetClassDevs

    //Alternate signature 0
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs
    (
       ref Guid ClassGuid,
       IntPtr Enumerator,
       IntPtr hwndParent,
       int Flags
    );

    //Alternate signature 1
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]    // 1st form using a ClassGUID only, with null Enumerator
    public static extern IntPtr SetupDiGetClassDevs
    (           
       ref Guid ClassGuid,
       IntPtr Enumerator,
       IntPtr hwndParent,
       int Flags
    );

    //Alternate signature 2
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]    // 2nd form uses an Enumerator only, with null ClassGUID
    public static extern IntPtr SetupDiGetClassDevs
    (
       IntPtr ClassGuid,
       string Enumerator,
       IntPtr hwndParent,
       int Flags
    );
    */

    /* TODO: Old ORIGINAL code. Must be removed after testing
    //TODO: Can be converted to an enum. Check http://www.pinvoke.net. Search for SetupDiGetClassDevs
    public const int DIGCF_PRESENT = 0x2;          //Return only devices that are currently present.
    public const int DIGCF_ALLCLASSES = 0x4;       //Return a list of installed devices for all classes. If this flag is set, the ClassGuid parameter is ignored.
    public const int DIGCF_DEVICEINTERFACE = 0x10; //Return devices that expose interfaces of the interface class specified by ClassGuid. If this flag is not set, ClassGuid specifies a setup class.
    */

    [FlagsAttribute]
    public enum DiGetClassFlags : uint
    {
      DIGCF_DEFAULT = 0x00000001,   // only valid with DIGCF_DEVICEINTERFACE
      DIGCF_PRESENT = 0x00000002,   //Return only devices that are currently present.     
      DIGCF_ALLCLASSES = 0x00000004,   //Return a list of installed devices for all classes. If this flag is set, the ClassGuid parameter is ignored.
      DIGCF_PROFILE = 0x00000008,
      DIGCF_DEVICEINTERFACE = 0x00000010,   //Return devices that expose interfaces of the interface class specified by ClassGuid. If this flag is not set, ClassGuid specifies a setup class.
    }

    #endregion

    #region SetupDiDestroyDeviceInfoList

    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

    #endregion

    #region SetupDiEnumDeviceInterfaces

    /// <summary>
    /// retrieves a context structure for a device interface of a device information set. 
    /// Each call returns information about one device interface. 
    /// The function can be called repeatedly to get information about several interfaces exposed by one or more devices.
    /// </summary>
    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Boolean SetupDiEnumDeviceInterfaces
    (
        IntPtr hDevInfo,
        IntPtr devInfo,
        ref Guid interfaceClassGuid,
        UInt32 memberIndex,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
    );

    #endregion

    #region SetupDiGetDeviceInterfaceDetail

    /// <summary>
    /// function retrieves detailed information about a specified device interface
    /// </summary>
    /// <param name="hDevInfo"></param>
    /// <param name="deviceInterfaceData"></param>
    /// <param name="deviceInterfaceDetailData"></param>
    /// <param name="deviceInterfaceDetailDataSize"></param>
    /// <param name="requiredSize"></param>
    /// <param name="deviceInfoData"></param>
    /// <returns></returns>
    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Boolean SetupDiGetDeviceInterfaceDetail
    (
       IntPtr hDevInfo,
       ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
       ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
       UInt32 deviceInterfaceDetailDataSize,
       out UInt32 requiredSize,
       ref SP_DEVINFO_DATA deviceInfoData
    );

    #endregion

    #region SetupDiGetDeviceInstanceId

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SetupDiGetDeviceInstanceId
    (
       IntPtr DeviceInfoSet,
       ref SP_DEVINFO_DATA DeviceInfoData,
       StringBuilder DeviceInstanceId,
       int DeviceInstanceIdSize,
       out int RequiredSize
    );

    #endregion

    #region Device Structs and Enums

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
      public UInt32 cbSize;
      public Guid ClassGuid;
      public UInt32 DevInst;
      public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
      public UInt32 cbSize;
      public Guid InterfaceClassGuid;
      public DeviceInterfaceDataFlags Flags;
      //public UInt32 Reserved;
      public UIntPtr Reserved;
    }

    // Device interface detail data
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
      public UInt32 cbSize;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BUFFER_SIZE)]
      public string DevicePath;
    }

    public enum DeviceInterfaceDataFlags : uint
    {
      SPINT_ACTIVE = 0x00000001,    //The device is active.
      SPINT_DEFAULT = 0x00000002,   //The device is the default device.
      SPINT_REMOVED = 0x00000004    //The device has been removed. 
    }

    #endregion

    #region CreateFile

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern SafeFileHandle CreateFile
    (
       string fileName,
       [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
       [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
       IntPtr securityAttributes,
       [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
       [MarshalAs(UnmanagedType.U4)] FileAttributes flags,
       //TODO: [MarshalAs(UnmanagedType.U4)] FileOptions flags,
       IntPtr template
    );

    [Flags]
    public enum EFileAttributes : uint
    {
      Readonly = 0x00000001,
      Hidden = 0x00000002,
      System = 0x00000004,
      Directory = 0x00000010,
      Archive = 0x00000020,
      Device = 0x00000040,
      Normal = 0x00000080,
      Temporary = 0x00000100,
      SparseFile = 0x00000200,
      ReparsePoint = 0x00000400,
      Compressed = 0x00000800,
      Offline = 0x00001000,
      NotContentIndexed = 0x00002000,
      Encrypted = 0x00004000,
      Write_Through = 0x80000000,
      Overlapped = 0x40000000,
      NoBuffering = 0x20000000,
      RandomAccess = 0x10000000,
      SequentialScan = 0x08000000,
      DeleteOnClose = 0x04000000,
      BackupSemantics = 0x02000000,
      PosixSemantics = 0x01000000,
      OpenReparsePoint = 0x00200000,
      OpenNoRecall = 0x00100000,
      FirstPipeInstance = 0x00080000
    }

    #endregion

    #region WriteFile

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern unsafe bool WriteFile
    (
        SafeHandle handle,
        IntPtr buffer,
        int numBytesToWrite,
        IntPtr numBytesWritten,
        Overlapped lpOverlapped
    );

    [DllImport("kernel32", SetLastError = true)]
    internal static extern bool ReadFile(
     SafeFileHandle hFile,
     Byte[] aBuffer,
     UInt32 cbToRead,
     ref UInt32 cbThatWereRead,
     IntPtr pOverlapped);

    public static uint Read(SafeFileHandle handle, byte[] buffer, uint cbToRead)
    {
      // returns bytes read
      uint cbThatWereRead = 0;
      if (!ReadFile(handle, buffer, cbToRead, ref cbThatWereRead, IntPtr.Zero))
        //ThrowLastWin32Err();
        return uint.MaxValue;
      return cbThatWereRead;
    }

    #endregion

    #region CloseHandle

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle
        (
            SafeHandle hObject
        );

    #endregion

    #region DeviceIOControl

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        ref uint lpBytesReturned,
        IntPtr lpOverlapped
    );

    #endregion
  }
}
