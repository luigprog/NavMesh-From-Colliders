# UnityNavMeshFromColliders
The Unity's NavMesh System is used to move objects in the game environment using pathfinding. This object is an agent, and it needs information to recognize the world it is supposed to walk on. This information is achieved by baking your scene, generating a "blue" geometric mesh representing the walkable area of your environment.

But analyzing the Unity's Navmesh bake process, it actually generates the navmesh based on **renderers, not in colliders**.

This too will help **preparing your scene for the NavMesh baking process**. As we know, the Unity's bake process is based on renderers, with that information, the tool **creates temporary objects** with mesh renderers generated **from the original object's collider**.

[Download Play Demo](https://github.com/garcialuigi/ArcadeVehiclesPhysics/releases)
