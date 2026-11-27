# Deadzone2D Online Server

The game now supports online accounts, friends, chat and matchmaking through a Node.js server.

## Run locally

```bash
npm install
npm start
```

The server listens on `http://localhost:3000` and creates `data/server.json` automatically.

## Deploy on Render

1. Push this repository to GitHub.
2. In Render, choose **New → Blueprint** and select this repository.
3. The `render.yaml` file creates a free Node web service (`deadzone2d`) with `npm ci --omit=dev` and `npm start`.
4. Wait for the deploy and copy the `https://...onrender.com` URL.

## Configure the game

Open Deadzone2D → **Settings** → **SERVER URL** → paste the Render URL → **SAVE URL**.

Then on the login screen use **SIGN IN ONLINE** with an email and password. The account is created on the server on first sign-in.

## What works online

- Online accounts with hashed passwords and sessions
- Friend requests and friend list
- Team / All chat with a profanity filter
- Matchmaking: 1v1 needs 2 players, 2v2 needs 4, Royale starts with 2+ after a short wait

Matches are authoritative on the server; player positions and firing are broadcast between clients in the same match.
