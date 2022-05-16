using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using SharpHoundRPC.Handles;
using SharpHoundRPC.Shared;

namespace SharpHoundRPC.LSANative
{
    [SuppressUnmanagedCodeSecurity]
    public class LSAMethods
    {
        internal static LSAHandle LsaOpenPolicy(string computerName, LSAEnums.LsaOpenMask desiredAccess)
        {
            var us = new SharedStructs.UnicodeString(computerName);
            var objectAttributes = default(LSAStructs.ObjectAttributes);
            var status = LsaOpenPolicy(ref us, ref objectAttributes, desiredAccess, out var handle);
            status.CheckError("LsaOpenPolicy");

            return handle;
        }
        
        [DllImport("advapi32.dll")]
        private static extern NtStatus LsaOpenPolicy(
            ref SharedStructs.UnicodeString server,
            ref LSAStructs.ObjectAttributes objectAttributes,
            LSAEnums.LsaOpenMask desiredAccess,
            out LSAHandle policyHandle
        );
        [DllImport("advapi32.dll")]
        internal static extern NtStatus LsaClose(
            IntPtr buffer
        );

        [DllImport("advapi32.dll")]
        internal static extern NtStatus LsaFreeMemory(
            IntPtr buffer
        );

        internal static LSAPointer LsaQueryInformationPolicy(LSAHandle policyHandle,
            LSAEnums.LSAPolicyInformation policyInformation)
        {
            var status = LsaQueryInformationPolicy(policyHandle, policyInformation, out var pointer);
            status.CheckError("LSAQueryInformationPolicy");

            return pointer;
        }
        
        [DllImport("advapi32.dll")]
        private static extern NtStatus LsaQueryInformationPolicy(
            LSAHandle policyHandle, 
            LSAEnums.LSAPolicyInformation policyInformation, 
            out LSAPointer buffer
        );

        internal static IEnumerable<SecurityIdentifier> LsaEnumerateAccountsWithUserRight(LSAHandle policyHandle,
            string userRight)
        {
            var us = new SharedStructs.UnicodeString(userRight);
            var status = LsaEnumerateAccountsWithUserRight(policyHandle, us, out var sids, out var count);
            status.CheckError("LsaEnumerateAccountsWithUserRight");

            for (var i = 0; i < count; i++)
            {
                var ptr = Marshal.ReadIntPtr(sids, i * Marshal.SizeOf<IntPtr>());
                yield return new SecurityIdentifier(ptr);
            }
        }
        
        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern NtStatus LsaEnumerateAccountsWithUserRight(
            LSAHandle policyHandle,
            SharedStructs.UnicodeString userRight,
            out LSAPointer sids,
            out int count
        );
    }
}