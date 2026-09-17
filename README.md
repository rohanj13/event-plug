# event-plug

Basic full-stack scaffold with:

- .NET Web API backend (`/backend/EventPlug.Api`)
- React frontend with Vite (`/frontend`)
- PostgreSQL database
- Docker Compose to run everything together

## Run with Docker

```bash
docker compose up --build
```

Services:

- Frontend: http://localhost:5173
- Backend health: http://localhost:8080/api/health
- Backend DB check: http://localhost:8080/api/db-check
- PostgreSQL: `localhost:5432` (user: `eventplug`, db: `eventplug`)

## Run locally without Docker

Backend:

```bash
cd backend/EventPlug.Api
dotnet run
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```