"""Extract only the hash-audited original files for the user-authorized CL-415 prototype.

Run the read-only verifier first. No network access, archive paths or executable content
are accepted; existing output must already match the recorded source exactly.
"""
import hashlib
import io
import json
from pathlib import Path
import zipfile


def main():
    """Check both archives and the fixed inventory before writing any original source file."""
    audit = json.loads(Path("AssetSources/Aircraft/CL415/source-audit.json").read_text())
    archive = Path("AssetSources/Aircraft/CL415/cl415.zip").read_bytes()
    assert hashlib.sha256(archive).hexdigest() == audit["archiveSha256"]
    with zipfile.ZipFile(io.BytesIO(archive)) as outer:
        source = outer.read("source/c1534907d9f049e082c228574873b06a.zip")
    assert hashlib.sha256(source).hexdigest() == audit["sourceArchiveSha256"]
    allowed = {"cl415.obj", "cl415.mtl", "Read-Me.txt", "adf.png", "ai.png", "alt.png",
               "asi.png", "interior.png", "panel.png", "texture.png", "tmp.png", "vsi.png"}
    output = Path("Assets/Fireline/Content/Aircraft/CL415Prototype/Source")
    with zipfile.ZipFile(io.BytesIO(source)) as inner:
        assert set(inner.namelist()) == allowed
        verified = {}
        for entry in audit["sourceInventory"]:
            name = entry["name"]
            assert name in allowed and Path(name).name == name
            data = inner.read(name)
            assert len(data) == entry["bytes"]
            assert hashlib.sha256(data).hexdigest() == entry["sha256"]
            target = output / name
            assert not target.exists() or target.read_bytes() == data, target
            verified[name] = data
    output.mkdir(parents=True, exist_ok=True)
    for name, data in verified.items():
        (output / name).write_bytes(data)
    # Unity's OBJ importer merges Blender 2.69's object-only declarations into one
    # renderer. Express the same partitions as OBJ groups; never change geometry,
    # faces, normals, UVs or the original source bytes retained in Source/.
    derived = output.parent / "Import"
    derived.mkdir(exist_ok=True)
    obj = verified["cl415.obj"].decode("utf-8")
    grouped = "\n".join("g " + line[2:] if line.startswith("o ") else line
                        for line in obj.split("\n")).encode("utf-8")
    mtl = verified["cl415.mtl"].decode("utf-8")
    relative = "\n".join("map_Kd ../Source/" + line[7:] if line.startswith("map_Kd ") else line
                         for line in mtl.split("\n")).encode("utf-8")
    for name, data in {"cl415-unity.obj": grouped, "cl415.mtl": relative}.items():
        target = derived / name
        assert not target.exists() or target.read_bytes() == data, target
        target.write_bytes(data)
    print(f"PASS: {len(verified)} audited source files extracted; original notices preserved.")
    print("Unity derivative: object declarations expressed as groups; texture paths refer to original PNGs.")


if __name__ == "__main__":
    main()
