
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTKGravitySim.Graphics;



namespace OpenTKGravitySim.Particles;



public class SpacialOctree
{
    public readonly List<SpacialOctreeNode> Nodes;
    public int NumNodes => Nodes.Count;
    public int NumInternalNodes => InternalNodeIndices.Count;
    public int NumLeafNodes => NumNodes - NumInternalNodes;
    public readonly List<SpacialOctreeNode> Leaves;
    public double MaxSizeDistanceRatio;

    private readonly List<int> InternalNodeIndices;

    public int MaxDepth = 0;
    public int MaxNodes = 0;



    public SpacialOctree(double maxSizeDistanceRatio)
    {
        Nodes = [];
        Leaves = [];
        InternalNodeIndices = [];

        MaxSizeDistanceRatio = maxSizeDistanceRatio;
    }



    public void Build(Particle[] particles)
    {
        Clear();

        // There are no particles to add
        if (particles.Length == 0)
        {
            Nodes.Add(new(Vector3d.Zero, 0.0));
            return;
        }

        // Find largest required bounding box
        Vector3d min = particles[0].Position;
        Vector3d max = particles[0].Position;
        for (int particleIndex = 1; particleIndex < particles.Length; particleIndex++)
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
        for (int particleIndex = 0; particleIndex < particles.Length; particleIndex++)
        {
            Insert(particles[particleIndex]);
        }


        if (Nodes.Count > MaxNodes)
        {
            MaxNodes = Nodes.Count;
            Console.WriteLine($"New MaxNodes: {MaxNodes}");
        }
    }



    public Vector3d CalcGravForce(Vector3d position)
    {
        Vector3d gravForce = Vector3d.Zero;

        int nextIndex = 0;
        do
        {
            var node = Nodes[nextIndex];

            Vector3d direction = node.CenterOfMass - position;
            double dist = direction.Length;

            // If the node is a leaf or the size - distance ratio is small enough, and the square distance is large enough (to ensure the particle doesn't affect itself and for numerical stability)
            if (dist < 0.0001 || node.IsEmpty)
            {
                nextIndex = node.NextIndex;
            }
            else if (node.IsLeaf || node.BoundingCube.Size < dist * MaxSizeDistanceRatio)
            {
                gravForce += direction * node.Mass / (dist * dist * dist);

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
        Insert(0, particle.Position, particle.Mass, 0);

        //     // Start at root
        //     int nodeIndex = 0;
        //     int depth = 0;

        //     // Find leaf node
        //     while (depth < MaxDepth && Nodes[nodeIndex].IsInternal)
        //     {
        //         nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position);
        //         depth++;
        //     }

        //     // Add particle if leaf node is empty
        //     if (Nodes[nodeIndex].IsEmpty)
        //     {
        //         SpacialOctreeNode emptyNode = Nodes[nodeIndex];
        //         emptyNode.Mass = particle.Mass;
        //         emptyNode.CenterOfMass = particle.Position;
        //         Nodes[nodeIndex] = emptyNode;
        //         return;
        //     }

        //     Vector3d nodeCenterOfMass = Nodes[nodeIndex].CenterOfMass;
        //     double nodeMass = Nodes[nodeIndex].Mass;

        //     int insertIndex = nodeIndex;
        //     // Subdivide nodes until the positions are in separate quadrants
        //     while (insertIndex == nodeIndex && depth < MaxDepth)
        //     {
        //         nodeIndex = insertIndex;
        //         Subdivide(nodeIndex);

        //         insertIndex = Nodes[nodeIndex].GetOctContainingIndex(particle.Position);
        //         nodeIndex = Nodes[nodeIndex].GetOctContainingIndex(nodeCenterOfMass);
        //         depth++;
        //     }

        //     // Insert masses into the new nodes
        //     InsertIntoNode(nodeIndex, nodeCenterOfMass, nodeMass);
        //     InsertIntoNode(insertIndex, particle.Position, particle.Mass);
    }



    private void Insert(int nodeIndex, Vector3d position, double mass, int depth)
    {
        if (depth > MaxDepth)
        {
            MaxDepth = depth;
            Console.WriteLine($"New MaxDepth: {MaxDepth}");
        }
        // if (depth > 50) return;

            var node = Nodes[nodeIndex];

        // If node x does not contain a body, put the new body b here.
        if (node.IsEmpty)
        {
            node.Mass = mass;
            node.CenterOfMass = position;
            Nodes[nodeIndex] = node;
            return;
        }

        // If node x is an internal node, update the center-of-mass and total mass of x.
        // Recursively insert the body b in the appropriate quadrant.
        if (node.IsInternal)
        {
            Insert(node.GetOctContainingIndex(position), position, mass, depth + 1);

            node.CenterOfMass = ((node.Mass * node.CenterOfMass) + (mass * position)) / (node.Mass + mass);
            node.Mass += mass;

            Nodes[nodeIndex] = node;
            return;
        }

        // If node x is an external node, say containing a body named c, then there are two bodies
        // b and c in the same region. Subdivide the region further by creating four children.
        // Then, recursively insert both b and c into the appropriate quadrant(s). Since b and c may still end up in
        // the same quadrant, there may be several subdivisions during a single insertion. Finally,
        // update the center-of-mass and total mass of x.
        Subdivide(nodeIndex);

        Insert(Nodes[nodeIndex].GetOctContainingIndex(position), position, mass, depth + 1);
        Insert(Nodes[nodeIndex].GetOctContainingIndex(node.CenterOfMass), node.CenterOfMass, node.Mass, depth + 1);
        node = Nodes[nodeIndex];

        node.CenterOfMass = ((node.Mass * node.CenterOfMass) + (mass * position)) / (node.Mass + mass);
        node.Mass += mass;

        Nodes[nodeIndex] = node;
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
        var node = Nodes[nodeIndex];
        node.FirstChildIndex = NumNodes;
        Nodes[nodeIndex] = node;

        int numNodes = NumNodes;

        AABC[] subdividedAABCs = node.BoundingCube.SplitIntoOctants();
        for (int child = 0; child < 8; child++)
        {
            int next = child == 7 ? node.NextIndex : numNodes + child + 1;
            var childNode = new SpacialOctreeNode(subdividedAABCs[child], next);
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

            if (Math.Abs(internalNode.Mass - mass) > 0.001)
            {
                Console.WriteLine($"Mass changed");
            }

            if ((internalNode.CenterOfMass - centerOfMass).Length > 0.001)
            {
                Console.WriteLine($"com changed");
            }

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



    public readonly RenderObject ToRenderObject()
    {
        Vector4 position = new((Vector3)CenterOfMass, 1.0f);
        Vector4 velocity = new((Vector3)BoundingCube.Center, 1.0f);
        Vector4 attributes = new((float)Mass, (float)BoundingCube.Size, 0.0f, 0.0f);

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
        double halfSize = 0.5 * Size + 0.0001;
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
