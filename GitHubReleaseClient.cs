using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace DayouImageToolsUpdater
{
    internal sealed class GitHubReleaseInfo
    {
        public Version Version;
        public string TagName;
        public string Name;
        public string Notes;
        public string PublishedAt;
        public string AssetName;
        public string AssetUrl;
        public string AssetDigest;
        public long AssetSize;
    }

    internal static class GitHubReleaseClient
    {
        private const string RepositoryOwner = "dayouAI777";
        private const string RepositoryName = "dayou-image-tools";
        private const long MaximumAssetSize = 2L * 1024L * 1024L * 1024L;
        private const int MaximumNetworkAttempts = 3;

        static GitHubReleaseClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public static GitHubReleaseInfo GetLatestRelease(Action<int> retrying)
        {
            string apiUrl = "https://api.github.com/repos/" + Uri.EscapeDataString(RepositoryOwner) + "/" + Uri.EscapeDataString(RepositoryName) + "/releases/latest";
            string json = null;
            for (int attempt = 1; attempt <= MaximumNetworkAttempts; attempt++)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                    request.Method = "GET";
                    request.Accept = "application/vnd.github+json";
                    request.UserAgent = "DayouImageToolsUpdater/1.3.7";
                    request.Headers["X-GitHub-Api-Version"] = "2022-11-28";
                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    request.Timeout = 45000;
                    request.ReadWriteTimeout = 60000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        json = reader.ReadToEnd();
                    break;
                }
                catch (WebException ex)
                {
                    HttpWebResponse response = ex.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                    {
                        response.Dispose();
                        return null;
                    }
                    if (!IsTransientNetworkFailure(ex) || attempt == MaximumNetworkAttempts)
                    {
                        string detail = ReadErrorResponse(response);
                        string diagnostic = ex.Status + "：" + ex.Message;
                        if (ex.InnerException != null && !String.IsNullOrWhiteSpace(ex.InnerException.Message))
                            diagnostic += "（" + ex.InnerException.Message + "）";
                        throw new IOException("无法读取 GitHub Releases。网络诊断：" + diagnostic + (String.IsNullOrWhiteSpace(detail) ? "" : "\n" + detail), ex);
                    }
                    if (response != null) response.Dispose();
                    NotifyRetry(retrying, attempt + 1);
                    Thread.Sleep(RetryDelay(attempt));
                }
                catch (IOException ex)
                {
                    if (attempt == MaximumNetworkAttempts)
                        throw new IOException("连接 GitHub 检查更新失败，已自动重试 " + MaximumNetworkAttempts + " 次。网络诊断：" + ex.Message, ex);
                    NotifyRetry(retrying, attempt + 1);
                    Thread.Sleep(RetryDelay(attempt));
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
            Dictionary<string, object> releaseData;
            try { releaseData = serializer.Deserialize<Dictionary<string, object>>(json); }
            catch (Exception ex) { throw new InvalidDataException("GitHub 返回的 Release 信息无法解析。", ex); }
            if (releaseData == null)
                throw new InvalidDataException("GitHub 没有返回 Release 信息。");

            string tag = GetString(releaseData, "tag_name");
            Version version = ParseTagVersion(tag);
            IEnumerable assets = releaseData.ContainsKey("assets") ? releaseData["assets"] as IEnumerable : null;
            Dictionary<string, object> updateAsset = null;
            if (assets != null)
            {
                foreach (object item in assets)
                {
                    Dictionary<string, object> candidate = item as Dictionary<string, object>;
                    if (candidate == null)
                        continue;
                    string name = GetString(candidate, "name");
                    if (name.EndsWith(".dayouupdate", StringComparison.OrdinalIgnoreCase))
                    {
                        updateAsset = candidate;
                        break;
                    }
                }
            }
            if (updateAsset == null)
                throw new InvalidDataException("GitHub 最新正式版没有附带 .dayouupdate 更新包。");

            string assetUrl = GetString(updateAsset, "browser_download_url");
            Uri parsedAssetUrl;
            if (!Uri.TryCreate(assetUrl, UriKind.Absolute, out parsedAssetUrl) || parsedAssetUrl.Scheme != Uri.UriSchemeHttps || !IsGitHubHost(parsedAssetUrl.Host))
                throw new InvalidDataException("GitHub Release 更新包链接无效。");

            long size = GetLong(updateAsset, "size");
            if (size <= 0 || size > MaximumAssetSize)
                throw new InvalidDataException("GitHub Release 更新包大小无效或超过 2 GB 限制。");

            return new GitHubReleaseInfo
            {
                Version = version,
                TagName = tag,
                Name = GetString(releaseData, "name"),
                Notes = GetString(releaseData, "body"),
                PublishedAt = GetString(releaseData, "published_at"),
                AssetName = GetString(updateAsset, "name"),
                AssetUrl = assetUrl,
                AssetDigest = GetString(updateAsset, "digest"),
                AssetSize = size
            };
        }

        public static string DownloadAsset(GitHubReleaseInfo release, Action<long, long> progress, Action<int> retrying)
        {
            if (release == null)
                throw new ArgumentNullException("release");
            Uri uri = new Uri(release.AssetUrl);
            if (uri.Scheme != Uri.UriSchemeHttps || !IsGitHubHost(uri.Host))
                throw new InvalidDataException("更新包下载链接不是受支持的 GitHub HTTPS 地址。");

            string downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "大有图片工具", "Downloads");
            Directory.CreateDirectory(downloadDirectory);
            string fileName = Path.GetFileName(release.AssetName);
            if (String.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".dayouupdate", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("更新包文件名无效。");
            string destination = Path.Combine(downloadDirectory, "大有图片工具_" + release.Version + ".dayouupdate");
            string partial = destination + ".download";
            if (File.Exists(partial)) File.Delete(partial);
            for (int attempt = 1; attempt <= MaximumNetworkAttempts; attempt++)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                    request.Method = "GET";
                    request.UserAgent = "DayouImageToolsUpdater/1.3.7";
                    request.Accept = "application/octet-stream";
                    request.Timeout = 60000;
                    request.ReadWriteTimeout = 90000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.ResponseUri == null || response.ResponseUri.Scheme != Uri.UriSchemeHttps || !IsGitHubHost(response.ResponseUri.Host))
                            throw new InvalidDataException("GitHub 更新包重定向到了不受信任的主机。");
                        long total = response.ContentLength > 0 ? response.ContentLength : release.AssetSize;
                        if (response.ContentLength > MaximumAssetSize || (response.ContentLength > 0 && response.ContentLength != release.AssetSize))
                            throw new InvalidDataException("下载包大小与 GitHub Release 清单不一致。");

                        long received = 0;
                        byte[] buffer = new byte[128 * 1024];
                        using (Stream input = response.GetResponseStream())
                        using (FileStream output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            int read;
                            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                received += read;
                                if (received > MaximumAssetSize || received > release.AssetSize)
                                    throw new InvalidDataException("下载包超过 GitHub Release 清单声明的大小。");
                                output.Write(buffer, 0, read);
                                if (progress != null)
                                    progress(received, total);
                            }
                            output.Flush();
                        }

                        if (received != release.AssetSize)
                            throw new IOException("下载中断：实际大小与 GitHub Release 清单不一致，正在自动重试。");
                    }

                    if (!String.IsNullOrWhiteSpace(release.AssetDigest))
                    {
                        string digest = release.AssetDigest.Trim();
                        const string prefix = "sha256:";
                        if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || digest.Length != prefix.Length + 64)
                            throw new InvalidDataException("GitHub 提供的更新包摘要格式无效。");
                        string actual = ComputeSha256(partial);
                        if (!String.Equals(actual, digest.Substring(prefix.Length), StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("更新包 SHA-256 与 GitHub Release 摘要不一致。");
                    }

                    if (File.Exists(destination)) File.Delete(destination);
                    File.Move(partial, destination);
                    return destination;
                }
                catch (Exception ex)
                {
                    bool retryable = IsTransientDownloadFailure(ex);
                    if (!retryable || attempt == MaximumNetworkAttempts)
                    {
                        try { if (File.Exists(partial)) File.Delete(partial); } catch { }
                        if (retryable)
                            throw new IOException("更新包下载在自动重试 " + MaximumNetworkAttempts + " 次后仍失败。网络诊断：" + ex.Message, ex);
                        throw;
                    }
                    try { if (File.Exists(partial)) File.Delete(partial); } catch { }
                    NotifyRetry(retrying, attempt + 1);
                    Thread.Sleep(RetryDelay(attempt));
                }
            }
            throw new IOException("更新包下载失败。");
        }

        private static bool IsTransientDownloadFailure(Exception error)
        {
            if (error is InvalidDataException)
                return false;
            WebException webError = error as WebException;
            if (webError != null)
                return IsTransientNetworkFailure(webError);
            return error is IOException;
        }

        private static bool IsTransientNetworkFailure(WebException error)
        {
            switch (error.Status)
            {
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.ProxyNameResolutionFailure:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.KeepAliveFailure:
                case WebExceptionStatus.PipelineFailure:
                    return true;
            }

            HttpWebResponse response = error.Response as HttpWebResponse;
            return response != null && ((int)response.StatusCode == 408 || (int)response.StatusCode == 429 || (int)response.StatusCode >= 500);
        }

        private static int RetryDelay(int failedAttempt)
        {
            return failedAttempt * 1200;
        }

        private static void NotifyRetry(Action<int> retrying, int nextAttempt)
        {
            if (retrying != null)
                retrying(nextAttempt);
        }

        private static Version ParseTagVersion(string tag)
        {
            string value = (tag ?? String.Empty).Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(1);
            Version version;
            if (!Version.TryParse(value, out version))
                throw new InvalidDataException("GitHub Release 标签必须是 v1.2.3 这类版本号。");
            return UpdatePackageReader.NormalizeVersion(version);
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.ContainsKey(key) || data[key] == null)
                return String.Empty;
            return Convert.ToString(data[key], CultureInfo.InvariantCulture) ?? String.Empty;
        }

        private static long GetLong(Dictionary<string, object> data, string key)
        {
            object value;
            if (data == null || !data.TryGetValue(key, out value) || value == null)
                return 0;
            try { return Convert.ToInt64(value, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static bool IsGitHubHost(string host)
        {
            return String.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase)
                || String.Equals(host, "githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                StringBuilder value = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                    value.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return value.ToString();
            }
        }

        private static string ReadErrorResponse(HttpWebResponse response)
        {
            if (response == null)
                return String.Empty;
            using (response)
            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null) return String.Empty;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string text = reader.ReadToEnd();
                    if (text.Length > 500) text = text.Substring(0, 500);
                    return text;
                }
            }
        }
    }
}
