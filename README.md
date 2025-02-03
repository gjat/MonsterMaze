# Monster Maze

A console maze game written in C# .NET 8.0.

<img src="./MonsterMazeScreenshot.png" width=640>

The player (green "O") has to get from the top left corner, to the bottom right of the maze (the exit).   At the same time, one or more monsters (red "M") chase the player.   The player must avoid the monster and not get caught.

There are three levels, with each level adding an additional monster, to increase difficulty.

To run, fetch the code and then ```dotnet build; dotnet run``` within a command prompt or powershell window.

I've looked into creating a "setup" installer, however Windows smart screen and anti-virus software will flag unsigned software as an untrusted program.  Short of publishing via the Microsoft Store, or buying a code signing certificate ($$$), there's not a cheap way around this.

You can publish to a self-contained executable for x64 windows with:
```dotnet publish -c Release -r win-x64```

Other .NET plaforms should work, but haven't been tested.
