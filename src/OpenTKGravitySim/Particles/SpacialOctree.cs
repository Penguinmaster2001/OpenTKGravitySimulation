
using System.Runtime.InteropServices;

using OpenTK.Mathematics;
using OpenTKGravitySim.Graphics;



namespace OpenTKGravitySim.Particles;



internal class SpacialOctree
{
    public readonly List<SpacialOctreeNode> Nodes;
    public int NumNodes => Nodes.Count;
    public int NumInternalNodes => InternalNodeIndices.Count;
    public int NumLeafNodes => NumNodes - NumInternalNodes;
    public readonly List<SpacialOctreeNode> Leaves;
    public double MaxSizeDistanceRatio;
    public int MaxDepth;

    private readonly List<int> InternalNodeIndices;



    public SpacialOctree(double maxSizeDistanceRatio, int maxDepth)
    {
        Nodes = [];
        Leaves = [];
        InternalNodeIndices = [];

        MaxSizeDistanceRatio = maxSizeDistanceRatio;
        MaxDepth = maxDepth;
    }



    public void Build(List<Particle> particles)
    {
        Clear();

        // There are no particles to add
        if (particles.Count == 0)
        {
            Nodes.Add(new(Vector3d.Zero, 0.0));
            return;
        }

        // Find largest required bounding box
        Vector3d min = particles[0].Position;
        Vector3d max = particles[0].Position;
        for (int particleIndex = 1; particleIndex < particles.Count; particleIndex++)
        {
            Vector3d position = particles[particleIndex].Position;
            min = Vector3d.ComponentMin(min, position);
            max = Vector3d.ComponentMax(max, position);
        }

        Vector3d center = 0.5 * (min + max);
        (max - min).Deconstruct(out double xLen, out double yLen, out double zLen);
        double size = Math.Max(Math.Max(xLen, yLen), zLen);

        SpacialOctreeNode root = new(center, size);
        Nodes.Add(root);
        
        // Insert each particle
        for (int particleIndex = 0; particleIndex < particles.Count; particleIndex++)
        {
            Insert(particles[particleIndex]);
        }

        CalculateMasses();
        // Console.WriteLine($"Depth: {CurrentDepth}, Nodes: {NumNodes}, Leaves: {NumLeafNodes}");
    }



    public Vector3d CalcGravForce(Vector3d position)
    {
        Vector3d gravForce = Vector3d.Zero;

        int nextIndex = 0;
        do
        {
            SpacialOctreeNode node = Nodes[nextIndex];

            Vector3d direction = node.CenterOfMass - position;
            double sq_dist = direction.LengthSquared;

            // If the node is a leaf or the size - distance ratio is small enough, and the square distance is large enough (to ensure the particle doesn't affect itself and for numerical stability)
            if ((node.IsLeaf || node.IsEmpty || node.BoundingCube.Size * node.BoundingCube.Size < sq_dist * MaxSizeDistanceRatio * MaxSizeDistanceRatio) && sq_dist > 0.01)
            {
                gravForce += direction.Normalized() * node.Mass / sq_dist;

                // We can move on to the next node
                nextIndex = node.NextIndex;
            }
            // We must check the children
            else
            {
                nextIndex = node.FirstChildIndex;
            }

            if (gravForce.X != 0.0 && !double.IsNormal(gravForce.X)
             || gravForce.Y != 0.0 && !double.IsNormal(gravForce.Y)
             || gravForce.Z != 0.0 && !double.IsNormal(gravForce.Z))
            {
                throw new Exception($"Bad grav force");
            }
        }
        while (nextIndex > 0);

        return gravForce;
    }



    private void Insert(Particle particle)
    {
        // Start at root
        int nodeIndex = 0;
        int depth = 0;

        // Find leaf node
        while (depth < MaxDepth && Nodes[nodeIndex].IsInternal)
        {
            nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position);
            depth++;
        }

        // Add particle if leaf node is empty
        if (Nodes[nodeIndex].IsEmpty)
        {
            SpacialOctreeNode emptyNode = Nodes[nodeIndex];
            emptyNode.Mass = particle.Mass;
            emptyNode.CenterOfMass = particle.Position;
            Nodes[nodeIndex] = emptyNode;
            return;
        }

        Vector3d nodeCenterOfMass = Nodes[nodeIndex].CenterOfMass;
        double nodeMass = Nodes[nodeIndex].Mass;

        int insertIndex = nodeIndex;
        // Subdivide nodes until the positions are in separate quadrants
        while (insertIndex == nodeIndex && depth < MaxDepth)
        {
            nodeIndex = insertIndex;
            Subdivide(nodeIndex);

            insertIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position);
            nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(nodeCenterOfMass);
            depth++;
        }

        // Insert masses into the new nodes
        InsertIntoNode(nodeIndex, nodeCenterOfMass, nodeMass);
        InsertIntoNode(insertIndex, particle.Position, particle.Mass);
    }



    private void InsertIntoNode(int nodeIndex, Vector3d centerOfMass, double mass)
    {
        SpacialOctreeNode insertNode = Nodes[nodeIndex];

        if (insertNode.IsEmpty)
        {
            insertNode.CenterOfMass = centerOfMass;
            insertNode.Mass = mass;
        }
        else
        {
            insertNode.CenterOfMass = (insertNode.Mass * insertNode.CenterOfMass) + (mass * centerOfMass);
            insertNode.Mass += mass;
            insertNode.CenterOfMass /= insertNode.Mass;
        }

        Nodes[nodeIndex] = insertNode;
    }



    private void Subdivide(int nodeIndex)
    {
        InternalNodeIndices.Add(nodeIndex);
        SpacialOctreeNode node = Nodes[nodeIndex];
        node.FirstChildIndex = NumNodes;
        Nodes[nodeIndex] = node;

        AABC[] subdividedAABCs = node.BoundingCube.SplitIntoOctants();
        for (int child = 0; child < 8; child++)
        {
            int next = child == 7 ? node.NextIndex : NumNodes + child + 1;
            SpacialOctreeNode childNode = new(subdividedAABCs[child], next);
            Nodes.Add(childNode);
        }
    }



    private void CalculateMasses()
    {
        for (int internalIndex = InternalNodeIndices.Count - 1; internalIndex >= 0; internalIndex--)
        {
            SpacialOctreeNode internalNode = Nodes[InternalNodeIndices[internalIndex]];

            double mass = Nodes[internalNode.FirstChildIndex + 0].Mass
                        + Nodes[internalNode.FirstChildIndex + 1].Mass
                        + Nodes[internalNode.FirstChildIndex + 2].Mass
                        + Nodes[internalNode.FirstChildIndex + 3].Mass
                        + Nodes[internalNode.FirstChildIndex + 4].Mass
                        + Nodes[internalNode.FirstChildIndex + 5].Mass
                        + Nodes[internalNode.FirstChildIndex + 6].Mass
                        + Nodes[internalNode.FirstChildIndex + 7].Mass;

            Vector3d centerOfMass = ((Nodes[internalNode.FirstChildIndex + 0].Mass * Nodes[internalNode.FirstChildIndex + 0].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 1].Mass * Nodes[internalNode.FirstChildIndex + 1].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 2].Mass * Nodes[internalNode.FirstChildIndex + 2].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 3].Mass * Nodes[internalNode.FirstChildIndex + 3].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 4].Mass * Nodes[internalNode.FirstChildIndex + 4].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 5].Mass * Nodes[internalNode.FirstChildIndex + 5].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 6].Mass * Nodes[internalNode.FirstChildIndex + 6].CenterOfMass)
                                   + (Nodes[internalNode.FirstChildIndex + 7].Mass * Nodes[internalNode.FirstChildIndex + 7].CenterOfMass))
                                  / mass;

            internalNode.Mass = mass;
            internalNode.CenterOfMass = centerOfMass;
            Nodes[InternalNodeIndices[internalIndex]] = internalNode;

            for (int i = 0; i < 8; i++)
            {
                SpacialOctreeNode childNode = Nodes[internalNode.FirstChildIndex + i];

                if (childNode.IsLeaf && !childNode.IsEmpty)
                {
                    Leaves.Add(childNode);
                }
            }
        }
    }



    public void Clear()
    {
        Leaves.Clear();
        Nodes.Clear();
        InternalNodeIndices.Clear();
    }
}



public struct SpacialOctreeNode(AABC boundingCube, int nextIndex = 0, int firstChildIndex = 0, double mass = 0.0f) : IRenderable
{
    public AABC BoundingCube = boundingCube;
    public Vector3d CenterOfMass = boundingCube.Center;
    public double Mass = mass;

    /// <summary>
    /// This node's next sibling, or its parent's next sibling
    /// </summary>
    public int NextIndex = nextIndex;
    public int FirstChildIndex = firstChildIndex;



    public SpacialOctreeNode(Vector3d center, double size, int firstChildIndex = 0, int nextIndex = 0, double mass = 0.0f)
        : this(new(center, size), firstChildIndex, nextIndex, mass) { }



    public readonly int GetOctContainingIndex(Vector3d position) => FirstChildIndex + BoundingCube.GetOctantIndex(position);


    public readonly bool IsLeaf => FirstChildIndex == 0;
    public readonly bool IsInternal => FirstChildIndex > 0;
    public readonly bool IsEmpty => Mass == 0.0f;
    public static readonly int SizeInBytes = Marshal.SizeOf<SpacialOctreeNode>();



    public RenderObject ToRenderObject()
    {
        Vector4 position = new((Vector3) CenterOfMass, 1.0f);
        Vector4 velocity = new((Vector3) BoundingCube.Center, 1.0f);
        Vector4 attributes = new((float) Mass, (float) BoundingCube.Size, 0.0f, 0.0f);

        return new(position, velocity, attributes);
    }
}



/// <summary>
/// Axis aligned bounding cube
/// </summary>
/// <param name="size"></param>
/// <param name="center"></param>
public readonly struct AABC(Vector3d center, double size)
{
    public readonly Vector3d Center = center;
    public readonly double Size = size;



    public bool IsInside(Vector3d point)
    {
        double halfSize = 0.5 * Size + 0.01;
        Vector3d localPoint = point - Center;
        return Math.Abs(localPoint.X) <= halfSize
            && Math.Abs(localPoint.Y) <= halfSize
            && Math.Abs(localPoint.Z) <= halfSize;
    }



    public int GetOctantIndex(Vector3d point)
    {
        int index = 0;
        if (point.X <= Center.X) index |= 4;
        if (point.Y <= Center.Y) index |= 2;
        if (point.Z <= Center.Z) index |= 1;
        return index;
    }



    public AABC[] SplitIntoOctants()
    {
        double octantSize = 0.5 * Size;
        double octantCenterOffset = 0.25 * Size;

        return [
            new(Center + (octantCenterOffset * new Vector3d( 1.0,  1.0,  1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d( 1.0,  1.0, -1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d( 1.0, -1.0,  1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d( 1.0, -1.0, -1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d(-1.0,  1.0,  1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d(-1.0,  1.0, -1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d(-1.0, -1.0,  1.0)), octantSize),
            new(Center + (octantCenterOffset * new Vector3d(-1.0, -1.0, -1.0)), octantSize)
        ];
    }


    public readonly override string ToString()
    {
        return $"Center: {Center}, Size: {Size}, Bounds: {Center - 0.5 * Size * Vector3d.One}, {Center + 0.5 * Size * Vector3d.One}";
    }
}
