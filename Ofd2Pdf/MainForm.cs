using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Spire.Pdf.Conversion;

namespace Ofd2Pdf
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        List<OFDFile> fileList = new List<OFDFile>();

        Converter converter = new Converter();

        // 转换或扫描进行中时为 true，此时不允许修改文件列表
        volatile bool isBusy = false;

        private bool CheckBusy()
        {
            if (isBusy)
            {
                MessageBox.Show("正在处理中，请稍候。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return isBusy;
        }

        /// <summary>
        /// 添加文件或文件夹（文件夹会在后台递归扫描 .ofd 文件），已在列表中的文件不会重复添加。
        /// </summary>
        private async void AddPaths(string[] paths)
        {
            if (CheckBusy())
            {
                return;
            }

            isBusy = true;
            var oldCursor = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                List<string> files = await Task.Run(() => OfdScanner.Expand(paths));

                var existing = new HashSet<string>(fileList.Select(f => f.FileName), StringComparer.OrdinalIgnoreCase);
                int added = 0;
                foreach (var file in files)
                {
                    if (existing.Add(file))
                    {
                        fileList.Add(new OFDFile(file));
                        added++;
                    }
                }
                LoadFilesToListView();

                if (files.Count == 0)
                {
                    MessageBox.Show("未找到 OFD 文件。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (added == 0)
                {
                    MessageBox.Show("找到的 OFD 文件均已在列表中。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("扫描文件失败：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = oldCursor;
                isBusy = false;
            }
        }

        private void buttonFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "选择文件夹，将递归扫描其中所有 OFD 文件（PDF 保存在原文件旁）";
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    AddPaths(new[] { fbd.SelectedPath });
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "OFD 文档(*.ofd)|*.ofd";
            ofd.Multiselect = true;
            ofd.CheckFileExists = true;
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                AddPaths(ofd.FileNames);
            }
        }

        private Color ConvertColor(Status status)
        {
            switch (status)
            {
                case Status.等待转换:
                    return Color.Black;
                case Status.正在转换:
                    return Color.CadetBlue;
                case Status.转换完成:
                    return Color.LimeGreen;
                case Status.转换失败:
                    return Color.IndianRed;
                default:
                    return Color.Black;
            }
        }

        private void LoadFilesToListView()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();
            foreach (OFDFile oFDFile in fileList)
            {
                var item = new ListViewItem(oFDFile.FileName);
                item.UseItemStyleForSubItems = false;
                var subItem = new ListViewItem.ListViewSubItem();
                subItem.Text = oFDFile.Status.ToString();
                subItem.ForeColor = ConvertColor(oFDFile.Status);
                item.SubItems.Add(subItem);
                listView1.Items.Add(item);
            }
            listView1.EndUpdate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (CheckBusy())
            {
                return;
            }
            fileList.Clear();
            LoadFilesToListView();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (fileList.Count == 0)
            {
                MessageBox.Show("请先添加或拖拽文件！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (CheckBusy())
            {
                return;
            }
            isBusy = true;
            Task.Run(() =>
            {
                try
                {
                    ConvertAll();
                }
                finally
                {
                    isBusy = false;
                }
            });
        }

        private void ConvertAll()
        {
            int succeeded = 0;
            int failed = 0;
            for (int i = 0; i < fileList.Count; i++)
            {
                if (fileList[i].Status == Status.转换完成)
                {
                    continue;
                }

                // get the list item
                var item = listView1.Items[i].SubItems[1];
                listView1.Items[i].EnsureVisible();

                // set converting
                item.ForeColor = ConvertColor(Status.正在转换);
                item.Text = Status.正在转换.ToString();
                fileList[i].Status = Status.正在转换;

                // convert to pdf，PDF 与 OFD 同目录、同名
                string PdfName = OfdScanner.GetPdfPath(fileList[i].FileName);
                ConvertResult result = converter.ConvertToPdf(fileList[i].FileName, PdfName);

                if (result == ConvertResult.Failed)
                {
                    item.ForeColor = ConvertColor(Status.转换失败);
                    item.Text = Status.转换失败.ToString();
                    fileList[i].Status = Status.转换失败;
                    failed++;
                }
                else
                {
                    item.ForeColor = ConvertColor(Status.转换完成);
                    item.Text = Status.转换完成.ToString();
                    fileList[i].Status = Status.转换完成;
                    succeeded++;
                }
            }

            if (succeeded == 0 && failed == 0)
            {
                MessageBox.Show("当前所有任务已经完成，请添加新的文件或清空重来。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(string.Format("转换结束：成功 {0} 个，失败 {1} 个。", succeeded, failed), "Ofd2Pdf",
                    MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            // 支持同时拖入文件和文件夹
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddPaths(files);
        }
    }
}
