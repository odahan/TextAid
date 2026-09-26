"""Build the Windows icon from the corrected TextAid PNG without changing its source."""

from pathlib import Path

from PIL import Image


ASSETS = Path(__file__).resolve().parent
SOURCE = ASSETS / "TextAid-icon.png"
DESTINATION = ASSETS / "TextAid.ico"
SIZES = (16, 24, 32, 48, 64, 128, 256)


def main() -> None:
    """Export all required Windows icon frames from the corrected PNG."""
    with Image.open(SOURCE) as source:
        if source.size != (512, 512):
            raise ValueError(f"Expected a 512 × 512 source PNG, got {source.size}")
        if source.mode != "RGBA":
            raise ValueError(f"Expected an RGBA source PNG, got {source.mode}")
        source.save(
            DESTINATION,
            format="ICO",
            sizes=[(size, size) for size in SIZES],
        )


if __name__ == "__main__":
    main()
