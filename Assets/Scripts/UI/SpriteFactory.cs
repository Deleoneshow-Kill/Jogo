
using System.Collections.Generic;
using UnityEngine;

namespace CleanRPG.UI
{
    public static class SpriteFactory
    {
        private static Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite GetCircle(string key, Color col, int size=64)
        {
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cx = size/2f; var cy = size/2f; var r = size/2f - 1f;
            var r2 = r*r;
            for (int y=0;y<size;y++)
            for (int x=0;x<size;x++)
            {
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;
                float d2 = dx*dx + dy*dy;
                if (d2 <= r2) tex.SetPixel(x,y,col);
                else tex.SetPixel(x,y, new Color(0,0,0,0));
            }
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100f);
            cache[key] = sp;
            return sp;
        }
    }
}
