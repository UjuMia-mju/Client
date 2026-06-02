#!/usr/bin/env python3
"""Apply Model hierarchy to 1-2 and 1-3 planet prefabs (YAML)."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Prefabs" / "Planet"

MODEL_GO_12 = "7100000000000000001"
MODEL_TR_12 = "7100000000000000002"
ROOT_TR_12 = "6927100825063874096"
PLANET_TR_12 = "6090262479104584690"
MEADOW_TR_12 = "5715348893134487362"

MODEL_GO_13 = "7100000000000000003"
MODEL_TR_13 = "7100000000000000004"
ROOT_TR_13 = "1508996713492246919"
PLANET_TR_13 = "4627539688368304285"
FOG_TR_13 = "4958120271394460059"
SNOW_TR_13 = "4808208521653462244"


def insert_after(content: str, anchor: str, block: str) -> str:
    idx = content.find(anchor)
    if idx == -1:
        raise ValueError(f"Anchor not found: {anchor[:80]}")
    line_end = content.find("\n", idx)
    return content[: line_end + 1] + block + content[line_end + 1 :]


def replace_once(content: str, old: str, new: str, label: str) -> str:
    if old not in content:
        raise ValueError(f"Missing pattern ({label}): {old[:120]}")
    return content.replace(old, new, 1)


def model_block(go_id: str, tr_id: str, parent_tr: str, child_tr: str) -> str:
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
  m_Name: Model
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
  - {{fileID: {child_tr}}}
  m_Father: {{fileID: {parent_tr}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""


def migrate_1_2(content: str) -> str:
    if f"m_Name: Model" in content and f"&{MODEL_TR_12}" in content:
        print("1-2 already has Model")
        return content

    content = insert_after(
        content,
        f"--- !u!1 &2931069670032783032",
        model_block(MODEL_GO_12, MODEL_TR_12, ROOT_TR_12, PLANET_TR_12),
    )

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: {MEADOW_TR_12}}}\n  - {{fileID: {PLANET_TR_12}}}\n  m_Father: {{fileID: 0}}",
        f"  m_Children:\n  - {{fileID: {MODEL_TR_12}}}\n  m_Father: {{fileID: 0}}",
        "1-2 root children",
    )

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: 5115037535527148192}}\n  - {{fileID: 1987624225582708222}}\n  m_Father: {{fileID: {ROOT_TR_12}}}",
        f"  m_Children:\n  - {{fileID: 5115037535527148192}}\n  - {{fileID: 1987624225582708222}}\n  - {{fileID: {MEADOW_TR_12}}}\n  m_Father: {{fileID: {MODEL_TR_12}}}",
        "1-2 planet parent",
    )

    content = replace_once(
        content,
        f"    m_TransformParent: {{fileID: {ROOT_TR_12}}}",
        f"    m_TransformParent: {{fileID: {PLANET_TR_12}}}",
        "1-2 meadow parent",
    )

    return content


def migrate_1_3(content: str) -> str:
    if f"m_Name: Model" in content and f"&{MODEL_TR_13}" in content:
        print("1-3 already has Model")
        return content

    content = insert_after(
        content,
        f"--- !u!1 &1577846143915004099",
        model_block(MODEL_GO_13, MODEL_TR_13, ROOT_TR_13, PLANET_TR_13),
    )

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: {PLANET_TR_13}}}\n  - {{fileID: {FOG_TR_13}}}\n  - {{fileID: {SNOW_TR_13}}}\n  m_Father: {{fileID: 0}}",
        f"  m_Children:\n  - {{fileID: {MODEL_TR_13}}}\n  - {{fileID: {FOG_TR_13}}}\n  - {{fileID: {SNOW_TR_13}}}\n  m_Father: {{fileID: 0}}",
        "1-3 root children",
    )

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: 9008421890049090147}}\n  m_Father: {{fileID: {ROOT_TR_13}}}",
        f"  m_Children:\n  - {{fileID: 9008421890049090147}}\n  m_Father: {{fileID: {MODEL_TR_13}}}",
        "1-3 planet parent",
    )

    return content


def main():
    p12 = ROOT / "1-2.prefab"
    p13 = ROOT / "1-3.prefab"

    c12 = migrate_1_2(p12.read_text(encoding="utf-8"))
    p12.write_text(c12, encoding="utf-8", newline="\n")
    print(f"Updated {p12}")

    c13 = migrate_1_3(p13.read_text(encoding="utf-8"))
    p13.write_text(c13, encoding="utf-8", newline="\n")
    print(f"Updated {p13}")


if __name__ == "__main__":
    main()
