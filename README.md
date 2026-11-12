# Ember EDL Sidecars — Jellyfin plugin

The server half of **Ember Player**'s content filters. Put an `.edl` file next to a movie or
episode, and Ember skips or mutes the listed ranges during playback.

- Read-only. Exposes one authenticated endpoint, `GET /EmberEDL/{itemId}`.
- `Movie.mkv` matches `Movie.edl` in the same folder, in any library, with no path mappings.
- Respects each user's library access, and never modifies your media.

Supports **Jellyfin 10.11.10+** and **Jellyfin 12**. The repository gives each server the right build automatically.

---

## 1. Add the plugin repository

In Jellyfin: **Dashboard → Plugins → Repositories (Manage Repositories) → +**

| Field | Value |
|---|---|
| Repository Name | `Ember` |
| Repository URL | `https://raw.githubusercontent.com/swahilitodd/jellyfin-ember-edl/main/manifest.json` |

Save.

## 2. Install

1. **Dashboard → Plugins → Catalog**, open **Ember EDL Sidecars** and click **Install**.
2. **Restart Jellyfin.**
3. In **Dashboard → Plugins → My Plugins**, the plugin should show as **Active**.

New versions appear as updates in **My Plugins** whenever they're published here.

> **Upgrading from a hand-copied DLL?** Install from the repository as above, restart, and
> confirm the new version is Active. Then stop Jellyfin, delete the old `/config/plugins/EmberEDL_0.1.0.0/`
> folder, and start Jellyfin again. Both copies share the same plugin ID, so the newer one takes over either way.

## 3. Add EDL files beside your media

The `.edl` file must have the **same name** as the video and sit in the **same folder**:

```text
/movies/My Movie (2025)/My Movie (2025).mkv
/movies/My Movie (2025)/My Movie (2025).edl

/tv/Show/Season 01/Show - S01E01 - Pilot.mkv
/tv/Show/Season 01/Show - S01E01 - Pilot.edl
```

Format: one range per line, `start_seconds end_seconds action`, where action `0` = **skip** and `1` = **mute**.

```text
12.50  13.80  1
94.10  97.25  0
```

- You don't need a library scan or refresh. The file is read each time playback starts, so edits apply right away.
- Jellyfin must be able to **read** the `.edl` file. On Docker, the media volume must be mounted and readable by the container.
- Maximum size: 1 MiB.

## 4. Use it in Ember Player

Nothing to configure. When Ember starts playing an item, it asks the plugin for that item's EDL
using the signed-in user's session:

- **EDL found:** skip and mute ranges are applied during playback. Ember also keeps the EDL with offline downloads.
- **No EDL (404):** the video plays normally.
- **Plugin missing or the file can't be read:** Ember shows an *EDL unavailable* warning.

## API reference

`GET /EmberEDL/{itemId}` with a normal Jellyfin user token (`X-Emby-Token` or `Authorization: MediaBrowser Token="…"`).
The item ID may be in hyphenated or 32-hex format.

| Status | Meaning |
|---|---|
| `200 text/plain` | The EDL text (empty if the file is empty) |
| `401` | Missing or invalid sign-in |
| `404` | No matching `.edl`, the item isn't a video, or this user can't access it |
| `413` | EDL is larger than 1 MiB |
| `503` | The file exists but Jellyfin can't read it (check permissions or mounts) |

Quick test:

```bash
curl -H "X-Emby-Token: YOUR_TOKEN" http://YOUR-SERVER:8096/EmberEDL/ITEM_ID
```

## Troubleshooting

| Problem | Fix |
|---|---|
| Plugin isn't in the Catalog | Check the repository URL. Your server must be 10.11.10 or newer. |
| Plugin shows *Malfunctioned* or *NotSupported* | Check the Jellyfin log for load errors, and make sure only one copy of the plugin folder exists. |
| Ember says *EDL unavailable* | Check the plugin is Active and restart Jellyfin. If you get a `503`, fix the file permissions or Docker mount. |
| EDL is ignored (no warning shown) | The name must match the video exactly, including case on Linux and Synology. |

---

## Releasing a new version (maintainers)

1. Edit `version` in `plugin.yaml` (3 parts, e.g. `0.1.1`) and add a `changelog` line.
2. Commit, push to `main`, then push the tag:
   ```bash
   git tag v0.1.1 && git push origin v0.1.1
   ```
   You can also run **Build & publish plugin** from the Actions tab.
3. The workflow builds the Jellyfin 10.11 build (.NET 9) and the Jellyfin 12 build (.NET 10), attaches the zips to
   a GitHub Release, and adds both versions to `manifest.json`. If one build fails, the other
   is still published, and a warning appears in the run.

Version scheme: `0.1.1.11` is the Jellyfin 10.11 build and `0.1.1.12` is the Jellyfin 12 build of release `0.1.1`.

Local build: `bash scripts/build.sh 10.11` or `bash scripts/build.sh 12` (needs the .NET 9 or .NET 10 SDK).
Original notes are in `docs/DEVELOPER-NOTES.md`.
