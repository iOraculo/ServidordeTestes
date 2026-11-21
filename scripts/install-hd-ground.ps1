param(
    [ValidateSet("marmore", "remaster", "original")]
    [string]$Mode = "marmore",
    [string]$ClientDir = "C:\AureumRO\Froggo2022",
    [string[]]$Maps = @("prontera", "pvp_n_1-5", "guild_vs3", "prt_cas01"),
    [switch]$SkipFog
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$HdAssetsDir = Join-Path $RepoRoot "client\visuals\hd"
$FogSrc = Join-Path $RepoRoot "client\visuals\fogParameterTable.txt"
$DataDir = Join-Path $ClientDir "data"
$TextureRoot = Join-Path $DataDir "texture"
$ManifestPath = Join-Path $TextureRoot ".aureumro_hd_manifest.txt"
$GrfPath = Join-Path $ClientDir "data.grf"

Write-Host "=== AureumRO HD Ground Texture Installer (Mode: $Mode) ===" -ForegroundColor Yellow

if (-not (Test-Path $ClientDir)) {
    throw "Pasta do cliente nao encontrada: $ClientDir"
}

function Remove-PreviousHdFiles {
    param([string]$Manifest, [string]$BaseDataDir)
    $removed = 0
    if (Test-Path $Manifest) {
        $lines = Get-Content -Path $Manifest -ErrorAction SilentlyContinue
        foreach ($rel in $lines) {
            if ([string]::IsNullOrWhiteSpace($rel)) { continue }
            $full = Join-Path $BaseDataDir $rel.Trim()
            if (Test-Path $full) {
                Remove-Item -Path $full -Force -ErrorAction SilentlyContinue
                $removed++
            }
        }
        Remove-Item -Path $Manifest -Force -ErrorAction SilentlyContinue
    }
    return $removed
}

if ($Mode -eq "original") {
    $count = Remove-PreviousHdFiles -Manifest $ManifestPath -BaseDataDir $DataDir
    $fogDst = Join-Path $DataDir "fogParameterTable.txt"
    if (Test-Path $fogDst) {
        Remove-Item -Path $fogDst -Force -ErrorAction SilentlyContinue
        $count++
    }
    Write-Host "[OK] Texturas HD removidas ($count arquivo(s)). Chao original do kRO restaurado!" -ForegroundColor Green
    exit 0
}

if (-not (Test-Path $GrfPath)) {
    throw "data.grf nao encontrado em: $GrfPath"
}

$MarbleProntera = Join-Path $HdAssetsDir "marble_prontera_hd.jpg"
$MarbleObsidian = Join-Path $HdAssetsDir "marble_obsidian_hd.jpg"
$GrassHd = Join-Path $HdAssetsDir "grass_prontera_hd.jpg"

if ($Mode -eq "marmore") {
    foreach ($req in @($MarbleProntera, $MarbleObsidian, $GrassHd)) {
        if (-not (Test-Path $req)) {
            throw "Textura HD nao encontrada: $req (rode git pull primeiro)."
        }
    }
}

# Clean any previously installed HD ground files first
$prev = Remove-PreviousHdFiles -Manifest $ManifestPath -BaseDataDir $DataDir
if ($prev -gt 0) {
    Write-Host "[INFO] Removidas $prev texturas HD anteriores antes de aplicar o modo '$Mode'." -ForegroundColor DarkGray
}

Add-Type -AssemblyName System.Drawing

$csharpCode = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

public static class AureumHdGround
{
    private class GrfEntry
    {
        public string Name;
        public uint CompressedSize;
        public uint CompressedSizeAligned;
        public uint UncompressedSize;
        public byte Flags;
        public uint Offset;
    }

    private static byte[] DecompressZlib(byte[] input)
    {
        if (input == null || input.Length < 2) return null;
        using (var ms = new MemoryStream(input, 2, input.Length - 2))
        using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
        using (var outMs = new MemoryStream())
        {
            ds.CopyTo(outMs);
            return outMs.ToArray();
        }
    }

    private static Dictionary<string, GrfEntry> LoadGrfTable(FileStream fs, Encoding enc)
    {
        var map = new Dictionary<string, GrfEntry>(StringComparer.OrdinalIgnoreCase);
        using (var br = new BinaryReader(fs, Encoding.ASCII, true))
        {
            fs.Seek(0, SeekOrigin.Begin);
            byte[] header = br.ReadBytes(46);
            string magic = Encoding.ASCII.GetString(header, 0, 15);
            if (magic != "Master of Magic")
            {
                throw new InvalidDataException("Arquivo GRF invalido (assinatura Master of Magic ausente).");
            }

            uint fileTableOffset = BitConverter.ToUInt32(header, 30);
            uint seed = BitConverter.ToUInt32(header, 34);
            uint rawCount = BitConverter.ToUInt32(header, 38);
            uint version = BitConverter.ToUInt32(header, 42);
            if (version != 0x200)
            {
                throw new InvalidDataException("Apenas GRF versao 0x200 e suportado.");
            }

            long realCount = (long)rawCount - (long)seed - 7L;
            fs.Seek(46L + (long)fileTableOffset, SeekOrigin.Begin);
            uint compTableSize = br.ReadUInt32();
            uint uncompTableSize = br.ReadUInt32();
            byte[] compTable = br.ReadBytes((int)compTableSize);
            byte[] table = DecompressZlib(compTable);

            int pos = 0;
            for (long i = 0; i < realCount && pos < table.Length; i++)
            {
                int zero = Array.IndexOf<byte>(table, 0, pos);
                if (zero < 0) break;
                string name = enc.GetString(table, pos, zero - pos).Replace('/', '\\');
                pos = zero + 1;
                if (pos + 17 > table.Length) break;

                var e = new GrfEntry
                {
                    Name = name,
                    CompressedSize = BitConverter.ToUInt32(table, pos),
                    CompressedSizeAligned = BitConverter.ToUInt32(table, pos + 4),
                    UncompressedSize = BitConverter.ToUInt32(table, pos + 8),
                    Flags = table[pos + 12],
                    Offset = BitConverter.ToUInt32(table, pos + 13)
                };
                pos += 17;
                map[name] = e;
            }
        }
        return map;
    }

    private static byte[] ExtractEntry(FileStream fs, GrfEntry e)
    {
        if (e == null || (e.Flags & 0x01) == 0) return null;
        // Skip DES encrypted entries safely (most kRO .gnd/.bmp are flags == 1 pure zlib)
        if ((e.Flags & 0x02) != 0 || (e.Flags & 0x04) != 0) return null;

        fs.Seek(46L + (long)e.Offset, SeekOrigin.Begin);
        byte[] comp = new byte[e.CompressedSize];
        int read = fs.Read(comp, 0, comp.Length);
        if (read < 2) return null;

        if (e.CompressedSize == e.UncompressedSize && comp[0] != 0x78)
        {
            return comp;
        }
        return DecompressZlib(comp);
    }

    private static List<string> ParseGndTextures(byte[] gnd, Encoding enc)
    {
        var list = new List<string>();
        if (gnd == null || gnd.Length < 26) return list;
        if (Encoding.ASCII.GetString(gnd, 0, 4) != "GRGN") return list;

        uint texCount = BitConverter.ToUInt32(gnd, 18);
        uint texNameLen = BitConverter.ToUInt32(gnd, 22);
        if (texNameLen == 0 || texNameLen > 256 || texCount > 4096) return list;

        int pos = 26;
        for (uint i = 0; i < texCount && pos + (int)texNameLen <= gnd.Length; i++)
        {
            int len = 0;
            while (len < (int)texNameLen && gnd[pos + len] != 0) len++;
            if (len > 0)
            {
                string t = enc.GetString(gnd, pos, len).Replace('/', '\\').Trim();
                if (!string.IsNullOrEmpty(t)) list.Add(t);
            }
            pos += (int)texNameLen;
        }
        return list;
    }

    private static void AnalyzeBitmap(Bitmap bmp, out bool hasMagenta, out bool isGrass)
    {
        hasMagenta = false;
        isGrass = false;
        long rSum = 0, gSum = 0, bSum = 0, count = 0, magCount = 0;
        int stepX = Math.Max(1, bmp.Width / 32);
        int stepY = Math.Max(1, bmp.Height / 32);
        for (int y = 0; y < bmp.Height; y += stepY)
        {
            for (int x = 0; x < bmp.Width; x += stepX)
            {
                Color c = bmp.GetPixel(x, y);
                if (c.R >= 245 && c.G <= 10 && c.B >= 245)
                {
                    magCount++;
                    continue;
                }
                rSum += c.R;
                gSum += c.G;
                bSum += c.B;
                count++;
            }
        }
        long total = count + magCount;
        if (total > 0 && (double)magCount / total > 0.03)
        {
            hasMagenta = true;
        }
        if (count > 0)
        {
            double avgR = (double)rSum / count;
            double avgG = (double)gSum / count;
            double avgB = (double)bSum / count;
            if (avgG > avgR * 1.07 && avgG > avgB * 1.09 && avgG > 50)
            {
                isGrass = true;
            }
        }
    }

    private static Bitmap RemasterBitmap(Bitmap src, int targetSize)
    {
        int outW = Math.Min(targetSize, Math.Max(256, src.Width * 4));
        int outH = Math.Min(targetSize, Math.Max(256, src.Height * 4));

        var up = new Bitmap(outW, outH, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(up))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(src, 0, 0, outW, outH);
        }

        // Apply crisp unsharp mask + subtle contrast/saturation boost while preserving magenta transparency
        Rectangle rect = new Rectangle(0, 0, outW, outH);
        BitmapData srcData = up.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        byte[] orig = new byte[Math.Abs(srcData.Stride) * outH];
        Marshal.Copy(srcData.Scan0, orig, 0, orig.Length);
        int stride = Math.Abs(srcData.Stride);
        up.UnlockBits(srcData);

        var result = new Bitmap(outW, outH, PixelFormat.Format24bppRgb);
        BitmapData dstData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        byte[] dst = new byte[orig.Length];

        for (int y = 0; y < outH; y++)
        {
            int ym1 = (y == 0) ? outH - 1 : y - 1;
            int yp1 = (y == outH - 1) ? 0 : y + 1;
            for (int x = 0; x < outW; x++)
            {
                int xm1 = (x == 0) ? outW - 1 : x - 1;
                int xp1 = (x == outW - 1) ? 0 : x + 1;

                int idx = y * stride + x * 3;
                byte cb = orig[idx];
                byte cg = orig[idx + 1];
                byte cr = orig[idx + 2];

                // Preserve RO magenta transparency pixel untouched
                if (cr >= 245 && cg <= 10 && cb >= 245)
                {
                    dst[idx] = 255;
                    dst[idx + 1] = 0;
                    dst[idx + 2] = 255;
                    continue;
                }

                int iUp = ym1 * stride + x * 3;
                int iDn = yp1 * stride + x * 3;
                int iLt = y * stride + xm1 * 3;
                int iRt = y * stride + xp1 * 3;

                double blurB = (orig[iUp] + orig[iDn] + orig[iLt] + orig[iRt]) * 0.25;
                double blurG = (orig[iUp + 1] + orig[iDn + 1] + orig[iLt + 1] + orig[iRt + 1]) * 0.25;
                double blurR = (orig[iUp + 2] + orig[iDn + 2] + orig[iLt + 2] + orig[iRt + 2]) * 0.25;

                // Sharpen + subtle contrast (+8%)
                double sharpB = cb + (cb - blurB) * 0.75;
                double sharpG = cg + (cg - blurG) * 0.75;
                double sharpR = cr + (cr - blurR) * 0.75;

                sharpB = (sharpB - 128.0) * 1.07 + 128.0;
                sharpG = (sharpG - 128.0) * 1.07 + 128.0;
                sharpR = (sharpR - 128.0) * 1.07 + 128.0;

                dst[idx] = (byte)Math.Max(0, Math.Min(255, (int)sharpB));
                dst[idx + 1] = (byte)Math.Max(0, Math.Min(255, (int)sharpG));
                dst[idx + 2] = (byte)Math.Max(0, Math.Min(255, (int)sharpR));
            }
        }

        Marshal.Copy(dst, 0, dstData.Scan0, dst.Length);
        result.UnlockBits(dstData);
        up.Dispose();
        return result;
    }

    private static Bitmap LoadAs24BitBmp(string path, int size)
    {
        using (var src = new Bitmap(path))
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, size, size);
            }
            return bmp;
        }
    }

    public static List<string> Install(
        string grfPath,
        string dataDir,
        string mode,
        string[] maps,
        string marblePronteraPath,
        string marbleObsidianPath,
        string grassHdPath)
    {
        var writtenRelative = new List<string>();
        var writtenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Encoding krEnc = Encoding.GetEncoding(949);

        Bitmap marblePrt = null;
        Bitmap marbleObs = null;
        Bitmap grassBmp = null;

        try
        {
            if (mode == "marmore")
            {
                marblePrt = LoadAs24BitBmp(marblePronteraPath, 512);
                marbleObs = LoadAs24BitBmp(marbleObsidianPath, 512);
                grassBmp = LoadAs24BitBmp(grassHdPath, 512);
            }

            using (var fs = new FileStream(grfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var table = LoadGrfTable(fs, krEnc);

                foreach (string mapName in maps)
                {
                    string gndKey = "data\\" + mapName + ".gnd";
                    GrfEntry gndEntry;
                    if (!table.TryGetValue(gndKey, out gndEntry))
                    {
                        Console.WriteLine("[WARN] Mapa nao encontrado no data.grf: " + gndKey);
                        continue;
                    }

                    byte[] gndBytes = ExtractEntry(fs, gndEntry);
                    if (gndBytes == null)
                    {
                        Console.WriteLine("[WARN] Nao foi possivel extrair: " + gndKey);
                        continue;
                    }

                    var textures = ParseGndTextures(gndBytes, krEnc);
                    bool isArenaMap = mapName.Equals("pvp_n_1-5", StringComparison.OrdinalIgnoreCase)
                                   || mapName.Equals("guild_vs3", StringComparison.OrdinalIgnoreCase);
                    int mapWritten = 0;

                    foreach (string texRel in textures)
                    {
                        string ext = Path.GetExtension(texRel);
                        if (!ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)) continue;

                        string relFromData = "texture\\" + texRel;
                        if (writtenSet.Contains(relFromData)) continue;

                        string grfTexKey = "data\\" + relFromData;
                        GrfEntry texEntry;
                        table.TryGetValue(grfTexKey, out texEntry);

                        byte[] rawBmp = ExtractEntry(fs, texEntry);
                        Bitmap origBmp = null;
                        bool hasMagenta = false;
                        bool isGrass = false;

                        if (rawBmp != null)
                        {
                            try
                            {
                                using (var ms = new MemoryStream(rawBmp))
                                using (var loaded = new Bitmap(ms))
                                {
                                    origBmp = new Bitmap(loaded);
                                }
                                AnalyzeBitmap(origBmp, out hasMagenta, out isGrass);
                            }
                            catch
                            {
                                origBmp = null;
                            }
                        }

                        if (!isGrass)
                        {
                            string lower = texRel.ToLowerInvariant();
                            if (lower.Contains("grass") || lower.Contains("lawn") || lower.Contains("leaf") || lower.Contains("tree"))
                            {
                                isGrass = true;
                            }
                        }

                        string outPath = Path.Combine(dataDir, relFromData);
                        string outDir = Path.GetDirectoryName(outPath);
                        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                        Bitmap outBmp = null;
                        try
                        {
                            if (mode == "remaster")
                            {
                                if (origBmp == null) continue;
                                outBmp = RemasterBitmap(origBmp, 512);
                            }
                            else // marmore
                            {
                                if (hasMagenta && origBmp != null)
                                {
                                    outBmp = RemasterBitmap(origBmp, 512);
                                }
                                else if (isGrass && !isArenaMap)
                                {
                                    outBmp = new Bitmap(grassBmp);
                                }
                                else
                                {
                                    outBmp = new Bitmap(isArenaMap ? marbleObs : marblePrt);
                                }
                            }

                            outBmp.Save(outPath, ImageFormat.Bmp);
                            writtenSet.Add(relFromData);
                            writtenRelative.Add(relFromData);
                            mapWritten++;
                        }
                        finally
                        {
                            if (outBmp != null) outBmp.Dispose();
                            if (origBmp != null) origBmp.Dispose();
                        }
                    }

                    Console.WriteLine("[OK] " + mapName + ": " + mapWritten + " textura(s) de chao HD geradas (total no .gnd: " + textures.Count + ").");
                }
            }
        }
        finally
        {
            if (marblePrt != null) marblePrt.Dispose();
            if (marbleObs != null) marbleObs.Dispose();
            if (grassBmp != null) grassBmp.Dispose();
        }

        return writtenRelative;
    }
}
'@

Add-Type -TypeDefinition $csharpCode -ReferencedAssemblies @("System.Drawing")

Write-Host "[1/2] Lendo data.grf e gerando texturas HD 512x512 para: $($Maps -join ', ')..." -ForegroundColor Cyan
$written = [AureumHdGround]::Install(
    $GrfPath,
    $DataDir,
    $Mode,
    $Maps,
    $MarbleProntera,
    $MarbleObsidian,
    $GrassHd
)

if (-not (Test-Path $TextureRoot)) {
    New-Item -ItemType Directory -Path $TextureRoot -Force | Out-Null
}
Set-Content -Path $ManifestPath -Value $written -Encoding UTF8

if (-not $SkipFog -and (Test-Path $FogSrc)) {
    Copy-Item -Path $FogSrc -Destination (Join-Path $DataDir "fogParameterTable.txt") -Force
    Write-Host "[2/2] fogParameterTable.txt (neblina limpa) instalado em data\fogParameterTable.txt." -ForegroundColor Green
} else {
    Write-Host "[2/2] Neblina mantida no padrao." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "CONCLUIDO! $($written.Count) textura(s) de chao HD instaladas no modo '$Mode'." -ForegroundColor Green
Write-Host "Dica: para trocar o modo a qualquer momento, rode:" -ForegroundColor Yellow
Write-Host "  - Modo Marmore Real + Arena Obsidiana: powershell -ExecutionPolicy Bypass -File .\scripts\install-hd-ground.ps1 -Mode marmore" -ForegroundColor Gray
Write-Host "  - Modo HD Remaster (fiel ao classico): powershell -ExecutionPolicy Bypass -File .\scripts\install-hd-ground.ps1 -Mode remaster" -ForegroundColor Gray
Write-Host "  - Voltar ao original do kRO:           powershell -ExecutionPolicy Bypass -File .\scripts\install-hd-ground.ps1 -Mode original" -ForegroundColor Gray
