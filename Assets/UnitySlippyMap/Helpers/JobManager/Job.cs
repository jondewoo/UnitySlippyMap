using System;
using System.Collections;
using System.Collections.Generic;

namespace UnitySlippyMap
{

public class JobEventArgs : EventArgs
{
	public JobEventArgs(bool wasKilled, object owner)
	{
		WasKilled = wasKilled;
		Owner = owner;
	}
	
	public readonly bool WasKilled;
	public readonly object Owner;
}

public class Job
{
	public delegate void 			JobCompleteHandler(object job, JobEventArgs e);
	public event JobCompleteHandler	JobComplete;
	
	private bool					running;
	public bool						Running	{ get { return running; } }
	
	private bool					paused;
	public bool						Paused	{ get { return paused; } }

	private IEnumerator				coroutine;
	private object					owner;
	private bool					jobWasKilled;
	private Queue<Job>				childrenJobQueue;
	
	#region Ctors
	
	public Job(IEnumerator coroutine, object owner) : this(coroutine, owner, true)
	{
	}
	
	public Job(IEnumerator coroutine, object owner, bool shouldStart)
	{
		this.coroutine = coroutine;
		this.owner = owner;
		if (shouldStart)
			Start();
	}
	
	#endregion
	
	#region Public methods
	
	public Job CreateAndAddChildJob(IEnumerator coroutine)
	{
		Job j = new Job(coroutine, false);
		AddChildJob(j);
		return j;
	}
	
	public void AddChildJob(Job child)
	{
		if (childrenJobQueue == null)
			childrenJobQueue = new Queue<Job>();
		childrenJobQueue.Enqueue(child);
	}
	
	public void RemoveChildJob(Job childJob)
	{
		if (childrenJobQueue.Contains(childJob) == false)
		{
#if DEBUG_LOG
			Debug.LogWarning("WARNING: Job.RemoveChildJob: this job doesn't contain that child");
#endif
			return ;
		}
		
		Queue<Job> newChildrenJobQueue = new Queue<Job>(childrenJobQueue.Count - 1);
		Job[] allCurrentChildren = childrenJobQueue.ToArray();
		
		for (int i = 0; i < allCurrentChildren.Length; ++i)
		{
			Job j = allCurrentChildren[i];
			if (j != childJob)
			{
				newChildrenJobQueue.Enqueue(j);
			}
		}
		
		childrenJobQueue = newChildrenJobQueue;
	}
	
	public void Start()
	{
		running = true;
		JobManager.Instance.StartCoroutine(doWork());
	}
	
	public IEnumerator StartAsCoroutine()
	{
		running = true;
		yield return JobManager.Instance.StartCoroutine(doWork());
	}
	
	public void Pause()
	{
		paused = true;
	}
	
	public void Unpause()
	{
		paused = false;
	}
	
	public void Kill()
	{
		jobWasKilled = true;
		running = false;
		paused = false;
	}
	
	public void Kill(float delayInSeconds)
	{
		int delay = (int)(delayInSeconds * 1000);
		new System.Threading.Timer(obj =>
		{
			lock (this)
			{
				Kill();
			}
		}, null, delay, System.Threading.Timeout.Infinite);
	}
	
	#endregion
	
	#region Private methods
	
	private IEnumerator doWork()
	{
		// null out the first run through in case we start paused
		yield return null;
		
		while (running)
		{
			if (paused)
			{
				yield return null;
			}
			else
			{
				// run the next iteration and stop if we are done
				if (coroutine.MoveNext())
				{
					yield return coroutine.Current;
				}
				else
				{
					// run our child job if we have any
					if (childrenJobQueue != null)
						yield return JobManager.Instance.StartCoroutine(runChildJobs());
					running = false;
				}
			}
		}
		
		if (JobComplete != null)
			JobComplete(this, new JobEventArgs(jobWasKilled, owner));
	}
	
	private IEnumerator runChildJobs()
	{
		if (childrenJobQueue != null && childrenJobQueue.Count > 0)
		{
			do
			{
				Job childJob = childrenJobQueue.Dequeue();
				yield return JobManager.Instance.StartCoroutine(childJob.StartAsCoroutine());
			} while (childrenJobQueue.Count > 0);
		}	
	}
	
	#endregion
}

}