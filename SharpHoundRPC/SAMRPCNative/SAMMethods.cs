using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using SharpHoundRPC.Handles;
using SharpHoundRPC.Wrappers;

namespace SharpHoundRPC.SAMRPCNative
{
    public static class SAMMethods
    {
        internal static SAMHandle SamConnect(string serverName, SAMEnums.SamAccessMasks requestedConnectAccess)
        {
            var us = new SAMStructs.SAMUnicodeString(serverName);
            var objectAttributes = default(SAMStructs.SAMObjectAttributes);

            var status = SamConnect(ref us, out var handle, requestedConnectAccess, ref objectAttributes);

            status.CheckError(RPCException.Connect);
            objectAttributes.Dispose();

            return handle;
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamConnect(
            ref SAMStructs.SAMUnicodeString serverName,
            out SAMHandle serverHandle,
            SAMEnums.SamAccessMasks desiredAccess,
            ref SAMStructs.SAMObjectAttributes objectAttributes
        );

        internal static IEnumerable<SAMStructs.SamRidEnumeration> SamEnumerateDomainsInSamServer(SAMHandle serverHandle)
        {
            var enumerationContext = 0;
            var status =
                SamEnumerateDomainsInSamServer(serverHandle, ref enumerationContext, out var domains, -1,
                    out var count);

            status.CheckError(RPCException.EnumerateDomains);

            return domains.GetEnumerable<SAMStructs.SamRidEnumeration>(count);
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamEnumerateDomainsInSamServer(
            SAMHandle serverHandle,
            ref int enumerationContext,
            out SAMPointer buffer,
            int prefMaxLen,
            out int count
        );

        internal static SecurityIdentifier SamLookupDomainInSamServer(SAMHandle serverHandle, string name)
        {
            var us = new SAMStructs.SAMUnicodeString(name);
            var status = SamLookupDomainInSamServer(serverHandle, ref us, out var sid);

            if (status == NtStatus.StatusNoSuchDomain)
                throw new RPCException(RPCException.LookupDomain, RPCException.DomainNotFound);
            status.CheckError(RPCException.LookupDomain);

            return sid.GetData<SecurityIdentifier>();
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamLookupDomainInSamServer(
            SAMHandle serverHandle,
            ref SAMStructs.SAMUnicodeString name,
            out SAMPointer sid);

        internal static SAMDomain SamOpenDomain(SAMHandle serverHandle, SecurityIdentifier securityIdentifier,
            SAMEnums.DomainAccessMask desiredAccess)
        {
            var bytes = new byte[securityIdentifier.BinaryLength];
            securityIdentifier.GetBinaryForm(bytes, 0);

            var status = SamOpenDomain(serverHandle, desiredAccess, bytes, out var handle);

            if (status == NtStatus.StatusNoSuchDomain)
                throw new RPCException(RPCException.OpenDomain, RPCException.DomainNotFound);

            status.CheckError(RPCException.OpenDomain);

            return new SAMDomain(handle);
        }


        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamOpenDomain(
            SAMHandle serverHandle,
            SAMEnums.DomainAccessMask desiredAccess,
            [MarshalAs(UnmanagedType.LPArray)] byte[] domainSid,
            out SAMHandle domainHandle
        );

        internal static IEnumerable<SecurityIdentifier> SamGetMembersInAlias(SAMHandle aliasHandle)
        {
            var status = SamGetMembersInAlias(aliasHandle, out var members, out var count);
            status.CheckError(RPCException.GetAliasMembers);

            return members.GetData(count);
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamGetMembersInAlias(
            SAMHandle aliasHandle,
            out SAMSidArray members,
            out int count
        );

        internal static SAMAlias SamOpenAlias(SAMHandle domainHandle, int aliasId,
            SAMEnums.AliasOpenFlags desiredAccess = SAMEnums.AliasOpenFlags.ListMembers)
        {
            var status = SamOpenAlias(domainHandle, desiredAccess, aliasId, out var aliasHandle);
            if (status == NtStatus.StatusNoSuchAlias)
                throw new RPCException(RPCException.OpenAlias, RPCException.AliasNotFound);

            status.CheckError(RPCException.OpenAlias);

            return new SAMAlias(aliasHandle);
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamOpenAlias(
            SAMHandle domainHandle,
            SAMEnums.AliasOpenFlags desiredAccess,
            int aliasId,
            out SAMHandle aliasHandle
        );

        internal static SAMStructs.SamRidEnumeration[] SamEnumerateAliasesInDomain(SAMHandle domainHandle)
        {
            var enumerationContext = 0;
            var status =
                SamEnumerateAliasesInDomain(domainHandle, ref enumerationContext, out var ridPointer, -1,
                    out var count);

            status.CheckError(RPCException.EnumerateAliases);

            return ridPointer.GetEnumerable<SAMStructs.SamRidEnumeration>(count).ToArray();
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamEnumerateAliasesInDomain(
            SAMHandle domainHandle,
            ref int enumerationContext,
            out SAMPointer buffer,
            int prefMaxLen,
            out int count
        );

        internal static (string Name, SAMEnums.SidNameUse Type) SamLookupIdsInDomain(SAMHandle domainHandle, int rid)
        {
            var ridArray = new[] {rid};
            var status = SamLookupIdsInDomain(domainHandle, 1, ridArray, out var namePointer, out var usePointer);

            status.CheckError(RPCException.LookupIds);

            return (namePointer.GetData<SAMStructs.SAMUnicodeString>().ToString(),
                (SAMEnums.SidNameUse) usePointer.GetData<int>());
        }

        internal static void SamLookupIdsInDomain(SAMHandle domainHandle, int[] rids, out string[] names,
            out SAMEnums.SidNameUse[] types)
        {
            var count = rids.Length;
            var status = SamLookupIdsInDomain(domainHandle, count, rids, out var namePointer, out var usePointer);

            status.CheckError(RPCException.LookupIds);

            names = namePointer.GetEnumerable<SAMStructs.SAMUnicodeString>(count).Select(x => x.ToString()).ToArray();
            types = new SAMEnums.SidNameUse[count];

            Marshal.Copy(usePointer.DangerousGetHandle(), (int[]) (object) types, 0, count);
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NtStatus SamLookupIdsInDomain(SAMHandle domainHandle,
            int count,
            int[] rids,
            out SAMPointer names,
            out SAMPointer use);

        #region Cleanup

        [DllImport("samlib.dll")]
        internal static extern NtStatus SamFreeMemory(
            IntPtr handle
        );

        [DllImport("samlib.dll")]
        internal static extern NtStatus SamCloseHandle(
            IntPtr handle
        );

        #endregion
    }
}