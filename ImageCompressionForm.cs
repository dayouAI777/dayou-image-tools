using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMagick;
using ImageMagick.Formats;

namespace LocalPhotoViewer
{
    internal sealed class ImageCompressionResult
    {
        public string OutputPath;
        public long OriginalBytes;
        public long OutputBytes;
        public bool Lossless;
        public int JpegQuality;
    }

    internal sealed class ImageCompressionForm : Form
    {
        private readonly string _sourcePath;
        private readonly long _sourceBytes;
        private readonly TrackBar _targetPercent;
        private readonly Label _targetText;
        private readonly Label _status;
        private readonly ProgressBar _progress;
        private readonly CheckBox _allowLossyJpeg;
        private readonly Button _start;
        private bool _running;

        public string OutputPath { get; private set; }
        public string Summary { get; private set; }

        public ImageCompressionForm(string sourcePath)
        {
            _sourcePath = Path.GetFullPath(sourcePath);
            _sourceBytes = new FileInfo(_sourcePath).Length;
            string extension = Path.GetExtension(_sourcePath).ToLowerInvariant();
            bool isJpeg = extension == ".jpg" || extension == ".jpeg";
            bool isPng = extension == ".png";

            Text = "图片压缩";
            AppBranding.ApplyWindowIcon(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(570, 350);

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(22, 18, 22, 16), BackColor = PhotoEditorForm.Paper };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            Label heading = new Label { Text = "压缩图片体积", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy, TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(heading, 0, 0);

            Label source = new Label { Text = Path.GetFileName(_sourcePath) + "    原图 " + FormatBytes(_sourceBytes), Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Regular), ForeColor = PhotoEditorForm.Navy, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            layout.Controls.Add(source, 0, 1);

            Panel targetPanel = new Panel { Dock = DockStyle.Fill };
            _targetText = new Label { Dock = DockStyle.Top, Height = 22, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy };
            _targetPercent = new TrackBar { Minimum = 10, Maximum = 100, Value = 70, TickFrequency = 10, SmallChange = 5, LargeChange = 10, Dock = DockStyle.Bottom, Height = 48, BackColor = PhotoEditorForm.Paper };
            _targetPercent.ValueChanged += delegate { UpdateTargetText(); };
            targetPanel.Controls.Add(_targetPercent);
            targetPanel.Controls.Add(_targetText);
            layout.Controls.Add(targetPanel, 0, 2);

            _allowLossyJpeg = new CheckBox
            {
                Text = "允许 JPEG 重新编码以接近目标体积（画质可能轻微变化）",
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular),
                ForeColor = PhotoEditorForm.Navy,
                Enabled = isJpeg
            };
            layout.Controls.Add(_allowLossyJpeg, 0, 3);

            string guidance = isJpeg
                ? "默认无损：只移除 JPEG 注释、XMP 和可安全删除的非画面元数据，保留 EXIF 方向与 ICC 色彩配置。勾选上方选项后会重新编码。"
                : (isPng
                    ? "PNG 使用无损优化。若设定的目标体积过小，程序会保留画质并显示实际结果，不会为了追求百分比而降低像素质量。"
                    : "当前版本的单张无损压缩支持 JPG、JPEG 和 PNG。其他格式请先转换为这两种格式。原图不会被覆盖。");
            Label help = new Label { Text = guidance, Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular), ForeColor = Color.FromArgb(95, 101, 107), TextAlign = ContentAlignment.TopLeft };
            layout.Controls.Add(help, 0, 4);

            _status = new Label { Text = "原图保持不变，压缩结果会另存为副本。", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular), ForeColor = PhotoEditorForm.Navy, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(_status, 0, 5);

            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            Button close = new Button { Text = "取消", Width = 92, Height = 30, DialogResult = DialogResult.Cancel };
            _start = new Button { Text = "压缩并另存…", Width = 120, Height = 30, BackColor = PhotoEditorForm.Coral, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _start.FlatAppearance.BorderSize = 0;
            _start.Click += StartCompression;
            buttons.Controls.Add(close);
            buttons.Controls.Add(_start);
            _progress = new ProgressBar { Width = 150, Height = 24, Style = ProgressBarStyle.Blocks, Visible = false, Margin = new Padding(0, 4, 12, 0) };
            buttons.Controls.Add(_progress);
            layout.Controls.Add(buttons, 0, 6);
            Controls.Add(layout);
            AcceptButton = _start;
            CancelButton = close;
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (_running)
                {
                    e.Cancel = true;
                    _status.Text = "压缩正在进行，请等待完成后再关闭窗口。";
                }
            };

            _allowLossyJpeg.Visible = isJpeg;
            _allowLossyJpeg.Checked = false;
            _start.Enabled = isJpeg || isPng;
            if (!isJpeg && !isPng)
                _status.Text = "暂不支持此格式的单张无损压缩。";
            UpdateTargetText();
        }

        private void UpdateTargetText()
        {
            if (_targetText == null || _targetPercent == null) return;
            long target = (long)Math.Round(_sourceBytes * (_targetPercent.Value / 100.0));
            _targetText.Text = "目标体积：原图的 " + _targetPercent.Value + "%（约 " + FormatBytes(target) + "）";
        }

        private void StartCompression(object sender, EventArgs e)
        {
            if (_running) return;
            string extension = Path.GetExtension(_sourcePath);
            using (SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "压缩图片另存为",
                FileName = Path.GetFileNameWithoutExtension(_sourcePath) + "_compressed" + extension,
                DefaultExt = extension.TrimStart('.'),
                AddExtension = true,
                OverwritePrompt = true,
                Filter = (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ? "PNG 图片|*.png" : "JPEG 图片|*.jpg;*.jpeg")
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string output = Path.GetFullPath(dialog.FileName);
                if (String.Equals(output, _sourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "请选择不同的文件名，原图不会被覆盖。", "不能覆盖原图", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _running = true;
                _start.Enabled = false;
                _allowLossyJpeg.Enabled = false;
                _targetPercent.Enabled = false;
                _progress.Visible = true;
                _progress.Style = ProgressBarStyle.Marquee;
                _status.Text = "正在进行无损优化…";
                bool allowLossy = _allowLossyJpeg.Checked;
                int targetPercent = _targetPercent.Value;

                Task.Run(delegate
                {
                    return Compress(_sourcePath, output, targetPercent, allowLossy, delegate(int attempt, int total)
                    {
                        try
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                _progress.Style = ProgressBarStyle.Blocks;
                                _progress.Maximum = Math.Max(1, total);
                                _progress.Value = Math.Min(_progress.Maximum, Math.Max(0, attempt));
                                _status.Text = "正在尝试 JPEG 质量档位 " + attempt + "/" + total + "…";
                            });
                        }
                        catch (InvalidOperationException) { }
                    });
                }).ContinueWith(delegate(Task<ImageCompressionResult> task)
                {
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            _running = false;
                            _progress.Visible = false;
                            if (task.IsFaulted)
                            {
                                _start.Enabled = true;
                                _allowLossyJpeg.Enabled = _allowLossyJpeg.Visible;
                                _targetPercent.Enabled = true;
                                string message = task.Exception.GetBaseException().Message;
                                _status.Text = "压缩失败：" + message;
                                MessageBox.Show(this, message, "压缩失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            ImageCompressionResult result = task.Result;
                            OutputPath = result.OutputPath;
                            double ratio = result.OriginalBytes == 0 ? 0 : result.OutputBytes * 100.0 / result.OriginalBytes;
                            string targetText = "目标 " + targetPercent + "%，实际 " + ratio.ToString("0.0", CultureInfo.InvariantCulture) + "%";
                            string qualityText = result.Lossless ? "无损优化，画质不变。" : "JPEG 质量档位 " + result.JpegQuality + "，画质可能轻微变化。";
                            Summary = "原图 " + FormatBytes(result.OriginalBytes) + "；压缩后 " + FormatBytes(result.OutputBytes) + "；" + targetText + "。";
                            _status.Text = "已完成：" + targetText;
                            MessageBox.Show(this, Summary + "\n" + qualityText + "\n\n保存位置：" + result.OutputPath, "压缩完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            DialogResult = DialogResult.OK;
                            Close();
                        });
                    }
                    catch (InvalidOperationException) { }
                });
            }
        }

        private static ImageCompressionResult Compress(string source, string destination, int targetPercent, bool allowLossyJpeg, Action<int, int> progress)
        {
            long originalBytes = new FileInfo(source).Length;
            string extension = Path.GetExtension(source).ToLowerInvariant();
            string temp = Path.Combine(Path.GetDirectoryName(destination), Path.GetFileNameWithoutExtension(destination) + ".compress-" + Guid.NewGuid().ToString("N") + extension);
            string best = temp + ".best";
            int quality = 0;
            bool lossless = true;
            try
            {
                if ((extension == ".jpg" || extension == ".jpeg") && allowLossyJpeg)
                {
                    quality = CompressJpegToTarget(source, temp, best, originalBytes, targetPercent, progress);
                    lossless = quality == 0;
                    File.Copy(best, temp, true);
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    OptimizeJpegMetadata(source, temp);
                    if (new FileInfo(temp).Length >= originalBytes)
                        File.Copy(source, temp, true);
                }
                else if (extension == ".png")
                {
                    if (IsPng16Bit(source) || IsAnimatedPng(source))
                        File.Copy(source, temp, true);
                    else
                    {
                        using (MagickImage image = new MagickImage(source))
                        {
                            image.Write(temp, new PngWriteDefines { CompressionLevel = 9U });
                        }
                        if (new FileInfo(temp).Length >= originalBytes)
                            File.Copy(source, temp, true);
                    }
                }
                else
                    throw new InvalidDataException("当前版本支持 JPEG 和 PNG 图片的单张压缩。");

                if (File.Exists(destination)) File.Delete(destination);
                File.Move(temp, destination);
                return new ImageCompressionResult
                {
                    OutputPath = destination,
                    OriginalBytes = originalBytes,
                    OutputBytes = new FileInfo(destination).Length,
                    Lossless = lossless,
                    JpegQuality = quality
                };
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                try { if (File.Exists(best)) File.Delete(best); } catch { }
            }
        }

        private static int CompressJpegToTarget(string source, string temp, string best, long originalBytes, int targetPercent, Action<int, int> progress)
        {
            long targetBytes = (long)Math.Round(originalBytes * (targetPercent / 100.0));
            if (targetPercent >= 100)
            {
                File.Copy(source, best, true);
                return 0;
            }

            int low = 1;
            int high = 100;
            int bestQuality = 0;
            long bestSize = Int64.MaxValue;
            int attempt = 0;
            using (MagickImage image = new MagickImage(source))
            {
                while (low <= high && attempt < 9)
                {
                    int current = (low + high) / 2;
                    image.Quality = (uint)current;
                    image.Write(temp, MagickFormat.Jpeg);
                    long size = new FileInfo(temp).Length;
                    attempt++;
                    if (progress != null) progress(attempt, 10);
                    if (size <= targetBytes)
                    {
                        if (current > bestQuality)
                        {
                            File.Copy(temp, best, true);
                            bestQuality = current;
                            bestSize = size;
                        }
                        low = current + 1;
                    }
                    else
                        high = current - 1;
                }

                if (bestQuality == 0)
                {
                    image.Quality = 1U;
                    image.Write(temp, MagickFormat.Jpeg);
                    bestQuality = 1;
                    bestSize = new FileInfo(temp).Length;
                    File.Copy(temp, best, true);
                }
            }

            if (bestSize >= originalBytes)
            {
                File.Copy(source, best, true);
                return 0;
            }
            return bestQuality;
        }

        private static void OptimizeJpegMetadata(string source, string destination)
        {
            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (ReadByteRequired(input) != 0xFF || ReadByteRequired(input) != 0xD8)
                    throw new InvalidDataException("JPEG 文件头无效。");
                output.WriteByte(0xFF);
                output.WriteByte(0xD8);
                bool foundScan = false;
                while (input.Position < input.Length)
                {
                    using (MemoryStream markerBytes = new MemoryStream())
                    {
                        int first = ReadByteRequired(input);
                        if (first != 0xFF) throw new InvalidDataException("JPEG 标记结构无效。");
                        markerBytes.WriteByte((byte)first);
                        int marker = ReadByteRequired(input);
                        while (marker == 0xFF)
                        {
                            markerBytes.WriteByte((byte)marker);
                            marker = ReadByteRequired(input);
                        }
                        markerBytes.WriteByte((byte)marker);

                        if (marker == 0xD8 || marker == 0xD9 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                        {
                            markerBytes.WriteTo(output);
                            if (marker == 0xD9) break;
                            continue;
                        }

                        int lengthHigh = ReadByteRequired(input);
                        int lengthLow = ReadByteRequired(input);
                        int segmentLength = (lengthHigh << 8) | lengthLow;
                        if (segmentLength < 2) throw new InvalidDataException("JPEG 元数据段长度无效。");
                        byte[] payload = ReadExact(input, segmentLength - 2);
                        bool remove = marker == 0xFE || marker == 0xED || (marker == 0xE1 && IsXmpPayload(payload));
                        if (!remove)
                        {
                            markerBytes.WriteTo(output);
                            output.WriteByte((byte)lengthHigh);
                            output.WriteByte((byte)lengthLow);
                            output.Write(payload, 0, payload.Length);
                        }
                        if (marker == 0xDA)
                        {
                            input.CopyTo(output);
                            foundScan = true;
                            break;
                        }
                    }
                }
                if (!foundScan) throw new InvalidDataException("JPEG 图像扫描数据缺失。");
            }
        }

        private static bool IsXmpPayload(byte[] payload)
        {
            return StartsWith(payload, "http://ns.adobe.com/xap/1.0/") || StartsWith(payload, "http://ns.adobe.com/xmp/extension/");
        }

        private static bool StartsWith(byte[] value, string prefix)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(prefix);
            if (value.Length < bytes.Length) return false;
            for (int i = 0; i < bytes.Length; i++) if (value[i] != bytes[i]) return false;
            return true;
        }

        private static bool IsPng16Bit(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] header = ReadExact(stream, 25);
                return header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71 && header[24] == 16;
            }
        }

        private static bool IsAnimatedPng(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] signature = ReadExact(stream, 8);
                if (signature[0] != 137 || signature[1] != 80 || signature[2] != 78 || signature[3] != 71) return false;
                while (stream.Position + 12 <= stream.Length)
                {
                    byte[] chunkHeader = ReadExact(stream, 8);
                    uint length = ReadUInt32BigEndian(chunkHeader, 0);
                    string type = Encoding.ASCII.GetString(chunkHeader, 4, 4);
                    if (type == "acTL") return true;
                    if (type == "IDAT") return false;
                    long next = stream.Position + length + 4L;
                    if (next < stream.Position || next > stream.Length) return false;
                    stream.Position = next;
                }
            }
            return false;
        }

        private static uint ReadUInt32BigEndian(byte[] bytes, int offset)
        {
            return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static int ReadByteRequired(Stream stream)
        {
            int value = stream.ReadByte();
            if (value < 0) throw new EndOfStreamException("图片文件意外结束。");
            return value;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] bytes = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(bytes, offset, count - offset);
                if (read <= 0) throw new EndOfStreamException("图片文件意外结束。");
                offset += read;
            }
            return bytes;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
            if (bytes >= 1024L) return (bytes / 1024.0).ToString("0.##", CultureInfo.InvariantCulture) + " KB";
            return bytes + " B";
        }
    }
}
