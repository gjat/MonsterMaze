public class MonsterMazeGame
{
    public const int MaxLevel = 3;
    
    // Found by looking at the available options in the "Character Map" windows system app
    //  viewing the Lucida Console font.
    const char WallCharacter = '\u2588';
    
    // Windows 11 Cascadia Code font doesn't have smiley face characters.   Sigh.  
    //  So reverting back to using standard text for the player and monsters, rather
    //  than using smiley faces, etc.
    const char PlayerCharacterA = 'O';
    const char PlayerCharacterB = 'o';
    const char MonsterCharacterA = 'M';
    const char MonsterCharacterB = 'm';
    const char CaughtCharacter = 'X';

    // Game state.
    private MazePoint playerPos;
    private int numMonsters;  // also the level number

    private MazePoint?[] monsterPos = new MazePoint?[MaxLevel];  // a point per monster (depending on the level)
    private List<MazeStep>[] monsterPath = new List<MazeStep>[MaxLevel];  // a list of steps per monster
    private CancellationTokenSource[] monsterPathCalcCancelSources = new CancellationTokenSource[MaxLevel];

    private char[,] theMaze = new char[1,1];

    private readonly int MaxWidth;
    private readonly int MaxHeight;

    public MonsterMazeGame(int maxWidth, int maxHeight)
    {
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
    }

    public bool PlayLevel(int levelNumber)
    {
        MakeMaze(MaxWidth, MaxHeight);

        // Initial positions
        numMonsters = levelNumber;
        playerPos = new MazePoint(0, 1);
        monsterPos[0] = new MazePoint(theMaze.GetLength(0)-1, theMaze.GetLength(1)-2);
        monsterPos[1] = levelNumber > 1 ? new MazePoint(1, theMaze.GetLength(1)-2) : null;
        monsterPos[2] = levelNumber > 2 ? new MazePoint(theMaze.GetLength(0)-2, 1) : null;

        for(int i = 0; i < levelNumber; i++)
        {
            StartMonsterPathCalculation(playerPos, i);
        }

        DisplayMaze(levelNumber: numMonsters);
        
        // returns true if the game is over, or the user wants to quit.
        return RunGameLoop();
    }

    protected bool RunGameLoop()
    {
        int loopCount = 0;
        while(true)
        {
            // Show the player and the monsters.   Using the loopCount as the basis for animation.
            ShowEntity(playerPos, loopCount % 20 < 10 ? PlayerCharacterA : PlayerCharacterB, ConsoleColor.Green);
            for(int i = 0; i < numMonsters; i++)
            {
                ShowEntity(monsterPos[i]!.Value, loopCount % 50 < 25 ? MonsterCharacterA : MonsterCharacterB, ConsoleColor.Red);
            }

            // Check to see if any of the monsters have reached the player.
            for(int i = 0; i < numMonsters; i++)
            {
                if(playerPos.X == monsterPos[i]?.X && playerPos.Y == monsterPos[i]?.Y)
                {
                    return DisplayCaught();
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
                (playerPos, var validPlayerMove) = MoveInDirection(userAction, playerPos);
                if(validPlayerMove)
                {
                    Console.SetCursorPosition(playerOldPos.X, playerOldPos.Y);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(".");
                    
                    // If the player is "outside of the border" on the right hand side, they've reached the one gap that is the exit.
                    if(playerPos.X == theMaze.GetLength(0)-1)  
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
                // Move the monster towards the player along the path previously calculated from the calculation tasks.
                bool validMonsterMove;
                for(int i = 0; i < numMonsters; i++)
                {
                    // If there is a path
                    if(monsterPath[i] != null && monsterPath[i].Count > 0)
                    {
                        MazePoint newPos;
                        ShowEntity(monsterPos[i]!.Value, ' ', ConsoleColor.Black);  // Clear where the monster was.
                        
                        (newPos, validMonsterMove) = MoveInDirection(monsterPath[i].First().Direction, monsterPos[i]!.Value);
                        
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

    protected void MakeMaze(int maxX, int maxY)
    {
        bool [,] mazeData;

        // Make sure dimensions are odd, as per the requirements of this algorithm
        if(maxX % 2 == 0)
            maxX--;

        if(maxY % 2 == 0)
            maxY--;

        mazeData = MazeRecursiveGenerator.GenerateMaze(maxX, maxY, MazeRecursiveGenerator.MazeMode.Loops);
        theMaze = GameUtils.ConvertToCharMaze(mazeData, WallCharacter);
    }

    protected static void ShowEntity(MazePoint entityPosition, char displayCharacter, ConsoleColor colour)
    {
        // A small helper to show either the player, or the monsters (depending on the parameters provided).
        Console.ForegroundColor = colour;
        Console.SetCursorPosition(entityPosition.X, entityPosition.Y);
        Console.Write(displayCharacter);
    }

    protected void DisplayMaze(int levelNumber)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;

        for(int y = 0; y < theMaze.GetLength(1); y++)
        {
            Console.SetCursorPosition(0,y);
            for(int x = 0; x < theMaze.GetLength(0); x++)
            {
                Console.Write(theMaze[x,y]);    
            }
        }
        
        Console.SetCursorPosition(0, theMaze.GetLength(1));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($" Lvl: {levelNumber}.  WASD or arrow keys to move.  Esc to quit.");
    }

    protected Tuple<MazePoint, bool> MoveInDirection(EntityAction userAction, MazePoint pos)
    {
        var newPos = userAction switch
        {
            EntityAction.Up => new MazePoint(pos.X, pos.Y - 1),
            EntityAction.Left => new MazePoint(pos.X - 1, pos.Y),
            EntityAction.Down => new MazePoint(pos.X, pos.Y + 1),
            EntityAction.Right => new MazePoint(pos.X + 1, pos.Y),
            _ => new MazePoint(pos.X, pos.Y),
        };

        if(newPos.X < 0 || newPos.Y < 0 || newPos.X >= theMaze.GetLength(0) || newPos.Y >= theMaze.GetLength(1) || theMaze[newPos.X,newPos.Y] != ' ' )
        {
            return new (pos, false);  // can't move to the new location.
        }

        return new (newPos, true);
    }


    protected bool DisplayCaught()
    {
        ShowEntity(playerPos, CaughtCharacter, ConsoleColor.Red);

        Console.SetCursorPosition((Console.WindowWidth-14)/2, Console.WindowHeight/2);
        Console.WriteLine("   You were caught!   ");

        Console.SetCursorPosition((Console.WindowWidth-14)/2, (Console.WindowHeight/2) +2);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press space to continue");

        GameUtils.WaitForEscapeOrSpace();
        return true;        
    }

    protected bool ShowLevelComplete()
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

        return GameUtils.WaitForEscapeOrSpace();
    }

    protected void StartMonsterPathCalculation(MazePoint playerPos, int monsterIndex)
    {
        if(monsterPathCalcCancelSources[monsterIndex] != null)
        {
            monsterPathCalcCancelSources[monsterIndex].Cancel();
            monsterPathCalcCancelSources[monsterIndex].Dispose();
        };
        monsterPathCalcCancelSources[monsterIndex] = new CancellationTokenSource();
        Task.Run(async () => monsterPath[monsterIndex] = await FindPathToTargetAsync(playerPos, monsterPos[monsterIndex]!.Value, monsterPathCalcCancelSources[monsterIndex].Token));
    }

    // This method should is a background task, ran on a threadpool thread, to calculate where the monsters should move.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected async Task<List<MazeStep>> FindPathToTargetAsync(MazePoint targetPos, MazePoint currentPos,
        CancellationToken cancellationToken)
#pragma warning restore CS1998 
    {
        var directions = new List<EntityAction> { EntityAction.Left, EntityAction.Right, EntityAction.Up, EntityAction.Down };
        var queue = new Queue<MazeStep>();
        var cameFrom = new Dictionary<MazePoint, MazeStep?>(); // To reconstruct the path
        var visited = new HashSet<MazePoint>();

        queue.Enqueue(new MazeStep(currentPos, EntityAction.None));
        visited.Add(currentPos);

        while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var currentStep = queue.Dequeue();
            var current = currentStep.Position;

            // If we've reached the target, reconstruct the path
            if (current.X == targetPos.X && current.Y == targetPos.Y)
                return ReconstructPath(cameFrom, currentPos, targetPos);

            foreach (var direction in directions)
            {
                var (nextPos, isValid) = MoveInDirection(direction, current);
                if (isValid && !visited.Contains(nextPos))
                {
                    visited.Add(nextPos);
                    queue.Enqueue(new MazeStep(nextPos, direction));
                    cameFrom[nextPos] = new MazeStep(current, direction);
                }
            }
        }
        return []; // No path found
    }

    private static List<MazeStep> ReconstructPath(Dictionary<MazePoint, MazeStep?> cameFrom, MazePoint start, MazePoint end)
    {
        var path = new List<MazeStep>();
        var current = end;

        while (current != start)
        {
            var prevStep = cameFrom[current];
            if (prevStep == null) 
                break;

            var direction = prevStep.Direction;
            path.Add(new MazeStep(current, direction));
            current = prevStep.Position;
        }

        path.Reverse();
        return path;
    }    
}