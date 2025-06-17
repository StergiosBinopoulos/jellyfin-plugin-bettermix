# BetterMix

**BetterMix** is a Jellyfin plugin that replaces Jellyfin's built-in *Instant Mix* feature with a smarter and customizable music playlist generator. It works by intercepting Instant Mix API calls through a reverse proxy and routing them to BetterMix’s logic. The plugin uses the [Deej-AI model by teticio](https://github.com/teticio/Deej-AI), and is made possible thanks to their work.


## Limitations

- The plugin will produce mixes only for tracks (Audio). For all other items (albums, genres, etc.), it will fall back to the native Instant Mix functionality.
- Currently, the only supported backend is Deej-AI.
- PyInstaller is used to create standalone executables from the Deej-Ai scripts, eliminating the Python dependency on the Jellyfin server. However the execution time of the scripts is increased as a result.


## Setup

### Requirements
- The Deej-AI model
- Python 3.10 (to build the Deej-AI script executables)
- NET 8.0 (to build the plugin)

### 1. Install the plugin


Clone the Repository

```bash
git clone https://github.com/StergiosBinopoulos/jellyfin-plugin-bettermix
cd jellyfin-plugin-bettermix
```

Download the Deej-AI [model](https://drive.google.com/file/d/1LM1WW1GCGKeFD1AAHS8ijNwahqH4r4xV/view), rename it to **"Deej-AI-model"** and place it inside the `Backend/Deej-AI/` directory

Create a python virtual environment. Make sure you have Python 3.10 installed (tested with python 3.10.10).

```bash
python3.10 -m venv venv
```

Activate the Virtual Environment and Install Requirements

```bash
source venv/bin/activate
pip install -r requirements.txt
```

Build the plugin. Make sure you have the .NET 8.0 SDK installed.

```bash
dotnet build
```

Install the plugin. Copy the plugin folder to the Jellyfin plugins directory and restart Jellyfin in order for the plugin to start scanning. The plugin folder must be named "Jellyfin.Plugin.BetterMix".

```bash
sudo mkdir -p /var/lib/jellyfin/plugins/Jellyfin.Plugin.BetterMix/
sudo cp -r bin/Debug/net8.0/* /var/lib/jellyfin/plugins/Jellyfin.Plugin.BetterMix/
sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/Jellyfin.Plugin.BetterMix/
sudo systemctl restart jellyfin
```

The plugin should now be functional. All that's left to do is to setup the reverse proxy to intercept the Instant mix requests.

### 2. Configure NGINX Proxy

Create an NGINX configuration for Jellyfin (or edit your existing one)

```bash
sudo vim /etc/nginx/sites-available/jellyfin
```

Intercept and proxy Instant Mix requests. Requests to */Items/{itemId}/InstantMix* should be send to */BetterMix/Items/{itemId}* instead. This example will use the port 8080 for the server:
```nginx
server {
    listen 8080;
    server_name localhost;

    # Redirect InstantMix requests to the plugin
    location ~ ^/Items/([a-f0-9]+)/InstantMix$ {
        rewrite ^/Items/([a-f0-9]+)/InstantMix$ /BetterMix/Items/$1 break;

        proxy_pass http://localhost:8096;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Emby-Token $http_x_emby_token;
    }

    # All other requests go to default Jellyfin
    location / {
        proxy_pass http://localhost:8096;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Emby-Token $http_x_emby_token;
    }
}
```
Add the configuration to the sites enabled:
```bash
sudo ln -s /etc/nginx/sites-available/jellyfin /etc/nginx/sites-enabled/
```

Test and reload NGINX:
```bash
sudo nginx -t
sudo systemctl reload nginx
```

Now all Instant Mix requests should be handled by the plugin (when accessing the server from the port 8080).
Until the scan is completed, the plugin will fallback to the native Instant Mix.

### 3. Configure the plugin

You can configure and fine tune the plugin using the Jellyfin plugin configuration page.
