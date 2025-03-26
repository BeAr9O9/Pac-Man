using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace PacManWindowsForms
{
    public partial class PacManForm : Form
    {
        private float cellSize = 30;
        private int mazeRows = 25;
        private int mazeColumns = 25;
        private const int MAX_ROWS = 100;
        private const int MAX_COLUMNS = 100;
        private int[,] mazeGrid = new int[MAX_ROWS, MAX_COLUMNS];
        private bool isPacmanTeleporting = false;
        private Point pacmanGridPosition;
        private PointF pacmanScreenPosition;
        private Point? pacmanTargetCell = null;
        private const float BASE_CELL_SIZE = 30f;
        private float pacmanSpeed => 5f * (cellSize / BASE_CELL_SIZE);
        private Point currentMoveDirection = new Point(0, 0);
        private Point nextMoveDirection = new Point(0, 0);
        private SoundPlayer dotEatingSoundPlayer = new SoundPlayer(global::Pac_Man.Properties.Resources.eat);
        private List<Ghost> ghostsList = new List<Ghost>();
        private Timer gameTimer;
        private Dictionary<Point, int> cellToGraphIndex = new Dictionary<Point, int>();
        private List<Point> graphIndexToCell = new List<Point>();
        private int[,] pathNextCellMatrix;
        private int remainingDots = 0;
        private int playerScore = 0;
        private const int INFINITY = 1000000;
        private Button pauseGameButton;

        private Bitmap pacmanImage;
        private Bitmap ghostImage;
        private bool isFunny = false;

        public PacManForm(bool useFunnyAssets)
        {
            this.KeyPreview = true;
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            InitializeGame(useFunnyAssets);
            this.ClientSize = new Size((int)(mazeColumns * cellSize), (int)(mazeRows * cellSize));
            this.DoubleBuffered = true;
            this.Text = "Pac-Man";
            this.ActiveControl = null;
            isFunny = useFunnyAssets;
        }

        private void InitializeGame(bool useFunnyAssets)
        {
            SetupResponsiveWindow();
            LoadMazeFromFile();
            BuildGraphAndComputePaths();
            UpdateCellSize();
            pacmanScreenPosition = GetCellCenterPosition(pacmanGridPosition);

            foreach (var ghost in ghostsList)
            {
                ghost.ScreenPos = GetCellCenterPosition(ghost.GridPos);
            }

            gameTimer = new Timer();
            gameTimer.Interval = 20;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            SetupPauseButton();
            this.KeyDown += OnKeyDown;
            this.ActiveControl = null;
            this.Invalidate();

            LoadGameAssets(useFunnyAssets);
        }

        private void LoadGameAssets(bool useFunnyAssets)
        {
            if (useFunnyAssets)
            {
                pacmanImage = global::Pac_Man.Properties.Resources.pacman_right;
                ghostImage = global::Pac_Man.Properties.Resources.ghost;
                dotEatingSoundPlayer = new SoundPlayer(global::Pac_Man.Properties.Resources.eat);
            }
            else
            {
                pacmanImage = global::Pac_Man.Properties.Resources.pacman_nf_right;
                ghostImage = global::Pac_Man.Properties.Resources.ghost_nf;
                dotEatingSoundPlayer = null;
            }
        }

        private void SetupPauseButton()
        {
            pauseGameButton = new Button();
            pauseGameButton.Text = "Pause";
            pauseGameButton.Size = new Size(80, 40);
            pauseGameButton.ForeColor = Color.White;
            pauseGameButton.BackColor = Color.DarkBlue;
            pauseGameButton.Location = new Point(this.ClientSize.Width - 100, 10);
            pauseGameButton.TabStop = false;

            pauseGameButton.Click += (sender, eventArgs) =>
            {
                if (gameTimer.Enabled)
                {
                    gameTimer.Stop();
                    pauseGameButton.Text = "Resume";
                }
                else
                {
                    gameTimer.Start();
                    pauseGameButton.Text = "Pause";
                }
                this.Focus();
            };
            this.Controls.Add(pauseGameButton);
        }

        private void SetupResponsiveWindow()
        {
            Rectangle screenSize = Screen.PrimaryScreen.WorkingArea;
            this.MinimumSize = new Size(screenSize.Width / 2, screenSize.Height / 2);
            this.Resize += OnWindowResize;
            UpdateCellSize();
        }

        private void OnWindowResize(object sender, EventArgs e)
        {
            UpdateCellSize();

            if (pacmanGridPosition != null)
            {
                pacmanScreenPosition = GetCellCenterPosition(pacmanGridPosition);
            }

            foreach (var ghost in ghostsList)
            {
                ghost.ScreenPos = GetCellCenterPosition(ghost.GridPos);
            }

            Invalidate();
        }

        private void UpdateCellSize()
        {
            float horizontalCellSize = (float)this.ClientSize.Width / mazeColumns;
            float verticalCellSize = (float)this.ClientSize.Height / mazeRows;
            cellSize = Math.Min(horizontalCellSize, verticalCellSize);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                    nextMoveDirection = new Point(0, -1);
                    return true;
                case Keys.Down:
                    nextMoveDirection = new Point(0, 1);
                    return true;
                case Keys.Left:
                    nextMoveDirection = new Point(-1, 0);
                    return true;
                case Keys.Right:
                    nextMoveDirection = new Point(1, 0);
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void LoadMazeFromFile()
        {
            using (StreamReader sr = new StreamReader(@"maze.txt"))
            {
                string line;
                line = sr.ReadLine();
                mazeRows = int.Parse(line);
                line = sr.ReadLine();
                mazeColumns = int.Parse(line);

                for (int i = 0; i < mazeRows; i++)
                {
                    line = sr.ReadLine();
                    for (int j = 0; j < mazeColumns; j++)
                    {
                        if (line[j] == '0')
                            mazeGrid[i, j] = 0;
                        else if (line[j] == '#')
                            mazeGrid[i, j] = 1;
                        else if (line[j] == '.')
                        {
                            mazeGrid[i, j] = 2;
                            remainingDots++;
                        }
                        else if (line[j] == 'P')
                        {
                            mazeGrid[i, j] = 0;
                            pacmanGridPosition = new Point(j, i);
                            pacmanScreenPosition = GetCellCenterPosition(pacmanGridPosition);
                        }
                        else if (line[j] == 'G')
                        {
                            mazeGrid[i, j] = 0;
                            Ghost ghost = new Ghost(false, new Point(j, i), GetCellCenterPosition(new Point(j, i)), null, 4f);
                            ghostsList.Add(ghost);
                        }
                    }
                }
            }
        }

        private void BuildGraphAndComputePaths()
        {
            cellToGraphIndex.Clear();
            graphIndexToCell.Clear();
            for (int i = 0; i < mazeRows; i++)
            {
                for (int j = 0; j < mazeColumns; j++)
                {
                    if (mazeGrid[i, j] != 1)
                    {
                        Point cell = new Point(j, i);
                        cellToGraphIndex[cell] = graphIndexToCell.Count;
                        graphIndexToCell.Add(cell);
                    }
                }
            }
            int nodeCount = graphIndexToCell.Count;
            int[,] distanceMatrix = new int[nodeCount, nodeCount];
            pathNextCellMatrix = new int[nodeCount, nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i == j)
                    {
                        distanceMatrix[i, j] = 0;
                        pathNextCellMatrix[i, j] = j;
                    }
                    else
                    {
                        distanceMatrix[i, j] = INFINITY;
                        pathNextCellMatrix[i, j] = -1;
                    }
                }
            }

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };
            foreach (var kv in cellToGraphIndex)
            {
                Point cell = kv.Key;
                int sourceIndex = kv.Value;
                for (int dir = 0; dir < 4; dir++)
                {
                    int newX, newY;
                    if (cell.X == 0 && dx[dir] < 0)
                        newX = mazeColumns - 1;
                    else if (cell.X == mazeColumns - 1 && dx[dir] > 0)
                        newX = 0;
                    else
                        newX = cell.X + dx[dir];

                    if (cell.Y == 0 && dy[dir] < 0)
                        newY = mazeRows - 1;
                    else if (cell.Y == mazeRows - 1 && dy[dir] > 0)
                        newY = 0;
                    else
                        newY = cell.Y + dy[dir];

                    Point neighbor = new Point(newX, newY);
                    if (cellToGraphIndex.ContainsKey(neighbor))
                    {
                        int targetIndex = cellToGraphIndex[neighbor];
                        distanceMatrix[sourceIndex, targetIndex] = 1;
                        pathNextCellMatrix[sourceIndex, targetIndex] = targetIndex;
                    }
                }
            }

            // Floyd-Warshall algorithm
            for (int k = 0; k < nodeCount; k++)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    for (int j = 0; j < nodeCount; j++)
                    {
                        if (distanceMatrix[i, k] + distanceMatrix[k, j] < distanceMatrix[i, j])
                        {
                            distanceMatrix[i, j] = distanceMatrix[i, k] + distanceMatrix[k, j];
                            pathNextCellMatrix[i, j] = pathNextCellMatrix[i, k];
                        }
                    }
                }
            }
        }

        private Point? GetNextCellInPath(Point from, Point to)
        {
            if (!cellToGraphIndex.ContainsKey(from) || !cellToGraphIndex.ContainsKey(to))
                return null;
            int sourceIndex = cellToGraphIndex[from];
            int targetIndex = cellToGraphIndex[to];
            if (pathNextCellMatrix[sourceIndex, targetIndex] == -1)
                return null;
            int nextCellIndex = pathNextCellMatrix[sourceIndex, targetIndex];
            return graphIndexToCell[nextCellIndex];
        }

        private PointF GetCellCenterPosition(Point cell)
        {
            float offsetX = (ClientSize.Width - mazeColumns * cellSize) / 2;
            float offsetY = (ClientSize.Height - mazeRows * cellSize) / 2;

            return new PointF(
                offsetX + cell.X * cellSize + cellSize / 2,
                offsetY + cell.Y * cellSize + cellSize / 2
            );
        }

        private int CalculateNextXPosition(int currentX, int direction)
        {
            if (direction == 0)
                return currentX;
            if (direction == 1 && currentX == mazeColumns - 1)
                return 0;
            if (direction == -1 && currentX == 0)
                return mazeColumns - 1;
            return currentX + direction;
        }

        private int CalculateNextYPosition(int currentY, int direction)
        {
            if (direction == 0)
                return currentY;
            if (direction == 1 && currentY == mazeRows - 1)
                return 0;
            if (direction == -1 && currentY == 0)
                return mazeRows - 1;
            return currentY + direction;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
                nextMoveDirection = new Point(0, -1);
            else if (e.KeyCode == Keys.Down)
                nextMoveDirection = new Point(0, 1);
            else if (e.KeyCode == Keys.Left)
                nextMoveDirection = new Point(-1, 0);
            else if (e.KeyCode == Keys.Right)
                nextMoveDirection = new Point(1, 0);
        }

        private void UpdatePacmanPosition()
        {
            if (!pacmanTargetCell.HasValue)
            {
                Point desiredCell = new Point(
                    CalculateNextXPosition(pacmanGridPosition.X, nextMoveDirection.X),
                    CalculateNextYPosition(pacmanGridPosition.Y, nextMoveDirection.Y)
                );

                if (mazeGrid[desiredCell.Y, desiredCell.X] != 1)
                {
                    currentMoveDirection = nextMoveDirection;
                }

                Point newCell = new Point(
                    CalculateNextXPosition(pacmanGridPosition.X, currentMoveDirection.X),
                    CalculateNextYPosition(pacmanGridPosition.Y, currentMoveDirection.Y)
                );

                if ((pacmanGridPosition.X == 0 && currentMoveDirection.X == -1) ||
                    (pacmanGridPosition.X == mazeColumns - 1 && currentMoveDirection.X == 1) ||
                    (pacmanGridPosition.Y == 0 && currentMoveDirection.Y == -1) ||
                    (pacmanGridPosition.Y == mazeRows - 1 && currentMoveDirection.Y == 1))
                {
                    if (mazeGrid[newCell.Y, newCell.X] != 1)
                        isPacmanTeleporting = true;
                }
                else
                {
                    isPacmanTeleporting = false;
                }

                if (newCell.X >= 0 && newCell.X < mazeColumns && newCell.Y >= 0 && newCell.Y < mazeRows && mazeGrid[newCell.Y, newCell.X] != 1)
                {
                    pacmanTargetCell = newCell;
                }
            }

            if (pacmanTargetCell.HasValue)
            {
                PointF targetCenter = GetCellCenterPosition(pacmanTargetCell.Value);
                if (!isPacmanTeleporting)
                {
                    PointF diff = new PointF(targetCenter.X - pacmanScreenPosition.X, targetCenter.Y - pacmanScreenPosition.Y);
                    float distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                    float currentPacmanSpeed = pacmanSpeed;

                    if (distance < currentPacmanSpeed)
                    {
                        pacmanScreenPosition = targetCenter;
                        pacmanGridPosition = pacmanTargetCell.Value;
                        pacmanTargetCell = null;
                        if (mazeGrid[pacmanGridPosition.Y, pacmanGridPosition.X] == 2)
                        {
                            mazeGrid[pacmanGridPosition.Y, pacmanGridPosition.X] = 0;
                            if (dotEatingSoundPlayer != null)
                            {
                                dotEatingSoundPlayer.Play();
                            }
                            playerScore += 10;
                            remainingDots--;

                            if (remainingDots == 0)
                            {
                                gameTimer.Stop();
                                MessageBox.Show($"You Win! Your score: {playerScore}");
                                Application.Exit();
                            }
                        }
                    }
                    else
                    {
                        float vx = diff.X / distance;
                        float vy = diff.Y / distance;
                        pacmanScreenPosition = new PointF(
                            pacmanScreenPosition.X + vx * currentPacmanSpeed,
                            pacmanScreenPosition.Y + vy * currentPacmanSpeed
                        );
                    }
                }
                else
                {
                    pacmanScreenPosition = targetCenter;
                    pacmanGridPosition = pacmanTargetCell.Value;
                    pacmanTargetCell = null;
                }
            }
        }

        private void RestartGame()
        {
            playerScore = 0;
            mazeGrid = new int[MAX_ROWS, MAX_COLUMNS];
            ghostsList.Clear();
            cellToGraphIndex.Clear();
            graphIndexToCell.Clear();
            currentMoveDirection = new Point(0, 0);
            nextMoveDirection = new Point(0, 0);
            pacmanTargetCell = null;
            isPacmanTeleporting = false;
            remainingDots = 0;

            LoadMazeFromFile();
            BuildGraphAndComputePaths();
            pacmanScreenPosition = GetCellCenterPosition(pacmanGridPosition);
            gameTimer.Start();
            this.ActiveControl = null;
        }

        private void GameLoop(object sender, EventArgs e)
        {
            UpdatePacmanPosition();

            foreach (var ghost in ghostsList)
            {
                ghost.Update(this);
            }

            bool isGameOver = false;
            foreach (var ghost in ghostsList)
            {
                PointF diff = ghost.ScreenPos.Subtract(pacmanScreenPosition);
                if (diff.Length() < cellSize / 2)
                {
                    isGameOver = true;
                    break;
                }
            }

            if (isGameOver)
            {
                if (dotEatingSoundPlayer != null)
                {
                    using (SoundPlayer deathSound = new SoundPlayer(global::Pac_Man.Properties.Resources.death))
                    {
                        deathSound.Play();
                    }
                }

                gameTimer.Stop();
                DialogResult result = MessageBox.Show($"Game Over! Your score: {playerScore}\nDo you want to restart?", "Game Over", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                    RestartGame();
                else
                    Application.Exit();
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            float offsetX = (ClientSize.Width - mazeColumns * cellSize) / 2;
            float offsetY = (ClientSize.Height - mazeRows * cellSize) / 2;

            for (int i = 0; i < mazeRows; i++)
            {
                for (int j = 0; j < mazeColumns; j++)
                {
                    RectangleF cellRect = new RectangleF(
                        offsetX + j * cellSize,
                        offsetY + i * cellSize,
                        cellSize,
                        cellSize
                    );

                    if (mazeGrid[i, j] == 1)
                    {
                        g.FillRectangle(Brushes.Blue, cellRect);
                    }
                    else if (mazeGrid[i, j] == 2)
                    {
                        float dotSize = cellSize / 3;
                        RectangleF dotRect = new RectangleF(
                            cellRect.X + cellSize / 3,
                            cellRect.Y + cellSize / 3,
                            dotSize,
                            dotSize
                        );
                        g.FillEllipse(Brushes.Yellow, dotRect);
                    }
                }
            }

            RectangleF pacmanRect = new RectangleF(
                pacmanScreenPosition.X - cellSize / 2,
                pacmanScreenPosition.Y - cellSize / 2,
                cellSize,
                cellSize
            );

            if (isFunny)
            {
                if (currentMoveDirection.X == -1)
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_left;
                else if (currentMoveDirection.Y == 1)
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_down;
                else if (currentMoveDirection.Y == -1)
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_up;
                else
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_right;
            }
            else
            {

                if (currentMoveDirection.X == -1)
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_nf_left;
                else if (currentMoveDirection.Y == 1)
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_nf_down;
                else if (currentMoveDirection.Y == -1)
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_nf_up;
                else
                    pacmanImage = global::Pac_Man.Properties.Resources.pacman_nf_right;
            }

            g.DrawImage(pacmanImage, pacmanRect);

            foreach (var ghost in ghostsList)
            {
                RectangleF ghostRect = new RectangleF(
                    ghost.ScreenPos.X - cellSize / 2,
                    ghost.ScreenPos.Y - cellSize / 2,
                    cellSize,
                    cellSize
                );

                g.DrawImage(ghostImage, ghostRect);
            }

            float fontSize = Math.Max(12, cellSize / 2);
            g.DrawString(
                $"Score: {playerScore}",
                new Font("Arial", fontSize),
                Brushes.White,
                new PointF(10, 10)
            );
            pauseGameButton.Location = new Point(this.ClientSize.Width - 100, 10);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.Black;
            this.ClientSize = new Size(800, 387);
            this.Name = "PacManForm";
            this.ResumeLayout(false);
        }

        private class Ghost
        {
            private bool isTeleporting = false;
            public Point GridPos;
            public PointF ScreenPos;
            private Point? TargetCell;
            private const float BASE_SPEED = 4f;
            private const float BASE_CELL_SIZE = 30f;

            public float CalculateSpeed(float currentCellSize) => BASE_SPEED * (currentCellSize / BASE_CELL_SIZE);

            public Ghost(bool isTeleporting, Point gridPos, PointF screenPos, Point? targetCell, float ghostSpeed)
            {
                this.isTeleporting = isTeleporting;
                GridPos = gridPos;
                ScreenPos = screenPos;
                TargetCell = targetCell;
            }

            public void Update(PacManForm form)
            {
                Point? nextStep = form.GetNextCellInPath(this.GridPos, form.pacmanGridPosition);
                this.TargetCell = nextStep.HasValue ? nextStep : this.GridPos;

                if (this.TargetCell.HasValue)
                {
                    if ((this.GridPos.X == 0 && this.TargetCell.Value.X == form.mazeColumns - 1) ||
                        (this.GridPos.X == form.mazeColumns - 1 && this.TargetCell.Value.X == 0) ||
                        (this.GridPos.Y == 0 && this.TargetCell.Value.Y == form.mazeRows - 1) ||
                        (this.GridPos.Y == form.mazeRows - 1 && this.TargetCell.Value.Y == 0))
                    {
                        if (form.mazeGrid[this.TargetCell.Value.Y, this.TargetCell.Value.X] != 1)
                            this.isTeleporting = true;
                    }
                    else
                    {
                        this.isTeleporting = false;
                    }

                    if (!this.isTeleporting)
                    {
                        PointF targetCenter = form.GetCellCenterPosition(this.TargetCell.Value);
                        PointF diff = new PointF(targetCenter.X - this.ScreenPos.X, targetCenter.Y - this.ScreenPos.Y);
                        float distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                        float currentSpeed = CalculateSpeed(form.cellSize);

                        if (distance < currentSpeed)
                        {
                            this.ScreenPos = targetCenter;
                            this.GridPos = this.TargetCell.Value;
                            this.TargetCell = null;
                        }
                        else
                        {
                            float vx = diff.X / distance;
                            float vy = diff.Y / distance;
                            this.ScreenPos = new PointF(
                                this.ScreenPos.X + vx * currentSpeed,
                                this.ScreenPos.Y + vy * currentSpeed
                            );
                        }
                    }
                    else
                    {
                        this.ScreenPos = form.GetCellCenterPosition(this.TargetCell.Value);
                        this.GridPos = this.TargetCell.Value;
                        Point? nextStep1 = form.GetNextCellInPath(this.GridPos, form.pacmanGridPosition);
                        this.TargetCell = nextStep1.HasValue ? nextStep1 : this.GridPos;
                    }
                }
            }
        }
    }

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