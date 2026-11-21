#Requires -Version 5.1
<#
.SYNOPSIS
    Localiza automaticamente os endereços estáticos (hpAddress, nameAddress e mapAddress)
    do processo Ragexe.exe em execução e gera o supported_servers.json para o AureumRO-4Tools,
    sem precisar abrir o Cheat Engine.

.EXAMPLE
    .\scripts\find-4tools-addresses.ps1
.EXAMPLE
    .\scripts\find-4tools-addresses.ps1 -ProcessName Ragexe -OutputJson "C:\AureumRO-4Tools\supported_servers.json"
#>
param(
    [string]$ProcessName = "Ragexe",
    [string]$OutputJson = "",
    [string]$ServerDescription = "AureumRO Servidor de Testes (rAthena Pré-Renewal 13.3 · PACKETVER 20220406)"
)

$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public static class RoMemoryScanner
{
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private static byte[] ReadMainModuleBytes(Process proc, out long baseAddress)
    {
        ProcessModule mod = proc.MainModule;
        baseAddress = mod.BaseAddress.ToInt64();
        int totalSize = mod.ModuleMemorySize;
        byte[] full = new byte[totalSize];
        IntPtr handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, proc.Id);
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Não foi possível abrir o processo. Execute o PowerShell como Administrador.");
        }

        try
        {
            const int pageSize = 4096;
            byte[] page = new byte[pageSize];
            for (int offset = 0; offset < totalSize; offset += pageSize)
            {
                int toRead = Math.Min(pageSize, totalSize - offset);
                int read = 0;
                if (ReadProcessMemory(handle, new IntPtr(baseAddress + offset), page, toRead, out read) && read > 0)
                {
                    Buffer.BlockCopy(page, 0, full, offset, read);
                }
            }
        }
        finally
        {
            CloseHandle(handle);
        }

        return full;
    }

    public static List<long> FindVitalsAddresses(Process proc, uint hp, uint maxHp, uint sp, uint maxSp)
    {
        long baseAddr;
        byte[] data = ReadMainModuleBytes(proc, out baseAddr);
        byte[] pattern = new byte[16];
        Buffer.BlockCopy(BitConverter.GetBytes(hp), 0, pattern, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(maxHp), 0, pattern, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(sp), 0, pattern, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(maxSp), 0, pattern, 12, 4);

        var matches = new List<long>();
        int limit = data.Length - 16;
        for (int i = 0; i <= limit; i += 4)
        {
            bool ok = true;
            for (int j = 0; j < 16; j++)
            {
                if (data[i + j] != pattern[j]) { ok = false; break; }
            }
            if (ok)
            {
                matches.Add(baseAddr + i);
            }
        }
        return matches;
    }

    public static List<long> FindAsciiStringAddresses(Process proc, string text)
    {
        long baseAddr;
        byte[] data = ReadMainModuleBytes(proc, out baseAddr);
        byte[] needle = Encoding.Default.GetBytes(text);
        var matches = new List<long>();
        int limit = data.Length - needle.Length - 1;

        for (int i = 0; i <= limit; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (data[i + j] != needle[j]) { ok = false; break; }
            }
            if (ok && data[i + needle.Length] == 0)
            {
                // Garante início de string (byte anterior 0 ou início de bloco)
                if (i == 0 || data[i - 1] == 0)
                {
                    matches.Add(baseAddr + i);
                }
            }
        }
        return matches;
    }
}
"@

Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  ✦ AureumRO-4Tools — Scanner Automático de Endereços" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

$cleanProc = $ProcessName -replace '\.exe$', ''
$procs = Get-Process -Name $cleanProc -ErrorAction SilentlyContinue
if (-not $procs) {
    Write-Host "[ERRO] Processo '$cleanProc.exe' não está rodando." -ForegroundColor Red
    Write-Host "Abra o jogo, faça login com seu personagem em Prontera e rode este script novamente." -ForegroundColor Yellow
    exit 1
}
$targetProc = $procs | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero } | Select-Object -First 1
if (-not $targetProc) { $targetProc = $procs[0] }

Write-Host "Processo encontrado: $($targetProc.ProcessName) (PID $($targetProc.Id))" -ForegroundColor Green
Write-Host "Informe os valores EXATOS exibidos no jogo agora:" -ForegroundColor Cyan

$charName = (Read-Host "Nome exato do personagem").Trim()
[uint32]$curHp = [uint32](Read-Host "HP Atual (ex.: 1540)")
[uint32]$maxHp = [uint32](Read-Host "HP Máximo (ex.: 1540)")
[uint32]$curSp = [uint32](Read-Host "SP Atual (ex.: 220)")
[uint32]$maxSp = [uint32](Read-Host "SP Máximo (ex.: 220)")
$currentMap = (Read-Host "Mapa atual (Enter para 'prontera.rsw')").Trim()
if ([string]::IsNullOrWhiteSpace($currentMap)) { $currentMap = "prontera.rsw" }

Write-Host "`nEscaneando módulo estático de $cleanProc.exe..." -ForegroundColor Yellow

$hpMatches = [RoMemoryScanner]::FindVitalsAddresses($targetProc, $curHp, $maxHp, $curSp, $maxSp)
$nameMatches = [RoMemoryScanner]::FindAsciiStringAddresses($targetProc, $charName)
$mapMatches = [RoMemoryScanner]::FindAsciiStringAddresses($targetProc, $currentMap)

function Format-Hex([long]$addr) {
    return ("0x{0:X8}" -f $addr)
}

if ($hpMatches.Count -eq 0) {
    Write-Host "[AVISO] Nenhum hpAddress encontrado para HP=$curHp/$maxHp SP=$curSp/$maxSp." -ForegroundColor Red
    Write-Host "Dica: verifique se digitou os valores exatos e se o PowerShell está aberto como Administrador." -ForegroundColor Yellow
    exit 1
}

if ($nameMatches.Count -eq 0) {
    Write-Host "[AVISO] Nenhum nameAddress encontrado para '$charName'." -ForegroundColor Red
    exit 1
}

$bestHp = Format-Hex $hpMatches[0]
$bestName = Format-Hex $nameMatches[0]
$bestMap = if ($mapMatches.Count -gt 0) { Format-Hex $mapMatches[0] } else { $null }

Write-Host "`nEndereços estáticos encontrados:" -ForegroundColor Green
Write-Host "  hpAddress   : $bestHp  (total candidatos: $($hpMatches.Count))" -ForegroundColor White
if ($hpMatches.Count -gt 1) {
    Write-Host "    outros HP : $(($hpMatches | Select-Object -Skip 1 | ForEach-Object { Format-Hex $_ }) -join ', ')" -ForegroundColor DarkGray
}
Write-Host "  nameAddress : $bestName  (total candidatos: $($nameMatches.Count))" -ForegroundColor White
if ($nameMatches.Count -gt 1) {
    Write-Host "    outros    : $(($nameMatches | Select-Object -Skip 1 | ForEach-Object { Format-Hex $_ }) -join ', ')" -ForegroundColor DarkGray
}
if ($bestMap) {
    Write-Host "  mapAddress  : $bestMap  (total candidatos: $($mapMatches.Count))" -ForegroundColor White
} else {
    Write-Host "  mapAddress  : (não encontrado para '$currentMap' — opcional)" -ForegroundColor DarkYellow
}

$entry = [ordered]@{
    name        = $cleanProc
    description = $ServerDescription
    hpAddress   = $bestHp
    nameAddress = $bestName
}
if ($bestMap) {
    $entry.mapAddress = $bestMap
}

$servers = @(
    [ordered]@{
        name        = "AureumRO"
        description = "AureumRO — O goldtimes que você sempre sonhou"
        hpAddress   = "0x015FF908"
        nameAddress = "0x01602568"
    },
    $entry
)

if ([string]::IsNullOrWhiteSpace($OutputJson)) {
    $OutputJson = Join-Path $PSScriptRoot "..\client\supported_servers.generated.json"
}

$jsonText = $servers | ConvertTo-Json -Depth 4
Set-Content -Path $OutputJson -Value $jsonText -Encoding UTF8

Write-Host "`nArquivo gerado com sucesso em:" -ForegroundColor Green
Write-Host "  $((Resolve-Path $OutputJson).Path)" -ForegroundColor Cyan
Write-Host "`nCopie esse arquivo como 'supported_servers.json' na pasta do AureumRO 4Tools.exe" -ForegroundColor White
Write-Host "ou cadastre os endereços acima diretamente na aba Servidores -> Testar Endereços!" -ForegroundColor White
