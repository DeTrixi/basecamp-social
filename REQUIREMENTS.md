# Basecamp Social — Open Source E2E Encrypted Chat App

## 1. Project Overview

**Basecamp Social** is an open-source, end-to-end encrypted (E2EE) chat application designed for friends and communities (like climbing clubs) to communicate privately on their mobile phones. All messages and stored data are encrypted — the server never has access to plaintext content.

### Core Principles

- **Privacy by default** — E2E encryption for all messages; the server is zero-knowledge.
- **Mobile-first** — Designed primarily for iOS and Android.
- **Open source** — Fully transparent codebase; anyone can audit or self-host.
- **Simple & fast** — Minimal friction; optimised for small friend groups.

---

## 2. Features

### MVP (v1.0)

| Feature | Description |
|---|---|
| **User registration & login** | Phone number or email-based signup with secure authentication |
| **1-on-1 messaging** | Real-time E2E encrypted direct messages |
| **Group chat** | Create groups of up to 1,000 members with E2E encryption |
| **Message types** | Text, emoji, and image attachments (encrypted) |
| **Read receipts** | Optional delivery & read indicators |
| **Online/typing status** | Real-time presence indicators |
| **Push notifications** | Encrypted push notifications (no message preview on server) |
| **Message history** | Encrypted local storage; optional encrypted cloud backup |
| **Contact management** | Add friends by username, QR code, or invite link |
| **Profile** | Display name, avatar, and status message |
| **Date polls** | Propose multiple dates/times for an event; group members vote on which ones work for them (Doodle-style). See results at a glance to pick the best date. |

### Future (v2.0+)

- Voice & video calls (E2EE via WebRTC + SRTP)
- Disappearing messages (auto-delete timer)
- File sharing (documents, videos)
- Reactions & replies
- Multi-device sync with E2EE key sharing
- Self-hosted server option with Docker Compose

---

## 3. Technology Stack

### 3.1 Mobile Frontend

| Choice | Technology |
|---|---|
| **Framework** | **React Native (Expo)** |
| **Language** | TypeScript |
| **Navigation** | React Navigation v7 |
| **State management** | Zustand |
| **Local database** | expo-sqlite (SQLite) — stores decrypted messages on-device only |
| **UI components** | React Native Paper (Material Design 3) |
| **Real-time** | `@microsoft/signalr` (SignalR JS client) |
| **Crypto** | `react-native-quick-crypto` (native OpenSSL bindings) |

**Why React Native (Expo)?**
- Single codebase for iOS & Android.
- Expo SDK simplifies builds, OTA updates, and push notifications.
- Mature ecosystem with strong community support.
- Excellent crypto library support through native modules.

### 3.2 Backend API

| Choice | Technology |
|---|---|
| **Runtime** | **.NET 10** |
| **Framework** | **ASP.NET Core Minimal APIs** |
| **Language** | C# 14 |
| **Real-time** | SignalR (WebSocket with fallback) |
| **Authentication** | JWT (access + refresh tokens) via ASP.NET Core Identity |
| **Validation** | FluentValidation |
| **API style** | REST (auth, users, groups, polls) + SignalR hubs (messaging, presence) |
| **File uploads** | Pre-signed URLs → S3-compatible storage (AWSSDK.S3) |
| **Logging** | Serilog (structured JSON) |
| **API docs** | Swagger / OpenAPI (Swashbuckle) |

**Why ASP.NET Core?**
- Excellent performance — consistently top-tier in TechEmpower benchmarks.
- Built-in SignalR for real-time WebSocket communication (no extra library needed).
- First-class dependency injection, middleware pipeline, and configuration.
- Strong typing with C# catches bugs at compile time.
- Native Docker support with optimised multi-stage builds.
- Mature ecosystem for auth, validation, and ORM.

### 3.3 Database

| Choice | Technology |
|---|---|
| **Primary database** | **PostgreSQL 18** (containerised) |
| **ORM** | Entity Framework Core 10 |
| **Migrations** | EF Core Migrations (CLI) |
| **Cache / Pub-Sub** | **Redis 7** (containerised — presence, typing indicators, SignalR backplane) |

**Why PostgreSQL?**
- Rock-solid reliability for user accounts, group metadata, and encrypted message blobs.
- JSONB support for flexible encrypted payload storage.
- Excellent indexing for message retrieval by conversation + timestamp.
- Open source and self-hostable — aligns with project values.

**Database stores only encrypted data.** The server never sees plaintext messages.

### 3.4 File / Media Storage

| Choice | Technology |
|---|---|
| **Storage** | **S3-compatible** (AWS S3, MinIO for self-hosting, or Cloudflare R2) |
| **Process** | Client encrypts file → uploads via pre-signed URL → shares encrypted key in message |

### 3.5 Push Notifications

| Choice | Technology |
|---|---|
| **Service** | **Expo Push Notifications** (wraps APNs + FCM) |
| **Privacy** | Notification payload contains only a signal; client fetches & decrypts the message locally |

### 3.6 DevOps & Infrastructure

| Choice | Technology |
|---|---|
| **Containerisation** | Docker + Docker Compose — **everything except the mobile app is containerised** |
| **CI/CD** | GitHub Actions |
| **Hosting (recommended)** | Any VPS, Railway, Fly.io, or cloud VM |
| **Monitoring** | Serilog + Seq (containerised) or Grafana/Loki |
| **Mobile builds** | EAS Build (Expo Application Services) |

#### Containerised Services

| Container | Image | Purpose |
|---|---|---|
| **api** | Custom .NET 10 image (multi-stage build) | REST API + SignalR hubs |
| **postgres** | `postgres:18.1-alpine` | Primary database |
| **redis** | `redis:7-alpine` | Cache, pub/sub, SignalR backplane |
| **minio** | `minio/minio` | S3-compatible file storage |
| **seq** | `datalust/seq` | Structured log viewer (optional) |

The **mobile app** is built separately using Expo/EAS and is NOT containerised (it runs natively on user devices).

---

## 4. End-to-End Encryption Design

### 4.1 Protocol

Based on the **Signal Protocol** (Double Ratchet + X3DH), simplified for the MVP:

```
Key Exchange:       X3DH (Extended Triple Diffie-Hellman)
Ratchet:            Double Ratchet Algorithm
Symmetric cipher:   AES-256-GCM
Key agreement:      X25519 (Curve25519 ECDH)
Signing:            Ed25519
KDF:                HKDF-SHA-256
```

### 4.2 Key Management

| Key | Purpose | Storage |
|---|---|---|
| **Identity Key Pair** | Long-term identity (Ed25519) | Device keychain (iOS Keychain / Android Keystore) |
| **Signed Pre-Key** | Medium-term key exchange key | Device keychain, public part on server |
| **One-Time Pre-Keys** | Prevent replay attacks; consumed on first message | Public parts on server, private on device |
| **Session Keys** | Per-conversation ratcheting keys | Device local encrypted DB |

### 4.3 Message Flow

```
┌──────────┐                    ┌──────────┐                    ┌──────────┐
│  Alice   │                    │  Server  │                    │   Bob    │
│ (sender) │                    │ (relay)  │                    │(receiver)│
└────┬─────┘                    └────┬─────┘                    └────┬─────┘
     │                               │                               │
     │  1. Fetch Bob's pre-key       │                               │
     │   bundle (public keys)        │                               │
     │──────────────────────────────>│                               │
     │<──────────────────────────────│                               │
     │                               │                               │
     │  2. X3DH key agreement        │                               │
     │   (derive shared secret)      │                               │
     │                               │                               │
     │  3. Encrypt message with      │                               │
     │   AES-256-GCM using           │                               │
     │   session key                 │                               │
     │                               │                               │
     │  4. Send encrypted blob       │                               │
     │──────────────────────────────>│                               │
     │                               │  5. Relay encrypted blob      │
     │                               │──────────────────────────────>│
     │                               │                               │
     │                               │  6. Bob performs X3DH,        │
     │                               │   derives same shared secret, │
     │                               │   decrypts message            │
     │                               │                               │
```

### 4.4 Group Encryption

- **Sender Keys** protocol (similar to Signal's group messaging).
- Each member generates a sender key and distributes it (encrypted) to all other members.
- Messages are encrypted once with the sender key and delivered to all members.
- When a member leaves, all sender keys are rotated.

### 4.5 Data at Rest

| Location | Encryption |
|---|---|
| **Server database** | Messages stored as opaque encrypted blobs (ciphertext + nonce + header) |
| **Server files (S3)** | Files encrypted client-side before upload |
| **Client local DB** | SQLite encrypted with SQLCipher or OS-level encryption |
| **Client keychain** | Identity keys stored in hardware-backed keychain |

---

## 5. API Design

### 5.1 REST Endpoints

```
POST   /api/v1/auth/register          Register new account
POST   /api/v1/auth/login             Login (returns JWT pair)
POST   /api/v1/auth/refresh           Refresh access token

GET    /api/v1/users/me                Get current user profile
PATCH  /api/v1/users/me                Update profile
GET    /api/v1/users/:id               Get user public profile
GET    /api/v1/users/search?q=         Search users by username

POST   /api/v1/keys/bundle             Upload pre-key bundle
GET    /api/v1/keys/:userId/bundle     Fetch user's pre-key bundle

POST   /api/v1/conversations           Create 1-on-1 or group conversation
GET    /api/v1/conversations           List user's conversations
GET    /api/v1/conversations/:id       Get conversation details + members
PATCH  /api/v1/conversations/:id       Update group name/avatar
POST   /api/v1/conversations/:id/members   Add members
DELETE /api/v1/conversations/:id/members/:userId   Remove member

GET    /api/v1/messages/:conversationId?before=&limit=   Fetch encrypted messages (paginated)
POST   /api/v1/messages/:conversationId/read             Mark messages as read

POST   /api/v1/upload/presign          Get pre-signed upload URL

POST   /api/v1/polls                              Create a date poll in a conversation
GET    /api/v1/polls/:pollId                       Get poll details & current votes
POST   /api/v1/polls/:pollId/vote                  Submit or update your vote
DELETE /api/v1/polls/:pollId                       Close/delete a poll (creator or admin)
PATCH  /api/v1/polls/:pollId/finalize              Lock poll and set the chosen date
```

### 5.2 SignalR Hubs

**ChatHub** (`/hubs/chat`)
```
── Client → Server (invoke) ──
SendMessage            { conversationId, encryptedPayload, clientMessageId }
StartTyping            { conversationId }
StopTyping             { conversationId }
SetOnline              { }

── Server → Client (on) ──
ReceiveMessage         { conversationId, encryptedPayload, senderId, timestamp, serverMessageId }
MessageDelivered       { conversationId, messageId }
MessageRead            { conversationId, messageId, readBy }
TypingUpdate           { conversationId, userId, isTyping }
PresenceChanged        { userId, status, lastSeen }
PollCreated            { conversationId, pollId, createdBy }
PollVoted              { conversationId, pollId, userId }
PollFinalized          { conversationId, pollId, chosenOption }
```

---

## 6. Database Schema (High-Level)

```sql
-- Users (minimal PII)
users
  id              UUID PRIMARY KEY
  username        VARCHAR(30) UNIQUE
  email           VARCHAR(255) UNIQUE (hashed for lookup)
  password_hash   VARCHAR(255)
  display_name    VARCHAR(50)
  avatar_url      VARCHAR(500)
  created_at      TIMESTAMPTZ
  last_seen_at    TIMESTAMPTZ

-- Pre-key bundles for E2EE key exchange
key_bundles
  id                  UUID PRIMARY KEY
  user_id             UUID REFERENCES users
  identity_key        BYTEA              -- public identity key
  signed_pre_key      BYTEA              -- public signed pre-key
  signed_pre_key_sig  BYTEA              -- signature
  one_time_pre_keys   BYTEA[]            -- array of public one-time keys

-- Conversations (1-on-1 and group)
conversations
  id              UUID PRIMARY KEY
  type            VARCHAR(10)            -- 'direct' | 'group'
  name            VARCHAR(100)           -- null for direct
  avatar_url      VARCHAR(500)
  created_by      UUID REFERENCES users
  created_at      TIMESTAMPTZ

-- Conversation membership
conversation_members
  conversation_id UUID REFERENCES conversations
  user_id         UUID REFERENCES users
  role            VARCHAR(10)            -- 'admin' | 'member'
  joined_at       TIMESTAMPTZ
  PRIMARY KEY (conversation_id, user_id)

-- Encrypted messages (server sees only ciphertext)
messages
  id                  UUID PRIMARY KEY
  conversation_id     UUID REFERENCES conversations
  sender_id           UUID REFERENCES users
  encrypted_payload   BYTEA              -- ciphertext (message + metadata)
  message_type        VARCHAR(10)        -- 'text' | 'image' | 'file'
  created_at          TIMESTAMPTZ
  INDEX (conversation_id, created_at DESC)

-- Delivery & read receipts
message_receipts
  message_id      UUID REFERENCES messages
  user_id         UUID REFERENCES users
  delivered_at    TIMESTAMPTZ
  read_at         TIMESTAMPTZ
  PRIMARY KEY (message_id, user_id)

-- Date polls (Doodle-style scheduling)
polls
  id                  UUID PRIMARY KEY
  conversation_id     UUID REFERENCES conversations
  created_by          UUID REFERENCES users
  title               VARCHAR(200)       -- e.g. "Weekend climbing trip"
  description         TEXT               -- optional details
  status              VARCHAR(10)        -- 'open' | 'finalized' | 'closed'
  chosen_option_id    UUID               -- set when finalized
  created_at          TIMESTAMPTZ
  closes_at           TIMESTAMPTZ        -- optional auto-close deadline

-- Poll date/time options
poll_options
  id              UUID PRIMARY KEY
  poll_id         UUID REFERENCES polls
  starts_at       TIMESTAMPTZ            -- proposed date/time
  ends_at         TIMESTAMPTZ            -- optional end time
  label           VARCHAR(100)           -- optional label, e.g. "Saturday morning"
  sort_order      INT

-- Member votes on poll options
poll_votes
  poll_option_id  UUID REFERENCES poll_options
  user_id         UUID REFERENCES users
  response        VARCHAR(10)            -- 'yes' | 'maybe' | 'no'
  voted_at        TIMESTAMPTZ
  PRIMARY KEY (poll_option_id, user_id)
```

---

## 7. Project Structure

```
chat-app/
├── apps/
│   ├── mobile/                    # React Native (Expo) app
│   │   ├── app/                   # Expo Router screens
│   │   ├── components/            # Reusable UI components
│   │   ├── lib/
│   │   │   ├── crypto/            # E2EE encryption/decryption
│   │   │   ├── api/               # REST API client
│   │   │   ├── signalr/           # SignalR hub client
│   │   │   └── storage/           # Local SQLite + Keychain
│   │   ├── stores/                # Zustand state stores
│   │   ├── app.json
│   │   └── package.json
│   │
│   └── server/                    # ASP.NET Core API (C#)
│       ├── src/
│       │   └── BasecampSocial.Api/
│       │       ├── Controllers/   # Minimal API endpoint groups
│       │       ├── Hubs/          # SignalR hubs (messaging, presence)
│       │       ├── Data/          # EF Core DbContext, entities, migrations
│       │       ├── Services/      # Business logic
│       │       ├── Middleware/    # Auth, rate-limiting, error handling
│       │       ├── Models/        # DTOs and request/response models
│       │       ├── Validators/    # FluentValidation validators
│       │       ├── Program.cs     # Entry point & DI configuration
│       │       ├── appsettings.json
│       │       └── BasecampSocial.Api.csproj
│       ├── tests/
│       │   └── BasecampSocial.Api.Tests/
│       │       └── BasecampSocial.Api.Tests.csproj
│       ├── BasecampSocial.sln
│       └── Dockerfile             # Multi-stage .NET 10 build
│
├── docker-compose.yml             # API + PostgreSQL + Redis + MinIO + Seq
├── docker-compose.override.yml    # Dev overrides (ports, volumes, hot-reload)
├── .github/workflows/             # CI/CD pipelines
├── REQUIREMENTS.md                # ← This file
├── LICENSE                        # MIT or AGPLv3
└── README.md
```

---

## 8. Security Requirements

| Requirement | Implementation |
|---|---|
| E2E encryption | Signal Protocol (X3DH + Double Ratchet) |
| Password storage | Argon2id hashing |
| Auth tokens | Short-lived JWT access tokens (15 min) + long-lived refresh tokens (30 days) |
| Transport security | TLS 1.3 everywhere |
| Rate limiting | Per-IP and per-user rate limits on all endpoints |
| Input validation | FluentValidation on all API inputs |
| Key storage | OS keychain (iOS Keychain / Android Keystore) |
| Forward secrecy | Double Ratchet ensures past messages can't be decrypted if keys are compromised |
| Metadata minimisation | Server logs no message content; minimal metadata retention |

---

## 9. Non-Functional Requirements

| Requirement | Target |
|---|---|
| Message delivery latency | < 300ms (same region) |
| App cold start | < 2 seconds |
| Offline support | Queue messages locally; send when reconnected |
| Max group size | 1,000 members (MVP) |
| Max message size | 64 KB (text), 25 MB (media) |
| Concurrent connections | 10,000+ per server instance |
| Uptime | 99.9% (self-hosted SLA depends on infra) |

---

## 10. Development Milestones

| Phase | Scope | Duration |
|---|---|---|
| **Phase 1** | Project setup, auth, user profiles, database schema | 2 weeks |
| **Phase 2** | E2EE key exchange, 1-on-1 messaging, WebSocket | 3 weeks |
| **Phase 3** | Group chat, media sharing, encrypted file uploads | 2 weeks |
| **Phase 4** | Push notifications, read receipts, presence | 2 weeks |
| **Phase 5** | Polish, testing, security audit, app store prep | 2 weeks |

---

## 11. License

**AGPLv3** recommended — ensures all modifications to the server remain open source (stronger copyleft than MIT for a privacy-focused app). Client code can optionally be MIT.

---

## 12. Getting Started (Development)

```bash
# Prerequisites: .NET 10 SDK, Docker, Node.js 22+, Expo CLI

# 1. Clone the repo
git clone https://github.com/your-org/chat-app.git
cd chat-app

# 2. Start ALL backend services (API + Postgres + Redis + MinIO + Seq)
docker compose up -d

# The API runs at http://localhost:5000
# Seq log viewer at http://localhost:5341
# MinIO console at http://localhost:9001

# 3. (Optional) Run the API outside Docker for debugging
cd apps/server/src/BasecampSocial.Api
dotnet run

# 4. Run database migrations
cd apps/server/src/BasecampSocial.Api
dotnet ef database update

# 5. Start the mobile app
cd apps/mobile && npm install && npx expo start
```
