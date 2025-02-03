public static class GameUtils
{
    public static char[,] ConvertToCharMaze(bool[,] maze, char wallCharacter = '#')
    {
        var result = new char[maze.GetLength(0), maze.GetLength(1)];
        for (int i = 0; i < maze.GetLength(0); i++)
        {
            for (int j = 0; j < maze.GetLength(1); j++)
            {
                result[i, j] = maze[i, j] ? ' ' : wallCharacter;
            }
        }
        return result;
    }

    public static bool WaitForEscapeOrSpace()
    {
        var key = Console.ReadKey(true).Key;
        while(key != ConsoleKey.Spacebar && key != ConsoleKey.Escape) 
        {
            key = Console.ReadKey(true).Key;
        }
        return key == ConsoleKey.Escape;
    }
}

