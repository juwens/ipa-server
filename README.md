# Installation

```
apt-get update && apt-get install -y dotnet-sdk-8.0
apt-get install -y caddy # reverse-proxy
apt-get install -y libgdiplus # mono gdi lib for linux

cat >Caddyfile<<EOF

EOF

dotnet run -c Release
```