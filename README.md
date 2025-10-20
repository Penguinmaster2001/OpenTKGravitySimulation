
# OpenTK Gravity Simulation

N-body particle simulator with efficient CPU and GPU computation with a custom renderer.

## Overview

### Motivation

I've always wanted my own n-body simulator. Who hasn't?

### Goals

- Decouple rendering and simulation performance
- Render many thousands of particles efficiently
- Compute physics of many thousands of particles on the CPU quickly
- Compute physics of many thousands of particles on the GPU
- Fancy rendering of particles, including redshift
- Do all this on my laptop

### Description

This project has three main components: CPU computation, GPU computation, and rendering.

For CPU computation, I use the [Barnes-Hut algorithm](http://arborjs.org/docs/barnes-hut) in 3 dimensions for a significant speedup.

A naive particle simulation requires O(n^2) calculations to update the simulation. The BH algorithm speeds this up significantly by treating sufficiently distant and tightly grouped particles as a single particle for force calculations. Speeding up to something like O(n * log(n)).

A size-distance ratio is set to determine how far and tightly bound the groups need to be. The tradeoff is simulation accuracy. A higher ratio is faster but less accurate.

The algorithm works by building a spacial octree each time step where each node contains the total mass and center of mass of all its children, and the leaf nodes each contain a single particle.

After the tree is built, the force on each particle is calculated by depth first traversal of the tree until a sufficiently small and distance node is found (based on the size-distance ratio), or a leaf node is found. The force for that node is applied to the particle. The traversal moves on to the node's sibling, skipping the computation for all its children.

The GPU computation is just a compute shader that updates the particles with the naive algorithm. It still runs very fast because of the massive parallelization for large numbers of particles, but slows down significantly compared to the CPU for very large amounts.

Rendering is done using OpenTK, which has bindings for OpenGL for C#. The particles are rendered as points with a radius, and the simulation has performance issues long before the rendering does. I also included some cool effects like redshift based on particle speed towards or away from the camera.

## Usage

No config file support yet. To change simulation parameters you need to edit the Main method in Program.cs.  

The `parameters` object and `Universe` constructor have the most "interesting" settings.  

To switch between CPU and GPU compute, comment and uncomment the `IParticleProcessor` implementations:  

```C#
// CPU
var particleProcessor = new CpuParticleProcessor();
// string computeShaderPath = "Compute/basic.compute";
// var computeShader = new ComputeShader(computeShaderPath);
// var particleProcessor = new GpuParticleProcessor(computeShader, gpuJobManager);
var universe = new Universe(10_000, 500.0, parameters, particleProcessor);

// GPU
// var particleProcessor = new CpuParticleProcessor();
string computeShaderPath = "Compute/basic.compute";
var computeShader = new ComputeShader(computeShaderPath);
var particleProcessor = new GpuParticleProcessor(computeShader, gpuJobManager);
var universe = new Universe(10_000, 500.0, parameters, particleProcessor);
```

Note that the GPU computations will block rendering, while CPU will not.  

For setting up how the particles spawn, the `Universe` constructor body needs to be edited.

Running:

```bash
% cd src/OpenTKGravitySim
% dotnet build -c Release OpenTKGravitySim.csproj
% ./bin/Release/net9.0/OpenTKGravitySim
```

Controls:

- esc to capture / free the mouse
- wasd for movement
- mouse for look
- scroll change movement speed
- ctrl + scroll change zoom
- shift + scroll change mouse sensitivity
- space run / pause
- alt + F4 exit

## Features

| name | status |
|-|-|
| Camera controls | done |
| OpenGL point rendering | done |
| GPU job management | done |
| BH acceleration on CPU | done |
| Configurable particle spawning | done |
| GPU computations | done |
| Injectable particle processors | done |
| Central OpenGL configuration | in progress |
| Physical light effects | in progress |
| Configuration with json | planned |
| GPU acceleration | planned |

## Built With

- C#
- OpenGL
- GLSL
- OpenTK

> Anthony Cieri [anthony.cieri@hotmail.com](mailto:anthony.cieri@hotmail.com) [anthonycieri.com](https://anthonycieri.com)
