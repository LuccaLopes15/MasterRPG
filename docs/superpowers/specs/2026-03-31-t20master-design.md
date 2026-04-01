# T20Master — Plataforma de Gerenciamento de Campanhas T20

**Data:** 2026-03-31
**Status:** Aprovado

## Visao Geral

Plataforma web para gerenciamento de campanhas de RPG Tormenta 20 (T20), com sistema de contas, fichas de personagens e IA consultora de regras baseada no livro basico.

### Objetivos
- Mestres criam campanhas e convidam jogadores via codigo de convite (GUID)
- Jogadores acessam e editam apenas suas proprias fichas
- Mestres visualizam todas as fichas da campanha
- Mestre tem acesso a uma IA consultora de regras T20, que usa RAG sobre o PDF do livro basico e tem contexto das fichas dos jogadores

### Restricoes
- Custo minimo: Gemini API free tier, Railway Hobby plan
- Stack: .NET 8 + Blazor Server + PostgreSQL
- Deploy: Docker na Railway

---

## Arquitetura

### Diagrama

```
┌─────────────────────────────────────────────┐
│              Railway (Docker)                │
│                                             │
│  ┌───────────────────────────────────────┐  │
│  │     .NET 8 — Blazor Server App        │  │
│  │                                       │  │
│  │  ┌─────────┐  ┌──────────┐  ┌──────┐ │  │
│  │  │  Auth /  │  │ Campaign │  │  AI  │ │  │
│  │  │ Identity │  │ Manager  │  │ RAG  │ │  │
│  │  └─────────┘  └──────────┘  └──┬───┘ │  │
│  │        │            │          │      │  │
│  │        └────────┬───┘          │      │  │
│  │                 │              │      │  │
│  │          ┌──────┴──────┐       │      │  │
│  │          │  EF Core    │       │      │  │
│  │          └──────┬──────┘       │      │  │
│  └─────────────────┼─────────────┼──────┘  │
│                    │             │          │
│              ┌─────┴─────┐   ┌──┴───────┐  │
│              │ PostgreSQL │   │  Gemini  │  │
│              │ + pgvector │   │   API    │  │
│              └───────────┘   └──────────┘  │
└─────────────────────────────────────────────┘
```

### Componentes

- **Auth/Identity**: ASP.NET Core Identity com PostgreSQL. Registro, login, roles (Mestre/Jogador por campanha).
- **Campaign Manager**: CRUD de campanhas, convites via GUID, associacao jogador-campanha.
- **Character Sheets**: Fichas de personagem T20 com atributos, classes, racas, habilidades, magias, equipamentos.
- **AI/RAG Module**: Processa PDF do T20, cria embeddings, busca trechos relevantes, envia ao Gemini com contexto das fichas.
- **PostgreSQL + pgvector**: Banco relacional para dados da aplicacao + extensao pgvector para busca vetorial dos embeddings.

### Stack Tecnica

| Componente | Tecnologia |
|-----------|-----------|
| Runtime | .NET 8 |
| Frontend | Blazor Server (SSR interativo) |
| Auth | ASP.NET Core Identity |
| ORM | Entity Framework Core |
| Banco | PostgreSQL + pgvector |
| PDF | UglyToad.PdfPig |
| IA | Google Gemini 2.0 Flash (API gratuita) |
| Embeddings | Gemini text-embedding-004 |
| Streaming | SignalR (nativo do Blazor Server) |
| Deploy | Docker na Railway |

### Libs NuGet

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Pgvector.EntityFrameworkCore`
- `UglyToad.PdfPig`
- `Mscc.GenerativeAI` (SDK Gemini para .NET)

---

## Modelo de Dados

### User (ASP.NET Identity)
- Id, Email, UserName, PasswordHash (gerenciados pelo Identity)
- DisplayName (nome de exibicao no sistema)

### Campaign
| Campo | Tipo | Descricao |
|-------|------|-----------|
| Id | Guid | PK |
| Name | string | Nome da campanha |
| Description | string? | Descricao opcional |
| CreatedAt | DateTime | Data de criacao |
| MasterId | string (FK → User) | Criador/mestre da campanha |
| InviteCode | Guid | Codigo unico gerado automaticamente para convites |
| Status | enum | Active, Paused, Finished |

### CampaignPlayer
| Campo | Tipo | Descricao |
|-------|------|-----------|
| CampaignId | Guid (FK) | Campanha |
| PlayerId | string (FK → User) | Jogador |
| JoinedAt | DateTime | Data de entrada |

### CharacterSheet
| Campo | Tipo | Descricao |
|-------|------|-----------|
| Id | Guid | PK |
| PlayerId | string (FK → User) | Dono da ficha |
| CampaignId | Guid (FK) | Campanha |
| Name | string | Nome do personagem |
| Race | string | Raca (Humano, Elfo, Anao, etc.) |
| Level | int | Nivel |
| XP | int | Experiencia |
| Strength | int | Forca |
| Dexterity | int | Destreza |
| Constitution | int | Constituicao |
| Intelligence | int | Inteligencia |
| Wisdom | int | Sabedoria |
| Charisma | int | Carisma |
| MaxHP | int | Pontos de vida maximos |
| CurrentHP | int | Pontos de vida atuais |
| MaxMP | int | Pontos de mana maximos |
| CurrentMP | int | Pontos de mana atuais |
| Defense | int | Defesa |
| Speed | decimal | Deslocamento |
| Classes | JSON | Array de {ClassName, Level} (suporta multiclasse) |
| Skills | JSON | Pericias treinadas |
| Abilities | JSON | Habilidades de classe/raca |
| Spells | JSON | Magias conhecidas |
| Equipment | JSON | Equipamentos |
| Notes | string? | Anotacoes livres do jogador |

### RuleChunk (RAG)
| Campo | Tipo | Descricao |
|-------|------|-----------|
| Id | Guid | PK |
| Content | string | Texto do trecho |
| Embedding | Vector(768) | Embedding via Gemini |
| SourcePage | int | Pagina no PDF |
| Section | string | Secao do livro |
| Chapter | string | Capitulo |
| Theme | string | Tema (combate, magia, racas, classes, etc.) |

### ChatHistory
| Campo | Tipo | Descricao |
|-------|------|-----------|
| Id | Guid | PK |
| CampaignId | Guid (FK) | Campanha |
| UserId | string (FK → User) | Quem perguntou |
| CreatedAt | DateTime | Data da conversa |
| Messages | JSON | Array de {Role, Content, Timestamp} |

### Permissoes

Roles sao **por campanha**, nao globais. Um usuario pode ser mestre em uma campanha e jogador em outra. O mestre e definido pelo campo `MasterId` na Campaign; todos os outros membros (via CampaignPlayer) sao jogadores.

- **Mestre da campanha**: ve/edita todas as fichas, acessa IA, gerencia campanha
- **Jogador na campanha**: ve/edita apenas sua propria ficha
- **IA**: recebe contexto das fichas da campanha ao responder

---

## Modulo de IA / RAG

### Pipeline de Indexacao (executado uma vez)

1. **Extracao**: PDF do T20 → texto extraido por pagina usando `UglyToad.PdfPig`
2. **Chunking**: Texto dividido em chunks de ~500-800 tokens, respeitando limites de secao/paragrafo
3. **Classificacao**: Cada chunk recebe metadata (capitulo, tema)
4. **Embeddings**: Cada chunk → embedding via Gemini Embedding API (`text-embedding-004`, gratuito)
5. **Armazenamento**: Chunks + embeddings salvos na tabela `RuleChunk` com pgvector

### Pipeline de Consulta (a cada pergunta)

1. Pergunta do mestre → embedding via Gemini
2. Busca vetorial: top 5-10 chunks mais similares via pgvector (cosine distance)
3. Contexto de fichas: se relevante, injeta dados das fichas dos jogadores da campanha
4. Prompt montado:
   ```
   Voce e um assistente especialista em Tormenta 20.
   Use APENAS as regras abaixo para responder.
   Cite a pagina/secao quando possivel.
   
   [Trechos relevantes do livro]
   
   [Fichas dos jogadores da campanha (se relevante)]
   
   Pergunta do mestre: {pergunta}
   ```
5. Gemini gera resposta → streamed via SignalR para o chat em Blazor
6. Resposta salva no ChatHistory

### Especificacoes da API Gemini
- **Modelo de chat**: Gemini 2.0 Flash (gratuito, 15 req/min, 1M tokens de contexto)
- **Modelo de embeddings**: text-embedding-004 (gratuito, 768 dimensoes)
- **Streaming**: Suportado nativamente, integrado via SignalR

---

## Paginas e Fluxos

### Mapa de Paginas

| Rota | Acesso | Descricao |
|------|--------|-----------|
| `/` | Publico | Landing page + login/registro |
| `/dashboard` | Autenticado | Lista campanhas (como mestre ou jogador) |
| `/campaign/create` | Autenticado | Criar nova campanha |
| `/campaign/{id}` | Membro | Visao geral: jogadores, fichas, status |
| `/campaign/{id}/join/{inviteCode}` | Autenticado | Entrar na campanha via convite |
| `/campaign/{id}/characters` | Mestre | Lista todas as fichas |
| `/campaign/{id}/character/{charId}` | Dono ou Mestre | Visualizar/editar ficha |
| `/campaign/{id}/character/create` | Jogador | Criar ficha na campanha |
| `/campaign/{id}/ai` | Mestre | Chat com IA consultora de regras |

### Fluxos Principais

**Mestre cria campanha:**
Dashboard → Criar Campanha → Recebe InviteCode (GUID) → Compartilha com jogadores

**Jogador entra na campanha:**
Recebe link → Login/Registro → Entra na campanha → Cria ficha

**Mestre consulta IA:**
Pagina da campanha → Chat IA → Faz pergunta → IA busca regras + contexto fichas → Responde em tempo real

**Jogador edita ficha:**
Dashboard → Campanha → Minha Ficha → Edita campos → Salva

---

## Estrutura do Projeto

```
T20Master/
├── T20Master.sln
├── src/
│   └── T20Master.Web/
│       ├── Program.cs
│       ├── Components/
│       │   ├── Layout/                  # MainLayout, NavMenu
│       │   ├── Pages/
│       │   │   ├── Home.razor
│       │   │   ├── Auth/               # Login, Register
│       │   │   ├── Dashboard/          # Dashboard.razor
│       │   │   ├── Campaign/           # Create, View, Join
│       │   │   ├── Character/          # Create, Edit, View
│       │   │   └── AI/                 # Chat.razor
│       │   └── Shared/                 # Componentes reutilizaveis
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Entities/               # User, Campaign, CharacterSheet, etc.
│       │   └── Migrations/
│       ├── Services/
│       │   ├── CampaignService.cs
│       │   ├── CharacterService.cs
│       │   └── AI/
│       │       ├── PdfProcessor.cs     # Extracao e chunking do PDF
│       │       ├── EmbeddingService.cs  # Gera embeddings via Gemini
│       │       ├── RagService.cs        # Busca vetorial + montagem de prompt
│       │       └── GeminiChatService.cs # Comunicacao com Gemini API
│       └── wwwroot/
├── Dockerfile
├── docker-compose.yml                  # Dev local com PostgreSQL
└── docs/
```

---

## Deploy

### Railway
- **Container**: Dockerfile multi-stage (SDK build → ASP.NET runtime)
- **PostgreSQL**: Addon nativo Railway, pgvector habilitado via extensao
- **Variaveis de ambiente**:
  - `GEMINI_API_KEY` — chave da API Gemini
  - `ConnectionStrings__DefaultConnection` — connection string PostgreSQL (injetada pela Railway)
  - `ASPNETCORE_URLS` — Railway injeta a porta via `PORT`

### Docker Compose (dev local)
- App .NET + PostgreSQL com pgvector
- Volume para persistencia do banco
- Hot reload habilitado para desenvolvimento

---

## Decisoes Tecnicas

1. **Blazor Server sobre WASM**: Poucos usuarios simultaneos (mesas de RPG), SignalR nativo para streaming da IA, projeto unico mais simples.
2. **pgvector sobre banco vetorial dedicado**: Evita servico extra, PostgreSQL ja e necessario para dados da app, pgvector e suficiente para ~10-20k chunks de um livro.
3. **JSON columns para dados flexiveis**: Magias, habilidades e equipamentos variam muito entre personagens. JSON no PostgreSQL permite flexibilidade sem explodir o schema.
4. **InviteCode como GUID**: Simples, unico, sem necessidade de logica extra de geracao.
5. **Gemini free tier**: 15 req/min e 1M tokens/dia e mais que suficiente para consultas de RPG durante sessoes.
