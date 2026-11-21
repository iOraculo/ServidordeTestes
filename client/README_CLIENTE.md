# Guia do Cliente (Ragexe 2022-04-06) e Integração com o AureumRO-4Tools

## ⚡ Atalho Automatizado no Windows (PowerShell)

Você pode automatizar a montagem da pasta do cliente e a descoberta dos endereços do `AureumRO-4Tools` (sem Cheat Engine):

```powershell
# 1. Monta C:\AureumRO\ClienteTestes (copia GRFs sem anti-cheat + instala clientinfo.xml + cria lançador)
.\scripts\setup-client.ps1

# 2. Depois de entrar no jogo com seu Ragexe.exe 2022-04-06, descobre hpAddress/nameAddress/mapAddress
#    automaticamente e gera o supported_servers.json pronto para o AureumRO-4Tools:
.\scripts\find-4tools-addresses.ps1
```

---

## 1. Por que usar um Ragexe limpo (sem Gepard/OdinAnti-Cheat)?

O cliente oficial do [AureumRO](https://aureumro.com/) utiliza **Gepard 3 + OdinAnti-Cheat**, que exige o módulo server-side licenciado para autenticar conexões.  
Para o seu **Servidor de Testes local**, o padrão mais prático é usar um cliente **Ragexe `2022-04-06`** sem anti-cheat (compatível com `RATHENA_PACKETVER=20220406` configurado neste repositório).

---

## 2. Preparando o Cliente Ragexe 2022-04-06

1. Tenha uma pasta base de Ragnarok Online com os arquivos `.grf` (kRO/bRO) e a pasta `System/`.
2. Baixe o executável `2022-04-06_Ragexe.exe` (via repositório comunitário kRO/NEMO ou WARP) e aplique os patches recomendados no **WARP / NEMO**:
   - **Read Data Folder First** (lê `data/clientinfo.xml` antes da GRF)
   - **Read msgstringtable.txt**
   - **Disable Packet Encryption** *(necessário porque `RATHENA_DISABLE_OBFUSCATION=1` está ativo no servidor)*
   - **Disable HShield / GameGuard**
   - **Enable Multiple GRFs** (opcional)
   - **Allow Multiple Windows** (útil para testar dual/suporte no 4Tools)
   - **Use Custom Aura / Normal Guild Brackets / Increase Zoom Out** (opcional)
   - **Skip Resurrection Buttons / Disable Nagle Algorithm** (opcional)
3. Copie `client/clientinfo.xml` (ou baixe-o pelo Painel Web em `http://localhost:8080/api/clientinfo.xml`) para `data/clientinfo.xml` dentro da pasta do seu cliente.
   - Se o servidor estiver em outra máquina/VPS, altere `SERVER_PUBLIC_IP` no `.env` e baixe novamente o `clientinfo.xml`.
4. (Opcional — para exibir o ícone do **Doce de Elvira** `ID 23044` corretamente):
   - Verifique se `System/itemInfo.lub` (ou `itemInfo_true.lub`) já possui a entrada `[23044]` (clientes 2022 já incluem por padrão).
5. Execute `Ragexe.exe` e entre com:
   - **GM Admin:** `admin` / `admin123`
   - **Tester:** `tester` / `tester123`
   - Ou crie uma nova conta no Painel Web (`http://localhost:8080`) ou usando `usuario_M` / `usuario_F` direto na tela de login.

---

## 3. Conectando o AureumRO-4Tools ao Servidor de Testes

O [AureumRO-4Tools](https://github.com/iOraculo/AureumRO-4Tools) lê da memória do processo do jogo:
- `hpAddress`: bloco de 16 bytes (`CurrentHP`, `MaxHP`, `CurrentSP`, `MaxSP`) — e `hpAddress + 0x474` para a tabela de status ativos (`EFST_*`).
- `nameAddress`: string terminada em nulo com o nome do personagem.
- `mapAddress` *(opcional)*: string com o nome do mapa atual (usado para a pausa automática em cidades como `prontera.rsw`).

### Como encontrar os endereços do seu `Ragexe.exe` (2022-04-06)

1. Entre no jogo com seu personagem de teste e abra o **Cheat Engine** anexado ao processo `Ragexe.exe`.
2. **HP Address (`hpAddress`)**:
   - Busque pelo seu **HP atual** (`4 Bytes`).
   - Fale com o **Instrutor de Debuffs** (`prontera 152,185`) para reduzir o HP e filtre pelo novo valor até encontrar o endereço **estático (verde)** onde os próximos 12 bytes são `MaxHP`, `CurrentSP` e `MaxSP`.
3. **Name Address (`nameAddress`)**:
   - Busque como `String` pelo nome exato do seu personagem até encontrar o endereço estático (verde) persistente.
4. **Map Address (`mapAddress`, opcional)**:
   - Em Prontera, busque como `String` por `prontera.rsw`; entre na **Arena de Treino 4Tools** e filtre por `pvp_n_1-5.rsw` até achar o endereço estático.

### Registrando no AureumRO-4Tools

- **Opção A (via interface do 4Tools):** abra a aba **Servidores**, informe o nome do processo (`Ragexe`), `hpAddress` e `nameAddress`, e clique em **Testar Endereços** (`Client.TestMemoryAddresses`) para confirmar que HP, SP, Nome e Buffs ativos são lidos corretamente.
- **Opção B (via JSON):** use a aba **Integração 4Tools** no Painel Web (`http://localhost:8080`) para baixar `supported_servers.json` e substitua na pasta do `AureumRO 4Tools.exe`.
