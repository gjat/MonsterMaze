public class MazeStep
{
    public MazePoint Position {get; set;}
    public EntityAction Direction {get; set;}

    public MazeStep(MazePoint position, EntityAction direction)
    {
        Position = position;
        Direction = direction;
    }
}