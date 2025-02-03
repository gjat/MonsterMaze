public class Program
{

    // Save console colours, to restore state after the game ends.
    private static ConsoleColor originalBackgroundColor;
    private static ConsoleColor originalForegroundColor;
    
    public static void Main()
    {    
        Console.CursorVisible = false;
        Console.CancelKeyPress += new ConsoleCancelEventHandler(CleanupHandler);
        
        originalBackgroundColor = Console.BackgroundColor;
        originalForegroundColor = Console.ForegroundColor;

        var maxWidth = Console.WindowWidth > 50 ? 50 : Console.WindowWidth-1;
        var maxHeight = Console.WindowHeight > 24 ? 24: Console.WindowHeight-2;

        var game = new MonsterMazeGame(maxWidth, maxHeight);

        bool quitGame = false;
        while(!quitGame)
        {
            ShowTitleScreen();
            
            if(GameUtils.WaitForEscapeOrSpace() != true)
            {
                bool gameOver = false;
                for(int levelNumber = 1; levelNumber <= MonsterMazeGame.MaxLevel && !gameOver; levelNumber++)
                {
                    gameOver = game.PlayLevel(levelNumber);
                }
            }
            else
            {
                // Player wants to quit the game
                quitGame = true;
            }
        }
        CleanupHandler(null, null);
    }

    protected static void ShowTitleScreen()
    {
        Console.Clear();
        

        Console.SetCursorPosition(Console.WindowWidth/2-20, 5);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("### ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Monster Maze");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(" ###");

        Console.SetCursorPosition(0, 10);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("You are trapped in a maze with monsters. Your goal is to escape.");
        Console.WriteLine("Use the arrow keys to move, avoid the monsters.");
        Console.WriteLine();
        Console.WriteLine("Press space to start, or escape to quit.");
    }

    // If "escape" or "control-c" is pressed, try to get the console window back into a clean state.
    protected static void CleanupHandler(object? sender, ConsoleCancelEventArgs? args)
    {
        Console.ForegroundColor = originalForegroundColor;
        Console.BackgroundColor = originalBackgroundColor;
        Console.Clear();
    }

}
