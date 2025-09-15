# Pac-Man Game

A classic Pac-Man game implementation in C# Windows Forms.

## Features

- Classic Pac-Man gameplay
- Maze editing functionality
- Two different asset modes (Funny/Normal)
- Ghost AI with pathfinding
- **NEW: Ghor entities** - Special ghost variant with enhanced abilities

## Ghor Feature

Ghor is a special type of ghost entity that differs from regular ghosts:

- **Faster Movement**: Ghor moves 1.5x faster than regular ghosts
- **Random Behavior**: 30% of the time, Ghor will move in a random direction instead of chasing the player
- **Visual Distinction**: Ghor appears as a regular ghost with a red overlay
- **Maze Character**: Use 'H' in maze files to place Ghor entities

## Maze Format

The maze file format supports the following characters:
- `#` - Wall
- `.` - Dot (collectible)
- `0` - Empty space
- `P` - Pac-Man starting position
- `G` - Regular ghost
- `H` - Ghor (special ghost)

## Playing

1. Run the game
2. Click "Start game" to begin
3. Use arrow keys to move Pac-Man
4. Collect all dots while avoiding ghosts and Ghor
5. Ghor will appear with a red tint and move unpredictably

## Editing Mazes

1. Click "Edit maze" to open the maze editor
2. Create a 25x25 maze using the supported characters
3. Save the maze to test your custom layouts
4. Include 'H' characters to add Ghor entities to your maze