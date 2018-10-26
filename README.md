# freeserf.net
freeserf.net is a C# port of [freeserf](https://github.com/freeserf/freeserf).

At the moment there is still a lot of work to do.


## Roadmap

### Phase 1: Porting

The first step is to port everything from C++ to C# and ensure that the game runs.
There may be some quick&dirty implementations or things that could be done better.

### Phase 2: Optimizing

This includes bug fixing and C#-specific optimizations.
Moreover this includes performance and stability optimizations if needed.
Also the plan is to make everything cross-plattform as much as possible.

### Phase 3: Extending

This includes:

- New features
- Better usability
- Other things like mod support, tools and so on


## Future Goal

At the end this should become a stable and performant game that runs on many platforms and can be easily compiled and extended by .NET developers.

I am not sure how far this project will go as my time is very limited. I can not promise anything at this point.


## Implementation details

The first version will be a Visual Studio 2017 WinForms project with .NET 4.6.1. But the plan is to move most of the code to a .NET Standard library and leave only the window/surface/context creation to the executable.

In theory later every .NET project which provides an OpenGL context should be able to run the game. But we will see in the future how this works out.
