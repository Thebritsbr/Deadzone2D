"""Gera placeholders OBJ (malhas simples de prateleira, importáveis pelo Unreal Engine)."""
import os

BASE = r"C:\Users\pc\OneDrive\Desktop\Deadzone\Assets\Guns\Models"
GUNS = ["Glock", "Mp40", "SniperAWM", "Ak47", "MiniUZI"]


def box_obj(filename, size=10):
    # Caixa simples: 8 vértices + 12 triângulos (box unity, escala por "size")
    def v(x, y, z):
        return f"v {x:.3f} {y:.3f} {z:.3f}"

    verts = []
    s = size / 2.0
    corners = [(x, y, z) for x in (-s, s) for y in (-s, s) for z in (-s, s)]
    for c in corners:
        verts.append(v(*c))
    faces = [
        (1, 3, 2), (1, 2, 4), (5, 6, 8), (5, 8, 7),
        (5, 1, 2), (5, 2, 6), (3, 4, 8), (3, 8, 7),
        (5, 7, 3), (5, 3, 1), (2, 6, 8), (2, 8, 4),
    ]
    with open(os.path.join(BASE, filename), "w", encoding="utf-8") as f:
        f.write(f"# Placeholder {filename} — substitui pelo modelo real exportado do Unreal.\n")
        f.write("\n".join(verts) + "\n\n")
        f.write("\n".join(f"f {a} {b} {c}" for a, b, c in faces) + "\n")


if __name__ == "__main__":
    os.makedirs(BASE, exist_ok=True)
    for name in GUNS:
        box_obj(f"{name}.obj", size=10 if name != "Glock" else 6)
    print("OBJs criados:", GUNS)
