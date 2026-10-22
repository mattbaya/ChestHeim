#!/usr/bin/env python3
"""Draws the VaultHeim Thunderstore icon (256 px) with the standard library only: package/icon.png"""
import math, os, struct, zlib

ROOT = os.path.join(os.path.dirname(__file__), "..")


def scene(x, y):
    layers = []
    d = math.hypot(x, y)
    t = min(1.0, d / 1.3)
    layers.append((22 - 14 * t, 16 - 9 * t, 44 - 26 * t, 1.0))

    # purple glow behind the chest
    g = max(0.0, 1 - math.hypot(x, y + 0.05) / 0.95)
    layers.append((150, 100, 255, 0.55 * g * g))

    def box(x0, x1, y0, y1, color, r=0.0):
        dx = max(x0 - x, 0, x - x1)
        dy = max(y0 - y, 0, y - y1)
        dist = math.hypot(dx, dy) - r
        a = max(0.0, min(1.0, 0.5 - dist / 0.012))
        if a > 0:
            layers.append((*color, a))

    # chest body and lid
    box(-0.52, 0.52, -0.5, 0.12, (112, 70, 42))
    box(-0.52, 0.52, -0.5, -0.42, (80, 50, 32))
    # lid: a half ellipse on top of the body
    ly = (y - 0.12) / 0.34
    lx = x / 0.52
    if y >= 0.12:
        e = math.hypot(lx, ly) - 1
        a = max(0.0, min(1.0, 0.5 - e / 0.03))
        if a > 0:
            layers.append((132, 86, 52, a))
    # iron bands
    for bx in (-0.34, 0.34):
        box(bx - 0.045, bx + 0.045, -0.5, 0.12, (78, 78, 92))
        if y >= 0.12 and abs(x - bx) < 0.045 and (x / 0.52) ** 2 + ((y - 0.12) / 0.34) ** 2 <= 1.0:
            layers.append((78, 78, 92, 1.0))
    box(-0.52, 0.52, 0.09, 0.14, (78, 78, 92))
    # lock plate and keyhole
    cd = math.hypot(x, y - 0.05)
    layers.append((236, 192, 72, max(0.0, min(1.0, (0.115 - cd) / 0.012 + 0.5))))
    kd = math.hypot(x, y - 0.075)
    layers.append((40, 28, 20, max(0.0, min(1.0, (0.03 - kd) / 0.012 + 0.5))))
    box(-0.012, 0.012, -0.0, 0.07, (40, 28, 20))

    # two linked rings at the top right: "the same vault everywhere"
    for cx, cy in ((0.58, 0.66), (0.76, 0.5)):
        rd = abs(math.hypot(x - cx, y - cy) - 0.13)
        layers.append((190, 150, 255, max(0.0, min(1.0, (0.03 - rd) / 0.012 + 0.5))))

    r = g_ = b = a = 0.0
    for (lr, lg, lb, la) in layers:
        out_a = la + a * (1 - la)
        if out_a <= 0:
            continue
        r = (lr * la + r * a * (1 - la)) / out_a
        g_ = (lg * la + g_ * a * (1 - la)) / out_a
        b = (lb * la + b * a * (1 - la)) / out_a
        a = out_a
    return r, g_, b, a


def render(size, samples=3):
    rows = []
    for py in range(size):
        row = bytearray()
        for px in range(size):
            acc = [0.0, 0.0, 0.0, 0.0]
            for sy in range(samples):
                for sx in range(samples):
                    x = ((px + (sx + 0.5) / samples) / size) * 2 - 1
                    y = 1 - ((py + (sy + 0.5) / samples) / size) * 2
                    r, g, b, a = scene(x, y)
                    acc[0] += r * a; acc[1] += g * a; acc[2] += b * a; acc[3] += a
            n = samples * samples
            if acc[3] > 0:
                row += bytes([min(255, int(acc[0] / acc[3])), min(255, int(acc[1] / acc[3])), min(255, int(acc[2] / acc[3])), min(255, int(acc[3] / n * 255))])
            else:
                row += bytes([0, 0, 0, 0])
        rows.append(bytes(row))
    return rows


def write_png(path, rows, size):
    raw = b"".join(b"\x00" + r for r in rows)

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "wb").write(png)
    print("wrote", path, len(png), "bytes")


write_png(os.path.join(ROOT, "package", "icon.png"), render(256), 256)
