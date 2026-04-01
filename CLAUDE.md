# T20 Master

Sistema de apoio para mestres de RPG Tormenta 20 (T20). Plataforma web para gerenciamento de campanhas, fichas de personagens e consulta de regras via IA.

## Stack

- **Runtime:** .NET 10 (Blazor Server)
- **Banco:** PostgreSQL + pgvector (docker-compose na porta 5433)
- **Auth:** ASP.NET Core Identity (cookie auth via HTTP POST, nao via SignalR)
- **ORM:** Entity Framework Core
- **IA:** Google Gemini API (free tier) com RAG sobre PDF do T20
- **Embeddings:** gemini-embedding-001 (3072 dimensoes, sem indice HNSW — busca sequencial)
- **Deploy:** Docker na Railway

## Estrutura do Projeto

```
T20Master/
├── src/T20Master.Web/
│   ├── Program.cs                    # Config, DI, auth endpoints, CLI commands
│   ├── Components/
│   │   ├── Layout/                   # MainLayout, NavMenu (+ .razor.css scoped)
│   │   ├── Pages/
│   │   │   ├── Home.razor            # Landing page
│   │   │   ├── Auth/                 # Login, Register (SSR, sem InteractiveServer)
│   │   │   ├── Dashboard/            # Dashboard.razor
│   │   │   ├── Campaign/             # Create, View, Join
│   │   │   ├── Character/            # List, Create, Edit (InteractiveServer)
│   │   │   └── AI/                   # Chat.razor (InteractiveServer)
│   │   └── Shared/                   # RedirectToLogin
│   ├── Data/
│   │   ├── ApplicationUser.cs        # IdentityUser + DisplayName
│   │   ├── ApplicationDbContext.cs   # DbContext com pgvector
│   │   ├── Entities/                 # Campaign, CampaignPlayer, CharacterSheet, RuleChunk, ChatHistory
│   │   ├── Models/                   # DTOs JSON: ClassEntry, SkillEntry, etc.
│   │   └── T20Reference.cs           # Listas estaticas de racas, classes, pericias
│   ├── Services/
│   │   ├── CampaignService.cs
│   │   ├── CharacterService.cs
│   │   └── AI/
│   │       ├── PdfProcessor.cs       # Extracao PDF (2 colunas) + chunking
│   │       ├── EmbeddingService.cs   # Gemini embedding via REST direto (nao SDK)
│   │       ├── RagIndexer.cs         # Indexacao com retry + failed-chunks.json
│   │       ├── RagService.cs         # Busca vetorial + contexto fichas
│   │       └── GeminiChatService.cs  # Chat streaming via REST direto (nao SDK)
│   └── wwwroot/
│       ├── app.css                   # Design system completo
│       ├── logo.png                  # Logo do Master
│       └── js/chat.js                # scrollToBottom, copyToClipboard
├── docker-compose.yml                # PostgreSQL pgvector na porta 5433
├── Dockerfile                        # Multi-stage build
└── docs/superpowers/specs/           # Spec de design aprovado
```

## Comandos CLI

```bash
# Desenvolvimento
docker-compose up -d                                          # Subir PostgreSQL
dotnet run --project src/T20Master.Web                        # Rodar o app

# Indexacao do PDF (RAG)
dotnet run --project src/T20Master.Web -- --index-pdf         # Indexar PDF do T20
dotnet run --project src/T20Master.Web -- --retry-failed      # Reprocessar chunks que falharam
dotnet run --project src/T20Master.Web -- --clear-chunks      # Limpar todos os chunks

# EF Core
dotnet ef migrations add <Nome> --project src/T20Master.Web
dotnet ef database update --project src/T20Master.Web
```

## Decisoes Tecnicas Importantes

### Auth: SSR para Login/Register
Login e Register usam SSR (sem `@rendermode InteractiveServer`) porque `SignInManager` precisa setar cookies via HTTP response, nao via SignalR. Os formularios fazem POST para endpoints em `Program.cs` (`/account/login`, `/account/register`, `/logout`).

### Demais paginas: InteractiveServer
Todas as outras paginas autenticadas usam `@rendermode InteractiveServer` para interatividade (forms, botoes, tabs, chat streaming).

### IA: REST direto, nao SDK
O SDK `Mscc.GenerativeAI` teve problemas de compatibilidade com a API atual. Tanto `EmbeddingService` quanto `GeminiChatService` usam `HttpClient` chamando a API REST do Gemini diretamente.

### Embeddings: 3072 dimensoes, sem indice
O modelo `gemini-embedding-001` gera vetores de 3072 dimensoes. pgvector limita HNSW e IVFFlat a 2000 dimensoes. Usamos busca sequencial (sem indice vetorial) — suficiente para ~1000 chunks de um livro.

### PDF: Deteccao de 2 colunas
O PDF do T20 tem layout em 2 colunas. `PdfProcessor` separa palavras por posicao X (esquerda/direita da metade da pagina) e processa coluna esquerda antes da direita.

### Roles por campanha
Nao ha roles globais. O mestre e definido por `Campaign.MasterId`. Membros via `CampaignPlayer` sao jogadores. Um usuario pode ser mestre em uma campanha e jogador em outra.

### InviteCode
GUID gerado automaticamente. Mestre compartilha o link `/campaign/{id}/join/{inviteCode}`.

## Modelo de Dados

- **ApplicationUser**: IdentityUser + DisplayName
- **Campaign**: Name, Description, MasterId, InviteCode (Guid), Status (Active/Paused/Finished)
- **CampaignPlayer**: chave composta (CampaignId, PlayerId)
- **CharacterSheet**: atributos T20 + colunas JSON (Classes, Skills, Abilities, Spells, Equipment)
- **RuleChunk**: Content, Embedding (vector 3072), SourcePage, Section, Chapter, Theme
- **ChatHistory**: CampaignId, UserId, Messages (JSON array)

## Configuracao (appsettings.Development.json)

- `ConnectionStrings:DefaultConnection` — PostgreSQL (porta 5433)
- `Gemini:ApiKey` — chave do Google AI Studio
- `Gemini:ChatModel` — modelo de chat (ex: gemini-2.5-flash-lite)
- `Gemini:EmbeddingModel` — modelo de embedding (gemini-embedding-001)
- `PdfPath` — caminho absoluto para o PDF do T20

---

## Design System — Frontend

### Direcao Visual
Tema **grimorio/pergaminho envelhecido sobre obsidiana**. A interface deve evocar um livro de RPG aberto numa mesa escura, iluminado por luz de tocha. Nada corporativo — tudo pertence ao mundo do RPG.

### Paleta de Cores

| Token              | Hex       | Uso                                    |
|---------------------|-----------|----------------------------------------|
| Obsidian            | `#0E1111` | Surface base, backgrounds              |
| Dark Umber          | `#4A2E19` | Leather, deep accents                  |
| Copper              | `#855227` | Borders, secondary accents             |
| Deep Amber          | `#E76D25` | Primary actions, active states, CTA    |
| Antique Gold        | `#F5E9CC` | Primary text, headlines                |
| Medium Gold         | `#CF934B` | Secondary text, links                  |
| Pale Gold           | `#FFEC97` | Highlight, emphasis                    |
| Parchment Tan       | `#D18C6A` | Warm accents                           |
| Faint Greenish Stone| `#B4C7B4` | Info states, success-adjacent          |

### Tokens CSS (em app.css)

**Surfaces** (obsidian base, warm shifts sutis):
- `--surface-base: #0E1111`
- `--surface-100: #151816`
- `--surface-200: #1C1F1B`
- `--surface-300: #242720`
- `--surface-400: #2E3228`

**Foreground** (hierarquia em 4 niveis):
- `--ink-primary` — texto principal (Antique Gold)
- `--ink-secondary` — texto de apoio (Medium Gold)
- `--ink-muted` — metadata, labels (50% opacity)
- `--ink-faint` — placeholders, disabled (25% opacity)

**Borders** (copper tints, low opacity):
- `--border-subtle` — separacao leve (12% opacity)
- `--border-default` — bordas padrao (25% opacity)
- `--border-strong` — hover, enfase (45% opacity)

**Actions:**
- Primary: Deep Amber (#E76D25) com texto escuro
- Secondary: outline Medium Gold, bg transparente

### Principios de Design

1. **Depth: borders-only** — sem sombras dramaticas, bordas copper sutis definem regioes
2. **Surfaces: whisper-quiet shifts** — elevacao por mudancas minimas de luminosidade, nao por cores diferentes
3. **Spacing: base 8px** — todos os valores sao multiplos de 4/8px
4. **Radius: 4px/6px/10px** — sharper para inputs, medium para cards, larger para containers
5. **Typography: Segoe UI/system** — hierarquia via peso e tracking, nao so tamanho
6. **Semantic colors desaturados** — success verde-musgo, danger terracota, warning amber

### Arquivos de Estilo

- `wwwroot/app.css` — design system completo com todos os tokens e componentes
- `Components/Layout/MainLayout.razor.css` — layout scoped (sidebar, topbar, page flex)
- `Components/Layout/NavMenu.razor.css` — navegacao scoped (toggler, nav items, active states)

**IMPORTANTE:** Os `.razor.css` sao scoped e tem prioridade sobre `app.css`. Qualquer mudanca de layout/sidebar/topbar deve ser feita nos arquivos scoped, nao no app.css global.
