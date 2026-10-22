"""Acquire the explicitly audited M00 Poly Haven/Copernicus sources and derive import data.

Needs Pillow, numpy, tifffile and imagecodecs. Sources remain under AssetSources;
Unity receives 2K color/normal/mask maps and a 257-square SI heightfield only.
"""

import hashlib
import json
import math
from pathlib import Path
import shutil
import urllib.request

import numpy as np
from PIL import Image, ImageOps
import tifffile


ROOT = Path(__file__).resolve().parent.parent
SOURCES = ROOT / "AssetSources/Environment"
CONTENT = ROOT / "Assets/Fireline/Content/Environment/M00"
MATERIALS = ("rocky_terrain", "dry_ground_rocks", "forrest_ground_01", "coast_sand_01", "burned_ground_01")
USER_AGENT = "FIRELINE-ADRIATIC-M00-import/1.0 (Poly Haven CC0 asset acquisition)"


def fetch(url, target, md5=None, expected_size=None):
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        with urllib.request.urlopen(request, timeout=90) as response:
            data = response.read()
        target.write_bytes(data)
    data = target.read_bytes()
    if md5 and hashlib.md5(data).hexdigest() != md5:
        raise ValueError(f"Upstream MD5 mismatch: {target}")
    if expected_size and len(data) != expected_size:
        raise ValueError(f"Upstream size mismatch: {target}")
    return {"url": url, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def json_source(url, target):
    fetch(url, target)
    return json.loads(target.read_text(encoding="utf-8"))


def materials():
    records = []
    for name in MATERIALS:
        source = SOURCES / "PolyHaven" / name
        output = CONTENT / name
        output.mkdir(parents=True, exist_ok=True)
        files = json_source(f"https://api.polyhaven.com/files/{name}", source / "files.json")
        info = json_source(f"https://api.polyhaven.com/info/{name}", source / "info.json")
        inputs = {}
        for kind in ("Diffuse", "nor_dx", "AO", "Rough"):
            item = files[kind]["2k"]["jpg"]
            if not item["url"].startswith("https://dl.polyhaven.org/"):
                raise ValueError("Unexpected source host")
            path = source / f"{kind}.jpg"
            inputs[kind] = fetch(item["url"], path, item["md5"], item["size"])
            with Image.open(path) as image:
                if image.size != (2048, 2048):
                    raise ValueError(f"Unexpected 2K dimensions: {path}: {image.size}")
        shutil.copyfile(source / "Diffuse.jpg", output / "albedo.jpg")
        shutil.copyfile(source / "nor_dx.jpg", output / "normal_dx.jpg")
        with Image.open(source / "AO.jpg") as ao, Image.open(source / "Rough.jpg") as rough:
            # HDRP TerrainLit: R=metal(0), G=AO, B=height neutral(0.5), A=1-roughness.
            zero = Image.new("L", ao.size, 0)
            neutral = Image.new("L", ao.size, 128)
            Image.merge("RGBA", (zero, ao.convert("L"), neutral,
                                 ImageOps.invert(rough.convert("L")))).save(output / "terrain_mask.png")
        records.append({"id": name, "author": info.get("authors"), "source": f"https://polyhaven.com/a/{name}",
                        "license": "CC0-1.0", "licenseUrl": "https://polyhaven.com/license", "inputs": inputs,
                        "dimensions": [2048, 2048], "sourceScaleMeters": info.get("scale"),
                        "mask": "R=0 metallic, G=AO, B=128/255 neutral height, A=255-roughness; JPEG source precision",
                        "outputHashes": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in output.iterdir() if p.suffix in (".jpg", ".png")}})
        print("Acquired and packed", name, flush=True)
    return records


def heightfield():
    source = SOURCES / "Copernicus"
    tile = "Copernicus_DSM_COG_30_N44_00_E015_00_DEM"
    url = f"https://copernicus-dem-90m.s3.amazonaws.com/{tile}/{tile}.tif"
    record = fetch(url, source / "N44E015.tif", expected_size=4859003)
    with tifffile.TiffFile(source / "N44E015.tif") as image:
        page = image.pages[0]
        grid = page.asarray()
        scale = page.tags[33550].value
        tie = page.tags[33922].value
        keys = page.tags[34735].value
        raster_type = next(keys[i + 3] for i in range(4, len(keys), 4) if keys[i] == 1025)
        if raster_type != 2:
            raise ValueError("This conversion requires PixelIsPoint GeoTIFF sample semantics.")
    if grid.shape != (1200, 1200) or not np.isfinite(grid).all():
        raise ValueError("Unexpected Copernicus GLO-90 tile")
    # Local equirectangular approximation over 6 km, no travel compression/exaggeration.
    center_lat, center_lon, size, resolution = 44.19, 15.16, 6000.0, 257
    meters_per_degree = math.pi * 6371000 / 180
    north = np.linspace(-size / 2, size / 2, resolution)
    east = np.linspace(-size / 2, size / 2, resolution)
    latitudes = center_lat + north / meters_per_degree
    longitudes = center_lon + east / (meters_per_degree * math.cos(math.radians(center_lat)))
    # GeoTIFF is PixelIsPoint: tie point corresponds to the actual sample center.
    columns = (longitudes - tie[3]) / scale[0] + tie[0]
    rows = (tie[4] - latitudes) / scale[1] + tie[1]
    x0, y0 = np.floor(columns).astype(int), np.floor(rows).astype(int)
    if min(x0) < 0 or min(y0) < 0 or max(x0) + 1 >= 1200 or max(y0) + 1 >= 1200:
        raise ValueError("Crop falls outside audited tile")
    fx, fy = columns - x0, rows - y0
    lower = grid[y0[:, None], x0] * (1 - fx) + grid[y0[:, None], x0 + 1] * fx
    upper = grid[(y0 + 1)[:, None], x0] * (1 - fx) + grid[(y0 + 1)[:, None], x0 + 1] * fx
    heights = np.maximum(0, lower * (1 - fy[:, None]) + upper * fy[:, None]).astype("<f4")
    output = CONTENT / "petrcane_heights.bytes"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(heights.tobytes())
    metadata = {"source": record, "dataset": "Copernicus GLO-90 DSM, AWS 2021 release",
                "licenseBundle": "AssetSources/Environment/Copernicus/cop_dem_licenses_2025.pdf, pages 19-21",
                "centerLatitude": center_lat, "centerLongitude": center_lon, "sizeMeters": size,
                "resolution": resolution, "heightRangeMeters": [float(heights.min()), float(heights.max())],
                "heightDatum": "EGM2008", "landFractionAbove1m": float((heights > 1).mean()),
                "format": "little-endian float32 meters, x=east fastest, rows south to north",
                "georeference": "spherical local equirectangular, radius 6371000m, bilinear resampling; DSM not bare-earth survey",
                "inputPixelScale": scale, "inputTiePoint": tie,
                "outputSha256": hashlib.sha256(output.read_bytes()).hexdigest()}
    (CONTENT / "petrcane_metadata.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
    print("Derived Petrcane heightfield", metadata["heightRangeMeters"], flush=True)
    return metadata


if __name__ == "__main__":
    SOURCES.mkdir(parents=True, exist_ok=True)
    audit = {"materials": materials(), "heightfield": heightfield()}
    (SOURCES / "acquisition-manifest.json").write_text(json.dumps(audit, indent=2) + "\n", encoding="utf-8")
