# Monster Maze

A console maze game written in C# .NET 8.0.

<img src="./MonsterMazeScreenshot.png" width=640>

The player (green smiley face) has to get from the top left corner, to the bottom right of the maze (the exit).   At the same time, one or more monsters (red diamond) chase the player.   The player must avoid the monster and not get caught.

There are three levels, with each level adding an additional monster, to increase difficulty.

To run, fetch the code and then ```dotnet build; dotnet run``` within a command prompt or powershell window.

Published to a self-contained executable for x64 windows with:
```dotnet publish -c Release -r win-x64```

Other .NET plaforms should work, but haven't been tested.
