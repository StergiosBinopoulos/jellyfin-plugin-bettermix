# BetterMix

**BetterMix** is a Jellyfin plugin that replaces Jellyfin's built-in *Instant Mix* feature with a smarter and customizable music playlist generator. It works by injecting an Action Filter to the Items Instant Mix controller (special thanks to [arnesacnussem](https://github.com/arnesacnussem/jellyfin-plugin-meilisearch) for inspiring that idea). The plugin uses the [Deej-AI model by teticio](https://github.com/teticio/Deej-AI), and is made possible thanks to their work.


## Limitations

- The plugin will produce mixes only for tracks (Audio). For all other items (albums, genres, etc.), it will fall back to the native Instant Mix functionality.
- Currently, the only supported backend is Deej-AI.
- PyInstaller is used to create standalone executables from the Deej-Ai scripts, eliminating the Python dependency on the Jellyfin server. However the execution time of the scripts is increased as a result.


## Setup


### 1. Install the plugin 
### Download the latest release (Option 1)

Download the plugin:
```bash
curl -L https://github.com/StergiosBinopoulos/jellyfin-plugin-bettermix/releases/latest/download/linux-x64.zip --output bettermix.zip
```
Unzip it and move it in your jellyfin server plugin directory:
```bash
unzip bettermix.zip
sudo mv Jellyfin.Plugin.BetterMix /var/lib/jellyfin/plugins/Jellyfin.Plugin.BetterMix
sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/Jellyfin.Plugin.BetterMix/
sudo systemctl restart jellyfin
```

### Build from source (Option 2)

#### Requirements
- The Deej-AI model
- Python 3.10 (to build the Deej-AI script executables)
- NET 8.0 (to build the plugin)

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

The plugin should now be functional. It will default to the native Instant Mix until the scan is completed. (this can take from minutes to several hours depending on your library size, rough estimate 5 seconds per track)

### 2. Configure the plugin

You can configure and fine tune the plugin by using the Jellyfin plugin configuration page.
