using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

[DisallowMultipleComponent]
public class CycloneX10 : MonoBehaviour
{
	[SerializeField]
    [Range(0, 9)]
    private int _pattern = 0;

    public int pattern
    {
        get { return _pattern; }
        set
        {
            if (value < 0 || value > 9)
            {
                throw new InvalidOperationException();
            }

            _pattern = value;
        }
    }


    [SerializeField]
    [Range(0, 10)]
    private int _level = 0;

    public int level
    {
        get { return _level; }
        set
        {
            if (value < 0 || value > 10)
            {
                throw new InvalidOperationException();
            }

            _level = value;
        }
    }

    private int oldPattern = 0;
    private int oldLevel = 0;

#if UNITY_STANDALONE_WIN
	private SafeFileHandle fileHandle;

    [StructLayout(LayoutKind.Sequential)]
    internal struct HIDD_ATTRIBUTES
    {
        public int Size;
        public ushort VendorID;
        public ushort ProductID;
        public short VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVICE_INTERFACE_DATA
    {
        public Int32 cbSize;
        public Guid interfaceClassGuid;
        public Int32 flags;
        public UIntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
    struct DEVICE_INTERFACE_DETAIL_DATA
    {
        public int cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DevicePath;
    }

    [DllImport("hid.dll", SetLastError = true)]
    static extern void HidD_GetHidGuid(out Guid guid);

    [DllImport("hid.dll", SetLastError = true)]
    static extern void HidD_GetAttributes(SafeFileHandle hidDeviceObject, out HIDD_ATTRIBUTES attributes);

    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, [MarshalAs(UnmanagedType.LPTStr)] string enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref DEVICE_INTERFACE_DATA deviceInterfaceData, ref DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, ref uint requiredSize, IntPtr deviceInfoData);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern int WriteFile(SafeFileHandle file, byte[] buffer, uint numberBytesToWrite, out uint numberOfBytesWritten, IntPtr overlapped);

    // Use this for initialization
    void Start()
    {
        Guid hidGuid;
        HidD_GetHidGuid(out hidGuid);

        IntPtr deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, 22);
        try
        {
            DEVICE_INTERFACE_DATA did = new DEVICE_INTERFACE_DATA();
            did.cbSize = Marshal.SizeOf(did);

            for (int i = 0; ; i++)
            {
                if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, (uint)i, ref did))
                {
                    if (Marshal.GetLastWin32Error() != 259)
                    {
                    }

                    break;
                }

                DEVICE_INTERFACE_DETAIL_DATA didd = new DEVICE_INTERFACE_DETAIL_DATA();
                didd.cbSize = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8;

                uint requiredSize = 0;
                SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref did, ref didd, (uint)Marshal.SizeOf(didd), ref requiredSize, IntPtr.Zero);

                using (SafeFileHandle fileHandle = CreateFile(didd.DevicePath, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
                {
                    HIDD_ATTRIBUTES attributes;
                    HidD_GetAttributes(fileHandle, out attributes);

                    if (attributes.VendorID == 0x0483 && attributes.ProductID == 0x5750)
                    {
                        Debug.Log("Cyclone X10 has detected on \"" + didd.DevicePath + "\".");

                        this.fileHandle = CreateFile(didd.DevicePath, 0xc0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                        break;
                    }
                }
            }
        }
        finally
        {
            if (deviceInfoSet.ToInt64() != -1)
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }
    }
#endif

#if UNITY_STANDALONE_OSX
	[DllImport ("CycloneX10Plugin")]
	private static extern void InitializePlugin ();

	[DllImport ("CycloneX10Plugin")]
	public static extern void DeinitializePlugin ();

	[DllImport ("CycloneX10Plugin")]
	private static extern void VibrateInternal (int index, int pattern, int level);

	private static bool isInitialized = false;
#endif

#if UNITY_STANDALONE_OSX
	void Start()
	{
		if (isInitialized) {
			return;
		}

		isInitialized = true;

		InitializePlugin ();
	}
#endif

    // Update is called once per frame
    void Update()
    {
        if (oldPattern != _pattern || oldLevel != _level)
        {
            SetPatternAndLevel(pattern, level);

            oldPattern = _pattern;
            oldLevel = _level;
        }
    }

    void OnDestroy()
    {
        SetPatternAndLevel(0, 0);

#if UNITY_STANDALONE_OSX
		DeinitializePlugin();
#endif
	}

    private void SetPatternAndLevel(int pattern, int level)
    {
#if UNITY_STANDALONE_WIN
        byte[] buffer = new byte[] { 0x00, 0x3C, 0x30, 0x31, 0x35, 0x32, 0x30, (byte)(0x30 + pattern), (byte)(0x30 + level), 0x30, 0x30, 0x01, 0x02, 0x03, 0x68, 0x3E };

        uint numberOfBytesWritten;
        WriteFile(fileHandle, buffer, (uint)buffer.Length, out numberOfBytesWritten, IntPtr.Zero);
#endif

#if UNITY_STANDALONE_OSX
		VibrateInternal(-1, pattern, level);
#endif
    }

}
