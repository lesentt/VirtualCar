#!/usr/bin/env python3
"""Batch-add MeshCollider to SimplePoly City prefabs missing colliders."""

import os
import re
import hashlib

PREFAB_ROOT = os.path.join(
    os.path.dirname(__file__),
    "..",
    "Assets",
    "download",
    "SimplePoly City - Low Poly Assets",
    "Prefab",
)

COLLIDER_MARKERS = ("MeshCollider:", "BoxCollider:", "CapsuleCollider:", "SphereCollider:")
MESH_COLLIDER_TEMPLATE = """--- !u!64 &{cid}
MeshCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 5
  m_Convex: 0
  m_CookingOptions: 30
  m_Mesh: {{fileID: {mesh_ref}}}
"""


def split_blocks(text):
    parts = re.split(r"(?=--- !u!)", text)
    return [p for p in parts if p.strip()]


def parse_block_header(block):
    m = re.match(r"--- !u!(\d+) &(\d+)", block)
    if not m:
        return None, None
    return int(m.group(1)), int(m.group(2))


def parse_gameobject_components(block):
    ids = re.findall(r"- component: \{fileID: (\d+)\}", block)
    return [int(x) for x in ids]


def parse_meshfilter(block):
    go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
    mesh = re.search(r"m_Mesh: \{fileID: ([^}]+)\}", block)
    if not go or not mesh:
        return None
    return int(go.group(1)), mesh.group(1)


def new_collider_id(path, go_id):
    h = hashlib.md5(f"{path}:{go_id}".encode()).hexdigest()
    return 640000000 + int(h[:7], 16) % 100000000


def process_prefab(path):
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()

    blocks = split_blocks(text)
    comp_type = {}
    go_components = {}
    meshfilters = []

    for block in blocks:
        type_id, comp_id = parse_block_header(block)
        if type_id is None:
            continue

        if type_id == 1:
            go_components[comp_id] = parse_gameobject_components(block)
        elif "MeshFilter:" in block:
            parsed = parse_meshfilter(block)
            if parsed:
                meshfilters.append((comp_id, parsed[0], parsed[1]))
            comp_type[comp_id] = "MeshFilter"
        elif any(marker in block for marker in COLLIDER_MARKERS):
            go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
            if go:
                comp_type[comp_id] = ("Collider", int(go.group(1)))
        else:
            comp_type[comp_id] = "Other"

    go_has_collider = set()
    for _, info in comp_type.items():
        if isinstance(info, tuple) and info[0] == "Collider":
            go_has_collider.add(info[1])

    additions = []
    go_patches = {}

    for _, go_id, mesh_ref in meshfilters:
        if go_id in go_has_collider:
            continue
        cid = new_collider_id(path, go_id)
        additions.append(MESH_COLLIDER_TEMPLATE.format(cid=cid, go_id=go_id, mesh_ref=mesh_ref))
        go_patches.setdefault(go_id, []).append(cid)
        go_has_collider.add(go_id)

    if not additions:
        return 0

    out_blocks = []
    for block in blocks:
        type_id, comp_id = parse_block_header(block)
        if type_id == 1 and comp_id in go_patches:
            insert = "".join(f"  - component: {{fileID: {cid}}}\n" for cid in go_patches[comp_id])
            block = re.sub(
                r"(m_Component:\n(?:  - component: \{fileID: \d+\}\n)+)",
                lambda m: m.group(1) + insert,
                block,
                count=1,
            )
        out_blocks.append(block)

    out_blocks.extend(additions)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("".join(out_blocks))

    return len(additions)


def main():
    total_files = 0
    total_colliders = 0
    for root, _, files in os.walk(PREFAB_ROOT):
        for name in files:
            if not name.endswith(".prefab"):
                continue
            path = os.path.join(root, name)
            n = process_prefab(path)
            if n:
                total_files += 1
                total_colliders += n
                print(f"+{n}  {path}")

    print(f"\nDone: {total_colliders} MeshColliders in {total_files} prefabs")


if __name__ == "__main__":
    main()
