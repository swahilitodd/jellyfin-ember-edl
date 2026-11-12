#!/usr/bin/env python3
"""Build every variant in plugin.yaml, zip each DLL, and print a JSON list of results.

A variant that fails to build is reported and skipped (so a missing Jellyfin 12 NuGet
package doesn't block the 10.11 release). Exits non-zero only if nothing built.
"""
import hashlib
import json
import os
import shutil
import subprocess
import sys
import zipfile

import yaml

meta = yaml.safe_load(open("plugin.yaml", encoding="utf-8"))
base_version = str(meta["version"])
if base_version.count(".") != 2:
    sys.exit("plugin.yaml version must be X.Y.Z")

os.makedirs("dist", exist_ok=True)
results = []
for v in meta["variants"]:
    version = f"{base_version}.{v['suffix']}"
    out = f"publish/jf{v['jellyfin']}"
    shutil.rmtree(out, ignore_errors=True)
    cmd = [
        "dotnet", "publish", "src/EmberEDL.csproj", "-c", "Release",
        "-f", v["framework"], f"-p:JellyfinVariant={v['jellyfin']}",
        f"-p:Version={version}", "-o", out,
    ]
    print("::group::" + " ".join(cmd), flush=True)
    rc = subprocess.call(cmd)
    print("::endgroup::", flush=True)
    if rc != 0:
        print(f"::warning::Build for Jellyfin {v['jellyfin']} failed (exit {rc}); skipping that variant.")
        continue
    zip_name = f"ember-edl-sidecars_{version}_jellyfin-{v['jellyfin']}.zip"
    zip_path = os.path.join("dist", zip_name)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        z.write(os.path.join(out, "Jellyfin.Plugin.EmberEDL.dll"), "Jellyfin.Plugin.EmberEDL.dll")
    md5 = hashlib.md5(open(zip_path, "rb").read()).hexdigest()
    results.append({"version": version, "targetAbi": v["targetAbi"], "zip": zip_name, "md5": md5,
                    "jellyfin": v["jellyfin"]})

json.dump(results, open("dist/results.json", "w"), indent=2)
print(json.dumps(results, indent=2))
if not results:
    sys.exit("No variant built successfully.")
