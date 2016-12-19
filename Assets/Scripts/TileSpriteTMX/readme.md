How to use:

1. Make a map in Tiled. (TODO insert link here). When using collisions, create them with the new Tiled Collision Editor (accessable under View > Tile Collision Editor).

2. Place your .tmx files somewhere in the Assets/StreamingAssets folder. For example, "Assets/StreamingAssets/Tilemaps/mymap.tmx". This is not necessary if you plan on using a custom path determined at runtime (see "Extra Tips" below), but works well for most use cases.

4. Add the ObjectPlacer script to a Gameobject, and drag-and-drop your tilesheets into the tilesheets list. Edit the path variable to the location relative to StreamingAssets. For example, "Tilemaps/mymap.tmx". Alternatively, you can give an absolute path (such as one that starts with a slash or backslash), but that is not recommended. You can also

Tips:
To ignore a layer or tile, add the custom property "ignore" and set it to "true".  

To make a collider into a trigger collider, add the custom property "isTrigger" and set it to "true"  

If you want to give some of your tile gameobjects some extra components (or have a custom material, or have a certain tag, etc.), put those components in a prefab and then put the prefab in the "Prefabs" list in the tileRenderer. Then add the custom property "basePrefab" and set it to the name of your prefab to the tiles you want to be based off that prefab. If for whatever reason you don't want the sprite from the tilemap to be added to the Gameobject, simply set the "noSprite" property to "true" or add your own sprite to the base prefab (if you have one).

Roadmap (order of importance)
1. ✓ Android/webplayer/uwp integration (still need to test)  
2. ✓ Flipx, flipy, flipdiagonal support  
3. ✓ Polygon collision support    
4. ✓ Box collision support    
5. ✓ Trigger colliders  
6. Object pooling
7. ✓ Polyline collision support    
8. ✓ Empty tile in tileset support  
9. ✓ Prefab creation  
10. ✓ Tile ignoring  
11. ✓ Helper functions (get tile properties, get tile form world position, etc.)  
12. Live updating  
13. Editor integration  
14. Support for Hexagonal, Isometric, and Staggered
15. Support for Render Order

Extra tips:

If you want to get the properties of a layer, use `getLayerProperties(string layer)`. To get the properties of a tile, use `getTileProperties(string layer, Vector3 gridlocation)` or `getTileProperties(string layer, int gridLocationX, int gridLocationY)`. For both of those, you need to remember to pass in the coordinates of the tile (i.e. 0,2 for the 2nd tile down from the 0th row). To convert World coordinates to tile coordinates, use `convertWorldPosToTilePos(Vector3 point)`.  

TileSprite uses a seperate Gameobject for each tile, although fortunately this is not an issue even for extremely large tilemaps (200,000 tiles ran fine on my computer) due to GPU instancing (colliders do add overhead - in the future this will be solved when I add Object Pooling). You can access the Gameobject for a tile with `getTileObject(string layer, Vector3 gridlocation)` or `getTileObject(string layer, int gridLocationX, int gridLocationY)`. Note that even if you move the Gameobject, you still must access it with the gridlocation it started out with.

By default, the map will be created on the first frame the object with the tilemap script is enabled, but this can be changed with the "Create On Awake" checkbox. If this is disabled, you can either create the tilemap with `createTilemap()` or `createTilemap(string path)` if you wish to use a custom path. The file is accessed with Unity's WWW library, so you can even download the tilemap from the web if you so choose.
