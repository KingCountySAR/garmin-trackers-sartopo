using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Garmin.Device.Core
{
  public static class GarminDeviceDiscovery
  {
    private static Guid garminGuid = new Guid("{2C9C45C2-8E7D-4C08-A12D-816BBAE722C0}");

    public static List<GarminDevice> FindDevices()
    {
      IntPtr handleDeviceInfo = IntPtr.Zero;

      ///Get device information set that contains all devices of a specified class.
      handleDeviceInfo = UnsafeNativeMethods.SetupDiGetClassDevs(
                                        ref garminGuid,
                                        IntPtr.Zero,
                                        IntPtr.Zero,
                                        UnsafeNativeMethods.DiGetClassFlags.DIGCF_PRESENT |
                                        UnsafeNativeMethods.DiGetClassFlags.DIGCF_DEVICEINTERFACE);

      if (handleDeviceInfo.ToInt32() == UnsafeNativeMethods.INVALID_HANDLE_VALUE)
      {
        UnsafeNativeMethods.SetupDiDestroyDeviceInfoList(handleDeviceInfo);
        throw new InvalidOperationException("invalid handle");
      }


      bool success = true;
      uint memberIndex = 0;

      var devices = new List<GarminDevice>();

      while (success)
      {
        // create a Device Interface Data structure
        UnsafeNativeMethods.SP_DEVICE_INTERFACE_DATA deviceInterfaceData =
                             new UnsafeNativeMethods.SP_DEVICE_INTERFACE_DATA();
        deviceInterfaceData.cbSize = (uint)Marshal.SizeOf(deviceInterfaceData);


        // start the enumeration of all devices
        success = UnsafeNativeMethods.SetupDiEnumDeviceInterfaces(
                                        handleDeviceInfo,
                                        IntPtr.Zero,
                                        ref garminGuid,
                                        memberIndex,
                                        ref deviceInterfaceData);

        if (Marshal.GetLastWin32Error() == UnsafeNativeMethods.ERROR_NO_MORE_ITEMS)
        {
          success = false;
          break;
        }

        if (!success)
        {
          break;
        }

        UnsafeNativeMethods.SP_DEVINFO_DATA deviceInfoData = new UnsafeNativeMethods.SP_DEVINFO_DATA();
        deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

        UnsafeNativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData =
                                            new UnsafeNativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA();
        if (IntPtr.Size == 8) // for 64 bit operating systems
        {
          deviceInterfaceDetailData.cbSize = 8;
        }
        else
        {
          deviceInterfaceDetailData.cbSize = 4 + (uint)Marshal.SystemDefaultCharSize; //i.e 5
        }

        // now we can get some more detailed information
        uint nRequiredSize = 0;
        uint nBytes = UnsafeNativeMethods.BUFFER_SIZE;

        if (!UnsafeNativeMethods.SetupDiGetDeviceInterfaceDetail(
                            handleDeviceInfo,
                            ref deviceInterfaceData,
                            ref deviceInterfaceDetailData,
                            nBytes,
                            out nRequiredSize,
                            ref deviceInfoData))
        {
          throw new InvalidOperationException("Error getting device interface detail");
        }
        else
        {
          GarminDevice garminDevice = GarminDevice.FromHandle(GetHandleFromDevicePath(deviceInterfaceDetailData.DevicePath));
          devices.Add(garminDevice);
        }

        memberIndex++;
      }

      return devices;
    }

    /// <summary>
    /// Function returns a handle to a file created on the device
    /// </summary>
    /// <param name="devicePath"></param>
    /// <returns></returns>
    private static SafeFileHandle GetHandleFromDevicePath(string devicePath)
    {
      return UnsafeNativeMethods.CreateFile(
                          devicePath,
                          FileAccess.ReadWrite,
                          FileShare.None,
                          IntPtr.Zero,
                          FileMode.Open,
                          FileAttributes.Normal,
                          IntPtr.Zero);
    }
  }
}
