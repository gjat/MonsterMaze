public class MazeRecursiveGenerator
{
    public enum MazeMode {
        OnePath,
        FilledDeadEnds,
        Loops
    };

    private static readonly (int, int)[] Directions = { (0, -1), (1, 0), (0, 1), (-1, 0) }; // Up, Right, Down, Left
    private static Random random = new Random();

    public static bool[,] GenerateMaze(int width, int height, MazeMode mazeMode = MazeMode.OnePath)
    {
        if (width % 2 == 0 || height % 2 == 0)
            throw new ArgumentException("Width and height must be odd numbers for a proper maze.");

        bool[,] maze = new bool[width, height];  // by default, everything is a wall (cell value == false)

        // Start the maze generation
        GenerateMazeRecursive(maze, 1, 1);

        // Make sure the entrance and exit are open
        maze[0, 1] = true; // Entrance
        maze[width - 1, height - 2] = true; // Exit

        if(mazeMode == MazeMode.FilledDeadEnds)
            FillDeadEnds(maze);
        
        else if(mazeMode == MazeMode.Loops)
            RemoveDeadEnds(maze);

        return maze;
    }

    private static void GenerateMazeRecursive(bool[,] maze, int x, int y)
    {
        maze[x, y] = true;

        // Shuffle directions
        var shuffledDirections = ShuffleDirections();

        foreach (var (dx, dy) in shuffledDirections)
        {
            int nx = x + dx * 2;
            int ny = y + dy * 2;

            // Check if the new position is within bounds and not visited
            if (IsInBounds(maze, nx, ny) && !maze[nx, ny])
            {
                // Carve a path
                maze[x + dx, y + dy] = true;
                GenerateMazeRecursive(maze, nx, ny);
            }
        }
    }

    private static List<(int, int)> ShuffleDirections()
    {
        var directions = new List<(int, int)>(Directions);
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (directions[i], directions[j]) = (directions[j], directions[i]);
        }
        return directions;
    }

    private static bool IsInBounds(bool[,] maze, int x, int y)
    {
        return x > 0 && y > 0 && x < maze.GetLength(0) - 1 && y < maze.GetLength(1) - 1;
    }

    private static void FillDeadEnds(bool[,] maze)
    {
        bool removed;
        do
        {
            removed = false;
            for (int x = 1; x < maze.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < maze.GetLength(1) - 1; y++)
                {
                    if (maze[x, y]) // If it's a path
                    {
                        int neighbors = 0;
                        foreach (var (dx, dy) in Directions)
                        {
                            if (maze[x + dx, y + dy])
                                neighbors++;
                        }
                        if (neighbors <= 1) // If it's a dead end
                        {
                            maze[x, y] = false;
                            removed = true;
                        }
                    }
                }
            }
        } while (removed);
    }

    private static void RemoveDeadEnds(bool[,] maze)
    {
        bool removed;
        do
        {
            removed = false;
            for (int x = 1; x < maze.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < maze.GetLength(1) - 1; y++)
                {
                    if (maze[x, y]) // If it's a path
                    {
                        int neighbors = 0;
                        foreach (var (dx, dy) in Directions)
                        {
                            if (maze[x + dx, y + dy])
                                neighbors++;
                        }
                        if (neighbors <= 1) // If it's a dead end
                        {
                            // Pick a random neighbor to keep open
                            var shuffledDirections = ShuffleDirections();
                            foreach(var (dx, dy) in shuffledDirections)
                            {
                                if(IsInBounds(maze, x + dx, y + dy) && !maze[x + dx, y + dy])
                                {
                                    maze[x + dx, y + dy] = true;
                                    break;
                                }
                            }
                            removed = true;
                        }
                    }
                }
            }
        } while (removed);
    }

}
