"""Verify the downloaded original archive against the recorded read-only audit.

This verifier extracts nothing. The separate prototype extractor follows the user's
2026-10-24 temporary-use instruction; the original licence conflict remains recorded.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import zipfile


def sha256(data):
    """Return the SHA-256 fingerprint of the supplied bytes without changing or extracting them."""
    return hashlib.sha256(data).hexdigest()


def main():
    """Compare all original hashes, notices and OBJ records; this check does not resolve contributor rights."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("--imported", action="store_true", help="Also verify extracted originals and the exact Unity group/path-only derivative")
    parser.add_argument("--output", type=Path, help="Write scoped verified file fingerprints as JSON")
    args = parser.parse_args()
    audit_dir = Path("AssetSources/Aircraft/CL415")
    audit = json.loads((audit_dir / "source-audit.json").read_text(encoding="utf-8"))
    data = args.archive.read_bytes()
    assert len(data) == audit["archiveBytes"] and sha256(data) == audit["archiveSha256"]
    with zipfile.ZipFile(io.BytesIO(data)) as outer:
        source = outer.read("source/c1534907d9f049e082c228574873b06a.zip")
    assert sha256(source) == audit["sourceArchiveSha256"]
    with zipfile.ZipFile(io.BytesIO(source)) as inner:
        inventory = [{"name": entry.filename, "bytes": entry.file_size,
                      "sha256": sha256(inner.read(entry))} for entry in inner.infolist()]
        assert inventory == audit["sourceInventory"]
        notice = inner.read("Read-Me.txt").decode()
        assert notice == audit["includedNotice"]
        assert notice == (audit_dir / "Read-Me.upstream.txt").read_text(encoding="utf-8")
        lines = inner.read("cl415.obj").decode().splitlines()
        originals = {entry["name"]: inner.read(entry["name"]) for entry in inventory}
    vertices = [[float(n) for n in line.split()[1:4]] for line in lines if line.startswith("v ")]
    assert len(vertices) == audit["vertices"]
    assert sum(line.startswith("f ") for line in lines) == audit["objFaces"]
    assert [line[2:] for line in lines if line.startswith(("o ", "g "))] == audit["objObjectGroups"]
    for key, operation in (("min", min), ("max", max)):
        assert [operation(v[i] for v in vertices) for i in range(3)] == audit["rawBounds"][key]
    files = []
    if args.imported:
        root = Path("Assets/Fireline/Content/Aircraft/CL415Prototype")
        for name, data in originals.items():
            path = root / "Source" / name
            assert path.read_bytes() == data, path
            files.append({"path": path.as_posix(), "bytes": len(data), "sha256": sha256(data)})
        expected_obj = "\n".join("g " + line[2:] if line.startswith("o ") else line
                                 for line in originals["cl415.obj"].decode().split("\n")).encode()
        expected_mtl = "\n".join("map_Kd ../Source/" + line[7:] if line.startswith("map_Kd ") else line
                                 for line in originals["cl415.mtl"].decode().split("\n")).encode()
        for name, data in {"cl415-unity.obj": expected_obj, "cl415.mtl": expected_mtl}.items():
            path = root / "Import" / name
            assert path.read_bytes() == data, path
            files.append({"path": path.as_posix(), "bytes": len(data), "sha256": sha256(data)})
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps({"passed": True, "archiveSha256": audit["archiveSha256"],
                                          "importedChecked": args.imported, "files": files}, indent=2) + "\n")
    print("PASS: original/nested SHA-256, every source file, notice, OBJ counts/groups/bounds.")
    print("NOTICE: listing CC BY 4.0 versus embedded GPL/texture credit retained; user authorized temporary prototype use, replacement before launch.")


if __name__ == "__main__":
    main()
