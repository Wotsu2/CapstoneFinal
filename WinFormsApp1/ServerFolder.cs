using System;
using System.Collections.Generic;
using System.Text;


namespace WinFormsApp1
{
    internal class ServerFolder
    {
        private AdminForm parentForm; 
        private string currentFolder;
        private Stack<string> folderHistory = new Stack<string>();

        public ServerFolder(AdminForm form)
        {
            parentForm = form;
        }

        public void lsServerFolderSetup()
        {
            parentForm.lvServerFolder.View = View.LargeIcon;
            parentForm.lvServerFolder.LargeImageList = parentForm.imageListIcon;
            parentForm.lvServerFolder.MultiSelect = false;
        }

        public void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (addToHistory && !string.IsNullOrEmpty(currentFolder))
            {
                folderHistory.Push(currentFolder);
            }

            currentFolder = path;
            parentForm.lvServerFolder.Items.Clear();
            parentForm.imageListIcon.Images.Clear();
            int imageIndex = 0;

            //To Show Folder
            foreach (string dir in Directory.GetDirectories(path))
            {
                parentForm.imageListIcon.Images.Add(Properties.Resources.Folder);
                ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                parentForm.lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            //to Show File

            foreach (string file in Directory.GetFiles(path))
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file);
                parentForm.imageListIcon.Images.Add(Properties.Resources.Item);

                ListViewItem item = new ListViewItem(Path.GetFileName(file), imageIndex);
                item.Tag = file;
                parentForm.lvServerFolder.Items.Add(item);
                imageIndex++;

            }

            parentForm.BtnBack.Enabled = folderHistory.Count > 0;
        }
        public void btnBack()
        {
            if (folderHistory.Count > 0)
            {
                string previousFolder = folderHistory.Pop();
                LoadServerFolder(previousFolder, addToHistory: false);
            }
        }
        public void doubleClick()
        {
            if (parentForm.lvServerFolder.SelectedItems.Count == 0) return;

            string path = parentForm.lvServerFolder.SelectedItems[0].Tag.ToString();

            if (Directory.Exists(path))
                LoadServerFolder(path);
            else if (File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
    }
}
