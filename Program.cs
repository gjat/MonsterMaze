public class Program
{
    private static ConsoleColor originalBackgroundColor;
    private static ConsoleColor originalForegroundColor;

    private static MazePoint playerCurrPos;
    private static MazePoint monsterPos;
    private static Tuple<MazePoint, MazePoint>? MonsterTarget;
    private static List<MazeStep> monsterPath = [];
    private static object monsterPathCalcLock = new();
    private static bool AppShutdown = false;
    private static char[,] TheMaze = new char[1,1];
    
    public static void Main(string[] args)
    {    
        Console.CursorVisible = false;
        Console.CancelKeyPress += new ConsoleCancelEventHandler(CleanupHandler);
        
        originalBackgroundColor = Console.BackgroundColor;
        originalForegroundColor = Console.ForegroundColor;
        Console.Clear();

        var maxX = Console.WindowWidth > 50 ? 50 : Console.WindowWidth-1;
        var maxY = Console.WindowHeight > 24 ? 24: Console.WindowHeight-2;
        
        bool [,] mazeData;

        string mazeMode = args.Length > 0 ? args[0] : "";
        if(mazeMode == "onepath")
        {
            // Make sure dimensions are odd, as per the requirements of this algorithm
            if(maxX % 2 == 0)
                maxX--;

            if(maxY % 2 == 0)
                maxY--;

            mazeData = MazeRecursiveGenerator.GenerateMaze(maxX, maxY);
        }
        else
        {
            // Make sure dimensions are odd, as per the requirements of this algorithm
            if(maxX % 2 == 0)
                maxX--;

            if(maxY % 2 == 0)
                maxY--;

            mazeData = MazeRecursiveGenerator.GenerateMaze(maxX, maxY, MazeRecursiveGenerator.MazeMode.Loops);
        }

        TheMaze = MazeUtils.ConvertToCharMaze(mazeData);

        DisplayMaze(TheMaze);

        // Initial positions
        playerCurrPos = new MazePoint(0, 1);
        monsterPos =  new MazePoint(maxX-1, maxY-2);

        var monsterCalcThread = new Thread(CalculateMonsterPath);
        monsterCalcThread.Start();
        lock(monsterPathCalcLock)
        {
            MonsterTarget = new(playerCurrPos, monsterPos);
        }

        int loopCount = 0;
        while(true)
        {
            ShowEntity(playerCurrPos, loopCount % 20 < 10 ? 'P' : 'p', ConsoleColor.Green);
            ShowEntity(monsterPos, loopCount % 50 < 25 ? 'M' : 'm', ConsoleColor.Red);

            if(Console.KeyAvailable)
            {
                var userAction = EntityActionExtensions.FromConsoleKey(Console.ReadKey(true));

                if(userAction == EntityAction.Quit)
                {
                    CleanupHandler(null, null);
                    AppShutdown = true;
                    return;
                }
                MazePoint playerOldPos = playerCurrPos;
                (playerCurrPos, var validPlayerMove) = MazeUtils.MoveInDirection(userAction, playerCurrPos, TheMaze);
                if(validPlayerMove)
                {
                    Console.SetCursorPosition(playerOldPos.X, playerOldPos.Y);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(".");
                    
                    // Start a new calculation of the monster's path
                    lock(monsterPathCalcLock)
                    {
                        MonsterTarget = new(playerCurrPos, monsterPos);
                    }
                }

                if(playerCurrPos.X == maxX-1)  // getting "outside of the border" on the right hand side is the exit.
                {
                    ShowEntity(playerCurrPos, 'P', ConsoleColor.Green);  // Show the player at the exit.
                    AppShutdown = true;
                    Console.ForegroundColor = originalForegroundColor;
                    Console.BackgroundColor = originalBackgroundColor;
                    Console.SetCursorPosition((Console.WindowWidth-14)/2, Console.WindowHeight/2);
                    Console.WriteLine("   You win!   ");
                    Console.SetCursorPosition(0, Console.WindowHeight- 3);
                    Console.WriteLine("Press ESC to exit");
                    while(Console.ReadKey(true).Key != ConsoleKey.Escape) 
                    {
                        // waiting
                    }
                    return;
                }
            }

            if(loopCount % 10 == 1)
            {
                // Move the monster towards the player
                bool validMonsterMove;
                lock(monsterPathCalcLock)
                {
                    if(monsterPath.Count > 0)
                    {
                        ShowEntity(monsterPos, ' ', ConsoleColor.Black);  // Clear where the monster was.
                        (monsterPos, validMonsterMove) = MazeUtils.MoveInDirection(monsterPath.First().Direction, monsterPos, TheMaze);
                        monsterPath.RemoveAt(0);
                        if(!validMonsterMove) 
                        {
                            // Um, something went wrong with following the steps (bug in code).
                            // issue a recalculate
                            monsterPath = [];
                            MonsterTarget = new(playerCurrPos, monsterPos);
                        }
                    }
                }
            }

            if(playerCurrPos.X == monsterPos.X && playerCurrPos.Y == monsterPos.Y)
            {
                // Caught!
                AppShutdown = true;
                ShowEntity(playerCurrPos, 'X', ConsoleColor.Red);
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
                return;
            }

            loopCount++;  
            if(loopCount > 100) 
                loopCount = 0;
            Thread.Sleep(50);
        }
    }

    protected static void ShowEntity(MazePoint entityPosition, char displayCharacter, ConsoleColor colour)
    {
        Console.ForegroundColor = colour;
        Console.SetCursorPosition(entityPosition.X, entityPosition.Y);
        Console.Write(displayCharacter);
    }

    protected static void DisplayMaze(char [,] maze)
    {
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
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(" WASD or arrow keys to move.  Esc to quit.");
    }

    protected static void CleanupHandler(object? sender, ConsoleCancelEventArgs? args)
    {
        Console.ForegroundColor = originalForegroundColor;
        Console.BackgroundColor = originalBackgroundColor;
        Console.Clear();
    }

    public static void CalculateMonsterPath()
    {
        // Doing this in a separate thread, to avoid pausing game play.
        while(!AppShutdown)
        {
            Tuple<MazePoint, MazePoint>? pathToCalculate = null;
            lock(monsterPathCalcLock)
            {
                pathToCalculate = MonsterTarget;
                MonsterTarget = null;
            }

            if(pathToCalculate != null)
            {
                var tmpPath = MazeUtils.FindPathToTarget(pathToCalculate.Item1, pathToCalculate.Item2, TheMaze);
                lock(monsterPathCalcLock)
                {
                    monsterPath = tmpPath;
                }
            }
            Thread.Sleep(100);
        }
    }
}
