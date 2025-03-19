using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Media;

namespace PacManWindowsForms
{
    public class PacManForm : Form
    {
        // Grid and maze settings.
        public const int gridSize = 30;
        public int rows = 25;
        public int cols = 25;
        public const int rMax = 100;
        public const int cMax = 100;

        // Maze grid: 0 = empty, 1 = wall, 2 = dot.
        public int[,] maze = new int[rMax, cMax];

        public bool pacTeleport = false;

        // Pac-Man state: grid position and smooth screen position.
        public Point pacGridPos;
        public PointF pacScreenPos;
        // pacTarget holds the next grid cell Pac-Man is moving towards.
        public Point? pacTarget = null;
        // Pac-Man's speed in pixels per tick.
        public const float pacSpeed = 5f;

        public Point currentDirection = new Point(0, 0);
        public Point nextDirection = new Point(0, 0);

        SoundPlayer soundPlayer = new SoundPlayer("eat.wav");


        // Ghost class to encapsulate each ghost's state.
        public class Ghost
        {
            public bool ghostTeleport = false;
            public Point GridPos;
            public PointF ScreenPos;
            // The target grid cell of the ghost (the next cell along the shortest path to Pac-Man).
            public Point? Target;
            public float ghostSpeed = 4f;

            public Ghost(bool ghostTeleport, Point gridPos, PointF screenPos, Point? target, float ghostSpeed)
            {
                this.ghostTeleport = ghostTeleport;
                GridPos = gridPos;
                ScreenPos = screenPos;
                Target = target;
                this.ghostSpeed = ghostSpeed;
            }

            public void UpdateGhost(PacManForm form)
            {
                Point? nextStep = form.GetNextStep(this.GridPos, form.pacGridPos);
                this.Target = nextStep.HasValue ? nextStep : this.GridPos;

                if (this.Target.HasValue)
                {
                    if ((this.GridPos.X == 0 && this.Target.Value.X == form.cols - 1) || (this.GridPos.X == form.cols - 1 && this.Target.Value.X == 0) || (this.GridPos.Y == 0 && this.Target.Value.Y == form.rows - 1) || (this.GridPos.Y == form.rows - 1 && this.Target.Value.Y == 0))
                    {
                        if (form.maze[this.Target.Value.Y, this.Target.Value.X] != 1)
                            this.ghostTeleport = true;
                    }
                    else
                    {
                        this.ghostTeleport = false;
                    }

                    if (!this.ghostTeleport)
                    {
                        // Move towards the target cell.
                        PointF targetCenter = form.GetCellCenter(this.Target.Value);
                        PointF diff = new PointF(targetCenter.X - this.ScreenPos.X, targetCenter.Y - this.ScreenPos.Y);
                        float distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                        if (distance < ghostSpeed)
                        {
                            // Reached the next grid cell.
                            this.ScreenPos = targetCenter;
                            this.GridPos = this.Target.Value;
                            this.Target = null; // Trigger recalculation on next update.
                        }
                        else
                        {
                            float vx = diff.X / distance;
                            float vy = diff.Y / distance;
                            this.ScreenPos = new PointF(this.ScreenPos.X + vx * ghostSpeed, this.ScreenPos.Y + vy * ghostSpeed);
                        }
                    }
                    else
                    {
                        this.ScreenPos = form.GetCellCenter(this.Target.Value);
                        this.GridPos = this.Target.Value;
                        Point? nextStep1 = form.GetNextStep(this.GridPos, form.pacGridPos);
                        this.Target = nextStep1.HasValue ? nextStep1 : this.GridPos;
                    }
                }
            }
        }
        List<Ghost> ghosts = new List<Ghost>();




        // Timer for smooth animations (20ms interval for ~50 FPS).
        Timer gameTimer;

        // Data structures for Floyd–Warshall algorithm.
        // Mapping from a grid cell (Point) to an index.
        Dictionary<Point, int> cellToIndex = new Dictionary<Point, int>();
        // Mapping from index to grid cell (Point).
        List<Point> indexToCell = new List<Point>();
        // Next step matrix: next[i,j] = index of the next cell from i in the shortest path to j.
        int[,] nextMatrix;

        int totalDots = 0;

        // Score field
        private int score = 0;

        // Use a large number as "infinity".
        const int INF = 1000000;

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


            gameTimer = new Timer();
            gameTimer.Interval = 20; // 20ms per tick ~50 FPS.
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += OnKeyDown;
        }

        // Initialize the maze: fill with dots and build borders and some inner walls.
        public void InitializeMaze()
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
                    {
                        maze[i, j] = 2;
                        totalDots++;
                    }
                    else if (line[j] == 'P')
                    {
                        maze[i, j] = 0;
                        pacGridPos = new Point(j, i);
                        pacScreenPos = GetCellCenter(pacGridPos);
                    }
                    else if (line[j] == 'G')
                    {
                        maze[i, j] = 0;

                        Ghost ghost = new Ghost(false, new Point(j, i), GetCellCenter(new Point(j, i)), null, 4f);

                        ghosts.Add(ghost);
                    }
                }
            }
        }

        // Build a graph from the maze and precompute shortest paths using Floyd–Warshall.
        public void BuildGraphAndComputePaths()
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
        public Point? GetNextStep(Point from, Point to)
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
        public PointF GetCellCenter(Point cell)
        {
            return new PointF(cell.X * gridSize + gridSize / 2, cell.Y * gridSize + gridSize / 2);
        }
        public int updateX(int cx, int dir)
        {
            if (dir == 0)
                return cx;
            if (dir == 1 && cx == cols - 1)
                return 0;
            if (dir == -1 && cx == 0)
                return cols - 1;
            return cx + dir;
        }

        public int updateY(int cy, int dir)
        {
            if (dir == 0)
                return cy;
            if (dir == 1 && cy == rows - 1)
                return 0;
            if (dir == -1 && cy == 0)
                return rows - 1;
            return cy + dir;
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Always update the buffered nextDirection.
            if (e.KeyCode == Keys.Up)
                nextDirection = new Point(0, -1);
            else if (e.KeyCode == Keys.Down)
                nextDirection = new Point(0, 1);
            else if (e.KeyCode == Keys.Left)
                nextDirection = new Point(-1, 0);
            else if (e.KeyCode == Keys.Right)
                nextDirection = new Point(1, 0);
        }

        // Update Pac-Man's position: move smoothly from current cell center to target cell center.
        public void UpdatePacMan()
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

                            soundPlayer.Play();

                            score += 10; // Increase score by 10 for each dot.

                            totalDots--;

                            // Check if all dots are eaten.
                            if (totalDots == 0)
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

        // The GameLoop is called every timer tick. It updates positions using smooth animations.
        public void GameLoop(object sender, EventArgs e)
        {
            // Update Pac-Man's movement.
            UpdatePacMan();

            // Update ghosts.
            foreach (var ghost in ghosts)
            {
                ghost.UpdateGhost(this);
            }

            // Check collision: if Pac-Man and any ghost are within a threshold distance.
            foreach (var ghost in ghosts)
            {
                PointF diff = ghost.ScreenPos.Subtract(pacScreenPos);
                if (diff.Length() < gridSize / 2)
                {
                    SoundPlayer soundPlayer = new SoundPlayer("death.wav");
                    soundPlayer.Play();
                    gameTimer.Stop();
                    MessageBox.Show($"Game Over! Your score: {score}");
                    Application.Exit();
                }
            }

            Invalidate(); // request redraw.
        }




        // Handle key presses to set Pac-Man's target cell.
        // Pac-Man moves one cell at a time. The move is initiated only if the next cell in that direction is not a wall.




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

            if (currentDirection.X == 1)
            {
                using (Bitmap bmp = new Bitmap("pacman_right.png"))
                {
                    g.DrawImage(bmp, pacRect);
                }
            }
            else if (currentDirection.X == -1)
            {
                using (Bitmap bmp = new Bitmap("pacman_left.png"))
                {
                    g.DrawImage(bmp, pacRect);
                }
            }
            else if (currentDirection.Y == 1)
            {
                using (Bitmap bmp = new Bitmap("pacman_down.png"))
                {
                    g.DrawImage(bmp, pacRect);
                }
            }
            else if (currentDirection.Y == -1)
            {
                using (Bitmap bmp = new Bitmap("pacman_up.png"))
                {
                    g.DrawImage(bmp, pacRect);
                }
            }
            else
            {
                using (Bitmap bmp = new Bitmap("pacman_right.png"))
                {
                    g.DrawImage(bmp, pacRect);
                }
            }

            // Draw ghosts as red circles.
            foreach (var ghost in ghosts)
            {
                Rectangle ghostRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 2), (int)(ghost.ScreenPos.Y - gridSize / 2), gridSize, gridSize);

                using (Bitmap bmp = new Bitmap("ghost.png"))
                {
                    g.DrawImage(bmp, ghostRect);
                }
            }

            // Draw the score.
            g.DrawString($"Score: {score}", new Font("Arial", 16), Brushes.White, new PointF(10, 10));
        }



        public void InitializeComponent()
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