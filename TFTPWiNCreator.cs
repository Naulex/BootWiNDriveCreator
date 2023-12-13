using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing;


namespace BootWiNDriveCreator
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            WinDriveTextBox.Text += "BootWiNDriveCreator - программа, позволяющая создавать универсальные загрузочные носители для Windows и LiveCD.\r\n\r\nBootWiNDriveCreator v.2.0\r\n(c)Naulex, 2023\r\n073797@gmail.com\r\n\r\n";
            WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Запуск выполнен успешно.\r\n";
            SoftExtractorDialog.SelectedPath = Environment.CurrentDirectory;
        }
        void CopyDir(string FromDir, string ToDir)
        {
            Directory.CreateDirectory(ToDir);
            foreach (string s1 in Directory.GetFiles(FromDir))
            {
                string s2 = ToDir + "\\" + Path.GetFileName(s1);
                File.Copy(s1, s2);
            }
            foreach (string s in Directory.GetDirectories(FromDir))
            {
                CopyDir(s, ToDir + "\\" + Path.GetFileName(s));
            }
        }

        private void ChooseBootISODrive_Click(object sender, EventArgs e)
        {
            if (OpenISODialogDrive.ShowDialog() == DialogResult.Cancel)
                return;
            WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Выбран ISO образ: " + OpenISODialogDrive.FileName + "\r\n";

        }
        private void ChooseFolderISODrive_Click(object sender, EventArgs e)
        {
            if (OpenFolderDrive.ShowDialog() == DialogResult.Cancel)
                return;
            WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Выбран ISO образ: " + OpenFolderDrive.SelectedPath + "\r\n";

        }
        private void ChooseFolderToExtract_Click(object sender, EventArgs e)
        {
            if (ChooseFolderDialog.ShowDialog() == DialogResult.Cancel)
                return;

            WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Выбрана папка: " + ChooseFolderDialog.SelectedPath + "\r\n";

        }

        private async void BeginDoDevice_Click(object sender, EventArgs e)
        {

            if (ChooseFolderDialog.SelectedPath.Length == 0)
            {
                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Ошибка! Не указана целевая папка!\r\n";
                return;
            }

            var FileSystem = new DriveInfo(ChooseFolderDialog.SelectedPath).DriveFormat;
            if (FileSystem != "FAT32")
            {
                DialogResult dialogResult = MessageBox.Show("Настоятельно рекомендуется, чтобы устройство было отформатировано в файловую систему FAT32. Настаиваете на продолжении?", "Неверное форматирование | BootWiNDriveCreator", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Начало работы...\r\n";
            if (DriveUnversalBoot.Checked == true)
            {
                if (Directory.Exists("ResourcesBootWinDrive"))
                {
                    MessageBox.Show("Обнаружена существующая папка \"ResourcesBootWinDrive\". В целях исключения ошибок в работе программы, эта папка будет удалена сразу после закрытия этого окна. Если там имеются важные файлы, скопируйте их, и только потом закрывайте это окно.", "Обнаружена существующая папка | BootWiNDriveCreator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Directory.Delete("ResourcesBootWinDrive", true);
                }
                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Создание папки ResourcesBootWinDrive...\r\n";
                Directory.CreateDirectory("ResourcesBootWinDrive");

                if (ISOChoosedDrive.Checked == true)
                {
                    WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла 7z.dll...\r\n";
                    File.WriteAllBytes(@"ResourcesBootWinDrive\7z.dll", Properties.Resources._7z1);
                    WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла 7z.exe...\r\n";
                    File.WriteAllBytes(@"ResourcesBootWinDrive\7z.exe", Properties.Resources._7z);

                    WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Вызов 7z.exe, извлечение образа " + OpenISODialogDrive.FileName + "...\r\n";
                    Process extractDevice = new Process();
                    extractDevice.StartInfo.FileName = @"ResourcesBootWinDrive\7z.exe";
                    extractDevice.StartInfo.Arguments = "x -o\"" + Environment.CurrentDirectory + "\\ResourcesBootWinDrive\\WinISO\" " + OpenISODialogDrive.FileName;
                    extractDevice.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    extractDevice.Start();
                    extractDevice.WaitForExit();
                    extractDevice.Dispose();
                }
                if (FolderChoosedDrive.Checked == true)
                {
                    Directory.CreateDirectory(@"ResourcesBootWinDrive\WinISO");
                    WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Копирование файлов из образа в папку назначения...\r\n";
                    await Task.Run(() =>
                    {
                        CopyFolder(OpenFolderDrive.SelectedPath, Environment.CurrentDirectory + @"\ResourcesBootWinDrive\WinISO");
                    });
                }

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Деление WIM файла начато!...\r\n";
                Process splitWIM = new Process();
                splitWIM.StartInfo.FileName = @"dism.exe";
                splitWIM.StartInfo.Arguments = "/split-image /imagefile:\"" + Environment.CurrentDirectory + @"\ResourcesBootWinDrive\WinISO\sources\install.wim" + "\" /swmfile:\"" + Environment.CurrentDirectory + "\\ResourcesBootWinDrive\\WinISO\\sources\\install.swm\"" + " /filesize:" + 3900;
                splitWIM.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                splitWIM.Start();
                splitWIM.WaitForExit();
                splitWIM.Dispose();

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Деление WIM файла завершено!...\r\n";

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Удаление образа...\r\n";
                File.Delete(Environment.CurrentDirectory + @"\ResourcesBootWinDrive\WinISO\sources\install.wim");

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла grldr...\r\n";
                File.WriteAllBytes(@"ResourcesBootWinDrive\WinISO\grldr", Properties.Resources.grldr);

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла 1.xpm.gz...\r\n";
                File.WriteAllBytes(@"ResourcesBootWinDrive\WinISO\1.xpm.gz", Properties.Resources._1_xpm);

                if (BootWinEI.Checked == true)
                {
                    WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла ei.cfg...\r\n";
                    File.WriteAllBytes(@"ResourcesBootWinDrive\WinISO\sources\ei.cfg", Properties.Resources.ei);
                }

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Генерация файла menu.lst...\r\n";
                using (StreamWriter sw = new StreamWriter(@"ResourcesBootWinDrive\WinISO\menu.lst", false))
                {
                    sw.WriteLine("timeout 30");
                    sw.WriteLine("splashimage (hd0,0)/1.xpm.gz");
                    sw.WriteLine("");
                    sw.WriteLine("title " + Path.GetFileName(OpenISODialogDrive.FileName));
                    sw.WriteLine("find --set-root --ignore-floppies --ignore-cd /bootmgr");
                    sw.WriteLine("map () (hd0)");
                    sw.WriteLine("map (hd0) ()");
                    sw.WriteLine("map --rehook");
                    sw.WriteLine("find --set-root --ignore-floppies --ignore-cd /bootmgr");
                    sw.WriteLine("chainloader /bootmgr");
                    sw.WriteLine("savedefault --wait=2");
                    sw.WriteLine("");
                    sw.WriteLine("title Auto-generated by BootWiNDriveCreator");
                    sw.Write("reboot");
                }

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Копирование на целевой носитель...\r\n";

                await Task.Run(() =>
                {
                    CopyFolder(@"ResourcesBootWinDrive\WinISO\", ChooseFolderDialog.SelectedPath);
                });
                WinDriveTextBox.Text += "========================================\r\n";
                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Копирование завершено...\r\n";
                WinDriveTextBox.Text += "========================================\r\n";
            }
            if (DriveGRLDRBoot.Checked == true)
            {
                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Копирование образа...\r\n";
                File.Copy(OpenISODialogDrive.FileName, ChooseFolderDialog.SelectedPath + "\\" + Path.GetFileName(OpenISODialogDrive.FileName));

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла grldr...\r\n";
                File.WriteAllBytes(ChooseFolderDialog.SelectedPath + "\\grldr", Properties.Resources.grldr);

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Извлечение файла 1.xpm.gz...\r\n";
                File.WriteAllBytes(ChooseFolderDialog.SelectedPath + "\\1.xpm.gz", Properties.Resources._1_xpm);

                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Генерация файла menu.lst...\r\n";
                using (StreamWriter sw = new StreamWriter(ChooseFolderDialog.SelectedPath + "\\menu.lst", false))
                {
                    sw.WriteLine("timeout 30");
                    sw.WriteLine("splashimage (hd0,0)/1.xpm.gz");
                    sw.WriteLine("");
                    sw.WriteLine("title " + Path.GetFileName(OpenISODialogDrive.FileName));

                    sw.WriteLine("map /" + Path.GetFileName(OpenISODialogDrive.FileName) + " || map --mem /" + Path.GetFileName(OpenISODialogDrive.FileName) + " (0xff)");

                    sw.WriteLine("map --hook");
                    sw.WriteLine("root (0xff)");
                    sw.WriteLine("chainloader (0xff)");
                    sw.WriteLine("");
                    sw.WriteLine("title Auto-generated by BootWiNDriveCreator");
                    sw.Write("reboot");
                }

                WinDriveTextBox.Text += "========================================\r\n";
                WinDriveTextBox.Text += Convert.ToString(DateTime.Now.ToShortTimeString()) + ": Копирование завершено...\r\n";
                WinDriveTextBox.Text += "========================================\r\n";

            }

        }
        private void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)));

            }
            string[] folders = Directory.GetDirectories(sourceFolder);

            foreach (string folder in folders)
            {
                CopyFolder(folder, Path.Combine(destFolder, Path.GetFileName(folder)));
            }
        }

        private void DriveUnversalBoot_CheckedChanged(object sender, EventArgs e)
        {
            if (DriveUnversalBoot.Checked == true)
            {
                BootWinEI.Enabled = true;
            }
            else
            {
                BootWinEI.Enabled = false;
            }
        }

        private void WinDriveDelFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists("ResourcesBootWinDrive"))
            {
                DialogResult DeleteOrNot = MessageBox.Show("Вы действительно хотите удалить эту папку?", "Удаление папки ResourcesBootWinDrive | BootWiNDriveCreator", MessageBoxButtons.OK, MessageBoxIcon.Question);

                if (DeleteOrNot == DialogResult.Yes)
                {
                    Directory.Delete("ResourcesBootWinDrive", true);
                    MessageBox.Show("Папка удалена.", "Успех | BootWiNDriveCreator", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }
            else
            { MessageBox.Show("Папка не обнаружена.", "Папка не обнаружена | BootWiNDriveCreator", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void ISOChoosedDrive_CheckedChanged(object sender, EventArgs e)
        {
            if (ISOChoosedDrive.Checked)
            {
                ChooseBootISODrive.Enabled = true;
            }
            else
            {
                ChooseBootISODrive.Enabled = false;
            }

        }

        private void FolderChoosedDrive_CheckedChanged(object sender, EventArgs e)
        {
            if (FolderChoosedDrive.Checked)
            {
                ChooseFolderISODrive.Enabled = true;
            }
            else
            {
                ChooseFolderISODrive.Enabled = false;
            }

        }

    }
}
