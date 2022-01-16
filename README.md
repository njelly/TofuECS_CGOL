# TofuECS_CGOL
An implementation of Conway's Game of Life, made in Unity using [the TofuECS framework](https://github.com/njelly/TofuECS).

This is a very simple Unity project. The key takeaways should be that all ECS code is kept inside an Assembly Definition that allows unsafe code and prevents any UnityEngine references. This helps keep the project and the code organized by maintaining a clear separation of game logic (`ISystem` instances) and the rest of Unity, which should only contain view logic. 

[`SimulationRunner`](https://github.com/njelly/TofuECS_CGOL/blob/main/Assets/_Game/Scripts/SimulationRunner.cs) provides an example of how you can initialize a `Simulation`, register components, and respond to state changes (in this case, update a texture on a sprite when a cell on the board toggles its value).

[`BoardSystem`](https://github.com/njelly/TofuECS_CGOL/blob/main/Assets/_Game/Scripts/ECS/BoardSystem.cs) isn't a very typical example of an `ISystem` implementation. The only component in the game is `bool` rather than something user-created, `Query` is not used, and entities aren't utilized. However it does show how TofuECS can be *very* fast when iterating over buffers directly.
