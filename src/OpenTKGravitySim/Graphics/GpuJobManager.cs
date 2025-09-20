
using System.Collections.Concurrent;



namespace OpenTKGravitySim.Graphics;



public class GpuJobManager : IGpuJobManager
{
    private readonly ConcurrentQueue<IGpuJob> _jobs = [];



    private bool _acceptJobs = true;



    public void AddJob(IGpuJob job)
    {
        if (_acceptJobs)
        {
            _jobs.Enqueue(job);
        }
        else
        {
            job.Cancel();
        }
    }



    public void RunJobs()
    {
        while (_jobs.TryDequeue(out var job))
        {
            job.RunJob();
        }
    }



    public void CancelJobs()
    {
        // TODO: Do this better
        _acceptJobs = false;
        while (_jobs.TryDequeue(out var job))
        {
            job.Cancel();
        }
    }
}
