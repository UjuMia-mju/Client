#!/usr/bin/env python3
"""Revert mistaken empty 'Model' GameObject from 1-2 / 1-3 prefabs."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Prefabs" / "Planet"

MODEL_TR_12 = "7100000000000000002"
ROOT_TR_12 = "6927100825063874096"
PLANET_TR_12 = "6090262479104584690"
MEADOW_TR_12 = "5715348893134487362"

MODEL_TR_13 = "7100000000000000004"
ROOT_TR_13 = "1508996713492246919"
PLANET_TR_13 = "4627539688368304285"
FOG_TR_13 = "4958120271394460059"
SNOW_TR_13 = "4808208521653462244"


def replace_once(content: str, old: str, new: str, label: str) -> str:
    if old not in content:
        raise ValueError(f"Missing ({label})")
    return content.replace(old, new, 1)


def remove_block(content: str, start_marker: str, end_marker: str) -> str:
    start = content.find(start_marker)
    if start == -1:
        return content
    end = content.find(end_marker, start + len(start_marker))
    if end == -1:
        raise ValueError(f"End marker not found after {start_marker[:40]}")
    return content[:start] + content[end:]


def revert_1_2(content: str) -> str:
    if MODEL_TR_12 not in content:
        print("1-2: no Model block to revert")
        return content

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: {MODEL_TR_12}}}\n  m_Father: {{fileID: 0}}",
        f"  m_Children:\n  - {{fileID: {MEADOW_TR_12}}}\n  - {{fileID: {PLANET_TR_12}}}\n  m_Father: {{fileID: 0}}",
        "1-2 root",
    )
    content = replace_once(
        content,
        f"  - {{fileID: 5715348893134487362}}\n  m_Father: {{fileID: {MODEL_TR_12}}}",
        f"  m_Father: {{fileID: {ROOT_TR_12}}}",
        "1-2 planet - remove meadow from children list wrongly",
    )
    # fix planet children - remove meadow from planet, restore original children
    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: 5115037535527148192}}\n  - {{fileID: 1987624225582708222}}\n  - {{fileID: {MEADOW_TR_12}}}\n  m_Father: {{fileID: {MODEL_TR_12}}}",
        f"  m_Children:\n  - {{fileID: 5115037535527148192}}\n  - {{fileID: 1987624225582708222}}\n  m_Father: {{fileID: {ROOT_TR_12}}}",
        "1-2 planet",
    )
    content = replace_once(
        content,
        f"    m_TransformParent: {{fileID: {PLANET_TR_12}}}",
        f"    m_TransformParent: {{fileID: {ROOT_TR_12}}}",
        "1-2 meadow parent",
    )
    content = remove_block(
        content,
        "--- !u!1 &7100000000000000001\n",
        "--- !u!1 &2931069670032783032\n",
    )
    return content


def revert_1_3(content: str) -> str:
    if MODEL_TR_13 not in content:
        print("1-3: no Model block to revert")
        return content

    content = replace_once(
        content,
        f"  m_Children:\n  - {{fileID: {MODEL_TR_13}}}\n  - {{fileID: {FOG_TR_13}}}\n  - {{fileID: {SNOW_TR_13}}}\n  m_Father: {{fileID: 0}}",
        f"  m_Children:\n  - {{fileID: {PLANET_TR_13}}}\n  - {{fileID: {FOG_TR_13}}}\n  - {{fileID: {SNOW_TR_13}}}\n  m_Father: {{fileID: 0}}",
        "1-3 root",
    )
    content = replace_once(
        content,
        f"  m_Father: {{fileID: {MODEL_TR_13}}}",
        f"  m_Father: {{fileID: {ROOT_TR_13}}}",
        "1-3 planet",
    )
    content = remove_block(
        content,
        "--- !u!1 &7100000000000000003\n",
        "--- !u!1 &1577846143915004099\n",
    )
    return content


def main():
    for name, fn in [("1-2.prefab", revert_1_2), ("1-3.prefab", revert_1_3)]:
        path = ROOT / name
        path.write_text(fn(path.read_text(encoding="utf-8")), encoding="utf-8", newline="\n")
        print(f"Reverted {path}")


if __name__ == "__main__":
    main()
