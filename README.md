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

## Project Structure

```
/client   React + Vite frontend
/server   ASP.NET Core 8 minimal API
/infra    Terraform (AWS infrastructure)
```
