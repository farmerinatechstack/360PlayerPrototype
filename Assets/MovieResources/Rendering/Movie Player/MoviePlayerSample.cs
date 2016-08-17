using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;					
using System.Runtime.InteropServices;		
using System;								
using System.IO;							

/************************************************************************************
Modified for usage from the Oculus VR Sample Framework

Usage:

	Place a simple textured quad surface with the correct aspect ratio in your scene.

	Add the MoviePlayerSample.cs script to the surface object.

	Supply the name of the media file to play:
	Place the file in "Assets/StreamingAssets", ie "ProjectName/Assets/StreamingAssets/MovieName.mp4".
	An mp4 and Ogg Theora format must be placed in the folder.

Implementation:

	In the MoviePlayerSample Awake() call, GetNativeTexturePtr() is called on 
	renderer.material.mainTexture.
	
	When the MediaSurface plugin gets the initialization event on the render thread, 
	it creates a new Android SurfaceTexture and Surface object in preparation 
	for receiving media. 

	When the game wants to start the video playing, it calls the StartVideoPlayerOnTextureId()
	script call, which creates an Android MediaPlayer java object, issues a 
	native plugin call to tell the native code to set up the target texture to
	render the video to and return the Android Surface object to pass to MediaPlayer,
	then sets up the media stream and starts it.
	
	Every frame, the SurfaceTexture object is checked for updates.  If there 
	is one, the target texId is re-created at the correct dimensions and format
	if it is the first frame, then the video image is rendered to it and mipmapped.  
	The following frame, instead of Unity drawing the image that was placed 
	on the surface in the Unity editor, it will draw the current video frame.

************************************************************************************/

public class MoviePlayerSample : MonoBehaviour
{
	public string 	movieName;
    public bool     videoPaused = false;
    private bool    videoPausedBeforeAppPause = false;

	public MovieInteraction movieInteraction;

    private string	mediaFullPath = string.Empty;
	private bool	startedVideo = false;

	#if (UNITY_ANDROID && !UNITY_EDITOR)
		private Texture2D nativeTexture = null;
		private IntPtr	  nativeTexId = IntPtr.Zero;
		private int		  textureWidth = 2880;
		private int 	  textureHeight = 1440;
		private AndroidJavaObject 	mediaPlayer = null;
	#else
		private MovieTexture 		movieTexture = null;
		private AudioSource			audioEmitter = null;
	#endif
	private Renderer 			mediaRenderer = null;

	private enum MediaSurfaceEventType
	{
		Initialize = 0,
		Shutdown = 1,
		Update = 2,
		Max_EventType
	};

	/// <summary>
	/// The start of the numeric range used by event IDs.
	/// </summary>
	/// <description>
	/// If multiple native rundering plugins are in use, the Oculus Media Surface plugin's event IDs
	/// can be re-mapped to avoid conflicts.
	/// 
	/// Set this value so that it is higher than the highest event ID number used by your plugin.
	/// Oculus Media Surface plugin event IDs start at eventBase and end at eventBase plus the highest
	/// value in MediaSurfaceEventType.
	/// </description>
	public static int eventBase
	{
		get { return _eventBase; }
		set
		{
			_eventBase = value;
			#if (UNITY_ANDROID && !UNITY_EDITOR)
						OVR_Media_Surface_SetEventBase(_eventBase);
			#endif
		}
	}
	private static int _eventBase = 0;

	private static void IssuePluginEvent(MediaSurfaceEventType eventType)
	{
		GL.IssuePluginEvent((int)eventType + eventBase);
	}

	void OnEnable() {
		movieInteraction.ToggleState += ToggleState;
		movieInteraction.Restart += Restart;
	}

	void OnDisable() {
		movieInteraction.ToggleState -= ToggleState;
		movieInteraction.Restart -= Restart;

	}

	void ToggleState() {
		videoPaused = !videoPaused;
		SetPaused (videoPaused);
	}

	void Restart() {
		SceneManager.LoadScene ("360Player");
	}

	/// <summary>
	/// Initialization of the movie surface
	/// </summary>
	void Awake()
	{		
		#if UNITY_ANDROID && !UNITY_EDITOR
				OVR_Media_Surface_Init();
		#endif

				mediaRenderer = GetComponent<Renderer>();
		#if !UNITY_ANDROID || UNITY_EDITOR
				audioEmitter = GetComponent<AudioSource>();
		#endif

		if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null)
		{
			Debug.LogError("No material for movie surface");
		}

		if (movieName != string.Empty)
		{
			StartCoroutine(RetrieveStreamingAsset(movieName));
		}
		else
		{
			Debug.LogError("No media file name provided");
		}

		#if UNITY_ANDROID && !UNITY_EDITOR
				nativeTexture = Texture2D.CreateExternalTexture(textureWidth, textureHeight,
				                                                TextureFormat.RGBA32, true, false,
				                                                IntPtr.Zero);

				IssuePluginEvent(MediaSurfaceEventType.Initialize);
		#endif
    }

    /// <summary>
    /// Construct the streaming asset path.
    /// Note: For Android, we need to retrieve the data from the apk.
    /// </summary>
    IEnumerator RetrieveStreamingAsset(string mediaFileName)
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
				string streamingMediaPath = Application.streamingAssetsPath + "/" + mediaFileName;
				string persistentPath = Application.persistentDataPath + "/" + mediaFileName;
				if (!File.Exists(persistentPath))
				{
					WWW wwwReader = new WWW(streamingMediaPath);
					yield return wwwReader;

					if (wwwReader.error != null)
					{
						Debug.LogError("wwwReader error: " + wwwReader.error);
					}

					System.IO.File.WriteAllBytes(persistentPath, wwwReader.bytes);
				}
				mediaFullPath = persistentPath;
		#else
				string mediaFileNameOgv = Path.GetFileNameWithoutExtension(mediaFileName) + ".ogv";
				string streamingMediaPath = "file:///" + Application.streamingAssetsPath + "/" + mediaFileNameOgv;
				WWW wwwReader = new WWW(streamingMediaPath);
				yield return wwwReader;

				if (wwwReader.error != null)
				{
					Debug.LogError("wwwReader error: " + wwwReader.error);
				}

				movieTexture = wwwReader.movie;
				mediaRenderer.material.mainTexture = movieTexture;
				audioEmitter.clip = movieTexture.audioClip;
				mediaFullPath = streamingMediaPath;
		#endif
        // Video must start only after mediaFullPath is filled in
        StartCoroutine(DelayedStartVideo());
    }

	/// <summary>
	/// Auto-starts video playback
	/// </summary>
	IEnumerator DelayedStartVideo()
	{
		yield return null; // delay 1 frame to allow MediaSurfaceInit from the render thread.

		if (!startedVideo)
		{
			startedVideo = true;
			#if (UNITY_ANDROID && !UNITY_EDITOR)
						mediaPlayer = StartVideoPlayerOnTextureId(textureWidth, textureHeight, mediaFullPath);
						mediaRenderer.material.mainTexture = nativeTexture;
			#else
						if (movieTexture != null && movieTexture.isReadyToPlay)
						{
							movieTexture.Play();
							if (audioEmitter != null)
							{
								audioEmitter.Play();
							}
						}
			#endif
		}
	}

	void Update()
	{
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		        if (!videoPaused) {
		            IntPtr currTexId = OVR_Media_Surface_GetNativeTexture();
		            if (currTexId != nativeTexId)
		            {
		                nativeTexId = currTexId;
		                nativeTexture.UpdateExternalTexture(currTexId);
		            }

		            IssuePluginEvent(MediaSurfaceEventType.Update);
		        }
		#else
		        if (movieTexture != null)
				{
					if (movieTexture.isReadyToPlay != movieTexture.isPlaying && !videoPaused)
					{
						movieTexture.Play();
						if (audioEmitter != null)
						{
							audioEmitter.Play();
						}
					}
				}
		#endif
	}

    public void StartOver()
    {
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		        if (mediaPlayer != null)
		        {
		            try
					{
						mediaPlayer.Call("seekTo", 0);
					}
					catch (Exception e)
					{
						Debug.Log("Failed to stop mediaPlayer with message " + e.Message);
					}
		        }
		#else
		        if (movieTexture != null)
		        {
		            movieTexture.Stop();
		            if (audioEmitter != null)
		            {
		                audioEmitter.Stop();
		            }
		        }
		#endif
    }

    public void SetPaused(bool wasPaused)
    {
		#if (UNITY_ANDROID && !UNITY_EDITOR)
				if (mediaPlayer != null)
				{
					try
					{
						mediaPlayer.Call((videoPaused) ? "pause" : "start");
					}
					catch (Exception e)
					{
						Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
					}
				}
		#else
		        if (movieTexture != null)
		        {
		            if (videoPaused)
		            {
		                movieTexture.Pause();
		                if (audioEmitter != null)
		                {
		                    audioEmitter.Pause();
		                }
		            }
		            else
		            {
		                movieTexture.Play();
		                if (audioEmitter != null)
		                {
		                    audioEmitter.Play();
		                }
		            }
		        }
		#endif
    }

    /// <summary>
    /// Pauses video playback when the app loses or gains focus
    /// </summary>
    void OnApplicationPause(bool appWasPaused)
    {
        if (appWasPaused)
        {
            videoPausedBeforeAppPause = videoPaused;
        }
        
        // Pause/unpause the video only if it had been playing prior to app pause
        if (!videoPausedBeforeAppPause)
        {
            SetPaused(appWasPaused);
        }
    }

    private void OnDestroy()
    {
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		        Debug.Log("Shutting down video");
				// This will trigger the shutdown on the render thread
				IssuePluginEvent(MediaSurfaceEventType.Shutdown);
		        mediaPlayer.Call("stop");
		        mediaPlayer.Call("release");
		        mediaPlayer = null;
		#endif
    }

	#if (UNITY_ANDROID && !UNITY_EDITOR)
		/// <summary>
		/// Set up the video player with the movie surface texture id.
		/// </summary>
		AndroidJavaObject StartVideoPlayerOnTextureId(int texWidth, int texHeight, string mediaPath)
		{
			Debug.Log("MoviePlayer: StartVideoPlayerOnTextureId");

			OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);

			IntPtr androidSurface = OVR_Media_Surface_GetObject();
			AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");

			// Can't use AndroidJavaObject.Call() with a jobject, must use low level interface
			//mediaPlayer.Call("setSurface", androidSurface);
			IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID(mediaPlayer.GetRawClass(),"setSurface","(Landroid/view/Surface;)V");
			jvalue[] parms = new jvalue[1];
			parms[0] = new jvalue();
			parms[0].l = androidSurface;
			AndroidJNI.CallVoidMethod(mediaPlayer.GetRawObject(), setSurfaceMethodId, parms);

			try
			{
				mediaPlayer.Call("setDataSource", mediaPath);
				mediaPlayer.Call("prepare");
				mediaPlayer.Call("setLooping", true);
				mediaPlayer.Call("start");
			}
			catch (Exception e)
			{
				Debug.Log("Failed to start mediaPlayer with message " + e.Message);
			}

			return mediaPlayer;
		}
	#endif

	#if (UNITY_ANDROID && !UNITY_EDITOR)
		[DllImport("OculusMediaSurface")]
		private static extern void OVR_Media_Surface_Init();

		[DllImport("OculusMediaSurface")]
		private static extern void OVR_Media_Surface_SetEventBase(int eventBase);

		// This function returns an Android Surface object that is
		// bound to a SurfaceTexture object on an independent OpenGL texture id.
		// Each frame, before the TimeWarp processing, the SurfaceTexture is checked
		// for updates, and if one is present, the contents of the SurfaceTexture
		// will be copied over to the provided surfaceTexId and mipmaps will be 
		// generated so normal Unity rendering can use it.
		[DllImport("OculusMediaSurface")]
		private static extern IntPtr OVR_Media_Surface_GetObject();

		[DllImport("OculusMediaSurface")]
		private static extern IntPtr OVR_Media_Surface_GetNativeTexture();

		[DllImport("OculusMediaSurface")]
		private static extern void OVR_Media_Surface_SetTextureParms(int texWidth, int texHeight);
	#endif
}
