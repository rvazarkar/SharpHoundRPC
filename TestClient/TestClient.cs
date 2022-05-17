using System;
using System.Linq;
using SharpHoundRPC;
using SharpHoundRPC.LSANative;
using SharpHoundRPC.Wrappers;

namespace TestClient
{
    internal static class TestClient
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: TestClient.exe <ComputerName> <SAM|LSA>");
                return;
            }
            var computerName = args[0];
            var method = args.Length > 1 ? args[1] : "SAM";
            Console.WriteLine($"Opening Computer: {computerName}");
            
            if (method == "LSA")
            {
                try
                {
                    var lsaServer = LSAPolicy.OpenPolicy(computerName);
                    var machineSid = lsaServer.GetLocalDomainInformation();
                    Console.WriteLine("=====================================================");
                    Console.WriteLine($"Machine Sid: {machineSid.Sid}. Domain Name: {machineSid.Name}");
                    Console.WriteLine("----------------------------------------------------");

                    foreach (var privilege in UserRights.AllPrivileges)
                    {
                        Console.WriteLine("----------------------------------------------------");
                        Console.WriteLine($"Querying privilege {privilege}");
                        var users = lsaServer.GetPrincipalsWithPrivilege(privilege).ToArray();
                        foreach (var user in lsaServer.LookupSids(users))
                        {
                            Console.WriteLine($"Found grant for User {user.Domain}\\{user.Name} ({user.Sid} - {user.Use})");
                        }
                    }
                }
                catch (RPCException e)
                {
                    Console.WriteLine(e.ToString());
                }
            }else if (method == "SAM")
            {
                try
                {
                    var samServer = SAMServer.OpenServer(computerName);
                    var machineSid = samServer.GetMachineSid();
                    Console.WriteLine("=====================================================");
                    Console.WriteLine($"Machine Sid: {machineSid}");
                    Console.WriteLine("----------------------------------------------------");

                    foreach (var domain in samServer.GetDomains())
                    {
                        Console.WriteLine($"Opening Domain {domain.Name} with RID {domain.Rid}");
                        var domainH = samServer.OpenDomain(domain.Name);
                        foreach (var group in domainH.GetAliases())
                        {
                            Console.WriteLine("----------------------------------------------------");
                            Console.WriteLine($"Opening group {group.Name} with RID {group.Rid}");
                            var groupH = domainH.OpenAlias(group.Rid);
                            foreach (var member in groupH.GetMembers())
                            {
                                Console.WriteLine($"Found member {member.Value}");
                            }

                            Console.WriteLine("----------------------------------------------------");
                        }
                    }
                }
                catch (RPCException e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            else
            {
                Console.WriteLine("Invalid method. Must be LSA or SAM");
            }
        }
    }
}