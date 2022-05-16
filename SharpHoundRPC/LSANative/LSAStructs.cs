using System;
using System.Runtime.InteropServices;
using SharpHoundRPC.Shared;

namespace SharpHoundRPC.LSANative
{
    public class LSAStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ObjectAttributes
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public int Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;

            public void Dispose()
            {
                if (ObjectName == IntPtr.Zero) return;
                Marshal.DestroyStructure(ObjectName, typeof(SharedStructs.UnicodeString));
                Marshal.FreeHGlobal(ObjectName);
                ObjectName = IntPtr.Zero;
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct PolicyAccountDomainInfo
        {
            public SharedStructs.UnicodeString DomainName;
            public IntPtr DomainSid;
        }
    }
}