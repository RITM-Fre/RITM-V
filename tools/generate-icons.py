#!/usr/bin/env python3
"""Pure-python generator for the RITM V icon set (no third-party deps).

Renders a 1024px master canvas and area-downsamples it into every size an .ico
needs, then writes the icons straight into the project folders:

    RITMV/RITMV/Resources/            (WPF)
    RITMV/RITMV.Desktop/Assets/       (Avalonia)

Run:  python tools/generate-icons.py
"""
import math
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)

WPF_OUT = os.path.join(REPO, "RITMV", "RITMV", "Resources")
AVA_OUT = os.path.join(REPO, "RITMV", "RITMV.Desktop", "Assets")
PREVIEW_OUT = os.path.join(HERE, "output")

HI = 1024  # high-res master canvas

# gradient endpoints (violet -> cyan)
G_FROM = (124, 58, 237)
G_TO = (34, 211, 238)


def lerp(a, b, t):
    return a + (b - a) * t


def rounded_rect(x, y, l, t, r, b, rad):
    if x < l or x > r or y < t or y > b:
        return False
    cx = l + rad if x < l + rad else (r - rad if x > r - rad else x)
    cy = t + rad if y < t + rad else (b - rad if y > b - rad else y)
    dx = x - cx
    dy = y - cy
    return dx * dx + dy * dy <= rad * rad


def seg_dist(px, py, ax, ay, bx, by):
    vx, vy = bx - ax, by - ay
    wx, wy = px - ax, py - ay
    d = vx * vx + vy * vy
    if d <= 1e-12:
        return math.hypot(wx, wy)
    t = max(0.0, min(1.0, (wx * vx + wy * vy) / d))
    return math.hypot(px - (ax + t * vx), py - (ay + t * vy))


def render_master(size=HI, grad=None):
    """Render the icon at `size` px. `grad` overrides the (from, to) gradient colours."""
    g_from, g_to = grad if grad else (G_FROM, G_TO)
    buf = bytearray(size * size * 4)

    # geometry (unit square)
    L, T, B, Rr = 0.300, 0.255, 0.745, 0.700
    sw = 0.100
    ro = (Rr - L) / 2.0
    cx = L + sw / 2.0
    cy = T + ro
    ri = ro - sw
    leg_a = (cx + ri * 0.15, cy + ro * 0.45)
    leg_b = (Rr - 0.008, B)
    leg_hw = sw / 2.0

    for py in range(size):
        y = (py + 0.5) / size
        row = py * size * 4
        for px in range(size):
            x = (px + 0.5) / size
            o = row + px * 4

            if not rounded_rect(x, y, 0.045, 0.045, 0.955, 0.955, 0.215):
                continue

            t = (x + y) / 2.0
            br = int(lerp(g_from[0], g_to[0], t))
            bg = int(lerp(g_from[1], g_to[1], t))
            bb = int(lerp(g_from[2], g_to[2], t))

            # subtle inner vignette so the tile reads as glassy
            vx = (x - 0.5) * 2.0
            vy = (y - 0.5) * 2.0
            v = 1.0 - 0.22 * min(1.0, (vx * vx + vy * vy) / 2.0)
            br, bg, bb = int(br * v), int(bg * v), int(bb * v)

            # --- glyph "R" -------------------------------------------------
            inside = False
            if L <= x <= L + sw and T <= y <= B:
                inside = True
            if not inside:
                dx = x - cx
                dy = y - cy
                if dx >= 0.0 and T <= y <= T + 2 * ro:
                    d = math.hypot(dx, dy)
                    if ri <= d <= ro:
                        inside = True
            if not inside and y >= leg_a[1] - leg_hw * 0.6:
                if seg_dist(x, y, leg_a[0], leg_a[1], leg_b[0], leg_b[1]) <= leg_hw:
                    inside = True

            if inside:
                br, bg, bb = 255, 255, 255

            buf[o] = br
            buf[o + 1] = bg
            buf[o + 2] = bb
            buf[o + 3] = 255

    return buf


def downsample(src, sw_, sh, tw, th):
    out = bytearray(tw * th * 4)
    xr = sw_ / tw
    yr = sh / th
    for ty in range(th):
        y0 = int(ty * yr)
        y1 = max(y0 + 1, int((ty + 1) * yr))
        for tx in range(tw):
            x0 = int(tx * xr)
            x1 = max(x0 + 1, int((tx + 1) * xr))
            r = g = b = a = 0
            n = 0
            for sy in range(y0, min(y1, sh)):
                base = sy * sw_ * 4
                for sx in range(x0, min(x1, sw_)):
                    o = base + sx * 4
                    al = src[o + 3]
                    r += src[o] * al
                    g += src[o + 1] * al
                    b += src[o + 2] * al
                    a += al
                    n += 1
            o = (ty * tw + tx) * 4
            if a > 0:
                out[o] = min(255, r // a)
                out[o + 1] = min(255, g // a)
                out[o + 2] = min(255, b // a)
            out[o + 3] = a // n if n else 0
    return out


def write_png(path, w, h, rgba):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        raw += rgba[y * w * 4:(y + 1) * w * 4]
    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def write_ico(path, images):
    """images: list of (size, rgba_bytes)"""
    n = len(images)
    header = struct.pack("<HHH", 0, 1, n)
    entries = b""
    payload = b""
    offset = 6 + 16 * n
    for size, rgba in images:
        # BGRA bottom-up XOR bitmap
        xor = bytearray()
        for y in range(size - 1, -1, -1):
            base = y * size * 4
            for x in range(size):
                o = base + x * 4
                xor += bytes((rgba[o + 2], rgba[o + 1], rgba[o], rgba[o + 3]))
        # 1bpp AND mask, rows padded to 4 bytes
        row_bytes = ((size + 31) // 32) * 4
        andmask = bytearray()
        for y in range(size - 1, -1, -1):
            bits = bytearray(row_bytes)
            base = y * size * 4
            for x in range(size):
                if rgba[base + x * 4 + 3] < 128:
                    bits[x // 8] |= 0x80 >> (x % 8)
            andmask += bits

        bih = struct.pack("<IiiHHIIiiII", 40, size, size * 2, 1, 32, 0,
                          len(xor) + len(andmask), 0, 0, 0, 0)
        data = bih + bytes(xor) + bytes(andmask)
        w = 0 if size >= 256 else size
        entries += struct.pack("<BBBBHHII", w, w, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
        payload += data

    with open(path, "wb") as f:
        f.write(header + entries + payload)


SIZES = [256, 128, 64, 48, 32, 24, 16]


def build(name, path_ico, grad=None, preview=False):
    master = render_master(HI, grad)
    imgs = []
    for s in SIZES:
        imgs.append((s, downsample(master, HI, HI, s, s)))
    os.makedirs(os.path.dirname(path_ico), exist_ok=True)
    write_ico(path_ico, imgs)
    if preview:
        os.makedirs(PREVIEW_OUT, exist_ok=True)
        big = downsample(master, HI, HI, 256, 256)
        write_png(os.path.join(PREVIEW_OUT, f"preview-{name}.png"), 256, 256, big)
    print("wrote", os.path.relpath(path_ico, REPO))


def build_all():
    """Writes the icon set into both the WPF and the Avalonia project folders."""
    for root in (WPF_OUT, AVA_OUT):
        build("RITMV", os.path.join(root, "RITMV.ico"),
              preview=(root == WPF_OUT))
        # tray variants: the tile tint encodes the system-proxy state
        build("notify1", os.path.join(root, "NotifyIcon1.ico"), grad=((100, 116, 139), (51, 65, 85)))
        build("notify2", os.path.join(root, "NotifyIcon2.ico"), grad=((34, 197, 94), (13, 148, 136)))
        build("notify3", os.path.join(root, "NotifyIcon3.ico"), grad=((245, 158, 11), (234, 88, 12)))
        build("notify4", os.path.join(root, "NotifyIcon4.ico"), grad=((56, 189, 248), (79, 70, 229)))

    # Avalonia also ships a png
    m = render_master(HI)
    write_png(os.path.join(AVA_OUT, "RITMV.png"), 256, 256,
              downsample(m, HI, HI, 256, 256))
    print("wrote", os.path.relpath(os.path.join(AVA_OUT, "RITMV.png"), REPO))


if __name__ == "__main__":
    build_all()
    print("done")
