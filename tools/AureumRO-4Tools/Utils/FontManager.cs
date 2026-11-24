using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    /// <summary>
    /// Loads embedded Google Sans (Regular &amp; Bold) into both GDI+ (PrivateFontCollection)
    /// and Win32 GDI (AddFontMemResourceEx) so every WinForms control renders authentic Google Sans
    /// without requiring font installation, while caching all Font instances to prevent GDI leaks.
    /// </summary>
    public static class FontManager
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<long, Font> FontCache = new Dictionary<long, Font>();
        private static PrivateFontCollection privateFonts;
        private static FontFamily googleSansFamily;
        private static bool initialized;

        public static bool IsGoogleSansLoaded
        {
            get
            {
                EnsureInitialized();
                return googleSansFamily != null;
            }
        }

        public static string ActiveFamilyName
        {
            get
            {
                EnsureInitialized();
                return googleSansFamily != null ? googleSansFamily.Name : "Segoe UI";
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                initialized = true;
                try
                {
                    privateFonts = new PrivateFontCollection();
                    Assembly asm = typeof(FontManager).Assembly;
                    string[] resourceNames = asm.GetManifestResourceNames();

                    bool loadedAny = false;
                    for (int i = 0; i < resourceNames.Length; i++)
                    {
                        string resName = resourceNames[i];
                        if (resName.EndsWith("GoogleSans-Regular.ttf", StringComparison.OrdinalIgnoreCase)
                            || resName.EndsWith("GoogleSans-Bold.ttf", StringComparison.OrdinalIgnoreCase))
                        {
                            using (Stream stream = asm.GetManifestResourceStream(resName))
                            {
                                if (stream != null && stream.Length > 0)
                                {
                                    byte[] data = new byte[stream.Length];
                                    int offset = 0;
                                    while (offset < data.Length)
                                    {
                                        int read = stream.Read(data, offset, data.Length - offset);
                                        if (read <= 0)
                                        {
                                            break;
                                        }
                                        offset += read;
                                    }

                                    IntPtr fontBuffer = Marshal.AllocCoTaskMem(data.Length);
                                    Marshal.Copy(data, 0, fontBuffer, data.Length);
                                    uint fontCount = 0;
                                    AddFontMemResourceEx(fontBuffer, (uint)data.Length, IntPtr.Zero, ref fontCount);
                                    privateFonts.AddMemoryFont(fontBuffer, data.Length);
                                    Marshal.FreeCoTaskMem(fontBuffer);
                                    loadedAny = true;
                                }
                            }
                        }
                    }

                    if (loadedAny && privateFonts.Families != null && privateFonts.Families.Length > 0)
                    {
                        for (int i = 0; i < privateFonts.Families.Length; i++)
                        {
                            if (string.Equals(privateFonts.Families[i].Name, "Google Sans", StringComparison.OrdinalIgnoreCase))
                            {
                                googleSansFamily = privateFonts.Families[i];
                                break;
                            }
                        }

                        if (googleSansFamily == null)
                        {
                            googleSansFamily = privateFonts.Families[0];
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("FontManager", "Failed to load embedded Google Sans font; falling back to Segoe UI", ex);
                    googleSansFamily = null;
                }
            }
        }

        public static Font Get(float sizeInPoints, FontStyle style = FontStyle.Regular)
        {
            EnsureInitialized();
            float clampedSize = Math.Max(6.5F, Math.Min(36F, sizeInPoints));
            int sizeKey = (int)Math.Round(clampedSize * 10F);
            long cacheKey = (((long)sizeKey) << 16) | ((uint)style & 0xFFFF);

            lock (SyncRoot)
            {
                Font cached;
                if (FontCache.TryGetValue(cacheKey, out cached))
                {
                    return cached;
                }

                Font created = CreateFontSafe(clampedSize, style);
                FontCache[cacheKey] = created;
                return created;
            }
        }

        public static Font Regular(float sizeInPoints = 8.5F)
        {
            return Get(sizeInPoints, FontStyle.Regular);
        }

        public static Font Bold(float sizeInPoints = 8.5F)
        {
            return Get(sizeInPoints, FontStyle.Bold);
        }

        public static Font Italic(float sizeInPoints = 8F)
        {
            return Get(sizeInPoints, FontStyle.Italic);
        }

        private static Font CreateFontSafe(float size, FontStyle style)
        {
            if (googleSansFamily != null)
            {
                try
                {
                    FontStyle supported = ResolveSupportedStyle(googleSansFamily, style);
                    return new Font(googleSansFamily, size, supported, GraphicsUnit.Point);
                }
                catch
                {
                    // Fall through to Segoe UI
                }
            }

            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }

        private static FontStyle ResolveSupportedStyle(FontFamily family, FontStyle requested)
        {
            if (family.IsStyleAvailable(requested))
            {
                return requested;
            }

            // Preserve Bold + Strikeout/Underline when Italic isn't bundled separately.
            FontStyle withoutItalic = requested & ~FontStyle.Italic;
            if (family.IsStyleAvailable(withoutItalic))
            {
                return withoutItalic;
            }

            if ((requested & FontStyle.Bold) != 0 && family.IsStyleAvailable(FontStyle.Bold))
            {
                return FontStyle.Bold;
            }

            return FontStyle.Regular;
        }

        public static void ApplyFontTree(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            EnsureInitialized();
            ApplyFontRecursive(root);
        }

        private static void ApplyFontRecursive(Control control)
        {
            if (control == null || control.IsDisposed)
            {
                return;
            }

            // Prevent WinForms AutoScaleMode.Font from rescaling coordinates/heights when switching
            // from Microsoft Sans Serif / Segoe UI to Google Sans (which has taller typographic metrics).
            ContainerControl containerCtrl = control as ContainerControl;
            if (containerCtrl != null && containerCtrl.AutoScaleMode == AutoScaleMode.Font)
            {
                containerCtrl.AutoScaleMode = AutoScaleMode.None;
            }

            if (!(control is Form))
            {
                Font current = control.Font;
                if (current != null && ShouldUpgradeFont(current.FontFamily.Name))
                {
                    float targetSize;
                    Button btn = control as Button;
                    if (btn != null && btn.Height <= 24)
                    {
                        // Keep compact action buttons at 8.5pt max so descenders (p, g, q, y) never clip.
                        targetSize = Math.Min(8.5F, current.SizeInPoints);
                    }
                    else if (btn != null)
                    {
                        targetSize = current.SizeInPoints < 9F
                            ? Math.Min(9F, current.SizeInPoints + 0.5F)
                            : current.SizeInPoints;
                    }
                    else
                    {
                        targetSize = current.SizeInPoints < 9.5F
                            ? Math.Min(9.5F, current.SizeInPoints + 0.5F)
                            : current.SizeInPoints;
                    }

                    Font upgraded = Get(targetSize, current.Style);
                    if (!ReferenceEquals(current, upgraded))
                    {
                        control.Font = upgraded;
                    }
                }
            }

            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                if (grid.DefaultCellStyle != null && grid.DefaultCellStyle.Font != null && ShouldUpgradeFont(grid.DefaultCellStyle.Font.FontFamily.Name))
                {
                    grid.DefaultCellStyle.Font = Get(grid.DefaultCellStyle.Font.SizeInPoints, grid.DefaultCellStyle.Font.Style);
                }
                if (grid.ColumnHeadersDefaultCellStyle != null && grid.ColumnHeadersDefaultCellStyle.Font != null && ShouldUpgradeFont(grid.ColumnHeadersDefaultCellStyle.Font.FontFamily.Name))
                {
                    grid.ColumnHeadersDefaultCellStyle.Font = Get(grid.ColumnHeadersDefaultCellStyle.Font.SizeInPoints, grid.ColumnHeadersDefaultCellStyle.Font.Style);
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyFontRecursive(child);
            }
        }

        private static bool ShouldUpgradeFont(string familyName)
        {
            if (string.IsNullOrEmpty(familyName))
            {
                return true;
            }

            if (string.Equals(familyName, ActiveFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Keep symbol/monospace fonts intact so directional arrows and EFST hex diagnostics stay aligned.
            if (familyName.IndexOf("Symbol", StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(familyName, "Consolas", StringComparison.OrdinalIgnoreCase)
                || string.Equals(familyName, "Courier New", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
