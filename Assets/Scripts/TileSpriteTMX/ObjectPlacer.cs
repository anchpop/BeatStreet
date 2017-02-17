using System.IO;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TiledSharp;

namespace TileSpriteTMX
{
    public class ObjectPlacer : MonoBehaviour
    {
        public string TmxPath = null;
        public bool createMapOnAwake = true;
        public float pixelsPerUnit = 100;
        public List<Texture2D> tileSheets = new List<Texture2D>();
        public List<GameObject> prefabs = new List<GameObject>();

        public bool combineColliders = false; // use with caution
        Dictionary<TmxLayer, List<List<Vector2>>> unsimplifiedPolygons = new Dictionary<TmxLayer, List<List<Vector2>>>();

        List<Sprite> spriteList = new List<Sprite> { null };
        Dictionary<int, TmxTilesetTile> tileIdMap = new Dictionary<int, TmxTilesetTile>();
        Dictionary<TmxLayerTile, GameObject> tileGameobjectMap = new Dictionary<TmxLayerTile, GameObject>();

        Dictionary<string, string> customProperties = new Dictionary<string, string> { { "don't render", "noSprite" },
        { "use base prefab", "basePrefab" },
        { "ignore", "ignore" },
        { "is trigger", "isTrigger" },
        { "unityLayer", "unityLayer" },
        { "used by effector", "usedByEffector" }};

        TmxMap map = null;

        void Start()
        {
            if (createMapOnAwake)
            {
                var url = convertPath(TmxPath);
                createTilemap(url);
            }
        }



        void refreshMap()
        {
            createTilemap();
        }


        public void createTilemap(string path)
        {
            StartCoroutine(RenderMap(path));
        }

        public void createTilemap()
        {
            StartCoroutine(RenderMap(convertPath(TmxPath)));
        }

        IEnumerator RenderMap(string path)
        {
            var tmxFile = new WWW(path);
            yield return tmxFile;
            if (tmxFile.text == "") throw new System.Exception("The file at " + path + " could not be found");
            map = new TmxMap(XDocument.Parse(tmxFile.text));
            if (map != null) Debug.Log("Map file found and parsed");


            if (map.Orientation != OrientationType.Orthogonal) throw new System.Exception("Modes other than Orthagonal are not supported at this time");

            
            foreach (var tileSet in map.Tilesets) // we need to get the tilesheets, slice them up into sprites, and put them in spriteList
            {
                var sheets = tileSheets.Where(c => c.name == tileSet.Name).ToList();
                if (sheets.Count == 0) throw new System.Exception("Couldn't find tileset " + tileSet.Name + " in the tileSheets list");
                var sheet = sheets[0];


                var slicer = new TileSlicer(sheet, tileSet.TileWidth, tileSet.TileHeight, tileSet.Spacing, tileSet.Margin, pixelsPerUnit);
                spriteList.AddRange(slicer.sprites);

                
                foreach (var tile in tileSet.Tiles.Values)
                {
                    tileIdMap[tile.Id + tileSet.FirstGid] = tile; // Gids are the first gid of the tileset plus the tile Id
                }

            }


            for (int layerindex = 0; layerindex < map.Layers.Count; layerindex++)
            {
                var layer = map.Layers[layerindex];
                if (!(getLayerProperty(layer.Name, customProperties["ignore"]).ToLower() == "true")) // if they've opted to ignore the layer
                {
                    var lparent = new GameObject(layer.Name); // object to parent the tiles created under
                    lparent.transform.position = transform.position; lparent.transform.SetParent(transform);

                    foreach (var tile in layer.Tiles)
                    {
                        if (tile.Gid > 0)
                        {
                            var tex = spriteList[tile.Gid];
                            var destx = tile.X * (map.TileWidth / pixelsPerUnit);
                            var desty = -tile.Y * (map.TileWidth / pixelsPerUnit);
                            var destPos = new Vector3(destx, desty, 0);
                            var go = addTileObject(tile, tex, layer, destPos, layerindex);
                            if (go)
                            {
                                go.transform.SetParent(lparent.transform);
                                go.transform.position = destPos + lparent.transform.position;
                            }
                        }
                    }
                    if (unsimplifiedPolygons.ContainsKey(layer) && unsimplifiedPolygons[layer].Count != 0)
                    {
                        if (layer.Properties.ContainsKey(customProperties["unityLayer"]))
                        {
                            string unityLayer = layer.Properties[customProperties["unityLayer"]];
                            if (LayerMask.NameToLayer(unityLayer) != -1)  // undocumented behavoir here, NameToLayer returns -1 if layer not found
                                lparent.layer = LayerMask.NameToLayer(unityLayer);
                            else
                                throw new System.Exception("Unable to find layer named " + unityLayer);
                        }

                        var simplifiedPolygons = ColliderSimplification.UniteCollisionPolygons(unsimplifiedPolygons[layer]);
                        var polCol = lparent.AddComponent<PolygonCollider2D>();
                        polCol.pathCount = simplifiedPolygons.Count;
                        simplifiedPolygons.Each((gon, index) =>  polCol.SetPath(index, gon.ToArray()) );
                    }
                }
            }
        }

        GameObject addTileObject(TmxLayerTile tile, Sprite tex, TmxLayer layer, Vector3 destPos, int orderInLayer = 0)
        {
            if (tile.Gid > 0 && !(getTileProperty(layer.Name, tile.X, tile.Y, customProperties["ignore"]).ToLower() == "true"))
            {
                // determine whether we should flip horisontally, flip vertically, or rotate 90 degrees
                bool flipX = tile.HorizontalFlip;
                bool flipY = tile.VerticalFlip;
                bool rotate90 = tile.DiagonalFlip;
                if (rotate90)
                {
                    Swap(ref flipX, ref flipY);
                    flipX = !flipX;
                }

                var go = getBaseGameobject(tile);
                go.transform.Rotate(new Vector3(0, 0, rotate90 ? 90 : 0));

                var sprend = go.GetComponent<SpriteRenderer>();
                if (!(getTileProperty(layer.Name, tile.X, tile.Y, customProperties["don't render"]).ToLower() == "true"))
                {
                    if (sprend.sprite == null) sprend.sprite = tex;
                    sprend.flipX = flipX;
                    sprend.flipY = flipY;
                    sprend.sortingOrder = orderInLayer;
                }



                if (tileIdMap.ContainsKey(tile.Gid)) // if we have special information about that tile (such as collision information or tile properties)
                {
                    if (tileIdMap[tile.Gid].Properties.ContainsKey(customProperties["unityLayer"]))
                        go.layer = LayerMask.NameToLayer(tileIdMap[tile.Gid].Properties[customProperties["unityLayer"]]);
                    foreach (TmxObjectGroup oGroup in tileIdMap[tile.Gid].ObjectGroups)
                    {
                        foreach (TmxObject collisionObject in oGroup.Objects)
                        {
                            if (collisionObject.ObjectType == TmxObjectType.Basic) // add a box collider, square object
                            {
                                addBoxCollider(go, collisionObject, flipX, flipY, rotate90, layer, destPos);
                            }
                            else if (collisionObject.ObjectType == TmxObjectType.Polygon)
                            {
                                addPolygonCollider(go, collisionObject, flipX, flipY, rotate90, layer, destPos);
                            }
                            else if (collisionObject.ObjectType == TmxObjectType.Polyline)
                            {
                                addEdgeCollider(go, collisionObject, flipX, flipY);
                            }
                        }
                    }
                }

                tileGameobjectMap[tile] = go;
                return go;
            }
            return null;
        }

        void addBoxCollider(GameObject go, TmxObject collisionObject, bool flipX, bool flipY, bool rotate90, TmxLayer layer, Vector3 destPos)
        {
            bool trigger = collisionObject.Properties.ContainsKey(customProperties["is trigger"]) && collisionObject.Properties[customProperties["is trigger"]].ToLower() == "true";


            // Don't forget we have to convert from pixels to unity units!
            float width = (float)collisionObject.Width / pixelsPerUnit;
            float height = (float)collisionObject.Height / pixelsPerUnit;

            float centerXPos = (float)((flipX ? -1 : 1) * (collisionObject.X * 2 + collisionObject.Width - map.TileWidth) / pixelsPerUnit);
            float centerYPos = (float)((flipY ? -1 : 1) * -(collisionObject.Y * 2 + collisionObject.Height - map.TileHeight) / pixelsPerUnit); // the positive y cord in Tiled goes down so we have to flip it
            if (trigger || !combineColliders)
            {
                var boxCol = go.AddComponent<BoxCollider2D>();
                boxCol.offset = new Vector2((float)centerXPos, (float)centerYPos) / 2;
                boxCol.size = new Vector2(width, height);

                boxCol.isTrigger = trigger;
                boxCol.usedByEffector = true;
            }
            else
            {
                var pos = new Vector2(destPos.x, destPos.y);
                var offset = new Vector2((flipX ? -1 : 1) * centerXPos, (flipY ? -1 : 1) * centerYPos) / pixelsPerUnit;
                var verticies = new List<Vector2>();
                verticies.Add(new Vector2( width / 2,  height / 2));
                verticies.Add(new Vector2(-width / 2,  height / 2));
                verticies.Add(new Vector2(-width / 2, -height / 2));
                verticies.Add(new Vector2( width / 2, -height / 2));
                verticies = verticies.Select(v => new Vector2((flipX ? -1 : 1) * v.x, (flipY ? -1 : 1) * v.y)).ToList();
                verticies = verticies.Select(v => v + offset).ToList();
                if (rotate90) verticies = verticies.Select(v => new Vector2(-v.y, v.x)).ToList();
                verticies = verticies.Select(v => v + pos).ToList();

                if (!unsimplifiedPolygons.ContainsKey(layer))
                    unsimplifiedPolygons[layer] = new List<List<Vector2>>();
                unsimplifiedPolygons[layer].Add(verticies);
            }
        }
        void addPolygonCollider(GameObject go, TmxObject collisionObject, bool flipX, bool flipY, bool rotate90, TmxLayer layer, Vector3 destPos)
        {
            bool trigger = collisionObject.Properties.ContainsKey(customProperties["is trigger"]) && collisionObject.Properties[customProperties["is trigger"]].ToLower() == "true";


            // set the path of the polygon collider
            // we must convert the TmxPoints to Vector2s
            var XPos = (flipX ? -1 : 1) * (float)(collisionObject.X - map.TileWidth / 2);
            var YPos = (flipY ? -1 : 1) * -(float)(collisionObject.Y - map.TileHeight / 2); // the positive y cord in Tiled goes down so we have to flip it
            var offset = new Vector2(XPos, YPos) / pixelsPerUnit;
            var points = collisionObject.Points.Select(p => new Vector2((flipX ? -1 : 1) * ((float)p.X),
                                                                              (flipY ? -1 : 1) * -((float)p.Y)) / pixelsPerUnit);
            if (trigger || !combineColliders)
            {
                var polCol = go.AddComponent<PolygonCollider2D>();
                polCol.SetPath(0, points.ToArray());

                polCol.offset = offset;
                polCol.isTrigger = trigger;
                polCol.usedByEffector = true;
            }
            else
            {
                //var pos = new Vector2(destPos.x + map.TileWidth / 2 / pixelsPerUnit, destPos.y - map.TileHeight / 2 / pixelsPerUnit);
                var pos = new Vector2(destPos.x, destPos.y);
                var verticies = points.Select(p => p + offset).ToList();
                if (rotate90) verticies = verticies.Select(v => new Vector2(-v.y, v.x)).ToList();
                verticies = verticies.Select(p => p + pos).ToList();

                if (!unsimplifiedPolygons.ContainsKey(layer))
                    unsimplifiedPolygons[layer] = new List<List<Vector2>>();
                unsimplifiedPolygons[layer].Add(verticies);
            }
        }
        void addEdgeCollider(GameObject go, TmxObject collisionObject, bool flipX, bool flipY)
        {
            bool trigger = collisionObject.Properties.ContainsKey(customProperties["is trigger"]) && collisionObject.Properties[customProperties["is trigger"]].ToLower() == "true";


            // set the path of the polygon collider
            // we must convert the TmxPoints to Vector2s
            var XPos = (flipX ? -1 : 1) * (float)(collisionObject.X - map.TileWidth / 2);
            var YPos = (flipY ? -1 : 1) * -(float)(collisionObject.Y - map.TileHeight / 2); // the positive y cord in Tiled goes down so we have to flip it

            var edgeCol = go.AddComponent<EdgeCollider2D>();
            edgeCol.points = collisionObject.Points.Select(p => new Vector2((flipX ? -1 : 1) * ((float)p.X),
                                                                            (flipY ? -1 : 1) * -((float)p.Y)) / pixelsPerUnit).ToArray();

            edgeCol.offset = new Vector2(XPos, YPos) / pixelsPerUnit;
            edgeCol.isTrigger = trigger;
            edgeCol.usedByEffector = true;
        }



        GameObject getBaseGameobject(TmxLayerTile tile)
        {
            GameObject go = null;
            if (tileIdMap.ContainsKey(tile.Gid) &&
                tileIdMap[tile.Gid].Properties.ContainsKey(customProperties["use base prefab"]))
            {
                var fabs = prefabs.Where(p => p.name == tileIdMap[tile.Gid].Properties[customProperties["use base prefab"]]).ToList();
                if (fabs.Count != 0)
                    go = Instantiate(fabs.First());
                else
                    go = new GameObject("Tile");
            }
            else
                go = new GameObject("Tile");

            if (go.GetComponent<SpriteRenderer>() == null &&
                !(tileIdMap.ContainsKey(tile.Gid) &&
                tileIdMap[tile.Gid].Properties.ContainsKey(customProperties["don't render"]) &&
                (tileIdMap[tile.Gid].Properties[customProperties["don't render"]].ToLower() != "true")))
                go.AddComponent<SpriteRenderer>();

            return go;
        }



        public TmxLayer getTmxLayer(string layer)
        {
            return map.Layers[layer];
        }

        public Vector2 convertWorldPosToTilePos(Vector3 point)
        {
            Vector3 p = (point - transform.position) * pixelsPerUnit;
            return new Vector3(Mathf.Floor(p.x / map.TileWidth), Mathf.Floor(p.y / map.TileHeight), 0); // Round the vector to get those nice integer values
        }


        public Dictionary<string, string> getLayerProperties(string layer)
        {
            return getTmxLayer(layer).Properties;
        }
        public string getLayerProperty(string layer, string property)  // returns "" if property does not exist
        {
            return getLayerProperties(layer).GetValueOrDefault(property, "");
        }

        public Dictionary<string, string> getTileProperties(string layer, Vector3 gridlocation)
        {
            return getTileProperties(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y));
        }
        public Dictionary<string, string> getTileProperties(string layer, int gridLocationX, int gridLocationY)
        {
            if (tileIdMap.ContainsKey(getTmxTile(layer, gridLocationX, gridLocationY).Gid)) return tileIdMap[getTmxTile(layer, gridLocationX, gridLocationY).Gid].Properties;
            else return new Dictionary<string, string>();
        }
        public string getTileProperty(string layer, Vector3 gridlocation, string property) // returns "" if property does not exist
        {
            return getTileProperty(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y), property);
        }
        public string getTileProperty(string layer, int gridLocationX, int gridLocationY, string property) // returns "" if property does not exist
        {
            //Debug.Log(getTileProperties(layer, gridLocationX, gridLocationY).GetValueOrDefault(property, "est"));
            return getTileProperties(layer, gridLocationX, gridLocationY).GetValueOrDefault(property, "");
        }



        public TmxLayerTile getTmxTile(string layer, Vector3 gridlocation)
        {        
            return getTmxTile(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y));
        }
        public TmxLayerTile getTmxTile(string layer, int gridLocationX, int gridLocationY)
        {
            var tilesFound = getTmxLayer(layer).Tiles.ToList().Where(t => t.X == gridLocationX && t.Y == gridLocationY).ToList();
            if (tilesFound.Count == 0) throw new Exception("There were no tiles found at (" + gridLocationX + ", " + gridLocationY + ")");
            return tilesFound.First();
        }


        public GameObject getTileObject(string layer, Vector3 gridlocation)
        {
            return getTileObject(layer, Mathf.FloorToInt(gridlocation.x), Mathf.FloorToInt(gridlocation.y));
        }
        public GameObject getTileObject(string layer, int gridLocationX, int gridLocationY)
        {
            return tileGameobjectMap[getTmxTile(layer, gridLocationX, gridLocationY)];
        }




        public string convertPath(string path)
        {
            if (TmxPath == null || TmxPath == "") throw new System.Exception("Invalid path for TMX file");
            var url = TmxPath;
            if (!url.Contains("://")) // make sure they haven't already added the "file://" or "www://" 
            {
                url = Path.Combine(Application.streamingAssetsPath, TmxPath);

#if UNITY_EDITOR || !UNITY_ANDROID // Android doesn't need "file://" added because Application.streamingAssetsPath comes with "file:"
                url = "file://" + url;
#endif
            }

            return url;
        }


        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }



    }
}