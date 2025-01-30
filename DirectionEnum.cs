public enum EntityAction 
{
    None,
    Left,
    Up,
    Right,
    Down,
    Quit
}

public static class EntityActionExtensions
{
    public static EntityAction FromConsoleKey(ConsoleKeyInfo key)
    {
        return key.Key switch
        {
            ConsoleKey.LeftArrow => EntityAction.Left,
            ConsoleKey.RightArrow => EntityAction.Right,
            ConsoleKey.UpArrow => EntityAction.Up,
            ConsoleKey.DownArrow => EntityAction.Down,
            ConsoleKey.Escape => EntityAction.Quit,
            _ => key.KeyChar switch {
                'a' => EntityAction.Left,
                'A' => EntityAction.Left,
                'w' => EntityAction.Up,
                'W' => EntityAction.Up,
                'd' => EntityAction.Right,
                'D' => EntityAction.Right,
                's' => EntityAction.Down,
                'S' => EntityAction.Down,
                'q' => EntityAction.Quit,
                'Q' => EntityAction.Quit,
                _ => EntityAction.None
            }
        };
    }
}