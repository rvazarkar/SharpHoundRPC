using System;
using System.Runtime.InteropServices;

namespace SharpHoundRPC.SAMRPCNative
{
    public static class SAMStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SAMUnicodeString : IDisposable
        {
            private readonly ushort Length;
            private readonly ushort MaximumLength;
            private IntPtr Buffer;

            public SAMUnicodeString(string s)
                : this()
            {
                if (s == null) return;
                Length = (ushort) (s.Length * 2);
                MaximumLength = (ushort) (Length + 2);
                Buffer = Marshal.StringToHGlobalUni(s);
            }

            public void Dispose()
            {
                if (Buffer == IntPtr.Zero) return;
                Marshal.FreeHGlobal(Buffer);
                Buffer = IntPtr.Zero;
            }

            public override string ToString()
            {
                return (Buffer != IntPtr.Zero ? Marshal.PtrToStringUni(Buffer, Length / 2) : null) ??
                       throw new InvalidOperationException();
            }
        }

        public struct SAMObjectAttributes : IDisposable
        {
            public void Dispose()
            {
                if (objectName == IntPtr.Zero) return;
                Marshal.DestroyStructure(objectName, typeof(SAMUnicodeString));
                Marshal.FreeHGlobal(objectName);
                objectName = IntPtr.Zero;
            }

            public int len;
            public IntPtr rootDirectory;
            public uint attribs;
            public IntPtr sid;
            public IntPtr qos;
            private IntPtr objectName;
            public SAMUnicodeString ObjectName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SamRidEnumeration
        {
            public int Rid;
            public SAMUnicodeString Name;
        }
    }
}