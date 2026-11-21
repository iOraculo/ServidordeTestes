# ✦ AureumRO — Servidor de Testes (rAthena Pré-Renewal 13.3 + Laboratório 4Tools)

Ambiente completo e reproduzível de servidor privado de **Ragnarök Online Pré-Renewal / Gold Times**, alinhado às especificações do [**AureumRO**](https://aureumro.com/) e projetado sob medida para validar todas as funcionalidades do [**AureumRO-4Tools**](https://github.com/iOraculo/AureumRO-4Tools).

---

## ✨ O que está incluído

| Componente | Detalhes |
| :--- | :--- |
| **Emulador** | **rAthena Pré-Renewal** compilado via Docker (`--enable-prere=yes --enable-packetver=20220406 --enable-vip=yes`), sem packet obfuscation para clientes de teste. |
| **Base AureumRO** | Episódio **13.3 El Dicastes**, cap **99/70**, ASPD máx. 190, instant cast 150 DES, comandos `@commands` estilo AureumRO e NPCs do Hub em Prontera (Curandeira, Mestra das Classes + Reborn + Skills de Platina + Reset, Teleportadora com El Dicastes, Estilista e Caixa do Iniciante). |
| **Presets de Rates** | **`sandbox` (padrão):** `100x Base / 100x Job / 50x Drop`, 1.000.000 Zeny inicial e sem penalidade de morte.<br>**`aureumro` (oficial):** `5x Base / 3x Job / 1x Drop`, penalidade clássica Pré-Renewal. |
| **Laboratório 4Tools** | NPCs dedicados em Prontera e na **Arena de Treino (`pvp_n_1-5`, fora da lista de cidades)**: Instrutor de Debuffs & Dano Contínuo (Autopot, Yggdrasil, todos os debuffs clássicos e todos os status de **Doce de Elvira `ID 23044`**), Suprimentos Grátis e Controlador de Dummies (5.000.000 HP, elementos/tamanhos/DEF configuráveis para testar AHK, Macros e ATK × DEF). |
| **Painel Web** | Interface na identidade oficial **Obsidiana & Ouro Real** (com alternador para **Pergaminho & Ouro Antigo**): status ao vivo de Login/Char/Map/MariaDB, cadastro de contas, ranking/personagens, download dinâmico de `clientinfo.xml` e gerador de `supported_servers.json` para o 4Tools. |

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

> **Primeira compilação:** o primeiro `docker compose up --build` clona e compila o rAthena em Pré-Renewal (leva alguns minutos dependendo da CPU). Nas próximas execuções, o container inicia em segundos.

### Acessos após subir
- **Painel Web (Obsidiana & Ouro):** `http://localhost:8080`
- **Login Server:** `127.0.0.1:6900`
- **Char Server:** `127.0.0.1:6121`
- **Map Server:** `127.0.0.1:5121`

### Contas prontas de fábrica
| Usuário | Senha | Nível | Uso |
| :--- | :--- | :--- | :--- |
| `admin` | `admin123` | **GM 99** | Administração total (`@reloadscript`, `@item`, `@blvl`, `@monster`, `@warp`, etc.) |
| `tester` | `tester123` | **Grupo 1 (Tester)** | Comandos de sandbox (`@blvl`, `@jlvl`, `@jobchange`, `@item`, `@zeny`, `@heal`, `@warp`, `@refine`) |

Você também pode criar novas contas pelo **Painel Web** (`http://localhost:8080`) ou diretamente na tela de login do jogo usando o sufixo `_M` ou `_F` (ex.: `oraculo_M`).

---

## 🔄 Alternar Presets de Rates

Troque instantaneamente entre **Sandbox (`100x/100x/50x`)** e **AureumRO Oficial (`5x/3x/1x`)** sem perder personagens ou contas:

```bash
# Modo Sandbox (padrão de testes)
make preset-sandbox

# Modo AureumRO Oficial (5x / 3x / 1x)
make preset-aureumro
```

---

## 🧪 NPCs do Hub AureumRO & Laboratório 4Tools

Todos os novos personagens nascem em **`prontera (156, 191)`**:

| NPC | Localização | Função |
| :--- | :--- | :--- |
| **Guia AureumRO** | `prontera (156, 195)` | Visão geral, lista de comandos e teleporte rápido para a Arena 4Tools |
| **Curandeira AureumRO** | `prontera (152, 193)` e Arena | Cura com buffs, cura **sem buffs** (para testar Autobuff) e Dispell |
| **Mestra das Classes** | `prontera (150, 191)` | 1ª, 2ª, Reborn 99/70, Expandidas, Skills de Platina, Aluguéis, Reset e Atalho 99/70 |
| **Teleportadora AureumRO** | `prontera (160, 193)` | Cidades (incl. El Dicastes 13.3), Campos, Calabouços, MVP e Arena 4Tools |
| **Estilista AureumRO** | `prontera (162, 191)` | Cabelo, cor de cabelo e cor de roupa grátis |
| **Caixa do Iniciante** | `prontera (148, 189)` | Kit de sobrevivência inicial (poções, asas, manuais, 100k Zeny) |
| **Instrutor de Debuffs** | `prontera (152, 185)` e `pvp_n_1-5 (96, 104)` | Aplica dano único, **5 pulsos contínuos** (Autopot/Ygg), debuffs clássicos e todos os **Status Elvira** |
| **Suprimentos 4Tools** | `prontera (156, 185)` e `pvp_n_1-5 (100, 104)` | Kits grátis: Autopot, Cura de Status + **Doce de Elvira (`23044`)**, Buffs, Conversores, Comidas +10, Gemas/EDP/AD |
| **Arena de Treino 4Tools** | `prontera (160, 185)` → `pvp_n_1-5 (104, 104)` | Mapa **fora de cidade** com **Controlador de Dummies** (5M HP, DEF 0/60, Elementais, Humanoide, AoE) |

---

## 💻 Cliente Ragnarok (Ragexe 2022-04-06) & Integração 4Tools

Consulte o guia completo em [`client/README_CLIENTE.md`](client/README_CLIENTE.md) para:
1. Preparar seu cliente **Ragexe 2022-04-06** sem Gepard/anti-cheat (patches WARP/NEMO).
2. Instalar o `clientinfo.xml` (template em `client/clientinfo.xml` ou download pronto em `http://localhost:8080/api/clientinfo.xml`).
3. Descobrir os endereços `hpAddress`, `nameAddress` e `mapAddress` do seu `Ragexe.exe` e registrá-lo no **AureumRO-4Tools**.

---

## 🛠️ Validação e Comandos Úteis

```bash
# Validar sintaxe dos NPCs e arquivos de configuração
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
├── docker/
│   ├── rathena/                  # Dockerfile multi-stage Pré-Renewal + entrypoint + healthcheck
│   └── db/seed-accounts.sql      # Seed idempotente (inter-server s1/p1, admin GM 99, tester)
├── config/
│   ├── import/                   # Sobreposições rAthena (inter/login/char/map/battle/groups/motd)
│   └── presets/                  # sandbox.conf (100x/100x/50x) e aureumro.conf (5x/3x/1x)
├── db/import/item_db.yml         # Doce de Elvira (ID 23044) portado para Pré-Renewal
├── npc/
│   ├── scripts_aureumro.conf     # Registro de scripts customizados
│   └── aureumro/                 # NPCs do Hub AureumRO (00..05) e Laboratório 4Tools (06..08)
├── webpanel/                     # Painel Web Node.js/Express (Obsidiana & Ouro + testes)
├── client/                       # clientinfo.xml, supported_servers.test.json e README_CLIENTE.md
└── scripts/                      # switch-preset.sh, validate-scripts.py e smoke-test.sh
```
