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

        // Maze grid: 0 = empty, 1 = wall, 2 = dot, 3 = cherry power-up
        int[,] maze = new int[rMax, cMax];

        bool pacTeleport = false;

        // Pac-Man state: grid position and smooth screen position.
        Point pacGridPos;
        PointF pacScreenPos;
        // pacTarget holds the next grid cell Pac-Man is moving towards.
        Point? pacTarget = null;
        // Pac-Man's speed in pixels per tick.
        const float pacSpeed = 5f;

        // Power-up state
        private bool powerUpActive = false;
        private int powerUpTimer = 0;
        private const int powerUpDuration = 300; // 300 ticks = approx. 6 seconds at 20ms per tick
        private Bitmap cherryImage;
        private Point ghostSpawnPoint = new Point(12, 12); // Default spawn point, update as needed

        // Ghost class to encapsulate each ghost's state.
        class Ghost
        {
            public bool ghostTeleport = false;
            public bool isVulnerable = false;
            public bool isAtSpawn = false; // Indică dacă fantoma este la punctul de spawn
            public Point spawnPoint;

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

        // Puncte pentru fuga fantomelor
        private List<Point> scatterPoints = new List<Point>();
        private Random random = new Random();

        public PacManForm()
        {
            InitializeComponent();

            this.ClientSize = new Size(cols * gridSize, rows * gridSize);
            this.DoubleBuffered = true;
            this.Text = "Pac-Man";

            InitializeMaze();
            BuildGraphAndComputePaths();
            InitializeScatterPoints();

            // Initialize Pac-Man at a fixed start position (grid coordinate 1,1).
            pacScreenPos = GetCellCenter(pacGridPos);

            pacManImage = new Bitmap("pacman.png");
            try
            {
                cherryImage = new Bitmap("cherry.png"); // Make sure to add a cherry image to your project
            }
            catch
            {
                // If cherry image isn't available, we'll use a simple circle instead
            }

            // Add one ghost, starting near bottom right (ensure not a wall).
            Ghost ghost = new Ghost
            {
                GridPos = new Point(100, 100),
                ScreenPos = GetCellCenter(new Point(100, 100)),
                Target = null,
                spawnPoint = new Point(100, 100)
            };
            ghosts.Add(ghost);

            gameTimer = new Timer();
            gameTimer.Interval = 20; // 20ms per tick ~50 FPS.
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += OnKeyDown;
        }

        // Inițializarea punctelor de fugă (colțurile labirintului)
        private void InitializeScatterPoints()
        {
            scatterPoints.Add(new Point(1, 1));                 // Colțul stânga sus
            scatterPoints.Add(new Point(cols - 2, 1));          // Colțul dreapta sus
            scatterPoints.Add(new Point(1, rows - 2));          // Colțul stânga jos
            scatterPoints.Add(new Point(cols - 2, rows - 2));   // Colțul dreapta jos

            // Asigură-te că punctele alese nu sunt ziduri
            for (int i = 0; i < scatterPoints.Count; i++)
            {
                Point p = scatterPoints[i];
                if (maze[p.Y, p.X] == 1) // Dacă e zid, caută o celulă liberă în apropiere
                {
                    bool found = false;
                    for (int offsetY = -2; offsetY <= 2 && !found; offsetY++)
                    {
                        for (int offsetX = -2; offsetX <= 2 && !found; offsetX++)
                        {
                            int newX = p.X + offsetX;
                            int newY = p.Y + offsetY;
                            if (newX >= 0 && newX < cols && newY >= 0 && newY < rows && maze[newY, newX] != 1)
                            {
                                scatterPoints[i] = new Point(newX, newY);
                                found = true;
                            }
                        }
                    }
                }
            }
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
                        Point ghostPos = new Point(j, i);
                        Ghost ghost = new Ghost
                        {
                            GridPos = ghostPos,
                            ScreenPos = GetCellCenter(ghostPos),
                            Target = null,
                            spawnPoint = ghostPos
                        };
                        ghosts.Add(ghost);
                        ghostSpawnPoint = ghostPos; // Store a default ghost spawn point
                    }
                    else if (line[j] == 'C') // Add 'C' in your maze.txt file to place cherries
                    {
                        maze[i, j] = 3; // 3 = cherry power-up
                    }
                }
            }

            // If no cherries were defined in the maze file, add some randomly
            AddCherriesToMaze();
        }

        // Add cherry power-ups to the maze at random positions
        private void AddCherriesToMaze()
        {
            int cherriesAdded = 0;
            Random rnd = new Random();

            // Count existing cherries
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (maze[i, j] == 3)
                        cherriesAdded++;
                }
            }

            // If we already have cherries, no need to add more
            if (cherriesAdded > 0)
                return;

            // Add 4 cherries at random positions
            while (cherriesAdded < 4)
            {
                int randRow = rnd.Next(rows);
                int randCol = rnd.Next(cols);

                // Only place cherries in empty spaces or dots (not walls or other objects)
                if (maze[randRow, randCol] == 0 || maze[randRow, randCol] == 2)
                {
                    maze[randRow, randCol] = 3; // Cherry power-up
                    cherriesAdded++;
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
            // Update power-up timer if active
            if (powerUpActive)
            {
                powerUpTimer--;
                if (powerUpTimer <= 0)
                {
                    powerUpActive = false;
                    // Eliberează fantomele din punctul de spawn
                    foreach (var ghost in ghosts.ToList())
                    {
                        ghost.isVulnerable = false;
                        ghost.isAtSpawn = false;
                    }
                }
            }

            // Update Pac-Man's movement.
            UpdatePacMan();

            // Update ghosts.
            foreach (var ghost in ghosts.ToList())
            {
                UpdateGhost(ghost);
            }

            // Check collision: if Pac-Man and any ghost are within a threshold distance.
            foreach (var ghost in ghosts.ToList())
            {
                if (!ghost.isAtSpawn) // Verifică coliziunea doar dacă fantoma nu este deja în punctul de spawn
                {
                    PointF diff = ghost.ScreenPos.Subtract(pacScreenPos);
                    if (diff.Length() < gridSize / 2)
                    {
                        if (powerUpActive && ghost.isVulnerable)
                        {
                            // Pac-Man mănâncă fantoma - o trimite la punctul de spawn
                            ghost.isAtSpawn = true;
                            ghost.isVulnerable = false; // Nu mai poate fi mâncată din nou
                            ghost.GridPos = ghost.spawnPoint;
                            ghost.ScreenPos = GetCellCenter(ghost.spawnPoint);
                            ghost.Target = null;
                            score += 200; // Puncte extra pentru mâncarea unei fantome
                        }
                        else if (!powerUpActive)
                        {
                            gameTimer.Stop();
                            ShowGameOverMessage();
                        }
                    }
                }
            }

            Invalidate(); // request redraw.
        }


        private void ShowGameOverMessage()
        {
            var result = MessageBox.Show($"Game Over! Your score: {score}\nDo you want to restart?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                RestartGame();
            }
            else
            {
                Application.Exit();
            }
        }

        private void RestartGame()
        {
            // Reset the game state
            score = 0;
            powerUpActive = false;
            powerUpTimer = 0;
            pacGridPos = new Point(1, 1);
            pacScreenPos = GetCellCenter(pacGridPos);
            pacTarget = null;
            currentDirection = new Point(0, 0);
            nextDirection = new Point(0, 0);

            // Reset ghosts
            foreach (var ghost in ghosts)
            {
                ghost.isVulnerable = false;
                ghost.isAtSpawn = false;
                ghost.GridPos = ghost.spawnPoint;
                ghost.ScreenPos = GetCellCenter(ghost.spawnPoint);
                ghost.Target = null;
            }

            // Reinitialize the maze
            InitializeMaze();
            BuildGraphAndComputePaths();

            // Restart the game timer
            gameTimer.Start();

            // Redraw the form
            Invalidate();
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
                if ((pacGridPos.X == 0 && currentDirection.X == -1) ||
                    (pacGridPos.X == cols - 1 && currentDirection.X == 1) ||
                    (pacGridPos.Y == 0 && currentDirection.Y == -1) ||
                    (pacGridPos.Y == rows - 1 && currentDirection.Y == 1))
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
                        // If there's a cherry power-up, eat it and activate power mode
                        else if (maze[pacGridPos.Y, pacGridPos.X] == 3)
                        {
                            maze[pacGridPos.Y, pacGridPos.X] = 0;
                            score += 50; // Increase score by 50 for cherry
                            ActivatePowerUp();
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

        // Activate the power-up mode
        private void ActivatePowerUp()
        {
            powerUpActive = true;
            powerUpTimer = powerUpDuration;

            // Make all ghosts vulnerable
            foreach (var ghost in ghosts)
            {
                if (!ghost.isAtSpawn) // Doar fantomele care nu sunt deja mâncate devin vulnerabile
                {
                    ghost.isVulnerable = true;
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

        // Găsește un punct de fugă pentru fantomă (un punct care e departe de Pac-Man)
        private Point GetFleeingTarget(Ghost ghost)
        {
            // Alegem un colț aleatoriu din labirint ca punct de fugă
            if (scatterPoints.Count > 0)
            {
                // Găsim cel mai îndepărtat punct de Pac-Man dintre colțuri
                Point bestTarget = scatterPoints[0];
                double maxDistance = 0;

                foreach (Point corner in scatterPoints)
                {
                    // Calculăm distanța Manhattan până la Pac-Man
                    double distance = Math.Abs(corner.X - pacGridPos.X) + Math.Abs(corner.Y - pacGridPos.Y);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        bestTarget = corner;
                    }
                }

                return bestTarget;
            }

            // Dacă nu avem puncte predefinite, creăm un vector de direcție opus
            int dx = ghost.GridPos.X - pacGridPos.X;
            int dy = ghost.GridPos.Y - pacGridPos.Y;

            // Dacă fantoma este foarte aproape de Pac-Man, fuge în direcția opusă
            if (Math.Abs(dx) + Math.Abs(dy) < 3)
            {
                int targetX = ghost.GridPos.X + (dx == 0 ? (random.Next(2) * 2 - 1) : (dx > 0 ? 2 : -2));
                int targetY = ghost.GridPos.Y + (dy == 0 ? (random.Next(2) * 2 - 1) : (dy > 0 ? 2 : -2));

                // Ne asigurăm că ținta este în limitele labirintului
                targetX = Math.Max(0, Math.Min(cols - 1, targetX));
                targetY = Math.Max(0, Math.Min(rows - 1, targetY));

                if (maze[targetY, targetX] != 1)
                {
                    return new Point(targetX, targetY);
                }
            }

            // Alegem un punct aleatoriu de pe margine
            int side = random.Next(4);
            Point target;

            switch (side)
            {
                case 0: // Sus
                    target = new Point(random.Next(cols), 0);
                    break;
                case 1: // Dreapta
                    target = new Point(cols - 1, random.Next(rows));
                    break;
                case 2: // Jos
                    target = new Point(random.Next(cols), rows - 1);
                    break;
                default: // Stânga
                    target = new Point(0, random.Next(rows));
                    break;
            }

            // Asigură-te că nu este un zid
            if (maze[target.Y, target.X] == 1)
            {
                // Caută o celulă liberă apropiată
                for (int offsetY = -2; offsetY <= 2; offsetY++)
                {
                    for (int offsetX = -2; offsetX <= 2; offsetX++)
                    {
                        int newX = target.X + offsetX;
                        int newY = target.Y + offsetY;
                        if (newX >= 0 && newX < cols && newY >= 0 && newY < rows && maze[newY, newX] != 1)
                        {
                            return new Point(newX, newY);
                        }
                    }
                }
            }

            return target;
        }

        // Update a ghost's position with smoothing and choose its next move based on the shortest path.
        private void UpdateGhost(Ghost ghost)
        {
            // Dacă fantoma este în punctul de spawn și power-up-ul este activ, rămâne acolo
            if (ghost.isAtSpawn)
            {
                // Fantoma rămâne la punctul de spawn până când se termină power-up-ul
                if (!powerUpActive)
                {
                    ghost.isAtSpawn = false;
                }
                return; // Nu actualizăm poziția fantomei dacă este la spawn
            }

            Point? nextStep;
            if (powerUpActive && ghost.isVulnerable)
            {
                // Fantoma fuge de Pac-Man când acesta are power-up
                Point fleeTarget = GetFleeingTarget(ghost);
                nextStep = GetNextStep(ghost.GridPos, fleeTarget);
            }
            else
            {
                // Comportament normal - fantoma urmărește Pac-Man
                nextStep = GetNextStep(ghost.GridPos, pacGridPos);
            }
            ghost.Target = nextStep.HasValue ? nextStep : ghost.GridPos;

            if (ghost.Target.HasValue)
            {
                if ((ghost.GridPos.X == 0 && ghost.Target.Value.X == cols - 1) ||
                    (ghost.GridPos.X == cols - 1 && ghost.Target.Value.X == 0) ||
                    (ghost.GridPos.Y == 0 && ghost.Target.Value.Y == rows - 1) ||
                    (ghost.GridPos.Y == rows - 1 && ghost.Target.Value.Y == 0))
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

                    // Recalculează ținta
                    if (powerUpActive && ghost.isVulnerable)
                    {
                        Point fleeTarget = GetFleeingTarget(ghost);
                        Point? nextStep1 = GetNextStep(ghost.GridPos, fleeTarget);
                        ghost.Target = nextStep1.HasValue ? nextStep1 : ghost.GridPos;
                    }
                    else
                    {
                        Point? nextStep1 = GetNextStep(ghost.GridPos, pacGridPos);
                        ghost.Target = nextStep1.HasValue ? nextStep1 : ghost.GridPos;
                    }
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
                    else if (maze[i, j] == 3)
                    {
                        // Draw cherry power-up
                        if (cherryImage != null)
                        {
                            Rectangle cherryRect = new Rectangle(cellRect.X + gridSize / 6, cellRect.Y + gridSize / 6,
                                                              gridSize * 2 / 3, gridSize * 2 / 3);
                            g.DrawImage(cherryImage, cherryRect);
                        }
                        else
                        {
                            // If no cherry image is available, draw a red circle
                            Rectangle cherryRect = new Rectangle(cellRect.X + gridSize / 4, cellRect.Y + gridSize / 4,
                                                               gridSize / 2, gridSize / 2);
                            g.FillEllipse(Brushes.Red, cherryRect);
                        }
                    }
                }
            }

            // Draw Pac-Man as an orange circle.
            // Draw Pac-Man as an orange circle.
            Rectangle pacRect = new Rectangle((int)(pacScreenPos.X - gridSize / 2), (int)(pacScreenPos.Y - gridSize / 2), gridSize, gridSize);
            g.TranslateTransform(pacRect.X + pacRect.Width / 2, pacRect.Y + pacRect.Height / 2);
            g.RotateTransform(pacManRotationAngle);
            g.TranslateTransform(-(pacRect.X + pacRect.Width / 2), -(pacRect.Y + pacRect.Height / 2));
            g.DrawImage(pacManImage, pacRect);
            g.ResetTransform();

            // Draw ghosts
            foreach (var ghost in ghosts)
            {
                Rectangle ghostRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 2), (int)(ghost.ScreenPos.Y - gridSize / 2), gridSize, gridSize);

                // Change ghost color based on state
                Brush ghostBrush;
                if (ghost.isAtSpawn)
                {
                    ghostBrush = Brushes.Gray; // Gray when at spawn point after being eaten
                }
                else if (ghost.isVulnerable)
                {
                    // Clipire albastru-alb când timpul de power-up este aproape de sfârșit
                    if (powerUpTimer < 50 && powerUpTimer / 5 % 2 == 0)
                    {
                        ghostBrush = Brushes.White; // Clipire albă
                    }
                    else
                    {
                        ghostBrush = Brushes.Blue; // Blue when vulnerable (can be eaten)
                    }
                }
                else
                {
                    ghostBrush = Brushes.Red; // Normal red color
                }

                g.FillEllipse(ghostBrush, ghostRect);

                // Desenăm ochii fantomei - două cercuri mici albe
                int eyeSize = gridSize / 5;
                Rectangle leftEyeRect, rightEyeRect;

                // Poziția ochilor depinde de direcția de mișcare a fantomei
                if (ghost.Target.HasValue)
                {
                    int dx = ghost.Target.Value.X - ghost.GridPos.X;
                    int dy = ghost.Target.Value.Y - ghost.GridPos.Y;

                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        // Mișcare orizontală predominantă
                        if (dx > 0) // Spre dreapta
                        {
                            leftEyeRect = new Rectangle((int)(ghost.ScreenPos.X), (int)(ghost.ScreenPos.Y - gridSize / 4), eyeSize, eyeSize);
                            rightEyeRect = new Rectangle((int)(ghost.ScreenPos.X), (int)(ghost.ScreenPos.Y + gridSize / 8), eyeSize, eyeSize);
                        }
                        else // Spre stânga
                        {
                            leftEyeRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 3), (int)(ghost.ScreenPos.Y - gridSize / 4), eyeSize, eyeSize);
                            rightEyeRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 3), (int)(ghost.ScreenPos.Y + gridSize / 8), eyeSize, eyeSize);
                        }
                    }
                    else
                    {
                        // Mișcare verticală predominantă
                        if (dy > 0) // În jos
                        {
                            leftEyeRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 5), (int)(ghost.ScreenPos.Y), eyeSize, eyeSize);
                            rightEyeRect = new Rectangle((int)(ghost.ScreenPos.X + gridSize / 10), (int)(ghost.ScreenPos.Y), eyeSize, eyeSize);
                        }
                        else // În sus
                        {
                            leftEyeRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 5), (int)(ghost.ScreenPos.Y - gridSize / 3), eyeSize, eyeSize);
                            rightEyeRect = new Rectangle((int)(ghost.ScreenPos.X + gridSize / 10), (int)(ghost.ScreenPos.Y - gridSize / 3), eyeSize, eyeSize);
                        }
                    }
                }
                else
                {
                    // Poziție default pentru ochi
                    leftEyeRect = new Rectangle((int)(ghost.ScreenPos.X - gridSize / 5), (int)(ghost.ScreenPos.Y - gridSize / 5), eyeSize, eyeSize);
                    rightEyeRect = new Rectangle((int)(ghost.ScreenPos.X + gridSize / 10), (int)(ghost.ScreenPos.Y - gridSize / 5), eyeSize, eyeSize);
                }

                g.FillEllipse(Brushes.White, leftEyeRect);
                g.FillEllipse(Brushes.White, rightEyeRect);
            }

            // Draw the score.
            g.DrawString($"Score: {score}", new Font("Arial", 16), Brushes.White, new PointF(10, 10));

            // Draw power-up status if active
            if (powerUpActive)
            {
                g.DrawString($"Power-Up: {powerUpTimer / 50 + 1}s", new Font("Arial", 16), Brushes.Yellow, new PointF(10, 40));
            }
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
