#!/usr/bin/env python3
"""Batch-generate localized StoryArc narration as MP3 files with Edge TTS.

From the Unity project root in PowerShell:
  py -m pip install --user edge-tts
  py Assets/Tools/generate_story_narration.py

This approach needs an internet connection but no cloud project, API key, or
billing account. It writes only Narration_*.mp3 files into StoryVoice; dialogue
files are kept. Edge TTS is an unofficial client of Microsoft's online service.
"""

from __future__ import annotations

import json
import re
import asyncio
from pathlib import Path

import edge_tts


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets" / "Scripts" / "Gameplay" / "StoryArc.cs"
OUTPUT = ROOT / "StoryVoice"
LANGUAGES = [
    ("English", "en-US-AriaNeural"),
    ("Malayalam", "ml-IN-SobhanaNeural"),
    ("Indonesian", "id-ID-GadisNeural"),
    ("TraditionalChinese", "zh-TW-HsiaoChenNeural"),
]
NARRATION_KEYS = (0, 1, 2, 5, 7, 8, 9, 11, 12, 14, 15, 17, 18, 19, 20)


def get_story_lines() -> list[list[str]]:
    source = SOURCE.read_text(encoding="utf-8")
    start = source.index("private static readonly string[][] Lines =")
    end = source.index("\n    };", start)
    rows = re.findall(r"new\[\]\s*\{(.*?)\}", source[start:end], re.DOTALL)
    result = []
    for row in rows:
        literals = re.findall(r'"(?:\\.|[^"\\])*"', row)
        result.append([json.loads(value) for value in literals])
    if len(result) != 4 or any(len(row) != 21 for row in result):
        raise RuntimeError(f"Expected 4 languages x 21 lines in {SOURCE}; found {[len(r) for r in result]}")
    return result


async def main() -> None:
    localized = get_story_lines()
    total = 0
    for row, (folder, voice) in zip(localized, LANGUAGES):
        destination = OUTPUT / folder
        destination.mkdir(parents=True, exist_ok=True)
        print(f"{folder}: using {voice}")
        for key in NARRATION_KEYS:
            filename = destination / f"Narration_{key:02d}.mp3"
            communicate = edge_tts.Communicate(row[key], voice, rate="-6%")
            await communicate.save(str(filename))
            total += 1
            print(f"  wrote {filename.relative_to(ROOT)}")
    print(f"Generated {total} narration MP3s under {OUTPUT}")


if __name__ == "__main__":
    asyncio.run(main())
