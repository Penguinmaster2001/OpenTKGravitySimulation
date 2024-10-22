
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
    public float MaxSizeDistanceRatio;
    public int MaxDepth;

    private readonly List<int> InternalNodeIndices;



    public SpacialOctree(float maxSizeDistanceRatio, int maxDepth)
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
            Nodes.Add(new(Vector3.Zero, 0.0f));
            return;
        }

        // Find largest required bounding box
        Vector3 min = particles[0].Position.Xyz;
        Vector3 max = particles[0].Position.Xyz;
        for (int particleIndex = 1; particleIndex < particles.Count; particleIndex++)
        {
            Vector3 position = particles[particleIndex].Position.Xyz;
            min = Vector3.ComponentMin(min, position);
            max = Vector3.ComponentMax(max, position);
        }

        Vector3 center = 0.5f * (min + max);
        (max - min).Deconstruct(out float xLen, out float yLen, out float zLen);
        float size = Math.Max(Math.Max(xLen, yLen), zLen);

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
            if ((node.IsLeaf || node.IsEmpty || node.BoundingCube.Size * node.BoundingCube.Size < sq_dist * MaxSizeDistanceRatio * MaxSizeDistanceRatio) && sq_dist > 1.0f)
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
        }
        while (nextIndex > 0);

        if (gravForce.X != 0.0f && !float.IsNormal(gravForce.X)
         || gravForce.Y != 0.0f && !float.IsNormal(gravForce.Y)
         || gravForce.Z != 0.0f && !float.IsNormal(gravForce.Z))
        {
            throw new Exception($"Bad grav force");
        }

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
            nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position.Xyz);
            depth++;
        }

        // Add particle if leaf node is empty
        if (Nodes[nodeIndex].IsEmpty)
        {
            SpacialOctreeNode emptyNode = Nodes[nodeIndex];
            emptyNode.Mass = particle.Mass.X;
            emptyNode.CenterOfMass = particle.Position.Xyz;
            Nodes[nodeIndex] = emptyNode;
            return;
        }

        Vector3 nodeCenterOfMass = Nodes[nodeIndex].CenterOfMass;
        float nodeMass = Nodes[nodeIndex].Mass;

        int insertIndex = nodeIndex;
        // Subdivide nodes until the positions are in separate quadrants
        while (insertIndex == nodeIndex && depth < MaxDepth)
        {
            nodeIndex = insertIndex;
            Subdivide(nodeIndex);

            insertIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position.Xyz);
            nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(nodeCenterOfMass);
            depth++;
        }

        // Insert masses into the new nodes
        InsertIntoNode(nodeIndex, nodeCenterOfMass, nodeMass);
        InsertIntoNode(insertIndex, particle.Position.Xyz, particle.Mass.X);
    }



    private void InsertIntoNode(int nodeIndex, Vector3 centerOfMass, float mass)
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



public struct SpacialOctreeNode(AABC boundingCube, int nextIndex = 0, int firstChildIndex = 0, float mass = 0.0f) : IRenderable
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
    public static readonly int SizeInBytes = Marshal.SizeOf<SpacialOctreeNode>();



    public RenderObject ToRenderObject()
    {
        Vector4 position = new(CenterOfMass, 1.0f);
        Vector4 velocity = Vector4.Zero;
        Vector4 attributes = new(Mass, BoundingCube.Size, 0.0f, 0.0f);

        return new(position, velocity, attributes);
    }
}



/// <summary>
/// Axis aligned bounding cube
/// </summary>
/// <param name="size"></param>
/// <param name="center"></param>
public readonly struct AABC(Vector3 center, float size)
{
    public readonly Vector3 Center = center;
    public readonly float Size = size;



    public bool IsInside(Vector3 point)
    {
        float halfSize = 0.5f * Size + 0.01f;
        Vector3 localPoint = point - Center;
        return Math.Abs(localPoint.X) <= halfSize
            && Math.Abs(localPoint.Y) <= halfSize
            && Math.Abs(localPoint.Z) <= halfSize;
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
        float octantCenterOffset = 0.25f * Size;

        return [
            new(Center + (octantCenterOffset * new Vector3( 1.0f,  1.0f,  1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3( 1.0f,  1.0f, -1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3( 1.0f, -1.0f,  1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3( 1.0f, -1.0f, -1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3(-1.0f,  1.0f,  1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3(-1.0f,  1.0f, -1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3(-1.0f, -1.0f,  1.0f)), octantSize),
            new(Center + (octantCenterOffset * new Vector3(-1.0f, -1.0f, -1.0f)), octantSize)
        ];
    }


    public readonly override string ToString()
    {
        return $"Center: {Center}, Size: {Size}, Bounds: {Center - 0.5f * Size * Vector3.One}, {Center + 0.5f * Size * Vector3.One}";
    }
}
