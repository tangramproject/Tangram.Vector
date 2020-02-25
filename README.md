# Running a node

## Minimum hardware requirements

CPU —Single Core (1-Thread)
RAM — 1GB RAM DDR4
OS — Any Linux distro

NB - Tangram Vector is a live system, and the hardware requirements may change.


## Building Vector
git clone https://github.com/tangramproject/Tangram.Vector

```
cd VectorContainers  
dotnet restore VectorContainers.sln
dotnet publish TGMGateway --output TGMGateway/publish --configuration Release  
dotnet publish Coin.API --output Coin.API/publish --configuration Release
dotnet publish MessagePool.API --output MessagePool.API/publish --configuration Release
dotnet publish Onion.API --output Onion.API/publish --configuration Release
dotnet publish Membership.API --output Membership.API/publish --configuration Release
```

## After you build

```
Run TGMGateway on port 8080
Run Onion.API default port
Check to see if the Onion folder has been created   
Shutdown the app once Onion is created by editing appsettings.jon and updating the settings to "onion_enabled": 0
Run Onion.API default port
Edit Membership.API as the following:

{
 "Logging": {
  "LogLevel": {
   "Default": "Debug"
  }
 },
 "AllowedHosts": "*",
 "onion": {
  "onion_service_address": "http://localhost:3001/",
  "onion_socks_host": "127.0.0.1",
  "onion_socks_port": 9050,
  "onion_control_host": "127.0.0.1",
  "onion_control_port": 9051
 },
 "Membership": {
  "PublicHost": "xx.xx.xxx.xx" // IP address //,
  "PublicPort": "8080",
  "BootstrapNodes": [
   "http://192.241.155.66:8080",
   "http://157.245.94.117:8080"
  ]
 }
}

Run Membership.API default port

```
## MessagePool.API Setup
```
At this point you should have installed RavenDB: https://ravendb.net/download

Edit MessagePool.API appsettings.json    "Database": {
    "Url": "http://localhost:32775"
  }

Run MessagePool.API default port 

```

## Coin.API Setup

```

Edit Coin.API appsettings.json    "Database": {
    "Url": "http://localhost:32775"
  }
 
Run Coin.API default port

```

## What is Vector (Test-net1 release)?
Vector is a testnet network for the team and community to first and foremost find critical bugs, run tests and experiments in a Tangram-like environment. If you’re apart of this release, you’re apart of a huge milestone and something very special to the team and community members.

Vector test-net1 will be used as an early proving ground for things like consensus (messages, states and broadcasting), gossip protocol (dissemination of members - membership).

Have fun on Vector and learn more about Tangram! Explore the open-source code, setup your node, and explore the features. The objective here is to get to a point where Vector will mimic the eventual live environment of Tangram network. Things may get messy, this is expected, this will progressively get better over-time. From installation on the user’s end to protocol enhancements and feature add-ons to Vector.

**Security warning**: Vector is the first release with consensus and then some and should be treated as an experiment! There are no guarantees and expect major flaws.

## Who can participate in Vector?
If you wish to run a node, experiment and support the release by finding bugs or even getting yourself accustomed to the intricacies of what Tangram is about, this is release is for you! This is the perfect time to start getting to know Tangram and the inner mechanics of its technologies and protocols.

If you wish to participate in the release of Vector, you can claim TGM through any of the channels (we recommend Discord, [**here**](https://discord.gg/w4t8hqg).

## Contribution and Support

If you’re thinking about starting a project and need support we thank you for considering. If you have a few questions that need answering or a little more detail than some, feel free to get in touch through any of Tangram’s channels and some community members and managers can point you in the right direction.

If you'd like to contribute to Tangram Vector (Node code), please know we're currently accepting issues, forks, fixes, commits and pull requests so that maintainers can review and merge into the main code base. If you wish to submit more complex changes, please check up with the core devs first on [Discord Channel](https://discord.gg/cZ8NtsY) to ensure the changes get some early feedback which can make both your efforts much lighter as well as review quick and simple.
