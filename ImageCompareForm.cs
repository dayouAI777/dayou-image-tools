using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace LocalPhotoViewer
{
    internal sealed class ImageCompareForm : Form
    {
        private readonly ImageCompareView _leftView;
        private readonly ImageCompareView _rightView;
        private readonly Label _leftZoom;
        private readonly Label _rightZoom;
        private readonly Label _leftName;
        private readonly Label _rightName;

        public ImageCompareForm()
        {
            Text = AppBranding.ProductName + " · 图片对比";
            AppBranding.ApplyWindowIcon(this);
            Width = 1320;
            Height = 820;
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterParent;

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PhotoEditorForm.Paper };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            FlowLayoutPanel heading = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(14, 9, 10, 5), BackColor = PhotoEditorForm.Paper };
            heading.Controls.Add(new Label { Text = "图片对比", AutoSize = true, Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy, Padding = new Padding(0, 5, 14, 0) });
            heading.Controls.Add(new Label { Text = "滚轮缩放，按住鼠标左键拖动画面；左右视图互不影响。", AutoSize = true, ForeColor = PhotoEditorForm.MutedInk, Padding = new Padding(0, 9, 0, 0) });
            root.Controls.Add(heading, 0, 0);

            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1320, 760), Orientation = Orientation.Vertical, SplitterWidth = 6, SplitterDistance = 650, BackColor = PhotoEditorForm.PaperShade };
            split.Panel1MinSize = 390;
            split.Panel2MinSize = 390;
            Shown += delegate { if (split.Width > 850) split.SplitterDistance = split.Width / 2; };

            _leftView = new ImageCompareView();
            _rightView = new ImageCompareView();
            _leftZoom = new Label { Text = "适应", AutoSize = true, ForeColor = PhotoEditorForm.Navy, Padding = new Padding(3, 8, 3, 0) };
            _rightZoom = new Label { Text = "适应", AutoSize = true, ForeColor = PhotoEditorForm.Navy, Padding = new Padding(3, 8, 3, 0) };
            _leftName = new Label { Text = "未添加图片", AutoSize = false, Width = 118, Height = 30, AutoEllipsis = true, ForeColor = PhotoEditorForm.MutedInk, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
            _rightName = new Label { Text = "未添加图片", AutoSize = false, Width = 118, Height = 30, AutoEllipsis = true, ForeColor = PhotoEditorForm.MutedInk, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
            split.Panel1.Controls.Add(CreatePane("左图", _leftView, _leftZoom, _leftName, OpenLeftImage));
            split.Panel2.Controls.Add(CreatePane("右图", _rightView, _rightZoom, _rightName, OpenRightImage));
            _leftView.ViewChanged += delegate { UpdatePaneLabels(); };
            _rightView.ViewChanged += delegate { UpdatePaneLabels(); };
            root.Controls.Add(split, 0, 1);
            Controls.Add(root);
        }

        private Control CreatePane(string title, ImageCompareView view, Label zoomLabel, Label nameLabel, EventHandler addHandler)
        {
            TableLayoutPanel pane = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PhotoEditorForm.PaperShade, Padding = new Padding(3) };
            pane.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            pane.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            FlowLayoutPanel tools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(3, 4, 2, 2), BackColor = PhotoEditorForm.Paper };
            tools.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy, Padding = new Padding(1, 6, 3, 0) });
            tools.Controls.Add(MakeButton("添加", 54, addHandler));
            tools.Controls.Add(MakeButton("−", 32, delegate { view.ZoomBy(1.0 / 1.2); }));
            tools.Controls.Add(zoomLabel);
            tools.Controls.Add(MakeButton("＋", 32, delegate { view.ZoomBy(1.2); }));
            tools.Controls.Add(MakeButton("适应", 50, delegate { view.SetFit(); }));
            tools.Controls.Add(MakeButton("原始大小", 70, delegate { view.SetActualSize(); }));
            tools.Controls.Add(nameLabel);
            pane.Controls.Add(tools, 0, 0);
            view.Dock = DockStyle.Fill;
            pane.Controls.Add(view, 0, 1);
            return pane;
        }

        private static Button MakeButton(string text, int width, EventHandler handler)
        {
            Button button = new Button { Text = text, Width = width, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = PhotoEditorForm.Navy, Margin = new Padding(2, 0, 2, 0) };
            button.FlatAppearance.BorderColor = Color.FromArgb(202, 196, 184);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 228, 218);
            button.Click += handler;
            return button;
        }

        private void OpenLeftImage(object sender, EventArgs e)
        {
            OpenImage(_leftView, "选择左侧图片");
        }

        private void OpenRightImage(object sender, EventArgs e)
        {
            OpenImage(_rightView, "选择右侧图片");
        }

        private void OpenImage(ImageCompareView target, string title)
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Title = title, Filter = ImageDecoder.OpenFileFilter, Multiselect = false })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) target.LoadImage(dialog.FileName);
            }
        }

        private void UpdatePaneLabels()
        {
            if (_leftZoom != null)
            {
                _leftZoom.Text = _leftView.ZoomPercent;
                _leftName.Text = _leftView.DisplayName;
            }
            if (_rightZoom != null)
            {
                _rightZoom.Text = _rightView.ZoomPercent;
                _rightName.Text = _rightView.DisplayName;
            }
        }
    }

    internal sealed class ImageCompareView : Control
    {
        private readonly BackgroundWorker _loader;
        private Image _image;
        private DecodedImage _decoded;
        private string _imagePath;
        private string _status = "拖放图片到这里，或点击上方“添加”";
        private float _zoom = 1f;
        private PointF _pan;
        private bool _fitMode = true;
        private bool _panning;
        private Point _panStart;
        private PointF _panAtStart;
        private bool _closing;

        public event EventHandler ViewChanged;

        public string ZoomPercent { get { return _image == null ? "—" : Math.Round(_zoom * 100f).ToString("0") + "%"; } }
        public string DisplayName { get { return String.IsNullOrEmpty(_imagePath) ? (_loader.IsBusy ? "正在加载…" : "未添加图片") : Path.GetFileName(_imagePath); } }

        public ImageCompareView()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(38, 41, 47);
            TabStop = true;
            AllowDrop = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

            _loader = new BackgroundWorker { WorkerSupportsCancellation = true };
            _loader.DoWork += LoadWorker;
            _loader.RunWorkerCompleted += LoadCompleted;
            MouseEnter += delegate { Focus(); };
            DragEnter += ViewDragEnter;
            DragDrop += ViewDragDrop;
        }

        public void LoadImage(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || _loader.IsBusy) return;
            _status = "正在加载图片…";
            RaiseViewChanged();
            Invalidate();
            _loader.RunWorkerAsync(path);
        }

        private void LoadWorker(object sender, DoWorkEventArgs e)
        {
            string path = (string)e.Argument;
            DecodedImage decoded = ImageDecoder.Load(path);
            e.Result = new LoadedImage(path, decoded);
        }

        private void LoadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) return;
            if (e.Error != null)
            {
                if (!_closing && !IsDisposed)
                {
                    _status = "加载失败：" + e.Error.Message;
                    RaiseViewChanged();
                    Invalidate();
                }
                return;
            }
            LoadedImage loaded = e.Result as LoadedImage;
            if (loaded == null) return;
            if (_closing || IsDisposed)
            {
                loaded.Dispose();
                return;
            }
            if (_decoded != null) _decoded.Dispose();
            _decoded = loaded.Decoded;
            _image = _decoded.Image;
            _imagePath = loaded.Path;
            loaded.Decoded = null;
            _pan = PointF.Empty;
            _fitMode = true;
            UpdateFitZoom();
            _status = String.Empty;
            RaiseViewChanged();
            Invalidate();
        }

        private void ViewDragEnter(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            e.Effect = paths != null && paths.Length > 0 && ImageDecoder.IsSupportedPath(paths[0]) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ViewDragDrop(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null && paths.Length > 0 && ImageDecoder.IsSupportedPath(paths[0])) LoadImage(paths[0]);
        }

        public void SetFit()
        {
            _fitMode = true;
            _pan = PointF.Empty;
            UpdateFitZoom();
            RaiseViewChanged();
            Invalidate();
        }

        public void SetActualSize()
        {
            if (_image == null) return;
            _fitMode = false;
            _zoom = 1f;
            _pan = PointF.Empty;
            RaiseViewChanged();
            Invalidate();
        }

        public void ZoomBy(double factor)
        {
            if (_image == null) return;
            _fitMode = false;
            ZoomAt(new Point(ClientSize.Width / 2, ClientSize.Height / 2), factor);
        }

        private void ZoomAt(Point point, double factor)
        {
            if (_image == null || factor <= 0) return;
            float oldZoom = _zoom;
            float newZoom = (float)Math.Max(0.01, Math.Min(32.0, oldZoom * factor));
            if (Math.Abs(newZoom - oldZoom) < 0.0001f) return;
            float relativeX = point.X - ClientSize.Width / 2f;
            float relativeY = point.Y - ClientSize.Height / 2f;
            _pan.X = relativeX - (relativeX - _pan.X) * newZoom / oldZoom;
            _pan.Y = relativeY - (relativeY - _pan.Y) * newZoom / oldZoom;
            _zoom = newZoom;
            _fitMode = false;
            RaiseViewChanged();
            Invalidate();
        }

        private void UpdateFitZoom()
        {
            if (_image == null) { _zoom = 1f; return; }
            float availableWidth = Math.Max(1f, ClientSize.Width - 32f);
            float availableHeight = Math.Max(1f, ClientSize.Height - 32f);
            _zoom = Math.Min(1f, Math.Min(availableWidth / _image.Width, availableHeight / _image.Height));
            _zoom = Math.Max(0.01f, _zoom);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_fitMode) UpdateFitZoom();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.Clear(BackColor);
            if (_image == null)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(210, 235, 238, 242)))
                using (Font font = new Font("Microsoft YaHei UI", 10f))
                {
                    string message = _loader.IsBusy ? "正在加载图片…" : (String.IsNullOrEmpty(_status) ? "拖放图片到这里，或点击上方“添加”" : _status);
                    SizeF size = graphics.MeasureString(message, font, Math.Max(120, ClientSize.Width - 40));
                    graphics.DrawString(message, font, brush, Math.Max(12f, (ClientSize.Width - size.Width) / 2f), Math.Max(12f, (ClientSize.Height - size.Height) / 2f));
                }
                return;
            }

            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float width = _image.Width * _zoom;
            float height = _image.Height * _zoom;
            RectangleF destination = new RectangleF((ClientSize.Width - width) / 2f + _pan.X, (ClientSize.Height - height) / 2f + _pan.Y, width, height);
            GraphicsState state = graphics.Save();
            graphics.SetClip(ClientRectangle);
            graphics.DrawImage(_image, destination);
            graphics.Restore(state);
            using (SolidBrush statusBack = new SolidBrush(Color.FromArgb(145, 0, 0, 0)))
            using (SolidBrush statusText = new SolidBrush(Color.White))
            using (Font font = new Font("Microsoft YaHei UI", 8.5f))
            {
                string info = Path.GetFileName(_imagePath) + "  ·  " + ZoomPercent;
                SizeF size = graphics.MeasureString(info, font);
                RectangleF rect = new RectangleF(8f, ClientSize.Height - 30f, size.Width + 18f, 22f);
                graphics.FillRectangle(statusBack, rect);
                graphics.DrawString(info, font, statusText, rect.X + 8f, rect.Y + 3f);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_image == null || e.Button != MouseButtons.Left) return;
            Focus();
            _panning = true;
            _panStart = e.Location;
            _panAtStart = _pan;
            Capture = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_panning) return;
            _pan = new PointF(_panAtStart.X + e.X - _panStart.X, _panAtStart.Y + e.Y - _panStart.Y);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _panning = false;
                Capture = false;
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Point point = PointToClient(Cursor.Position);
            if (_image != null && ClientRectangle.Contains(point))
            {
                ZoomAt(point, Math.Pow(1.15, e.Delta / 120.0));
                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null) handled.Handled = true;
            }
            base.OnMouseWheel(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closing = true;
                if (_loader != null && _loader.IsBusy) _loader.CancelAsync();
                if (_decoded != null) _decoded.Dispose();
                _decoded = null;
                _image = null;
            }
            base.Dispose(disposing);
        }

        private void RaiseViewChanged()
        {
            EventHandler handler = ViewChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private sealed class LoadedImage : IDisposable
        {
            public readonly string Path;
            public DecodedImage Decoded;
            public LoadedImage(string path, DecodedImage decoded) { Path = path; Decoded = decoded; }
            public void Dispose() { if (Decoded != null) Decoded.Dispose(); Decoded = null; }
        }
    }
}
