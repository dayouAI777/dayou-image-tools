#!/usr/bin/env python3
"""Build a SHA-256-verified .dayouupdate archive from the current build output."""

import argparse
import hashlib
from pathlib import Path
import zipfile
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
PAYLOAD_FILES = (
    "DayouImageTools.exe",
    "Magick.NET-Q8-x64.dll",
    "Magick.NET.Core.dll",
    "Magick.Native-Q8-x64.dll",
    "DayouImageTools.ico",
)
MINIMUM_UPDATER_VERSION = "1.3.1"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True, help="release version, for example 1.3.1")
    parser.add_argument("--output", type=Path, help="output path; defaults to bin/大有图片工具_vVERSION.dayouupdate")
    args = parser.parse_args()

    output = args.output or (ROOT / "bin" / ("大有图片工具_v" + args.version + ".dayouupdate"))
    output = output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)

    manifest = ET.Element(
        "dayouUpdate",
        product="DayouImageTools",
        version=args.version,
        minimumUpdaterVersion=MINIMUM_UPDATER_VERSION,
    )
    files = []
    for name in PAYLOAD_FILES:
        source = ROOT / "bin" / name
        if not source.is_file():
            raise FileNotFoundError("Missing built release file: " + str(source))
        files.append((source, "payload/" + name))
        ET.SubElement(
            manifest,
            "file",
            source="payload/" + name,
            target=name,
            sha256=sha256(source),
        )

    xml = ET.tostring(manifest, encoding="utf-8", xml_declaration=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        archive.writestr("dayou-update.xml", xml)
        for source, archive_name in files:
            archive.write(source, archive_name)

    with zipfile.ZipFile(output, "r") as archive:
        bad_member = archive.testzip()
        if bad_member:
            raise RuntimeError("Archive CRC check failed for " + bad_member)
    print("Created:", output)
    print("Files:", len(files))
    print("Bytes:", output.stat().st_size)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
