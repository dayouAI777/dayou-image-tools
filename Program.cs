using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace LocalPhotoViewer
{
    internal enum ImageSortMode
    {
        ModifiedNewestFirst,
        ModifiedOldestFirst,
        CaptureNewestFirst,
        CaptureOldestFirst,
        NameAscending,
        NameDescending,
        SizeDescending,
        SizeAscending
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            StartUpdaterBootstrapIfNeeded();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ViewerForm(args));
        }

        private static void StartUpdaterBootstrapIfNeeded()
        {
            try
            {
                string directory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
                string installedUpdater = Path.Combine(directory, "DayouImageToolsUpdater.exe");
                string nextUpdater = Path.Combine(directory, "DayouImageToolsUpdaterNext.exe");
                string bootstrap = Path.Combine(directory, "DayouImageToolsUpdaterBootstrap.exe");
                string main = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                if (!File.Exists(installedUpdater) || !File.Exists(nextUpdater) || !File.Exists(bootstrap)) return;

                Version current = ReadVersion(installedUpdater);
                Version candidate = ReadVersion(nextUpdater);
                Version helper = ReadVersion(bootstrap);
                Version mainVersion = ReadVersion(main);
                if (candidate <= current || helper < candidate || mainVersion < candidate) return;

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(bootstrap, "--replace-installed-updater")
                {
                    WorkingDirectory = directory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                });
            }
            catch { }
        }

        private static Version ReadVersion(string path)
        {
            Version version;
            string value = System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
            return Version.TryParse(value, out version) ? version : new Version(0, 0, 0, 0);
        }
    }

    internal static class AppBranding
    {
        public const string ProductName = "大有图片工具";

        public static void ApplyWindowIcon(Form form)
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null) form.Icon = icon;
            }
            catch { }
        }
    }

    internal sealed class ViewerForm : Form
    {
        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(int action, int param, string value, int flags);

        private readonly ImageCanvas _canvas;
        private readonly FloatingNavigationButton _previousImageButton;
        private readonly FloatingNavigationButton _nextImageButton;
        private readonly ToolStrip _toolbar;
        private readonly StatusStrip _status;
        private readonly ToolStripStatusLabel _statusText;
        private readonly Timer _slideTimer;
        private readonly ListView _thumbnailList;
        private readonly ImageList _thumbnailImages;
        private readonly List<Bitmap> _thumbnailBitmapOwners = new List<Bitmap>();
        private readonly List<string> _files = new List<string>();
        private CancellationTokenSource _thumbnailCancellation;
        private CancellationTokenSource _folderScanCancellation;
        private int _thumbnailGeneration;
        private int _folderScanGeneration;
        private bool _uiReady;
        private string _pendingFolderPath;
        private string _pendingSelectedPath;
        private ImageSortMode _sortMode = ImageSortMode.ModifiedNewestFirst;
        private int _index = -1;
        private bool _fullScreen;
        private bool _syncingSelection;
        private FormBorderStyle _savedBorderStyle;
        private FormWindowState _savedWindowState;

        public ViewerForm(string[] args)
        {
            Text = AppBranding.ProductName;
            AppBranding.ApplyWindowIcon(this);
            Width = 1120;
            Height = 760;
            MinimumSize = new Size(720, 480);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            AllowDrop = true;

            MenuStrip menu = BuildMenu();
            _toolbar = BuildToolbar();
            _status = new StatusStrip();
            _statusText = new ToolStripStatusLabel("打开图片或文件夹，也可以把图片拖到窗口里") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _status.Items.Add(_statusText);

            _canvas = new ImageCanvas { Dock = DockStyle.Fill, BackColor = PhotoEditorForm.Navy };
            _previousImageButton = new FloatingNavigationButton(true) { Visible = false, AccessibleName = "上一张" };
            _nextImageButton = new FloatingNavigationButton(false) { Visible = false, AccessibleName = "下一张" };
            _previousImageButton.Click += delegate { Navigate(-1, false); };
            _nextImageButton.Click += delegate { Navigate(1, false); };
            _canvas.Controls.Add(_previousImageButton);
            _canvas.Controls.Add(_nextImageButton);
            _canvas.ViewChanged += delegate { UpdateStatus(); PositionFloatingNavigationButtons(); };
            _canvas.Resize += delegate { PositionFloatingNavigationButtons(); };
            _canvas.DragEnter += CanvasDragEnter;
            _canvas.DragDrop += CanvasDragDrop;

            _thumbnailImages = new ImageList { ImageSize = new Size(92, 72), ColorDepth = ColorDepth.Depth32Bit };
            _thumbnailList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.LargeIcon,
                LargeImageList = _thumbnailImages,
                MultiSelect = true,
                HideSelection = false,
                LabelWrap = true,
                BackColor = PhotoEditorForm.Paper,
                BorderStyle = BorderStyle.None,
                Activation = ItemActivation.OneClick,
                OwnerDraw = true
            };
            _thumbnailList.DrawItem += DrawThumbnailItem;
            _thumbnailList.SelectedIndexChanged += ThumbnailSelectionChanged;
            _thumbnailList.ContextMenuStrip = BuildThumbnailMenu();
            Panel thumbnailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = PhotoEditorForm.Paper };
            EditorialSidebarHeader thumbnailHeader = new EditorialSidebarHeader { Dock = DockStyle.Top, Height = 76 };
            Label thumbnailKicker = new Label { Text = "PHOTO JOURNAL / 01", Location = new Point(8, 9), Width = 180, Height = 16, Font = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Bold), ForeColor = PhotoEditorForm.Coral, BackColor = Color.Transparent };
            Label thumbnailTitle = new Label { Text = "文件夹图片", Location = new Point(8, 27), Width = 180, Height = 29, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold), ForeColor = PhotoEditorForm.Navy, BackColor = Color.Transparent };
            thumbnailHeader.Controls.Add(thumbnailKicker);
            thumbnailHeader.Controls.Add(thumbnailTitle);
            thumbnailPanel.Controls.Add(_thumbnailList);
            thumbnailPanel.Controls.Add(thumbnailHeader);
            SplitContainer viewerSplit = new SplitContainer { Width = 1080, Height = 700, SplitterDistance = 220, FixedPanel = FixedPanel.Panel1, Panel1MinSize = 160, Panel2MinSize = 420, SplitterWidth = 2, BackColor = PhotoEditorForm.Mint, Dock = DockStyle.Fill };
            viewerSplit.Panel1.Controls.Add(thumbnailPanel);
            viewerSplit.Panel2.Controls.Add(_canvas);

            Controls.Add(viewerSplit);
            Controls.Add(_status);
            Controls.Add(_toolbar);
            Controls.Add(menu);
            MainMenuStrip = menu;
            BackColor = PhotoEditorForm.Paper;
            ForeColor = PhotoEditorForm.Navy;
            PhotoEditorForm.ApplyEditorialTheme(this);
            _status.BackColor = PhotoEditorForm.Navy;
            _status.ForeColor = PhotoEditorForm.Paper;

            DragEnter += CanvasDragEnter;
            DragDrop += CanvasDragDrop;
            _slideTimer = new Timer { Interval = 3000 };
            _slideTimer.Tick += delegate { Navigate(1, true); };
            FormClosed += delegate
            {
                _thumbnailGeneration++;
                _folderScanGeneration++;
                CancelThumbnailGeneration();
                CancelFolderScan();
                _slideTimer.Stop();
                _canvas.ClearImage();
                _thumbnailList.LargeImageList = null;
                _thumbnailImages.Dispose();
                foreach (Bitmap bitmap in _thumbnailBitmapOwners) bitmap.Dispose();
                _thumbnailBitmapOwners.Clear();
            };

            Shown += delegate
            {
                _uiReady = true;
                if (!String.IsNullOrEmpty(_pendingFolderPath))
                {
                    string folder = _pendingFolderPath;
                    string selected = _pendingSelectedPath;
                    _pendingFolderPath = null;
                    _pendingSelectedPath = null;
                    QueueFolderScan(folder, selected);
                }
                else
                    StartThumbnailGeneration();
            };

            if (args != null && args.Length > 0)
                OpenArgument(args[0]);
            else
                UpdateStatus();
        }

        private MenuStrip BuildMenu()
        {
            MenuStrip menu = new MenuStrip();
            ToolStripMenuItem file = new ToolStripMenuItem("文件(&F)");
            file.DropDownItems.Add(MenuItem("打开图片…", Keys.Control | Keys.O, delegate { OpenFiles(); }));
            file.DropDownItems.Add(MenuItem("打开文件夹…", Keys.Control | Keys.Shift | Keys.O, delegate { OpenFolder(); }));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(MenuItem("编辑当前图片…", Keys.Control | Keys.E, delegate { OpenEditor(); }));
            file.DropDownItems.Add(MenuItem("批量图片工具…", Keys.None, delegate { OpenBatchTools(); }));
            file.DropDownItems.Add(MenuItem("图片信息 / EXIF…", Keys.None, delegate { ShowCurrentInfo(); }));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(MenuItem("另存为…", Keys.Control | Keys.S, delegate { SaveAs(); }));
            file.DropDownItems.Add(MenuItem("GIF 全部帧另存为 PNG…", Keys.None, delegate { SaveAllFrames(); }));
            file.DropDownItems.Add(MenuItem("打印…", Keys.Control | Keys.P, delegate { PrintImage(); }));
            file.DropDownItems.Add(MenuItem("在文件夹中显示", Keys.None, delegate { OpenCurrentFolder(); }));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(MenuItem("退出", Keys.Alt | Keys.F4, delegate { Close(); }));

            ToolStripMenuItem view = new ToolStripMenuItem("查看(&V)");
            view.DropDownItems.Add(MenuItem("上一张", Keys.Left, delegate { Navigate(-1, false); }));
            view.DropDownItems.Add(MenuItem("下一张", Keys.Right, delegate { Navigate(1, false); }));
            view.DropDownItems.Add(new ToolStripSeparator());
            view.DropDownItems.Add(MenuItem("放大", Keys.Add, delegate { _canvas.ZoomBy(1.25); UpdateStatus(); }));
            view.DropDownItems.Add(MenuItem("缩小", Keys.Subtract, delegate { _canvas.ZoomBy(1.0 / 1.25); UpdateStatus(); }));
            view.DropDownItems.Add(MenuItem("适应窗口", Keys.F, delegate { _canvas.SetFit(); UpdateStatus(); }));
            view.DropDownItems.Add(MenuItem("原始大小", Keys.D0, delegate { _canvas.SetOriginalSize(); UpdateStatus(); }));
            view.DropDownItems.Add(new ToolStripSeparator());
            view.DropDownItems.Add(MenuItem("向左旋转", Keys.Shift | Keys.R, delegate { _canvas.Rotate(-90); UpdateStatus(); }));
            view.DropDownItems.Add(MenuItem("向右旋转", Keys.R, delegate { _canvas.Rotate(90); UpdateStatus(); }));
            view.DropDownItems.Add(MenuItem("幻灯片", Keys.Space, delegate { ToggleSlideshow(); }));
            view.DropDownItems.Add(MenuItem("全屏", Keys.F11, delegate { ToggleFullScreen(); }));

            ToolStripMenuItem tools = new ToolStripMenuItem("工具(&T)");
            tools.DropDownItems.Add(MenuItem("批量尺寸 / 裁剪 / 压缩 / 转换 / 旋转 / 命名 / 水印 / 文件整理…", Keys.None, delegate { OpenBatchTools(); }));
            tools.DropDownItems.Add(MenuItem("删除当前图片…", Keys.Delete, delegate { DeleteCurrentFile(); }));
            tools.DropDownItems.Add(MenuItem("重命名当前图片…", Keys.F2, delegate { RenameCurrentFile(); }));
            tools.DropDownItems.Add(MenuItem("复制图片到剪贴板", Keys.None, delegate { CopyCurrentImage(); }));
            tools.DropDownItems.Add(MenuItem("设为桌面壁纸", Keys.None, delegate { SetWallpaper(); }));

            ToolStripMenuItem help = new ToolStripMenuItem("帮助(&H)");
            help.DropDownItems.Add(MenuItem("升级大有图片工具…", Keys.None, delegate { LaunchUpdater(); }));
            help.DropDownItems.Add(new ToolStripSeparator());
            help.DropDownItems.Add(MenuItem("快捷键", Keys.None, delegate { ShowShortcuts(); }));
            help.DropDownItems.Add(MenuItem("关于", Keys.None, delegate
            {
                MessageBox.Show(this, "大有图片工具\n图片浏览、编辑、批量处理、动画帧导出、打印和文件管理。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

            menu.Items.Add(file);
            menu.Items.Add(view);
            menu.Items.Add(tools);
            menu.Items.Add(help);
            return menu;
        }

        private void LaunchUpdater()
        {
            string updater = Path.Combine(Application.StartupPath, "DayouImageToolsUpdater.exe");
            if (!File.Exists(updater))
            {
                MessageBox.Show(this, "没有找到升级程序。请重新运行“大有图片工具”安装程序。", "升级程序缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(updater) { WorkingDirectory = Application.StartupPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "无法启动升级程序", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static ToolStripMenuItem MenuItem(string text, Keys shortcut, EventHandler action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            // WinForms rejects modifierless keys and Shift-only combinations as
            // ToolStripMenuItem.ShortcutKeys. Those keys are handled in
            // ProcessCmdKey; only register shortcuts that include Ctrl or Alt.
            if (shortcut != Keys.None && (shortcut & (Keys.Control | Keys.Alt)) != 0)
                item.ShortcutKeys = shortcut;
            item.Click += action;
            return item;
        }

        private ContextMenuStrip BuildThumbnailMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(MenuItem("打开 / 查看", Keys.None, delegate { if (_thumbnailList.FocusedItem != null) { _index = _thumbnailList.FocusedItem.Index; DisplayCurrent(); } }));
            menu.Items.Add(MenuItem("编辑当前图片…", Keys.None, delegate { OpenEditor(); }));
            menu.Items.Add(MenuItem("批量处理所选图片…", Keys.None, delegate { OpenBatchTools(); }));
            menu.Items.Add(MenuItem("图片信息 / EXIF…", Keys.None, delegate { ShowCurrentInfo(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuItem("重命名…", Keys.None, delegate { RenameCurrentFile(); }));
            menu.Items.Add(MenuItem("删除图片…", Keys.None, delegate { DeleteCurrentFile(); }));
            menu.Items.Add(MenuItem("在文件夹中显示", Keys.None, delegate { OpenCurrentFolder(); }));
            menu.Items.Add(MenuItem("设为桌面壁纸", Keys.None, delegate { SetWallpaper(); }));
            return menu;
        }

        private void OpenEditor()
        {
            if (_index < 0 || _index >= _files.Count || _canvas.CurrentImage == null) return;
            using (PhotoEditorForm editor = new PhotoEditorForm(_files[_index])) editor.ShowDialog(this);
        }

        private void OpenBatchTools()
        {
            List<string> targets = new List<string>();
            if (_thumbnailList.SelectedIndices.Count > 1)
            {
                foreach (int selected in _thumbnailList.SelectedIndices)
                    if (selected >= 0 && selected < _files.Count) targets.Add(_files[selected]);
            }
            else targets.AddRange(_files);
            string currentPath = _index >= 0 && _index < _files.Count ? _files[_index] : null;
            _canvas.ClearImage();
            using (BatchToolsForm dialog = new BatchToolsForm(targets)) dialog.ShowDialog(this);
            _files.RemoveAll(delegate(string path) { return !File.Exists(path); });
            if (_files.Count == 0) _index = -1;
            else
            {
                int oldIndex = currentPath == null ? -1 : _files.IndexOf(currentPath);
                _index = oldIndex >= 0 ? oldIndex : Math.Min(Math.Max(0, _index), _files.Count - 1);
            }
            RefreshThumbnailList();
            DisplayCurrent();
        }

        private void ShowCurrentInfo()
        {
            if (_index < 0 || _index >= _files.Count) return;
            using (ImageInfoForm dialog = new ImageInfoForm(_files[_index])) dialog.ShowDialog(this);
        }

        private void OpenCurrentFolder()
        {
            if (_index < 0 || _index >= _files.Count) return;
            try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + _files[_index] + "\""); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "无法打开文件夹", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void CopyCurrentImage()
        {
            if (_canvas.CurrentImage == null) return;
            try
            {
                using (Bitmap copy = new Bitmap(_canvas.CurrentImage)) Clipboard.SetImage(copy);
                _statusText.Text = "图片已复制到剪贴板";
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "复制失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void SaveAllFrames()
        {
            if (_index < 0 || _index >= _files.Count) return;
            try
            {
                using (DecodedImage decoded = ImageDecoder.Load(_files[_index]))
                {
                    Image image = decoded.Image;
                    FrameDimension dimension = null;
                    foreach (Guid id in image.FrameDimensionsList)
                    {
                        FrameDimension candidate = new FrameDimension(id);
                        if (candidate.Guid == FrameDimension.Time.Guid) { dimension = FrameDimension.Time; break; }
                        if (candidate.Guid == FrameDimension.Page.Guid) dimension = FrameDimension.Page;
                    }
                    if (dimension == null) { MessageBox.Show(this, "这张图片没有可拆分的动画帧。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                    int count = image.GetFrameCount(dimension);
                    if (count < 2) { MessageBox.Show(this, "这张图片只有一帧。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                    string defaultFolder = Path.Combine(Path.GetDirectoryName(_files[_index]), Path.GetFileNameWithoutExtension(_files[_index]) + "_frames");
                    using (FolderBrowserDialog folder = new FolderBrowserDialog { Description = "选择动画帧 PNG 的输出位置", SelectedPath = defaultFolder, ShowNewFolderButton = true })
                    {
                        if (folder.ShowDialog(this) != DialogResult.OK) return;
                        Directory.CreateDirectory(folder.SelectedPath);
                        for (int i = 0; i < count; i++)
                        {
                            image.SelectActiveFrame(dimension, i);
                            using (Bitmap frame = new Bitmap(image))
                                frame.Save(Path.Combine(folder.SelectedPath, Path.GetFileNameWithoutExtension(_files[_index]) + "_" + (i + 1).ToString("D4") + ".png"), ImageFormat.Png);
                        }
                        _statusText.Text = "已导出 " + count + " 帧到 " + folder.SelectedPath;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "导出帧失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void SetWallpaper()
        {
            if (_canvas.CurrentImage == null) return;
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DayouImageTools");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "wallpaper.bmp");
                using (Bitmap bitmap = new Bitmap(_canvas.CurrentImage)) bitmap.Save(path, ImageFormat.Bmp);
                if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE))
                    throw new InvalidOperationException("Windows 未能设置壁纸。错误码：" + Marshal.GetLastWin32Error());
                _statusText.Text = "已将当前图片设为桌面壁纸";
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "设置壁纸失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void DeleteCurrentFile()
        {
            if (_index < 0 || _index >= _files.Count) return;
            string path = _files[_index];
            if (MessageBox.Show(this, "将把图片移入 Windows 回收站，之后可以从回收站恢复。是否继续？\n\n" + path, "删除图片", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                _canvas.ClearImage();
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                _files.RemoveAt(_index);
                _index = _files.Count == 0 ? -1 : Math.Min(_index, _files.Count - 1);
                RefreshThumbnailList();
                DisplayCurrent();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void RenameCurrentFile()
        {
            if (_index < 0 || _index >= _files.Count) return;
            string path = _files[_index];
            using (SimpleInputDialog dialog = new SimpleInputDialog("重命名图片", "新文件名（不含扩展名）", Path.GetFileNameWithoutExtension(path)))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || String.IsNullOrWhiteSpace(dialog.Value)) return;
                string destination = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(dialog.Value) + Path.GetExtension(path));
                if (File.Exists(destination)) { MessageBox.Show(this, "目标文件已存在。", "重命名失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                try
                {
                    _canvas.ClearImage();
                    File.Move(path, destination);
                    _files[_index] = destination;
                    RefreshThumbnailList();
                    DisplayCurrent();
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "重命名失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private ToolStrip BuildToolbar()
        {
            ToolStrip bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(18, 18), Padding = new Padding(6, 4, 6, 4) };
            bar.Items.Add(Button("打开", "打开图片文件", delegate { OpenFiles(); }));
            bar.Items.Add(Button("文件夹", "打开文件夹中的图片", delegate { OpenFolder(); }));
            bar.Items.Add(Button("编辑", "裁剪、调色、滤镜、标注、水印、尺寸等", delegate { OpenEditor(); }));
            bar.Items.Add(Button("批处理", "批量尺寸、裁剪、压缩、转换、旋转、命名、水印、复制、移动、回收站", delegate { OpenBatchTools(); }));
            bar.Items.Add(Button("信息", "查看尺寸、文件属性与 EXIF", delegate { ShowCurrentInfo(); }));
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(new ToolStripLabel("排序"));
            ToolStripComboBox sort = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Width = 170, BackColor = PhotoEditorForm.Paper, ForeColor = PhotoEditorForm.Navy, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular), Margin = new Padding(3, 2, 3, 2) };
            sort.Items.AddRange(new object[] { "文件日期  新→旧", "文件日期  旧→新", "拍摄日期  新→旧", "拍摄日期  旧→新", "文件名  A→Z", "文件名  Z→A", "文件大小  大→小", "文件大小  小→大" });
            sort.SelectedIndex = (int)_sortMode;
            sort.SelectedIndexChanged += delegate
            {
                if (sort.SelectedIndex < 0) return;
                _sortMode = (ImageSortMode)sort.SelectedIndex;
                ResortCurrentFiles();
            };
            bar.Items.Add(sort);
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(Button("◀", "上一张 (←)", delegate { Navigate(-1, false); }));
            bar.Items.Add(Button("▶", "下一张 (→)", delegate { Navigate(1, false); }));
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(Button("－", "缩小 (−)", delegate { _canvas.ZoomBy(1.0 / 1.25); UpdateStatus(); }));
            bar.Items.Add(Button("＋", "放大 (+)", delegate { _canvas.ZoomBy(1.25); UpdateStatus(); }));
            bar.Items.Add(Button("适应", "适应窗口 (F)", delegate { _canvas.SetFit(); UpdateStatus(); }));
            bar.Items.Add(Button("100%", "原始大小 (0)", delegate { _canvas.SetOriginalSize(); UpdateStatus(); }));
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(Button("↶", "向左旋转 (Shift+R)", delegate { _canvas.Rotate(-90); UpdateStatus(); }));
            bar.Items.Add(Button("↷", "向右旋转 (R)", delegate { _canvas.Rotate(90); UpdateStatus(); }));
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(Button("幻灯片", "播放/停止幻灯片 (Space)", delegate { ToggleSlideshow(); }));
            bar.Items.Add(Button("全屏", "切换全屏 (F11)", delegate { ToggleFullScreen(); }));
            bar.Items.Add(new ToolStripSeparator());
            bar.Items.Add(Button("打印", "打印 (Ctrl+P)", delegate { PrintImage(); }));
            bar.Items.Add(Button("另存为", "将当前画面另存为副本 (Ctrl+S)", delegate { SaveAs(); }));
            return bar;
        }

        private void DrawThumbnailItem(object sender, DrawListViewItemEventArgs e)
        {
            Rectangle bounds = e.Bounds;
            if (bounds.Width < 12 || bounds.Height < 18) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle card = Rectangle.Inflate(bounds, -2, -2);
            Color cardColor = e.Item.Selected ? Color.FromArgb(225, 241, 233) : Color.FromArgb(255, 253, 248);
            using (Brush background = new SolidBrush(cardColor)) e.Graphics.FillRectangle(background, card);

            int imageWidth = Math.Min(92, Math.Max(24, bounds.Width - 18));
            int imageHeight = Math.Min(72, Math.Max(24, bounds.Height - 38));
            Rectangle imageBounds = new Rectangle(bounds.Left + (bounds.Width - imageWidth) / 2, bounds.Top + 7, imageWidth, imageHeight);
            if (e.Item.ImageIndex >= 0 && e.Item.ImageIndex < _thumbnailImages.Images.Count)
            {
                Image image = _thumbnailImages.Images[e.Item.ImageIndex];
                e.Graphics.DrawImage(image, imageBounds);
            }
            using (Pen imageFrame = new Pen(Color.FromArgb(210, 197, 172), 1f))
                e.Graphics.DrawRectangle(imageFrame, imageBounds);

            Rectangle caption = new Rectangle(bounds.Left + 4, imageBounds.Bottom + 4, bounds.Width - 8, Math.Max(14, bounds.Bottom - imageBounds.Bottom - 7));
            TextRenderer.DrawText(e.Graphics, e.Item.Text, new Font("Microsoft YaHei UI", 8f, FontStyle.Regular), caption, PhotoEditorForm.Navy, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            using (Pen outline = new Pen(e.Item.Selected ? PhotoEditorForm.Coral : Color.FromArgb(222, 214, 197), e.Item.Selected ? 2f : 1f))
                e.Graphics.DrawRectangle(outline, card);
        }

        private static ToolStripButton Button(string text, string tip, EventHandler action)
        {
            ToolStripButton button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = tip, AutoSize = true };
            button.Click += action;
            return button;
        }

        private void OpenFiles()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "打开图片";
                dialog.Filter = ImageDecoder.OpenFileFilter;
                dialog.Multiselect = true;
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    OpenSelected(dialog.FileNames);
            }
        }

        private void OpenFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择包含图片的文件夹";
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadFolder(dialog.SelectedPath, null);
            }
        }

        private void OpenArgument(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    LoadFolder(path, null);
                else if (File.Exists(path))
                    OpenSingleFileFast(path);
                else
                    UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "无法打开", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenSelected(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            string first = Path.GetFullPath(paths[0]);
            if (paths.Length == 1 && Directory.Exists(first))
            {
                LoadFolder(first, null);
                return;
            }

            if (paths.Length == 1 && File.Exists(first))
            {
                OpenSingleFileFast(first);
                return;
            }

            _files.Clear();
            foreach (string path in paths)
            {
                if (File.Exists(path) && IsSupportedImage(path))
                    _files.Add(Path.GetFullPath(path));
            }
            SortFiles(_files);
            _index = _files.Count > 0 ? 0 : -1;
            DisplayCurrent();
            RefreshThumbnailList();
        }

        private void LoadFolder(string folder, string selectedPath)
        {
            if (String.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;
            if (!_uiReady)
            {
                _pendingFolderPath = folder;
                _pendingSelectedPath = selectedPath;
                _statusText.Text = "正在读取文件夹图片…";
                return;
            }
            QueueFolderScan(folder, selectedPath);
        }

        private void OpenSingleFileFast(string path)
        {
            string fullPath = Path.GetFullPath(path);
            _files.Clear();
            _files.Add(fullPath);
            _index = 0;
            DisplayCurrent();
            RefreshThumbnailList(false);

            string folder = Path.GetDirectoryName(fullPath);
            if (!_uiReady)
            {
                _pendingFolderPath = folder;
                _pendingSelectedPath = fullPath;
                _statusText.Text = "图片已打开，正在读取同文件夹图片…";
            }
            else
                QueueFolderScan(folder, fullPath);
        }

        private void QueueFolderScan(string folder, string selectedPath)
        {
            CancelFolderScan();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _folderScanCancellation = cancellation;
            CancellationToken token = cancellation.Token;
            int generation = ++_folderScanGeneration;
            _statusText.Text = "图片已打开，正在读取同文件夹图片…";

            Task.Run(delegate
            {
                List<string> images = new List<string>();
                string error = null;
                try
                {
                    foreach (string candidate in Directory.EnumerateFiles(folder, "*", System.IO.SearchOption.TopDirectoryOnly))
                    {
                        if (token.IsCancellationRequested) return;
                        if (IsSupportedImage(candidate)) images.Add(Path.GetFullPath(candidate));
                    }
                    SortFiles(images);
                }
                catch (Exception ex) { error = ex.Message; }

                if (token.IsCancellationRequested || IsDisposed || !IsHandleCreated) return;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || token.IsCancellationRequested || generation != _folderScanGeneration) return;
                        if (error != null)
                        {
                            _statusText.Text = "图片已打开；读取同文件夹列表失败：" + error;
                            StartThumbnailGeneration();
                            return;
                        }

                        _files.Clear();
                        _files.AddRange(images);
                        _index = selectedPath == null ? 0 : _files.FindIndex(delegate(string item)
                        {
                            return String.Equals(item, Path.GetFullPath(selectedPath), StringComparison.OrdinalIgnoreCase);
                        });
                        if (_index < 0 && _files.Count > 0) _index = 0;

                        if (_files.Count == 0)
                        {
                            _index = -1;
                            _canvas.ClearImage();
                        }
                        else if (selectedPath == null || !String.Equals(_files[_index], selectedPath, StringComparison.OrdinalIgnoreCase))
                            DisplayCurrent();
                        else
                            UpdateStatus();

                        RefreshThumbnailList();
                    });
                }
                catch (InvalidOperationException) { }
            }, token);
        }

        private void ResortCurrentFiles()
        {
            if (_files.Count < 2) return;
            string selectedPath = _index >= 0 && _index < _files.Count ? _files[_index] : null;
            SortFiles(_files);
            _index = selectedPath == null ? 0 : _files.FindIndex(delegate(string path)
            {
                return String.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase);
            });
            if (_index < 0 && _files.Count > 0) _index = 0;
            RefreshThumbnailList();
            DisplayCurrent();
        }

        private sealed class ImageSortData
        {
            public DateTime Modified;
            public DateTime Captured;
            public long Length;
        }

        private void SortFiles(List<string> files)
        {
            Dictionary<string, ImageSortData> metadata = new Dictionary<string, ImageSortData>(StringComparer.OrdinalIgnoreCase);
            bool useCaptureDate = _sortMode == ImageSortMode.CaptureNewestFirst || _sortMode == ImageSortMode.CaptureOldestFirst;
            foreach (string path in files)
            {
                ImageSortData data = new ImageSortData { Modified = DateTime.MinValue, Captured = DateTime.MinValue, Length = -1 };
                try
                {
                    FileInfo info = new FileInfo(path);
                    data.Modified = info.LastWriteTime;
                    data.Length = info.Length;
                }
                catch { }
                data.Captured = useCaptureDate ? ReadCaptureDate(path, data.Modified) : data.Modified;
                metadata[path] = data;
            }

            files.Sort(delegate(string left, string right)
            {
                ImageSortData a = metadata[left];
                ImageSortData b = metadata[right];
                int comparison = 0;
                switch (_sortMode)
                {
                    case ImageSortMode.ModifiedNewestFirst: comparison = b.Modified.CompareTo(a.Modified); break;
                    case ImageSortMode.ModifiedOldestFirst: comparison = a.Modified.CompareTo(b.Modified); break;
                    case ImageSortMode.CaptureNewestFirst: comparison = b.Captured.CompareTo(a.Captured); break;
                    case ImageSortMode.CaptureOldestFirst: comparison = a.Captured.CompareTo(b.Captured); break;
                    case ImageSortMode.NameAscending: comparison = StringComparer.CurrentCultureIgnoreCase.Compare(Path.GetFileName(left), Path.GetFileName(right)); break;
                    case ImageSortMode.NameDescending: comparison = StringComparer.CurrentCultureIgnoreCase.Compare(Path.GetFileName(right), Path.GetFileName(left)); break;
                    case ImageSortMode.SizeDescending: comparison = b.Length.CompareTo(a.Length); break;
                    case ImageSortMode.SizeAscending: comparison = a.Length.CompareTo(b.Length); break;
                }
                if (comparison == 0)
                    comparison = StringComparer.CurrentCultureIgnoreCase.Compare(Path.GetFileName(left), Path.GetFileName(right));
                return comparison;
            });
        }

        private static DateTime ReadCaptureDate(string path, DateTime fallback)
        {
            try
            {
                using (DecodedImage decoded = ImageDecoder.Load(path))
                {
                    Image image = decoded.Image;
                    int[] tags = new int[] { 0x9003, 0x9004, 0x0132 };
                    foreach (int tag in tags)
                    {
                        try
                        {
                            PropertyItem item = image.GetPropertyItem(tag);
                            string text = System.Text.Encoding.ASCII.GetString(item.Value).Trim('\0', ' ');
                            DateTime value;
                            string[] formats = new string[] { "yyyy:MM:dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
                            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                                return value;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return fallback;
        }

        private void RefreshThumbnailList(bool startThumbnailWork = true)
        {
            CancelThumbnailGeneration();
            int generation = ++_thumbnailGeneration;
            _syncingSelection = true;
            _thumbnailList.BeginUpdate();
            try
            {
                _thumbnailList.Items.Clear();
                _thumbnailList.LargeImageList = null;
                _thumbnailImages.Images.Clear();
                foreach (Bitmap oldBitmap in _thumbnailBitmapOwners) oldBitmap.Dispose();
                _thumbnailBitmapOwners.Clear();

                Bitmap placeholder = CreateThumbnailPlaceholder();
                _thumbnailBitmapOwners.Add(placeholder);
                _thumbnailImages.Images.Add(placeholder);
                for (int i = 0; i < _files.Count; i++)
                    _thumbnailList.Items.Add(new ListViewItem(Path.GetFileName(_files[i]), 0) { Tag = i });

                if (_index >= 0 && _index < _thumbnailList.Items.Count)
                {
                    _thumbnailList.Items[_index].Selected = true;
                    _thumbnailList.Items[_index].Focused = true;
                }
            }
            finally
            {
                if (_thumbnailList.LargeImageList == null) _thumbnailList.LargeImageList = _thumbnailImages;
                _thumbnailList.EndUpdate();
                _syncingSelection = false;
            }

            if (startThumbnailWork && _uiReady && generation == _thumbnailGeneration)
                StartThumbnailGeneration();
            PositionFloatingNavigationButtons();
        }

        private static Bitmap CreateThumbnailPlaceholder()
        {
            Bitmap placeholder = new Bitmap(92, 72);
            using (Graphics g = Graphics.FromImage(placeholder))
            using (Brush brush = new SolidBrush(Color.FromArgb(230, 232, 235)))
            using (Pen pen = new Pen(Color.FromArgb(185, 189, 195)))
            {
                g.Clear(Color.FromArgb(245, 246, 248));
                g.FillRectangle(brush, 18, 12, 56, 44);
                g.DrawRectangle(pen, 18, 12, 56, 44);
            }
            return placeholder;
        }

        private void StartThumbnailGeneration()
        {
            if (!_uiReady || IsDisposed || _files.Count == 0) return;
            CancelThumbnailGeneration();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _thumbnailCancellation = cancellation;
            CancellationToken token = cancellation.Token;
            int generation = _thumbnailGeneration;
            int limit = Math.Min(500, _files.Count);
            List<Tuple<int, string>> work = new List<Tuple<int, string>>(limit);
            if (_index >= 0 && _index < limit)
                work.Add(Tuple.Create(_index, _files[_index]));
            for (int i = 0; i < limit; i++)
                if (i != _index) work.Add(Tuple.Create(i, _files[i]));
            Size imageSize = _thumbnailImages.ImageSize;

            Task.Run(delegate
            {
                List<Tuple<int, Bitmap>> batch = new List<Tuple<int, Bitmap>>(8);
                foreach (Tuple<int, string> item in work)
                {
                    if (token.IsCancellationRequested) break;
                    Bitmap thumb = CreateThumbnail(item.Item2, imageSize);
                    if (thumb == null) continue;
                    batch.Add(Tuple.Create(item.Item1, thumb));
                    if (batch.Count >= 8)
                    {
                        PostThumbnailBatch(batch, generation, token);
                        batch = new List<Tuple<int, Bitmap>>(8);
                    }
                }
                if (batch.Count > 0) PostThumbnailBatch(batch, generation, token);
            }, token);
        }

        private static Bitmap CreateThumbnail(string path, Size size)
        {
            try
            {
                using (Bitmap source = ImageDecoder.LoadBitmap(path))
                {
                    Bitmap thumb = new Bitmap(size.Width, size.Height);
                    try
                    {
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            g.Clear(Color.FromArgb(245, 246, 248));
                            double scale = Math.Min((double)thumb.Width / source.Width, (double)thumb.Height / source.Height);
                            int w = Math.Max(1, (int)(source.Width * scale));
                            int h = Math.Max(1, (int)(source.Height * scale));
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(source, (thumb.Width - w) / 2, (thumb.Height - h) / 2, w, h);
                        }
                        return thumb;
                    }
                    catch
                    {
                        thumb.Dispose();
                        throw;
                    }
                }
            }
            catch { return null; }
        }

        private void PostThumbnailBatch(List<Tuple<int, Bitmap>> batch, int generation, CancellationToken token)
        {
            List<Tuple<int, Bitmap>> pending = batch;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (IsDisposed || token.IsCancellationRequested || generation != _thumbnailGeneration)
                    {
                        DisposeThumbnailBatch(pending);
                        return;
                    }

                    foreach (Tuple<int, Bitmap> item in pending)
                    {
                        bool retained = false;
                        try
                        {
                            if (item.Item1 < 0 || item.Item1 >= _thumbnailList.Items.Count)
                            {
                                item.Item2.Dispose();
                                continue;
                            }
                            int imageIndex = _thumbnailImages.Images.Count;
                            _thumbnailImages.Images.Add(item.Item2);
                            _thumbnailBitmapOwners.Add(item.Item2);
                            retained = true;
                            _thumbnailList.Items[item.Item1].ImageIndex = imageIndex;
                        }
                        catch
                        {
                            if (!retained) item.Item2.Dispose();
                        }
                    }
                    pending.Clear();
                });
            }
            catch (InvalidOperationException) { DisposeThumbnailBatch(pending); }
        }

        private static void DisposeThumbnailBatch(List<Tuple<int, Bitmap>> batch)
        {
            foreach (Tuple<int, Bitmap> item in batch)
                if (item.Item2 != null) item.Item2.Dispose();
            batch.Clear();
        }

        private void CancelThumbnailGeneration()
        {
            if (_thumbnailCancellation == null) return;
            _thumbnailCancellation.Cancel();
            _thumbnailCancellation.Dispose();
            _thumbnailCancellation = null;
        }

        private void CancelFolderScan()
        {
            if (_folderScanCancellation == null) return;
            _folderScanCancellation.Cancel();
            _folderScanCancellation.Dispose();
            _folderScanCancellation = null;
        }

        private void ThumbnailSelectionChanged(object sender, EventArgs e)
        {
            if (_syncingSelection || _thumbnailList.FocusedItem == null) return;
            int selected = _thumbnailList.FocusedItem.Index;
            if (selected >= 0 && selected < _files.Count && selected != _index)
            {
                _index = selected;
                DisplayCurrent();
            }
        }

        private void SyncThumbnailSelection()
        {
            if (_index < 0 || _index >= _thumbnailList.Items.Count) return;
            _syncingSelection = true;
            try
            {
                bool currentInMultiSelection = _thumbnailList.SelectedIndices.Count > 1 && _thumbnailList.Items[_index].Selected;
                if (!currentInMultiSelection)
                {
                    _thumbnailList.SelectedIndices.Clear();
                    _thumbnailList.Items[_index].Selected = true;
                }
                _thumbnailList.Items[_index].Focused = true;
                _thumbnailList.EnsureVisible(_index);
            }
            finally { _syncingSelection = false; }
        }

        private static bool IsSupportedImage(string path)
        {
            return ImageDecoder.IsSupportedPath(path);
        }

        private void DisplayCurrent()
        {
            _slideTimer.Stop();
            if (_index < 0 || _index >= _files.Count)
            {
                _canvas.ClearImage();
                UpdateStatus();
                return;
            }

            string path = _files[_index];
            Stream stream = null;
            Image image = null;
            try
            {
                using (DecodedImage decoded = ImageDecoder.Load(path))
                {
                    image = decoded.TakeImage();
                    stream = decoded.TakeStream();
                    _canvas.SetImage(image, stream);
                    image = null;
                    stream = null;
                }
                Text = Path.GetFileName(path) + " — " + AppBranding.ProductName;
            }
            catch (Exception ex)
            {
                if (image != null) image.Dispose();
                if (stream != null) stream.Dispose();
                _canvas.ClearImage();
                _statusText.Text = "无法解码图片：" + Path.GetFileName(path);
                Text = AppBranding.ProductName;
                MessageBox.Show(this, "无法打开这张图片。\n\n" + ex.Message, "无法解码图片", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            SyncThumbnailSelection();
            UpdateStatus();
        }

        private void Navigate(int delta, bool wrap)
        {
            if (_files.Count < 2)
                return;
            int next = _index + delta;
            if (wrap)
            {
                if (next < 0) next = _files.Count - 1;
                if (next >= _files.Count) next = 0;
            }
            else if (next < 0 || next >= _files.Count)
            {
                return;
            }
            _index = next;
            DisplayCurrent();
            if (wrap)
                _slideTimer.Start();
        }

        private void ToggleSlideshow()
        {
            if (_canvas.CurrentImage == null || _files.Count < 2)
                return;
            if (_slideTimer.Enabled)
            {
                _slideTimer.Stop();
                _statusText.Text = "幻灯片已暂停";
            }
            else
            {
                if (!_fullScreen)
                    ToggleFullScreen();
                _statusText.Text = "正在播放幻灯片 · Space 停止";
                _slideTimer.Start();
            }
        }

        private void ToggleFullScreen()
        {
            if (!_fullScreen)
            {
                _savedBorderStyle = FormBorderStyle;
                _savedWindowState = WindowState;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                _fullScreen = true;
                MainMenuStrip.Visible = false;
                _toolbar.Visible = false;
                _status.Visible = false;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                FormBorderStyle = _savedBorderStyle;
                _fullScreen = false;
                MainMenuStrip.Visible = true;
                _toolbar.Visible = true;
                _status.Visible = true;
                WindowState = _savedWindowState;
                _canvas.SetFit();
            }
            _canvas.Focus();
            UpdateStatus();
        }

        private void SaveAs()
        {
            Image current = _canvas.CurrentImage;
            if (current == null)
                return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "将当前画面另存为副本";
                dialog.FileName = Path.GetFileNameWithoutExtension(_files[_index]) + "_copy.png";
                dialog.Filter = "PNG 图片|*.png|JPEG 图片|*.jpg;*.jpeg|BMP 图片|*.bmp|GIF 图片|*.gif|TIFF 图片|*.tif;*.tiff";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    ImageAnimator.UpdateFrames(current);
                    using (Bitmap copy = new Bitmap(current))
                    {
                        int rotation = _canvas.Rotation % 360;
                        if (rotation == 90) copy.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        else if (rotation == 180) copy.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        else if (rotation == 270) copy.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        copy.Save(dialog.FileName, GetImageFormat(dialog.FileName));
                    }
                    _statusText.Text = "已另存副本：" + Path.GetFileName(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "另存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static ImageFormat GetImageFormat(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg") return ImageFormat.Jpeg;
            if (ext == ".bmp") return ImageFormat.Bmp;
            if (ext == ".gif") return ImageFormat.Gif;
            if (ext == ".tif" || ext == ".tiff") return ImageFormat.Tiff;
            return ImageFormat.Png;
        }

        private void PrintImage()
        {
            Image current = _canvas.CurrentImage;
            if (current == null)
                return;
            using (PrintDocument document = new PrintDocument())
            {
                document.DocumentName = Path.GetFileName(_files[_index]);
                document.PrintPage += delegate(object sender, PrintPageEventArgs e)
                {
                    float originalW = (_canvas.Rotation % 180 == 0) ? current.Width : current.Height;
                    float originalH = (_canvas.Rotation % 180 == 0) ? current.Height : current.Width;
                    float scale = Math.Min(e.MarginBounds.Width / originalW, e.MarginBounds.Height / originalH);
                    float drawW = originalW * scale;
                    float drawH = originalH * scale;
                    GraphicsState state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(e.MarginBounds.Left + e.MarginBounds.Width / 2f, e.MarginBounds.Top + e.MarginBounds.Height / 2f);
                    e.Graphics.RotateTransform(_canvas.Rotation);
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(current, new RectangleF(-current.Width * scale / 2f, -current.Height * scale / 2f, current.Width * scale, current.Height * scale));
                    e.Graphics.Restore(state);
                    e.HasMorePages = false;
                };
                using (PrintDialog dialog = new PrintDialog())
                {
                    dialog.Document = document;
                    dialog.UseEXDialog = true;
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        try { document.Print(); }
                        catch (Exception ex) { MessageBox.Show(this, ex.Message, "打印失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
                }
            }
        }

        private void CanvasDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void CanvasDragDrop(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0)
                return;
            if (paths.Length == 1 && Directory.Exists(paths[0]))
                LoadFolder(paths[0], null);
            else
                OpenSelected(paths);
        }

        private void UpdateStatus()
        {
            if (_index >= 0 && _index < _files.Count && _canvas.CurrentImage != null)
            {
                Image image = _canvas.CurrentImage;
                _statusText.Text = String.Format("{0} / {1}    {2} × {3}    {4}%    {5}", _index + 1, _files.Count, image.Width, image.Height, _canvas.ZoomPercent, Path.GetFileName(_files[_index]));
            }
            else if (_index >= 0 && _index < _files.Count)
            {
                _statusText.Text = String.Format("{0} / {1}    {2}", _index + 1, _files.Count, Path.GetFileName(_files[_index]));
            }
            else
            {
                _statusText.Text = "打开图片或文件夹，也可以把图片拖到窗口里";
            }
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(this,
                "← / →：上一张 / 下一张\n滚轮：以鼠标位置为中心缩放图片\n+ / −：放大 / 缩小\nF：适应窗口    0：原始大小\nR / Shift + R：向右 / 向左旋转\nSpace：播放 / 停止幻灯片\nF11：全屏    Esc：退出全屏\nCtrl + O：打开图片    Ctrl + Shift + O：打开文件夹\nCtrl + S：另存为副本    Ctrl + P：打印",
                "快捷键", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PositionFloatingNavigationButtons()
        {
            if (_canvas == null || _previousImageButton == null || _nextImageButton == null) return;
            if (_canvas.CurrentImage == null)
            {
                _previousImageButton.Visible = false;
                _nextImageButton.Visible = false;
                return;
            }

            const int buttonWidth = 48;
            const int buttonHeight = 76;
            int maxX = Math.Max(0, _canvas.ClientSize.Width - buttonWidth);
            int left = 0;
            int right = maxX;
            int maxY = Math.Max(0, _canvas.ClientSize.Height - buttonHeight);
            int y = Math.Max(0, Math.Min(maxY, (_canvas.ClientSize.Height - buttonHeight) / 2));

            _previousImageButton.Bounds = new Rectangle(left, y, buttonWidth, buttonHeight);
            _nextImageButton.Bounds = new Rectangle(right, y, buttonWidth, buttonHeight);
            _previousImageButton.Enabled = _files.Count > 1 && _index > 0;
            _nextImageButton.Enabled = _files.Count > 1 && _index >= 0 && _index < _files.Count - 1;
            _previousImageButton.Visible = true;
            _nextImageButton.Visible = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            bool control = (keyData & Keys.Control) != 0;
            bool shift = (keyData & Keys.Shift) != 0;
            if (control && shift && key == Keys.O) { OpenFolder(); return true; }
            if (control && key == Keys.O) { OpenFiles(); return true; }
            if (control && key == Keys.S) { SaveAs(); return true; }
            if (control && key == Keys.P) { PrintImage(); return true; }
            if (key == Keys.Left || key == Keys.PageUp) { Navigate(-1, false); return true; }
            if (key == Keys.Right || key == Keys.PageDown) { Navigate(1, false); return true; }
            if (key == Keys.Add || key == Keys.Oemplus) { _canvas.ZoomBy(1.25); UpdateStatus(); return true; }
            if (key == Keys.Subtract || key == Keys.OemMinus) { _canvas.ZoomBy(1.0 / 1.25); UpdateStatus(); return true; }
            if (key == Keys.D0 || key == Keys.NumPad0) { _canvas.SetOriginalSize(); UpdateStatus(); return true; }
            if (key == Keys.F) { _canvas.SetFit(); UpdateStatus(); return true; }
            if (key == Keys.R) { _canvas.Rotate(shift ? -90 : 90); UpdateStatus(); return true; }
            if (key == Keys.Space) { ToggleSlideshow(); return true; }
            if (key == Keys.F11) { ToggleFullScreen(); return true; }
            if (key == Keys.Escape)
            {
                if (_fullScreen) { ToggleFullScreen(); return true; }
                if (_slideTimer.Enabled) { _slideTimer.Stop(); UpdateStatus(); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    internal sealed class FloatingNavigationButton : Control
    {
        private readonly bool _pointsLeft;
        private bool _hovered;
        private bool _pressed;

        public FloatingNavigationButton(bool pointsLeft)
        {
            _pointsLeft = pointsLeft;
            Size = new Size(48, 76);
            Cursor = Cursors.Hand;
            TabStop = false;
            AccessibleRole = AccessibleRole.PushButton;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF box = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
            using (GraphicsPath shape = RoundedBox(box, 18f))
            using (SolidBrush background = new SolidBrush(Color.FromArgb(!Enabled ? 70 : (_pressed ? 220 : (_hovered ? 200 : 140)), 18, 24, 32)))
            using (Pen border = new Pen(Color.FromArgb(!Enabled ? 55 : (_hovered ? 210 : 120), 255, 255, 255), 1f))
            {
                e.Graphics.FillPath(background, shape);
                e.Graphics.DrawPath(border, shape);
            }

            float centerX = Width / 2f;
            float centerY = Height / 2f;
            float direction = _pointsLeft ? -1f : 1f;
            PointF[] chevron = new PointF[]
            {
                new PointF(centerX - direction * 5f, centerY - 10f),
                new PointF(centerX + direction * 5f, centerY),
                new PointF(centerX - direction * 5f, centerY + 10f)
            };
            using (Pen arrow = new Pen(Color.FromArgb(Enabled ? 245 : 110, 255, 255, 255), 3f))
            {
                arrow.StartCap = LineCap.Round;
                arrow.EndCap = LineCap.Round;
                e.Graphics.DrawLines(arrow, chevron);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            _pressed = true;
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool click = _pressed && ClientRectangle.Contains(e.Location) && Enabled;
            _pressed = false;
            Capture = false;
            Invalidate();
            if (click) OnClick(EventArgs.Empty);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        private static GraphicsPath RoundedBox(RectangleF rect, float radius)
        {
            float d = radius * 2f;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, d, d, 180f, 90f);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270f, 90f);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class ImageCanvas : Control
    {
        private Image _image;
        private Stream _stream;
        private bool _fit = true;
        private double _zoom = 1.0;
        private int _rotation;
        private float _panX;
        private float _panY;
        private bool _dragging;
        private Point _lastMouse;
        private readonly EventHandler _frameChanged;

        public event Action ViewChanged;

        public Image CurrentImage { get { return _image; } }
        public int Rotation { get { return _rotation; } }

        public int ZoomPercent
        {
            get { return (int)Math.Round(EffectiveScale() * 100.0); }
        }

        public ImageCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = true;
            MouseEnter += delegate { Focus(); };
            _frameChanged = new EventHandler(FrameChanged);
        }

        public void SetImage(Image image, Stream keepAlive)
        {
            ClearImage();
            _image = image;
            _stream = keepAlive;
            _fit = true;
            _zoom = 1.0;
            _rotation = 0;
            _panX = _panY = 0;
            if (_image != null && ImageAnimator.CanAnimate(_image))
                ImageAnimator.Animate(_image, _frameChanged);
            Invalidate();
            RaiseChanged();
        }

        public void ClearImage()
        {
            if (_image != null)
            {
                ImageAnimator.StopAnimate(_image, _frameChanged);
                _image.Dispose();
                _image = null;
            }
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
            _rotation = 0;
            _panX = _panY = 0;
            Invalidate();
            RaiseChanged();
        }

        public void SetFit()
        {
            _fit = true;
            _panX = _panY = 0;
            Invalidate();
            RaiseChanged();
        }

        public void SetOriginalSize()
        {
            _fit = false;
            _zoom = 1.0;
            _panX = _panY = 0;
            Invalidate();
            RaiseChanged();
        }

        public void ZoomBy(double factor)
        {
            ZoomAt(factor, new Point(ClientSize.Width / 2, ClientSize.Height / 2));
        }

        public void ZoomAt(double factor, Point anchor)
        {
            if (_image == null)
                return;
            if (_fit)
            {
                _zoom = FitScale();
                _fit = false;
            }
            double oldZoom = _zoom;
            double newZoom = Math.Max(0.05, Math.Min(16.0, oldZoom * factor));
            double actualFactor = newZoom / oldZoom;
            _panX += (float)((1.0 - actualFactor) * (anchor.X - ClientSize.Width / 2.0 - _panX));
            _panY += (float)((1.0 - actualFactor) * (anchor.Y - ClientSize.Height / 2.0 - _panY));
            _zoom = newZoom;
            Invalidate();
            RaiseChanged();
        }

        public void Rotate(int degrees)
        {
            if (_image == null)
                return;
            _rotation = (_rotation + degrees) % 360;
            if (_rotation < 0) _rotation += 360;
            Invalidate();
            RaiseChanged();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            if (_image == null)
            {
                float centerX = ClientSize.Width / 2f;
                float centerY = ClientSize.Height / 2f;
                using (Brush circle = new SolidBrush(Color.FromArgb(30, PhotoEditorForm.Mint)))
                    e.Graphics.FillEllipse(circle, centerX + 110f, centerY - 96f, 136f, 136f);
                using (Pen dots = new Pen(Color.FromArgb(75, PhotoEditorForm.Paper), 1f))
                    for (int y = -52; y <= 52; y += 11)
                        for (int x = -52; x <= 52; x += 11)
                            e.Graphics.DrawEllipse(dots, centerX + x, centerY + y, 2f, 2f);
                using (Font kicker = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold))
                using (Font title = new Font("Microsoft YaHei UI", 22f, FontStyle.Bold))
                using (Font hint = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular))
                using (Brush coral = new SolidBrush(PhotoEditorForm.Coral))
                using (Brush paper = new SolidBrush(PhotoEditorForm.Paper))
                using (Brush muted = new SolidBrush(Color.FromArgb(192, 204, 207)))
                using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString("TOKYO / PHOTO STUDIO", kicker, coral, new RectangleF(0, centerY - 68f, ClientSize.Width, 24f), format);
                    e.Graphics.DrawString("打开图片，开始浏览", title, paper, new RectangleF(0, centerY - 32f, ClientSize.Width, 42f), format);
                    e.Graphics.DrawString("拖入图片或文件夹    ·    滚轮缩放    ·    左右切换", hint, muted, new RectangleF(0, centerY + 24f, ClientSize.Width, 28f), format);
                }
                return;
            }

            ImageAnimator.UpdateFrames(_image);
            double scale = EffectiveScale();
            float width = (float)(_image.Width * scale);
            float height = (float)(_image.Height * scale);
            GraphicsState state = e.Graphics.Save();
            e.Graphics.TranslateTransform(ClientSize.Width / 2f + _panX, ClientSize.Height / 2f + _panY);
            e.Graphics.RotateTransform(_rotation);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.DrawImage(_image, new RectangleF(-width / 2f, -height / 2f, width, height));
            e.Graphics.Restore(state);
        }

        private double EffectiveScale()
        {
            if (_image == null)
                return 1.0;
            return _fit ? FitScale() : _zoom;
        }

        private double FitScale()
        {
            if (_image == null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return 1.0;
            double width = (_rotation % 180 == 0) ? _image.Width : _image.Height;
            double height = (_rotation % 180 == 0) ? _image.Height : _image.Width;
            return Math.Max(0.01, Math.Min(ClientSize.Width / width, ClientSize.Height / height));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            double factor = Math.Pow(1.15, e.Delta / 120.0);
            ZoomAt(factor, e.Location);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left && _image != null)
            {
                _dragging = true;
                _lastMouse = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                _panX += e.X - _lastMouse.X;
                _panY += e.Y - _lastMouse.Y;
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = false;
                Cursor = Cursors.Default;
            }
        }

        private void FrameChanged(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                if (InvokeRequired)
                    BeginInvoke((MethodInvoker)delegate { if (!IsDisposed) Invalidate(); });
                else
                    Invalidate();
            }
            catch (InvalidOperationException) { }
        }

        private void RaiseChanged()
        {
            if (ViewChanged != null)
                ViewChanged();
        }
    }
}
