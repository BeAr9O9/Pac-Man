namespace Pac_Man
{
    partial class BeforeGame
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.startButton = new System.Windows.Forms.Button();
            this.editMazeButton = new System.Windows.Forms.Button();
            this.textBoxMaze = new System.Windows.Forms.TextBox();
            this.saveMazeButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.funnyCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.BackColor = System.Drawing.Color.Black;
            this.startButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.startButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.startButton.ForeColor = System.Drawing.Color.White;
            this.startButton.Location = new System.Drawing.Point(50, 20);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(135, 46);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start game";
            this.startButton.UseVisualStyleBackColor = false;
            this.startButton.Click += new System.EventHandler(this.startButton_Click_1);
            // 
            // editMazeButton
            // 
            this.editMazeButton.BackColor = System.Drawing.Color.Black;
            this.editMazeButton.Dock = System.Windows.Forms.DockStyle.Left;
            this.editMazeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.editMazeButton.ForeColor = System.Drawing.Color.White;
            this.editMazeButton.Location = new System.Drawing.Point(15, 15);
            this.editMazeButton.Name = "editMazeButton";
            this.editMazeButton.Size = new System.Drawing.Size(132, 70);
            this.editMazeButton.TabIndex = 1;
            this.editMazeButton.Text = "Edit maze";
            this.editMazeButton.UseVisualStyleBackColor = false;
            this.editMazeButton.Click += new System.EventHandler(this.editMazeButton_Click);
            // 
            // textBoxMaze
            // 
            this.textBoxMaze.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxMaze.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxMaze.Location = new System.Drawing.Point(0, 164);
            this.textBoxMaze.Multiline = true;
            this.textBoxMaze.Name = "textBoxMaze";
            this.textBoxMaze.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxMaze.Size = new System.Drawing.Size(228, 237);
            this.textBoxMaze.TabIndex = 2;
            this.textBoxMaze.Visible = false;
            // 
            // saveMazeButton
            // 
            this.saveMazeButton.Location = new System.Drawing.Point(153, 42);
            this.saveMazeButton.Name = "saveMazeButton";
            this.saveMazeButton.Size = new System.Drawing.Size(75, 23);
            this.saveMazeButton.TabIndex = 3;
            this.saveMazeButton.Text = "Save maze";
            this.saveMazeButton.UseVisualStyleBackColor = true;
            this.saveMazeButton.Visible = false;
            this.saveMazeButton.Click += new System.EventHandler(this.saveMazeButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.Black;
            this.closeButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.closeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.closeButton.ForeColor = System.Drawing.Color.White;
            this.closeButton.Location = new System.Drawing.Point(600, 15);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(135, 70);
            this.closeButton.TabIndex = 4;
            this.closeButton.Text = "Close app";
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.closeButton);
            this.panel1.Controls.Add(this.editMazeButton);
            this.panel1.Controls.Add(this.saveMazeButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 401);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(15);
            this.panel1.Size = new System.Drawing.Size(750, 100);
            this.panel1.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Transparent;
            this.panel2.Controls.Add(this.textBoxMaze);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(228, 401);
            this.panel2.TabIndex = 6;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Transparent;
            this.panel3.Controls.Add(this.funnyCheckBox);
            this.panel3.Controls.Add(this.startButton);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(550, 0);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(50, 20, 15, 0);
            this.panel3.Size = new System.Drawing.Size(200, 401);
            this.panel3.TabIndex = 7;
            // 
            // funnyCheckBox
            // 
            this.funnyCheckBox.AutoSize = true;
            this.funnyCheckBox.BackColor = System.Drawing.Color.Black;
            this.funnyCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.funnyCheckBox.ForeColor = System.Drawing.Color.White;
            this.funnyCheckBox.Location = new System.Drawing.Point(80, 72);
            this.funnyCheckBox.Name = "funnyCheckBox";
            this.funnyCheckBox.Size = new System.Drawing.Size(72, 24);
            this.funnyCheckBox.TabIndex = 1;
            this.funnyCheckBox.Text = "Funny";
            this.funnyCheckBox.UseVisualStyleBackColor = false;
            this.funnyCheckBox.CheckedChanged += new System.EventHandler(this.funnyCheckBox_CheckedChanged);
            // 
            // BeforeGame
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Pac_Man.Properties.Resources.pacman_bg1;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(750, 501);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(471, 378);
            this.Name = "BeforeGame";
            this.Text = "BeforeGame";
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button editMazeButton;
        private System.Windows.Forms.TextBox textBoxMaze;
        private System.Windows.Forms.Button saveMazeButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.CheckBox funnyCheckBox;
    }
}