# About

TODO

**NB** For doing the same with terraform see the [rgl/terraform-rke-vsphere-cloud-provider-example](https://github.com/rgl/terraform-rke-vsphere-cloud-provider-example) repository.

## Usage (Ubuntu 20.04)

[Install the dotnet 5 SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu):

```bash
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >/etc/profile.d/opt-out-dotnet-cli-telemetry.sh
source /etc/profile.d/opt-out-dotnet-cli-telemetry.sh
wget -qO packages-microsoft-prod.deb "https://packages.microsoft.com/config/ubuntu/$(lsb_release -s -r)/packages-microsoft-prod.deb"
dpkg -i packages-microsoft-prod.deb
apt-get install -y apt-transport-https
apt-get update
apt-get install -y dotnet-sdk-5.0
```

[Install Pulumi](https://www.pulumi.com/docs/get-started/install/):

```bash
wget https://get.pulumi.com/releases/sdk/pulumi-v3.10.1-linux-x64.tar.gz
sudo tar xf pulumi-v3.10.1-linux-x64.tar.gz -C /usr/local/bin --strip-components 1
rm pulumi-v3.10.1-linux-x64.tar.gz
```

Configure the stack:

```bash
cat >secrets.sh <<'EOF'
export PULUMI_SKIP_UPDATE_CHECK=true
export PULUMI_CONFIG_PASSPHRASE='password'
export PULUMI_BACKEND_URL="file://$PWD" # NB pulumi will create the .pulumi sub-directory.
export VSPHERE_USER='administrator@vsphere.local'
export VSPHERE_PASSWORD='password'
export VSPHERE_SERVER='vsphere.local'
export VSPHERE_DATACENTER='Datacenter'
export VSPHERE_COMPUTE_CLUSTER='Cluster'
export VSPHERE_DATASTORE='Datastore'
export VSPHERE_NETWORK='VM Network'
export PREFIX='rke_example'
export VSPHERE_FOLDER="examples/$PREFIX"
export VSPHERE_UBUNTU_TEMPLATE='vagrant-templates/ubuntu-20.04-amd64-vsphere'
export CONTROLLER_COUNT='1'
export WORKER_COUNT='1'
export GOVC_INSECURE='1'
export GOVC_URL="https://$VSPHERE_SERVER/sdk"
export GOVC_USERNAME="$VSPHERE_USER"
export GOVC_PASSWORD="$VSPHERE_PASSWORD"
EOF
```

Launch this example:

```bash
source secrets.sh
# see https://github.com/vmware/govmomi/blob/master/govc/USAGE.md
govc version
govc about
govc datacenter.info # list datacenters
govc find # find all managed objects
pulumi login
pulumi whoami -v
pulumi up
```

Destroy everything:

```bash
pulumi destroy
```

## References

* https://www.pulumi.com/docs/intro/cloud-providers/vsphere/
* https://www.pulumi.com/docs/reference/pkg/vsphere/
* https://www.pulumi.com/docs/intro/cloud-providers/rke/
* https://www.pulumi.com/docs/intro/cloud-providers/kubernetes/
* https://github.com/pulumi/pulumi-vsphere
* https://github.com/pulumi/examples/
