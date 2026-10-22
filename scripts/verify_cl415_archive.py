"""Verify the downloaded original archive against the recorded read-only audit.

No files are extracted or imported. Licence reconciliation is a separate gate.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import zipfile


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
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
    vertices = [[float(n) for n in line.split()[1:4]] for line in lines if line.startswith("v ")]
    assert len(vertices) == audit["vertices"]
    assert sum(line.startswith("f ") for line in lines) == audit["objFaces"]
    assert [line[2:] for line in lines if line.startswith(("o ", "g "))] == audit["objObjectGroups"]
    for key, operation in (("min", min), ("max", max)):
        assert [operation(v[i] for v in vertices) for i in range(3)] == audit["rawBounds"][key]
    print("PASS: original/nested SHA-256, every source file, notice, OBJ counts/groups/bounds.")
    print("LICENSING BLOCKER remains: listing CC BY 4.0 versus embedded GPL and separate texture credit.")


if __name__ == "__main__":
    main()
