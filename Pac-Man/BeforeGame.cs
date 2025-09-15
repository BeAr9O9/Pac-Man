using PacManWindowsForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pac_Man
{
    public partial class BeforeGame : Form
    {
        public BeforeGame()
        {
            InitializeComponent();
        }

        private void startButton_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            PacManForm pacManForm = new PacManForm(funnyCheckBox.Checked);
            pacManForm.ShowDialog();
            this.Close();
        }


        private void editMazeButton_Click(object sender, EventArgs e)
        {
            try
            {
                string appName = "Pac-Man";
                string appDataPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), appName);
                string filePath = Path.Combine(appDataPath, "maze.txt");

                if (File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);
                    textBoxMaze.Text = content;
                }
                else
                {
                    // Provide default maze with power pellets as example
                    textBoxMaze.Text = @"25
25
#########################
#*......#.........*#
#.##.#######.#######.##.#
#.##.......#.#.......##.#
#.##.#####.#.#.#####.##.#
#....#...#...#...#....#
####.#.#.#####.#.#.####
....#.#.#.....#.#.#....
##.#.#.#########.#.#.##
#..#...#.......#...#..#
#.###.###.###.###.###.#
#.....P.#.....#.G.....#
#.###.###.###.###.###.#
#..#...#.......#...#..#
##.#.#.#########.#.#.##
....#.#.#.....#.#.#....
####.#.#.#####.#.#.####
#....#...#...#...#....#
#.##.#####.#.#.#####.##.#
#.##.......#.#.......##.#
#.##.#######.#.#######.##.#
#*......#.........*#
#########################

Maze format:
# = Wall
. = Dot (10 points)
* = Power pellet (50 points, makes ghosts vulnerable)
P = Pac-Man start position
G = Ghost start position
0 = Empty space";
                }
                textBoxMaze.Visible = true;
                saveMazeButton.Visible = true;
            }
            catch
            {
                textBoxMaze.Visible = true;
                saveMazeButton.Visible = true;
            }
        }

        private void saveMazeButton_Click(object sender, EventArgs e)
        {
            try
            {
                var lines = textBoxMaze.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines.Length < 2)
                {
                    MessageBox.Show("Invalid maze format. Must have at least height and width specified.");
                    return;
                }
                
                int rows = int.Parse(lines[0]);
                int cols = int.Parse(lines[1]);
                if (rows != 25 || cols != 25)
                {
                    MessageBox.Show("Maze size must be 25x25");
                    return;
                }
                
                string appName = "Pac-Man";
                string appDataPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), appName);

                // Create the directory if it doesn't exist
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                string path = Path.Combine(appDataPath, "maze.txt");
                File.WriteAllText(path, textBoxMaze.Text);
                MessageBox.Show("Maze saved successfully!\n\nPower pellets (*) will make ghosts vulnerable for 10 seconds.");
                textBoxMaze.Visible = false;
                saveMazeButton.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving maze file: {ex.Message}");
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void funnyCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
