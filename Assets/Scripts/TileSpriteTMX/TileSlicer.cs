using System.IO;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace TileSpriteTMX
{
    class TileSlicer
    {
        public List<Sprite> sprites = new List<Sprite>();

        public TileSlicer(Texture2D tex, int tileWidth, int tileHeight, int padding, int margin, float ppu)
        {
            tex.filterMode = FilterMode.Point;
            int tilesWide = Mathf.FloorToInt((tex.width - margin * 2) / (tileWidth + padding));
            int tilesTall = Mathf.FloorToInt((tex.height - margin * 2) / (tileHeight + padding));

            for (int tileY = 0; tileY < tilesTall; tileY++)
                for (int tileX = 0; tileX < tilesWide; tileX++)
                {
                    var x = tileX * (tileWidth + padding) + margin;
                    var y = (tex.height - (tileY * (tileHeight + padding)) + margin) - tileHeight;
                    var width = tileWidth;
                    var height = tileHeight;
                    var rect = new Rect(x, y, width, height);
                    sprites.Add(Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu));
                }
        }

    }
}

