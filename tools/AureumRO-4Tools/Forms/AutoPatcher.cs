using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aspose.Zip.Rar;
using Newtonsoft.Json.Linq;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AutoPatcher : Form
    {
        public bool UpdateApplied { get; private set; }

        public AutoPatcher()
        {
            InitializeComponent();
            LocalizationManager.ApplyToControlTree(this);
            ThemeManager.ApplyTheme(this, Model.AppSettings.Load().DarkMode);
            this.Shown += AutoPatcher_Shown;
        }

        private async void AutoPatcher_Shown(object sender, EventArgs e)
        {
            await StartAutopatcherAsync();
        }

        private async Task StartAutopatcherAsync()
        {
            try
            {
                string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                string baseDirectory = Path.GetDirectoryName(currentExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                string exeFileName = Path.GetFileName(currentExePath);
                string oldFilePath = Path.Combine(baseDirectory, Path.GetFileNameWithoutExtension(exeFileName) + "_old.exe");

                if (File.Exists(oldFilePath))
                {
                    try
                    {
                        File.Delete(oldFilePath);
                    }
                    catch (Exception deleteEx)
                    {
                        Logger.Warn("AutoPatcher", "Could not delete previous backup executable", deleteEx);
                    }
                }

                string latestVersionJson = await HttpClientProvider.GetStringAsync(AppConfig._4RLatestVersionURL, TimeSpan.FromSeconds(5));
                JObject obj = JObject.Parse(latestVersionJson);

                string tag = (string)(obj["tag_name"] ?? obj["name"]);
                if (string.IsNullOrWhiteSpace(tag) || string.Equals(tag, AppConfig.Version, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                JArray assets = obj["assets"] as JArray;
                if (assets == null || assets.Count == 0)
                {
                    return;
                }

                JToken selectedAsset = SelectArchiveAsset(assets);
                if (selectedAsset == null)
                {
                    return;
                }

                string downloadUrl = (string)selectedAsset["browser_download_url"];
                string assetName = (string)selectedAsset["name"];
                if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(assetName))
                {
                    return;
                }

                string safeFileName = Path.GetFileName(assetName);
                string downloadPath = Path.Combine(baseDirectory, safeFileName);

                await DownloadAsync(downloadUrl, downloadPath);

                File.Move(currentExePath, oldFilePath);

                try
                {
                    ExtractArchiveSafely(downloadPath, baseDirectory);
                    this.UpdateApplied = true;
                    if (File.Exists(currentExePath))
                    {
                        Process.Start(new ProcessStartInfo(currentExePath) { UseShellExecute = true });
                    }
                }
                catch
                {
                    // Restore original executable if extraction fails.
                    if (!File.Exists(currentExePath) && File.Exists(oldFilePath))
                    {
                        File.Move(oldFilePath, currentExePath);
                    }
                    throw;
                }
                finally
                {
                    if (File.Exists(downloadPath))
                    {
                        File.Delete(downloadPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("AutoPatcher", "Auto-update skipped due to error", ex);
            }
            finally
            {
                if (!this.IsDisposed)
                {
                    this.Close();
                }
            }
        }

        private static JToken SelectArchiveAsset(JArray assets)
        {
            if (assets == null)
            {
                return null;
            }

            foreach (JToken asset in assets)
            {
                string name = (string)asset["name"];
                if (!string.IsNullOrWhiteSpace(name) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }

            foreach (JToken asset in assets)
            {
                string name = (string)asset["name"];
                if (!string.IsNullOrWhiteSpace(name) && name.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }

            return null;
        }

        private static void ExtractArchiveSafely(string archivePath, string destinationDirectory)
        {
            string fullDestDir = Path.GetFullPath(destinationDirectory);
            if (!fullDestDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fullDestDir += Path.DirectorySeparatorChar;
            }

            string ext = Path.GetExtension(archivePath);
            if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                using (ZipArchive zip = ZipFile.OpenRead(archivePath))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string targetPath = Path.GetFullPath(Path.Combine(fullDestDir, entry.FullName));
                        if (!targetPath.StartsWith(fullDestDir, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new IOException("Blocked unsafe archive path: " + entry.FullName);
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(targetPath);
                        }
                        else
                        {
                            string entryDir = Path.GetDirectoryName(targetPath);
                            if (!string.IsNullOrEmpty(entryDir))
                            {
                                Directory.CreateDirectory(entryDir);
                            }
                            entry.ExtractToFile(targetPath, true);
                        }
                    }
                }
            }
            else
            {
                using (RarArchive arch = new RarArchive(archivePath))
                {
                    foreach (RarArchiveEntry entry in arch.Entries)
                    {
                        string targetPath = Path.GetFullPath(Path.Combine(fullDestDir, entry.Name));
                        if (!targetPath.StartsWith(fullDestDir, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new IOException("Blocked unsafe archive path: " + entry.Name);
                        }
                    }

                    arch.ExtractToDirectory(fullDestDir);
                }
            }
        }

        private async Task DownloadAsync(string url, string filePath)
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AureumTools/" + AppConfig.Version);
                webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                await webClient.DownloadFileTaskAsync(new Uri(url), filePath);
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            int percent = e.ProgressPercentage;
            if (e.TotalBytesToReceive > 0)
            {
                percent = (int)Math.Floor((double)e.BytesReceived * 100.0 / e.TotalBytesToReceive);
            }

            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            this.BeginInvoke((MethodInvoker)delegate
            {
                if (!this.IsDisposed && !this.pbPatcher.IsDisposed)
                {
                    this.pbPatcher.Value = percent;
                }
            });
        }
    }
}
