public static class MazeUtils
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

    public static Tuple<MazePoint, bool> MoveInDirection(EntityAction userAction, MazePoint pos, char[,] maze)
    {
        var newPos = userAction switch
        {
            EntityAction.Up => new MazePoint(pos.X, pos.Y - 1),
            EntityAction.Left => new MazePoint(pos.X - 1, pos.Y),
            EntityAction.Down => new MazePoint(pos.X, pos.Y + 1),
            EntityAction.Right => new MazePoint(pos.X + 1, pos.Y),
            _ => new MazePoint(pos.X, pos.Y),
        };

        if(newPos.X < 0 || newPos.Y < 0 || newPos.X >= maze.GetLength(0) || newPos.Y >= maze.GetLength(1) || maze[newPos.X,newPos.Y] != ' ' )
        {
            return new (pos, false);  // can't move to the new location.
        }

        return new (newPos, true);
    }

// This is an intentional async background task, so suppress the warning.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public static async Task<List<MazeStep>> FindPathToTargetAsync(MazePoint targetPos, MazePoint currentPos,
        char[,] maze, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
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
                var (nextPos, isValid) = MazeUtils.MoveInDirection(direction, current, maze);
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

