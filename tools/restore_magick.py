#!/usr/bin/env python3
"""Restore the pinned Magick.NET runtime files from their official NuGet packages."""

from pathlib import Path
from urllib.request import urlopen
import zipfile


ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "third_party" / "Magick.NET"
VERSION = "15.0.0"
PACKAGES = {
    "magick.net-q8-x64": {
        "lib/netstandard20/Magick.NET-Q8-x64.dll": "Magick.NET-Q8-x64.dll",
        "runtimes/win-x64/native/Magick.Native-Q8-x64.dll": "Magick.Native-Q8-x64.dll",
        "Notice.txt": "Notice.txt",
    },
    "magick.net.core": {
        "lib/netstandard20/Magick.NET.Core.dll": "Magick.NET.Core.dll",
    },
}


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    for package, members in PACKAGES.items():
        url = (
            "https://api.nuget.org/v3-flatcontainer/"
            + package
            + "/"
            + VERSION
            + "/"
            + package
            + "."
            + VERSION
            + ".nupkg"
        )
        print("Downloading", package, VERSION)
        with urlopen(url, timeout=60) as response:
            package_bytes = response.read()
        archive_path = DEST / (package + "." + VERSION + ".nupkg")
        archive_path.write_bytes(package_bytes)
        with zipfile.ZipFile(archive_path) as archive:
            for member, filename in members.items():
                output = DEST / filename
                output.write_bytes(archive.read(member))
                print("Restored", output.name)
        archive_path.unlink()

    copyright_file = DEST / "Copyright.txt"
    if not copyright_file.exists():
        copyright_file.write_text("Magick.NET is Copyright 2013-2026 Dirk Lemstra.\n", encoding="utf-8")


if __name__ == "__main__":
    main()
