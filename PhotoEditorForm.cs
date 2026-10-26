using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LocalPhotoViewer
{
    internal enum EditorTool { View, Crop, Brush, Text, Arrow, Rectangle, Mosaic }
    internal enum BrushTipShape { Round, Square }

    internal sealed class PhotoEditorForm : Form
    {
        internal static readonly Color Paper = Color.FromArgb(248, 243, 231);
        internal static readonly Color PaperShade = Color.FromArgb(239, 231, 212);
        internal static readonly Color Navy = Color.FromArgb(24, 43, 65);
        internal static readonly Color NavySoft = Color.FromArgb(42, 63, 83);
        internal static readonly Color Coral = Color.FromArgb(220, 82, 76);
        internal static readonly Color Mint = Color.FromArgb(111, 190, 176);
        internal static readonly Color MutedInk = Color.FromArgb(93, 104, 112);
        private readonly string _sourcePath;
        private readonly PhotoEditCanvas _canvas;
        private readonly Bitmap _original;
        private Bitmap _image;
        private readonly List<Bitmap> _undo = new List<Bitmap>();
        private readonly List<Bitmap> _redo = new List<Bitmap>();
        private readonly TextBox _annotationText;
        private readonly ComboBox _cropRatio;
        private readonly NumericUpDown _cropWidth;
        private readonly NumericUpDown _cropHeight;
        private bool _updatingCropControls;
        private readonly NumericUpDown _brushSize;
        private readonly ComboBox _brushShape;
        private TabControl _sideTabs;
        private TabPage _cropPage;
        private TabPage _annotatePage;
        private TabPage _colorPage;
        private TabPage _infoPage;
        private Label _contextKicker;
        private Label _contextTitle;
        private Label _contextHint;
        private Label _annotationTextLabel;
        private Label _brushSizeLabel;
        private Label _brushShapeLabel;
        private Label _annotateHelp;
        private Label _brightnessLabel;
        private Label _contrastLabel;
        private Label _saturationLabel;
        private Button _drawColorButton;
        private Button _applyCropButton;
        private Button _cancelCropButton;
        private Button _applyWatermarkButton;
        private Button _applyColorButton;
        private Button _applyFilterButton;
        private ComboBox _filterChoice;
        private TrackBar _brightnessTrack;
        private TrackBar _contrastTrack;
        private TrackBar _saturationTrack;
        private bool _updatingSideTabs;
        private Color _drawColor = Color.Red;
        private readonly Label _status;
        private TextEditOverlay _textEditor;
        private bool _hasUnsavedChanges;

        public PhotoEditorForm(string sourcePath)
        {
            _sourcePath = sourcePath;
            Text = AppBranding.ProductName + " · 编辑 · " + Path.GetFileName(sourcePath);
            AppBranding.ApplyWindowIcon(this);
            Width = 1320;
            Height = 850;
            MinimumSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            _image = LoadBitmap(sourcePath);
            _original = (Bitmap)_image.Clone();
            _canvas = new PhotoEditCanvas { Dock = DockStyle.Fill, Image = _image, BackColor = Color.FromArgb(35, 37, 42) };
            _canvas.ToolCompleted += CanvasToolCompleted;
            _canvas.Resize += delegate { UpdateTextPreview(); };
            _canvas.TextPreviewMoved += delegate { PositionTextEditor(); };
            _canvas.TextPreviewMoveCompleted += delegate { PositionTextEditor(); if (_textEditor != null) _textEditor.FocusEditorEnd(); };
            FormClosing += EditorFormClosing;

            MenuStrip menu = BuildMenu();
            ToolStrip tools = BuildToolbar();
            _status = new Label { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), BackColor = Color.FromArgb(245, 246, 248), ForeColor = Color.FromArgb(70, 75, 84) };
            _annotationText = new TextBox { Text = "文字标注", Width = 120 };
            _cropRatio = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
            _cropRatio.Items.AddRange(new object[] { "自由比例", "原图比例", "1:1", "4:3", "3:2", "16:9", "9:16", "3:4", "2:3", "5:4", "4:5", "自定义输出尺寸" });
            _cropRatio.SelectedIndex = 0;
            _cropWidth = new NumericUpDown { Minimum = 0, Maximum = 30000, Value = 0, Width = 100, ThousandsSeparator = true };
            _cropHeight = new NumericUpDown { Minimum = 0, Maximum = 30000, Value = 0, Width = 100, ThousandsSeparator = true };
            _cropWidth.ValueChanged += CropOutputSizeChanged;
            _cropHeight.ValueChanged += CropOutputSizeChanged;
            _cropRatio.SelectedIndexChanged += delegate
            {
                if (_updatingCropControls) return;
                if (_cropRatio.SelectedIndex == _cropRatio.Items.Count - 1)
                {
                    if (_cropWidth.Value > 0 && _cropHeight.Value > 0)
                        _canvas.AspectRatio = (double)_cropWidth.Value / (double)_cropHeight.Value;
                    else
                        _canvas.AspectRatio = 0;
                    BeginCropSelection();
                    return;
                }
                _updatingCropControls = true;
                _cropWidth.Value = 0;
                _cropHeight.Value = 0;
                _updatingCropControls = false;
                double ratio = RatioValue(_cropRatio.SelectedIndex);
                _canvas.AspectRatio = ratio == -1 ? (double)_image.Width / _image.Height : ratio;
                BeginCropSelection();
            };
            _brushSize = new NumericUpDown { Minimum = 1, Maximum = 120, Value = 8, Width = 62 };
            _brushSize.ValueChanged += delegate { _canvas.BrushSize = (int)_brushSize.Value; };
            _brushShape = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 96 };
            _brushShape.Items.AddRange(new object[] { "圆形", "方形" });
            _brushShape.SelectedIndex = 0;
            _brushShape.SelectedIndexChanged += delegate { _canvas.BrushShape = _brushShape.SelectedIndex == 1 ? BrushTipShape.Square : BrushTipShape.Round; _canvas.Invalidate(); };

            Panel sidePanel = BuildSidePanel();
            SplitContainer split = new SplitContainer { Width = 1240, Height = 720, SplitterDistance = 940, FixedPanel = FixedPanel.Panel2, IsSplitterFixed = true, Panel1MinSize = 500, Panel2MinSize = 270, SplitterWidth = 2, BackColor = Mint, Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(_canvas);
            split.Panel1.BackColor = Navy;
            split.Panel2.BackColor = Paper;
            split.Panel2.Controls.Add(sidePanel);
            Controls.Add(split);
            Controls.Add(_status);
            Controls.Add(tools);
            Controls.Add(menu);
            MainMenuStrip = menu;
            BackColor = Paper;
            ForeColor = Navy;
            ApplyEditorialTheme(this);
            _status.BackColor = Navy;
            _status.ForeColor = Paper;
            _status.Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
            UpdateToolContext(EditorTool.View);
            UpdateStatus();
        }

        private MenuStrip BuildMenu()
        {
            MenuStrip menu = new MenuStrip();
            ToolStripMenuItem file = new ToolStripMenuItem("文件(&F)");
            file.DropDownItems.Add(Item("另存为副本…", Keys.Control | Keys.Shift | Keys.S, SaveCopy));
            file.DropDownItems.Add(Item("图片信息…", Keys.None, delegate { ShowInfo(this, EventArgs.Empty); }));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(Item("关闭编辑器", Keys.Escape, delegate { Close(); }));
            ToolStripMenuItem edit = new ToolStripMenuItem("编辑(&E)");
            edit.DropDownItems.Add(Item("撤销", Keys.Control | Keys.Z, Undo));
            edit.DropDownItems.Add(Item("重做", Keys.Control | Keys.Y, Redo));
            edit.DropDownItems.Add(Item("恢复原图", Keys.None, delegate { RestoreOriginal(this, EventArgs.Empty); }));
            ToolStripMenuItem image = new ToolStripMenuItem("图像(&I)");
            image.DropDownItems.Add(Item("向左旋转", Keys.None, delegate { Rotate(-90); }));
            image.DropDownItems.Add(Item("向右旋转", Keys.None, delegate { Rotate(90); }));
            image.DropDownItems.Add(Item("水平翻转", Keys.None, delegate { Flip(RotateFlipType.RotateNoneFlipX); }));
            image.DropDownItems.Add(Item("垂直翻转", Keys.None, delegate { Flip(RotateFlipType.RotateNoneFlipY); }));
            image.DropDownItems.Add(Item("改尺寸…", Keys.None, ResizeImage));
            image.DropDownItems.Add(Item("纠偏/旋正…", Keys.None, Straighten));
            image.DropDownItems.Add(Item("扩展画布…", Keys.None, ExtendCanvas));
            menu.Items.Add(file);
            menu.Items.Add(edit);
            menu.Items.Add(image);
            return menu;
        }

        private ToolStrip BuildToolbar()
        {
            ToolStrip bar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(18, 18), Padding = new Padding(8, 5, 8, 5), AutoSize = true };
            AddButton(bar, "撤销", Undo);
            AddButton(bar, "重做", Redo);
            bar.Items.Add(new ToolStripSeparator());
            AddButton(bar, "裁剪", delegate { SetTool(EditorTool.Crop); });
            AddButton(bar, "左旋", delegate { Rotate(-90); });
            AddButton(bar, "右旋", delegate { Rotate(90); });
            AddButton(bar, "翻转", delegate { Flip(RotateFlipType.RotateNoneFlipX); });
            AddButton(bar, "纠偏", Straighten);
            AddButton(bar, "改尺寸", ResizeImage);
            AddButton(bar, "扩展画布", ExtendCanvas);
            bar.Items.Add(new ToolStripSeparator());
            AddButton(bar, "画笔", delegate { SetTool(EditorTool.Brush); });
            AddButton(bar, "文字", delegate { SetTool(EditorTool.Text); });
            AddButton(bar, "箭头", delegate { SetTool(EditorTool.Arrow); });
            AddButton(bar, "框选", delegate { SetTool(EditorTool.Rectangle); });
            AddButton(bar, "马赛克", delegate { SetTool(EditorTool.Mosaic); });
            AddButton(bar, "文字水印", OpenTextWatermarkOptions);
            AddButton(bar, "图片水印", ApplyImageWatermark);
            AddButton(bar, "边框", AddBorder);
            bar.Items.Add(new ToolStripSeparator());
            AddButton(bar, "滤镜", OpenFilterOptions);
            AddButton(bar, "调色", OpenColorOptions);
            AddButton(bar, "锐化", delegate { ApplyConvolution(true); });
            AddButton(bar, "柔化", delegate { ApplyConvolution(false); });
            AddButton(bar, "另存副本", SaveCopy);
            return bar;
        }

        private Panel BuildSidePanel()
        {
            _sideTabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 5), DrawMode = TabDrawMode.OwnerDrawFixed, ItemSize = new Size(64, 36), SizeMode = TabSizeMode.Fixed, BackColor = Paper };
            _sideTabs.DrawItem += DrawEditorialTab;

            _cropPage = new TabPage("裁剪");
            AddSectionTitle(_cropPage, "裁剪比例", 14, 18);
            _cropRatio.Location = new Point(14, 48);
            _cropRatio.Width = 230;
            _cropPage.Controls.Add(_cropRatio);
            AddSectionTitle(_cropPage, "输出尺寸", 14, 104);
            AddLabel(_cropPage, "单位为像素。宽高都设为 0 时，保留裁剪框的实际尺寸。", 14, 130, 238, 48);
            _cropPage.Controls.Add(new Label { Text = "宽", Location = new Point(14, 188), Width = 20, Height = 26, TextAlign = ContentAlignment.MiddleCenter });
            _cropWidth.Location = new Point(36, 188);
            _cropWidth.Width = 82;
            _cropPage.Controls.Add(_cropWidth);
            _cropPage.Controls.Add(new Label { Text = "×", Location = new Point(120, 188), Width = 18, Height = 26, TextAlign = ContentAlignment.MiddleCenter });
            _cropPage.Controls.Add(new Label { Text = "高", Location = new Point(140, 188), Width = 20, Height = 26, TextAlign = ContentAlignment.MiddleCenter });
            _cropHeight.Location = new Point(162, 188);
            _cropHeight.Width = 82;
            _cropPage.Controls.Add(_cropHeight);
            AddLabel(_cropPage, "在图片上拖动裁剪框可调整构图；拖动角点可改变大小。", 14, 234, 238, 52);
            _applyCropButton = CreateActionButton("应用裁剪", 14, 310, 230, 38, true);
            _applyCropButton.Click += ApplyCrop;
            _cropPage.Controls.Add(_applyCropButton);
            _cancelCropButton = CreateActionButton("取消裁剪", 14, 356, 230, 36, false);
            _cancelCropButton.Click += CancelCrop;
            _cropPage.Controls.Add(_cancelCropButton);
            _sideTabs.TabPages.Add(_cropPage);

            _annotatePage = new TabPage("标注");
            _annotationTextLabel = AddSectionTitle(_annotatePage, "文字水印", 14, 18);
            _annotationText.Location = new Point(14, 48);
            _annotationText.Width = 230;
            _annotationText.Height = 28;
            _annotatePage.Controls.Add(_annotationText);
            _drawColorButton = CreateActionButton("选择颜色…", 14, 94, 112, 34, false);
            _drawColorButton.Click += delegate { ChooseColor(); };
            _annotatePage.Controls.Add(_drawColorButton);
            _brushSizeLabel = new Label { Text = "粗细", Location = new Point(134, 96), Width = 34, Height = 28, TextAlign = ContentAlignment.MiddleCenter };
            _annotatePage.Controls.Add(_brushSizeLabel);
            _brushSize.Location = new Point(168, 96);
            _annotatePage.Controls.Add(_brushSize);
            _brushShapeLabel = new Label { Text = "形状", Location = new Point(14, 142), Width = 42, Height = 28, TextAlign = ContentAlignment.MiddleCenter };
            _annotatePage.Controls.Add(_brushShapeLabel);
            _brushShape.Location = new Point(58, 142);
            _annotatePage.Controls.Add(_brushShape);
            _applyWatermarkButton = CreateActionButton("添加文字水印", 14, 194, 230, 38, true);
            _applyWatermarkButton.Click += ApplyTextWatermark;
            _annotatePage.Controls.Add(_applyWatermarkButton);
            _annotateHelp = new Label { Location = new Point(14, 252), Width = 238, Height = 92, AutoSize = false, ForeColor = MutedInk };
            _annotatePage.Controls.Add(_annotateHelp);
            _sideTabs.TabPages.Add(_annotatePage);

            _colorPage = new TabPage("色彩");
            _brightnessLabel = AddSectionTitle(_colorPage, "亮度", 14, 18);
            _brightnessTrack = CreateTrackBar(_colorPage, 14, 42, -100, 100, 0);
            _contrastLabel = AddSectionTitle(_colorPage, "对比度", 14, 104);
            _contrastTrack = CreateTrackBar(_colorPage, 14, 128, 0, 200, 100);
            _saturationLabel = AddSectionTitle(_colorPage, "饱和度", 14, 190);
            _saturationTrack = CreateTrackBar(_colorPage, 14, 214, 0, 200, 100);
            _applyColorButton = CreateActionButton("应用色彩调整", 14, 274, 230, 38, true);
            _applyColorButton.Click += delegate { AdjustColors(_brightnessTrack.Value, _contrastTrack.Value, _saturationTrack.Value); };
            _colorPage.Controls.Add(_applyColorButton);
            _filterChoice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(14, 48), Width = 230 };
            _filterChoice.Items.AddRange(new object[] { "黑白", "暖色", "冷色", "复古", "鲜艳", "低饱和" });
            _filterChoice.SelectedIndex = 0;
            _colorPage.Controls.Add(_filterChoice);
            _applyFilterButton = CreateActionButton("应用滤镜", 14, 104, 230, 38, true);
            _applyFilterButton.Click += ApplySelectedFilter;
            _colorPage.Controls.Add(_applyFilterButton);
            _filterChoice.Visible = false;
            _applyFilterButton.Visible = false;
            _sideTabs.TabPages.Add(_colorPage);

            _infoPage = new TabPage("信息");
            AddSectionTitle(_infoPage, "图片资料", 14, 18);
            AddLabel(_infoPage, "查看文件属性、尺寸与 EXIF 信息，或恢复刚打开时的原图。", 14, 48, 238, 52);
            Button details = CreateActionButton("查看文件属性与 EXIF", 14, 118, 230, 38, false);
            details.Click += delegate { ShowInfo(this, EventArgs.Empty); };
            _infoPage.Controls.Add(details);
            Button restore = CreateActionButton("恢复刚打开时的原图", 14, 164, 230, 38, false);
            restore.Click += delegate { RestoreOriginal(this, EventArgs.Empty); };
            _infoPage.Controls.Add(restore);
            _sideTabs.TabPages.Add(_infoPage);
            _sideTabs.SelectedIndexChanged += delegate
            {
                if (_updatingSideTabs) return;
                if (_sideTabs.SelectedTab == _cropPage) SetTool(EditorTool.Crop);
                else if (_sideTabs.SelectedTab == _annotatePage && _canvas.ActiveTool != EditorTool.Brush && _canvas.ActiveTool != EditorTool.Text && _canvas.ActiveTool != EditorTool.Arrow && _canvas.ActiveTool != EditorTool.Rectangle && _canvas.ActiveTool != EditorTool.Mosaic) SetTool(EditorTool.Brush);
                else if (_sideTabs.SelectedTab == _colorPage) OpenColorOptions(this, EventArgs.Empty);
                else if (_sideTabs.SelectedTab == _infoPage) SetTool(EditorTool.View);
            };

            EditorialSidebarHeader header = new EditorialSidebarHeader { Dock = DockStyle.Top, Height = 104, BackColor = Paper };
            _contextKicker = new Label { Text = "TOKYO / IMAGE STUDIO", Location = new Point(16, 12), Width = 235, Height = 18, Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold), ForeColor = Coral, BackColor = Color.Transparent };
            _contextTitle = new Label { Text = "图片信息", Location = new Point(16, 32), Width = 250, Height = 32, Font = new Font("Microsoft YaHei UI", 17f, FontStyle.Bold), ForeColor = Navy, BackColor = Color.Transparent };
            _contextHint = new Label { Text = "查看当前图片", Location = new Point(16, 69), Width = 245, Height = 20, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular), ForeColor = MutedInk, BackColor = Color.Transparent };
            header.Controls.Add(_contextKicker);
            header.Controls.Add(_contextTitle);
            header.Controls.Add(_contextHint);

            Panel container = new Panel { Dock = DockStyle.Fill, BackColor = Paper, Padding = new Padding(0) };
            container.Controls.Add(_sideTabs);
            container.Controls.Add(header);
            return container;
        }

        private Label AddSectionTitle(Control parent, string text, int x, int y)
        {
            Label label = new Label { Text = text, Location = new Point(x, y), Width = 230, Height = 24, Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold), ForeColor = Navy };
            parent.Controls.Add(label);
            return label;
        }

        private static Button CreateActionButton(string text, int x, int y, int width, int height, bool primary)
        {
            Button button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false, Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold), BackColor = primary ? Coral : Paper, ForeColor = primary ? Color.White : Navy, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = primary ? Coral : Mint;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(198, 66, 62) : PaperShade;
            return button;
        }

        private void DrawEditorialTab(object sender, DrawItemEventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs == null || e.Index < 0 || e.Index >= tabs.TabPages.Count) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle bounds = e.Bounds;
            using (Brush background = new SolidBrush(selected ? Paper : PaperShade))
                e.Graphics.FillRectangle(background, bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, new Font("Microsoft YaHei UI", 8.5f, selected ? FontStyle.Bold : FontStyle.Regular), bounds, selected ? Navy : MutedInk, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            if (selected)
                using (Pen accent = new Pen(Coral, 3f)) e.Graphics.DrawLine(accent, bounds.Left + 12, bounds.Bottom - 2, bounds.Right - 12, bounds.Bottom - 2);
        }

        internal static void ApplyEditorialTheme(Control root)
        {
            if (root == null) return;
            root.ForeColor = Navy;
            if (root is Form || root is Panel || root is TabPage) root.BackColor = Paper;
            if (root is Label)
            {
                Label label = (Label)root;
                if (label.ForeColor == SystemColors.ControlText) label.ForeColor = Navy;
                if (label.BackColor == SystemColors.Control) label.BackColor = Color.Transparent;
                if (label.Font.FontFamily.Name != "Microsoft YaHei UI") label.Font = new Font("Microsoft YaHei UI", label.Font.Size, label.Font.Style);
            }
            else if (root is TextBox)
            {
                TextBox textBox = (TextBox)root;
                textBox.BackColor = Color.FromArgb(255, 253, 248);
                textBox.ForeColor = Navy;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
            }
            else if (root is ComboBox)
            {
                ComboBox combo = (ComboBox)root;
                combo.BackColor = Color.FromArgb(255, 253, 248);
                combo.ForeColor = Navy;
                combo.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
            }
            else if (root is NumericUpDown)
            {
                NumericUpDown number = (NumericUpDown)root;
                number.BackColor = Color.FromArgb(255, 253, 248);
                number.ForeColor = Navy;
                number.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
            }
            else if (root is TrackBar)
                root.BackColor = Paper;
            else if (root is ListView)
            {
                ListView list = (ListView)root;
                list.BackColor = Color.FromArgb(255, 253, 248);
                list.ForeColor = Navy;
                list.Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
            }
            else if (root is Button)
            {
                Button button = (Button)root;
                button.FlatStyle = FlatStyle.Flat;
                button.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            }
            else if (root is PhotoEditCanvas)
                root.BackColor = Navy;

            if (root is ToolStrip)
            {
                ToolStrip strip = (ToolStrip)root;
                bool menu = strip is MenuStrip;
                strip.Renderer = new EditorialToolStripRenderer(Paper, PaperShade, Navy, Coral, Mint, menu);
                strip.BackColor = menu ? Paper : Navy;
                strip.ForeColor = menu ? Navy : Paper;
                foreach (ToolStripItem item in strip.Items)
                {
                    item.ForeColor = menu ? Navy : Paper;
                    if (item is ToolStripButton)
                    {
                        item.Padding = new Padding(7, 5, 7, 5);
                        item.Margin = new Padding(2, 1, 2, 1);
                        item.Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold);
                    }
                    ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                    if (menuItem != null) menuItem.DropDown.BackColor = Paper;
                }
            }
            if (root is SplitContainer)
            {
                SplitContainer split = (SplitContainer)root;
                split.BackColor = Mint;
                split.Panel1.BackColor = Navy;
                split.Panel2.BackColor = Paper;
            }
            foreach (Control child in root.Controls) ApplyEditorialTheme(child);
        }

        private void UpdateToolContext(EditorTool tool)
        {
            bool brush = tool == EditorTool.Brush;
            bool mosaic = tool == EditorTool.Mosaic;
            bool line = tool == EditorTool.Arrow || tool == EditorTool.Rectangle;
            bool text = tool == EditorTool.Text;
            _annotationTextLabel.Visible = false;
            _annotationText.Visible = false;
            _drawColorButton.Visible = brush || line;
            _brushSizeLabel.Visible = brush || mosaic || line;
            _brushSize.Visible = brush || mosaic || line;
            _brushShapeLabel.Visible = brush || mosaic;
            _brushShape.Visible = brush || mosaic;
            _applyWatermarkButton.Visible = false;
            _annotateHelp.Visible = true;

            if (tool == EditorTool.Crop)
                ShowContext(_cropPage, "FRAME / CROP", "裁剪", "设定比例与导出尺寸");
            else if (brush)
            {
                ShowContext(_annotatePage, "INK / BRUSH", "画笔", "选择笔尖外观与颜色");
                _annotateHelp.Text = "在图片上按住鼠标拖动即可绘制。笔迹会随当前颜色、粗细和圆形或方形笔尖实时预览。";
            }
            else if (mosaic)
            {
                ShowContext(_annotatePage, "PIXEL / MOSAIC", "马赛克", "调整涂抹笔尖");
                _annotateHelp.Text = "在图片上拖动即可即时涂抹马赛克。粗细决定涂抹范围，形状可选圆形或方形。";
            }
            else if (line)
            {
                ShowContext(_annotatePage, tool == EditorTool.Arrow ? "LINE / ARROW" : "FRAME / RECTANGLE", tool == EditorTool.Arrow ? "箭头" : "框选", "设置标注颜色与线宽");
                _annotateHelp.Text = tool == EditorTool.Arrow ? "在图片上按住并拖动，松开后放置箭头。" : "在图片上拖动即可绘制矩形框。";
            }
            else if (text)
            {
                ShowContext(_annotatePage, "TYPE / TEXT", "文字", "在图片上直接输入并预览");
                _annotateHelp.Text = "文字输入框会出现在图片上。可直接输入、拖动或缩放文字框，并在浮动工具条调整字体、字号与颜色。";
            }
            else
                ShowContext(_infoPage, "TOKYO / IMAGE STUDIO", "图片信息", "查看资料或恢复原图");
        }

        private void ShowContext(TabPage page, string kicker, string title, string hint)
        {
            if (_sideTabs != null && page != null && _sideTabs.SelectedTab != page)
            {
                _updatingSideTabs = true;
                _sideTabs.SelectedTab = page;
                _updatingSideTabs = false;
            }
            _contextKicker.Text = kicker;
            _contextTitle.Text = title;
            _contextHint.Text = hint;
        }

        private void OpenTextWatermarkOptions(object sender, EventArgs e)
        {
            if (_textEditor != null) CommitTextEditor();
            _canvas.ActiveTool = EditorTool.View;
            _canvas.ClearSelection();
            _annotationTextLabel.Text = "水印文字";
            _annotationTextLabel.Visible = true;
            _annotationText.Visible = true;
            _drawColorButton.Visible = true;
            _brushSizeLabel.Visible = false;
            _brushSize.Visible = false;
            _brushShapeLabel.Visible = false;
            _brushShape.Visible = false;
            _applyWatermarkButton.Visible = true;
            _annotateHelp.Visible = true;
            _annotateHelp.Text = "文字水印会放在图片右下方；颜色使用上方选定的标注颜色。";
            ShowContext(_annotatePage, "TYPE / WATERMARK", "文字水印", "输入内容后再应用到图片");
        }

        private void OpenColorOptions(object sender, EventArgs e)
        {
            if (_textEditor != null) CommitTextEditor();
            _canvas.ActiveTool = EditorTool.View;
            _canvas.ClearSelection();
            _brightnessLabel.Visible = _brightnessTrack.Visible = true;
            _contrastLabel.Visible = _contrastTrack.Visible = true;
            _saturationLabel.Visible = _saturationTrack.Visible = true;
            _applyColorButton.Visible = true;
            _filterChoice.Visible = _applyFilterButton.Visible = false;
            ShowContext(_colorPage, "COLOR / TONE", "调色", "微调明暗、对比与饱和度");
        }

        private void OpenFilterOptions(object sender, EventArgs e)
        {
            if (_textEditor != null) CommitTextEditor();
            _canvas.ActiveTool = EditorTool.View;
            _canvas.ClearSelection();
            _brightnessLabel.Visible = _brightnessTrack.Visible = false;
            _contrastLabel.Visible = _contrastTrack.Visible = false;
            _saturationLabel.Visible = _saturationTrack.Visible = false;
            _applyColorButton.Visible = false;
            _filterChoice.Visible = _applyFilterButton.Visible = true;
            ShowContext(_colorPage, "PRINT / FILTER", "滤镜", "选择适合图片的色彩预设");
        }

        private void ApplySelectedFilter(object sender, EventArgs e)
        {
            int mode = _filterChoice.SelectedIndex + 1;
            Commit(AdjustBitmap(_image, 0, mode == 5 ? 115 : 100, mode == 6 ? 45 : (mode == 5 ? 145 : 100), mode));
            _status.Text = "已应用“" + _filterChoice.SelectedItem + "”滤镜。";
        }

        private static TrackBar CreateTrackBar(Control parent, int x, int y, int min, int max, int value)
        {
            TrackBar bar = new TrackBar { Location = new Point(x, y), Width = 230, Minimum = min, Maximum = max, Value = value, TickFrequency = Math.Max(1, (max - min) / 10), SmallChange = 1, LargeChange = Math.Max(1, (max - min) / 10) };
            parent.Controls.Add(bar);
            return bar;
        }

        private static void AddLabel(Control parent, string text, int x = 14, int y = 14, int width = 235, int height = 64)
        {
            parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Width = width, Height = height, AutoSize = false });
        }

        private static ToolStripMenuItem Item(string text, Keys shortcut, EventHandler handler)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            if (shortcut != Keys.None && (shortcut & (Keys.Control | Keys.Alt)) != 0) item.ShortcutKeys = shortcut;
            item.Click += handler;
            return item;
        }

        private static void AddButton(ToolStrip bar, string text, EventHandler handler)
        {
            ToolStripButton button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text, AutoSize = true };
            button.Click += handler;
            bar.Items.Add(button);
        }

        private static double RatioValue(int index)
        {
            switch (index)
            {
                case 1: return -1;
                case 2: return 1;
                case 3: return 4.0 / 3.0;
                case 4: return 3.0 / 2.0;
                case 5: return 16.0 / 9.0;
                case 6: return 9.0 / 16.0;
                case 7: return 3.0 / 4.0;
                case 8: return 2.0 / 3.0;
                case 9: return 5.0 / 4.0;
                case 10: return 4.0 / 5.0;
                default: return 0;
            }
        }

        private void CropOutputSizeChanged(object sender, EventArgs e)
        {
            if (_updatingCropControls || _cropWidth.Value <= 0 || _cropHeight.Value <= 0) return;
            _updatingCropControls = true;
            _cropRatio.SelectedIndex = _cropRatio.Items.Count - 1;
            _updatingCropControls = false;
            _canvas.AspectRatio = (double)_cropWidth.Value / (double)_cropHeight.Value;
            BeginCropSelection();
        }

        private void BeginCropSelection()
        {
            if (_textEditor != null) CommitTextEditor();
            UpdateToolContext(EditorTool.Crop);
            _canvas.ActiveTool = EditorTool.Crop;
            _canvas.CreateCenteredSelection();
            _status.Text = "拖动裁剪框移动位置，拖动边缘或角点调整大小；框外拖动可重新框选。";
        }

        private void SetTool(EditorTool tool)
        {
            UpdateToolContext(tool);
            if (tool == EditorTool.Text)
            {
                StartTextEditor();
                return;
            }
            if (_textEditor != null) CommitTextEditor();
            _canvas.ActiveTool = tool;
            if (tool == EditorTool.Crop)
            {
                if (_canvas.SelectionInImage.IsEmpty) _canvas.CreateCenteredSelection();
                _status.Text = "拖动裁剪框移动位置，拖动边缘或角点调整大小；框外拖动可重新框选。";
            }
            else
            {
                _canvas.ClearSelection();
                _status.Text = tool == EditorTool.Mosaic ? "像画笔一样拖动图片进行马赛克涂抹；画笔大小可在“标注”里调整。" : "编辑工具：" + tool.ToString();
            }
        }

        private void CanvasToolCompleted(EditorTool tool, Point start, Point end, List<Point> points)
        {
            if (tool == EditorTool.Brush) DrawStroke(points, _drawColor, (int)_brushSize.Value, _canvas.BrushShape);
            else if (tool == EditorTool.Arrow) DrawArrow(start, end, _drawColor, (int)_brushSize.Value);
            else if (tool == EditorTool.Rectangle) DrawRectangle(start, end, _drawColor, (int)_brushSize.Value);
            else if (tool == EditorTool.Mosaic)
            {
                ApplyMosaic(points);
                _status.Text = "马赛克已涂抹；继续拖动可涂下一笔。";
                return;
            }
            UpdateStatus();
        }

        private void StartTextEditor()
        {
            if (_textEditor != null) CommitTextEditor();
            _canvas.ActiveTool = EditorTool.View;
            _canvas.ClearSelection();

            int width = Math.Max(320, Math.Min(520, _canvas.ClientSize.Width - 20));
            TextEditOverlay editor = new TextEditOverlay(width, _drawColor);
            _textEditor = editor;
            editor.ApplyRequested += delegate { CommitTextEditor(); };
            editor.CancelRequested += delegate { CancelTextEditor(); };
            editor.ColorRequested += delegate { ChooseTextColor(); };
            editor.TextStyleChanged += delegate { UpdateTextPreview(); };
            editor.GeometryChanged += delegate { _canvas.SetTextPreviewBounds(editor.PreviewBounds); };

            RectangleF imageBounds = _canvas.DisplayedImageBounds;
            float scale = Math.Max(0.01f, _canvas.DisplayScale);
            int previewWidth = Math.Max(1, Math.Min(_image.Width, (int)Math.Round(Math.Max(80, Math.Min(460, imageBounds.Width - 24)) / scale)));
            int previewHeight = Math.Max(1, Math.Min(_image.Height, (int)Math.Round(Math.Max(42, Math.Min(94, imageBounds.Height - 24)) / scale)));
            Rectangle previewBounds = new Rectangle((_image.Width - previewWidth) / 2, (_image.Height - previewHeight) / 2, previewWidth, previewHeight);
            _canvas.BeginTextPreview(previewBounds, editor.Editor.Text, editor.FontPixels, editor.FontFamilyName, editor.TextStyle, editor.TextColor, editor.ShadowEnabled);
            editor.SyncGeometry(previewBounds);
            _canvas.Controls.Add(editor);
            PositionTextEditor();
            editor.BringToFront();
            UpdateTextPreview();
            editor.FocusEditor();
            _status.Text = "文字实时预览；拖动虚线框移动、拖动控制点缩放，也可输入横X、纵Y、宽和高精确调整。";
        }

        private void UpdateTextPreview()
        {
            if (_textEditor == null) return;
            _canvas.UpdateTextPreview(_textEditor.Editor.Text, _textEditor.FontPixels, _textEditor.FontFamilyName, _textEditor.TextStyle, _textEditor.TextColor, _textEditor.ShadowEnabled);
            PositionTextEditor();
        }

        private void PositionTextEditor()
        {
            if (_textEditor == null) return;
            _textEditor.SyncGeometry(_canvas.TextPreviewBoundsInImage);
            RectangleF frame = _canvas.ImageRectangleToCanvas(_canvas.TextPreviewBoundsInImage);
            Rectangle canvasBounds = _canvas.ClientRectangle;
            int x = (int)Math.Round(frame.Left + (frame.Width - _textEditor.Width) / 2f);
            int y = (int)Math.Round(frame.Bottom + 7f);
            if (y + _textEditor.Height > canvasBounds.Bottom)
                y = (int)Math.Round(frame.Top - _textEditor.Height - 7f);
            x = Math.Max(canvasBounds.Left, Math.Min(canvasBounds.Right - _textEditor.Width, x));
            y = Math.Max(canvasBounds.Top, Math.Min(canvasBounds.Bottom - _textEditor.Height, y));
            _textEditor.Location = new Point(x, y);
        }

        private void ChooseTextColor()
        {
            if (_textEditor == null) return;
            using (ColorDialog dialog = new ColorDialog { Color = _textEditor.TextColor, FullOpen = true })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _textEditor.TextColor = dialog.Color;
                    UpdateTextPreview();
                }
            }
        }

        private void CommitTextEditor()
        {
            TextEditOverlay editor = _textEditor;
            if (editor == null) return;
            _textEditor = null;

            string text = editor.Editor.Text;
            Rectangle previewBounds = _canvas.TextPreviewBoundsInImage;
            int fontSize = editor.FontPixels;
            string fontFamily = editor.FontFamilyName;
            FontStyle fontStyle = editor.TextStyle;
            Color textColor = editor.TextColor;
            bool shadowEnabled = editor.ShadowEnabled;

            _canvas.Controls.Remove(editor);
            _canvas.ClearTextPreview();
            editor.Dispose();
            if (!String.IsNullOrWhiteSpace(text))
            {
                Bitmap result = (Bitmap)_image.Clone();
                using (Graphics g = Graphics.FromImage(result))
                using (Font font = new Font(fontFamily, Math.Max(8, Math.Min(300, fontSize)), fontStyle, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(textColor))
                using (Brush shadow = new SolidBrush(Color.FromArgb(165, Color.Black)))
                using (StringFormat format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word })
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    RectangleF textBounds = new RectangleF(previewBounds.X + 7, previewBounds.Y + 5, Math.Max(1, previewBounds.Width - 14), Math.Max(1, previewBounds.Height - 10));
                    if (shadowEnabled)
                    {
                        RectangleF shadowBounds = textBounds;
                        shadowBounds.Offset(Math.Max(1, fontSize / 28f), Math.Max(1, fontSize / 28f));
                        g.DrawString(text, font, shadow, shadowBounds, format);
                    }
                    g.DrawString(text, font, brush, textBounds, format);
                }
                Commit(result);
            }
            _canvas.Focus();
            UpdateStatus();
        }

        private void CancelTextEditor()
        {
            if (_textEditor == null) return;
            TextEditOverlay editor = _textEditor;
            _textEditor = null;
            _canvas.Controls.Remove(editor);
            _canvas.ClearTextPreview();
            editor.Dispose();
            _canvas.Focus();
            _status.Text = "已取消文字输入。";
        }

        private void ApplyCrop(object sender, EventArgs e)
        {
            Rectangle rect = _canvas.SelectionInImage;
            if (rect.Width < 2 || rect.Height < 2)
            {
                MessageBox.Show(this, "先在图片上拖动选择裁剪区域。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool hasWidth = _cropWidth.Value > 0;
            bool hasHeight = _cropHeight.Value > 0;
            if (hasWidth != hasHeight || (_cropRatio.SelectedIndex == _cropRatio.Items.Count - 1 && !hasWidth))
            {
                MessageBox.Show(this, "自定义输出尺寸需要同时填写宽和高。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Bitmap result;
            using (Bitmap cropped = _image.Clone(rect, PixelFormat.Format32bppArgb))
            {
                if (hasWidth && hasHeight)
                {
                    result = new Bitmap((int)_cropWidth.Value, (int)_cropHeight.Value, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(cropped, new Rectangle(0, 0, result.Width, result.Height));
                    }
                }
                else result = (Bitmap)cropped.Clone();
            }
            Commit(result);
            _canvas.ClearSelection();
            SetTool(EditorTool.View);
        }

        private void CancelCrop(object sender, EventArgs e)
        {
            _canvas.ClearSelection();
            SetTool(EditorTool.View);
            _status.Text = "已取消裁剪，图片没有改变。";
        }

        private void Rotate(int degrees)
        {
            Bitmap result = (Bitmap)_image.Clone();
            result.RotateFlip(degrees < 0 ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone);
            Commit(result);
        }

        private void Flip(RotateFlipType type)
        {
            Bitmap result = (Bitmap)_image.Clone();
            result.RotateFlip(type);
            Commit(result);
        }

        private void ResizeImage(object sender, EventArgs e)
        {
            using (ResizeDialog dialog = new ResizeDialog(_image.Width, _image.Height))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Bitmap result = new Bitmap(dialog.NewWidth, dialog.NewHeight, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(_image, new Rectangle(0, 0, result.Width, result.Height));
                }
                Commit(result);
            }
        }

        private void Straighten(object sender, EventArgs e)
        {
            using (AngleDialog dialog = new AngleDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                double radians = dialog.Angle * Math.PI / 180.0;
                int width = (int)Math.Ceiling(Math.Abs(_image.Width * Math.Cos(radians)) + Math.Abs(_image.Height * Math.Sin(radians)));
                int height = (int)Math.Ceiling(Math.Abs(_image.Height * Math.Cos(radians)) + Math.Abs(_image.Width * Math.Sin(radians)));
                Bitmap result = new Bitmap(Math.Max(1, width), Math.Max(1, height), PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TranslateTransform(width / 2f, height / 2f);
                    g.RotateTransform((float)dialog.Angle);
                    g.DrawImage(_image, -_image.Width / 2f, -_image.Height / 2f, _image.Width, _image.Height);
                }
                Commit(result);
            }
        }

        private void ExtendCanvas(object sender, EventArgs e)
        {
            using (ResizeDialog dialog = new ResizeDialog(_image.Width, _image.Height))
            {
                dialog.Text = "扩展画布尺寸";
                dialog.Caption = "输入新画布宽高；原图将居中放置。";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Color background = Color.White;
                using (ColorDialog color = new ColorDialog())
                {
                    color.Color = Color.White;
                    if (MessageBox.Show(this, "要选择画布填充色吗？选择“是”打开颜色选择器。", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes && color.ShowDialog(this) == DialogResult.OK)
                        background = color.Color;
                }
                Bitmap result = new Bitmap(dialog.NewWidth, dialog.NewHeight, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.Clear(background);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_image, (result.Width - _image.Width) / 2, (result.Height - _image.Height) / 2, _image.Width, _image.Height);
                }
                Commit(result);
            }
        }

        private void AdjustColors(object sender, EventArgs e)
        {
            OpenColorOptions(sender, e);
        }

        private void AdjustColors(int brightness, int contrast, int saturation)
        {
            Commit(AdjustBitmap(_image, brightness, contrast, saturation, 0));
        }

        private void ApplyFilter(object sender, EventArgs e)
        {
            OpenFilterOptions(sender, e);
        }

        private void ApplyConvolution(bool sharpen)
        {
            Commit(Convolve(_image, sharpen));
        }

        private void ApplyMosaic(List<Point> points)
        {
            if (points == null || points.Count == 0) return;
            int block = Math.Max(5, Math.Min(48, (int)_brushSize.Value * 2));
            int diameter = Math.Max(12, (int)_brushSize.Value * 4);
            using (GraphicsPath brushPath = BrushPathFactory.Create(points, diameter, _canvas.BrushShape))
            {
                RectangleF mask = brushPath.GetBounds();
                int left = Math.Max(0, (int)Math.Floor(mask.Left / block) * block);
                int top = Math.Max(0, (int)Math.Floor(mask.Top / block) * block);
                int right = Math.Min(_image.Width, (int)Math.Ceiling(mask.Right / block) * block);
                int bottom = Math.Min(_image.Height, (int)Math.Ceiling(mask.Bottom / block) * block);
                Rectangle sourceRegion = Rectangle.FromLTRB(left, top, right, bottom);
                if (sourceRegion.Width < 1 || sourceRegion.Height < 1) return;
                int smallW = Math.Max(1, (int)Math.Ceiling((double)sourceRegion.Width / block));
                int smallH = Math.Max(1, (int)Math.Ceiling((double)sourceRegion.Height / block));
                using (Bitmap small = new Bitmap(smallW, smallH, PixelFormat.Format32bppArgb))
                {
                    using (Graphics downsample = Graphics.FromImage(small))
                    {
                        downsample.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        downsample.DrawImage(_image, new Rectangle(0, 0, smallW, smallH), sourceRegion, GraphicsUnit.Pixel);
                    }
                    Bitmap result = (Bitmap)_image.Clone();
                    using (Graphics target = Graphics.FromImage(result))
                    {
                        target.InterpolationMode = InterpolationMode.NearestNeighbor;
                        target.PixelOffsetMode = PixelOffsetMode.Half;
                        target.SetClip(brushPath);
                        target.DrawImage(small, sourceRegion);
                    }
                    Commit(result);
                }
            }
        }

        private void DrawStroke(List<Point> points, Color color, int width, BrushTipShape shape)
        {
            if (points == null || points.Count == 0) return;
            Bitmap result = (Bitmap)_image.Clone();
            using (Graphics g = Graphics.FromImage(result))
            using (GraphicsPath path = BrushPathFactory.Create(points, width, shape))
            using (Brush brush = new SolidBrush(color))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(brush, path);
            }
            Commit(result);
        }

        private void DrawArrow(Point start, Point end, Color color, int width)
        {
            Bitmap result = (Bitmap)_image.Clone();
            using (Graphics g = Graphics.FromImage(result))
            using (Pen pen = new Pen(color, width))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                pen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(pen, start, end);
            }
            Commit(result);
        }

        private void DrawRectangle(Point start, Point end, Color color, int width)
        {
            Bitmap result = (Bitmap)_image.Clone();
            using (Graphics g = Graphics.FromImage(result))
            using (Pen pen = new Pen(color, width))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawRectangle(pen, Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(1, Math.Abs(end.X - start.X)), Math.Max(1, Math.Abs(end.Y - start.Y)));
            }
            Commit(result);
        }

        private void ApplyTextWatermark(object sender, EventArgs e)
        {
            string text = _annotationText.Text;
            if (String.IsNullOrWhiteSpace(text)) text = "我的图片";
            Bitmap result = (Bitmap)_image.Clone();
            using (Graphics g = Graphics.FromImage(result))
            using (Font font = new Font("Microsoft YaHei UI", Math.Max(18, _image.Width / 45f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (Brush brush = new SolidBrush(Color.FromArgb(150, _drawColor)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                SizeF size = g.MeasureString(text, font);
                g.DrawString(text, font, brush, Math.Max(0, _image.Width - size.Width - 24), Math.Max(0, _image.Height - size.Height - 20));
            }
            Commit(result);
        }

        private void ApplyImageWatermark(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Title = "选择透明 PNG 水印", Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                using (Image mark = Image.FromFile(dialog.FileName))
                {
                    Bitmap result = (Bitmap)_image.Clone();
                    int w = Math.Min(result.Width / 3, mark.Width);
                    int h = Math.Max(1, (int)(mark.Height * (w / (double)Math.Max(1, mark.Width))));
                    using (Graphics g = Graphics.FromImage(result))
                    using (ImageAttributes attr = new ImageAttributes())
                    {
                        ColorMatrix matrix = new ColorMatrix();
                        matrix.Matrix33 = 0.55f;
                        attr.SetColorMatrix(matrix);
                        g.DrawImage(mark, new Rectangle(result.Width - w - 18, result.Height - h - 18, w, h), 0, 0, mark.Width, mark.Height, GraphicsUnit.Pixel, attr);
                    }
                    Commit(result);
                }
            }
        }

        private void AddBorder(object sender, EventArgs e)
        {
            using (NumericDialog dialog = new NumericDialog("边框宽度", "边框像素", 24, 1, 1000))
            using (ColorDialog color = new ColorDialog { Color = Color.White })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || color.ShowDialog(this) != DialogResult.OK) return;
                int width = dialog.Value;
                Bitmap result = new Bitmap(_image.Width + width * 2, _image.Height + width * 2, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.Clear(color.Color);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_image, new Rectangle(width, width, _image.Width, _image.Height));
                }
                Commit(result);
            }
        }

        private void ChooseColor()
        {
            using (ColorDialog dialog = new ColorDialog { Color = _drawColor, FullOpen = true })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _drawColor = dialog.Color;
                    _canvas.DrawColor = _drawColor;
                    _drawColorButton.BackColor = _drawColor;
                    _drawColorButton.ForeColor = _drawColor.GetBrightness() < 0.48f ? Color.White : Navy;
                    _drawColorButton.Text = "颜色 #" + _drawColor.R.ToString("X2") + _drawColor.G.ToString("X2") + _drawColor.B.ToString("X2");
                    _drawColorButton.FlatAppearance.BorderColor = Mint;
                }
        }

        private void Commit(Bitmap next)
        {
            if (next == null) return;
            _hasUnsavedChanges = true;
            _undo.Add((Bitmap)_image.Clone());
            while (_undo.Count > 12) { _undo[0].Dispose(); _undo.RemoveAt(0); }
            ClearStack(_redo);
            _image.Dispose();
            _image = next;
            _canvas.Image = _image;
            _canvas.Invalidate();
            UpdateStatus();
        }

        private void Undo(object sender, EventArgs e)
        {
            if (_undo.Count == 0) return;
            _hasUnsavedChanges = true;
            _redo.Add((Bitmap)_image.Clone());
            _image.Dispose();
            _image = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            _canvas.Image = _image;
            _canvas.Invalidate();
            UpdateStatus();
        }

        private void Redo(object sender, EventArgs e)
        {
            if (_redo.Count == 0) return;
            _hasUnsavedChanges = true;
            _undo.Add((Bitmap)_image.Clone());
            _image.Dispose();
            _image = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            _canvas.Image = _image;
            _canvas.Invalidate();
            UpdateStatus();
        }

        private void RestoreOriginal(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "撤销所有本次编辑并恢复刚打开的图像？", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Commit((Bitmap)_original.Clone());
        }

        private void SaveCopy(object sender, EventArgs e)
        {
            if (_textEditor != null) CommitTextEditor();
            SaveCopyInteractive();
        }

        private bool SaveCopyInteractive()
        {
            using (SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "保存编辑后的图片副本",
                FileName = Path.GetFileNameWithoutExtension(_sourcePath) + "_edited.png",
                Filter = "PNG 图片|*.png|JPEG 图片|*.jpg;*.jpeg|BMP 图片|*.bmp|TIFF 图片|*.tif;*.tiff"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                try
                {
                    SaveBitmap(_image, dialog.FileName, 92L);
                    _hasUnsavedChanges = false;
                    _status.Text = "已保存副本：" + dialog.FileName;
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private void EditorFormClosing(object sender, FormClosingEventArgs e)
        {
            bool pendingText = _textEditor != null && !String.IsNullOrWhiteSpace(_textEditor.Editor.Text);
            if (!_hasUnsavedChanges && !pendingText) return;
            DialogResult result = MessageBox.Show(this, "图片有尚未保存的修改。是否保存为副本？", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (result == DialogResult.Yes)
            {
                if (_textEditor != null) CommitTextEditor();
                if (!SaveCopyInteractive()) e.Cancel = true;
            }
        }

        private void ShowInfo(object sender, EventArgs e)
        {
            using (ImageInfoForm dialog = new ImageInfoForm(_sourcePath)) dialog.ShowDialog(this);
        }

        private void UpdateStatus()
        {
            _status.Text = String.Format("{0} × {1} 像素    {2}    编辑不会覆盖源文件；使用“另存副本”导出。", _image.Width, _image.Height, Path.GetFileName(_sourcePath));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_textEditor != null)
            {
                if ((keyData & Keys.KeyCode) == Keys.Escape) { CancelTextEditor(); return true; }
                if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.Enter) { CommitTextEditor(); return true; }
                if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.Z) { _textEditor.Editor.Undo(); return true; }
                if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.Y) return true;
            }
            if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.Z) { Undo(this, EventArgs.Empty); return true; }
            if ((keyData & Keys.Control) != 0 && (keyData & Keys.KeyCode) == Keys.Y) { Redo(this, EventArgs.Empty); return true; }
            if ((keyData & Keys.KeyCode) == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_textEditor != null)
            {
                TextEditOverlay editor = _textEditor;
                _textEditor = null;
                _canvas.Controls.Remove(editor);
                editor.Dispose();
            }
            ClearStack(_undo);
            ClearStack(_redo);
            if (_image != null) _image.Dispose();
            if (_original != null) _original.Dispose();
            base.OnFormClosed(e);
        }

        private static Bitmap LoadBitmap(string path)
        {
            using (DecodedImage decoded = ImageDecoder.Load(path))
            {
                Image source = decoded.Image;
                Bitmap result = new Bitmap(source);
                try
                {
                    const int orientationTag = 0x0112;
                    if (Array.IndexOf(source.PropertyIdList, orientationTag) >= 0)
                    {
                        PropertyItem item = source.GetPropertyItem(orientationTag);
                        ushort orientation = BitConverter.ToUInt16(item.Value, 0);
                        switch (orientation)
                        {
                            case 2: result.RotateFlip(RotateFlipType.RotateNoneFlipX); break;
                            case 3: result.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                            case 4: result.RotateFlip(RotateFlipType.Rotate180FlipX); break;
                            case 5: result.RotateFlip(RotateFlipType.Rotate90FlipX); break;
                            case 6: result.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                            case 7: result.RotateFlip(RotateFlipType.Rotate270FlipX); break;
                            case 8: result.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                        }
                    }
                }
                catch { }
                return result;
            }
        }

        internal static void SaveBitmap(Image image, string path, long quality)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg")
            {
                ImageCodecInfo codec = Array.Find(ImageCodecInfo.GetImageEncoders(), delegate(ImageCodecInfo item) { return item.MimeType == "image/jpeg"; });
                using (EncoderParameters parameters = new EncoderParameters(1))
                {
                    parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, Math.Max(1L, Math.Min(100L, quality)));
                    image.Save(path, codec, parameters);
                }
            }
            else
            {
                ImageFormat format = ext == ".bmp" ? ImageFormat.Bmp : (ext == ".gif" ? ImageFormat.Gif : (ext == ".tif" || ext == ".tiff" ? ImageFormat.Tiff : ImageFormat.Png));
                image.Save(path, format);
            }
        }

        private static void ClearStack(List<Bitmap> stack)
        {
            foreach (Bitmap bitmap in stack) bitmap.Dispose();
            stack.Clear();
        }

        internal static Bitmap AdjustBitmap(Bitmap source, int brightness, int contrast, int saturation, int filter)
        {
            Bitmap result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result)) g.DrawImage(source, 0, 0, source.Width, source.Height);
            Rectangle rect = new Rectangle(0, 0, result.Width, result.Height);
            BitmapData data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int length = Math.Abs(data.Stride) * data.Height;
            byte[] pixels = new byte[length];
            Marshal.Copy(data.Scan0, pixels, 0, length);
            double contrastFactor = contrast / 100.0;
            double saturationFactor = saturation / 100.0;
            for (int i = 0; i < length; i += 4)
            {
                double b = pixels[i], g = pixels[i + 1], r = pixels[i + 2];
                if (filter == 1)
                {
                    double gray = 0.299 * r + 0.587 * g + 0.114 * b; r = g = b = gray;
                }
                else if (filter == 2) { r *= 1.08; g *= 1.01; b *= 0.92; }
                else if (filter == 3) { r *= 0.92; g *= 1.01; b *= 1.09; }
                else if (filter == 4) { double rr = r; r = 0.393 * rr + 0.769 * g + 0.189 * b; g = 0.349 * rr + 0.686 * g + 0.168 * b; b = 0.272 * rr + 0.534 * g + 0.131 * b; }
                double lum = 0.299 * r + 0.587 * g + 0.114 * b;
                r = lum + (r - lum) * saturationFactor;
                g = lum + (g - lum) * saturationFactor;
                b = lum + (b - lum) * saturationFactor;
                r = (r - 128) * contrastFactor + 128 + brightness * 2.55;
                g = (g - 128) * contrastFactor + 128 + brightness * 2.55;
                b = (b - 128) * contrastFactor + 128 + brightness * 2.55;
                pixels[i] = ClampByte(b); pixels[i + 1] = ClampByte(g); pixels[i + 2] = ClampByte(r);
            }
            Marshal.Copy(pixels, 0, data.Scan0, length);
            result.UnlockBits(data);
            return result;
        }

        private static byte ClampByte(double value) { return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(value))); }

        private static Bitmap Convolve(Bitmap source, bool sharpen)
        {
            Bitmap src = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(src)) g.DrawImage(source, 0, 0, source.Width, source.Height);
            Bitmap result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            BitmapData sd = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData rd = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int len = Math.Abs(sd.Stride) * sd.Height;
            byte[] input = new byte[len]; byte[] output = new byte[len];
            Marshal.Copy(sd.Scan0, input, 0, len);
            int[] kernel = sharpen ? new int[] { 0,-1,0,-1,5,-1,0,-1,0 } : new int[] { 1,1,1,1,1,1,1,1,1 };
            int divisor = sharpen ? 1 : 9;
            for (int y = 0; y < src.Height; y++) for (int x = 0; x < src.Width; x++)
            {
                int dst = y * sd.Stride + x * 4;
                for (int c = 0; c < 3; c++)
                {
                    int sum = 0, ki = 0;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++, ki++)
                    {
                        int sx = Math.Max(0, Math.Min(src.Width - 1, x + dx));
                        int sy = Math.Max(0, Math.Min(src.Height - 1, y + dy));
                        sum += input[sy * sd.Stride + sx * 4 + c] * kernel[ki];
                    }
                    output[dst + c] = ClampByte(sum / (double)divisor);
                }
                output[dst + 3] = input[dst + 3];
            }
            Marshal.Copy(output, 0, rd.Scan0, len);
            src.UnlockBits(sd); result.UnlockBits(rd); src.Dispose();
            return result;
        }
    }

    internal sealed class PhotoEditCanvas : Control
    {
        [Flags]
        private enum CropHandle { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }
        private enum CropDragMode { None, Create, Move, Resize }

        private bool _dragging;
        private Point _startImage;
        private Rectangle _dragStartSelection;
        private CropHandle _activeCropHandle;
        private CropDragMode _cropDragMode;
        private bool _textPreviewActive;
        private bool _movingTextPreview;
        private bool _resizingTextPreview;
        private CropHandle _textResizeHandle;
        private Point _textDragStart;
        private Rectangle _textDragStartBounds;
        private Point _mousePosition;
        private bool _hasMousePosition;
        private Bitmap _liveMosaicBitmap;
        private List<Point> _points = new List<Point>();
        private float _scale = 1f;
        private PointF _offset;
        public Bitmap Image { get; set; }
        public EditorTool ActiveTool { get; set; }
        public double AspectRatio { get; set; }
        public int BrushSize { get; set; }
        public BrushTipShape BrushShape { get; set; }
        public Color DrawColor { get; set; }
        public Rectangle SelectionInImage { get; private set; }
        public float DisplayScale { get { UpdateImageTransform(); return _scale; } }
        public RectangleF DisplayedImageBounds { get { UpdateImageTransform(); return ImageScreenBounds(); } }
        public Rectangle TextPreviewBoundsInImage { get; private set; }
        public string TextPreview { get; private set; }
        public int TextPreviewFontPixels { get; private set; }
        public string TextPreviewFontFamily { get; private set; }
        public FontStyle TextPreviewStyle { get; private set; }
        public Color TextPreviewColor { get; private set; }
        public bool TextPreviewShadow { get; private set; }
        public event Action<EditorTool, Point, Point, List<Point>> ToolCompleted;
        public event Action<Rectangle> TextPreviewMoved;
        public event EventHandler TextPreviewMoveCompleted;

        public PhotoEditCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = true;
            ActiveTool = EditorTool.View;
            BrushSize = 8;
            BrushShape = BrushTipShape.Round;
            DrawColor = Color.Red;
        }

        public void ClearSelection() { SelectionInImage = Rectangle.Empty; Invalidate(); }

        public Point CanvasPointToImage(Point point)
        {
            UpdateImageTransform();
            return ImagePoint(point);
        }

        public RectangleF ImageRectangleToCanvas(Rectangle rect)
        {
            UpdateImageTransform();
            return new RectangleF(_offset.X + rect.X * _scale, _offset.Y + rect.Y * _scale, rect.Width * _scale, rect.Height * _scale);
        }

        public void BeginTextPreview(Rectangle bounds, string text, int fontPixels, string fontFamily, FontStyle style, Color color, bool shadow)
        {
            _textPreviewActive = true;
            TextPreviewBoundsInImage = NormalizeTextPreviewBounds(bounds);
            UpdateTextPreview(text, fontPixels, fontFamily, style, color, shadow);
        }

        public void SetTextPreviewBounds(Rectangle bounds)
        {
            if (!_textPreviewActive) return;
            TextPreviewBoundsInImage = NormalizeTextPreviewBounds(bounds);
            Invalidate();
            if (TextPreviewMoved != null) TextPreviewMoved(TextPreviewBoundsInImage);
        }

        public void UpdateTextPreview(string text, int fontPixels, string fontFamily, FontStyle style, Color color, bool shadow)
        {
            TextPreview = text ?? String.Empty;
            TextPreviewFontPixels = fontPixels;
            TextPreviewFontFamily = String.IsNullOrEmpty(fontFamily) ? "Microsoft YaHei UI" : fontFamily;
            TextPreviewStyle = style;
            TextPreviewColor = color;
            TextPreviewShadow = shadow;
            Invalidate();
        }

        public void ClearTextPreview()
        {
            _textPreviewActive = false;
            _movingTextPreview = false;
            _resizingTextPreview = false;
            TextPreview = String.Empty;
            Invalidate();
        }

        private void BeginMosaicStrokePreview()
        {
            ClearMosaicStrokePreview();
            if (Image == null) return;
            UpdateImageTransform();
            RectangleF bounds = ImageScreenBounds();
            int width = Math.Max(1, (int)Math.Round(bounds.Width));
            int height = Math.Max(1, (int)Math.Round(bounds.Height));
            int sourceBlock = Math.Max(5, Math.Min(48, BrushSize * 2));
            int screenBlock = Math.Max(2, (int)Math.Round(sourceBlock * _scale));
            int smallWidth = Math.Max(1, (int)Math.Ceiling((double)width / screenBlock));
            int smallHeight = Math.Max(1, (int)Math.Ceiling((double)height / screenBlock));
            try
            {
                using (Bitmap displayImage = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                using (Bitmap small = new Bitmap(smallWidth, smallHeight, PixelFormat.Format32bppArgb))
                {
                    using (Graphics displayGraphics = Graphics.FromImage(displayImage))
                    {
                        displayGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        displayGraphics.DrawImage(Image, new Rectangle(0, 0, width, height));
                    }
                    using (Graphics reduce = Graphics.FromImage(small))
                    {
                        reduce.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        reduce.DrawImage(displayImage, new Rectangle(0, 0, smallWidth, smallHeight));
                    }
                    _liveMosaicBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using (Graphics enlarge = Graphics.FromImage(_liveMosaicBitmap))
                    {
                        enlarge.InterpolationMode = InterpolationMode.NearestNeighbor;
                        enlarge.PixelOffsetMode = PixelOffsetMode.Half;
                        enlarge.DrawImage(small, new Rectangle(0, 0, width, height));
                    }
                }
            }
            catch
            {
                ClearMosaicStrokePreview();
            }
        }

        private void ClearMosaicStrokePreview()
        {
            if (_liveMosaicBitmap != null)
            {
                _liveMosaicBitmap.Dispose();
                _liveMosaicBitmap = null;
            }
        }

        private void DrawBrushStrokePreview(Graphics graphics)
        {
            if (_points.Count == 0) return;
            using (GraphicsPath path = BrushPathFactory.Create(_points, Math.Max(1, BrushSize), BrushShape))
            using (Matrix transform = new Matrix(_scale, 0f, 0f, _scale, _offset.X, _offset.Y))
            using (Brush brush = new SolidBrush(DrawColor))
            {
                path.Transform(transform);
                GraphicsState state = graphics.Save();
                graphics.SetClip(ImageScreenBounds());
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.Restore(state);
            }
        }

        private void DrawMosaicStrokePreview(Graphics graphics)
        {
            if (_liveMosaicBitmap == null || _points.Count == 0) return;
            int diameter = Math.Max(12, BrushSize * 4);
            using (GraphicsPath path = BrushPathFactory.Create(_points, diameter, BrushShape))
            using (Matrix transform = new Matrix(_scale, 0f, 0f, _scale, _offset.X, _offset.Y))
            {
                path.Transform(transform);
                GraphicsState state = graphics.Save();
                graphics.SetClip(ImageScreenBounds());
                graphics.SetClip(path, CombineMode.Intersect);
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(_liveMosaicBitmap, ImageScreenBounds());
                graphics.Restore(state);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClearMosaicStrokePreview();
            base.Dispose(disposing);
        }

        public void CreateCenteredSelection()
        {
            if (Image == null) return;
            int width, height;
            if (AspectRatio > 0)
            {
                if ((double)Image.Width / Image.Height >= AspectRatio)
                {
                    height = Math.Max(1, (int)Math.Round(Image.Height * 0.72));
                    width = Math.Max(1, (int)Math.Round(height * AspectRatio));
                    if (width > Image.Width)
                    {
                        width = Image.Width;
                        height = Math.Max(1, (int)Math.Round(width / AspectRatio));
                    }
                }
                else
                {
                    width = Math.Max(1, (int)Math.Round(Image.Width * 0.72));
                    height = Math.Max(1, (int)Math.Round(width / AspectRatio));
                    if (height > Image.Height)
                    {
                        height = Image.Height;
                        width = Math.Max(1, (int)Math.Round(height * AspectRatio));
                    }
                }
            }
            else
            {
                width = Math.Max(1, (int)Math.Round(Image.Width * 0.72));
                height = Math.Max(1, (int)Math.Round(Image.Height * 0.72));
            }
            width = Math.Min(Image.Width, width);
            height = Math.Min(Image.Height, height);
            SelectionInImage = new Rectangle((Image.Width - width) / 2, (Image.Height - height) / 2, width, height);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            if (Image == null) return;
            UpdateImageTransform();
            float w = Image.Width * _scale, h = Image.Height * _scale;
            using (Brush checkerA = new SolidBrush(Color.FromArgb(55, 58, 64)))
            using (Brush checkerB = new SolidBrush(Color.FromArgb(68, 71, 77)))
            {
                const int cell=16;
                for (int y=0; y<h; y+=cell) for (int x=0; x<w; x+=cell)
                    e.Graphics.FillRectangle((((x/cell)+(y/cell))%2==0)?checkerA:checkerB,_offset.X+x,_offset.Y+y,cell,cell);
            }
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(Image, new RectangleF(_offset.X, _offset.Y, w, h));
            if (_dragging && ActiveTool == EditorTool.Brush) DrawBrushStrokePreview(e.Graphics);
            if (_dragging && ActiveTool == EditorTool.Mosaic) DrawMosaicStrokePreview(e.Graphics);
            if (_textPreviewActive) DrawTextPreview(e.Graphics);
            Rectangle rect = SelectionInImage;
            if (!rect.IsEmpty)
            {
                RectangleF screen = new RectangleF(_offset.X + rect.X * _scale, _offset.Y + rect.Y * _scale, rect.Width * _scale, rect.Height * _scale);
                if (ActiveTool == EditorTool.Crop)
                {
                    RectangleF imageBounds = new RectangleF(_offset.X, _offset.Y, w, h);
                    using (Region outside = new Region(imageBounds))
                    using (Brush shade = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                    {
                        outside.Exclude(screen);
                        e.Graphics.FillRegion(shade, outside);
                    }
                    using (Pen thirds = new Pen(Color.FromArgb(150, 255, 255, 255), 1f))
                    {
                        e.Graphics.DrawLine(thirds, screen.Left + screen.Width / 3f, screen.Top, screen.Left + screen.Width / 3f, screen.Bottom);
                        e.Graphics.DrawLine(thirds, screen.Left + screen.Width * 2f / 3f, screen.Top, screen.Left + screen.Width * 2f / 3f, screen.Bottom);
                        e.Graphics.DrawLine(thirds, screen.Left, screen.Top + screen.Height / 3f, screen.Right, screen.Top + screen.Height / 3f);
                        e.Graphics.DrawLine(thirds, screen.Left, screen.Top + screen.Height * 2f / 3f, screen.Right, screen.Top + screen.Height * 2f / 3f);
                    }
                    using (Pen border = new Pen(Color.White, 2f))
                        e.Graphics.DrawRectangle(border, screen.X, screen.Y, screen.Width, screen.Height);
                    DrawCropHandles(e.Graphics, screen);
                }
                else
                {
                    using (Pen dark = new Pen(Color.Black, 3)) using (Pen light = new Pen(Color.White, 1))
                    {
                        dark.DashStyle = DashStyle.Dash; light.DashStyle = DashStyle.Dash;
                        e.Graphics.DrawRectangle(dark, screen.X, screen.Y, screen.Width, screen.Height);
                        e.Graphics.DrawRectangle(light, screen.X, screen.Y, screen.Width, screen.Height);
                    }
                }
            }
            if ((ActiveTool == EditorTool.Brush || ActiveTool == EditorTool.Mosaic) && _hasMousePosition)
            {
                float diameter = ActiveTool == EditorTool.Mosaic ? Math.Max(12f, BrushSize * 4f) * _scale : Math.Max(3f, BrushSize * _scale);
                RectangleF cursor = new RectangleF(_mousePosition.X - diameter / 2f, _mousePosition.Y - diameter / 2f, diameter, diameter);
                using (Pen outline = new Pen(Color.Black, 3f))
                using (Pen inner = new Pen(Color.White, 1f))
                {
                    if (BrushShape == BrushTipShape.Round)
                    {
                        e.Graphics.DrawEllipse(outline, cursor);
                        e.Graphics.DrawEllipse(inner, cursor);
                    }
                    else
                    {
                        e.Graphics.DrawRectangle(outline, cursor.X, cursor.Y, cursor.Width, cursor.Height);
                        e.Graphics.DrawRectangle(inner, cursor.X, cursor.Y, cursor.Width, cursor.Height);
                    }
                }
            }
        }

        private static void DrawCropHandles(Graphics graphics, RectangleF rect)
        {
            PointF[] centers = new PointF[]
            {
                new PointF(rect.Left, rect.Top), new PointF(rect.Left + rect.Width / 2f, rect.Top), new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Top + rect.Height / 2f), new PointF(rect.Right, rect.Bottom), new PointF(rect.Left + rect.Width / 2f, rect.Bottom),
                new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top + rect.Height / 2f)
            };
            using (Brush fill = new SolidBrush(Color.White))
            using (Pen outline = new Pen(Color.FromArgb(40, 120, 220), 1.5f))
            {
                foreach (PointF center in centers)
                {
                    RectangleF handle = new RectangleF(center.X - 4.5f, center.Y - 4.5f, 9f, 9f);
                    graphics.FillRectangle(fill, handle);
                    graphics.DrawRectangle(outline, handle.X, handle.Y, handle.Width, handle.Height);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            _mousePosition = e.Location;
            _hasMousePosition = true;
            if (_textPreviewActive)
            {
                if (Image == null || e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location)) return;
                RectangleF frameBounds = ImageRectangleToCanvas(TextPreviewBoundsInImage);
                _textResizeHandle = HitTestTextPreviewHandle(e.Location);
                if (_textResizeHandle == CropHandle.None && !frameBounds.Contains(e.Location))
                {
                    Point imagePoint = ImagePointUnclamped(e.Location);
                    Rectangle moved = TextPreviewBoundsInImage;
                    moved.X = imagePoint.X - moved.Width / 2;
                    moved.Y = imagePoint.Y - moved.Height / 2;
                    SetTextPreviewBounds(moved);
                }
                _movingTextPreview = _textResizeHandle == CropHandle.None;
                _resizingTextPreview = !_movingTextPreview;
                _textDragStart = e.Location;
                _textDragStartBounds = TextPreviewBoundsInImage;
                Capture = true;
                Cursor = _movingTextPreview ? Cursors.SizeAll : CursorForHandle(_textResizeHandle);
                Invalidate();
                return;
            }
            if (Image == null || e.Button != MouseButtons.Left || ActiveTool == EditorTool.View) return;
            if ((ActiveTool == EditorTool.Mosaic || ActiveTool == EditorTool.Brush) && !ImageScreenBounds().Contains(e.Location)) return;
            if (ActiveTool == EditorTool.Mosaic) BeginMosaicStrokePreview();
            _dragging = true;
            _startImage = ImagePoint(e.Location);
            _points.Clear();
            _points.Add(_startImage);
            if (ActiveTool == EditorTool.Crop)
            {
                if (!ImageScreenBounds().Contains(e.Location)) { _dragging = false; return; }
                _dragStartSelection = SelectionInImage;
                _activeCropHandle = HitTestCropHandle(e.Location);
                if (_activeCropHandle != CropHandle.None)
                    _cropDragMode = CropDragMode.Resize;
                else if (!SelectionInImage.IsEmpty && SelectionScreenBounds().Contains(e.Location))
                    _cropDragMode = CropDragMode.Move;
                else
                {
                    _cropDragMode = CropDragMode.Create;
                    SelectionInImage = Rectangle.Empty;
                }
            }
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePosition = e.Location;
            _hasMousePosition = true;
            if (_movingTextPreview || _resizingTextPreview)
            {
                int dx = (int)Math.Round((e.X - _textDragStart.X) / Math.Max(0.01f, _scale));
                int dy = (int)Math.Round((e.Y - _textDragStart.Y) / Math.Max(0.01f, _scale));
                Rectangle moved = _textDragStartBounds;
                if (_movingTextPreview)
                {
                    moved.X += dx;
                    moved.Y += dy;
                }
                else
                    moved = ResizeTextPreviewBounds(_textDragStartBounds, _textResizeHandle, dx, dy);
                SetTextPreviewBounds(moved);
                return;
            }
            if (!_dragging)
            {
                if (_textPreviewActive)
                {
                    CropHandle handle = HitTestTextPreviewHandle(e.Location);
                    Cursor = handle != CropHandle.None ? CursorForHandle(handle) : Cursors.SizeAll;
                    return;
                }
                if (ActiveTool == EditorTool.Crop)
                {
                    CropHandle handle = HitTestCropHandle(e.Location);
                    if (handle != CropHandle.None) Cursor = CursorForHandle(handle);
                    else if (!SelectionInImage.IsEmpty && SelectionScreenBounds().Contains(e.Location)) Cursor = Cursors.SizeAll;
                    else Cursor = Cursors.Cross;
                }
                else if (ActiveTool == EditorTool.Mosaic || ActiveTool == EditorTool.Brush)
                {
                    Cursor = Cursors.Cross;
                    Invalidate();
                }
                return;
            }
            Point current = ImagePoint(e.Location);
            if (ActiveTool == EditorTool.Brush || ActiveTool == EditorTool.Mosaic) _points.Add(current);
            if (ActiveTool == EditorTool.Crop)
            {
                if (_cropDragMode == CropDragMode.Create)
                    SelectionInImage = CreateSelectionFromPoints(_startImage, current, AspectRatio);
                else if (_cropDragMode == CropDragMode.Move)
                    SelectionInImage = MoveSelection(_dragStartSelection, current.X - _startImage.X, current.Y - _startImage.Y);
                else if (_cropDragMode == CropDragMode.Resize)
                    SelectionInImage = ResizeSelection(_dragStartSelection, _activeCropHandle, current);
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if ((_movingTextPreview || _resizingTextPreview) && e.Button == MouseButtons.Left)
            {
                _movingTextPreview = false;
                _resizingTextPreview = false;
                Capture = false;
                Cursor = Cursors.SizeAll;
                if (TextPreviewMoved != null) TextPreviewMoved(TextPreviewBoundsInImage);
                if (TextPreviewMoveCompleted != null) TextPreviewMoveCompleted(this, EventArgs.Empty);
                return;
            }
            if (!_dragging || e.Button != MouseButtons.Left) return;
            _dragging = false;
            Point end = ImagePoint(e.Location);
            if (ActiveTool == EditorTool.Crop)
            {
                if (_cropDragMode == CropDragMode.Create) SelectionInImage = CreateSelectionFromPoints(_startImage, end, AspectRatio);
                else if (_cropDragMode == CropDragMode.Move) SelectionInImage = MoveSelection(_dragStartSelection, end.X - _startImage.X, end.Y - _startImage.Y);
                else if (_cropDragMode == CropDragMode.Resize) SelectionInImage = ResizeSelection(_dragStartSelection, _activeCropHandle, end);
                _cropDragMode = CropDragMode.None;
                Capture = false;
                _points.Clear();
                Invalidate();
                return;
            }
            Point start = _startImage;
            if (ActiveTool != EditorTool.Crop && ToolCompleted != null) ToolCompleted(ActiveTool, start, end, new List<Point>(_points));
            ClearMosaicStrokePreview();
            _points.Clear();
            Capture = false;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_dragging)
            {
                Cursor = Cursors.Default;
                _hasMousePosition = false;
                Invalidate();
            }
        }

        private RectangleF ImageScreenBounds()
        {
            if (Image == null) return RectangleF.Empty;
            return new RectangleF(_offset.X, _offset.Y, Image.Width * _scale, Image.Height * _scale);
        }

        private void DrawTextPreview(Graphics graphics)
        {
            Rectangle bounds = TextPreviewBoundsInImage;
            RectangleF screenBounds = ImageRectangleToCanvas(bounds);
            using (Brush tint = new SolidBrush(Color.FromArgb(14, 70, 160, 245)))
            using (Pen frame = new Pen(Color.FromArgb(230, 95, 185, 255), 1.5f) { DashStyle = DashStyle.Dash })
            using (Brush handleFill = new SolidBrush(Color.White))
            using (Pen handleOutline = new Pen(Color.FromArgb(25, 125, 220), 1f))
            {
                graphics.FillRectangle(tint, screenBounds);
                graphics.DrawRectangle(frame, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);
                PointF[] handles = new PointF[]
                {
                    new PointF(screenBounds.Left, screenBounds.Top), new PointF(screenBounds.Left + screenBounds.Width / 2f, screenBounds.Top), new PointF(screenBounds.Right, screenBounds.Top),
                    new PointF(screenBounds.Right, screenBounds.Top + screenBounds.Height / 2f), new PointF(screenBounds.Right, screenBounds.Bottom),
                    new PointF(screenBounds.Left + screenBounds.Width / 2f, screenBounds.Bottom), new PointF(screenBounds.Left, screenBounds.Bottom),
                    new PointF(screenBounds.Left, screenBounds.Top + screenBounds.Height / 2f)
                };
                foreach (PointF center in handles)
                {
                    RectangleF grip = new RectangleF(center.X - 4f, center.Y - 4f, 8f, 8f);
                    graphics.FillRectangle(handleFill, grip);
                    graphics.DrawRectangle(handleOutline, grip.X, grip.Y, grip.Width, grip.Height);
                }
            }

            if (String.IsNullOrEmpty(TextPreview)) return;
            float previewSize = Math.Max(6f, Math.Min(300f, TextPreviewFontPixels * _scale));
            using (Font font = new Font(TextPreviewFontFamily, previewSize, TextPreviewStyle, GraphicsUnit.Pixel))
            using (Brush brush = new SolidBrush(TextPreviewColor))
            using (Brush shadow = new SolidBrush(Color.FromArgb(165, Color.Black)))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word })
            {
                RectangleF textBounds = new RectangleF(screenBounds.X + 7 * _scale, screenBounds.Y + 5 * _scale, Math.Max(1, screenBounds.Width - 14 * _scale), Math.Max(1, screenBounds.Height - 10 * _scale));
                if (TextPreviewShadow)
                {
                    RectangleF shadowBounds = textBounds;
                    shadowBounds.Offset(Math.Max(1, TextPreviewFontPixels / 28f) * _scale, Math.Max(1, TextPreviewFontPixels / 28f) * _scale);
                    graphics.DrawString(TextPreview, font, shadow, shadowBounds, format);
                }
                graphics.DrawString(TextPreview, font, brush, textBounds, format);
            }
        }

        private Rectangle NormalizeTextPreviewBounds(Rectangle bounds)
        {
            int width = Math.Max(1, bounds.Width);
            int height = Math.Max(1, bounds.Height);
            return new Rectangle(bounds.X, bounds.Y, width, height);
        }

        private void UpdateImageTransform()
        {
            if (Image == null) { _scale = 1f; _offset = PointF.Empty; return; }
            _scale = (float)Math.Min((double)ClientSize.Width / Image.Width, (double)ClientSize.Height / Image.Height);
            if (_scale <= 0) _scale = 1f;
            float width = Image.Width * _scale;
            float height = Image.Height * _scale;
            _offset = new PointF((ClientSize.Width - width) / 2f, (ClientSize.Height - height) / 2f);
        }

        private RectangleF SelectionScreenBounds()
        {
            if (SelectionInImage.IsEmpty) return RectangleF.Empty;
            return new RectangleF(_offset.X + SelectionInImage.X * _scale, _offset.Y + SelectionInImage.Y * _scale, SelectionInImage.Width * _scale, SelectionInImage.Height * _scale);
        }

        private CropHandle HitTestTextPreviewHandle(Point point)
        {
            if (!_textPreviewActive) return CropHandle.None;
            RectangleF rect = ImageRectangleToCanvas(TextPreviewBoundsInImage);
            const float radius = 9f;
            if (Near(point, rect.Left, rect.Top, radius)) return CropHandle.Left | CropHandle.Top;
            if (Near(point, rect.Left + rect.Width / 2f, rect.Top, radius)) return CropHandle.Top;
            if (Near(point, rect.Right, rect.Top, radius)) return CropHandle.Right | CropHandle.Top;
            if (Near(point, rect.Right, rect.Top + rect.Height / 2f, radius)) return CropHandle.Right;
            if (Near(point, rect.Right, rect.Bottom, radius)) return CropHandle.Right | CropHandle.Bottom;
            if (Near(point, rect.Left + rect.Width / 2f, rect.Bottom, radius)) return CropHandle.Bottom;
            if (Near(point, rect.Left, rect.Bottom, radius)) return CropHandle.Left | CropHandle.Bottom;
            if (Near(point, rect.Left, rect.Top + rect.Height / 2f, radius)) return CropHandle.Left;
            return CropHandle.None;
        }

        private Rectangle ResizeTextPreviewBounds(Rectangle start, CropHandle handle, int dx, int dy)
        {
            int minSize = Math.Max(1, (int)Math.Ceiling(28f / Math.Max(0.01f, _scale)));
            int left = start.Left, top = start.Top, right = start.Right, bottom = start.Bottom;
            if ((handle & CropHandle.Left) != 0) left = Math.Min(start.Right - minSize, start.Left + dx);
            if ((handle & CropHandle.Right) != 0) right = Math.Max(start.Left + minSize, start.Right + dx);
            if ((handle & CropHandle.Top) != 0) top = Math.Min(start.Bottom - minSize, start.Top + dy);
            if ((handle & CropHandle.Bottom) != 0) bottom = Math.Max(start.Top + minSize, start.Bottom + dy);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        private CropHandle HitTestCropHandle(Point point)
        {
            if (SelectionInImage.IsEmpty) return CropHandle.None;
            RectangleF rect = SelectionScreenBounds();
            const float radius = 8f;
            if (Near(point, rect.Left, rect.Top, radius)) return CropHandle.Left | CropHandle.Top;
            if (Near(point, rect.Left + rect.Width / 2f, rect.Top, radius)) return CropHandle.Top;
            if (Near(point, rect.Right, rect.Top, radius)) return CropHandle.Right | CropHandle.Top;
            if (Near(point, rect.Right, rect.Top + rect.Height / 2f, radius)) return CropHandle.Right;
            if (Near(point, rect.Right, rect.Bottom, radius)) return CropHandle.Right | CropHandle.Bottom;
            if (Near(point, rect.Left + rect.Width / 2f, rect.Bottom, radius)) return CropHandle.Bottom;
            if (Near(point, rect.Left, rect.Bottom, radius)) return CropHandle.Left | CropHandle.Bottom;
            if (Near(point, rect.Left, rect.Top + rect.Height / 2f, radius)) return CropHandle.Left;
            return CropHandle.None;
        }

        private static bool Near(Point point, float x, float y, float radius)
        {
            return Math.Abs(point.X - x) <= radius && Math.Abs(point.Y - y) <= radius;
        }

        private static Cursor CursorForHandle(CropHandle handle)
        {
            bool left = (handle & CropHandle.Left) != 0;
            bool right = (handle & CropHandle.Right) != 0;
            bool top = (handle & CropHandle.Top) != 0;
            bool bottom = (handle & CropHandle.Bottom) != 0;
            if ((left || right) && (top || bottom))
                return (left == top) ? Cursors.SizeNWSE : Cursors.SizeNESW;
            if (left || right) return Cursors.SizeWE;
            if (top || bottom) return Cursors.SizeNS;
            return Cursors.Default;
        }

        private Rectangle MoveSelection(Rectangle source, int dx, int dy)
        {
            int x = Math.Max(0, Math.Min(Image.Width - source.Width, source.X + dx));
            int y = Math.Max(0, Math.Min(Image.Height - source.Height, source.Y + dy));
            return new Rectangle(x, y, source.Width, source.Height);
        }

        private Rectangle ResizeSelection(Rectangle source, CropHandle handle, Point current)
        {
            bool left = (handle & CropHandle.Left) != 0;
            bool right = (handle & CropHandle.Right) != 0;
            bool top = (handle & CropHandle.Top) != 0;
            bool bottom = (handle & CropHandle.Bottom) != 0;
            const int minSize = 2;

            if (AspectRatio <= 0)
            {
                int x = source.X, y = source.Y, w = source.Width, h = source.Height;
                if (left) { x = Math.Max(0, Math.Min(current.X, source.Right - minSize)); w = source.Right - x; }
                if (right) { int edge = Math.Max(source.Left + minSize, Math.Min(current.X, Image.Width)); w = edge - source.Left; }
                if (top) { y = Math.Max(0, Math.Min(current.Y, source.Bottom - minSize)); h = source.Bottom - y; }
                if (bottom) { int edge = Math.Max(source.Top + minSize, Math.Min(current.Y, Image.Height)); h = edge - source.Top; }
                return new Rectangle(x, y, w, h);
            }

            double ratio = AspectRatio;
            double width, height, maxWidth, maxHeight;
            double centerX = source.Left + source.Width / 2.0;
            double centerY = source.Top + source.Height / 2.0;
            int anchorX = left ? source.Right : source.Left;
            int anchorY = top ? source.Bottom : source.Top;
            bool corner = (left || right) && (top || bottom);

            if (corner)
            {
                double wantedWidth = Math.Abs(current.X - anchorX);
                double wantedHeight = Math.Abs(current.Y - anchorY);
                height = (wantedWidth * ratio + wantedHeight) / (ratio * ratio + 1.0);
                width = height * ratio;
                maxWidth = left ? anchorX : Image.Width - anchorX;
                maxHeight = top ? anchorY : Image.Height - anchorY;
                Size size = FitRatioSize(width, height, maxWidth, maxHeight, ratio);
                int x = left ? anchorX - size.Width : anchorX;
                int y = top ? anchorY - size.Height : anchorY;
                return new Rectangle(x, y, size.Width, size.Height);
            }

            if (left || right)
            {
                width = Math.Abs(current.X - anchorX);
                height = width / ratio;
                maxWidth = left ? anchorX : Image.Width - anchorX;
                maxHeight = 2.0 * Math.Min(centerY, Image.Height - centerY);
                Size size = FitRatioSize(width, height, maxWidth, maxHeight, ratio);
                int x = left ? anchorX - size.Width : anchorX;
                int y = (int)Math.Round(centerY - size.Height / 2.0);
                return new Rectangle(x, y, size.Width, size.Height);
            }

            height = Math.Abs(current.Y - anchorY);
            width = height * ratio;
            maxHeight = top ? anchorY : Image.Height - anchorY;
            maxWidth = 2.0 * Math.Min(centerX, Image.Width - centerX);
            Size verticalSize = FitRatioSize(width, height, maxWidth, maxHeight, ratio);
            int verticalX = (int)Math.Round(centerX - verticalSize.Width / 2.0);
            int verticalY = top ? anchorY - verticalSize.Height : anchorY;
            return new Rectangle(verticalX, verticalY, verticalSize.Width, verticalSize.Height);
        }

        private static Size FitRatioSize(double wantedWidth, double wantedHeight, double maxWidth, double maxHeight, double ratio)
        {
            if (maxWidth < 1 || maxHeight < 1) return new Size(1, 1);
            wantedWidth = Math.Max(1, wantedWidth);
            wantedHeight = Math.Max(1, wantedHeight);
            double scale = Math.Min(1.0, Math.Min(maxWidth / wantedWidth, maxHeight / wantedHeight));
            int width = Math.Max(1, (int)Math.Round(wantedWidth * scale));
            int height = Math.Max(1, (int)Math.Round(width / ratio));
            if (height > maxHeight)
            {
                height = Math.Max(1, (int)Math.Floor(maxHeight));
                width = Math.Max(1, (int)Math.Round(height * ratio));
            }
            if (width > maxWidth)
            {
                width = Math.Max(1, (int)Math.Floor(maxWidth));
                height = Math.Max(1, (int)Math.Round(width / ratio));
            }
            return new Size(width, height);
        }

        private Rectangle CreateSelectionFromPoints(Point start, Point current, double ratio)
        {
            if (Image == null) return Rectangle.Empty;
            if (ratio <= 0)
            {
                int freeX = Math.Min(start.X, current.X), freeY = Math.Min(start.Y, current.Y);
                int freeRight = Math.Max(start.X, current.X), freeBottom = Math.Max(start.Y, current.Y);
                return Rectangle.FromLTRB(freeX, freeY, freeRight, freeBottom);
            }

            bool left = current.X < start.X;
            bool top = current.Y < start.Y;
            double wantedWidth = Math.Abs(current.X - start.X);
            double wantedHeight = Math.Abs(current.Y - start.Y);
            double height = (wantedWidth * ratio + wantedHeight) / (ratio * ratio + 1.0);
            double width = height * ratio;
            double maxWidth = left ? start.X : Image.Width - start.X;
            double maxHeight = top ? start.Y : Image.Height - start.Y;
            Size size = FitRatioSize(width, height, maxWidth, maxHeight, ratio);
            int x = left ? start.X - size.Width : start.X;
            int y = top ? start.Y - size.Height : start.Y;
            return new Rectangle(x, y, size.Width, size.Height);
        }

        private Point ImagePoint(Point screen)
        {
            if (Image == null) return Point.Empty;
            int x = (int)Math.Round((screen.X - _offset.X) / _scale);
            int y = (int)Math.Round((screen.Y - _offset.Y) / _scale);
            return new Point(Math.Max(0, Math.Min(Image.Width - 1, x)), Math.Max(0, Math.Min(Image.Height - 1, y)));
        }

        private Point ImagePointUnclamped(Point screen)
        {
            if (Image == null) return Point.Empty;
            return new Point((int)Math.Round((screen.X - _offset.X) / Math.Max(0.01f, _scale)), (int)Math.Round((screen.Y - _offset.Y) / Math.Max(0.01f, _scale)));
        }

    }

    internal sealed class TextEditOverlay : Panel
    {
        private readonly NumericUpDown _fontSize;
        private readonly NumericUpDown _horizontal;
        private readonly NumericUpDown _vertical;
        private readonly NumericUpDown _frameWidth;
        private readonly NumericUpDown _frameHeight;
        private readonly ComboBox _fontFamily;
        private readonly Button _colorButton;
        private readonly CheckBox _bold;
        private readonly CheckBox _italic;
        private readonly CheckBox _shadow;
        private bool _syncingGeometry;
        public readonly TextBox Editor;
        public event EventHandler ApplyRequested;
        public event EventHandler CancelRequested;
        public event EventHandler ColorRequested;
        public event EventHandler TextStyleChanged;
        public event EventHandler GeometryChanged;

        public int FontPixels { get { return (int)_fontSize.Value; } }
        public string FontFamilyName { get { return _fontFamily.SelectedItem as string ?? "Microsoft YaHei UI"; } }
        public FontStyle TextStyle { get { return (_bold.Checked ? FontStyle.Bold : FontStyle.Regular) | (_italic.Checked ? FontStyle.Italic : FontStyle.Regular); } }
        public bool ShadowEnabled { get { return _shadow.Checked; } }
        public Rectangle PreviewBounds { get { return new Rectangle((int)_horizontal.Value, (int)_vertical.Value, (int)_frameWidth.Value, (int)_frameHeight.Value); } }

        public TextEditOverlay(int width, Color color)
        {
            Width = Math.Max(320, width);
            Height = 160;
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = Color.FromArgb(246, 248, 251);
            TextColor = color;
            TabStop = false;

            Label header = new Label { Text = "文字实时显示在图片上；拖动虚线框移动、拖动控制点缩放", Location = new Point(6, 2), Size = new Size(Width - 12, 18), ForeColor = Color.FromArgb(67, 76, 90), TextAlign = ContentAlignment.MiddleLeft };
            Controls.Add(header);

            Editor = new TextBox
            {
                Location = new Point(5, 22),
                Size = new Size(Width - 12, 38),
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = color,
                Text = String.Empty
            };
            Controls.Add(Editor);

            FlowLayoutPanel geometryRow = new FlowLayoutPanel { Location = new Point(4, 62), Size = new Size(Width - 10, 28), WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Margin = Padding.Empty, Padding = Padding.Empty };
            geometryRow.Controls.Add(new Label { Text = "横X", Width = 35, Height = 26, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(1, 2, 1, 0) });
            _horizontal = new NumericUpDown { Minimum = -1000000, Maximum = 1000000, Width = 72, Height = 25, TextAlign = HorizontalAlignment.Center, Margin = new Padding(1, 2, 5, 0), ThousandsSeparator = true };
            geometryRow.Controls.Add(_horizontal);
            geometryRow.Controls.Add(new Label { Text = "纵Y", Width = 35, Height = 26, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(1, 2, 1, 0) });
            _vertical = new NumericUpDown { Minimum = -1000000, Maximum = 1000000, Width = 72, Height = 25, TextAlign = HorizontalAlignment.Center, Margin = new Padding(1, 2, 5, 0), ThousandsSeparator = true };
            geometryRow.Controls.Add(_vertical);
            geometryRow.Controls.Add(new Label { Text = "宽", Width = 24, Height = 26, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(1, 2, 1, 0) });
            _frameWidth = new NumericUpDown { Minimum = 1, Maximum = 1000000, Value = 460, Width = 68, Height = 25, TextAlign = HorizontalAlignment.Center, Margin = new Padding(1, 2, 5, 0), ThousandsSeparator = true };
            geometryRow.Controls.Add(_frameWidth);
            geometryRow.Controls.Add(new Label { Text = "高", Width = 24, Height = 26, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(1, 2, 1, 0) });
            _frameHeight = new NumericUpDown { Minimum = 1, Maximum = 1000000, Value = 94, Width = 68, Height = 25, TextAlign = HorizontalAlignment.Center, Margin = new Padding(1, 2, 1, 0), ThousandsSeparator = true };
            geometryRow.Controls.Add(_frameHeight);
            Controls.Add(geometryRow);

            FlowLayoutPanel firstRow = new FlowLayoutPanel { Location = new Point(4, 92), Size = new Size(Width - 10, 30), WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Margin = Padding.Empty, Padding = Padding.Empty };
            firstRow.Controls.Add(new Label { Text = "字号", Width = 34, Height = 26, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(1, 2, 1, 0) });
            _fontSize = new NumericUpDown { Minimum = 8, Maximum = 300, Value = 48, Width = 58, Height = 25, TextAlign = HorizontalAlignment.Center, Margin = new Padding(1, 2, 4, 0) };
            firstRow.Controls.Add(_fontSize);
            _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Height = 25, Margin = new Padding(1, 2, 4, 0) };
            using (InstalledFontCollection installed = new InstalledFontCollection())
            {
                foreach (FontFamily family in installed.Families)
                    _fontFamily.Items.Add(family.Name);
            }
            string preferred = "Microsoft YaHei UI";
            if (_fontFamily.Items.Contains(preferred)) _fontFamily.SelectedItem = preferred;
            else if (_fontFamily.Items.Contains("Microsoft YaHei")) _fontFamily.SelectedItem = "Microsoft YaHei";
            else if (_fontFamily.Items.Count > 0) _fontFamily.SelectedIndex = 0;
            firstRow.Controls.Add(_fontFamily);
            _colorButton = new Button { Text = "颜色", Width = 54, Height = 26, Margin = new Padding(1, 2, 4, 0), UseVisualStyleBackColor = false };
            firstRow.Controls.Add(_colorButton);
            _bold = new CheckBox { Text = "B", Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Width = 32, Height = 26, Margin = new Padding(1, 2, 2, 0), Checked = true };
            _italic = new CheckBox { Text = "I", Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Width = 32, Height = 26, Margin = new Padding(1, 2, 2, 0), Font = new Font("Segoe UI", 9f, FontStyle.Italic) };
            firstRow.Controls.Add(_bold);
            firstRow.Controls.Add(_italic);
            Controls.Add(firstRow);

            FlowLayoutPanel secondRow = new FlowLayoutPanel { Location = new Point(4, 125), Size = new Size(Width - 10, 31), WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Margin = Padding.Empty, Padding = Padding.Empty };
            _shadow = new CheckBox { Text = "文字阴影", Width = 88, Height = 26, Margin = new Padding(2, 2, 4, 0), Checked = true };
            secondRow.Controls.Add(_shadow);
            secondRow.Controls.Add(new Label { Text = "X/Y/宽高单位：图片像素", Width = 180, Height = 26, ForeColor = Color.FromArgb(100, 105, 114), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(1, 2, 1, 0) });
            Button cancel = new Button { Text = "取消", Width = 54, Height = 26, Margin = new Padding(1, 2, 2, 0) };
            Button apply = new Button { Text = "完成", Width = 54, Height = 26, Margin = new Padding(1, 2, 2, 0), BackColor = Color.FromArgb(45, 112, 210), ForeColor = Color.White, UseVisualStyleBackColor = false };
            secondRow.Controls.Add(cancel);
            secondRow.Controls.Add(apply);
            Controls.Add(secondRow);

            _fontSize.ValueChanged += delegate { RaiseStyleChanged(); };
            _fontFamily.SelectedIndexChanged += delegate { RaiseStyleChanged(); };
            _bold.CheckedChanged += delegate { RaiseStyleChanged(); };
            _italic.CheckedChanged += delegate { RaiseStyleChanged(); };
            _shadow.CheckedChanged += delegate { RaiseStyleChanged(); };
            Editor.TextChanged += delegate { RaiseStyleChanged(); };
            _horizontal.ValueChanged += delegate { RaiseGeometryChanged(); };
            _vertical.ValueChanged += delegate { RaiseGeometryChanged(); };
            _frameWidth.ValueChanged += delegate { RaiseGeometryChanged(); };
            _frameHeight.ValueChanged += delegate { RaiseGeometryChanged(); };
            _colorButton.Click += delegate { if (ColorRequested != null) ColorRequested(this, EventArgs.Empty); };
            cancel.Click += delegate { if (CancelRequested != null) CancelRequested(this, EventArgs.Empty); };
            apply.Click += delegate { if (ApplyRequested != null) ApplyRequested(this, EventArgs.Empty); };
            SetColorButton();
        }

        public void FocusEditor()
        {
            Editor.Focus();
            Editor.SelectAll();
        }

        public void FocusEditorEnd()
        {
            Editor.Focus();
            Editor.SelectionStart = Editor.TextLength;
            Editor.SelectionLength = 0;
        }

        public void SyncGeometry(Rectangle bounds)
        {
            _syncingGeometry = true;
            _horizontal.Value = Math.Max(_horizontal.Minimum, Math.Min(_horizontal.Maximum, bounds.X));
            _vertical.Value = Math.Max(_vertical.Minimum, Math.Min(_vertical.Maximum, bounds.Y));
            _frameWidth.Value = Math.Max(_frameWidth.Minimum, Math.Min(_frameWidth.Maximum, Math.Max(1, bounds.Width)));
            _frameHeight.Value = Math.Max(_frameHeight.Minimum, Math.Min(_frameHeight.Maximum, Math.Max(1, bounds.Height)));
            _syncingGeometry = false;
        }

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                if (_colorButton != null) SetColorButton();
                if (Editor != null) Editor.ForeColor = value;
            }
        }
        private Color _textColor;

        private void SetColorButton()
        {
            _colorButton.BackColor = TextColor;
            _colorButton.ForeColor = TextColor.GetBrightness() < 0.5f ? Color.White : Color.Black;
        }

        private void RaiseStyleChanged()
        {
            if (TextStyleChanged != null) TextStyleChanged(this, EventArgs.Empty);
        }

        private void RaiseGeometryChanged()
        {
            if (!_syncingGeometry && GeometryChanged != null) GeometryChanged(this, EventArgs.Empty);
        }

    }

    internal sealed class EditorialSidebarHeader : Panel
    {
        public EditorialSidebarHeader()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (Brush paper = new SolidBrush(PhotoEditorForm.Paper)) e.Graphics.FillRectangle(paper, ClientRectangle);
            using (Brush circle = new SolidBrush(Color.FromArgb(75, PhotoEditorForm.Mint)))
                e.Graphics.FillEllipse(circle, Width - 92, -37, 112, 112);
            using (Pen dots = new Pen(Color.FromArgb(145, PhotoEditorForm.NavySoft), 1f))
            {
                for (int y = 12; y < 66; y += 8)
                    for (int x = Width - 70; x < Width - 18; x += 8)
                        e.Graphics.DrawEllipse(dots, x, y, 1.5f, 1.5f);
            }
            using (Pen rule = new Pen(PhotoEditorForm.Coral, 2f)) e.Graphics.DrawLine(rule, 14, Height - 2, Width - 14, Height - 2);
        }
    }

    internal sealed class EditorialToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _paper;
        private readonly Color _paperShade;
        private readonly Color _navy;
        private readonly Color _coral;
        private readonly Color _mint;
        private readonly bool _menu;

        public EditorialToolStripRenderer(Color paper, Color paperShade, Color navy, Color coral, Color mint, bool menu)
        {
            _paper = paper;
            _paperShade = paperShade;
            _navy = navy;
            _coral = coral;
            _mint = mint;
            _menu = menu;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (Brush background = new SolidBrush(_menu ? _paper : _navy))
                e.Graphics.FillRectangle(background, e.AffectedBounds);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed) return;
            Rectangle bounds = e.Item.Bounds;
            bounds.Inflate(-1, -1);
            using (GraphicsPath path = new GraphicsPath())
            {
                int radius = 7;
                path.AddArc(bounds.Left, bounds.Top, radius * 2, radius * 2, 180, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Top, radius * 2, radius * 2, 270, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                Color fill = e.Item.Pressed ? _coral : (_menu ? _paperShade : _navySoftColor());
                using (Brush brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
                if (!_menu && e.Item.Selected && !e.Item.Pressed)
                    using (Pen line = new Pen(_mint, 1f)) e.Graphics.DrawPath(line, path);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed) return;
            using (Brush brush = new SolidBrush(e.Item.Pressed ? _paperShade : Color.FromArgb(222, 239, 231)))
                e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Rectangle bounds = e.Item.Bounds;
            using (Pen line = new Pen(Color.FromArgb(120, _menu ? _mint : _paperShade), 1f))
            {
                if (e.Vertical) e.Graphics.DrawLine(line, bounds.Left + bounds.Width / 2, bounds.Top + 4, bounds.Left + bounds.Width / 2, bounds.Bottom - 4);
                else e.Graphics.DrawLine(line, bounds.Left + 4, bounds.Top + bounds.Height / 2, bounds.Right - 4, bounds.Top + bounds.Height / 2);
            }
        }

        private Color _navySoftColor()
        {
            return Color.FromArgb(Math.Min(255, _navy.R + 18), Math.Min(255, _navy.G + 20), Math.Min(255, _navy.B + 20));
        }
    }

    internal static class BrushPathFactory
    {
        public static GraphicsPath Create(IList<Point> points, int width, BrushTipShape shape)
        {
            GraphicsPath path = new GraphicsPath();
            if (points == null || points.Count == 0) return path;
            List<Point> distinct = new List<Point>();
            for (int i = 0; i < points.Count; i++)
                if (distinct.Count == 0 || distinct[distinct.Count - 1] != points[i]) distinct.Add(points[i]);

            float diameter = Math.Max(1, width);
            if (distinct.Count == 1)
            {
                Point point = distinct[0];
                RectangleF tip = new RectangleF(point.X - diameter / 2f, point.Y - diameter / 2f, diameter, diameter);
                if (shape == BrushTipShape.Round) path.AddEllipse(tip);
                else path.AddRectangle(tip);
                return path;
            }

            PointF[] line = new PointF[distinct.Count];
            for (int i = 0; i < distinct.Count; i++) line[i] = distinct[i];
            path.AddLines(line);
            LineCap cap = shape == BrushTipShape.Round ? LineCap.Round : LineCap.Square;
            LineJoin join = shape == BrushTipShape.Round ? LineJoin.Round : LineJoin.Miter;
            using (Pen pen = new Pen(Color.Black, diameter) { StartCap = cap, EndCap = cap, LineJoin = join })
                path.Widen(pen);
            return path;
        }
    }

    internal sealed class ResizeDialog : Form
    {
        private readonly NumericUpDown _width, _height;
        private readonly CheckBox _lock;
        private readonly int _sourceWidth, _sourceHeight;
        public string Caption { set { _caption.Text = value; } }
        private readonly Label _caption;
        public int NewWidth { get { return (int)_width.Value; } }
        public int NewHeight { get { return (int)_height.Value; } }
        public ResizeDialog(int width, int height)
        {
            _sourceWidth=width; _sourceHeight=height;
            Text="改尺寸"; Width=330; Height=230; FormBorderStyle=FormBorderStyle.FixedDialog; StartPosition=FormStartPosition.CenterParent; MinimizeBox=false; MaximizeBox=false;
            _caption=new Label {Text="输入新宽高；保持比例可避免变形。",Location=new Point(16,14),Width=285,Height=34}; Controls.Add(_caption);
            Controls.Add(new Label {Text="宽度",Location=new Point(16,60),Width=80});
            Controls.Add(new Label {Text="高度",Location=new Point(160,60),Width=80});
            _width=new NumericUpDown {Location=new Point(16,82),Width=120,Minimum=1,Maximum=30000,Value=width};
            _height=new NumericUpDown {Location=new Point(160,82),Width=120,Minimum=1,Maximum=30000,Value=height};
            _lock=new CheckBox {Text="锁定比例",Location=new Point(16,120),Width=120,Checked=true}; Controls.Add(_lock);
            _width.ValueChanged += delegate { if (_lock.Checked) _height.Value=Math.Max(1,Math.Min(30000,(int)Math.Round((double)_width.Value*_sourceHeight/_sourceWidth))); };
            _height.ValueChanged += delegate { if (_lock.Checked) _width.Value=Math.Max(1,Math.Min(30000,(int)Math.Round((double)_height.Value*_sourceWidth/_sourceHeight))); };
            Controls.Add(_width); Controls.Add(_height);
            Button ok=new Button {Text="确定",Location=new Point(110,156),DialogResult=DialogResult.OK}; Button cancel=new Button {Text="取消",Location=new Point(200,156),DialogResult=DialogResult.Cancel}; Controls.Add(ok); Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
        }
    }

    internal sealed class AngleDialog : Form
    {
        private readonly NumericUpDown _angle;
        public double Angle { get { return (double)_angle.Value; } }
        public AngleDialog()
        {
            Text="纠偏/旋正"; Width=300; Height=170; FormBorderStyle=FormBorderStyle.FixedDialog; StartPosition=FormStartPosition.CenterParent; MinimizeBox=false; MaximizeBox=false;
            Controls.Add(new Label {Text="旋转角度（-45° 到 45°）",Location=new Point(16,18),Width=250});
            _angle=new NumericUpDown {Location=new Point(16,48),Width=250,Minimum=-45,Maximum=45,DecimalPlaces=1,Increment=0.5M}; Controls.Add(_angle);
            Button ok=new Button {Text="确定",Location=new Point(100,88),DialogResult=DialogResult.OK}; Button cancel=new Button {Text="取消",Location=new Point(190,88),DialogResult=DialogResult.Cancel}; Controls.Add(ok); Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
        }
    }

    internal sealed class NumericDialog : Form
    {
        private readonly NumericUpDown _value;
        public int Value { get { return (int)_value.Value; } }
        public NumericDialog(string title, string label, int value, int min, int max)
        {
            Text=title; Width=310; Height=165; FormBorderStyle=FormBorderStyle.FixedDialog; StartPosition=FormStartPosition.CenterParent; MinimizeBox=false; MaximizeBox=false;
            Controls.Add(new Label {Text=label,Location=new Point(16,18),Width=250});
            _value=new NumericUpDown {Location=new Point(16,46),Width=250,Minimum=min,Maximum=max,Value=value}; Controls.Add(_value);
            Button ok=new Button {Text="确定",Location=new Point(100,84),DialogResult=DialogResult.OK}; Button cancel=new Button {Text="取消",Location=new Point(190,84),DialogResult=DialogResult.Cancel}; Controls.Add(ok); Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
        }
    }

    internal sealed class FilterDialog : Form
    {
        private readonly ComboBox _filters;
        public int SelectedIndex { get { return _filters.SelectedIndex; } }
        public FilterDialog(string[] filters)
        {
            Text="滤镜效果"; Width=320; Height=170; FormBorderStyle=FormBorderStyle.FixedDialog; StartPosition=FormStartPosition.CenterParent; MinimizeBox=false; MaximizeBox=false;
            Controls.Add(new Label {Text="选择滤镜",Location=new Point(16,18),Width=250});
            _filters=new ComboBox {Location=new Point(16,46),Width=270,DropDownStyle=ComboBoxStyle.DropDownList}; _filters.Items.AddRange(filters); _filters.SelectedIndex=0; Controls.Add(_filters);
            Button ok=new Button {Text="应用",Location=new Point(100,88),DialogResult=DialogResult.OK}; Button cancel=new Button {Text="取消",Location=new Point(190,88),DialogResult=DialogResult.Cancel}; Controls.Add(ok); Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
        }
    }

    internal sealed class ColorAdjustDialog : Form
    {
        private readonly TrackBar _brightness, _contrast, _saturation;
        public int Brightness { get { return _brightness.Value; } }
        public int Contrast { get { return _contrast.Value; } }
        public int Saturation { get { return _saturation.Value; } }
        public ColorAdjustDialog()
        {
            Text="颜色调整"; Width=370; Height=350; FormBorderStyle=FormBorderStyle.FixedDialog; StartPosition=FormStartPosition.CenterParent; MinimizeBox=false; MaximizeBox=false;
            AddSlider("亮度",-100,100,0,16,20,out _brightness);
            AddSlider("对比度",0,200,100,16,92,out _contrast);
            AddSlider("饱和度",0,200,100,16,164,out _saturation);
            Button reset=new Button {Text="重置",Location=new Point(16,236),Width=86}; reset.Click+=delegate {_brightness.Value=0;_contrast.Value=100;_saturation.Value=100;}; Controls.Add(reset);
            Button ok=new Button {Text="应用",Location=new Point(170,236),Width=80,DialogResult=DialogResult.OK}; Button cancel=new Button {Text="取消",Location=new Point(260,236),Width=80,DialogResult=DialogResult.Cancel}; Controls.Add(ok); Controls.Add(cancel); AcceptButton=ok; CancelButton=cancel;
        }
        private void AddSlider(string text,int min,int max,int val,int x,int y,out TrackBar track)
        {
            Controls.Add(new Label {Text=text,Location=new Point(x,y),Width=100});
            track=new TrackBar {Location=new Point(x,y+20),Width=320,Minimum=min,Maximum=max,Value=val,TickFrequency=Math.Max(1,(max-min)/10)}; Controls.Add(track);
        }
    }

    internal sealed class ImageInfoForm : Form
    {
        public ImageInfoForm(string path)
        {
            Text="图片信息"; Width=650; Height=570; StartPosition=FormStartPosition.CenterParent;
            ListView list=new ListView {Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,GridLines=true}; list.Columns.Add("属性",180); list.Columns.Add("值",430);
            FileInfo file=new FileInfo(path);
            Add(list,"文件名",file.Name); Add(list,"完整路径",file.FullName); Add(list,"文件大小",FormatBytes(file.Length)); Add(list,"修改时间",file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            try
            {
                using (DecodedImage decoded=ImageDecoder.Load(path))
                {
                    Image image=decoded.Image;
                    int frameCount = 1;
                    foreach (Guid id in image.FrameDimensionsList)
                    {
                        try { frameCount = Math.Max(frameCount, image.GetFrameCount(new FrameDimension(id))); }
                        catch { }
                    }
                    Add(list,"格式",image.RawFormat.ToString()); Add(list,"像素尺寸",image.Width+" × "+image.Height); Add(list,"DPI",image.HorizontalResolution.ToString("0.##")+" × "+image.VerticalResolution.ToString("0.##")); Add(list,"帧数",frameCount.ToString());
                    foreach (PropertyItem item in image.PropertyItems)
                    {
                        string name=ExifName(item.Id); if (name==null) continue;
                        Add(list,name,ExifValue(item));
                    }
                }
            }
            catch (Exception ex) { Add(list,"读取元数据",ex.Message); }
            Controls.Add(list);
        }
        private static void Add(ListView list,string name,string value) { list.Items.Add(new ListViewItem(new string[]{name,value ?? ""})); }
        private static string FormatBytes(long value) { double x=value; string[] units={"B","KB","MB","GB"}; int i=0; while(x>=1024&&i<units.Length-1){x/=1024;i++;} return x.ToString("0.##")+" "+units[i]; }
        private static string ExifName(int id)
        {
            switch(id){case 0x010F:return "相机厂商";case 0x0110:return "相机型号";case 0x0112:return "方向";case 0x0131:return "软件";case 0x0132:return "拍摄时间";case 0x829A:return "曝光时间";case 0x829D:return "光圈";case 0x8827:return "ISO";case 0x9003:return "原始拍摄时间";case 0x920A:return "焦距";case 0xA002:return "Exif宽度";case 0xA003:return "Exif高度";default:return null;}
        }
        private static string ExifValue(PropertyItem item)
        {
            try
            {
                if (item.Type==2) return System.Text.Encoding.ASCII.GetString(item.Value).Trim('\0');
                if (item.Type==3 && item.Value.Length>=2) return BitConverter.ToUInt16(item.Value,0).ToString();
                if (item.Type==4 && item.Value.Length>=4) return BitConverter.ToUInt32(item.Value,0).ToString();
                if (item.Type==5 && item.Value.Length>=8) {uint a=BitConverter.ToUInt32(item.Value,0),b=BitConverter.ToUInt32(item.Value,4);return b==0?"":a+"/"+b;}
            } catch { }
            return BitConverter.ToString(item.Value);
        }
    }
}
