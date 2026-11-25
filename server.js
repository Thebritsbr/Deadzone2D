services:
  - type: web
    name: crossfire
    runtime: node
    plan: free
    numInstances: 1
    buildCommand: npm ci --omit=dev
    startCommand: npm start
    healthCheckPath: /health
    envVars:
      - key: NODE_ENV
        value: production
      - key: DATABASE_URL
        sync: false
      - key: SEASON_ID
        value: "01"
      - key: SEASON_NAME
        value: Opening
