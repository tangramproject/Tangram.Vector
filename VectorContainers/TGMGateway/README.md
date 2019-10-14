# TGMGateway as Daemon 
## systemd - applies to Ubuntu 16.04
Open bash terminal, and create file */etc/systemd/system/tgmgateway.service*, using super user permissions, containing:

```
[Unit]
Description=Tangram gateway service
After=network.target
[Service]
WorkingDirectory={Working Directory}
User={User}
Group={User}
ExecStart=/usr/bin/dotnet {Working Directory}/TGMGateway.dll --environment Production
Restart=always
RestartSec=10
SyslogIdentifier=TGMGateway
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
[Install]
WantedBy=multi-user.target
```
> Replace "User={User}"

> Replace "WorkingDirectory={Working Directory}"

> Replace "ExecStart=/usr/bin/dotnet **{Working Directory}**/TGMGateway.dll --environment Production"

### Then register the service and enable it on startup by typing:
```
systemctl daemon-reload
systemctl enable tgmgateway.service
```

### Start the service:
```
systemctl start TGMGateway
```

### Stop the service:
```
systemctl stop TGMGateway
```

### View its status using:
```
systemctl status TGMGateway
or
journalctl -f -u TGMGateway
```
***