
using System.Runtime.InteropServices;

using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



internal class SpacialOctree
{
    public readonly List<SpacialOctreeNode> Nodes;
    public int NumNodes => Nodes.Count;
    public int NumInternalNodes => InternalNodeIndices.Count;
    public int NumLeafNodes => NumNodes - NumInternalNodes;
    public float MaxSizeDistanceRatio;

    private readonly List<int> InternalNodeIndices;



    public SpacialOctree(float maxSizeDistanceRatio)
    {
        Nodes = [];
        InternalNodeIndices = [];

        MaxSizeDistanceRatio = maxSizeDistanceRatio;
    }



    public void Build(List<Particle> particles)
    {
        Clear();

        // There are no particles to add
        if (particles.Count == 0)
        {
            Nodes.Add(new(Vector3.Zero, 0.0f));
            return;
        }

        // Find largest required bounding box
        Vector3 min = particles[0].Position;
        Vector3 max = particles[0].Position;
        for (int particleIndex = 1; particleIndex < particles.Count; particleIndex++)
        {
            Vector3 position = particles[particleIndex].Position;
            min = Vector3.ComponentMin(min, position);
            max = Vector3.ComponentMax(max, position);
        }

        Vector3 center = 0.5f * (min + max);
        (max - min).Deconstruct(out float xLen, out float yLen, out float zLen);
        float size = Math.Min(Math.Min(xLen, yLen), zLen);

        SpacialOctreeNode root = new(center, size);
        Nodes.Add(root);
        
        // Insert each particle
        for (int particleIndex = 0; particleIndex < particles.Count; particleIndex++)
        {
            Insert(particles[particleIndex]);
        }

        CalculateMasses();
    }



    public Vector3 CalcGravForce(Vector3 position)
    {
        Vector3 gravForce = Vector3.Zero;

        int nextIndex = 0;
        do
        {
            SpacialOctreeNode node = Nodes[nextIndex];

            Vector3 direction = node.CenterOfMass - position;
            float sq_dist = direction.LengthSquared;

            // If the node is a leaf or the size - distance ratio is small enough, and the square distance is large enough (to ensure the particle doesn't affect itself and for numerical stability)
            if ((node.IsLeaf || node.BoundingCube.Size * node.BoundingCube.Size < sq_dist * MaxSizeDistanceRatio * MaxSizeDistanceRatio) && sq_dist > 0.0025f)
            {
                gravForce += (node.Mass / sq_dist) * direction.Normalized();

                // We can move on to the next node
                nextIndex = node.NextIndex;
            }
            // We must check the children
            else
            {
                nextIndex = node.FirstChildIndex;
            }
        }
        while (nextIndex > 0);

        return gravForce;
    }



    private void Insert(Particle particle)
    {
        // Start at root
        int nodeIndex = 0;

        // Find leaf node
        while (Nodes[nodeIndex].IsInternal)
        {
            nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position);
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

        Vector3 nodeCenterOfMass = Nodes[nodeIndex].CenterOfMass;
        float nodeMass = Nodes[nodeIndex].Mass;
        
        // Subdivide nodes until the positions are in separate quadrants
        int insertIndex = nodeIndex;
        while (insertIndex == nodeIndex)
        {
            nodeIndex = insertIndex;
            Subdivide(nodeIndex);

            insertIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position);
            nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(nodeCenterOfMass);
        }

        // Insert masses into the new nodes
        SpacialOctreeNode node = Nodes[nodeIndex];
        node.Mass = nodeMass;
        node.CenterOfMass = nodeCenterOfMass;
        Nodes[nodeIndex] = node;

        SpacialOctreeNode insertNode = Nodes[insertIndex];
        insertNode.Mass = particle.Mass;
        insertNode.CenterOfMass = particle.Position;
        Nodes[insertIndex] = insertNode;
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
        for (int internalIndex = InternalNodeIndices.Count - 1; internalIndex > 0; internalIndex--)
        {
            SpacialOctreeNode internalNode = Nodes[InternalNodeIndices[internalIndex]];

            float mass = Nodes[internalNode.FirstChildIndex + 0].Mass
                       + Nodes[internalNode.FirstChildIndex + 1].Mass
                       + Nodes[internalNode.FirstChildIndex + 2].Mass
                       + Nodes[internalNode.FirstChildIndex + 3].Mass
                       + Nodes[internalNode.FirstChildIndex + 4].Mass
                       + Nodes[internalNode.FirstChildIndex + 5].Mass
                       + Nodes[internalNode.FirstChildIndex + 6].Mass
                       + Nodes[internalNode.FirstChildIndex + 7].Mass;

            Vector3 centerOfMass = ((Nodes[internalNode.FirstChildIndex + 0].Mass * Nodes[internalNode.FirstChildIndex + 0].CenterOfMass)
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
        }
    }



    public void Clear()
    {
        Nodes.Clear();
        InternalNodeIndices.Clear();
    }
}



internal struct SpacialOctreeNode(AABC boundingCube, int nextIndex = 0, int firstChildIndex = 0, float mass = 0.0f)
{
    public AABC BoundingCube = boundingCube;
    public Vector3 CenterOfMass = boundingCube.Center;
    public float Mass = mass;

    /// <summary>
    /// This node's next sibling, or its parent's next sibling
    /// </summary>
    public int NextIndex = nextIndex;
    public int FirstChildIndex = firstChildIndex;



    public SpacialOctreeNode(Vector3 center, float size, int firstChildIndex = 0, int nextIndex = 0, float mass = 0.0f)
        : this(new(center, size), firstChildIndex, nextIndex, mass) { }



    public readonly int GetOctContainingIndex(Vector3 position) => FirstChildIndex + BoundingCube.GetOctantIndex(position);


    public readonly bool IsLeaf => FirstChildIndex == 0;
    public readonly bool IsInternal => FirstChildIndex > 0;
    public readonly bool IsEmpty => Mass == 0.0f;
    public int SizeInBytes = Marshal.SizeOf<SpacialOctreeNode>();
}



/// <summary>
/// Axis aligned bounding cube
/// </summary>
/// <param name="size"></param>
/// <param name="center"></param>
internal readonly struct AABC(Vector3 center, float size)
{
    public readonly Vector3 Center = center;
    public readonly float Size = size;



    public bool IsInside(Vector3 point)
    {
        float halfSize = 0.5f * Size;
        Vector3 localPoint = point - Center;
        return (-halfSize < localPoint.X) && (localPoint.X < halfSize)
            && (-halfSize < localPoint.Y) && (localPoint.Y < halfSize)
            && (-halfSize < localPoint.Z) && (localPoint.Z < halfSize);
    }



    public int GetOctantIndex(Vector3 point)
    {
        int index = 0;
        if (point.X <= Center.X) index |= 4;
        if (point.Y <= Center.Y) index |= 2;
        if (point.Z <= Center.Z) index |= 1;
        return index;
    }



    public AABC[] SplitIntoOctants()
    {
        float octantSize = 0.5f * Size;

        return [
            new(Center + (0.25f * Size * new Vector3( 1.0f,  1.0f,  1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3( 1.0f,  1.0f, -1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3( 1.0f, -1.0f,  1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3( 1.0f, -1.0f, -1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3(-1.0f,  1.0f,  1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3(-1.0f,  1.0f, -1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3(-1.0f, -1.0f,  1.0f)), octantSize),
            new(Center + (0.25f * Size * new Vector3(-1.0f, -1.0f, -1.0f)), octantSize)
        ];
    }
}
