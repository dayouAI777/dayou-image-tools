using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace DayouImageToolsUpdater
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UpdaterForm(args));
        }
    }

    internal sealed class UpdateFile
    {
        public string Source;
        public string Target;
        public string Sha256;
        public long Size;
    }

    internal sealed class UpdatePackageInfo
    {
        public string Path;
        public Version Version;
        public Version MinimumUpdaterVersion;
        public readonly List<UpdateFile> Files = new List<UpdateFile>();
    }

    internal static class UpdatePackageReader
    {
        private const long MaximumFileSize = 1024L * 1024L * 1024L;
        private const long MaximumPackageSize = 2L * 1024L * 1024L * 1024L;
        private const int MaximumFileCount = 1000;
        private const string ManifestName = "dayou-update.xml";

        public static UpdatePackageInfo Read(string packagePath, string installDirectory)
        {
            if (String.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
                throw new FileNotFoundException("找不到更新包。", packagePath);

            string root = Path.GetFullPath(installDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            using (ZipArchive archive = ZipFile.OpenRead(packagePath))
            {
                Dictionary<string, ZipArchiveEntry> entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
                ZipArchiveEntry manifest = null;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string name = entry.FullName.Replace('\\', '/');
                    if (name.Equals(ManifestName, StringComparison.OrdinalIgnoreCase))
                        manifest = entry;
                    if (name.EndsWith("/", StringComparison.Ordinal))
                        continue;
                    if (!entries.ContainsKey(name))
                        entries.Add(name, entry);
                }

                if (manifest == null)
                    throw new InvalidDataException("包内没有根目录清单 dayou-update.xml，无法识别为大有图片工具更新包。");

                XDocument document;
                XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using (Stream stream = manifest.Open())
                using (XmlReader reader = XmlReader.Create(stream, settings))
                    document = XDocument.Load(reader);

                XElement descriptor = document.Root;
                if (descriptor == null || descriptor.Name.LocalName != "dayouUpdate")
                    throw new InvalidDataException("更新清单根节点无效。");
                if (!String.Equals((string)descriptor.Attribute("product"), "DayouImageTools", StringComparison.Ordinal))
                    throw new InvalidDataException("这个更新包不是为“大有图片工具”制作的。");

                Version version;
                if (!Version.TryParse((string)descriptor.Attribute("version"), out version))
                    throw new InvalidDataException("更新清单缺少有效版本号。");
                version = NormalizeVersion(version);

                Version minimumUpdater = new Version(1, 0, 0, 0);
                string minText = (string)descriptor.Attribute("minimumUpdaterVersion");
                if (!String.IsNullOrWhiteSpace(minText))
                {
                    if (!Version.TryParse(minText, out minimumUpdater))
                        throw new InvalidDataException("更新清单中的 minimumUpdaterVersion 无效。");
                    minimumUpdater = NormalizeVersion(minimumUpdater);
                }

                UpdatePackageInfo result = new UpdatePackageInfo { Path = Path.GetFullPath(packagePath), Version = version, MinimumUpdaterVersion = minimumUpdater };
                HashSet<string> targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                long totalSize = 0;

                foreach (XElement element in descriptor.Elements())
                {
                    if (element.Name.LocalName != "file")
                        continue;
                    if (result.Files.Count >= MaximumFileCount)
                        throw new InvalidDataException("更新包文件数量超过限制。");

                    string source = ValidateRelativePath((string)element.Attribute("source"), "包内源路径");
                    string target = ValidateRelativePath((string)element.Attribute("target"), "安装目标路径");
                    string hash = ((string)element.Attribute("sha256") ?? String.Empty).Trim();
                    if (hash.Length != 64 || !IsHex(hash))
                        throw new InvalidDataException("文件“" + target + "”缺少有效的 SHA-256 校验值。");
                    if (target.Equals("DayouImageToolsUpdater.exe", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("当前升级程序不能替换自身；更新包请勿包含 DayouImageToolsUpdater.exe。");
                    if (!targets.Add(target))
                        throw new InvalidDataException("更新清单重复指定了目标文件：" + target);

                    ZipArchiveEntry payload;
                    if (!entries.TryGetValue(source, out payload))
                        throw new InvalidDataException("包内找不到清单指定的文件：" + source);
                    if (payload.Length < 0 || payload.Length > MaximumFileSize)
                        throw new InvalidDataException("文件超过 1 GB 限制：" + source);
                    totalSize += payload.Length;
                    if (totalSize > MaximumPackageSize)
                        throw new InvalidDataException("更新包展开后超过 2 GB 限制。");

                    string destination = Path.GetFullPath(Path.Combine(root, target.Replace('/', Path.DirectorySeparatorChar)));
                    string rootPrefix = root + Path.DirectorySeparatorChar;
                    if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("安装目标超出了程序目录：" + target);

                    result.Files.Add(new UpdateFile { Source = source, Target = target, Sha256 = hash.ToLowerInvariant(), Size = payload.Length });
                }

                if (result.Files.Count == 0)
                    throw new InvalidDataException("更新清单里没有可部署的文件。");
                return result;
            }
        }

        public static void ExtractAndVerify(UpdatePackageInfo package, string stageDirectory, Action<string> log)
        {
            using (ZipArchive archive = ZipFile.OpenRead(package.Path))
            {
                Dictionary<string, ZipArchiveEntry> entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string name = entry.FullName.Replace('\\', '/');
                    if (!name.EndsWith("/", StringComparison.Ordinal) && !entries.ContainsKey(name))
                        entries.Add(name, entry);
                }

                foreach (UpdateFile file in package.Files)
                {
                    ZipArchiveEntry entry = entries[file.Source];
                    string outputPath = Path.GetFullPath(Path.Combine(stageDirectory, file.Target.Replace('/', Path.DirectorySeparatorChar)));
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);

                    FileInfo staged = new FileInfo(outputPath);
                    if (staged.Length != file.Size)
                        throw new InvalidDataException("解压后文件大小不符：" + file.Source);
                    string actualHash;
                    using (FileStream input = File.OpenRead(outputPath))
                    using (SHA256 sha = SHA256.Create())
                        actualHash = ToHex(sha.ComputeHash(input));
                    if (!String.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("SHA-256 校验失败：" + file.Source);
                    log("已校验：" + file.Target);
                }
            }
        }

        private static string ValidateRelativePath(string value, string description)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new InvalidDataException(description + "为空。");
            string path = value.Replace('\\', '/');
            if (path.StartsWith("/", StringComparison.Ordinal) || path.IndexOf(':') >= 0)
                throw new InvalidDataException(description + "必须是相对路径：" + value);
            string[] parts = path.Split('/');
            foreach (string part in parts)
            {
                if (part.Length == 0 || part == "." || part == "..")
                    throw new InvalidDataException(description + "包含不允许的路径段：" + value);
                foreach (char c in Path.GetInvalidFileNameChars())
                    if (part.IndexOf(c) >= 0)
                        throw new InvalidDataException(description + "包含无效字符：" + value);
                foreach (char c in part)
                    if (Char.IsControl(c))
                        throw new InvalidDataException(description + "包含控制字符：" + value);
            }
            return String.Join("/", parts);
        }

        private static bool IsHex(string value)
        {
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            return true;
        }

        internal static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", String.Empty).ToLowerInvariant();
        }

        internal static Version NormalizeVersion(Version version)
        {
            return new Version(Math.Max(0, version.Major), Math.Max(0, version.Minor), Math.Max(0, version.Build), Math.Max(0, version.Revision));
        }
    }

    internal sealed class UpdaterForm : Form
    {
        private const int MainShutdownTimeoutSeconds = 45;
        private const int FileCopyMaximumAttempts = 8;
        private readonly string _installDirectory;
        private readonly string _mainPath;
        private readonly TextBox _packagePath;
        private readonly Label _status;
        private readonly ListBox _fileList;
        private readonly Button _applyButton;
        private readonly Button _onlineButton;
        private UpdatePackageInfo _package;
        private GitHubReleaseInfo _onlineRelease;
        private Version _installedVersion;
        private bool _working;
        private bool _installationValid;

        public UpdaterForm(string[] args)
        {
            _installDirectory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _mainPath = Path.Combine(_installDirectory, "DayouImageTools.exe");
            Text = "大有图片工具升级程序";
            Width = 720;
            Height = 520;
            MinimumSize = new Size(620, 440);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 244, 235);
            ForeColor = Color.FromArgb(28, 42, 61);
            AllowDrop = true;

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            try
            {
                if (!File.Exists(_mainPath))
                    throw new FileNotFoundException("升级程序必须放在大有图片工具的安装目录中。", _mainPath);
                _installedVersion = ParseVersion(FileVersionInfo.GetVersionInfo(_mainPath).FileVersion);
                _installationValid = true;
            }
            catch (Exception ex)
            {
                _installedVersion = new Version(0, 0, 0, 0);
                MessageBox.Show(this, ex.Message, "无法读取已安装版本", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Label title = new Label
            {
                Text = "大有图片工具 · 更新中心",
                Dock = DockStyle.Top,
                Height = 62,
                Padding = new Padding(18, 14, 12, 0),
                Font = new Font("Microsoft YaHei UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 42, 61)
            };
            Label intro = new Label
            {
                Text = "拖入 .dayouupdate / ZIP 更新包，或选择文件。程序会读取包内清单，自动校验并部署到本安装目录。",
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(20, 0, 18, 6),
                Font = new Font("Microsoft YaHei UI", 9.5f),
                ForeColor = Color.FromArgb(80, 92, 104)
            };

            Panel pathPanel = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(18, 3, 18, 5) };
            _packagePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White };
            Button browse = new Button { Text = "选择更新包…", Dock = DockStyle.Right, Width = 126 };
            browse.Click += delegate { BrowsePackage(); };
            pathPanel.Controls.Add(_packagePath);
            pathPanel.Controls.Add(browse);

            _status = new Label
            {
                Text = "当前安装版本：" + _installedVersion + "。把更新包拖到此窗口即可识别。",
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(20, 8, 18, 0),
                ForeColor = Color.FromArgb(45, 98, 111)
            };

            _fileList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Panel listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 2, 18, 10) };
            listPanel.Controls.Add(_fileList);

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(18, 8, 18, 12) };
            Button close = new Button { Text = "关闭", Dock = DockStyle.Right, Width = 92, DialogResult = DialogResult.Cancel };
            _applyButton = new Button { Text = "开始升级", Dock = DockStyle.Right, Width = 120, Enabled = false, BackColor = Color.FromArgb(223, 91, 78), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _applyButton.FlatAppearance.BorderSize = 0;
            _applyButton.Click += delegate { BeginApply(); };
            _onlineButton = new Button { Text = "检查在线更新", Dock = DockStyle.Left, Width = 138 };
            _onlineButton.Click += delegate { OnOnlineButtonClick(); };
            bottom.Controls.Add(close);
            bottom.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            bottom.Controls.Add(_applyButton);
            bottom.Controls.Add(_onlineButton);

            Controls.Add(listPanel);
            Controls.Add(bottom);
            Controls.Add(_status);
            Controls.Add(pathPanel);
            Controls.Add(intro);
            Controls.Add(title);
            AcceptButton = _applyButton;
            CancelButton = close;

            DragEnter += OnPackageDragEnter;
            DragDrop += OnPackageDragDrop;
            pathPanel.DragEnter += OnPackageDragEnter;
            pathPanel.DragDrop += OnPackageDragDrop;
            listPanel.DragEnter += OnPackageDragEnter;
            listPanel.DragDrop += OnPackageDragDrop;
            _packagePath.AllowDrop = true;
            _packagePath.DragEnter += OnPackageDragEnter;
            _packagePath.DragDrop += OnPackageDragDrop;
            _fileList.AllowDrop = true;
            _fileList.DragEnter += OnPackageDragEnter;
            _fileList.DragDrop += OnPackageDragDrop;
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (_working) e.Cancel = true; };

            if (args != null && args.Length > 0)
                LoadPackage(args[0]);
        }

        private void BrowsePackage()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "选择大有图片工具更新包";
                dialog.Filter = "大有图片工具更新包|*.dayouupdate;*.zip|所有文件|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadPackage(dialog.FileName);
            }
        }

        private void OnOnlineButtonClick()
        {
            if (_working)
                return;
            if (_onlineRelease == null)
                CheckOnlineUpdates();
            else
                DownloadOnlineUpdate();
        }

        private void CheckOnlineUpdates()
        {
            if (!_installationValid)
            {
                MessageBox.Show(this, "当前目录不是有效的大有图片工具安装目录。", "无法检查更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _working = true;
            _onlineButton.Enabled = false;
            _status.ForeColor = Color.FromArgb(45, 98, 111);
            _status.Text = "正在连接 GitHub 检查更新…";
            Task.Run(delegate { return GitHubReleaseClient.GetLatestRelease(); }).ContinueWith(delegate(Task<GitHubReleaseInfo> task)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _working = false;
                        _onlineButton.Enabled = true;
                        if (task.IsFaulted)
                        {
                            Exception error = task.Exception == null ? new Exception("未知错误") : task.Exception.GetBaseException();
                            _status.ForeColor = Color.FromArgb(174, 61, 49);
                            _status.Text = "在线检查失败：" + error.Message;
                            return;
                        }

                        GitHubReleaseInfo release = task.Result;
                        if (release == null)
                        {
                            _onlineRelease = null;
                            _onlineButton.Text = "检查在线更新";
                            _status.Text = "GitHub 仓库还没有发布正式版本。";
                            return;
                        }
                        if (release.Version <= _installedVersion)
                        {
                            _onlineRelease = null;
                            _onlineButton.Text = "检查在线更新";
                            _status.Text = "当前已是最新版本：" + _installedVersion + "。";
                            return;
                        }

                        _onlineRelease = release;
                        _onlineButton.Text = "下载 v" + release.Version;
                        _status.Text = "发现新版本 " + release.Version + "（当前 " + _installedVersion + "），点击左下角按钮下载。";
                        _status.ForeColor = Color.FromArgb(45, 98, 111);
                        _fileList.Items.Clear();
                        _fileList.Items.Add("GitHub Release：" + release.Name);
                        _fileList.Items.Add("发布时间：" + release.PublishedAt);
                        _fileList.Items.Add("更新说明：");
                        string notes = String.IsNullOrWhiteSpace(release.Notes) ? "此版本没有附带更新说明。" : release.Notes;
                        string[] lines = notes.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                        int count = Math.Min(lines.Length, 80);
                        for (int i = 0; i < count; i++)
                            _fileList.Items.Add(lines[i]);
                        if (count < lines.Length)
                            _fileList.Items.Add("（更新说明较长，已截断显示。）");
                    });
                }
                catch (InvalidOperationException) { }
            });
        }

        private void DownloadOnlineUpdate()
        {
            GitHubReleaseInfo release = _onlineRelease;
            if (release == null || _working)
                return;

            _working = true;
            _onlineButton.Enabled = false;
            _applyButton.Enabled = false;
            _status.ForeColor = Color.FromArgb(45, 98, 111);
            _status.Text = "正在从 GitHub 下载版本 " + release.Version + "…";
            Task.Run(delegate
            {
                return GitHubReleaseClient.DownloadAsset(release, delegate(long received, long total)
                {
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (total > 0)
                                _status.Text = "正在下载更新：" + FormatBytes(received) + " / " + FormatBytes(total);
                            else
                                _status.Text = "正在下载更新：" + FormatBytes(received);
                        });
                    }
                    catch (InvalidOperationException) { }
                });
            }).ContinueWith(delegate(Task<string> task)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _working = false;
                        _onlineButton.Enabled = true;
                        _onlineButton.Text = "检查在线更新";
                        if (task.IsFaulted)
                        {
                            Exception error = task.Exception == null ? new Exception("未知错误") : task.Exception.GetBaseException();
                            _onlineButton.Text = "重试下载 v" + release.Version;
                            _applyButton.Enabled = _package != null;
                            _status.ForeColor = Color.FromArgb(174, 61, 49);
                            _status.Text = "下载失败：" + error.Message;
                            return;
                        }

                        Version expectedVersion = release.Version;
                        _onlineRelease = null;
                        LoadPackage(task.Result);
                        if (_package != null && _package.Version != expectedVersion)
                        {
                            _package = null;
                            _applyButton.Enabled = false;
                            _status.ForeColor = Color.FromArgb(174, 61, 49);
                            _status.Text = "下载包版本与 GitHub Release 标签不一致，已拒绝安装。";
                        }
                    });
                }
                catch (InvalidOperationException) { }
            });
        }

        private void OnPackageDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnPackageDragDrop(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null && paths.Length > 0)
                LoadPackage(paths[0]);
        }

        private void LoadPackage(string path)
        {
            _package = null;
            _applyButton.Enabled = false;
            _fileList.Items.Clear();
            _packagePath.Text = path;
            _status.ForeColor = Color.FromArgb(45, 98, 111);
            try
            {
                if (!_installationValid)
                    throw new InvalidOperationException("当前目录不是有效的大有图片工具安装目录，请从开始菜单的升级快捷方式启动。");
                UpdatePackageInfo package = UpdatePackageReader.Read(path, _installDirectory);
                Version updaterVersion = GetUpdaterVersion();
                if (package.MinimumUpdaterVersion > updaterVersion)
                    throw new InvalidDataException("此更新包要求升级程序版本至少为 " + package.MinimumUpdaterVersion + "，请先运行新版安装程序。");
                if (package.Version < _installedVersion)
                    throw new InvalidDataException("更新包版本 " + package.Version + " 低于当前版本 " + _installedVersion + "，已阻止降级覆盖。");

                _package = package;
                _status.Text = "识别成功：版本 " + package.Version + " · 当前 " + _installedVersion + " · " + package.Files.Count + " 个文件。确认后自动部署。";
                foreach (UpdateFile file in package.Files)
                    _fileList.Items.Add(file.Target + "    （" + FormatBytes(file.Size) + "）");
                _applyButton.Enabled = true;
            }
            catch (Exception ex)
            {
                _status.Text = "无法识别此更新包：" + ex.Message;
                _status.ForeColor = Color.FromArgb(174, 61, 49);
            }
        }

        private void BeginApply()
        {
            if (_package == null || _working)
                return;
            DialogResult answer = MessageBox.Show(this,
                "将大有图片工具更新到版本 " + _package.Version + "。\n\n升级时会请求主程序正常关闭，并在替换前备份现有文件。继续吗？",
                "确认升级", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            string packagePath = _package.Path;
            _working = true;
            _applyButton.Enabled = false;
            _status.ForeColor = Color.FromArgb(45, 98, 111);
            _status.Text = "正在校验、备份并部署，请稍候…";
            Task.Run(delegate
            {
                Exception failure = null;
                string outcome = null;
                try { outcome = ApplyUpdate(packagePath); }
                catch (Exception ex) { failure = ex; }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _working = false;
                        if (failure == null)
                        {
                            _installedVersion = _package.Version;
                            _applyButton.Enabled = false;
                            _status.Text = outcome;
                            MessageBox.Show(this, outcome, "升级完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            _applyButton.Enabled = _package != null;
                            _status.Text = "升级未完成：" + failure.Message;
                            _status.ForeColor = Color.FromArgb(174, 61, 49);
                            MessageBox.Show(this, failure.Message, "升级失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                }
                catch (InvalidOperationException) { }
            });
        }

        private string ApplyUpdate(string packagePath)
        {
            AppendDiagnosticLog("开始应用更新包：" + packagePath);
            Action<string> log = delegate(string message)
            {
                AppendDiagnosticLog(message);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _fileList.Items.Add(message);
                        _fileList.TopIndex = Math.Max(0, _fileList.Items.Count - 1);
                    });
                }
                catch (InvalidOperationException) { }
            };

            UpdatePackageInfo package = UpdatePackageReader.Read(packagePath, _installDirectory);
            if (!_installationValid)
                throw new InvalidOperationException("当前目录不是有效的大有图片工具安装目录。");
            if (package.MinimumUpdaterVersion > GetUpdaterVersion())
                throw new InvalidDataException("此更新包要求升级程序版本至少为 " + package.MinimumUpdaterVersion + "，请先运行新版安装程序。");
            Version currentVersion = ParseVersion(FileVersionInfo.GetVersionInfo(_mainPath).FileVersion);
            if (package.Version < currentVersion)
                throw new InvalidDataException("更新包版本低于当前安装版本，已阻止降级覆盖。");
            string stagingRoot = Path.Combine(Path.GetTempPath(), "DayouImageToolsUpdate_" + Guid.NewGuid().ToString("N"));
            string backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "大有图片工具", "UpdateBackups", package.Version.ToString() + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(stagingRoot);
            Directory.CreateDirectory(backupRoot);
            List<string> touched = new List<string>();
            Dictionary<string, bool> existed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            try
            {
                log("正在解压到临时目录并检查 SHA-256…");
                UpdatePackageReader.ExtractAndVerify(package, stagingRoot, log);
                CloseRunningMain(log);

                log("正在备份旧文件…");
                foreach (UpdateFile file in package.Files)
                {
                    string relative = file.Target.Replace('/', Path.DirectorySeparatorChar);
                    string destination = Path.GetFullPath(Path.Combine(_installDirectory, relative));
                    string backup = Path.GetFullPath(Path.Combine(backupRoot, relative));
                    string stage = Path.GetFullPath(Path.Combine(stagingRoot, relative));
                    if (File.Exists(destination))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backup));
                        CopyFileWithRetry(destination, backup, true, "备份", log);
                        existed[relative] = true;
                    }
                    else
                        existed[relative] = false;

                    touched.Add(relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    CopyFileWithRetry(stage, destination, true, "部署", log);
                    log("已部署：" + file.Target);
                }
            }
            catch (Exception updateError)
            {
                AppendDiagnosticLog("更新失败，开始回滚：" + updateError);
                List<string> rollbackErrors = new List<string>();
                for (int i = touched.Count - 1; i >= 0; i--)
                {
                    string relative = touched[i];
                    string destination = Path.Combine(_installDirectory, relative);
                    try
                    {
                        if (existed[relative])
                        {
                            string backup = Path.Combine(backupRoot, relative);
                            if (File.Exists(backup)) CopyFileWithRetry(backup, destination, true, "回滚", log);
                        }
                        else if (File.Exists(destination))
                            File.Delete(destination);
                    }
                    catch (Exception rollbackError)
                    {
                        rollbackErrors.Add(relative + "：" + rollbackError.Message);
                        AppendDiagnosticLog("回滚文件失败：" + relative + "；" + rollbackError);
                    }
                }
                if (rollbackErrors.Count > 0)
                    throw new IOException("更新失败，自动回滚时也遇到错误。备份位置：" + backupRoot + "\n\n更新错误：" + updateError.Message + "\n\n回滚错误：" + String.Join("\n", rollbackErrors.ToArray()), updateError);
                throw new IOException("更新失败，已自动还原被替换的文件。备份位置：" + backupRoot + "\n\n" + updateError.Message, updateError);
            }
            finally
            {
                try { if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true); } catch { }
            }

            AppendDiagnosticLog("更新到版本 " + package.Version + " 已完成。");
            string restartMessage = "更新到版本 " + package.Version + " 已完成。旧文件备份在：\n" + backupRoot;
            try
            {
                Process.Start(new ProcessStartInfo(_mainPath) { WorkingDirectory = _installDirectory, UseShellExecute = true });
                log("已重新启动大有图片工具。");
            }
            catch (Exception ex)
            {
                AppendDiagnosticLog("自动重新启动主程序失败：" + ex);
                restartMessage += "\n\n自动启动失败，请从桌面快捷方式启动程序。\n" + ex.Message;
            }
            return restartMessage;
        }

        private void CloseRunningMain(Action<string> log)
        {
            List<Process> matching = new List<Process>();
            bool inspectionFailed = false;
            string expected = Path.GetFullPath(_mainPath);
            foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_mainPath)))
            {
                try
                {
                    string actual = Path.GetFullPath(process.MainModule.FileName);
                    if (String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                        matching.Add(process);
                    else
                        process.Dispose();
                }
                catch (Exception ex)
                {
                    bool hasExited = false;
                    try
                    {
                        hasExited = process.HasExited;
                    }
                    catch { }

                    if (!hasExited)
                    {
                        inspectionFailed = true;
                        log("无法确认同名进程 PID " + SafeProcessId(process) + " 的程序路径：" + ex.Message);
                    }
                    process.Dispose();
                }
            }

            if (inspectionFailed)
            {
                foreach (Process process in matching)
                    process.Dispose();
                throw new IOException("无法确认所有大有图片工具进程是否已退出。请先保存并手动退出大有图片工具，再重新升级。");
            }

            if (matching.Count == 0)
                return;
            log("正在请求主程序保存并关闭…");
            foreach (Process process in matching)
            {
                try
                {
                    bool requested = process.CloseMainWindow();
                    log("已向主程序 PID " + SafeProcessId(process) + (requested ? " 发送关闭请求。" : " 发送关闭请求失败，继续等待其退出。"));
                }
                catch (Exception ex)
                {
                    log("向主程序 PID " + SafeProcessId(process) + " 发送关闭请求时出错：" + ex.Message);
                }
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(MainShutdownTimeoutSeconds);
            HashSet<int> reportedStatusErrors = new HashSet<int>();
            try
            {
                while (DateTime.UtcNow < deadline)
                {
                    bool anyRunning = false;
                    foreach (Process process in matching)
                    {
                        try
                        {
                            process.Refresh();
                            if (!process.HasExited) anyRunning = true;
                        }
                        catch (Exception ex)
                        {
                            // An unreadable state is not proof that the executable has exited.
                            bool hasExited = false;
                            try { hasExited = process.HasExited; }
                            catch { }
                            if (!hasExited)
                            {
                                anyRunning = true;
                                int processId = SafeProcessId(process);
                                if (reportedStatusErrors.Add(processId))
                                    log("无法确认主程序 PID " + processId + " 是否已退出，将继续等待：" + ex.Message);
                            }
                        }
                    }
                    if (!anyRunning)
                        return;
                    Thread.Sleep(250);
                }
            }
            finally
            {
                foreach (Process process in matching)
                    process.Dispose();
            }

            throw new IOException("主程序在 " + MainShutdownTimeoutSeconds + " 秒内没有关闭。请先保存编辑内容并手动退出大有图片工具，再重新升级。");
        }

        private static int SafeProcessId(Process process)
        {
            try { return process.Id; }
            catch { return 0; }
        }

        private static void CopyFileWithRetry(string source, string destination, bool overwrite, string operation, Action<string> log)
        {
            for (int attempt = 1; attempt <= FileCopyMaximumAttempts; attempt++)
            {
                try
                {
                    File.Copy(source, destination, overwrite);
                    return;
                }
                catch (IOException ex)
                {
                    if (attempt == FileCopyMaximumAttempts) throw;
                    log(operation + "文件时遇到短暂占用，" + attempt + "/" + FileCopyMaximumAttempts + " 次后重试：" + Path.GetFileName(destination) + "；" + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (attempt == FileCopyMaximumAttempts) throw;
                    log(operation + "文件时暂时无法访问，" + attempt + "/" + FileCopyMaximumAttempts + " 次后重试：" + Path.GetFileName(destination) + "；" + ex.Message);
                }

                Thread.Sleep(500 * attempt);
            }
        }

        private static void AppendDiagnosticLog(string message)
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "大有图片工具", "Logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "updater-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine;
                File.AppendAllText(path, line, new UTF8Encoding(false));
            }
            catch { }
        }

        private static Version ParseVersion(string text)
        {
            Version result;
            if (!Version.TryParse(text, out result)) return new Version(0, 0, 0, 0);
            return UpdatePackageReader.NormalizeVersion(result);
        }

        private static Version GetUpdaterVersion()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version ?? new Version(1, 0, 0, 0);
        }

        private static string FormatBytes(long bytes)
        {
            double value = bytes;
            string[] units = new string[] { "B", "KB", "MB", "GB" };
            int index = 0;
            while (value >= 1024 && index < units.Length - 1) { value /= 1024; index++; }
            return value.ToString("0.##") + " " + units[index];
        }
    }
}
