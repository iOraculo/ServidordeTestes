#!/usr/bin/env python3
"""Validador estático dos arquivos de configuração e scripts NPC do AureumRO Servidor de Testes."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent


def check_balanced_delimiters(path: Path, text: str) -> list[str]:
    errors = []
    stack = []
    pairs = {"{": "}", "(": ")", "[": "]"}
    closing = {v: k for k, v in pairs.items()}
    in_string = False
    escape = False
    in_block_comment = False

    for lineno, line in enumerate(text.splitlines(), 1):
        i = 0
        n = len(line)
        while i < n:
            ch = line[i]
            nxt = line[i + 1] if i + 1 < n else ""

            if in_block_comment:
                if ch == "*" and nxt == "/":
                    in_block_comment = False
                    i += 2
                    continue
                i += 1
                continue

            if in_string:
                if escape:
                    escape = False
                elif ch == "\\":
                    escape = True
                elif ch == '"':
                    in_string = False
                i += 1
                continue

            if ch == "/" and nxt == "/":
                break
            if ch == "/" and nxt == "*":
                in_block_comment = True
                i += 2
                continue
            if ch == '"':
                in_string = True
                i += 1
                continue

            if ch in pairs:
                stack.append((ch, lineno))
            elif ch in closing:
                if not stack or stack[-1][0] != closing[ch]:
                    errors.append(f"{path.relative_to(ROOT)}:{lineno}: delimitador '{ch}' sem abertura correspondente")
                else:
                    stack.pop()
            i += 1

    for opener, lineno in stack:
        errors.append(f"{path.relative_to(ROOT)}:{lineno}: delimitador '{opener}' não fechado")
    return errors


def validate_npc_file(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    errors = check_balanced_delimiters(path, text)

    header_found = False
    for lineno, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if not stripped or stripped.startswith("//"):
            continue
        if "\tscript\t" in line or "\tduplicate(" in line or "\tmapflag\t" in line:
            header_found = True
            parts = line.split("\t")
            if "\tscript\t" in line and len(parts) < 4:
                errors.append(f"{path.relative_to(ROOT)}:{lineno}: header 'script' malformado (esperado 4 colunas separadas por TAB)")
            if "\tduplicate(" in line and len(parts) < 4:
                errors.append(f"{path.relative_to(ROOT)}:{lineno}: header 'duplicate' malformado (esperado 4 colunas separadas por TAB)")
    if not header_found:
        errors.append(f"{path.relative_to(ROOT)}: nenhum cabeçalho NPC (script/duplicate/mapflag) com separador TAB encontrado")

    return errors


def main() -> int:
    errors: list[str] = []

    required_files = [
        "docker-compose.yml",
        ".env.example",
        "docker/rathena/Dockerfile",
        "docker/rathena/entrypoint.sh",
        "docker/rathena/healthcheck.sh",
        "docker/db/seed-accounts.sql",
        "config/import/inter_conf.txt.tmpl",
        "config/import/login_conf.txt",
        "config/import/char_conf.txt.tmpl",
        "config/import/map_conf.txt.tmpl",
        "config/import/battle_conf.txt",
        "config/import/packet_conf.txt",
        "config/import/groups.yml",
        "config/import/motd.txt",
        "config/presets/sandbox.conf",
        "config/presets/aureumro.conf",
        "db/import/item_db.yml",
        "npc/scripts_aureumro.conf",
        "client/clientinfo.xml",
        "client/supported_servers.test.json",
    ]
    for rel in required_files:
        if not (ROOT / rel).exists():
            errors.append(f"Arquivo obrigatório ausente: {rel}")

    scripts_conf = ROOT / "npc/scripts_aureumro.conf"
    registered = set()
    if scripts_conf.exists():
        for lineno, line in enumerate(scripts_conf.read_text(encoding="utf-8").splitlines(), 1):
            m = re.match(r"^\s*npc:\s*(\S+)\s*$", line)
            if m:
                rel_npc = m.group(1)
                registered.add(rel_npc)
                target = ROOT / rel_npc
                if not target.exists():
                    errors.append(f"npc/scripts_aureumro.conf:{lineno}: script referenciado não existe ({rel_npc})")

    npc_dir = ROOT / "npc/aureumro"
    npc_files = sorted(npc_dir.glob("*.txt"))
    for npc_file in npc_files:
        rel_str = str(npc_file.relative_to(ROOT)).replace("\\", "/")
        if rel_str not in registered:
            errors.append(f"{rel_str}: arquivo NPC não registrado em npc/scripts_aureumro.conf")
        errors.extend(validate_npc_file(npc_file))

    if errors:
        print("Falhas encontradas na validação:")
        for err in errors:
            print(f"  - {err}")
        return 1

    print(f"OK! {len(required_files)} arquivos base e {len(npc_files)} scripts NPC validados sem erros.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
