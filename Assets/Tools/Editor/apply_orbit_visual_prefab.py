#!/usr/bin/env python3
"""Bake OrbitVisual container into 1-2 / 1-3 prefabs."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Prefabs" / "Planet"

ORBIT_VIS_GO_12 = "7200000000000000001"
ORBIT_VIS_TR_12 = "7200000000000000002"
ROOT_TR_12 = "6927100825063874096"
PLANET_TR_12 = "6090262479104584690"
MEADOW_TR_12 = "5715348893134487362"

ORBIT_VIS_GO_13 = "7200000000000000003"
ORBIT_VIS_TR_13 = "7200000000000000004"
ROOT_TR_13 = "1508996713492246919"
PLANET_TR_13 = "4627539688368304285"
FOG_TR_13 = "4958120271394460059"
SNOW_TR_13 = "4808208521653462244"


def replace_once(content: str, old: str, new: str, label: str) -> str:
    if old not in content:
        raise ValueError(f"Missing ({label})")
    return content.replace(old, new, 1)


def orbit_visual_block(go_id: str, tr_id: str, parent_tr: str, children: list[str]) -> str:
    child_lines = "\n".join(f"  - {{fileID: {c}}}" for c in children)
    return f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr_id}}}
  m_Layer: 0
  m_Name: OrbitVisual
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{child_lines}
  m_Father: {{fileID: {parent_tr}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def migrate_1_2(content: str) -> str:
    if "m_Name: OrbitVisual" in content and ORBIT_VIS_TR_12 in content:
        print("1-2: OrbitVisual already present")
        return content

    content = replace_once(
        content,
        "--- !u!1 &2931069670032783032\nGameObject:",
        orbit_visual_block(
            ORBIT_VIS_GO_12,
            ORBIT_VIS_TR_12,
            ROOT_TR_12,
            [MEADOW_TR_12, PLANET_TR_12],
        )
        + "--- !u!1 &2931069670032783032\nGameObject:",
        "1-2 insert OrbitVisual",
    )

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: {MEADOW_TR_12}}}\n  - {{fileID: {PLANET_TR_12}}}\n  m_Father: {{fileID: 0}}",
        f"  m_Children:\n  - {{fileID: {ORBIT_VIS_TR_12}}}\n  m_Father: {{fileID: 0}}",
        "1-2 root children",
    )

    content = replace_once(
        content,
        f"  m_Father: {{fileID: {ROOT_TR_12}}}\n  m_LocalEulerAnglesHint: {{x: -90, y: 0, z: 0}}\n--- !u!33 &3086512465145533825",
        f"  m_Father: {{fileID: {ORBIT_VIS_TR_12}}}\n  m_LocalEulerAnglesHint: {{x: -90, y: 0, z: 0}}\n--- !u!33 &3086512465145533825",
        "1-2 planet parent",
    )

    return content


def migrate_1_3(content: str) -> str:
    if "m_Name: OrbitVisual" in content and ORBIT_VIS_TR_13 in content:
        print("1-3: OrbitVisual already present")
        return content

    content = replace_once(
        content,
        "--- !u!1 &1577846143915004099\nGameObject:",
        orbit_visual_block(
            ORBIT_VIS_GO_13,
            ORBIT_VIS_TR_13,
            ROOT_TR_13,
            [PLANET_TR_13, FOG_TR_13, SNOW_TR_13],
        )
        + "--- !u!1 &1577846143915004099\nGameObject:",
        "1-3 insert OrbitVisual",
    )

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: {PLANET_TR_13}}}\n  - {{fileID: {FOG_TR_13}}}\n  - {{fileID: {SNOW_TR_13}}}\n  m_Father: {{fileID: 0}}",
        f"  m_Children:\n  - {{fileID: {ORBIT_VIS_TR_13}}}\n  m_Father: {{fileID: 0}}",
        "1-3 root children",
    )

    content = replace_once(
        content,
        f"  m_Father: {{fileID: {ROOT_TR_13}}}\n  m_LocalEulerAnglesHint: {{x: -90, y: 0, z: 0}}\n--- !u!33 &4560097482292108776",
        f"  m_Father: {{fileID: {ORBIT_VIS_TR_13}}}\n  m_LocalEulerAnglesHint: {{x: -90, y: 0, z: 0}}\n--- !u!33 &4560097482292108776",
        "1-3 planet parent",
    )

    return content


def main():
    for name, fn in [("1-2.prefab", migrate_1_2), ("1-3.prefab", migrate_1_3)]:
        path = ROOT / name
        path.write_text(fn(path.read_text(encoding="utf-8")), encoding="utf-8", newline="\n")
        print(f"Updated {path}")


if __name__ == "__main__":
    main()
