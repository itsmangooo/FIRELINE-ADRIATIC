"""Verify acquired source hashes, real mask pixels, Unity import flags and SI height data."""
import hashlib
import json
from pathlib import Path
import re
import numpy as np
from PIL import Image
import tifffile

ROOT = Path(__file__).resolve().parent.parent
source = ROOT / "AssetSources/Environment"
content = ROOT / "Assets/Fireline/Content/Environment/M00"
manifest = json.loads((source / "acquisition-manifest.json").read_text())
for asset in manifest["materials"]:
    name = asset["id"]
    for kind, record in asset["inputs"].items():
        path = source / "PolyHaven" / name / f"{kind}.jpg"
        assert path.stat().st_size == record["bytes"]
        assert hashlib.sha256(path.read_bytes()).hexdigest() == record["sha256"]
    for name_part, expected in asset["outputHashes"].items():
        path = content / name / name_part
        assert hashlib.sha256(path.read_bytes()).hexdigest() == expected
        with Image.open(path) as image:
            assert image.size == (2048, 2048)
        meta = path.with_suffix(path.suffix + ".meta").read_text()
        def field(key):
            return int(re.search(rf"^\s*{key}: (\d+)$", meta, re.M).group(1))
        assert field("sRGBTexture") == int(name_part == "albedo.jpg")
        assert field("textureType") == int(name_part == "normal_dx.jpg")
        assert field("flipGreenChannel") == int(name_part == "normal_dx.jpg")
        assert field("enableMipMap") == field("streamingMipmaps") == 1
        assert field("maxTextureSize") == 2048
    packed = np.array(Image.open(content / name / "terrain_mask.png"))
    ao = np.array(Image.open(source / "PolyHaven" / name / "AO.jpg").convert("L"))
    rough = np.array(Image.open(source / "PolyHaven" / name / "Rough.jpg").convert("L"))
    assert (packed[:, :, 0] == 0).all() and (packed[:, :, 2] == 128).all()
    assert np.array_equal(packed[:, :, 1], ao)
    assert np.array_equal(packed[:, :, 3], 255 - rough)
    print("PASS: source/derived hashes, all mask pixels, 2K dimensions and Unity import flags:", name)

record = manifest["heightfield"]
height_path = content / "petrcane_heights.bytes"
assert hashlib.sha256(height_path.read_bytes()).hexdigest() == record["outputSha256"]
heights = np.fromfile(height_path, dtype="<f4").reshape(257, 257)
assert np.isfinite(heights).all() and heights.min() == 0 and heights.max() < 128
with tifffile.TiffFile(source / "Copernicus/N44E015.tif") as image:
    keys = image.pages[0].tags[34735].value
    assert next(keys[i + 3] for i in range(4, len(keys), 4) if keys[i] == 1025) == 2
assert record["sizeMeters"] == 6000 and record["centerLatitude"] == 44.19 and record["centerLongitude"] == 15.16
assert float(heights[128, 128]) == 21.497352600097656
assert float(heights[100, 160]) == 29.8624267578125
assert float(heights[180, 180]) == 48.34121322631836
print("PASS: PixelIsPoint semantics, geographic extent, finite SI float32 heights and orientation checkpoints")
