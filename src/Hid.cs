using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AlienKeyboardLights
{
    // Minimal Windows HID / SetupAPI interop shared by the AlienFX drivers.
    static class Hid
    {
        public static List<string> EnumeratePaths(string vidPidFilter)
        {
            var result = new List<string>();
            Guid g; HidD_GetHidGuid(out g);
            IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12);
            try
            {
                var did = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA)) };
                for (uint i = 0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref did); i++)
                {
                    uint size;
                    SetupDiGetDeviceInterfaceDetail(set, ref did, IntPtr.Zero, 0, out size, IntPtr.Zero);
                    IntPtr detail = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (SetupDiGetDeviceInterfaceDetail(set, ref did, detail, size, out size, IntPtr.Zero))
                        {
                            string p = Marshal.PtrToStringUni(detail + 4);
                            if (p.IndexOf(vidPidFilter, StringComparison.OrdinalIgnoreCase) >= 0) result.Add(p);
                        }
                    }
                    finally { Marshal.FreeHGlobal(detail); }
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
            return result;
        }

        // Opens a HID collection and returns its report sizes (including the report id byte), or null.
        public static SafeFileHandle OpenDevice(string path, ushort vid, ushort pid, out int outputLength, out int featureLength)
        {
            outputLength = featureLength = 0;
            var h = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h.IsInvalid) { h.Dispose(); return null; }
            var attr = new HIDD_ATTRIBUTES { Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES)) };
            IntPtr prep;
            if (HidD_GetAttributes(h, ref attr) && attr.VendorID == vid && attr.ProductID == pid && HidD_GetPreparsedData(h, out prep))
            {
                HIDP_CAPS caps;
                HidP_GetCaps(prep, out caps);
                HidD_FreePreparsedData(prep);
                outputLength = caps.OutputReportByteLength;
                featureLength = caps.FeatureReportByteLength;
                return h;
            }
            h.Dispose();
            return null;
        }

        [StructLayout(LayoutKind.Sequential)] struct HIDD_ATTRIBUTES { public int Size; public ushort VendorID, ProductID, VersionNumber; }
        [StructLayout(LayoutKind.Sequential)] struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }
        [StructLayout(LayoutKind.Sequential)] struct HIDP_CAPS
        {
            public ushort Usage, UsagePage, InputReportByteLength, OutputReportByteLength, FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes, NumberInputButtonCaps, NumberInputValueCaps, NumberInputDataIndices,
                NumberOutputButtonCaps, NumberOutputValueCaps, NumberOutputDataIndices, NumberFeatureButtonCaps, NumberFeatureValueCaps, NumberFeatureDataIndices;
        }

        [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
        [DllImport("hid.dll")] static extern bool HidD_GetAttributes(SafeFileHandle h, ref HIDD_ATTRIBUTES a);
        [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
        [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr p);
        [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr p, out HIDP_CAPS c);
        [DllImport("hid.dll")] public static extern bool HidD_SetFeature(SafeFileHandle h, byte[] b, int len);
        [DllImport("hid.dll")] public static extern bool HidD_GetFeature(SafeFileHandle h, byte[] b, int len);
        [DllImport("hid.dll")] public static extern bool HidD_SetOutputReport(SafeFileHandle h, byte[] b, int len);
        [DllImport("hid.dll")] public static extern bool HidD_GetInputReport(SafeFileHandle h, byte[] b, int len);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
        [DllImport("setupapi.dll")] static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr hwnd, uint flags);
        [DllImport("setupapi.dll")] static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, uint i, ref SP_DEVICE_INTERFACE_DATA did);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_DEVICE_INTERFACE_DATA did, IntPtr detail, uint size, out uint req, IntPtr dev);
        [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
    }
}
