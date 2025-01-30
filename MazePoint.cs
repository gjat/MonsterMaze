public struct MazePoint(int x, int y)
{
    public int X { get; set; } = x; 
    public int Y { get; set; } = y;

    public override bool Equals(object? obj)
    {
        if(obj is not MazePoint)
            return false;
        
        var other = (MazePoint)obj;
        return other.X == X && other.Y == Y;
    }
    public static bool operator ==(MazePoint left, MazePoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MazePoint left, MazePoint right)
    {
        return !(left == right);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}
