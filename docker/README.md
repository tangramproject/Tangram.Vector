# Docker containers

This document describes the steps needed to run a Tangram Vector node as a set of containers.

Prerequisites:

* [`docker` >= 18.09](https://docs.docker.com/install/)
* [`docker-compose` >= 3.4](https://docs.docker.com/compose/install/).

## Preliminary configuration

First, we need a folder to act as a volume for storing our configuration files and state between runs. For the purposes of this example, I will use `/var/tmp/tangramvector`. You may prefer to choose a more appropriate, less volatile location. This folder will be referred to later using the `APPDATA` environment variable. Copy the example configurations from the `example` subfolder into place.

```bash
export APPDATA=/var/tmp/tangramvector
mkdir -p $APPDATA
cp -a example $APPDATA/Tangram
```

# Hostnames/IPs

The `appsettings.json` files currently built into the containers are as per the defaults found here in git, which were designed for developers working locally, and will use `127.0.0.1` and `localhost` in various places. As each component runs in it's own container, custom configurations will need to be supplied for each component to ensure they can communicate with each other as expected.

## `docker-compose`

In a `docker-compose`-based setup, peer containers will be accessible via the IP address of the docker virtual NIC address (which usually has the IP address of `172.17.0.1`).

The instructions on this page assume you're creating a `docker-compose`-based environment.

## Other orchestrators

When working with other container orchestration tools (i.e. Kubernetes), these peer settings will need to be replaced with the relevant hostnames. Other orchestrators may also have other ways of managing and provisioning configuration files for the containers.

## Running up the node

To run the stack:

```bash
docker-compose up -d
```

This will build the containers if they have not already been built, and run then run them in the background.

Check that all containers are running as expected:

```
docker ps -a
```

## Troubleshooting

Check the ravendb is up, has been initialised and is accepting connections by connecting to `http://172.17.0.1:32775`.
