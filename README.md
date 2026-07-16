# AI Support Agent

RAG-powered customer support chatbot. Ask questions in natural language — the AI searches the knowledge base using **semantic vector search** and streams a grounded answer in real time.

## Stack

| Layer | Tech |
|---|---|
| Frontend | Next.js 15 (App Router, streaming SSE) |
| Backend | .NET 9 ASP.NET Core |
| AI | OpenAI `gpt-4o-mini` + `text-embedding-3-small` |
| Database | MongoDB Atlas (Vector Search) |
| Container | Docker + docker-compose |

## Features

- **RAG pipeline** — embeds every question, retrieves top-3 articles via vector similarity, injects context into the system prompt
- **Streaming responses** — AI answer streams token-by-token (Server-Sent Events)
- **Source citations** — UI shows which knowledge articles backed each answer
- **Admin panel** — `/admin` to create, edit, delete articles and trigger embedding generation
- **Multi-turn** — conversation history sent with each request for context continuity

## Local Development

### Prerequisites
- .NET 9 SDK
- Node.js 22
- MongoDB Atlas account (free M0 cluster)
- OpenAI API key

### 1. Backend

```bash
cd backend/SupportAgent.API

# Copy the example config and fill in your values
cp appsettings.example.json appsettings.json
# → set MongoDB:ConnectionString and OpenAI:ApiKey

dotnet run
# → http://localhost:5000/swagger
```

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
# → http://localhost:3000
```

### 3. Generate Embeddings

After first startup, seed articles are inserted but have no embeddings yet.

1. Open `http://localhost:3000/admin`
2. Click **"Embed All"** — this calls OpenAI to embed all 8 seed articles

Or via Swagger: `POST /api/knowledge/embed-all`

### 4. Configure MongoDB Atlas Vector Search Index

Vector search requires a search index on the `knowledge_articles` collection.

1. Go to **MongoDB Atlas** → your cluster → **Search** tab
2. Click **Create Search Index** → **JSON Editor**
3. Select database `support_agent`, collection `knowledge_articles`
4. Paste this index definition:

```json
{
  "fields": [
    {
      "type": "vector",
      "path": "embedding",
      "numDimensions": 1536,
      "similarity": "cosine"
    }
  ]
}
```

5. Name it **`vector_index`** and click **Create**

> **Note:** Until the index is created, the app falls back to returning the most recently updated embedded articles. The chatbot still works — just without true semantic search.

## Docker

```bash
cp .env.example .env
# fill in MONGODB_CONNECTION_STRING and OPENAI_API_KEY

docker-compose up --build
```

## API Reference

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/chat/ask` | SSE stream — ask a question |
| `GET` | `/api/chat/sessions/{id}` | Get chat history |
| `GET` | `/api/knowledge` | List all articles |
| `POST` | `/api/knowledge` | Create article |
| `PUT` | `/api/knowledge/{id}` | Update article |
| `DELETE` | `/api/knowledge/{id}` | Delete article |
| `POST` | `/api/knowledge/{id}/embed` | Embed single article |
| `POST` | `/api/knowledge/embed-all` | Embed all articles |
| `GET` | `/health` | Health check |
