using UnityEngine;

namespace UnitySlippyMap
{

public class JobManager : MonoBehaviour
{
	#region Singleton stuff

	private static JobManager instance = null;

	public static JobManager Instance {
		get {
			if (null == (object)instance) {
				instance = FindObjectOfType (typeof(JobManager)) as JobManager;
				if (null == (object)instance) {
					var go = new GameObject ("[JobManager]");
					go.hideFlags = HideFlags.HideAndDontSave;
					instance = go.AddComponent<JobManager> ();
					instance.EnsureJobManager ();
				}
			}

			return instance;
		}
	}

	private void EnsureJobManager ()
	{
	}

	private JobManager ()
	{
	}

	private void OnApplicationQuit ()
	{
		DestroyImmediate (this.gameObject);
	}

	private void OnDestroy ()
	{
		instance = null;
	}

	#endregion
}

}