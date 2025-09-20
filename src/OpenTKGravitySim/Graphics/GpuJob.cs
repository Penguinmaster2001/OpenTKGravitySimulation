
namespace OpenTKGravitySim.Graphics;



public class GpuJob<T>(Func<T> job) : IGpuJob
{
    public readonly TaskCompletionSource<T> Completion = new();



    private readonly Func<T> _job = job;



    public void Cancel() => Completion.SetCanceled();



    public void RunJob() => Completion.SetResult(_job.Invoke());
}
