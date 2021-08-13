using System;
using System.IO;
using System.Linq;
using Pulumi;
using Pulumi.VSphere;
using Pulumi.VSphere.Inputs;

class Stack : Pulumi.Stack
{
    public Stack()
    {
        var datacenterName = Environment.GetEnvironmentVariable("VSPHERE_DATACENTER");
        var computeClusterName = Environment.GetEnvironmentVariable("VSPHERE_COMPUTE_CLUSTER");
        var datastoreName = Environment.GetEnvironmentVariable("VSPHERE_DATASTORE");
        var networkName = Environment.GetEnvironmentVariable("VSPHERE_NETWORK");
        var prefix = Environment.GetEnvironmentVariable("PREFIX");
        var folderPath = Environment.GetEnvironmentVariable("VSPHERE_FOLDER");
        var ubuntuTemplateName = Environment.GetEnvironmentVariable("VSPHERE_UBUNTU_TEMPLATE");
        var controllerCount = int.Parse(Environment.GetEnvironmentVariable("CONTROLLER_COUNT") ?? "1");
        var workerCount = int.Parse(Environment.GetEnvironmentVariable("WORKER_COUNT") ?? "1");

        var datacenter = Datacenter.Get(
            "datacenter",
            "/" + datacenterName
        );

        // TODO why do we have to use datacenter.Moid instead of datacenter.Id?

        var datastoreId = datacenter.Moid.Apply(
                id =>
                    Output.Create(
                        Pulumi.VSphere.GetDatastore.InvokeAsync(
                            new Pulumi.VSphere.GetDatastoreArgs
                            {
                                DatacenterId = id,
                                Name = datastoreName,
                            }
                        )
                    )
            ).Apply(r => r.Id);

        var networkId = datacenter.Moid.Apply(
                id =>
                    Output.Create(
                        Pulumi.VSphere.GetNetwork.InvokeAsync(
                            new Pulumi.VSphere.GetNetworkArgs
                            {
                                DatacenterId = id,
                                Name = networkName,
                            }
                        )
                    )
            ).Apply(r => r.Id);
        
        var resourcePoolId = datacenter.Moid.Apply(
                id =>
                    Output.Create(
                        Pulumi.VSphere.GetComputeCluster.InvokeAsync(
                            new Pulumi.VSphere.GetComputeClusterArgs
                            {
                                DatacenterId = id,
                                Name = computeClusterName,
                            }
                        )
                    )
            ).Apply(r => r.ResourcePoolId);

        var ubuntuTemplate = datacenter.Moid.Apply(
                id =>
                    Output.Create(
                        Pulumi.VSphere.GetVirtualMachine.InvokeAsync(
                            new Pulumi.VSphere.GetVirtualMachineArgs
                            {
                                DatacenterId = id,
                                Name = ubuntuTemplateName,
                            }
                        )
                    )
            );

        var folder = new Folder(
            Path.GetFileName(folderPath),
            new FolderArgs
            {
                Path = folderPath,
                Type = "vm",
                DatacenterId = datacenter.Moid,
            }
        );

        Func<string, int, VirtualMachine> vm = (name, memory) =>
        {
            return new VirtualMachine(
                name,
                new VirtualMachineArgs
                {
                    Folder = folder.Path,
                    Name = name,
                    GuestId = ubuntuTemplate.Apply(r => r.GuestId),
                    NumCpus = 4,
                    NumCoresPerSocket = 4,
                    Memory = memory,
                    EnableDiskUuid = true,
                    ResourcePoolId = resourcePoolId,
                    DatastoreId = datastoreId,
                    ScsiType = ubuntuTemplate.Apply(r => r.ScsiType),
                    Disks = new InputList<VirtualMachineDiskArgs>
                    {
                        new VirtualMachineDiskArgs
                        {
                            UnitNumber = 0,
                            Label = "os",
                            Size = ubuntuTemplate.Apply(r => Math.Max(r.Disks[0].Size, 15)),
                            EagerlyScrub = ubuntuTemplate.Apply(r => r.Disks[0].EagerlyScrub),
                            ThinProvisioned = ubuntuTemplate.Apply(r => r.Disks[0].ThinProvisioned),
                        }
                    },
                    NetworkInterfaces = new InputList<Pulumi.VSphere.Inputs.VirtualMachineNetworkInterfaceArgs>
                    {
                        new VirtualMachineNetworkInterfaceArgs
                        {
                            NetworkId = networkId,
                        }
                    },
                    Clone = new VirtualMachineCloneArgs
                    {
                        TemplateUuid = ubuntuTemplate.Apply(r => r.Uuid),
                    },
                }
            );
        };

        var controllers = Enumerable.Range(0, controllerCount)
            .Select(index => vm($"{prefix}_c{index}", 4*1024))
            .ToList();

        var workers = Enumerable.Range(0, workerCount)
            .Select(index => vm($"{prefix}_w{index}", 8*1024))
            .ToList();
    }
}