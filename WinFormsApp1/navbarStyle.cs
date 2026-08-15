using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp1
{
    internal class navbarStyle
    {
        public static Panel CreateIndicator(Guna2Button TargetButton)
        {
            Panel panel = new Panel();
            panel.Width = 305;
            panel.Height = 2;
            panel.BackColor = Color.FromArgb(123, 15, 23);
            panel.Location = new Point(12, 77);

            TargetButton.Controls.Add(panel);

            return panel;
        }
        public static void RemoveIndicator(Panel panel)
        {
            if (panel != null && panel.Parent != null)
            {
                panel.Parent.Controls.Remove(panel);
            }
        }
    }
}
