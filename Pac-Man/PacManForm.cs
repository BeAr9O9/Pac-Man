using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace PacManWindowsForms
{
    public class PacManForm : Form
    {
        // Grid and maze settings.
        const int gridSize = 30;
        int rows = 25;
        int cols = 25;
        const int rMax = 100;
        const int cMax = 100;

        // Maze grid: 0 = empty, 1 = wall, 2 = dot.
        int[,] maze = new int[rMax, cMax];

        bool pacTeleport = false;

        // Pac-Man state: grid position and smooth screen position.
        Point pacGridPos;
        PointF pacScreenPos;
        // pacTarget holds the next grid cell Pac-Man is moving towards.
        Point? pacTarget = null;
        // Pac-Man's speed in pixels per tick.
        const float pacSpeed = 5f;

        // Ghost class to encapsulate each ghost's state.
        class Ghost
        {
            public bool ghostTeleport = false;

            public Point GridPos;
            public PointF ScreenPos;
            // The target grid cell of the ghost (the next cell along the shortest path to Pac-Man).
            public Point? Target;
        }
        List<Ghost> ghosts = new List<Ghost>();
        // Ghost speed in pixels per tick.
        const float ghostSpeed = 4f;

        // Timer for smooth animations (20ms interval for ~50 FPS).
        Timer gameTimer;

        // Data structures for Floyd–Warshall algorithm.
        // Mapping from a grid cell (Point) to an index.
        Dictionary<Point, int> cellToIndex = new Dictionary<Point, int>();
        // Mapping from index to grid cell (Point).
        List<Point> indexToCell = new List<Point>();
        // Next step matrix: next[i,j] = index of the next cell from i in the shortest path to j.
        int[,] nextMatrix;

        // Score field
        private int score = 0;

        // Use a large number as "infinity".
        const int INF = 1000000;

        private Bitmap pacManImage;
        private float pacManRotationAngle = 0f;

        public PacManForm()
        {
            InitializeComponent();

            this.ClientSize = new Size(cols * gridSize, rows * gridSize);
            this.DoubleBuffered = true;
            this.Text = "Pac-Man";

            InitializeMaze();
            BuildGraphAndComputePaths();

            // Initialize Pac-Man at a fixed start position (grid coordinate 1,1).
            pacScreenPos = GetCellCenter(pacGridPos);

            pacManImage = new Bitmap("pacman.png");

            // Add one ghost, starting near bottom right (ensure not a wall).
            Ghost ghost = new Ghost
            {
                GridPos = new Point(100, 100),
                ScreenPos = GetCellCenter(new Point(100, 100)),
                Target = null
            };
            ghosts.Add(ghost);

            gameTimer = new Timer();
            gameTimer.Interval = 20; // 20ms per tick ~50 FPS.
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += OnKeyDown;
        }

        // Initialize the maze: fill with dots and build borders and some inner walls.
        private void InitializeMaze()
        {
            StreamReader sr = new StreamReader(@"maze.txt");
            string line;
            line = sr.ReadLine();
            rows = int.Parse(line);
            line = sr.ReadLine();
            cols = int.Parse(line);

            // Fill maze with dots (2).
            for (int i = 0; i < rows; i++)
            {
                line = sr.ReadLine();
                for (int j = 0; j < cols; j++)
                {
                    if (line[j] == '0')
                        maze[i, j] = 0;
                    else if (line[j] == '#')
                        maze[i, j] = 1;
                    else if (line[j] == '.')
                        maze[i, j] = 2;
                    else if (line[j] == 'P')
                    {
                        maze[i, j] = 0;
                        pacGridPos = new Point(j, i);
                        pacScreenPos = GetCellCenter(pacGridPos);
                    }
                    else if (line[j] == 'G')
                    {
                        maze[i, j] = 0;
                        Ghost ghost = new Ghost
                        {
                            GridPos = new Point(j, i),
                            ScreenPos = GetCellCenter(new Point(j, i)),
                            Target = null
                        };
                        ghosts.Add(ghost);
                    }
                }
            }
        }

        // Build a graph from the maze and precompute shortest paths using Floyd–Warshall.
        private void BuildGraphAndComputePaths()
        {
            // Map each open cell (non-wall) to a unique index.
            cellToIndex.Clear();
            indexToCell.Clear();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (maze[i, j] != 1)
                    {
                        Point cell = new Point(j, i);
                        cellToIndex[cell] = indexToCell.Count;
                        indexToCell.Add(cell);
                    }
                }
            }
            int n = indexToCell.Count;
            int[,] dist = new int[n, n];
            nextMatrix = new int[n, n];

            // Initialize distances.
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        dist[i, j] = 0;
                        nextMatrix[i, j] = j;
                    }
                    else
                    {
                        dist[i, j] = INF;
                        nextMatrix[i, j] = -1;
                    }
                }
            }

            // For each cell, check its 4 neighbors (up, down, left, right).
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };
            foreach (var kv in cellToIndex)
            {
                Point cell = kv.Key;
                int iIdx = kv.Value;
                for (int dir = 0; dir < 4; dir++)
                {
                    int newX, newY;
                    if (cell.X == 0 && dx[dir] < 0)
                    {
                        newX = cols - 1;
                    }
                    else if (cell.X == cols - 1 && dx[dir] > 0)
                    {
                        newX = 0;
                    }
                    else
                    {
                        newX = cell.X + dx[dir];
                    }
                    if (cell.Y == 0 && dy[dir] < 0)
                    {
                        newY = rows - 1;
                    }
                    else if (cell.Y == rows - 1 && dy[dir] > 0)
                    {
                        newY = 0;
                    }
                    else
                    {
                        newY = cell.Y + dy[dir];
                    }

                    Point neighbor = new Point(newX, newY);
                    if (cellToIndex.ContainsKey(neighbor))
                    {
                        int jIdx = cellToIndex[neighbor];
                        dist[iIdx, jIdx] = 1;
                        nextMatrix[iIdx, jIdx] = jIdx;
                    }
                }
            }

            // Floyd–Warshall algorithm.
            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (dist[i, k] + dist[k, j] < dist[i, j])
                        {
                            dist[i, j] = dist[i, k] + dist[k, j];
                            nextMatrix[i, j] = nextMatrix[i, k];
                        }
                    }
                }
            }
        }

        // Returns the next grid cell along the shortest path from 'from' to 'to'.
        private Point? GetNextStep(Point from, Point to)
        {
            if (!cellToIndex.ContainsKey(from) || !cellToIndex.ContainsKey(to))
                return null;
            int i = cellToIndex[from];
            int j = cellToIndex[to];
            if (nextMatrix[i, j] == -1)
                return null;
            int nextIndex = nextMatrix[i, j];
            return indexToCell[nextIndex];
        }

        // Returns the center point (in screen coordinates) of a given grid cell.
        private PointF GetCellCenter(Point cell)
        {
            return new PointF(cell.X * gridSize + gridSize / 2, cell.Y * gridSize + gridSize / 2);
        }

        // The GameLoop is called every timer tick. It updates positions using smooth animations.
        private void GameLoop(object sender, EventArgs e)
        {
            // Update Pac-Man's movement.
            UpdatePacMan();

            // Update ghosts.
            foreach (var ghost in ghosts)
            {
                UpdateGhost(ghost);
            }

            // Check collision: if Pac-Man and any ghost are within a threshold distance.
            foreach (var ghost in ghosts)
            {
                PointF diff = ghost.ScreenPos.Subtract(pacScreenPos);
                if (diff.Length() < gridSize / 2)
                {
                    gameTimer.Stop();
                    MessageBox.Show($"Game Over! Your score: {score}");
                    Application.Exit();
                }
            }

            Invalidate(); // request redraw.
        }

        // Update Pac-Man's position: move smoothly from current cell center to target cell center.
        private void UpdatePacMan()
        {
            if (!pacTarget.HasValue)
            {
                // Set the target cell based on the current direction.
                Point desiredCell = new Point(updateX(pacGridPos.X, nextDirection.X), updateY(pacGridPos.Y, nextDirection.Y));
                if (maze[desiredCell.Y, desiredCell.X] != 1)
                {
                    // Change direction immediately.
                    currentDirection = nextDirection;
                }
                Point newCell = new Point(updateX(pacGridPos.X, currentDirection.X), updateY(pacGridPos.Y, currentDirection.Y));
                if ((pacGridPos.X == 0 && currentDirection.X == -1) || (pacGridPos.X == cols - 1 && currentDirection.X == 1) || (pacGridPos.Y == 0 && currentDirection.Y == -1) || (pacGridPos.Y == rows - 1 && currentDirection.Y == 1))
                {
                    if (maze[newCell.Y, newCell.X] != 1)
                        pacTeleport = true;
                }
                else
                {
                    pacTeleport = false;
                }

                // Validate move: within bounds and not a wall.
                if (newCell.X >= 0 && newCell.X < cols && newCell.Y >= 0 && newCell.Y < rows && maze[newCell.Y, newCell.X] != 1)
                {
                    pacTarget = newCell;
                }
            }

            if (pacTarget.HasValue)
            {
                // Target center position.
                PointF targetCenter = GetCellCenter(pacTarget.Value);
                if (!pacTeleport)
                {
                    // Compute vector from current position to target.
                    PointF diff = new PointF(targetCenter.X - pacScreenPos.X, targetCenter.Y - pacScreenPos.Y);
                    float distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                    if (distance < pacSpeed)
                    {
                        // Reached the target cell.
                        pacScreenPos = targetCenter;
                        pacGridPos = pacTarget.Value;
                        pacTarget = null;
                        // If there's a dot, eat it.
                        if (maze[pacGridPos.Y, pacGridPos.X] == 2)
                        {
                            maze[pacGridPos.Y, pacGridPos.X] = 0;
                            score += 10; // Increase score by 10 for each dot.
                            // Check if all dots are eaten.
                            if (IsAllDotsEaten())
                            {
                                gameTimer.Stop();
                                MessageBox.Show($"You Win! Your score: {score}");
                                Application.Exit();
                            }
                        }
                    }
                    else
                    {
                        // Normalize and move pacSpeed pixels.
                        float vx = diff.X / distance;
                        float vy = diff.Y / distance;
                        pacScreenPos = new PointF(pacScreenPos.X + vx * pacSpeed, pacScreenPos.Y + vy * pacSpeed);
                    }
                }
                else
                {
                    pacScreenPos = targetCenter;
                    pacGridPos = pacTarget.Value;
                    pacTarget = null;
                }
            }
        }

        // Check if all dots are eaten.
        private bool IsAllDotsEaten()
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (maze[i, j] == 2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Update a ghost's position with smoothing and choose its next move based on the shortest path.
        private void UpdateGhost(Ghost ghost)
        {
            Point? nextStep = GetNextStep(ghost.GridPos, pacGridPos);
            ghost.Target = nextStep.HasValue ? nextStep : ghost.GridPos;

            if (ghost.Target.HasValue)
            {
                if ((ghost.GridPos.X == 0 && ghost.Target.Value.X == cols - 1) || (ghost.GridPos.X == cols - 1 && ghost.Target.Value.X == 0) || (ghost.GridPos.Y == 0 && ghost.Target.Value.Y == rows - 1) || (ghost.GridPos.Y == rows - 1 && ghost.Target.Value.Y == 0))
                {
                    if (maze[ghost.Target.Value.Y, ghost.Target.Value.X] != 1)
                        ghost.ghostTeleport = true;
                }
                else
                {
                    ghost.ghostTeleport = false;
                }

                if (!ghost.ghostTeleport)
                {
                    // Move towards the target cell.

                    PointF targetCenter = GetCellCenter(ghost.Target.Value);
                    PointF diff = new PointF(targetCenter.X - ghost.ScreenPos.X, targetCenter.Y - ghost.ScreenPos.Y);
                    float distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                    if (distance < ghostSpeed)
                    {
                        // Reached the next grid cell.
                        ghost.ScreenPos = targetCenter;
                        ghost.GridPos = ghost.Target.Value;
                        ghost.Target = null; // Trigger recalculation on next update.
                    }
                    else
                    {
                        float vx = diff.X / distance;
                        float vy = diff.Y / distance;
                        ghost.ScreenPos = new PointF(ghost.ScreenPos.X + vx * ghostSpeed, ghost.ScreenPos.Y + vy * ghostSpeed);
                    }
                }
                else
                {
                    ghost.ScreenPos = GetCellCenter(ghost.Target.Value);
                    ghost.GridPos = ghost.Target.Value;
                    Point? nextStep1 = GetNextStep(ghost.GridPos, pacGridPos);
                    ghost.Target = nextStep1.HasValue ? nextStep1 : ghost.GridPos;
                }
            }
        }

        // Handle key presses to set Pac-Man's target cell.
        // Pac-Man moves one cell at a time. The move is initiated only if the next cell in that direction is not a wall.

        private Point currentDirection = new Point(0, 0);
        private Point nextDirection = new Point(0, 0);
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Always update the buffered nextDirection.
            if (e.KeyCode == Keys.Up)
            {
                nextDirection = new Point(0, -1);
                pacManRotationAngle = 270f;
            }
            else if (e.KeyCode == Keys.Down)
            {
                nextDirection = new Point(0, 1);
                pacManRotationAngle = 90f;
            }
            else if (e.KeyCode == Keys.Left)
            {
                nextDirection = new Point(-1, 0);
                pacManRotationAngle = 180f;
            }
            else if (e.KeyCode == Keys.Right)
            {
                nextDirection = new Point(1, 0);
                pacManRotationAngle = 0f;
            }
        }

        // Render the maze, Pac-Man, and ghosts.

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Draw the maze grid.
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Rectangle cellRect = new Rectangle(j * gridSize, i * gridSize, gridSize, gridSize);
                    if (maze[i, j] == 1)
                    {
                        g.FillRectangle(Brushes.Blue, cellRect);
                    }
                    else if (maze[i, j] == 2)
                    {
                        // Draw dot: a small yellow circle in the center of the cell.
                        int dotSize = gridSize / 3;
                        Rectangle dotRect = new Rectangle(cellRect.X + gridSize / 3, cellRect.Y + gridSize / 3, dotSize, dotSize);
                        g.FillEllipse(Brushes.Yellow, dotRect);
                    }
                }
            }

            // Draw Pac-Man as an orange circle.
            Rectangle pacRect = new Rectangle((int)(pacScreenPos.X - gridSize / 2), (int)(pacScreenPos.Y - gridSize / 2), gridSize, gridSize);
            g.TranslateTransform(pacRect.X + pacRect.Width / 2, pacRect.Y + pacRect.Height / 2);
            g.RotateTransform(pacManRotationAngle);
            g.TranslateTransform(-(pacRect.X + pacRect.Width / 2), -(pacRect.Y + pacRect.Height / 2));
            g.DrawImage(pacManImage, pacRect);
            g.ResetTransform();

            // Draw ghosts as red circles.
            foreach (var ghost in ghosts)
            {
                Rectangle ghostRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 2), (int)(ghost.ScreenPos.Y - gridSize / 2), gridSize, gridSize);
                g.FillEllipse(Brushes.Red, ghostRect);
            }

            // Draw the score.
            g.DrawString($"Score: {score}", new Font("Arial", 16), Brushes.White, new PointF(10, 10));
        }

        private int updateX(int cx, int dir)
        {
            if (dir == 0)
                return cx;
            if (dir == 1 && cx == cols - 1)
                return 0;
            if (dir == -1 && cx == 0)
                return cols - 1;
            return cx + dir;
        }

        private int updateY(int cy, int dir)
        {
            if (dir == 0)
                return cy;
            if (dir == 1 && cy == rows - 1)
                return 0;
            if (dir == -1 && cy == 0)
                return rows - 1;
            return cy + dir;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PacManForm
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 387);
            this.Name = "PacManForm";
            this.ResumeLayout(false);

        }
    }

    // Extension method for PointF subtraction and length calculation.
    public static class PointFExtensions
    {
        public static PointF Subtract(this PointF a, PointF b)
        {
            return new PointF(a.X - b.X, a.Y - b.Y);
        }

        public static float Length(this PointF pt)
        {
            return (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
        }
    }
}