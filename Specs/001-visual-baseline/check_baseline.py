"""Check that the visual reference contains every required color and brand asset."""

import hashlib
import re
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[2]
LOT = Path(__file__).resolve().parent
NAMES = (
    "Background", "Surface", "SurfaceAlt", "Foreground", "ForegroundMuted",
    "Border", "Accent", "AccentHover", "AccentPressed", "Success", "Warning",
    "Error", "Disabled", "Selection",
)
FILES = (
    ROOT / "assets/Logo-final.png",
    ROOT / "assets/Logo-large.png",
    ROOT / "assets/TextAid-icon.svg",
    ROOT / "assets/TextAid-icon.png",
    ROOT / "assets/TextAid.ico",
    LOT / "VISUAL-BASELINE.md",
    LOT / "reference.html",
)


def main() -> None:
    """Validate the baseline and print a content identity for gate evaluation."""
    document = (LOT / "VISUAL-BASELINE.md").read_text(encoding="utf-8")
    reference = (LOT / "reference.html").read_text(encoding="utf-8")
    for name in NAMES:
        documented = re.search(rf"\| {name} \| `#([0-9A-Fa-f]{{6}})`", document)
        rendered = re.search(rf"--{name}: #([0-9A-Fa-f]{{6}});", reference)
        assert documented is not None and rendered is not None, f"Missing {name}"
        assert documented.group(1).lower() == rendered.group(1).lower(), f"Mismatch: {name}"

    for path in FILES:
        assert path.is_file() and path.stat().st_size > 0, f"Missing {path}"
    assert "Logo-final.png" in document and "TextAid.ico" in document
    assert "Logo-final.png" in reference and "TextAid-icon.svg" in reference

    ET.parse(ROOT / "assets/TextAid-icon.svg")
    with Image.open(ROOT / "assets/TextAid.ico") as icon:
        assert {16, 24, 32, 48, 64, 128, 256}.issubset(
            {size[0] for size in icon.info["sizes"]}
        ), "Icon frame sizes missing"
        frame = icon.ico.getimage((256, 256)).convert("RGBA")
    with Image.open(ROOT / "assets/TextAid-icon.png") as preview:
        assert preview.size == (512, 512)
        expected = preview.convert("RGBA").resize((256, 256), Image.Resampling.LANCZOS)
        assert ImageChops.difference(frame, expected).getbbox() is None, "ICO does not match PNG"

    digest = hashlib.sha256()
    for path in FILES:
        digest.update(path.relative_to(ROOT).as_posix().encode("utf-8"))
        digest.update(path.read_bytes())
    print("PASS: 14 matching tokens, 7 assets/files, SVG syntax, ICO frames and PNG match")
    print(f"Result SHA-256: {digest.hexdigest()}")


if __name__ == "__main__":
    main()
