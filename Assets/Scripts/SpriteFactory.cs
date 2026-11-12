using UnityEngine;

/// <summary>
/// Genera sprites por código para no depender de imágenes externas.
/// Cada sprite mide 1 unidad de mundo (pixelsPerUnit = tamaño).
/// </summary>
public static class SpriteFactory
{
    static Sprite Build(int size, System.Func<float, float, Color> pixel)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Coordenadas normalizadas de -1 a 1
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = pixel(u, v);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static Sprite Circle(int size = 64)
    {
        return Build(size, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            float a = Mathf.Clamp01((1f - d) * size * 0.5f);
            return new Color(1, 1, 1, a);
        });
    }

    public static Sprite Ship(int size = 64)
    {
        // Triángulo con alas y una cabina
        return Build(size, (u, v) =>
        {
            bool body = v > -0.8f && Mathf.Abs(u) < (1f - v) * 0.5f && v < 0.95f;
            bool wings = v > -0.8f && v < -0.2f && Mathf.Abs(u) < 0.9f - (v + 0.8f) * 0.6f;
            bool cockpit = (u * u) / 0.02f + ((v - 0.15f) * (v - 0.15f)) / 0.06f < 1f;
            if (cockpit) return new Color(0.55f, 0.9f, 1f, 1f);
            if (body) return new Color(0.92f, 0.95f, 1f, 1f);
            if (wings) return new Color(0.95f, 0.35f, 0.4f, 1f);
            return Color.clear;
        });
    }

    public static Sprite Meteor(int size = 64, int seed = 0)
    {
        var rnd = new System.Random(seed);
        float[] bumps = new float[8];
        for (int i = 0; i < bumps.Length; i++) bumps[i] = 0.8f + (float)rnd.NextDouble() * 0.2f;
        Vector2[] craters =
        {
            new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f),
            new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f),
        };

        return Build(size, (u, v) =>
        {
            float ang = (Mathf.Atan2(v, u) / (2 * Mathf.PI) + 1f) % 1f * bumps.Length;
            int i0 = (int)ang % bumps.Length;
            int i1 = (i0 + 1) % bumps.Length;
            float radius = Mathf.Lerp(bumps[i0], bumps[i1], ang - Mathf.Floor(ang));
            float d = Mathf.Sqrt(u * u + v * v);
            if (d > radius) return Color.clear;

            float shade = 0.75f + 0.25f * (-u - v) * 0.5f; // luz desde arriba a la izquierda
            Color c = new Color(0.55f, 0.42f, 0.33f) * shade;
            foreach (var cr in craters)
                if ((new Vector2(u, v) - cr).sqrMagnitude < 0.05f) c *= 0.7f;
            c.a = 1f;
            return c;
        });
    }

    public static Sprite Star(int size = 64)
    {
        // Estrella de 5 puntas
        Vector2[] poly = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? 0.95f : 0.42f;
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            poly[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        return Build(size, (u, v) =>
        {
            if (!InsidePolygon(poly, u, v)) return Color.clear;
            float d = Mathf.Sqrt(u * u + v * v);
            return Color.Lerp(new Color(1f, 1f, 0.8f), new Color(1f, 0.8f, 0.1f), d);
        });
    }

    static bool InsidePolygon(Vector2[] poly, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > y) != (poly[j].y > y) &&
                x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }
}
