
using OpenTK.Mathematics;

namespace OpenTKGravitySim.Particles;



internal class SpacialOctree
{
    public readonly List<SpacialOctreeNode> Nodes = [];
    public int NumNodes => Nodes.Count;



    private void Subdivide(int nodeIndex)
    {
        int childrenIndex = NumNodes;
    }
}



internal struct SpacialOctreeNode()
{
    public int FirstChildIndex;
    public AABC BoundingCube;
    public float Mass;
}



/// <summary>
/// Axis aligned bounding cube
/// </summary>
/// <param name="size"></param>
/// <param name="center"></param>
internal readonly struct AABC(float size, Vector3 center)
{
    public readonly float Size = size;
    public readonly Vector3 Center = center;



    public bool Inside(Vector3 point)
    {
        float halfSize = 0.5f * Size;
        Vector3 localPoint = point - Center;
        return (-halfSize < localPoint.X) && (localPoint.X < halfSize)
            && (-halfSize < localPoint.Y) && (localPoint.Y < halfSize)
            && (-halfSize < localPoint.Z) && (localPoint.Z < halfSize);
    }



    public AABC[] SplitIntoOctants()
    {

        float octantSize = 0.5f * Size;

        return [
            new(octantSize, Center + (0.25f * Size * new Vector3( 1.0f,  1.0f,  1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3( 1.0f,  1.0f, -1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3( 1.0f, -1.0f,  1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3( 1.0f, -1.0f, -1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3(-1.0f,  1.0f,  1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3(-1.0f,  1.0f, -1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3(-1.0f, -1.0f,  1.0f))),
            new(octantSize, Center + (0.25f * Size * new Vector3(-1.0f, -1.0f, -1.0f))),
        ];
    }
}
