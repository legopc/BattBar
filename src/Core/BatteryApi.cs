using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BattBar.Core;

/// <summary>
/// P/Invoke surface for the Windows battery class driver.
/// Calls into setupapi.dll (device enumeration) and kernel32.dll (DeviceIoControl, handles).
/// </summary>
internal static class BatteryApi
{
    // ----- setupapi.dll -----
    internal const uint DIGCF_PRESENT = 0x00000002;
    internal const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        public uint cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DevicePath;
    }

    /// <summary>
    /// Correct cbSize for SP_DEVICE_INTERFACE_DETAIL_DATA: 4 (DWORD) + sizeof first char.
    /// On 64-bit Unicode Windows this is 8. Setting cbSize to sizeof(full struct)
    /// causes SetupDiGetDeviceInterfaceDetail to fail with ERROR_INVALID_USER_BUFFER.
    /// </summary>
    internal static uint DeviceInterfaceDetailDataCbSize => (uint)(4 + IntPtr.Size);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public uint Reserved;
    }

    internal static readonly Guid GuidDeviceBattery = new("72631e54-78a4-11d0-bcf7-00aa00b7b32a");

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid,
        IntPtr Enumerator,
        IntPtr hwndParent,
        uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr DeviceInfoSet,
        IntPtr DeviceInfoData,
        ref Guid InterfaceClassGuid,
        uint MemberIndex,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr DeviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData,
        uint DeviceInterfaceDetailDataSize,
        out uint RequiredSize,
        IntPtr DeviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    // ----- kernel32.dll -----

    internal const uint IOCTL_BATTERY_QUERY_TAG = 0x00294040;
    internal const uint IOCTL_BATTERY_QUERY_INFORMATION = 0x00294044;
    internal const uint IOCTL_BATTERY_QUERY_STATUS = 0x00294048;

    internal const uint BatteryTagInvalid = 0;
    internal const uint BATTERY_UNKNOWN_CAPACITY = 0xFFFFFFFF;

    internal enum BatteryInformationLevel : uint
    {
        BatteryInformation = 0,
        BatteryGranularityInformation = 1,
        BatteryTemperature = 2,
        BatteryEstimatedTime = 3,
        BatteryDeviceName = 4,
        BatteryManufactureDate = 5,
        BatteryManufactureName = 6,
        BatteryUniqueID = 7,
        BatteryStateName = 8,
        BatteryFullChargedCapacity = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_QUERY_INFORMATION
    {
        public uint BatteryTag;
        public uint InformationLevel;
        public uint AtRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_INFORMATION
    {
        public uint Capabilities;
        public uint Technology;
        public uint DesignedCapacity;
        public uint FullChargedCapacity;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_STATUS
    {
        public uint PowerState;
        public uint Capacity;
        public uint Rate;
        public uint Voltage;
        public uint Temperature;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_WAIT_STATUS
    {
        public uint BatteryTag;
        public uint Timeout;
        public uint PowerState;
        public uint LowCapacity;
        public uint HighCapacity;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);
}