using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;

namespace LocalPhotoViewer
{
    internal sealed class CollageLayer : IDisposable
    {
        public string Path { get; private set; }
        public Bitmap Preview { get; private set; }
        public int SourceWidth { get; private set; }
        public int SourceHeight { get; private set; }
        public RectangleF Bounds;

        public CollageLayer(string path, Bitmap preview, int sourceWidth, int sourceHeight)
        {
            Path = path;
            Preview = preview;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
        }

        public void Dispose()
        {
            if (Preview != null) Preview.Dispose();
            Preview = null;
        }
    }

    internal sealed class CollageEditorForm : Form
    {
        private readonly List<CollageLayer> _layers = new List<CollageLayer>();
        private readonly CollageCanvas _canvas;
        private readonly ComboBox _ratio;
        private readonly NumericUpDown _canvasWidth;
        private readonly NumericUpDown _canvasHeight;
        private readonly ComboBox _background;
        private readonly ComboBox _guideGrid;
        private readonly CheckBox _showGridGuides;
        private readonly CheckBox _snapToCanvas;
        private readonly ComboBox _format;
        private readonly ComboBox _jpegQuality;
        private readonly Label _qualityLabel;
        private readonly ListBox _layerList;
        private readonly NumericUpDown _layerWidth;
        private readonly NumericUpDown _layerHeight;
        private readonly CheckBox _keepLayerRatio;
        private readonly Label _selectedLayerName;
        private readonly Label _status;
        private readonly Button _addButton;
        private readonly Button _exportButton;
        private BackgroundWorker _loadWorker;
        private BackgroundWorker _exportWorker;
        private bool _closing;
        private bool _updatingCanvasSize;
        private bool _updatingLayerSize;
        private bool _updatingLayerList;
        private Color _customBackground = Color.White;

        public string OutputPath { get; private set; }

        public CollageEditorForm(IList<string> initialFiles)
        {
            Text = AppBranding.ProductName + " · 自由拼图";
            AppBranding.ApplyWindowIcon(this);
            Width = 1240;
            Height = 820;
            MinimumSize = new Size(1020, 680);
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = PhotoEditorForm.Paper };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            FlowLayoutPanel toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(12, 9, 8, 5), BackColor = PhotoEditorForm.Paper };
            Label title = new Label { Text = "自由拼图", AutoSize = true, Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy, Padding = new Padding(0, 5, 16, 0) };
            toolbar.Controls.Add(title);
            _addButton = MakeButton("添加图片", 104, AddImages);
            toolbar.Controls.Add(_addButton);
            toolbar.Controls.Add(MakeButton("删除图片", 92, delegate { RemoveSelectedLayer(); }));
            toolbar.Controls.Add(MakeButton("置顶", 68, delegate { MoveSelectedLayer(1); }));
            toolbar.Controls.Add(MakeButton("置底", 68, delegate { MoveSelectedLayer(-1); }));
            _exportButton = MakeButton("导出拼图", 104, ExportCollage);
            toolbar.Controls.Add(_exportButton);
            root.Controls.Add(toolbar, 0, 0);

            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1240, 760), Orientation = Orientation.Vertical, SplitterWidth = 6, SplitterDistance = 870, FixedPanel = FixedPanel.Panel2, BackColor = PhotoEditorForm.PaperShade };
            split.Panel1MinSize = 580;
            split.Panel2MinSize = 292;
            Shown += delegate { if (split.Width > 900) split.SplitterDistance = split.Width - 310; };
            _canvas = new CollageCanvas(_layers) { Dock = DockStyle.Fill };
            _canvas.MouseEnter += delegate { _canvas.Focus(); };
            split.Panel1.Controls.Add(_canvas);

            FlowLayoutPanel settings = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(8, 8, 8, 8), BackColor = PhotoEditorForm.PaperShade };
            GroupBox canvasGroup = new GroupBox { Text = "画布", Width = 278, Height = 226, Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy };
            TableLayoutPanel canvasOptions = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 6), ColumnCount = 2, RowCount = 5 };
            canvasOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
            canvasOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ratio = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            _ratio.Items.AddRange(new object[] { "1:1", "1:2", "2:1", "9:16", "16:9", "3:4", "4:3", "3:2", "2:3", "5:4", "4:5", "21:9", "9:21", "自定义" });
            _ratio.SelectedIndex = 4;
            _canvasWidth = NewDimension(1920);
            _canvasHeight = NewDimension(1080);
            _background = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            _background.Items.AddRange(new object[] { "白色", "浅灰", "深色", "透明棋盘", "自定义颜色…" });
            _background.SelectedIndex = 0;
            AddOptionRow(canvasOptions, 0, "比例", _ratio);
            AddOptionRow(canvasOptions, 1, "宽度", _canvasWidth);
            AddOptionRow(canvasOptions, 2, "高度", _canvasHeight);
            AddOptionRow(canvasOptions, 3, "背景", _background);
            _format = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            _format.Items.AddRange(new object[] { "PNG", "JPEG" });
            _format.SelectedIndex = 0;
            AddOptionRow(canvasOptions, 4, "导出格式", _format);
            canvasGroup.Controls.Add(canvasOptions);
            settings.Controls.Add(canvasGroup);

            GroupBox guideGroup = new GroupBox { Text = "辅助线与吸附", Width = 278, Height = 142, Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy };
            TableLayoutPanel guideOptions = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 5, 8, 5), ColumnCount = 1, RowCount = 3 };
            guideOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            guideOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            guideOptions.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            _guideGrid = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            _guideGrid.Items.AddRange(new object[] { "4宫格（2×2）", "6宫格（3×2）", "9宫格（3×3）" });
            _guideGrid.SelectedIndex = 2;
            _showGridGuides = new CheckBox { Text = "显示引导线", AutoSize = true, ForeColor = PhotoEditorForm.Navy, Anchor = AnchorStyles.Left };
            _snapToCanvas = new CheckBox { Text = "图片边界自动吸附到画布", AutoSize = true, ForeColor = PhotoEditorForm.Navy, Anchor = AnchorStyles.Left };
            guideOptions.Controls.Add(_guideGrid, 0, 0);
            guideOptions.Controls.Add(_showGridGuides, 0, 1);
            guideOptions.Controls.Add(_snapToCanvas, 0, 2);
            guideGroup.Controls.Add(guideOptions);
            settings.Controls.Add(guideGroup);

            FlowLayoutPanel qualityRow = new FlowLayoutPanel { Width = 270, Height = 38, WrapContents = false, Margin = new Padding(4, 0, 4, 8) };
            _qualityLabel = new Label { Text = "JPEG 清晰度", AutoSize = true, Padding = new Padding(0, 8, 4, 0), ForeColor = PhotoEditorForm.Navy };
            _jpegQuality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 116 };
            _jpegQuality.Items.AddRange(new object[] { "普通（75）", "高清（90）", "超清（98）" });
            _jpegQuality.SelectedIndex = 1;
            qualityRow.Controls.Add(_qualityLabel);
            qualityRow.Controls.Add(_jpegQuality);
            settings.Controls.Add(qualityRow);

            GroupBox selectedGroup = new GroupBox { Text = "选中图片", Width = 278, Height = 138, Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy };
            TableLayoutPanel selectedOptions = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4), ColumnCount = 2, RowCount = 3 };
            selectedOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            selectedOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _selectedLayerName = new Label { Text = "未选择图片", AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = PhotoEditorForm.MutedInk };
            selectedOptions.Controls.Add(new Label { Text = "当前", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            selectedOptions.Controls.Add(_selectedLayerName, 1, 0);
            _layerWidth = NewDimension(1);
            _layerHeight = NewDimension(1);
            _keepLayerRatio = new CheckBox { Text = "等比例缩放", Checked = true, AutoSize = true, ForeColor = PhotoEditorForm.Navy, Padding = new Padding(0, 4, 0, 0) };
            selectedOptions.Controls.Add(new Label { Text = "宽度", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            selectedOptions.Controls.Add(_layerWidth, 1, 1);
            selectedOptions.Controls.Add(new Label { Text = "高度", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            selectedOptions.Controls.Add(_layerHeight, 1, 2);
            selectedGroup.Controls.Add(selectedOptions);
            settings.Controls.Add(selectedGroup);
            settings.Controls.Add(_keepLayerRatio);

            GroupBox layersGroup = new GroupBox { Text = "图层（列表从底层到顶层）", Width = 278, Height = 254, Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy };
            _layerList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Microsoft YaHei UI", 9f) };
            layersGroup.Controls.Add(_layerList);
            settings.Controls.Add(layersGroup);
            split.Panel2.Controls.Add(settings);
            root.Controls.Add(split, 0, 1);

            _status = new Label { Dock = DockStyle.Fill, Text = "拖动图片自由摆放；拖拽角点或滚动鼠标滚轮调整大小。引导线仅供参考，不会导出。", Padding = new Padding(10, 5, 0, 0), ForeColor = PhotoEditorForm.MutedInk, BackColor = PhotoEditorForm.Paper };
            root.Controls.Add(_status, 0, 2);
            Controls.Add(root);

            _canvas.SelectionChanged += CanvasSelectionChanged;
            _canvas.BoundsChanged += CanvasBoundsChanged;
            _guideGrid.SelectedIndexChanged += GuideSettingsChanged;
            _showGridGuides.CheckedChanged += GuideSettingsChanged;
            _snapToCanvas.CheckedChanged += GuideSettingsChanged;
            _layerList.SelectedIndexChanged += LayerListSelectionChanged;
            _ratio.SelectedIndexChanged += RatioChanged;
            _canvasWidth.ValueChanged += CanvasDimensionsChanged;
            _canvasHeight.ValueChanged += CanvasDimensionsChanged;
            _background.SelectedIndexChanged += BackgroundChanged;
            _format.SelectedIndexChanged += delegate { UpdateJpegQualityVisibility(); };
            _layerWidth.ValueChanged += LayerDimensionsChanged;
            _layerHeight.ValueChanged += LayerDimensionsChanged;
            ApplyGuideSettings();
            UpdateJpegQualityVisibility();

            _loadWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _loadWorker.DoWork += LoadImagesWorker;
            _loadWorker.ProgressChanged += LoadImagesProgressChanged;
            _loadWorker.RunWorkerCompleted += LoadImagesCompleted;
            _exportWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _exportWorker.DoWork += ExportWorker;
            _exportWorker.ProgressChanged += ExportProgressChanged;
            _exportWorker.RunWorkerCompleted += ExportCompleted;
            FormClosing += delegate { _closing = true; if (_loadWorker.IsBusy) _loadWorker.CancelAsync(); if (_exportWorker.IsBusy) _exportWorker.CancelAsync(); };

            if (initialFiles != null && initialFiles.Count > 0) AddImagePaths(initialFiles);
            RefreshLayerList();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete && !(ActiveControl is NumericUpDown))
            {
                RemoveSelectedLayer();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Up)) { MoveSelectedLayer(1); return true; }
            if (keyData == (Keys.Control | Keys.Down)) { MoveSelectedLayer(-1); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static NumericUpDown NewDimension(int value)
        {
            return new NumericUpDown { Minimum = 1, Maximum = 30000, Value = value, ThousandsSeparator = true, Dock = DockStyle.Fill };
        }

        private static void AddOptionRow(TableLayoutPanel panel, int row, string label, Control control)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = PhotoEditorForm.Navy }, 0, row);
            panel.Controls.Add(control, 1, row);
        }

        private static Button MakeButton(string text, int width, EventHandler handler)
        {
            Button button = new Button { Text = text, Width = width, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = PhotoEditorForm.Navy, Margin = new Padding(3, 0, 3, 0) };
            button.FlatAppearance.BorderColor = Color.FromArgb(202, 196, 184);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 228, 218);
            button.Click += handler;
            return button;
        }

        private void AddImages(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Title = "添加到自由拼图", Filter = ImageDecoder.OpenFileFilter, Multiselect = true })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) AddImagePaths(dialog.FileNames);
            }
        }

        private void AddImagePaths(IList<string> paths)
        {
            if (_loadWorker.IsBusy || paths == null || paths.Count == 0) return;
            string[] files = new string[paths.Count];
            paths.CopyTo(files, 0);
            _addButton.Enabled = false;
            _status.Text = "正在后台生成图片预览…";
            _loadWorker.RunWorkerAsync(new ImportRequest(files, _canvas.CanvasWidth, _canvas.CanvasHeight, _layers.Count));
        }

        private void LoadImagesWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            ImportRequest request = (ImportRequest)e.Argument;
            ImportResult result = new ImportResult();
            int validCount = 0;
            foreach (string path in request.Paths) if (!String.IsNullOrWhiteSpace(path) && File.Exists(path)) validCount++;
            int processed = 0;
            for (int i = 0; i < request.Paths.Length; i++)
            {
                if (worker.CancellationPending) { result.Dispose(); e.Cancel = true; return; }
                string path = request.Paths[i];
                if (String.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                try
                {
                    CollageLayer layer = CreatePreviewLayer(path, request.CanvasWidth, request.CanvasHeight, request.StartCount + result.Layers.Count);
                    result.Layers.Add(layer);
                }
                catch (Exception ex)
                {
                    if (result.Errors.Count < 10) result.Errors.Add(Path.GetFileName(path) + "：" + ex.Message);
                }
                processed++;
                worker.ReportProgress(validCount == 0 ? 100 : processed * 100 / validCount, new ImportProgress(processed, validCount));
            }
            if (worker.CancellationPending) { result.Dispose(); e.Cancel = true; return; }
            e.Result = result;
        }

        private static CollageLayer CreatePreviewLayer(string path, int canvasWidth, int canvasHeight, int offsetSeed)
        {
            using (DecodedImage decoded = ImageDecoder.Load(path))
            {
                int sourceWidth = decoded.Image.Width;
                int sourceHeight = decoded.Image.Height;
                int maxEdge = 768;
                float previewScale = Math.Min(1f, maxEdge / (float)Math.Max(sourceWidth, sourceHeight));
                int previewWidth = Math.Max(1, (int)Math.Round(sourceWidth * previewScale));
                int previewHeight = Math.Max(1, (int)Math.Round(sourceHeight * previewScale));
                Bitmap preview = new Bitmap(previewWidth, previewHeight, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(preview))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(decoded.Image, new Rectangle(0, 0, previewWidth, previewHeight));
                    }
                    CollageLayer layer = new CollageLayer(path, preview, sourceWidth, sourceHeight);
                    float maxWidth = canvasWidth * 0.42f;
                    float maxHeight = canvasHeight * 0.42f;
                    float scale = Math.Min(maxWidth / sourceWidth, maxHeight / sourceHeight);
                    float width = Math.Max(1f, sourceWidth * scale);
                    float height = Math.Max(1f, sourceHeight * scale);
                    int offsetIndex = offsetSeed % 7;
                    layer.Bounds = new RectangleF((canvasWidth - width) / 2f + offsetIndex * 24f, (canvasHeight - height) / 2f + offsetIndex * 18f, width, height);
                    return layer;
                }
                catch { preview.Dispose(); throw; }
            }
        }

        private void LoadImagesProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (_closing || IsDisposed) return;
            ImportProgress progress = e.UserState as ImportProgress;
            if (progress != null) _status.Text = "正在生成预览… " + progress.Processed + " / " + progress.Total;
        }

        private void LoadImagesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_closing || IsDisposed)
            {
                if (!e.Cancelled && e.Error == null && e.Result is ImportResult) ((ImportResult)e.Result).Dispose();
                return;
            }
            _addButton.Enabled = true;
            if (e.Error != null)
            {
                _status.Text = "添加图片失败。";
                MessageBox.Show(this, "添加图片失败：\n" + e.Error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (e.Cancelled) return;
            ImportResult result = (ImportResult)e.Result;
            int added = result.Layers.Count;
            foreach (CollageLayer layer in result.Layers) _layers.Add(layer);
            result.Layers.Clear();
            if (added > 0) _canvas.SelectLayer(_layers.Count - 1);
            _canvas.InvalidateScene();
            RefreshLayerList();
            _status.Text = added > 0 ? "已添加 " + added + " 张图片。拖动图片自由摆放，拖拽角点缩放。" : "没有添加图片。";
            if (result.Errors.Count > 0)
            {
                string details = String.Join("\n", result.Errors.ToArray());
                if (result.Errors.Count >= 10) details += "\n…其余错误已省略。";
                MessageBox.Show(this, "部分图片无法加载：\n" + details, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RemoveSelectedLayer()
        {
            int selected = _canvas.SelectedIndex;
            if (selected < 0 || selected >= _layers.Count) return;
            _layers[selected].Dispose();
            _layers.RemoveAt(selected);
            int next = Math.Min(selected, _layers.Count - 1);
            _canvas.SelectLayer(next);
            _canvas.InvalidateScene();
            RefreshLayerList();
            _status.Text = "已删除选中图片。";
        }

        private void MoveSelectedLayer(int direction)
        {
            int selected = _canvas.SelectedIndex;
            int target = direction > 0 ? _layers.Count - 1 : 0;
            if (selected < 0 || target < 0 || target >= _layers.Count) return;
            CollageLayer layer = _layers[selected];
            _layers.RemoveAt(selected);
            _layers.Insert(target, layer);
            _canvas.SelectLayer(target);
            _canvas.InvalidateScene();
            RefreshLayerList();
            _status.Text = direction > 0 ? "图片已置于更上层。" : "图片已置于更下层。";
        }

        private void RefreshLayerList()
        {
            if (_layerList == null) return;
            _updatingLayerList = true;
            _layerList.BeginUpdate();
            _layerList.Items.Clear();
            for (int i = 0; i < _layers.Count; i++)
                _layerList.Items.Add((i + 1).ToString("D2") + "  " + System.IO.Path.GetFileName(_layers[i].Path));
            if (_canvas.SelectedIndex >= 0 && _canvas.SelectedIndex < _layerList.Items.Count) _layerList.SelectedIndex = _canvas.SelectedIndex;
            _layerList.EndUpdate();
            _updatingLayerList = false;
            UpdateSelectedLayerControls();
        }

        private void CanvasSelectionChanged(object sender, EventArgs e)
        {
            RefreshLayerList();
        }

        private void CanvasBoundsChanged(object sender, EventArgs e)
        {
            UpdateSelectedLayerControls();
        }

        private void LayerListSelectionChanged(object sender, EventArgs e)
        {
            if (!_updatingLayerList) _canvas.SelectLayer(_layerList.SelectedIndex);
        }

        private void UpdateSelectedLayerControls()
        {
            int selected = _canvas.SelectedIndex;
            bool hasLayer = selected >= 0 && selected < _layers.Count;
            _selectedLayerName.Text = hasLayer ? System.IO.Path.GetFileName(_layers[selected].Path) : "未选择图片";
            _layerWidth.Enabled = hasLayer;
            _layerHeight.Enabled = hasLayer;
            _keepLayerRatio.Enabled = hasLayer;
            if (!hasLayer) return;
            RectangleF bounds = _layers[selected].Bounds;
            _updatingLayerSize = true;
            _layerWidth.Value = ClampToControl(bounds.Width, _layerWidth);
            _layerHeight.Value = ClampToControl(bounds.Height, _layerHeight);
            _updatingLayerSize = false;
        }

        private static decimal ClampToControl(float value, NumericUpDown control)
        {
            decimal result = (decimal)Math.Max((double)control.Minimum, Math.Min((double)control.Maximum, Math.Round(value)));
            return result;
        }

        private void LayerDimensionsChanged(object sender, EventArgs e)
        {
            if (_updatingLayerSize) return;
            int selected = _canvas.SelectedIndex;
            if (selected < 0 || selected >= _layers.Count) return;
            CollageLayer layer = _layers[selected];
            float width = (float)_layerWidth.Value;
            float height = (float)_layerHeight.Value;
            if (_keepLayerRatio.Checked)
            {
                float ratio = layer.Bounds.Width / Math.Max(1f, layer.Bounds.Height);
                if (sender == _layerWidth) height = width / ratio;
                else width = height * ratio;
                _updatingLayerSize = true;
                if (sender == _layerWidth) _layerHeight.Value = ClampToControl(height, _layerHeight);
                else _layerWidth.Value = ClampToControl(width, _layerWidth);
                _updatingLayerSize = false;
            }
            RectangleF bounds = layer.Bounds;
            bounds.Width = Math.Max(1f, width);
            bounds.Height = Math.Max(1f, height);
            layer.Bounds = bounds;
            _canvas.InvalidateScene();
        }

        private void RatioChanged(object sender, EventArgs e)
        {
            if (_updatingCanvasSize || _ratio.SelectedIndex < 0 || _ratio.SelectedIndex == _ratio.Items.Count - 1) return;
            string[] parts = _ratio.SelectedItem.ToString().Split(':');
            int rw = Int32.Parse(parts[0]);
            int rh = Int32.Parse(parts[1]);
            int edge = 2400;
            int width = rw >= rh ? edge : Math.Max(64, (int)Math.Round(edge * (double)rw / rh));
            int height = rh > rw ? edge : Math.Max(64, (int)Math.Round(edge * (double)rh / rw));
            SetCanvasSize(width, height);
        }

        private void CanvasDimensionsChanged(object sender, EventArgs e)
        {
            if (_updatingCanvasSize) return;
            _updatingCanvasSize = true;
            _ratio.SelectedIndex = _ratio.Items.Count - 1;
            _updatingCanvasSize = false;
            _canvas.SetCanvasSize((int)_canvasWidth.Value, (int)_canvasHeight.Value);
        }

        private void SetCanvasSize(int width, int height)
        {
            _updatingCanvasSize = true;
            _canvasWidth.Value = Math.Max((int)_canvasWidth.Minimum, Math.Min((int)_canvasWidth.Maximum, width));
            _canvasHeight.Value = Math.Max((int)_canvasHeight.Minimum, Math.Min((int)_canvasHeight.Maximum, height));
            _updatingCanvasSize = false;
            _canvas.SetCanvasSize((int)_canvasWidth.Value, (int)_canvasHeight.Value);
        }

        private Color GetCanvasBackground()
        {
            if (_background.SelectedIndex == 1) return Color.FromArgb(238, 238, 238);
            if (_background.SelectedIndex == 2) return Color.FromArgb(32, 34, 38);
            if (_background.SelectedIndex == 3) return Color.Transparent;
            if (_background.SelectedIndex == 4) return _customBackground;
            return Color.White;
        }

        private void BackgroundChanged(object sender, EventArgs e)
        {
            if (_background.SelectedIndex == 4)
            {
                using (ColorDialog dialog = new ColorDialog { Color = _customBackground, FullOpen = true })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK) _customBackground = dialog.Color;
                    else _background.SelectedIndex = 0;
                }
            }
            _canvas.CanvasBackground = GetCanvasBackground();
            _canvas.InvalidateScene();
        }

        private void GuideSettingsChanged(object sender, EventArgs e)
        {
            ApplyGuideSettings();
        }

        private void ApplyGuideSettings()
        {
            if (_guideGrid.SelectedIndex < 0) return;
            if (_guideGrid.SelectedIndex == 0) { _canvas.GuideColumns = 2; _canvas.GuideRows = 2; }
            else if (_guideGrid.SelectedIndex == 1) { _canvas.GuideColumns = 3; _canvas.GuideRows = 2; }
            else { _canvas.GuideColumns = 3; _canvas.GuideRows = 3; }
            _canvas.ShowGridGuides = _showGridGuides.Checked;
            _canvas.SnapToCanvas = _snapToCanvas.Checked;
            _canvas.Invalidate();
        }

        private void UpdateJpegQualityVisibility()
        {
            bool jpeg = String.Equals(_format.SelectedItem as string, "JPEG", StringComparison.OrdinalIgnoreCase);
            _qualityLabel.Visible = jpeg;
            _jpegQuality.Visible = jpeg;
        }

        private void ExportCollage(object sender, EventArgs e)
        {
            if (_exportWorker.IsBusy)
            {
                _status.Text = "正在导出，请稍候…";
                return;
            }
            if (_layers.Count == 0)
            {
                MessageBox.Show(this, "请先添加图片，再导出拼图。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool jpeg = String.Equals(_format.SelectedItem as string, "JPEG", StringComparison.OrdinalIgnoreCase);
            using (SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "导出拼图",
                Filter = jpeg ? "JPEG 图片|*.jpg" : "PNG 图片|*.png",
                DefaultExt = jpeg ? "jpg" : "png",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "自由拼图_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if ((long)_canvas.CanvasWidth * _canvas.CanvasHeight > 40000000L)
                {
                    MessageBox.Show(this, "画布尺寸过大，导出上限为 4000 万像素。请调小画布尺寸后重试。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ExportLayer[] layers = new ExportLayer[_layers.Count];
                for (int i = 0; i < _layers.Count; i++) layers[i] = new ExportLayer(_layers[i].Path, _layers[i].Bounds);
                ExportRequest request = new ExportRequest(dialog.FileName, _canvas.CanvasWidth, _canvas.CanvasHeight, layers, _canvas.CanvasBackground, jpeg, GetJpegQuality());
                _addButton.Enabled = false;
                _exportButton.Enabled = false;
                _status.Text = "正在后台以原图质量合成…";
                _exportWorker.RunWorkerAsync(request);
            }
        }

        private void ExportWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            ExportRequest request = (ExportRequest)e.Argument;
            using (Bitmap output = new Bitmap(request.Width, request.Height, PixelFormat.Format32bppArgb))
            {
                output.SetResolution(96f, 96f);
                using (Graphics graphics = Graphics.FromImage(output))
                {
                    Color background = request.Jpeg && request.Background.A == 0 ? Color.White : request.Background;
                    graphics.Clear(background);
                    ConfigureExportQuality(graphics);
                    for (int i = 0; i < request.Layers.Length; i++)
                    {
                        if (worker.CancellationPending) { e.Cancel = true; return; }
                        ExportLayer layer = request.Layers[i];
                        using (DecodedImage decoded = ImageDecoder.Load(layer.Path))
                        {
                            RectangleF clipped = RectangleF.Intersect(layer.Bounds, new RectangleF(0f, 0f, request.Width, request.Height));
                            if (clipped.Width > 0f && clipped.Height > 0f)
                            {
                                GraphicsState state = graphics.Save();
                                graphics.SetClip(new RectangleF(0f, 0f, request.Width, request.Height));
                                graphics.DrawImage(decoded.Image, layer.Bounds);
                                graphics.Restore(state);
                            }
                        }
                        worker.ReportProgress((i + 1) * 100 / request.Layers.Length, new ExportProgress(i + 1, request.Layers.Length));
                    }
                }
                if (worker.CancellationPending) { e.Cancel = true; return; }
                PhotoEditorForm.SaveBitmap(output, request.Path, request.Jpeg ? request.JpegQuality : 90L);
            }
            e.Result = request.Path;
        }

        private static void ConfigureExportQuality(Graphics graphics)
        {
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
        }

        private void ExportProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (_closing || IsDisposed) return;
            ExportProgress progress = e.UserState as ExportProgress;
            if (progress != null) _status.Text = "正在合成原图… " + progress.Processed + " / " + progress.Total;
        }

        private void ExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_closing || IsDisposed) return;
            _addButton.Enabled = true;
            _exportButton.Enabled = true;
            if (e.Error != null)
            {
                _status.Text = "导出失败。";
                MessageBox.Show(this, "导出失败：\n" + e.Error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!e.Cancelled)
            {
                OutputPath = (string)e.Result;
                _status.Text = "已导出：" + OutputPath;
            }
        }

        private long GetJpegQuality()
        {
            if (_jpegQuality.SelectedIndex == 0) return 75L;
            if (_jpegQuality.SelectedIndex == 2) return 98L;
            return 90L;
        }

        private sealed class ImportRequest
        {
            public readonly string[] Paths;
            public readonly int CanvasWidth;
            public readonly int CanvasHeight;
            public readonly int StartCount;
            public ImportRequest(string[] paths, int canvasWidth, int canvasHeight, int startCount) { Paths = paths; CanvasWidth = canvasWidth; CanvasHeight = canvasHeight; StartCount = startCount; }
        }

        private sealed class ImportResult : IDisposable
        {
            public readonly List<CollageLayer> Layers = new List<CollageLayer>();
            public readonly List<string> Errors = new List<string>();
            public void Dispose() { foreach (CollageLayer layer in Layers) layer.Dispose(); Layers.Clear(); }
        }

        private sealed class ImportProgress
        {
            public readonly int Processed;
            public readonly int Total;
            public ImportProgress(int processed, int total) { Processed = processed; Total = total; }
        }

        private sealed class ExportLayer
        {
            public readonly string Path;
            public readonly RectangleF Bounds;
            public ExportLayer(string path, RectangleF bounds) { Path = path; Bounds = bounds; }
        }

        private sealed class ExportRequest
        {
            public readonly string Path;
            public readonly int Width;
            public readonly int Height;
            public readonly ExportLayer[] Layers;
            public readonly Color Background;
            public readonly bool Jpeg;
            public readonly long JpegQuality;
            public ExportRequest(string path, int width, int height, ExportLayer[] layers, Color background, bool jpeg, long jpegQuality) { Path = path; Width = width; Height = height; Layers = layers; Background = background; Jpeg = jpeg; JpegQuality = jpegQuality; }
        }

        private sealed class ExportProgress
        {
            public readonly int Processed;
            public readonly int Total;
            public ExportProgress(int processed, int total) { Processed = processed; Total = total; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_loadWorker != null && _loadWorker.IsBusy) _loadWorker.CancelAsync();
                if (_exportWorker != null && _exportWorker.IsBusy) _exportWorker.CancelAsync();
                if (_canvas != null) _canvas.DisposeSceneCache();
                foreach (CollageLayer layer in _layers) layer.Dispose();
                _layers.Clear();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class CollageCanvas : Control
    {
        private readonly IList<CollageLayer> _layers;
        private int _selectedIndex = -1;
        private int _canvasWidth = 1920;
        private int _canvasHeight = 1080;
        private float _displayScale = 1f;
        private RectangleF _displayCanvas;
        private bool _dragging;
        private bool _resizing;
        private int _resizeHandle = -1;
        private PointF _dragStart;
        private RectangleF _boundsStart;
        private PointF _resizeAnchor;
        private float _resizeSignX;
        private float _resizeSignY;
        private Bitmap _backCache;
        private Bitmap _frontCache;
        private bool _sceneCacheDirty = true;
        private int _cacheWidth;
        private int _cacheHeight;
        private float _cacheScale;
        private const float HandleSize = 9f;

        public event EventHandler SelectionChanged;
        public event EventHandler BoundsChanged;
        public int SelectedIndex { get { return _selectedIndex; } }
        public int CanvasWidth { get { return _canvasWidth; } }
        public int CanvasHeight { get { return _canvasHeight; } }
        public Color CanvasBackground { get; set; }
        public bool ShowGridGuides { get; set; }
        public int GuideColumns { get; set; }
        public int GuideRows { get; set; }
        public bool SnapToCanvas { get; set; }

        public CollageCanvas(IList<CollageLayer> layers)
        {
            _layers = layers;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(43, 47, 53);
            TabStop = true;
            CanvasBackground = Color.White;
            GuideColumns = 3;
            GuideRows = 3;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        public void SetCanvasSize(int width, int height)
        {
            _canvasWidth = Math.Max(1, width);
            _canvasHeight = Math.Max(1, height);
            InvalidateScene();
        }

        public void InvalidateScene()
        {
            _sceneCacheDirty = true;
            Invalidate();
        }

        public void DisposeSceneCache()
        {
            if (_backCache != null) _backCache.Dispose();
            if (_frontCache != null) _frontCache.Dispose();
            _backCache = null;
            _frontCache = null;
            _sceneCacheDirty = true;
        }

        public void SelectLayer(int index)
        {
            int next = index >= 0 && index < _layers.Count ? index : -1;
            if (_selectedIndex == next) { Invalidate(); return; }
            _selectedIndex = next;
            InvalidateScene();
            EventHandler handler = SelectionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(BackColor);
            float availableWidth = Math.Max(1f, ClientSize.Width - 64f);
            float availableHeight = Math.Max(1f, ClientSize.Height - 64f);
            _displayScale = Math.Min(availableWidth / _canvasWidth, availableHeight / _canvasHeight);
            _displayScale = Math.Max(0.01f, _displayScale);
            _displayCanvas = new RectangleF((ClientSize.Width - _canvasWidth * _displayScale) / 2f, (ClientSize.Height - _canvasHeight * _displayScale) / 2f, _canvasWidth * _displayScale, _canvasHeight * _displayScale);
            Rectangle shadow = Rectangle.Round(_displayCanvas);
            shadow.Offset(5, 7);
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(65, 0, 0, 0))) graphics.FillRectangle(shadowBrush, shadow);
            using (SolidBrush canvasBrush = new SolidBrush(CanvasBackground)) graphics.FillRectangle(canvasBrush, _displayCanvas);
            if (CanvasBackground.A == 0) DrawCheckerboard(graphics, _displayCanvas);

            GraphicsState state = graphics.Save();
            graphics.SetClip(_displayCanvas);
            graphics.TranslateTransform(_displayCanvas.Left, _displayCanvas.Top);
            graphics.ScaleTransform(_displayScale, _displayScale);
            EnsureSceneCache();
            graphics.InterpolationMode = InterpolationMode.Bilinear;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            if (_backCache != null) graphics.DrawImage(_backCache, new RectangleF(0f, 0f, _canvasWidth, _canvasHeight));
            if (_selectedIndex >= 0 && _selectedIndex < _layers.Count)
            {
                CollageLayer selected = _layers[_selectedIndex];
                if (selected.Preview != null && RectangleF.Intersect(selected.Bounds, new RectangleF(0f, 0f, _canvasWidth, _canvasHeight)).Width > 0f)
                    graphics.DrawImage(selected.Preview, selected.Bounds);
            }
            if (_frontCache != null) graphics.DrawImage(_frontCache, new RectangleF(0f, 0f, _canvasWidth, _canvasHeight));
            graphics.Restore(state);
            DrawGridGuides(graphics);

            using (Pen border = new Pen(Color.FromArgb(180, 255, 255, 255), 1f)) graphics.DrawRectangle(border, _displayCanvas.X, _displayCanvas.Y, _displayCanvas.Width, _displayCanvas.Height);
            if (_layers.Count == 0)
            {
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(210, 238, 238, 238)))
                using (Font font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular))
                {
                    string text = "点击“添加图片”，然后在画布上拖动和缩放";
                    SizeF size = graphics.MeasureString(text, font);
                    graphics.DrawString(text, font, textBrush, _displayCanvas.Left + (_displayCanvas.Width - size.Width) / 2f, _displayCanvas.Top + (_displayCanvas.Height - size.Height) / 2f);
                }
            }
            DrawSelection(graphics);
        }

        private void EnsureSceneCache()
        {
            if (!_sceneCacheDirty) return;
            DisposeSceneCache();
            _cacheScale = Math.Min(1f, 1600f / Math.Max(_canvasWidth, _canvasHeight));
            _cacheWidth = Math.Max(1, (int)Math.Round(_canvasWidth * _cacheScale));
            _cacheHeight = Math.Max(1, (int)Math.Round(_canvasHeight * _cacheScale));
            if (_layers.Count == 0) { _sceneCacheDirty = false; return; }

            _backCache = CreateCompositeCache(0, _selectedIndex < 0 ? _layers.Count : _selectedIndex, true);
            if (_selectedIndex >= 0 && _selectedIndex + 1 < _layers.Count)
                _frontCache = CreateCompositeCache(_selectedIndex + 1, _layers.Count, false);
            _sceneCacheDirty = false;
        }

        private Bitmap CreateCompositeCache(int startIndex, int endIndex, bool includeCanvasBackground)
        {
            Bitmap bitmap = new Bitmap(_cacheWidth, _cacheHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(includeCanvasBackground ? CanvasBackground : Color.Transparent);
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Bilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                RectangleF canvasBounds = new RectangleF(0f, 0f, _canvasWidth, _canvasHeight);
                for (int i = startIndex; i < endIndex; i++)
                {
                    CollageLayer layer = _layers[i];
                    if (layer.Preview == null || RectangleF.Intersect(layer.Bounds, canvasBounds).IsEmpty) continue;
                    RectangleF scaledBounds = new RectangleF(layer.Bounds.X * _cacheScale, layer.Bounds.Y * _cacheScale, layer.Bounds.Width * _cacheScale, layer.Bounds.Height * _cacheScale);
                    graphics.DrawImage(layer.Preview, scaledBounds);
                }
            }
            return bitmap;
        }

        private void DrawGridGuides(Graphics graphics)
        {
            if (!ShowGridGuides || GuideColumns < 2 || GuideRows < 2) return;
            GraphicsState state = graphics.Save();
            graphics.SetClip(_displayCanvas);
            using (Pen guide = new Pen(Color.FromArgb(205, 0, 117, 190), 1.5f))
            {
                guide.DashStyle = DashStyle.Dash;
                for (int column = 1; column < GuideColumns; column++)
                {
                    float x = _displayCanvas.Left + _displayCanvas.Width * column / GuideColumns;
                    graphics.DrawLine(guide, x, _displayCanvas.Top, x, _displayCanvas.Bottom);
                }
                for (int row = 1; row < GuideRows; row++)
                {
                    float y = _displayCanvas.Top + _displayCanvas.Height * row / GuideRows;
                    graphics.DrawLine(guide, _displayCanvas.Left, y, _displayCanvas.Right, y);
                }
            }
            graphics.Restore(state);
        }

        private RectangleF ApplyBoundarySnap(RectangleF bounds)
        {
            if (!SnapToCanvas) return bounds;
            float threshold = 12f / Math.Max(0.01f, _displayScale);
            if (bounds.Width <= _canvasWidth)
            {
                if (Math.Abs(bounds.Left) <= threshold) bounds.X = 0f;
                else if (Math.Abs(bounds.Right - _canvasWidth) <= threshold) bounds.X = _canvasWidth - bounds.Width;
            }
            if (bounds.Height <= _canvasHeight)
            {
                if (Math.Abs(bounds.Top) <= threshold) bounds.Y = 0f;
                else if (Math.Abs(bounds.Bottom - _canvasHeight) <= threshold) bounds.Y = _canvasHeight - bounds.Height;
            }
            return bounds;
        }

        private void DrawCheckerboard(Graphics graphics, RectangleF rect)
        {
            float tile = 18f;
            using (SolidBrush light = new SolidBrush(Color.White))
            using (SolidBrush dark = new SolidBrush(Color.FromArgb(224, 226, 230)))
            {
                graphics.FillRectangle(light, rect);
                for (float y = rect.Top; y < rect.Bottom; y += tile)
                    for (float x = rect.Left; x < rect.Right; x += tile)
                        if ((((int)Math.Floor((x - rect.Left) / tile)) + ((int)Math.Floor((y - rect.Top) / tile))) % 2 == 0)
                            graphics.FillRectangle(dark, x, y, Math.Min(tile, rect.Right - x), Math.Min(tile, rect.Bottom - y));
            }
        }

        private void DrawSelection(Graphics graphics)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _layers.Count) return;
            RectangleF bounds = ToScreen(_layers[_selectedIndex].Bounds);
            using (Pen outline = new Pen(Color.FromArgb(238, 82, 76), 1.5f))
            {
                outline.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(outline, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
            PointF[] points = GetHandlePoints(bounds);
            for (int i = 0; i < points.Length; i++)
            {
                RectangleF handle = new RectangleF(points[i].X - HandleSize / 2f, points[i].Y - HandleSize / 2f, HandleSize, HandleSize);
                using (SolidBrush brush = new SolidBrush(Color.White)) graphics.FillRectangle(brush, handle);
                using (Pen pen = new Pen(Color.FromArgb(220, 82, 76), 1.4f)) graphics.DrawRectangle(pen, handle.X, handle.Y, handle.Width, handle.Height);
            }
        }

        private static PointF[] GetHandlePoints(RectangleF rect)
        {
            return new PointF[]
            {
                new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)
            };
        }

        private RectangleF ToScreen(RectangleF bounds)
        {
            return new RectangleF(_displayCanvas.Left + bounds.X * _displayScale, _displayCanvas.Top + bounds.Y * _displayScale, bounds.Width * _displayScale, bounds.Height * _displayScale);
        }

        private PointF ToCanvas(Point point)
        {
            return new PointF((point.X - _displayCanvas.Left) / _displayScale, (point.Y - _displayCanvas.Top) / _displayScale);
        }

        private int HitHandle(Point point)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _layers.Count) return -1;
            PointF[] handles = GetHandlePoints(ToScreen(_layers[_selectedIndex].Bounds));
            for (int i = 0; i < handles.Length; i++)
                if (Math.Abs(point.X - handles[i].X) <= HandleSize && Math.Abs(point.Y - handles[i].Y) <= HandleSize) return i;
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !_displayCanvas.Contains(e.Location)) return;
            Focus();
            int handle = HitHandle(e.Location);
            if (handle >= 0)
            {
                _resizeHandle = handle;
                _resizing = true;
                _boundsStart = _layers[_selectedIndex].Bounds;
                _dragStart = ToCanvas(e.Location);
                _resizeSignX = (handle == 0 || handle == 3) ? -1f : 1f;
                _resizeSignY = (handle == 0 || handle == 1) ? -1f : 1f;
                _resizeAnchor = new PointF(_resizeSignX < 0 ? _boundsStart.Right : _boundsStart.Left, _resizeSignY < 0 ? _boundsStart.Bottom : _boundsStart.Top);
                Capture = true;
                return;
            }

            PointF point = ToCanvas(e.Location);
            int hit = -1;
            for (int i = _layers.Count - 1; i >= 0; i--)
                if (_layers[i].Bounds.Contains(point)) { hit = i; break; }
            SelectLayer(hit);
            if (hit >= 0)
            {
                _dragging = true;
                _dragStart = point;
                _boundsStart = _layers[hit].Bounds;
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_resizing && _selectedIndex >= 0 && _selectedIndex < _layers.Count)
            {
                PointF point = ToCanvas(e.Location);
                float initialWidth = _boundsStart.Width;
                float initialHeight = _boundsStart.Height;
                float initialX = _resizeSignX * initialWidth;
                float initialY = _resizeSignY * initialHeight;
                float currentX = point.X - _resizeAnchor.X;
                float currentY = point.Y - _resizeAnchor.Y;
                float scale = (currentX * initialX + currentY * initialY) / Math.Max(1f, initialWidth * initialWidth + initialHeight * initialHeight);
                scale = Math.Max(0.03f, Math.Min(20f, scale));
                float width = Math.Max(16f, initialWidth * scale);
                float height = Math.Max(16f, initialHeight * scale);
                float x = _resizeSignX < 0 ? _resizeAnchor.X - width : _resizeAnchor.X;
                float y = _resizeSignY < 0 ? _resizeAnchor.Y - height : _resizeAnchor.Y;
                _layers[_selectedIndex].Bounds = ApplyBoundarySnap(new RectangleF(x, y, width, height));
                Invalidate();
                return;
            }
            if (_dragging && _selectedIndex >= 0 && _selectedIndex < _layers.Count)
            {
                PointF point = ToCanvas(e.Location);
                RectangleF bounds = _boundsStart;
                bounds.X += point.X - _dragStart.X;
                bounds.Y += point.Y - _dragStart.Y;
                _layers[_selectedIndex].Bounds = ApplyBoundarySnap(bounds);
                Invalidate();
                return;
            }
            Cursor = HitHandle(e.Location) >= 0 ? Cursors.SizeNWSE : (HitLayer(e.Location) >= 0 ? Cursors.SizeAll : Cursors.Default);
        }

        private int HitLayer(Point point)
        {
            if (!_displayCanvas.Contains(point)) return -1;
            PointF canvasPoint = ToCanvas(point);
            for (int i = _layers.Count - 1; i >= 0; i--)
                if (_layers[i].Bounds.Contains(canvasPoint)) return i;
            return -1;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = false;
                _resizing = false;
                _resizeHandle = -1;
                Capture = false;
                EventHandler handler = BoundsChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Point pointer = PointToClient(Cursor.Position);
            if (_displayCanvas.Contains(pointer) && _selectedIndex >= 0 && _selectedIndex < _layers.Count)
            {
                CollageLayer layer = _layers[_selectedIndex];
                RectangleF bounds = layer.Bounds;
                float minFactor = Math.Max(16f / Math.Max(1f, bounds.Width), 16f / Math.Max(1f, bounds.Height));
                float maxFactor = Math.Min(30000f / Math.Max(1f, bounds.Width), 30000f / Math.Max(1f, bounds.Height));
                float factor = (float)Math.Pow(1.1, e.Delta / 120.0);
                factor = Math.Max(minFactor, Math.Min(maxFactor, factor));
                float width = bounds.Width * factor;
                float height = bounds.Height * factor;
                float centerX = bounds.Left + bounds.Width / 2f;
                float centerY = bounds.Top + bounds.Height / 2f;
                bounds = new RectangleF(centerX - width / 2f, centerY - height / 2f, width, height);
                layer.Bounds = ApplyBoundarySnap(bounds);
                Invalidate();
                EventHandler changed = BoundsChanged;
                if (changed != null) changed(this, EventArgs.Empty);
                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null) handled.Handled = true;
            }
            base.OnMouseWheel(e);
        }
    }
}
