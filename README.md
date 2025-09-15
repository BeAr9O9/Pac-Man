# Pac-Man Game

A C# Windows Forms implementation of the classic Pac-Man game.

## Features

### Game Mechanics
- **Classic Pac-Man Movement**: Navigate Pac-Man through the maze using arrow keys
- **Dot Collection**: Collect regular dots (`.`) worth 10 points each
- **Power Pellets**: Collect power pellets (`*`) worth 50 points each
- **Ghost AI**: Ghosts automatically chase Pac-Man using pathfinding
- **Power Mode**: When Pac-Man eats a power pellet, he enters power mode for 10 seconds

### Power Mode
When Pac-Man eats a power pellet:
- All ghosts become vulnerable (displayed in dark blue)
- Pac-Man can eat vulnerable ghosts for 200 points each
- Eaten ghosts respawn at their starting positions
- Power mode lasts for 10 seconds
- "POWER MODE!" indicator appears on screen

### Maze Format
The game uses a text-based maze format stored in `%APPDATA%\Pac-Man\maze.txt`:

```
25
25
#########################
#*......#.........*#
#.##.#######.#######.##.#
...
```

**Maze Elements:**
- `#` = Wall
- `.` = Regular dot (10 points)
- `*` = Power pellet (50 points, activates power mode)
- `P` = Pac-Man starting position
- `G` = Ghost starting position
- ` ` (space) or `0` = Empty space

### Controls
- **Arrow Keys**: Move Pac-Man (Up, Down, Left, Right)
- **Pause Button**: Pause/Resume the game

### Maze Editor
- Click "Edit maze" to modify the game maze
- Save custom mazes with power pellets
- Default maze includes 4 power pellets in the corners

## Installation & Running
This is a .NET Framework 4.7.2 Windows Forms application. Build and run using Visual Studio or MSBuild.

## Game Rules
1. Collect all dots and power pellets to win
2. Avoid ghosts unless they are vulnerable (power mode active)
3. Use power pellets strategically to clear ghosts and gain bonus points
4. Game ends when Pac-Man collides with a non-vulnerable ghost