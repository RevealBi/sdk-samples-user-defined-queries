# Dynamic Query & RDash Builder

A small web application that lets users build SQL queries visually and generate Reveal dashboard (RDash) files backed by a lightweight ASP.NET server. The project includes a responsive client UI (vanilla HTML/CSS/JS) and a server that exposes REST endpoints to enumerate allowed tables, generate and persist queries, and serve Reveal dashboards.

Watch the project overview video: [YouTube - Dynamic Query & RDash Builder](https://youtu.be/6wWYeSIhFqs)

![YouTube Poster](https://i.ytimg.com/vi/6wWYeSIhFqs/maxresdefault.jpg)

## Repository layout

- `client/` — static frontend assets (HTML, CSS, JS). Key files:
  - `index.html` — single-page UI for query creation, query list, and dashboard viewer.
  - `scripts/main.js` — client logic for interacting with the server API and Reveal SDK.
  - `styles/main.css` — styling for the UI.
  - `create-dashboard.html`, `create-data-grid.html` — helper pages used inside the dashboard iframe viewer.

- `server/aspnet/` — ASP.NET Core server that wires up the Reveal SDK and provides API endpoints.
  - `Program.cs` — app startup, services and endpoints registration.
  - `Reveal/` — Reveal-specific providers (e.g. `DashboardProvider.cs`).
  - `Services/` — core services such as `QueryService.cs`, `DatabaseService.cs`, `DashboardService.cs`.
  - `Queries/`, `Dashboards/` — runtime folders where generated query JSON files and RDash dashboards are saved.

## What it does

- Let users visually select tables and fields to create SQL SELECT queries.
- Save query metadata and SQL on the server as JSON files (under `server/aspnet/Queries`).
- Generate and persist Reveal dashboard (.rdash) files (under `server/aspnet/Dashboards`).
- List existing queries and open them in a Reveal dashboard viewer (client uses Reveal SDK).

## Tech stack

- Frontend: HTML, CSS, vanilla JavaScript, Reveal SDK (in-browser), jQuery (for Reveal SDK integration).
- Backend: .NET 8 (ASP.NET Core), Reveal SDK server-side integration.
- Data sources: PostgreSQL support is registered in `Program.cs` via `builder.DataSources.RegisterPostgreSQL()`; `DatabaseService` abstracts DB metadata reads.

## Quick start (development)

Prerequisites:

- .NET 8 SDK installed
- PostgreSQL (if you plan to connect to a real database) or configure a data source compatible with Reveal SDK settings in `appsettings.json`.

Run the server:

1. Open a terminal and change to the server folder:

   cd server/aspnet

2. Restore and run the app (macOS / zsh):

```bash
dotnet restore
dotnet run
```

By default the client expects the API to be at http://localhost:5111 (see `client/scripts/main.js` constants `API_BASE_URL` and `API_BASE`). If your server runs on a different port, update those constants or set up a reverse proxy.

Serve the client:

The `client` folder contains static files. You can open `client/index.html` directly in the browser for local testing, or serve it from the ASP.NET app (e.g., configure static file middleware) or an HTTP static server such as `live-server` or `http-server`.

## Important server endpoints (used by the client)

- GET /api/allowed-tables
  - Returns the list of allowed tables and metadata (display names, descriptions). The client uses this to populate the tables list in the query builder.

- GET /api/table-schema/{tableName}
  - Returns column schema for the named table. The client requests this when a table is selected to display available fields.

- POST /api/generate-query
  - Payload: { id, friendlyName, description, tableName, fields }
  - Generates a SQL SELECT for the requested fields, fetches column metadata, and writes a JSON query metadata file under `Queries/`.

- GET /api/queries
  - Lists available queries (reads files under `Queries/` and returns metadata used to populate the grid view).

- DELETE /api/queries/{id}
  - Deletes the query JSON file and associated `user_{id}.rdash` dashboard if present.

- POST /api/generate-grid-dashboard/{queryId}
  - Generates a simple grid dashboard RDash for the query and saves it under `Dashboards/user_{queryId}.rdash`.

- GET /dashboards/names
  - Returns list of dashboard filenames and titles for the Reveal viewer dropdown.

Note: the exact route prefixes are registered in `Program.cs` via Map* endpoints and controllers.

## Configuration

- `server/aspnet/appsettings.json` and `appsettings.Development.json` contain server configuration such as Reveal SDK and database settings. Edit them to point to your DB and adjust server options.
- `server/aspnet/Configuration/ServerOptions.cs` defines server options used by the app.

## Security & validation notes

- `QueryService.GenerateAndSaveQueryAsync` sanitizes and validates selected field names and limits the generated SQL length to prevent overly long queries.
- The server registers an `AuthenticationProvider` and `UserContextProvider` for Reveal integration — extend or replace these for real authentication.

## Developer notes

- To add support for a new data source, register it in `Program.cs` using the Reveal SDK data source registration helpers, and implement metadata access in `DatabaseService`.
- Query metadata is stored as JSON with column metadata — this makes it easy to migrate or transform queries later.
