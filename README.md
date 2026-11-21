# ✦ AureumRO — Servidor de Testes (rAthena Pré-Renewal 13.3 + Laboratório 4Tools)

Ambiente completo e reproduzível de servidor privado de **Ragnarök Online Pré-Renewal / Gold Times**, alinhado às especificações do [**AureumRO**](https://aureumro.com/) e projetado sob medida para validar todas as funcionalidades do [**AureumRO-4Tools**](https://github.com/iOraculo/AureumRO-4Tools).

---

## ✨ O que está incluído

| Componente | Detalhes |
| :--- | :--- |
| **Emulador** | **rAthena Pré-Renewal** compilado via Docker (`--enable-prere=yes --enable-packetver=20220406 --enable-vip=yes`), sem packet obfuscation para clientes de teste. |
| **Base AureumRO** | Episódio **13.3 El Dicastes**, cap **99/70**, ASPD máx. 190, instant cast 150 DES, comandos `@commands` estilo AureumRO e NPCs do Hub em Prontera (Curandeira, Mestra das Classes + Reborn + Skills de Platina + Reset, Teleportadora com El Dicastes, Estilista e Caixa do Iniciante). |
| **Presets de Rates** | **`sandbox` (padrão):** `100x Base / 100x Job / 50x Drop`, 1.000.000 Zeny inicial e sem penalidade de morte.<br>**`aureumro` (oficial):** `5x Base / 3x Job / 1x Drop`, penalidade clássica Pré-Renewal. |
| **Laboratório 4Tools** | NPCs dedicados em Prontera e na **Arena de Treino (`pvp_n_1-5`, fora da lista de cidades)**: Instrutor de Debuffs & Dano Contínuo (Autopot, Yggdrasil, debuffs clássicos e todos os status de **Doce de Elvira `ID 23044`**), Suprimentos Grátis, Controlador de Dummies (5M HP, DEF 0/60, Elementais, AoE), **Medidor de DPS em Tempo Real** e **Simulador de Combate / Stress Test WoE + MVPs**. |
| **Arsenal & Sistemas** | **Arsenal Divino +10** (kits automáticos por classe + **Acessórios Divinos, Ifrit, MVP & WoE** + kit troca ATK×DEF), **Mestre das Builds** (1-clique + 3 slots salvos), **Ferreiro +10 & Cartas MVP**, **Kafra/Lixeira + `@restock`/`@buff`/`@arena`/`@hub`**, **Sala MVP Privada (`guild_vs3`), Torre Sem Fim sem cooldown, WoE Testes**, **Roleta Poring, Guarda-Roupa Visual (Costumes), Auras Contínuas e Homúnculos Express**. |
| **Painel Web** | Identidade oficial **Obsidiana & Ouro Real** (com alternador para **Pergaminho**), brasão + arte hero, relógio ao vivo, status pulsante de serviços, cadastro de contas, ranking + **Gráfico de Distribuição de Classes**, **Mapa Interativo do Hub de Prontera**, **Ferramentas GM & Logs em tempo real**, toasts dourados, `clientinfo.xml` e gerador de `supported_servers.json`. |
| **Visuais do Cliente** | Tela de Login e Loading Screens customizadas Obsidiana & Ouro Real, skin **`AureumRO_Obsidian`** e alternador de **Graymap (`chaopreto.grf`)** via scripts PowerShell. |
| **AureumRO-4Tools Integrado** | Código adaptado em [`tools/AureumRO-4Tools/`](tools/AureumRO-4Tools/) com suporte nativo a `FroggoClient` e `Ragexe` (incluindo `mapAddress` para Pausa na Cidade), **Mini HUD** com mapa/buffs/badge de pausa (`ON`/`CHAT`/`CIDADE`/`FOCO`/`OFF`), selo **`MODO TESTE`** e build automático via GitHub Actions. |

---

## 🚀 Como Subir o Servidor

### Pré-requisitos
- **Docker** e **Docker Compose** (Linux ou Windows com Docker Desktop / WSL2)

### Passo a passo

```bash
# 1. Crie o arquivo de ambiente local
cp .env.example .env

# 2. (Opcional) Se for acessar de outra máquina na LAN/VPS, edite SERVER_PUBLIC_IP no .env

# 3. Compile o rAthena Pré-Renewal e suba toda a stack (MariaDB + rAthena + Painel Web)
docker compose up -d --build
# ou usando o Makefile:
make up
```

> **Primeira compilação:** o primeiro `docker compose up --build` clona e compila o rAthena em Pré-Renewal (`PACKETVER=20220406`, `RATHENA_MAKE_JOBS=2`, otimizado para VMs com 2 GB RAM). Nas próximas execuções, o container inicia em segundos e converte automaticamente os scripts PT-BR para **Windows-1252** no boot.

### Acessos após subir
- **Painel Web (Obsidiana & Ouro):** `http://localhost:8080`
- **Login Server:** `127.0.0.1:6900`
- **Char Server:** `127.0.0.1:6121`
- **Map Server:** `127.0.0.1:5121`

### Contas prontas de fábrica
| Usuário | Senha | Nível | Uso |
| :--- | :--- | :--- | :--- |
| `admin` | `admin123` | **GM 99** | Administração total (`@reloadscript`, `@item`, `@blvl`, `@monster`, `@warp`, etc.) |
| `tester` | `tester123` | **Grupo 1 (Tester)** | Comandos de sandbox (`@blvl`, `@jlvl`, `@jobchange`, `@allskill`, `@item`, `@zeny`, `@heal`, `@warp`, `@refine`) |

Você também pode criar novas contas pelo **Painel Web** (`http://localhost:8080`) ou diretamente na tela de login do jogo usando o sufixo `_M` ou `_F` (ex.: `oraculo_M`).

---

## 🔄 Alternar Presets de Rates

Troque instantaneamente entre **Sandbox (`100x/100x/50x`)** e **AureumRO Oficial (`5x/3x/1x`)** pelo Painel Web (aba **Ferramentas GM & Logs**) ou via terminal:

```bash
# Modo Sandbox (padrão de testes)
make preset-sandbox

# Modo AureumRO Oficial (5x / 3x / 1x)
make preset-aureumro
```

---

## 🧪 Os 16 NPCs do Hub AureumRO & Laboratório 4Tools

Todos os novos personagens nascem em **`prontera (156, 191)`**, cercados por 4 alas simétricas:

| Ala | NPC | Localização | Função |
| :--- | :--- | :--- | :--- |
| **Centro** | **Guia AureumRO** | `prontera (156, 195)` | Diretório das alas, guia 4Tools, comandos e teleporte rápido |
| **Oeste** | **Curandeira AureumRO** | `prontera (152, 193)` e Arena | Cura com buffs, cura **sem buffs** (Autobuff) e Dispell |
| **Oeste** | **Mestra das Classes** | `prontera (150, 191)` e Arena | **Transclasse / Expandida direta 99/70 com `@allskill`**, Platina, Reset e Aluguel |
| **Oeste** | **Arsenal Divino (+10)** | `prontera (148, 193)` e Arena | Kits Divinos +10 com cartas MVP, **submenu de Acessórios (Megin, Bris, Ifrit, Diabolus, Orleans, Rosários)** e kit DEF/WoE |
| **Oeste** | **Caixa do Iniciante** | `prontera (148, 189)` | Kit de sobrevivência inicial (poções, asas, manuais, Zeny) |
| **Oeste** | **Ferreiro & Cartas MVP** | `prontera (146, 195)` e Arena | Refinador seguro +10, Removedor de Cartas grátis e catálogo MVP |
| **Oeste** | **Mestre das Builds** | `prontera (146, 191)` e Arena | Presets de atributos 1 clique + salvar/carregar até 3 builds |
| **Leste** | **Teleportadora AureumRO** | `prontera (160, 193)` | Cidades (incl. El Dicastes 13.3), Campos, Calabouços, MVP e Arena |
| **Leste** | **Estilista AureumRO** | `prontera (162, 191)` | Cabelo e roupas (Modo Seguro `0..3` para kRO + Modo Expandido `0..553`) |
| **Leste** | **Sistemas AureumRO** | `prontera (164, 193)` → `guild_vs3` | **Sala MVP Privada**, reset Torre Sem Fim, WoE sob demanda e ranking MVP |
| **Leste** | **Kafra & Lixeira** | `prontera (164, 189)` e Arena | Armazém universal, Lixeira e comandos globais `@restock`, `@buff`, `@arena`, `@hub` |
| **Leste** | **Roleta & Visuais** | `prontera (166, 195)` | **Guarda-Roupa Visual (Costumes)**, **Auras Contínuas persistentes**, Roleta Poring e Homúnculos Express |
| **Sul (4Tools)** | **Instrutor de Debuffs** | `prontera (152, 185)` e Arena | Dano único, **5 pulsos contínuos**, debuffs clássicos e **Status Elvira** |
| **Sul (4Tools)** | **Suprimentos 4Tools** | `prontera (156, 185)` e Arena | Poções, Yggdrasil, Panaceia, Água Benta, **Doce de Elvira (`23044`)**, Comidas +10 e 10M Zeny |
| **Sul (4Tools)** | **Arena de Treino 4Tools** | `prontera (160, 185)` → `pvp_n_1-5` | Mapa fora de cidade com **Controlador de Dummies** (5M HP, DEF 0/60, Elementais, AoE) |
| **Arena** | **Medidor de DPS & Simulador** | `pvp_n_1-5 (100, 90)` / `(92, 98)` | **DPS Meter em tempo real** + **Stress Test WoE** (ondas 20s/30s) + Spawner MVP |

---

## 🎨 Scripts PowerShell (Cliente & Operação no Windows)

Rode com `-ExecutionPolicy Bypass` no PowerShell:

```powershell
# Instalar Tela de Login, Loading Screens e Skin AureumRO_Obsidian no cliente
powershell -ExecutionPolicy Bypass -File .\scripts\install-client-visuals.ps1

# Instalar Chao HD 512x512 (modos: marmore | remaster | original) + Neblina limpa
powershell -ExecutionPolicy Bypass -File .\scripts\install-hd-ground.ps1 -Mode marmore

# Ativar Graficos HD globais (dgVoodoo2: DX11 + Anisotropico 16x + MSAA 4x)
powershell -ExecutionPolicy Bypass -File .\scripts\install-dgvoodoo-hd.ps1

# Ativar / Desativar Graymap (chaopreto.grf) no DATA.INI
powershell -ExecutionPolicy Bypass -File .\scripts\toggle-graymap.ps1 on

# Descobrir/validar enderecos de memoria do cliente para o 4Tools
powershell -ExecutionPolicy Bypass -File .\scripts\find-4tools-addresses.ps1

# Backup e Restore do banco MariaDB
powershell -ExecutionPolicy Bypass -File .\scripts\backup-db.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\restore-db.ps1 backups\aureumro_YYYYMMDD_HHMMSS.sql
```

Consulte também [`client/README_CLIENTE.md`](client/README_CLIENTE.md) para detalhes da preparação do cliente **Ragexe 2022-04-06**.

---

## 🛠️ Validação e Comandos Úteis

```bash
# Validar sintaxe dos 16 NPCs e arquivos de configuração
make validate

# Rodar testes automatizados do Painel Web
make test-web

# Verificar saúde dos serviços (TCP 6900/6121/5121 + API Painel Web)
make smoke

# Acompanhar logs do rAthena
make logs
```

---

## 📁 Estrutura do Repositório

```text
├── docker-compose.yml            # Orquestração MariaDB + rAthena Pré-Renewal + Painel Web
├── Makefile                      # Atalhos operacionais (up, down, presets, validate, smoke)
├── .github/workflows/            # CI Windows: compila e testa AureumRO-4Tools e publica artefato .zip
├── docker/
│   ├── rathena/                  # Dockerfile multi-stage Pré-Renewal + entrypoint (CP1252) + healthcheck
│   └── db/seed-accounts.sql      # Seed idempotente (inter-server s1/p1, admin GM 99, tester)
├── config/
│   ├── import/                   # Sobreposições rAthena (inter/login/char/map/battle/groups/motd)
│   └── presets/                  # sandbox.conf (100x/100x/50x) e aureumro.conf (5x/3x/1x)
├── db/import/item_db.yml         # Doce de Elvira (23044) e Costumes visuais portados para Pré-Renewal
├── npc/
│   ├── scripts_aureumro.conf     # Registro dos 16 scripts customizados
│   └── aureumro/                 # NPCs do Hub (00..15), Arsenal Divino, Laboratório 4Tools e Sistemas
├── tools/AureumRO-4Tools/        # 4Tools adaptado (FroggoClient/Ragexe, Mini HUD, selo MODO TESTE + testes xUnit)
├── webpanel/                     # Painel Web Node.js/Express (Obsidiana & Ouro, Mapa Interativo, GM Tools, Logs)
├── client/                       # clientinfo.xml, supported_servers.test.json, visuals/ (Login & Loading)
└── scripts/                      # Scripts PowerShell (visuais, graymap, palette, endereços, backup) e validação
```
