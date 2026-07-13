# Trivia v2

A real-time multiplayer trivia game built with React, ASP.NET Core 8, and PostgreSQL.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) (v18+)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Local Development

### 1. Start the database

```bash
docker compose up -d
```

### 2. Start the API

```bash
cd server
dotnet run
```

### 3. Start the frontend

```bash
cd client
npm install
npm run dev
```

The React app will be available at `http://localhost:5173`.

## Useful Commands

### Run tests

```bash
cd tests && dotnet build --verbosity minimal && dotnet test --no-build --verbosity minimal
```

### Check TypeScript errors

```bash
cd client && npx tsc --noEmit
```

### Check ESLint errors

```bash
cd client && npx eslint src --ext .ts,.tsx
```

### Clear the database

wipe all data and start fresh (e.g. to clear out test games):

```bash
docker compose down -v
docker compose up -d
cd server && dotnet ef database update
```

### Fix port 5432 conflict (system Postgres running)

```bash
sudo pkill -u postgres && docker compose up -d
```

## Project Structure

```
/client   React + Vite frontend
/server   ASP.NET Core 8 minimal API
/tests    xUnit integration tests
```
