"""Gera PNGs de placeholder para os skins de armas (geração real via IA pode falhar sem saldo)."""
import os
import zlib
import struct

BASE = r"C:\Users\pc\OneDrive\Desktop\Deadzone\Assets\Textures\Skins"


def make_png(path, color, size=64):
    r, g, b = color

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data +
                struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    ihdr = struct.pack(">IIBBBBB", size, size, 8, 2, 0, 0, 0)
    raw = b""
    row = bytes([r, g, b]) * size
    for _ in range(size):
        raw += b"\x00" + row
    data = zlib.compress(raw)
    out = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", data) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(out)


if __name__ == "__main__":
    os.makedirs(BASE, exist_ok=True)
    skins = {
        "Default.png": (160, 160, 160),
        "AmberRoyal.png": (212, 140, 35),
        "RustyTactical.png": (120, 70, 35),
        "NeonFury.png": (20, 240, 255),
    }
    for name, color in skins.items():
        make_png(os.path.join(BASE, name), color)
    print("PNGs criados em", BASE, os.listdir(BASE))
