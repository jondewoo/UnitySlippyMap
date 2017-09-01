// 
//  TileDownloader.cs
//  
//  Author:
//       Jonathan Derrough <jonathan.derrough@gmail.com>
//  
// Copyright (c) 2017 Jonathan Derrough
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace UnitySlippyMap.Map
{
	/// <summary>
	/// A singleton class in charge of downloading, caching and serving tiles.
	/// </summary>
	public class TileDownloaderBehaviour : MonoBehaviour
	{
	#region Singleton implementation
	
		/// <summary>
		/// The instance.
		/// </summary>
		private static TileDownloaderBehaviour instance = null;

		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance.</value>
		public static TileDownloaderBehaviour Instance
		{
			get
			{
	            if (null == (object)instance)
	            {
					instance = FindObjectOfType(typeof (TileDownloaderBehaviour)) as TileDownloaderBehaviour;
	                if (null == (object)instance)
	                {
	                    var go = new GameObject("[TileDownloader]");
	                    go.hideFlags = HideFlags.HideAndDontSave;
						instance = go.AddComponent<TileDownloaderBehaviour>();
	                    instance.EnsureDownloader();
	                }
				}

				return instance;
			}
		}

		/// <summary>
		/// Ensures the downloader.
		/// </summary>
		private void EnsureDownloader()
		{
	#if !UNITY_WEBPLAYER
			LoadTiles();
	#endif
		}
	
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloader"/> class.
		/// </summary>
		private TileDownloaderBehaviour()
		{
		}
	
		/// <summary>
		/// Raises the application quit event.
		/// </summary>
		private void OnApplicationQuit()
		{
			DestroyImmediate(this.gameObject);
		}
	
	#endregion
	
	#region Tile download subclasses
	
		/// <summary>
		/// A helper class for asynchronous IO operations.
		/// </summary>
	    private class AsyncInfo
	    {
			/// <summary>
			/// The tile entry.
			/// </summary>
	        private TileEntry entry;

			/// <summary>
			/// Gets the tile entry.
			/// </summary>
			/// <value>The tile entry.</value>
	        public TileEntry Entry { get { return entry;  } }

			/// <summary>
			/// The filestream.
			/// </summary>
	        private FileStream fs;

			/// <summary>
			/// Gets the FileStream instance.
			/// </summary>
			/// <value>The filestream.</value>
	        public FileStream FS { get { return fs; } }

			/// <summary>
			/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloaderBehaviour+AsyncInfo"/> class.
			/// </summary>
			/// <param name="entry">Entry.</param>
			/// <param name="fs">Fs.</param>
	        public AsyncInfo(TileEntry entry, FileStream fs)
	        {
	            this.entry = entry;
	            this.fs = fs;
	        }
	    }

		/// <summary>
		/// The TileEntry class holds the information necessary to the TileDownloader to manage the tiles.
		/// It also handles the (down)loading/caching of the concerned tile, taking advantage of Prime31's JobManager
		/// </summary>
		public class TileEntry
		{
#if !UNITY_WEBPLAYER
			/// <summary>
			/// The timestamp.
			/// </summary>
			[XmlAttribute("timestamp")]
			public double	timestamp;

			/// <summary>
			/// The size.
			/// </summary>
			[XmlAttribute("size")]
			public int		size;

			/// <summary>
			/// The GUID.
			/// </summary>
			[XmlAttribute("guid")]
			public string	guid;
#endif

			/// <summary>
			/// The URL.
			/// </summary>
			[XmlAttribute("url")]
			public string	url;
		
			/// <summary>
			/// The tile.
			/// </summary>
	        [XmlIgnore]
	        public TileBehaviour     tile;

			/// <summary>
			/// The texture.
			/// </summary>
	        [XmlIgnore]
	        public Texture2D texture;

#if !UNITY_WEBPLAYER
			/// <summary>
			/// The cached flag.
			/// </summary>
	        [XmlIgnore]
			public bool		cached = false;
#endif

			/// <summary>
			/// The error flag.
			/// </summary>
			[XmlIgnore]
			public bool		error = false;
		
			/// <summary>
			/// The job.
			/// </summary>
			[XmlIgnore]
			public Job		job;

			/// <summary>
			/// The job complete handler.
			/// </summary>
	        [XmlIgnore]
	        public Job.JobCompleteHandler jobCompleteHandler;

			/// <summary>
			/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloader+TileEntry"/> class.
			/// </summary>
			public TileEntry()
			{
				this.jobCompleteHandler = new Job.JobCompleteHandler(TileDownloaderBehaviour.Instance.JobTerminationEvent);
			}
		
			/// <summary>
			/// Initializes a new instance of the <see cref="UnitySlippyMap.Map.TileDownloader+TileEntry"/> class.
			/// </summary>
			/// <param name="url">URL.</param>
			/// <param name="tile">Tile.</param>
			public TileEntry(string url, TileBehaviour tile)
			{
				this.url = url;
	            if (tile == null)
	                throw new ArgumentNullException("tile");
	            this.tile = tile;
				this.jobCompleteHandler = new Job.JobCompleteHandler(TileDownloaderBehaviour.Instance.JobTerminationEvent);
			}
		
			/// <summary>
			/// Starts the download.
			/// </summary>
			public void StartDownload()
			{
#if DEBUG_LOG
				Debug.Log("DEBUG: TileEntry.StartDownload: " + url);
#endif
				job = new Job(DownloadCoroutine(), this);
				job.JobComplete += jobCompleteHandler;
			}
		
			/// <summary>
			/// Stops the download.
			/// </summary>
			public void StopDownload()
			{
#if DEBUG_LOG
				Debug.Log("DEBUG: TileEntry.StopDownload: " + url);
#endif
	            job.JobComplete -= jobCompleteHandler;
				job.Kill();
			}
			
			/// <summary>
			/// The download the coroutine.
			/// </summary>
			/// <returns>The coroutine.</returns>
			private IEnumerator DownloadCoroutine()
			{
				WWW www = null;
				string ext = Path.GetExtension(url);
	            if (ext.Contains("?"))
	                ext = ext.Substring(0, ext.IndexOf('?'));
#if !UNITY_WEBPLAYER
	            if (cached && File.Exists(Application.temporaryCachePath + "/" + this.guid + ext))
	            {
	                www = new WWW("file:///" + Application.temporaryCachePath + "/" + this.guid + ext);
#if DEBUG_LOG
                	Debug.Log("DEBUG: TileDownloader.DownloadCoroutine: loading tile from cache: url: " + www.url);
#endif
            	}
            	else
#endif
            	{
                	www = new WWW(url);
#if DEBUG_LOG
                	Debug.Log("DEBUG: TileDownloader.DownloadCoroutine: loading tile from provider: url: " + www.url
#if !UNITY_WEBPLAYER
                    	+ "(cached: " + cached + ")"
#endif
                    	);
#endif
	            }

	            yield return www;
			
#if DEBUG_PROFILE
				UnitySlippyMap.Profiler.Begin("TileDownloader.TileEntry.DownloadCoroutine");
#endif

#if DEBUG_PROFILE
				UnitySlippyMap.Profiler.Begin("www error test");
#endif
				if (String.IsNullOrEmpty(www.error) && www.text.Contains("404 Not Found") == false)
				{
#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.End("www error test");
#endif
#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.Begin("www.texture");
#endif

	                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, true);
					www.LoadImageIntoTexture(texture);
					
#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.End("www.texture");
#endif

#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.Begin("is cached?");
#endif
#if !UNITY_WEBPLAYER
	                if (this.cached == false)
					{
#if DEBUG_PROFILE
						UnitySlippyMap.Profiler.End("is cached?");
#endif
    					
#if DEBUG_PROFILE
						UnitySlippyMap.Profiler.Begin("set TileEntry members");
#endif

		                byte[] bytes = www.bytes;
						
						this.size = bytes.Length;
						this.guid = Guid.NewGuid().ToString();
#if DEBUG_PROFILE
						UnitySlippyMap.Profiler.End("set TileEntry members");
#endif
					
#if DEBUG_PROFILE
						UnitySlippyMap.Profiler.Begin("new FileStream & FileStream.BeginWrite");
#endif
						FileStream fs = new FileStream(Application.temporaryCachePath + "/" + this.guid + ext, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
						fs.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(EndWriteCallback), new AsyncInfo(this, fs));
#if DEBUG_PROFILE
						UnitySlippyMap.Profiler.End("new FileStream & FileStream.BeginWrite");
#endif
				
#if DEBUG_LOG
						Debug.Log("DEBUG: TileEntry.DownloadCoroutine: done loading: " + www.url + ", writing to cache: " + fs.Name);
#endif
					}
					else
					{
#if DEBUG_PROFILE
						UnitySlippyMap.Profiler.End("is cached?");
#endif
#if DEBUG_LOG
		    			Debug.Log("DEBUG: TileEntry.DownloadCoroutine: done loading from cache: " + www.url + " [" + url + "]");
#endif
					}

					this.timestamp = (DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
#endif

#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.Begin("Tile.SetTexture");
#endif
					tile.SetTexture(texture);
#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.End("Tile.SetTexture");
#endif
				}
				else
				{
#if DEBUG_PROFILE
					UnitySlippyMap.Profiler.End("www error test");
#endif
					this.error = true;
#if DEBUG_LOG
					Debug.LogError("ERROR: TileEntry.DownloadCoroutine: done downloading: " + www.url + " with error: " + www.error);
#endif
				}
			
#if DEBUG_PROFILE
				UnitySlippyMap.Profiler.End("TileDownloader.TileEntry.DownloadCoroutine");
#endif
			}
		
#if !UNITY_WEBPLAYER
			/// <summary>
			/// The callback called at the end of the writing operation.
			/// </summary>
			/// <param name="result">Result.</param>
			private static void EndWriteCallback(IAsyncResult result)
			{
				AsyncInfo info = result.AsyncState as AsyncInfo;
				info.Entry.cached = true;

	            info.FS.EndWrite(result);
	            info.FS.Flush();

	            info.FS.Close();

#if DEBUG_LOG
				Debug.Log("DEBUG: TileEntry.EndWriteCallback: done writing: " + info.Entry.url + " [" + info.Entry.guid + "]");
#endif
			}
#endif
		}
	
	#endregion
	
	#region Private members & properties

		/// <summary>
		/// The tile URL looked for.
		/// </summary>
		private static string tileURLLookedFor;

		/// <summary>
		/// The match predicate to find tiles by URL
		/// </summary>
		/// <returns><c>true</c>, if URL match predicate was tiled, <c>false</c> otherwise.</returns>
		/// <param name="entry">Entry.</param>
		private static bool tileURLMatchPredicate(TileEntry entry)
		{
			if (entry.url == tileURLLookedFor)
				return true;
			return false;
		}
	
		/// <summary>
		/// The tiles to load.
		/// </summary>
		private List<TileEntry>	tilesToLoad = new List<TileEntry>();

		/// <summary>
		/// The tiles loading.
		/// </summary>
		private List<TileEntry>	tilesLoading = new List<TileEntry>();

#if !UNITY_WEBPLAYER
		/// <summary>
		/// The tiles.
		/// </summary>
		private List<TileEntry>	tiles = new List<TileEntry>();

		/// <summary>
		/// The tile path.
		/// </summary>
    	private string tilePath = Application.temporaryCachePath;
#endif
	
		/// <summary>
		/// The max simultaneous downloads.
		/// </summary>
		private int maxSimultaneousDownloads = 2;

		/// <summary>
		/// Gets or sets the max simultaneous downloads.
		/// </summary>
		/// <value>The max simultaneous downloads.</value>
		public int MaxSimultaneousDownloads { get { return maxSimultaneousDownloads; } set { maxSimultaneousDownloads = value; } }
	
#if !UNITY_WEBPLAYER
		/// <summary>
		/// The size of the max cache.
		/// </summary>
		private int maxCacheSize = 20000000; // 20 Mo

		/// <summary>
		/// Gets or sets the size of the max cache.
		/// </summary>
		/// <value>The size of the max cache.</value>
		public int MaxCacheSize { get { return maxCacheSize; } set { maxCacheSize = value; } }
	
		/// <summary>
		/// The size of the cache.
		/// </summary>
		private int cacheSize = 0;
#endif
	
	#endregion
		
	#region Public methods

		/// <summary>
		/// Gets a tile by its URL, the main texture of the material is assigned if successful.
		/// </summary>
		/// <param name="url">URL.</param>
		/// <param name="tile">Tile.</param>
		public void Get(string url, TileBehaviour tile)
		{
	#if DEBUG_LOG
	        Debug.Log("DEBUG: TileDownloader.Get: url: " + url);
	#endif
	        
			tileURLLookedFor = url;
			if (tilesToLoad.Exists(tileURLMatchPredicate))
			{
	#if DEBUG_LOG
				Debug.LogWarning("WARNING: TileDownloader.Get: already asked for url: " + url);
	#endif
				return ;
			}
			
			if (tilesLoading.Exists(tileURLMatchPredicate))
			{
	#if DEBUG_LOG
				Debug.LogWarning("WARNING: TileDownloader.Get: already downloading url: " + url);
	#endif
				return ;
			}
			
	#if !UNITY_WEBPLAYER
			TileEntry cachedEntry = tiles.Find(tileURLMatchPredicate);

			if (cachedEntry == null)
	#endif
	        {
	#if DEBUG_LOG
	            Debug.Log("DEBUG: TileDownloader.Get: adding '" + url + "' to loading list");
	#endif
	            tilesToLoad.Add(new TileEntry(url, tile));
	        }
	#if !UNITY_WEBPLAYER
			else
			{
	#if DEBUG_LOG
	            Debug.Log("DEBUG: TileDownloader.Get: adding '" + url + "' to loading list (cached)");
	#endif
				cachedEntry.cached = true;
	            cachedEntry.tile = tile;
				//cachedEntry.Complete = material;
				tilesToLoad.Add(cachedEntry);
			}
	#endif
	    }

		/// <summary>
		/// Cancels the request for a tile by its URL.
		/// </summary>
		/// <returns><c>true</c> if this instance cancel url; otherwise, <c>false</c>.</returns>
		/// <param name="url">URL.</param>
		public void Cancel(string url)
		{
			tileURLLookedFor = url;
			TileEntry entry = tilesToLoad.Find(tileURLMatchPredicate);
			if (entry != null)
			{
	#if DEBUG_LOG
				Debug.Log("DEBUG: TileDownloader.Cancel: remove download from schedule: " + url);
	#endif
				tilesToLoad.Remove(entry);
				return ;
			}
			
			entry = tilesLoading.Find(tileURLMatchPredicate);
			if (entry != null)
			{
	#if DEBUG_LOG
				Debug.Log("DEBUG: TileDownloader.Cancel: stop downloading: " + url);
	#endif
	            tilesLoading.Remove(entry);
				entry.StopDownload();
				return ;
			}

	#if DEBUG_LOG
			Debug.LogWarning("WARNING: TileDownloader.Cancel: url not scheduled to be downloaded nor downloading: " + url);
	#endif
		}

		/// <summary>
		/// A method called when the job is done, successfully or not.
		/// </summary>
		/// <param name="job">Job.</param>
		/// <param name="e">E.</param>
		public void JobTerminationEvent(object job, JobEventArgs e)
		{
	#if DEBUG_LOG
			Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: Tile download complete, but was it murdered? " + e.WasKilled);
	#endif
			TileEntry entry = e.Owner as TileEntry;
			tilesLoading.Remove(entry);
			
	#if !UNITY_WEBPLAYER
			if (e.WasKilled == false)
			{
				if (entry.error && entry.cached)
				{
	                if (entry.cached)
	                {
	#if DEBUG_LOG
					    Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: loading cached tile failed, trying to download it: " + entry.url);
	#endif
	    				// try downloading the tile again
	    				entry.cached = false;
						cacheSize -= entry.size;
	    				tiles.Remove(entry);
	                }
	                else
	                {
	#if DEBUG_LOG
	                     Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: downloading tile failed, trying to download it again: " + entry.url);
	#endif
	                }

					Get(entry.url, entry.tile);
					
					return ;
				}
				
				tileURLLookedFor = entry.url;
				TileEntry existingEntry = tiles.Find(tileURLMatchPredicate);
				if (existingEntry != null)
				{
					tiles.Remove(existingEntry);
					cacheSize -= existingEntry.size;
				}
				
				entry.timestamp = (DateTime.Now.ToLocalTime() - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
				tiles.Add(entry);
				cacheSize += entry.size;
				
				// if the cache is full, erase the oldest entry
				// FIXME: find a better way to handle the cache (cf. iPhone Maps app)
				// FIXME: one aspect might be to erase tiles in batch, 10 or 20 at a time, a significant number anyway
				if (cacheSize > MaxCacheSize)
				{
	                // beware the year 3000 bug :)
					double oldestTimestamp = (new DateTime(3000, 1, 1) - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
					TileEntry entryToErase = null;
					foreach (TileEntry tile in tiles)
					{
						if (tile.timestamp < oldestTimestamp
							&& tile != entry)
						{
							oldestTimestamp = tile.timestamp;
							entryToErase = tile;
						}
					}
					if (entryToErase == null)
					{
	#if DEBUG_LOG
						Debug.LogWarning("WARNING: TileDownloader.JobTerminationEvent: no cache entry to erase (should not happen)");
	#endif
						return ;
					}

	                DeleteCachedTile(entryToErase);
	#if DEBUG_LOG
					Debug.Log("DEBUG: TileDownloader.JobTerminationEvent: erased from cache: " + entryToErase.url + " [" + entryToErase.guid + "]");
	#endif
				}
			}
	#endif
		}

		/// <summary>
		/// Pauses all.
		/// </summary>
		public void PauseAll()
		{
	        foreach (TileEntry entry in tilesLoading)
	        {
	            entry.job.Pause();
	        }
		}

		/// <summary>
		/// Unpauses all.
		/// </summary>
		public void UnpauseAll()
		{
	        foreach (TileEntry entry in tilesLoading)
	        {
	            entry.job.Unpause();
	        }
		}
		
	#endregion
	
	#region Private methods
 
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Start().
		/// </summary>
	    private void Start()
	    {
	        TextureBogusExtension.Init(this);
	    }
    
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.Update().
		/// </summary>
		private void Update()
		{
			while (tilesToLoad.Count > 0
				&& tilesLoading.Count < MaxSimultaneousDownloads)
			{
				DownloadNextTile();
			}
	        
	#if DEBUG_LOG
	        /*
	        if (tilesLoading.Count >= MaxSimultaneousDownloads)
	        {
	            Debug.Log("DEBUG: TileDownload.Update: tilesLoading.Count (" + tilesLoading.Count + ") > MaxSimultaneousDownloads (" + MaxSimultaneousDownloads + ")");
	            string dbg = "DEBUG: tilesLoading entries:\n";
	            foreach (TileEntry entry in tilesLoading)
	            {
	                dbg += entry.url + "\n";
	            }
	            Debug.Log(dbg);
	        }
	         */
	  
	        /*
	        {
	            string dbg = "DEBUG: tilesToLoad entries:\n";
	            foreach (TileEntry entry in tilesToLoad)
	            {
	                dbg += entry.url + "\n";
	            }
	            Debug.Log(dbg);
	        }
	        */
	#endif
		}
		
		/// <summary>
		/// Downloads the next tile.
		/// </summary>
		private void DownloadNextTile()
		{
			TileEntry entry = tilesToLoad[0];
			tilesToLoad.RemoveAt(0);
			tilesLoading.Add(entry);
			
	#if DEBUG_LOG
	        Debug.Log("DEBUG: TileDownloader.DownloadNextTile: entry.url: " + entry.url);
	#endif
	        
			entry.StartDownload();		
		}
	 
		/// <summary>
		/// Implementation of <see cref="http://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</see>.OnDestroy().
		/// </summary>
		private void OnDestroy()
		{
	        KillAll();		
	#if !UNITY_WEBPLAYER
			SaveTiles();
	#endif
			instance = null;
		}
    
		/// <summary>
		/// Kills all.
		/// </summary>
	    private void KillAll()
	    {
	        foreach (TileEntry entry in tilesLoading)
	        {
	            entry.job.Kill();
	        }
	    }
	    
#if !UNITY_WEBPLAYER
		/// <summary>
		/// Deletes the cached tile.
		/// </summary>
		/// <param name="t">T.</param>
	    private void DeleteCachedTile(TileEntry t)
	    {
	        cacheSize -= t.size;
	        File.Delete(tilePath + "/" + t.guid + ".png");
	        tiles.Remove(t);
	    }

		/// <summary>
		/// Saves the tile informations to an XML file stored in tilePath.
		/// </summary>
		private void SaveTiles()
		{
	        string filepath = tilePath + "/" + "tile_downloader.xml";
			
	#if DEBUG_LOG
			Debug.Log("DEBUG: TileDownloader.SaveTiles: file: " + filepath);
	#endif

			XmlSerializer xs = new XmlSerializer(tiles.GetType());
			using (StreamWriter sw = new StreamWriter(filepath))
	    	{
				xs.Serialize(sw, tiles);
			}
		}

		/// <summary>
		/// Loads the tile informations from an XML file stored in tilePath.
		/// </summary>
		private void LoadTiles()
		{
	        string filepath = tilePath + "/" + "tile_downloader.xml";
			
			if (File.Exists(filepath) == false)
			{
	#if DEBUG_LOG
				Debug.Log("DEBUG: TileDownloader.LoadTiles: file doesn't exist: " + filepath);
	#endif
				return ;
			}
			
	#if DEBUG_LOG
			Debug.Log("DEBUG: TileDownloader.LoadTiles: file: " + filepath);
	#endif
			
			XmlSerializer xs = new XmlSerializer(tiles.GetType());
			using (StreamReader sr = new StreamReader(filepath))
	    	{
				tiles = xs.Deserialize(sr) as List<TileEntry>;
			}
			
			foreach (TileEntry tile in tiles)
			{
				cacheSize += tile.size;
			}
		}
#endif
	
	#endregion
	}


}