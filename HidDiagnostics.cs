using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace BlazeMacro;

// Metadata only: no ReadFile/WriteFile, no GetFeature/SetFeature, no output reports.
public static class HidDiagnostics
{
    public sealed record ReportCapability(string Type, string Kind, int ReportId, string UsagePage, int UsageMin, int UsageMax, int? BitSize, int? Count);
    public sealed class Collection
    {
        public string UsbId { get; set; } = "";
        [System.Text.Json.Serialization.JsonIgnore] public string DevicePath { get; set; } = "";
        public string Interface { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Product { get; set; } = "";
        public string DeviceReleaseBcd { get; set; } = "";
        public string UsagePage { get; set; } = "";
        public string Usage { get; set; } = "";
        public int InputBytes { get; set; }
        public int OutputBytes { get; set; }
        public int FeatureBytes { get; set; }
        public List<ReportCapability> Reports { get; set; } = [];
        public string? Error { get; set; }
    }
    public sealed class Result
    {
        public int Schema { get; set; } = 1;
        public DateTimeOffset CapturedUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Scope { get; set; } = "HID metadata for USB 320F:5084 and 04D9:A09F; no input recording or report transfers; serial numbers omitted.";
        public List<Collection> Collections { get; set; } = [];
        public List<string> Errors { get; set; } = [];
    }

    public static Result Scan()
    {
        var result = new Result();
        if (Marshal.SizeOf<Caps>() != 64) throw new InvalidOperationException("Unexpected HIDP_CAPS layout.");
        HidD_GetHidGuid(out var hidGuid);
        var set = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, 0x12);
        if (set == new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            for (uint index = 0; ; index++)
            {
                var data = new InterfaceData { Size = Marshal.SizeOf<InterfaceData>() };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref hidGuid, index, ref data))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != 259) result.Errors.Add(new Win32Exception(error).Message);
                    break;
                }
                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out uint size, IntPtr.Zero);
                if (size < 6 || size > 65536) { result.Errors.Add("Unexpected interface detail size."); continue; }
                var buffer = Marshal.AllocHGlobal((int)size);
                try
                {
                    Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, buffer, size, out _, IntPtr.Zero)) { result.Errors.Add(new Win32Exception(Marshal.GetLastWin32Error()).Message); continue; }
                    var path = Marshal.PtrToStringUni(buffer + 4) ?? "";
                    if (!Regex.IsMatch(path, @"vid_320f&pid_5084|vid_04d9&pid_a09f", RegexOptions.IgnoreCase)) continue;
                    result.Collections.Add(Inspect(path));
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return result;
    }

    static Collection Inspect(string path)
    {
        var id = Regex.Match(path, @"vid_([0-9a-f]{4})&pid_([0-9a-f]{4})", RegexOptions.IgnoreCase);
        var item = new Collection { UsbId = $"{id.Groups[1].Value}:{id.Groups[2].Value}".ToUpperInvariant(), Interface = string.Join(" / ", Regex.Matches(path, @"mi_[0-9a-f]{2}|col[0-9a-f]{2}", RegexOptions.IgnoreCase).Select(m => m.Value.ToUpperInvariant())) };
        item.DevicePath = path;
        try
        {
            // Desired access 0 intentionally excludes both GENERIC_READ and GENERIC_WRITE.
            using var handle = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            var attributes = new Attributes { Size = Marshal.SizeOf<Attributes>() };
            if (!HidD_GetAttributes(handle, ref attributes)) throw new Win32Exception(Marshal.GetLastWin32Error());
            item.DeviceReleaseBcd = $"0x{attributes.Version:X4}";
            item.Manufacturer = ReadString(handle, HidD_GetManufacturerString);
            item.Product = ReadString(handle, HidD_GetProductString);
            if (!HidD_GetPreparsedData(handle, out var preparsed)) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var status = HidP_GetCaps(preparsed, out var caps);
                if (status != 0x00110000) throw new IOException($"HidP_GetCaps: 0x{status:X8}");
                item.UsagePage = $"0x{caps.UsagePage:X4}"; item.Usage = $"0x{caps.Usage:X4}";
                item.InputBytes = caps.InputBytes; item.OutputBytes = caps.OutputBytes; item.FeatureBytes = caps.FeatureBytes;
                for (int type = 0; type < 3; type++)
                {
                    ReadCapabilities(item, preparsed, type, false, type == 0 ? caps.InputValues : type == 1 ? caps.OutputValues : caps.FeatureValues);
                    ReadCapabilities(item, preparsed, type, true, type == 0 ? caps.InputButtons : type == 1 ? caps.OutputButtons : caps.FeatureButtons);
                }
            }
            finally { HidD_FreePreparsedData(preparsed); }
        }
        catch (Exception e) { item.Error = e.Message; }
        return item;
    }

    delegate bool GetString(SafeFileHandle handle, byte[] data, uint length);
    static string ReadString(SafeFileHandle handle, GetString get)
    {
        var bytes = new byte[256];
        return get(handle, bytes, (uint)bytes.Length) ? Encoding.Unicode.GetString(bytes).TrimEnd('\0') : "";
    }
    static void ReadCapabilities(Collection item, IntPtr preparsed, int type, bool buttons, ushort count)
    {
        if (count == 0) return;
        // Both HIDP_VALUE_CAPS and HIDP_BUTTON_CAPS are 72-byte Windows ABI records.
        var buffer = Marshal.AllocHGlobal(count * 72);
        try
        {
            var capacity = count;
            var status = buttons ? HidP_GetButtonCaps(type, buffer, ref count, preparsed) : HidP_GetValueCaps(type, buffer, ref count, preparsed);
            if (status != 0x00110000 || count > capacity) throw new IOException($"HID capabilities: 0x{status:X8}");
            for (int i = 0; i < count; i++)
            {
                var p = buffer + i * 72;
                int Word(int offset) => (ushort)Marshal.ReadInt16(p, offset);
                var min = Word(56); var max = Marshal.ReadByte(p, 12) != 0 ? Word(58) : min;
                item.Reports.Add(new ReportCapability(new[] { "Input", "Output", "Feature" }[type], buttons ? "Buttons" : "Values", Marshal.ReadByte(p, 2), $"0x{Word(0):X4}", min, max, buttons ? null : Word(18), buttons ? null : Word(20)));
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    public static string Describe(Result result)
    {
        var text = new StringBuilder("BLAZE — ДИАГНОСТИКА HID (ТОЛЬКО МЕТАДАННЫЕ)\r\n");
        text.AppendLine("Без записи нажатий, передачи Input/Output/Feature reports и изменения настроек.");
        text.AppendLine("Размеры отчётов включают байт Report ID. Наличие vendor-канала не доказывает поддержку RGB.");
        foreach (var c in result.Collections.OrderBy(c => c.UsbId).ThenBy(c => c.Interface))
        {
            text.AppendLine($"\r\n{c.UsbId} | {c.Interface} | {c.Manufacturer} | {c.Product}");
            text.AppendLine($"bcdDevice {c.DeviceReleaseBcd}; Usage {c.UsagePage}:{c.Usage}; Input {c.InputBytes}, Output {c.OutputBytes}, Feature {c.FeatureBytes} байт");
            if (c.Error != null) text.AppendLine("Ошибка: " + c.Error);
            foreach (var r in c.Reports) text.AppendLine($"  {r.Type}: ID 0x{r.ReportId:X2}; {r.Kind}; page {r.UsagePage}; usage {r.UsageMin:X4}–{r.UsageMax:X4}" + (r.Count.HasValue ? $"; {r.Count} × {r.BitSize} бит" : "; диапазон допустимых usages, не длина отчёта"));
        }
        foreach (var error in result.Errors) text.AppendLine("Ошибка перечисления: " + error);
        return text.ToString();
    }
    public static void Save(Result result, string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "HID-diagnostic.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(directory, "HID-diagnostic.txt"), Describe(result));
    }

    [StructLayout(LayoutKind.Sequential)] struct InterfaceData { public int Size; public Guid Guid; public uint Flags; public UIntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)] struct Attributes { public int Size; public ushort Vendor, Product, Version; }
    [StructLayout(LayoutKind.Sequential)] struct Caps
    {
        public ushort Usage, UsagePage, InputBytes, OutputBytes, FeatureBytes;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
        public ushort Links, InputButtons, InputValues, InputIndices, OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
    }
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string? enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint required, IntPtr info);
    [DllImport("setupapi.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("hid.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetAttributes(SafeFileHandle handle, ref Attributes attributes);
    [DllImport("hid.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetProductString(SafeFileHandle handle, byte[] buffer, uint length);
    [DllImport("hid.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetManufacturerString(SafeFileHandle handle, byte[] buffer, uint length);
    [DllImport("hid.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr data, out Caps caps);
    [DllImport("hid.dll")] static extern int HidP_GetValueCaps(int type, IntPtr caps, ref ushort count, IntPtr data);
    [DllImport("hid.dll")] static extern int HidP_GetButtonCaps(int type, IntPtr caps, ref ushort count, IntPtr data);
}
