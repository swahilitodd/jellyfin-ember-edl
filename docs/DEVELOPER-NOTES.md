# Ember EDL Sidecars — Jellyfin plugin v0.1.0 (source)

A small **read-only**, authenticated Jellyfin server plugin for Ember's EDL mute/skip filters. No Python bridge, SMBClient, port 8765, path mappings, or extra web service. `Episode.mkv` automatically matches `Episode.edl` in the same folder, regardless of Jellyfin library.

## Important compatibility / build status

**This archive is source code, not a compiled DLL.** This environment does not have the .NET SDK or external package download access, so the plugin has **not yet been compiled against Jellyfin**. Compile and test it before installing it on production Jellyfin. Install only the matching variant for your Jellyfin server release. Jellyfin 12.x requires .NET 10; Jellyfin 10.11.x requires .NET 9. Jellyfin 10.10 and older are not included.

## Build on Windows (PowerShell, .NET SDK 10 installed)

```powershell
cd .\Ember-EDL-Jellyfin-Plugin-v0.1.0
.\scripts\build.ps1 -Jellyfin 12
# Or, if server is 10.11.x:
.\scripts\build.ps1 -Jellyfin 10.11
```

On macOS/Linux with the .NET SDK: `bash scripts/build.sh 12` or `bash scripts/build.sh 10.11`.

The output is `dist/jf12/Jellyfin.Plugin.EmberEDL.dll` or `dist/jf10.11/Jellyfin.Plugin.EmberEDL.dll`. If the build reports an error, save it for troubleshooting rather than installing an uncompiled DLL.

## Install on Jellyfin Synology Docker

1. In Jellyfin Dashboard → General, confirm the installed **server version**. Do **not** upgrade Jellyfin just to use this plugin.
2. Back up Jellyfin's persistent `/config` directory. Locate the Synology bind mount used for `/config` in Container Manager or `docker inspect jellyfin`.
3. Stop the Jellyfin container (not other containers).
4. Inside the persistent Jellyfin `/config/plugins/` directory, create `EmberEDL_0.1.0.0/` and put ONLY the matching compiled `Jellyfin.Plugin.EmberEDL.dll` inside. If `/config/plugins/` is a separate bind mount, use that mount instead.
5. Start Jellyfin and check Dashboard → Plugins → My Plugins. Check Jellyfin logs for any load/ABI errors.
6. Keep your existing bridge running until the Ember client is switched and the EDL lookup has been verified. **Installing the plugin alone does not modify Ember v0.15**, which still requests the old bridge endpoint.

## API

`GET /EmberEDL/{itemId}` with an ordinary Jellyfin user token, for example:

```powershell
$base = 'http://YOUR-JELLYFIN:8096'
$itemId = 'YOUR-REAL-ITEM-GUID'
$token = 'YOUR-SESSION-TOKEN'
Invoke-WebRequest -Uri "$base/EmberEDL/$itemId" -Headers @{ 'X-Emby-Token' = $token }
```

* `200 text/plain`: exact EDL text (can be empty if the actual file is empty)
* `404`: no matching EDL, item not in an accessible library, or not a video
* `401`: missing/invalid auth identity
* `413`: EDL over 1 MiB
* `503`: file exists but Jellyfin cannot read it

The plugin never accepts a path from the client and never modifies files. It resolves media from the Jellyfin library item, and the user-specific item lookup enforces the requesting profile's library access. Note that EDLs are sidecars; they are not Jellyfin subtitle files and do not need a scan or media refresh if the item already exists. Jellyfin must be able to **read** the media folder and the adjacent .edl (Docker volume mounts and filesystem permissions matter).

## Format

Standard EDL text: `start_seconds end_seconds action`; Ember supports `0` for skip, `1` for mute. Example:

```text
12.50 13.80 1
94.10 97.25 0
```

## Security

Only GET is exposed. Uses Jellyfin authentication, server-resolved item IDs, user-scoped library lookup, and a 1 MiB size limit. It does not permit arbitrary filesystem paths. If authentication or lookup fails, it fails closed. Use HTTPS when accessing outside a trusted LAN. Do not expose item tokens in logs or screenshots.

## Connecting Ember

The current v0.15 client still points at `http://10.4.29.225:8765/api/edl?path=...` and **will not automatically use this plugin**. A follow-up Ember client update must call `GET /EmberEDL/{itemId}` with the already-saved user token instead. Test that change before retiring the bridge.
