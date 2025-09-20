
namespace OpenTKGravitySim.Graphics;



public interface IGpuJobManager
{
    public void AddJob(IGpuJob job);



    public void RunJobs();



    void CancelJobs();
}
