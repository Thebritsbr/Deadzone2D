# Deadzone2D Online Server

Deadzone2D connects automatically to the public server configured in `Config/Online.json`
(https://deadzone2d.onrender.com). Players do **not** need to configure anything.

## Sign in

On the login screen choose **SIGN IN ONLINE** and enter any email and password.
If the account does not exist yet, the game creates it automatically on the server.
The session is saved locally, so the game reconnects automatically on the next launch.

## What works online

- Online accounts with hashed passwords and sessions
- Friend requests and friend list
- Team / All chat with a profanity filter
- Matchmaking: 1v1 needs 2 players, 2v2 needs 4, Royale starts with 2+ after a short wait
- Synchronized player positions, firing and damage between computers

## Run the server locally (development only)

```bash
npm install
npm start
```

The server listens on `http://localhost:3000` and creates `data/server.json` automatically.
For local testing, change `Config/Online.json` to `http://localhost:3000`.

## Deploy on Render

The included `render.yaml` creates a free Node web service named `deadzone2d`.
In Render: **New → Blueprint** → select the GitHub repository → **Apply**.
The service uses `npm ci --omit=dev`, `npm start` and `/health` as the health check.
