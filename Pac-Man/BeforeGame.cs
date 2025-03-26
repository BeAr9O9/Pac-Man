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
                string mazeContent = File.ReadAllText("maze.txt");
                textBoxMaze.Text = mazeContent;
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
                var parts = textBoxMaze.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                int size = int.Parse(parts[0]);
                if (size != 25) 
                {
                    MessageBox.Show("Maze size must be 25x25");
                    return;
                }
                File.WriteAllText("maze.txt", textBoxMaze.Text);
                MessageBox.Show("Maze saved successfully!");
                textBoxMaze.Visible = false;
                saveMazeButton.Visible = false;
            }
            catch
            {
                MessageBox.Show("Error saving maze file");
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
