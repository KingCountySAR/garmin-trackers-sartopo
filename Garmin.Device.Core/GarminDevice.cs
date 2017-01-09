using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Garmin.Device.Core
{
  public class GarminDevice : IDisposable
  {
    private bool isDisposed = false;
    private readonly uint IOCTL_ASYNC_IN = Utilities.CTL_CODE(UnsafeNativeMethods.FILE_DEVICE_UNKNOWN,
                                0x850,
                                UnsafeNativeMethods.METHOD_BUFFERED,
                                UnsafeNativeMethods.FILE_ANY_ACCESS);

    public void Dispose()
    {
      Dispose(true);
      // Use SupressFinalize in case a subclass
      // of this type implements a finalizer.
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      // If you need thread safety, use a lock around these 
      // operations, as well as in your methods that use the resource.
      if (!isDisposed)
      {
        if (disposing)
        {
          if (handle != null)
          {
            handle.Close();
          //  UnsafeNativeMethods.CloseHandle(handle);
            handle.Dispose();
          }
        }

        // Indicate that the instance has been disposed.
        handle = null;
        isDisposed = true;
      }
    }

    public void GarminWrite(UsbPacket usbPacket)
    {
      //TODO: Place this check ouside this method to make sure that multiple write calls are made if
      //data size is greater than MAX_BUFFER_SIZE

      /*
       * the size of all the data sent to the device is the size of the 
       * usb data structure(3.2.2) plus the size of all the data sent (index 12+ of the structure)
       */
      int theBytesToWrite = Marshal.SizeOf(usbPacket) + usbPacket.dataSize;
      int theBytesReturned = 0;

      // prepare and format the data to be sent
      byte[] dataAsArray = null;

      Utilities.CreateUSBPacketByteArray(usbPacket, ref dataAsArray);

      GCHandle gcDataHandle = GCHandle.Alloc(dataAsArray, GCHandleType.Pinned);
      IntPtr gcDataHandleAddress = gcDataHandle.AddrOfPinnedObject();

      // prepare a destination address for the data to be written
      GCHandle gcReturnDataHandle = GCHandle.Alloc(theBytesReturned, GCHandleType.Pinned);
      IntPtr gcReturnDataHandleAddress = gcReturnDataHandle.AddrOfPinnedObject();

      try
      {
        bool success = UnsafeNativeMethods.WriteFile(
                            Handle,
                            gcDataHandleAddress,
                            theBytesToWrite,
                            gcReturnDataHandleAddress,
                            new Overlapped());

        // format the data written at the addresses in a .net readable structure
        int returnedBytes = (int)Marshal.PtrToStructure(gcReturnDataHandleAddress, typeof(Int32));

        if (success == false || returnedBytes != theBytesToWrite)
        {
          throw new InvalidOperationException("UnableToWriteToDevice");
        }

        // If the packet size was an exact multiple of the USB packet 
        // size, we must make a final write call with no data (3.2.3.1)
        if (theBytesToWrite % PacketSize == 0)
        {
          success = UnsafeNativeMethods.WriteFile(
              Handle,
              IntPtr.Zero,
              0,
              gcReturnDataHandleAddress,
              new Overlapped());
        }

        if (success == false)
        {
          throw new InvalidOperationException("Unable to write to device");
        }
      }
      finally
      {
        gcDataHandleAddress = IntPtr.Zero;
        gcReturnDataHandleAddress = IntPtr.Zero;

        gcDataHandle.Free();
        gcReturnDataHandle.Free();
      }
    }

    public byte[] GarminRead()
    {
      byte[] dataAsArray = new byte[GarminUSBConstants.MAX_BUFFER_SIZE];
      UnsafeNativeMethods.Read(Handle, dataAsArray, GarminUSBConstants.MAX_BUFFER_SIZE);
      return dataAsArray;
    }

    public UsbPacket GarminReadSingleton()
    {
      // Read async data until the driver returns less than the
      // max async data size, which signifies the end of a packet
      byte[] buffer = new byte[GarminUSBConstants.ASYNC_DATA_SIZE];  //TODO: shouldnt this be device.packetSize?
      uint noOfBytesReturned = 0;

      GCHandle tbHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
      IntPtr tpIntPtr = tbHandle.AddrOfPinnedObject();

      try
      {
        UnsafeNativeMethods.DeviceIoControl(
                        Handle,
                        IOCTL_ASYNC_IN,
                        IntPtr.Zero,
                        0,
                        tpIntPtr,
                        GarminUSBConstants.ASYNC_DATA_SIZE,
                        ref noOfBytesReturned,
                        IntPtr.Zero);

        return Utilities.CreateUSBPacketFromByteArray(buffer);
      }
      finally
      {
        tpIntPtr = IntPtr.Zero;
        tbHandle.Free();
      }
    }


    public static List<GarminDevice> DiscoverDevices()
    {
      return GarminDeviceDiscovery.FindDevices();
    }

    internal static GarminDevice FromHandle(SafeFileHandle handle)
    {
      var device = new GarminDevice();
      device.handle = handle;

      device.PacketSize = GetUSBPacketSize(handle);

      return device;
    }

    private static int GetUSBPacketSize(SafeFileHandle handle)
    {
      uint IOCTL_USB_PACKET_SIZE = Utilities.CTL_CODE(
                    UnsafeNativeMethods.FILE_DEVICE_UNKNOWN,
                    0x851,
                    UnsafeNativeMethods.METHOD_BUFFERED,
                    UnsafeNativeMethods.FILE_ANY_ACCESS);

      GCHandle USBPacketSizeHandle = GCHandle.Alloc(new int(), GCHandleType.Pinned);
      IntPtr USBPacketSizeHandleAddress = USBPacketSizeHandle.AddrOfPinnedObject();

      uint requiredSize = 0;
      if (UnsafeNativeMethods.DeviceIoControl(
              handle,
              IOCTL_USB_PACKET_SIZE,
              IntPtr.Zero,
              0,
              USBPacketSizeHandleAddress,
              (uint)Marshal.SizeOf(USBPacketSizeHandleAddress),
              ref requiredSize,
              IntPtr.Zero))
      {
        return Marshal.ReadInt32(USBPacketSizeHandleAddress);
      }
      else
      {
        return (int)GarminUSBConstants.ASYNC_DATA_SIZE;
      }
    }

    /// <summary>
    /// Contains a handle to the file created on the device
    /// </summary>
    public SafeFileHandle Handle
    {
      get
      {
        if ((handle == null) || ((handle.DangerousGetHandle().ToInt32() == 0) && (PacketSize == 0)))
        {
          throw new InvalidOperationException("Garmin Device Cannot Be Found");
        }
        return handle;
      }
      set { handle = value; }
    }
    private SafeFileHandle handle;

    public int PacketSize { get; set; }
  }
}
