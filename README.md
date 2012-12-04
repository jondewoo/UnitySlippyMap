UnitySlippyMap
==============

A slippy map implementation written in C# for Unity3D.

It aims at helping developpers create 2D/3D maps working with a variety of online tile providers (OpenStreetMap, VirtualEarth/Bing Maps, ...) and offline sources (DBMap, MBTiles, ...) like [Route-me](https://github.com/route-me/route-me) (iOS) or [Leaflet](http://leaflet.cloudmade.com/) (HTML5), on every platform supported by Unity3D.
Google Maps won't be supported (see [Google Maps tiles terms of service](https://developers.google.com/maps/faq#tos_tiles)).
Yahoo! Maps neither since it was [closed on September 13, 2011](http://developer.yahoo.com/blogs/ydn/posts/2011/06/yahoo-maps-apis-service-closure-announcement-new-maps-offerings-coming-soon/).
Nokia Maps (now called [Here](http://developer.here.net/)) provides a REST API designed for static maps. They could be used as tiles but would get a 'Nokia' watermark on each one of them. Also Nokia and Microsoft are now [teaming up](http://www.microsoft.com/en-us/news/download/presskits/bing/docs/MSBingMapsFS.docx) so supporting both might be redundant.

Hopefully, it will also be compliant with a number of popular [OGC](http://www.opengeospatial.org/) standards like WMS or GML.

Map objects (tiles, markers, ...) are placed in 3D space along X and Z axis. The idea is to be able to use 3D geometry as terrain or markers with a free camera.

Current status
--------------

UnitySlippyMap is in alpha stage and currently supports:
  * [OSM tiles](http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames) from providers such as [OpenStreetMap](http://www.openstreetmap.org/), [MapQuest](http://www.mapquest.com/) or [CloudMade](http://cloudmade.com/)
  * [WMS tiles](http://en.wikipedia.org/wiki/Web_Map_Service)
  * [VirtualEarth/Bing maps tiles](http://www.microsoft.com/maps/)
  * [MBTiles databases](http://mapbox.com/developers/mbtiles/)

It is tested in Unity3D Editor 3.5.6f4 & 4.0.0, and on iOS (beware the [Unity 4 GPS bug](http://forum.unity3d.com/threads/159257-Unity-4.0-iOS-GPS-Fix)) and Android devices.

See the [TODO list](/jderrough/UnitySlippyMap#todo) if you want to contribute and don't know where to start.

License
-------

UnitySplippyMap is released under the [LGPL 3.0](http://www.gnu.org/licenses/lgpl.html).

TODO
----

Here is a short list of what could be fixed or added to UnitySlippyMap:

* Fix the voodooish way I implemented the camera elevation and tile positioning (see the comments in Map.cs)
* Handle device orientations properly when using the compass
* Add new map objects (polygons, lines, ...)
* Add support for orthographic cameras
* Add a better zoom rounding (>80%, <20%?)
* Add a map constraint to a given 'size', bounce on limits
* Add movements inertia
* Support other versions of WMS (used trang to convert dtd to xsd, then Xsd2Code to generate xml serializable classes)
* Subdomain rotations for VirtualEarth & OSM
* Display logo and copyright for VirtualEarth (url in metadata) & OSM (see OSM Wiki)
	

* Optimise, use one material for the tiles to enable dynamic batching: tried and failed, merging textures on the fly is to costly (see TextureAtlasManager.cs)
