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
        public bool Resized;
        public bool MetadataStripped;
    }

    internal sealed class ImageCompressionForm : Form
    {
        private readonly string _sourcePath;
        private readonly long _sourceBytes;
        private readonly TrackBar _targetPercent;
        private readonly Label _targetText;
        private readonly RadioButton _percentageMode;
        private readonly RadioButton _forceTargetMode;
        private readonly NumericUpDown _targetSizeKb;
        private readonly Label _help;
        private readonly Label _status;
        private readonly ProgressBar _progress;
        private readonly CheckBox _allowLossyJpeg;
        private readonly Button _start;
        private readonly TableLayoutPanel _layout;
        private readonly bool _isJpeg;
        private readonly bool _isPng;
        private bool _running;

        public string OutputPath { get; private set; }
        public string Summary { get; private set; }

        public ImageCompressionForm(string sourcePath)
        {
            _sourcePath = Path.GetFullPath(sourcePath);
            _sourceBytes = new FileInfo(_sourcePath).Length;
            string extension = Path.GetExtension(_sourcePath).ToLowerInvariant();
            _isJpeg = extension == ".jpg" || extension == ".jpeg";
            _isPng = extension == ".png";

            Text = "图片压缩";
            AppBranding.ApplyWindowIcon(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(600, 410);

            _layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, Padding = new Padding(22, 18, 22, 16), BackColor = PhotoEditorForm.Paper };
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            Label heading = new Label { Text = "压缩图片体积", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy, TextAlign = ContentAlignment.MiddleLeft };
            _layout.Controls.Add(heading, 0, 0);

            Label source = new Label { Text = Path.GetFileName(_sourcePath) + "    原图 " + FormatBytes(_sourceBytes), Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Regular), ForeColor = PhotoEditorForm.Navy, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            _layout.Controls.Add(source, 0, 1);

            FlowLayoutPanel modePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 7, 0, 0) };
            _percentageMode = new RadioButton { Text = "按比例压缩", Checked = true, AutoSize = true, ForeColor = PhotoEditorForm.Navy, Margin = new Padding(0, 0, 24, 0) };
            _forceTargetMode = new RadioButton { Text = "强制到指定大小", AutoSize = true, ForeColor = PhotoEditorForm.Navy };
            modePanel.Controls.Add(_percentageMode);
            modePanel.Controls.Add(_forceTargetMode);
            _layout.Controls.Add(modePanel, 0, 2);

            Panel targetPanel = new Panel { Dock = DockStyle.Fill };
            _targetText = new Label { Dock = DockStyle.Top, Height = 22, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy };
            _targetPercent = new TrackBar { Minimum = 10, Maximum = 100, Value = 70, TickFrequency = 10, SmallChange = 5, LargeChange = 10, Dock = DockStyle.Bottom, Height = 48, BackColor = PhotoEditorForm.Paper };
            _targetPercent.ValueChanged += delegate { UpdateTargetText(); };
            targetPanel.Controls.Add(_targetPercent);
            targetPanel.Controls.Add(_targetText);
            _layout.Controls.Add(targetPanel, 0, 3);

            FlowLayoutPanel exactTargetPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
            exactTargetPanel.Controls.Add(new Label { Text = "目标大小上限", AutoSize = true, ForeColor = PhotoEditorForm.Navy, Padding = new Padding(0, 4, 8, 0) });
            decimal suggestedTarget = Math.Max(50m, Math.Min(1000000m, (decimal)Math.Round(_sourceBytes / 1024.0 * 0.5)));
            _targetSizeKb = new NumericUpDown { Minimum = 50, Maximum = 1000000, Increment = 10, Value = suggestedTarget, Width = 115, ThousandsSeparator = true };
            exactTargetPanel.Controls.Add(_targetSizeKb);
            exactTargetPanel.Controls.Add(new Label { Text = "KB（最小 50）", AutoSize = true, ForeColor = PhotoEditorForm.MutedInk, Padding = new Padding(8, 4, 0, 0) });
            _layout.Controls.Add(exactTargetPanel, 0, 4);

            _allowLossyJpeg = new CheckBox
            {
                Text = "允许 JPEG 重新编码以接近目标体积（画质可能轻微变化）",
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular),
                ForeColor = PhotoEditorForm.Navy,
                Enabled = _isJpeg
            };
            _layout.Controls.Add(_allowLossyJpeg, 0, 5);

            _help = new Label { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular), ForeColor = Color.FromArgb(95, 101, 107), TextAlign = ContentAlignment.TopLeft };
            _layout.Controls.Add(_help, 0, 6);

            _status = new Label { Text = "原图保持不变，压缩结果会另存为副本。", Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular), ForeColor = PhotoEditorForm.Navy, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
            _layout.Controls.Add(_status, 0, 7);

            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            Button close = new Button { Text = "取消", Width = 92, Height = 30, DialogResult = DialogResult.Cancel };
            _start = new Button { Text = "压缩并另存…", Width = 120, Height = 30, BackColor = PhotoEditorForm.Coral, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _start.FlatAppearance.BorderSize = 0;
            _start.Click += StartCompression;
            buttons.Controls.Add(close);
            buttons.Controls.Add(_start);
            _progress = new ProgressBar { Width = 150, Height = 24, Style = ProgressBarStyle.Blocks, Visible = false, Margin = new Padding(0, 4, 12, 0) };
            buttons.Controls.Add(_progress);
            _layout.Controls.Add(buttons, 0, 8);
            Controls.Add(_layout);
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

            _allowLossyJpeg.Visible = _isJpeg;
            _allowLossyJpeg.Checked = false;
            _start.Enabled = _isJpeg || _isPng;
            _forceTargetMode.Enabled = _isJpeg || _isPng;
            if (!_isJpeg && !_isPng)
                _status.Text = "暂不支持此格式的单张无损压缩。";
            _percentageMode.CheckedChanged += delegate { if (_percentageMode.Checked) UpdateModeVisibility(); };
            _forceTargetMode.CheckedChanged += delegate { if (_forceTargetMode.Checked) UpdateModeVisibility(); };
            UpdateModeVisibility();
            UpdateTargetText();
        }

        private void UpdateTargetText()
        {
            if (_targetText == null || _targetPercent == null) return;
            long target = (long)Math.Round(_sourceBytes * (_targetPercent.Value / 100.0));
            _targetText.Text = "目标体积：原图的 " + _targetPercent.Value + "%（约 " + FormatBytes(target) + "）";
        }

        private void UpdateModeVisibility()
        {
            bool forceTarget = _forceTargetMode.Checked;
            _layout.RowStyles[3].Height = forceTarget ? 0 : 76;
            _layout.RowStyles[4].Height = forceTarget ? 42 : 0;
            bool showLossyOption = _isJpeg && !forceTarget;
            _layout.RowStyles[5].Height = showLossyOption ? 38 : 0;
            _targetPercent.Visible = !forceTarget;
            _targetSizeKb.Parent.Visible = forceTarget;
            _allowLossyJpeg.Visible = showLossyOption;
            _percentageMode.Enabled = !_running;
            _forceTargetMode.Enabled = (_isJpeg || _isPng) && !_running;
            _targetPercent.Enabled = !forceTarget && !_running;
            _targetSizeKb.Enabled = forceTarget && !_running;
            _allowLossyJpeg.Enabled = showLossyOption && !_running;

            if (forceTarget)
            {
                _help.Text = _isJpeg
                    ? "强制模式会优先调整 JPEG 编码质量；仍超出目标时会降低分辨率并移除部分元数据。画面细节或色彩可能变化。"
                    : "强制模式会先无损优化 PNG；仍超出目标时会降低分辨率并移除部分元数据。PNG 格式保留，但画面细节或色彩可能变化。";
            }
            else
            {
                _help.Text = _isJpeg
                    ? "默认无损：只移除 JPEG 注释、XMP 和可安全删除的非画面元数据，保留 EXIF 方向与 ICC 色彩配置。勾选上方选项后会重新编码。"
                    : (_isPng
                        ? "PNG 使用无损优化。若设定的目标体积过小，程序会保留画质并显示实际结果，不会为了追求百分比而降低像素质量。"
                        : "当前版本的单张无损压缩支持 JPG、JPEG 和 PNG。其他格式请先转换为这两种格式。原图不会被覆盖。");
            }
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
                UpdateModeVisibility();
                _progress.Visible = true;
                _progress.Style = ProgressBarStyle.Marquee;
                bool forceTarget = _forceTargetMode.Checked;
                long targetBytes = (long)_targetSizeKb.Value * 1024L;
                _status.Text = forceTarget ? "正在调整压缩参数以满足目标体积…" : "正在进行无损优化…";
                bool allowLossy = _allowLossyJpeg.Checked;
                int targetPercent = _targetPercent.Value;

                Task.Run(delegate
                {
                    return Compress(_sourcePath, output, targetPercent, forceTarget, targetBytes, allowLossy, delegate(int attempt, int total)
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
                                UpdateModeVisibility();
                                string message = task.Exception.GetBaseException().Message;
                                _status.Text = "压缩失败：" + message;
                                MessageBox.Show(this, message, "压缩失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            ImageCompressionResult result = task.Result;
                            OutputPath = result.OutputPath;
                            double ratio = result.OriginalBytes == 0 ? 0 : result.OutputBytes * 100.0 / result.OriginalBytes;
                            string targetText = forceTarget
                                ? "目标≤" + FormatBytes(targetBytes) + "，实际 " + FormatBytes(result.OutputBytes) + (result.OutputBytes <= targetBytes ? "（已达到）" : "（未达到）")
                                : "目标 " + targetPercent + "%，实际 " + ratio.ToString("0.0", CultureInfo.InvariantCulture) + "%";
                            string qualityText;
                            if (result.Resized)
                                qualityText = "为达到目标已降低分辨率，画面细节会变化。" + (result.MetadataStripped ? "部分元数据也已移除。" : "");
                            else if (result.JpegQuality > 0)
                                qualityText = "JPEG 质量档位 " + result.JpegQuality + "，画质可能变化。" + (result.MetadataStripped ? "部分元数据也已移除。" : "");
                            else if (result.MetadataStripped) qualityText = "已移除部分元数据，像素尺寸未改变。";
                            else qualityText = result.Lossless ? "无损优化，画质不变。" : "图片质量可能变化。";
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

        private static ImageCompressionResult Compress(string source, string destination, int targetPercent, bool forceTarget, long targetBytes, bool allowLossyJpeg, Action<int, int> progress)
        {
            long originalBytes = new FileInfo(source).Length;
            string extension = Path.GetExtension(source).ToLowerInvariant();
            string temp = Path.Combine(Path.GetDirectoryName(destination), Path.GetFileNameWithoutExtension(destination) + ".compress-" + Guid.NewGuid().ToString("N") + extension);
            string best = temp + ".best";
            int quality = 0;
            bool lossless = true;
            bool resized = false;
            bool metadataStripped = false;
            try
            {
                if ((extension == ".jpg" || extension == ".jpeg") && forceTarget)
                {
                    quality = CompressJpegToSize(source, temp, best, originalBytes, targetBytes, out resized, out metadataStripped);
                    lossless = quality == 0 && !resized && !metadataStripped;
                    File.Copy(best, temp, true);
                }
                else if ((extension == ".jpg" || extension == ".jpeg") && allowLossyJpeg)
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
                    if (forceTarget)
                    {
                        if (IsPng16Bit(source) || IsAnimatedPng(source))
                            throw new InvalidDataException("强制压缩暂不支持 16 位或动画 PNG，以避免丢失色深或动画帧。请先另存为普通 PNG。");
                        CompressPngToSize(source, temp, best, originalBytes, targetBytes, out resized, out metadataStripped);
                        File.Copy(best, temp, true);
                        lossless = !resized && !metadataStripped;
                    }
                    else if (IsPng16Bit(source) || IsAnimatedPng(source))
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

                if (forceTarget && new FileInfo(temp).Length > targetBytes)
                    throw new InvalidDataException("无法将图片压缩到指定体积。");
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(temp, destination);
                return new ImageCompressionResult
                {
                    OutputPath = destination,
                    OriginalBytes = originalBytes,
                    OutputBytes = new FileInfo(destination).Length,
                    Lossless = lossless,
                    JpegQuality = quality,
                    Resized = resized,
                    MetadataStripped = metadataStripped
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

        private const int MinimumJpegQuality = 35;

        private static int CompressJpegToSize(string source, string temp, string best, long originalBytes, long targetBytes, out bool resized, out bool metadataStripped)
        {
            resized = false;
            metadataStripped = false;
            if (originalBytes <= targetBytes)
            {
                File.Copy(source, best, true);
                return 0;
            }

            using (MagickImage original = new MagickImage(source))
            {
                original.AutoOrient();
                long size = WriteJpegAtQuality(original, MinimumJpegQuality, temp);
                if (size > targetBytes)
                {
                    original.Strip();
                    metadataStripped = true;
                    size = WriteJpegAtQuality(original, MinimumJpegQuality, temp);
                }
                if (size <= targetBytes)
                {
                    File.Copy(temp, best, true);
                    return FindJpegQuality(original, temp, best, targetBytes);
                }

                double scale = FindFittingScale(temp, best, targetBytes, size, Math.Max(original.Width, original.Height),
                    delegate(double value)
                    {
                        using (MagickImage candidate = CreateScaledImage(original, value))
                            return WriteJpegAtQuality(candidate, MinimumJpegQuality, temp);
                    });
                resized = true;
                using (MagickImage candidate = CreateScaledImage(original, scale))
                    return FindJpegQuality(candidate, temp, best, targetBytes);
            }
        }

        private static int FindJpegQuality(MagickImage image, string temp, string best, long targetBytes)
        {
            int low = MinimumJpegQuality + 1;
            int high = 100;
            int bestQuality = MinimumJpegQuality;
            while (low <= high)
            {
                int quality = (low + high) / 2;
                if (WriteJpegAtQuality(image, quality, temp) <= targetBytes)
                {
                    File.Copy(temp, best, true);
                    bestQuality = quality;
                    low = quality + 1;
                }
                else
                    high = quality - 1;
            }
            return bestQuality;
        }

        private static long WriteJpegAtQuality(MagickImage image, int quality, string path)
        {
            image.Quality = (uint)quality;
            image.Write(path, MagickFormat.Jpeg);
            return new FileInfo(path).Length;
        }

        private static void CompressPngToSize(string source, string temp, string best, long originalBytes, long targetBytes, out bool resized, out bool metadataStripped)
        {
            resized = false;
            metadataStripped = false;
            if (originalBytes <= targetBytes)
            {
                File.Copy(source, best, true);
                return;
            }

            using (MagickImage original = new MagickImage(source))
            {
                original.AutoOrient();
                long size = WritePngCandidate(original, temp);
                if (size <= targetBytes)
                {
                    File.Copy(temp, best, true);
                    return;
                }

                using (MagickImage stripped = CloneDecodedImage(original))
                {
                    stripped.Strip();
                    size = WritePngCandidate(stripped, temp);
                    if (size <= targetBytes)
                    {
                        metadataStripped = true;
                        File.Copy(temp, best, true);
                        return;
                    }

                    double scale = FindFittingScale(temp, best, targetBytes, size, Math.Max(stripped.Width, stripped.Height),
                        delegate(double value)
                        {
                            using (MagickImage candidate = CreateScaledImage(stripped, value))
                                return WritePngCandidate(candidate, temp);
                        });
                    resized = true;
                    using (MagickImage candidate = CreateScaledImage(original, scale))
                    {
                        metadataStripped = WritePngCandidate(candidate, temp) > targetBytes;
                        if (!metadataStripped)
                            File.Copy(temp, best, true);
                    }
                }
            }
        }

        private static long WritePngCandidate(MagickImage image, string path)
        {
            image.Write(path, new PngWriteDefines { CompressionLevel = 4U });
            return new FileInfo(path).Length;
        }

        private static double FindFittingScale(string temp, string best, long targetBytes, long fullSize, uint longestEdge, Func<double, long> encode)
        {
            double minScale = 1.0 / longestEdge;
            double upper = 1.0;
            double scale = Math.Max(minScale, EstimateSmallerScale(upper, fullSize, targetBytes));
            long fittingSize = 0;
            double fittingScale = 0;

            // Estimate how much the pixel area must shrink from each measured file size.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                long size = encode(scale);
                if (size <= targetBytes)
                {
                    fittingSize = size;
                    fittingScale = scale;
                    File.Copy(temp, best, true);
                    break;
                }
                upper = scale;
                if (scale <= minScale) break;
                scale = Math.Max(minScale, EstimateSmallerScale(scale, size, targetBytes));
            }

            if (fittingScale == 0)
            {
                if (encode(minScale) > targetBytes)
                    throw new InvalidDataException("无法将图片压缩到指定体积。");
                File.Copy(temp, best, true);
                return minScale;
            }

            // Once a candidate fits, limit further attempts to enlarge it.
            for (int attempt = 0; attempt < 4 && fittingSize < targetBytes * 0.95; attempt++)
            {
                double middle = (fittingScale + upper) / 2.0;
                if ((middle - fittingScale) * longestEdge < 1.0) break;
                long size = encode(middle);
                if (size <= targetBytes)
                {
                    fittingScale = middle;
                    fittingSize = size;
                    File.Copy(temp, best, true);
                }
                else
                    upper = middle;
            }
            return fittingScale;
        }

        private static double EstimateSmallerScale(double scale, long size, long targetBytes)
        {
            return scale * Math.Min(0.85, 0.97 * Math.Sqrt(targetBytes / (double)size));
        }

        private static MagickImage CreateScaledImage(MagickImage original, double scale)
        {
            MagickImage candidate = CloneDecodedImage(original);
            try
            {
                uint width = Math.Max(1U, (uint)Math.Round(original.Width * scale));
                uint height = Math.Max(1U, (uint)Math.Round(original.Height * scale));
                if (candidate.Width != width || candidate.Height != height)
                    candidate.Resize(width, height);
                return candidate;
            }
            catch
            {
                candidate.Dispose();
                throw;
            }
        }

        private static MagickImage CloneDecodedImage(MagickImage image)
        {
            // The .NET Framework compiler cannot call Magick.NET's unmanaged generic return type directly.
            return (MagickImage)typeof(MagickImage).GetMethod("Clone", Type.EmptyTypes).Invoke(image, null);
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
