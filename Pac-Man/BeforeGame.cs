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
            catch (Exception ex)
            {
                MessageBox.Show("Error reading maze file: " + ex.Message);
            }
        }

        private void saveMazeButton_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText("maze.txt", textBoxMaze.Text);
                MessageBox.Show("Maze saved successfully!");
                textBoxMaze.Visible = false;
                saveMazeButton.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving maze file: " + ex.Message);
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
