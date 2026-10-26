using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Windows.Forms;

namespace LocalPhotoViewer
{
    internal enum BatchOperation { Resize, Compress, Convert, Rotate, Rename, Watermark, Crop, Copy, Move, Delete }

    internal sealed class BatchToolsForm : Form
    {
        private readonly List<string> _files;
        private readonly ComboBox _operation;
        private readonly TextBox _output;
        private readonly TextBox _width;
        private readonly TextBox _height;
        private readonly CheckBox _keepRatio;
        private readonly NumericUpDown _quality;
        private readonly ComboBox _format;
        private readonly ComboBox _rotation;
        private readonly TextBox _prefix;
        private readonly TextBox _watermark;
        private readonly ProgressBar _progress;
        private readonly ListBox _log;
        private bool _running;

        public BatchToolsForm(IList<string> files)
        {
            _files = new List<string>(files ?? new string[0]);
            Text = AppBranding.ProductName + " · 批量工具";
            AppBranding.ApplyWindowIcon(this);
            Width = 820;
            Height = 720;
            MinimumSize = new Size(680, 570);
            StartPosition = FormStartPosition.CenterParent;

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            Label heading = new Label { Text = "批量处理  ·  " + _files.Count + " 张图片", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(heading, 0, 0);

            FlowLayoutPanel settings = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
            _operation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
            _operation.Items.AddRange(new object[] { "批量改尺寸", "批量压缩 JPEG", "批量格式转换", "批量旋转", "批量重命名", "批量文字水印", "批量居中裁剪", "批量复制", "批量移动", "批量移入回收站" });
            _operation.SelectedIndex = 0;
            _width = new TextBox { Text = "1920", Width = 62 };
            _height = new TextBox { Text = "1080", Width = 62 };
            _keepRatio = new CheckBox { Text = "保持比例", Checked = true, AutoSize = true, Padding = new Padding(5, 7, 5, 0) };
            _quality = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 85, Width = 65 };
            _format = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75 };
            _format.Items.AddRange(new object[] { "PNG", "JPEG", "BMP", "TIFF", "GIF" });
            _format.SelectedIndex = 0;
            _rotation = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
            _rotation.Items.AddRange(new object[] { "向右 90°", "向左 90°", "180°" });
            _rotation.SelectedIndex = 0;
            _prefix = new TextBox { Text = "图片_", Width = 120 };
            _watermark = new TextBox { Text = "我的图片", Width = 130 };
            settings.Controls.Add(new Label { Text = "操作", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            settings.Controls.Add(_operation);
            settings.Controls.Add(new Label { Text = "宽", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }); settings.Controls.Add(_width);
            settings.Controls.Add(new Label { Text = "高", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }); settings.Controls.Add(_height);
            settings.Controls.Add(_keepRatio);
            settings.Controls.Add(new Label { Text = "质量", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }); settings.Controls.Add(_quality);
            settings.Controls.Add(_format);
            settings.Controls.Add(_rotation);
            settings.Controls.Add(_prefix);
            settings.Controls.Add(_watermark);
            layout.Controls.Add(settings, 0, 1);

            FlowLayoutPanel outputRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            _output = new TextBox { Width = 610, ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            if (_files.Count > 0) _output.Text = Path.Combine(Path.GetDirectoryName(_files[0]), "PhotoStudio_Output");
            Button browse = new Button { Text = "输出文件夹…", Width = 120 };
            browse.Click += BrowseOutput;
            outputRow.Controls.Add(new Label { Text = "输出到", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); outputRow.Controls.Add(_output); outputRow.Controls.Add(browse);
            layout.Controls.Add(outputRow, 0, 2);

            ListView fileList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
            fileList.Columns.Add("待处理图片", 740);
            foreach (string path in _files) fileList.Items.Add(new ListViewItem(path));
            layout.Controls.Add(fileList, 0, 3);

            FlowLayoutPanel commands = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            Button run = new Button { Text = "开始批处理", Width = 130, Height = 30 };
            run.Click += RunBatch;
            Button folder = new Button { Text = "打开输出位置", Width = 130, Height = 30 };
            folder.Click += delegate { if (Directory.Exists(_output.Text)) System.Diagnostics.Process.Start("explorer.exe", _output.Text); };
            _progress = new ProgressBar { Width = 350, Height = 24, Style = ProgressBarStyle.Continuous };
            commands.Controls.Add(run); commands.Controls.Add(folder); commands.Controls.Add(_progress);
            layout.Controls.Add(commands, 0, 4);

            _log = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            layout.Controls.Add(_log, 0, 5);
            Controls.Add(layout);
            _operation.SelectedIndexChanged += delegate { UpdateSettingsVisibility(settings); };
            UpdateSettingsVisibility(settings);
        }

        private void UpdateSettingsVisibility(FlowLayoutPanel panel)
        {
            int op = _operation.SelectedIndex;
            foreach (Control control in panel.Controls) control.Visible = false;
            panel.Controls[0].Visible = true; panel.Controls[1].Visible = true;
            if (op == 0 || op == 6) { for (int i = 2; i <= 6; i++) panel.Controls[i].Visible = true; }
            else if (op == 1) { panel.Controls[7].Visible = true; panel.Controls[8].Visible = true; }
            else if (op == 2) { panel.Controls[7].Visible = true; panel.Controls[8].Visible = true; panel.Controls[9].Visible = true; }
            else if (op == 3) { panel.Controls[10].Visible = true; }
            else if (op == 4) { panel.Controls[11].Visible = true; }
            else if (op == 5) { panel.Controls[12].Visible = true; }
        }

        private void BrowseOutput(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog { Description = "选择批处理结果保存位置", ShowNewFolderButton = true })
            {
                if (Directory.Exists(_output.Text)) dialog.SelectedPath = _output.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK) _output.Text = dialog.SelectedPath;
            }
        }

        private void RunBatch(object sender, EventArgs e)
        {
            if (_running) return;
            if (_files.Count == 0) { MessageBox.Show(this, "没有选择图片。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            BatchOperation operation = (BatchOperation)_operation.SelectedIndex;
            if (operation == BatchOperation.Delete)
            {
                DialogResult confirm = MessageBox.Show(this, "将把选中的 " + _files.Count + " 个源文件移入 Windows 回收站，之后可以从回收站恢复。是否继续？", "批量移入回收站", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
            }
            else
            {
                if (String.IsNullOrWhiteSpace(_output.Text)) { MessageBox.Show(this, "请先选择输出文件夹。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                try { Directory.CreateDirectory(_output.Text); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "不能创建输出文件夹", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            }

            _running = true;
            _progress.Value = 0;
            _progress.Maximum = _files.Count;
            _log.Items.Clear();
            int width = ParsePositive(_width.Text, 1920), height = ParsePositive(_height.Text, 1080);
            int index = 0, failures = 0;
            foreach (string source in _files)
            {
                try
                {
                    string output = OutputPath(source, operation, index);
                    ProcessOne(source, output, operation, width, height);
                    _log.Items.Add(operation == BatchOperation.Delete
                        ? "已移入回收站  " + Path.GetFileName(source)
                        : "完成  " + Path.GetFileName(source) + "  →  " + Path.GetFileName(output));
                }
                catch (Exception ex)
                {
                    failures++;
                    _log.Items.Add("失败  " + Path.GetFileName(source) + "  ·  " + ex.Message);
                }
                index++;
                _progress.Value = index;
                Application.DoEvents();
            }
            _running = false;
            MessageBox.Show(this, String.Format("处理完成：成功 {0} 张，失败 {1} 张。\n结果位于：{2}", _files.Count - failures, failures, _output.Text), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string OutputPath(string source, BatchOperation operation, int index)
        {
            string stem = Path.GetFileNameWithoutExtension(source);
            string ext = Path.GetExtension(source);
            if (operation == BatchOperation.Rename) stem = _prefix.Text + (index + 1).ToString("D3");
            if (operation == BatchOperation.Convert) ext = "." + _format.SelectedItem.ToString().ToLowerInvariant().Replace("jpeg", "jpg").Replace("tiff", "tif");
            if (operation == BatchOperation.Compress) ext = ".jpg";
            if (operation == BatchOperation.Delete) return String.Empty;
            if (operation == BatchOperation.Copy || operation == BatchOperation.Move)
            {
                string plain = Path.Combine(_output.Text, Path.GetFileName(source));
                if (!File.Exists(plain)) return plain;
                int copySuffix = 1;
                string copyPath;
                do { copyPath = Path.Combine(_output.Text, stem + "_" + copySuffix++ + ext); } while (File.Exists(copyPath));
                return copyPath;
            }
            string path = Path.Combine(_output.Text, stem + "_processed" + ext);
            if (operation == BatchOperation.Rename) path = Path.Combine(_output.Text, stem + ext);
            int suffix = 1;
            while (File.Exists(path))
            {
                string name = operation == BatchOperation.Rename ? stem + "_" + suffix : stem + "_processed_" + suffix;
                path = Path.Combine(_output.Text, name + ext);
                suffix++;
            }
            return path;
        }

        private void ProcessOne(string source, string destination, BatchOperation operation, int width, int height)
        {
            if (operation == BatchOperation.Delete)
            {
                FileSystem.DeleteFile(source, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                return;
            }
            if (operation == BatchOperation.Copy) { File.Copy(source, destination, false); return; }
            if (operation == BatchOperation.Move) { File.Move(source, destination); return; }
            if (operation == BatchOperation.Rename) { File.Copy(source, destination, false); return; }
            using (DecodedImage decoded = ImageDecoder.Load(source))
            using (Bitmap bitmap = new Bitmap(decoded.Image))
            {
                if (operation == BatchOperation.Rotate)
                {
                    if (_rotation.SelectedIndex == 0) bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    else if (_rotation.SelectedIndex == 1) bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    else bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    PhotoEditorForm.SaveBitmap(bitmap, destination, (long)_quality.Value);
                }
                else if (operation == BatchOperation.Resize)
                {
                    int targetW=width, targetH=height;
                    if (_keepRatio.Checked)
                    {
                        double scale=Math.Min((double)width/bitmap.Width,(double)height/bitmap.Height);
                        targetW=Math.Max(1,(int)Math.Round(bitmap.Width*scale)); targetH=Math.Max(1,(int)Math.Round(bitmap.Height*scale));
                    }
                    using (Bitmap resized = new Bitmap(targetW,targetH,PixelFormat.Format32bppArgb))
                    using (Graphics g=Graphics.FromImage(resized))
                    {
                        g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.SmoothingMode=SmoothingMode.HighQuality;g.DrawImage(bitmap,new Rectangle(0,0,targetW,targetH));
                        PhotoEditorForm.SaveBitmap(resized,destination,(long)_quality.Value);
                    }
                }
                else if (operation == BatchOperation.Crop)
                {
                    double targetRatio = (double)width / height;
                    int cropWidth = bitmap.Width, cropHeight = bitmap.Height;
                    if ((double)bitmap.Width / bitmap.Height > targetRatio) cropWidth = Math.Max(1, (int)Math.Round(bitmap.Height * targetRatio));
                    else cropHeight = Math.Max(1, (int)Math.Round(bitmap.Width / targetRatio));
                    Rectangle crop = new Rectangle((bitmap.Width - cropWidth) / 2, (bitmap.Height - cropHeight) / 2, cropWidth, cropHeight);
                    using (Bitmap cropped = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    using (Graphics g = Graphics.FromImage(cropped))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bitmap, new Rectangle(0, 0, width, height), crop, GraphicsUnit.Pixel);
                        PhotoEditorForm.SaveBitmap(cropped, destination, (long)_quality.Value);
                    }
                }
                else if (operation == BatchOperation.Watermark)
                {
                    using (Graphics g=Graphics.FromImage(bitmap))
                    using (Font font=new Font("Microsoft YaHei UI",Math.Max(18,bitmap.Width/45f),FontStyle.Bold,GraphicsUnit.Pixel))
                    using (Brush brush=new SolidBrush(Color.FromArgb(150,Color.White)))
                    {
                        SizeF size=g.MeasureString(_watermark.Text,font);
                        g.DrawString(_watermark.Text,font,brush,Math.Max(0,bitmap.Width-size.Width-20),Math.Max(0,bitmap.Height-size.Height-18));
                    }
                    PhotoEditorForm.SaveBitmap(bitmap,destination,(long)_quality.Value);
                }
                else if (operation == BatchOperation.Compress)
                {
                    using (Bitmap flattened = FlattenOnWhite(bitmap)) PhotoEditorForm.SaveBitmap(flattened,destination,(long)_quality.Value);
                }
                else if (operation == BatchOperation.Convert)
                {
                    if (destination.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase))
                    {
                        using (Bitmap flattened = FlattenOnWhite(bitmap)) PhotoEditorForm.SaveBitmap(flattened,destination,(long)_quality.Value);
                    }
                    else PhotoEditorForm.SaveBitmap(bitmap,destination,(long)_quality.Value);
                }
            }
        }

        private static Bitmap FlattenOnWhite(Image source)
        {
            Bitmap result=new Bitmap(source.Width,source.Height,PixelFormat.Format24bppRgb);
            using(Graphics g=Graphics.FromImage(result)){g.Clear(Color.White);g.DrawImage(source,0,0,source.Width,source.Height);}
            return result;
        }

        private static int ParsePositive(string text,int fallback)
        {
            int value; return Int32.TryParse(text,out value)&&value>0?Math.Min(30000,value):fallback;
        }
    }

    internal sealed class SimpleInputDialog : Form
    {
        private readonly TextBox _value;
        public string Value { get { return _value.Text; } }
        public SimpleInputDialog(string title, string label, string initialValue)
        {
            Text = title; Width = 430; Height = 180; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MinimizeBox = false; MaximizeBox = false;
            Controls.Add(new Label { Text = label, Location = new Point(16, 18), Width = 380 });
            _value = new TextBox { Text = initialValue, Location = new Point(16, 46), Width = 380 };
            Controls.Add(_value);
            Button ok = new Button { Text = "确定", Location = new Point(214, 88), DialogResult = DialogResult.OK };
            Button cancel = new Button { Text = "取消", Location = new Point(304, 88), DialogResult = DialogResult.Cancel };
            Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel;
        }
    }
}
