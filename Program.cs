public class Program
{
    const int MaxLevel = 3;
    
    // Find the available options in the "Character Map" windows system app
    //  viewing the Lucida Console font
    const char WallCharacter = '\u2588';
    
    // Windows 11 Cascadia Code font doesn't have smiley face characters.   Sigh.  
    //  So going back to using standard text for the player and monsters.  
    const char PlayerCharacterA = 'O';
    const char PlayerCharacterB = 'o';
    const char MonsterCharacterA = 'M';
    const char MonsterCharacterB = 'm';
    const char CaughtCharacter = 'X';

    // Save console colours, to restore state after the game ends.
    private static ConsoleColor originalBackgroundColor;
    private static ConsoleColor originalForegroundColor;

    // Global game state.
    private static MazePoint playerPos;
    private static int numMonsters;  // also the level number

    private static MazePoint?[] monsterPos = new MazePoint?[MaxLevel];  // a point per monster (depending on the level)
    private static List<MazeStep>[] monsterPath = new List<MazeStep>[MaxLevel];  // a list of steps per monster
    private static CancellationTokenSource[] monsterPathCalcCancel = new CancellationTokenSource[MaxLevel];

    private static char[,] TheMaze = new char[1,1];
    
    public static void Main(string[] args)
    {    
        Console.CursorVisible = false;
        Console.CancelKeyPress += new ConsoleCancelEventHandler(CleanupHandler);
        
        originalBackgroundColor = Console.BackgroundColor;
        originalForegroundColor = Console.ForegroundColor;

        var maxX = Console.WindowWidth > 50 ? 50 : Console.WindowWidth-1;
        var maxY = Console.WindowHeight > 24 ? 24: Console.WindowHeight-2;

        bool quitGame = false;
        while(!quitGame)
        {
            for(numMonsters = 1; numMonsters <= MaxLevel; numMonsters++)
            {
                MakeMaze(maxX, maxY);
                DisplayMaze(TheMaze, levelNumber: numMonsters);

                // Initial positions
                playerPos = new MazePoint(0, 1);
                monsterPos[0] = new MazePoint(TheMaze.GetLength(0)-1, TheMaze.GetLength(1)-2);
                monsterPos[1] = numMonsters > 1 ? new MazePoint(1, TheMaze.GetLength(1)-2) : null;
                monsterPos[2] = numMonsters > 2 ? new MazePoint(TheMaze.GetLength(0)-2, 1) : null;

                for(int i = 0; i < numMonsters; i++)
                {
                    StartMonsterPathCalculation(playerPos, i);
                }

                quitGame = PlayLevel();
                if(quitGame)
                    break;
            }


        }
        CleanupHandler(null, null);
    }

    protected static bool PlayLevel()
    {
        int loopCount = 0;
        while(true)
        {
            ShowEntity(playerPos, loopCount % 20 < 10 ? PlayerCharacterA : PlayerCharacterB, ConsoleColor.Green);
            for(int i = 0; i < numMonsters; i++)
            {
                ShowEntity(monsterPos[i]!.Value, loopCount % 50 < 25 ? MonsterCharacterA : MonsterCharacterB, ConsoleColor.Red);
            }

            // Check to see if any of the "active" monsters have reached the player.
            for(int i = 0; i < numMonsters; i++)
            {
                if(playerPos.X == monsterPos[i]?.X && playerPos.Y == monsterPos[i]?.Y)
                {
                    // Caught!
                    ShowEntity(playerPos, CaughtCharacter, ConsoleColor.Red);
                    Console.SetCursorPosition((Console.WindowWidth-14)/2, Console.WindowHeight/2);
                    Console.WriteLine("   You were caught!   ");
                    Console.SetCursorPosition(0, Console.WindowHeight- 3);

                    Console.ForegroundColor = originalForegroundColor;
                    Console.BackgroundColor = originalBackgroundColor;

                    Console.WriteLine("Press ESC to exit");
                    while(Console.ReadKey(true).Key != ConsoleKey.Escape) 
                    {
                        // waiting
                    }
                    return true;
                }
            }

            if(Console.KeyAvailable)
            {
                var userAction = EntityActionExtensions.FromConsoleKey(Console.ReadKey(true));

                if(userAction == EntityAction.Quit)
                {
                    return true;
                }

                // Soak up any other keypresses (avoid key buffering)
                while(Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }

                // Try to move the player, and start recalculating monster paths if the player does move
                MazePoint playerOldPos = playerPos;
                (playerPos, var validPlayerMove) = MazeUtils.MoveInDirection(userAction, playerPos, TheMaze);
                if(validPlayerMove)
                {
                    Console.SetCursorPosition(playerOldPos.X, playerOldPos.Y);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(".");
                    
                    // If the player is "outside of the border" on the right hand side, they've reached the one gap that is the exit.
                    if(playerPos.X == TheMaze.GetLength(0)-1)  
                    {
                        return ShowLevelComplete();
                    }

                    // Start a new calculation of the monster's path
                    for(int i = 0; i < numMonsters; i++)
                    {
                        StartMonsterPathCalculation(playerPos, i);
                    }
                }
            }

            // Move the monsters slower than the player can move.
            if(loopCount % 10 == 1)
            {
                // Move the monster towards the player along the path calculated from the calculation thread.
                bool validMonsterMove;

                for(int i = 0; i < numMonsters; i++)
                {
                    if(monsterPath[i] != null && monsterPath[i].Count > 0)
                    {
                        MazePoint newPos;
                        ShowEntity(monsterPos[i]!.Value, ' ', ConsoleColor.Black);  // Clear where the monster was.
                        (newPos, validMonsterMove) = MazeUtils.MoveInDirection(monsterPath[i].First().Direction, monsterPos[i]!.Value, TheMaze);
                        monsterPos[i] = newPos;
                        monsterPath[i].RemoveAt(0);
                        if(!validMonsterMove) 
                        {
                            // Um, something went wrong with following the steps (bug in code).
                            // issue a recalculate
                            monsterPath[i] = [];
                            StartMonsterPathCalculation(playerPos, i);
                        }
                    }
                }
            }

            loopCount++;  
            if(loopCount > 100) 
                loopCount = 0;
            Thread.Sleep(50);
        }
    }

    protected static void MakeMaze(int maxX, int maxY)
    {
        bool [,] mazeData;

        // Make sure dimensions are odd, as per the requirements of this algorithm
        if(maxX % 2 == 0)
            maxX--;

        if(maxY % 2 == 0)
            maxY--;

        mazeData = MazeRecursiveGenerator.GenerateMaze(maxX, maxY, MazeRecursiveGenerator.MazeMode.Loops);
        TheMaze = MazeUtils.ConvertToCharMaze(mazeData, WallCharacter);
    }

    protected static void ShowEntity(MazePoint entityPosition, char displayCharacter, ConsoleColor colour)
    {
        // A small helper to show either the player, or the monsters (depending on the parameters provided).
        Console.ForegroundColor = colour;
        Console.SetCursorPosition(entityPosition.X, entityPosition.Y);
        Console.Write(displayCharacter);
    }

    protected static void DisplayMaze(char [,] maze, int levelNumber)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;

        for(int y = 0; y < maze.GetLength(1); y++)
        {
            Console.SetCursorPosition(0,y);
            for(int x = 0; x < maze.GetLength(0); x++)
            {
                Console.Write(maze[x,y]);    
            }
        }
        
        Console.SetCursorPosition(0,maze.GetLength(1));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($" Lvl: {levelNumber}.  WASD or arrow keys to move.  Esc to quit.");
    }

    protected static bool ShowLevelComplete()
    {
        ShowEntity(playerPos, PlayerCharacterA, ConsoleColor.Green);  // Show the player at the exit.
                    
        if(numMonsters < MaxLevel)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition((Console.WindowWidth-40)/2, Console.WindowHeight/2);
            Console.WriteLine(" You escaped, ready for the next level? ");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition((Console.WindowWidth-14)/2, Console.WindowHeight/2);
            Console.WriteLine("   You won!   ");
        }
        
        Console.SetCursorPosition((Console.WindowWidth-38)/2, (Console.WindowHeight/2)+2);
        Console.WriteLine("Press space to continue or Esc to exit");
        
        while(true)
        {
            var key = Console.ReadKey(true).Key;
            switch(key)
            {
                case ConsoleKey.Escape:
                    return true;
                case ConsoleKey.Spacebar:
                    return false;
            }                        
        }
    }

    // If "escape" or "control-c" is pressed, try to get the console window back into a clean state.
    protected static void CleanupHandler(object? sender, ConsoleCancelEventArgs? args)
    {
        Console.ForegroundColor = originalForegroundColor;
        Console.BackgroundColor = originalBackgroundColor;
        Console.Clear();
    }

    protected static void StartMonsterPathCalculation(MazePoint playerPos, int monsterIndex)
    {
        if(monsterPathCalcCancel[monsterIndex] != null)
        {
            monsterPathCalcCancel[monsterIndex].Cancel();
            monsterPathCalcCancel[monsterIndex].Dispose();
        };
        monsterPathCalcCancel[monsterIndex] = new CancellationTokenSource();
        Task.Run(async () => monsterPath[monsterIndex] = await MazeUtils.FindPathToTargetAsync(playerPos, monsterPos[monsterIndex]!.Value, TheMaze, monsterPathCalcCancel[monsterIndex].Token));
    }
}
