#!/usr/bin/env python3
"""Remove MeshCollider overrides mistakenly added to Car 1 / Police 1 in SimplePoly Demo scene."""
import re
import sys
import os

SCENE_PATH = "Assets/download/SimplePoly City - Low Poly Assets/Demo/SimplePoly City - Low Poly Assets_Demo Scene.unity"
CAR_INSTANCE = "406022014"
POLICE_INSTANCE = "847077038"


def main(root):
    path = os.path.join(root, *SCENE_PATH.split("/"))

    with open(path, "r", encoding="utf-8") as f:
        text = f.read()

    car_go = set(re.findall(
        r"--- !u!1 &(406022\d+) stripped\nGameObject:.*?m_PrefabInstance: \{fileID: " + CAR_INSTANCE + r"\}",
        text, re.S))
    police_go = set(re.findall(
        r"--- !u!1 &(847077\d+) stripped\nGameObject:.*?m_PrefabInstance: \{fileID: " + POLICE_INSTANCE + r"\}",
        text, re.S))
    vehicle_go = car_go | police_go

    mesh_pattern = re.compile(
        r"--- !u!64 &\d+\nMeshCollider:\n(?:.*?\n)*?  m_GameObject: \{fileID: (\d+)\}\n(?:.*?\n)*?  m_Mesh:.*?\n",
        re.MULTILINE)

    removed_mesh = 0

    def repl_mesh(m):
        nonlocal removed_mesh
        go_id = m.group(1)
        if go_id in vehicle_go:
            removed_mesh += 1
            return ""
        return m.group(0)

    text = mesh_pattern.sub(repl_mesh, text)

    for inst_id in (CAR_INSTANCE, POLICE_INSTANCE):
        block_pat = re.compile(
            r"(--- !u!1001 &" + inst_id + r"\nPrefabInstance:.*?m_AddedComponents:\n)(?:    - targetCorrespondingSourceObject:.*?\n      insertIndex: -1\n      addedObject: \{fileID: \d+\}\n)+",
            re.S)

        def repl_added(m):
            return m.group(1) + "    []\n"

        text, n = block_pat.subn(repl_added, text)
        if n:
            print("Cleared added components on instance", inst_id)

    rb_pattern = re.compile(
        r"--- !u!54 &\d+\nRigidbody:\n(?:.*?\n)*?  m_GameObject: \{fileID: (\d+)\}\n(?:.*?\n)*?(?=--- !u!|\Z)",
        re.MULTILINE)
    removed_rb = 0

    def repl_rb(m):
        nonlocal removed_rb
        if m.group(1) in vehicle_go:
            removed_rb += 1
            return ""
        return m.group(0)

    text = rb_pattern.sub(repl_rb, text)

    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    print("Vehicle child GO ids:", len(vehicle_go))
    print("Removed MeshColliders:", removed_mesh)
    print("Removed child Rigidbody blocks:", removed_rb)
    print("Done.")


if __name__ == "__main__":
    project_root = sys.argv[1] if len(sys.argv) > 1 else r"d:\myUnity\Virtual_vehicle"
    main(project_root)
