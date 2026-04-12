# Residential Area Management System

A polyglot **microservices-based Residential Area Management System** designed for residential communities, apartment complexes, and gated societies.

This project design uses **domain-aligned services**, **per-service database ownership**, **OAuth2/OpenID Connect authentication**, and **event-driven communication** for scalable, secure, and maintainable system development.

## Overview

The system is designed around these principles:

- **Microservices architecture** with clear bounded contexts
- **Database per service** ownership
- **OAuth2 + OpenID Connect** with JWT-based authorization
- **Event-driven workflows** for billing, bookings, and payments
- **Polyglot persistence** using PostgreSQL, SQL Server, MongoDB, and SQLite where appropriate

According to the design document, the recommended split is:
- **SQL Server** for Auth and Payments
- **PostgreSQL** for core relational domains
- **MongoDB** for flexible document-heavy domains
- **SQLite** only for offline or edge scenarios fileciteturn1file0

## Architecture Style

- Use an **API Gateway** in front of user-facing services
- Do not allow one microservice to write directly into another service’s database
- Use **APIs** for synchronous communication
- Use an **event broker** such as RabbitMQ or Kafka for asynchronous workflows
- Store cross-service references as IDs instead of cross-database foreign keys fileciteturn1file0

## Service Boundaries

### 1. AuthService
Owns:
- Users
- Roles
- Permissions
- OAuth clients
- Scopes
- Refresh tokens
- Consent

**Recommended database:** SQL Server fileciteturn1file0

### 2. AreaService
Owns:
- Residential areas
- Buildings
- Units
- Facilities
- Parking slots

**Recommended database:** PostgreSQL fileciteturn1file0

### 3. TenantService
Owns:
- Tenant profiles
- Leases
- Lease occupants
- Tenant documents
- Move requests

**Recommended database:** PostgreSQL fileciteturn1file0

### 4. ResidentService
Owns:
- Residents
- Households
- Vehicles
- Visitor passes
- Complaints
- Activities

**Recommended database:** PostgreSQL fileciteturn1file0

### 5. BrowseRequestService
Owns:
- Rental listings
- Viewing requests
- Rent applications
- Facility bookings

**Recommended database:** PostgreSQL fileciteturn1file0

### 6. ProviderService
Owns:
- Utility providers
- Service subscriptions
- Meter readings
- Utility bills
- Provider webhooks

**Recommended database:** MongoDB fileciteturn1file0

### 7. PaymentService
Owns:
- Invoices
- Payments
- Transactions
- Refunds
- Ledger entries

**Recommended database:** SQL Server fileciteturn1file0

### 8. ChatService
Owns:
- Conversations
- Messages
- Participants
- Read receipts
- Chat settings

**Recommended database:** MongoDB fileciteturn1file0

## Database Strategy

| Service | Database | Why |
|---|---|---|
| AuthService | SQL Server | Strong consistency, auditability, identity, OAuth client configuration |
| PaymentService | SQL Server | Strict ACID consistency for invoices, refunds, and ledger records |
| AreaService | PostgreSQL | Hierarchical and relational area data |
| TenantService | PostgreSQL | Lease lifecycle and tenant-unit relationships |
| ResidentService | PostgreSQL | Structured relations, reporting, and filtering |
| BrowseRequestService | PostgreSQL | Rule-driven listings, applications, and bookings |
| ProviderService | MongoDB | Flexible provider payloads and variable billing structures |
| ChatService | MongoDB | Append-heavy messages and flexible attachment metadata |
| SQLite | Offline/Edge only | Kiosks, mobile sync, worker checkpoints; not for core shared storage | fileciteturn1file0

## Core Domain Models

### AreaService (PostgreSQL)
Main entities:
- `residential_areas`
- `buildings`
- `units`
- `facilities`
- `parking_slots` fileciteturn1file0

### TenantService (PostgreSQL)
Main entities:
- `tenants`
- `leases`
- `lease_occupants`
- `tenant_documents`
- `move_requests` fileciteturn1file0

### ResidentService (PostgreSQL)
Main entities:
- `residents`
- `resident_household_members`
- `resident_vehicles`
- `visitor_passes`
- `complaints`
- `resident_activities` fileciteturn1file0

### BrowseRequestService (PostgreSQL)
Main entities:
- `rental_listings`
- `rent_applications`
- `viewing_requests`
- `facility_bookings` fileciteturn1file0

### ProviderService (MongoDB)
Main entities:
- `providers`
- `service_subscriptions`
- `meter_readings`
- `utility_bills`
- `provider_webhook_logs` fileciteturn1file0

### PaymentService (SQL Server)
Main entities:
- `Invoices`
- `Payments`
- `PaymentTransactions`
- `Refunds`
- `LedgerEntries` fileciteturn1file0

### AuthService (SQL Server)
Main entities:
- `Users`
- `Roles`
- `Permissions`
- `UserRoles`
- `OAuthClients`
- `OAuthScopes`
- `RefreshTokens`
- `UserConsents` fileciteturn1file0

### ChatService (MongoDB)
Main entities:
- `conversations`
- `messages`
- `read_receipts`
- `conversation_settings` fileciteturn1file0

## Authentication and Authorization

The design recommends:

- **OAuth2 + OpenID Connect**
- **JWT access tokens**
- **Authorization Code + PKCE** for browser/mobile clients
- **Refresh tokens** for session continuation
- **Client Credentials** for service-to-service authentication
- **RBAC + scope-based authorization**
- Avoid the **password grant** in production fileciteturn1file0

Example roles:
- `SUPER_ADMIN`
- `AREA_MANAGER`
- `BUILDING_MANAGER`
- `RESIDENT`
- `TENANT`
- `PROVIDER_ADMIN`
- `SECURITY_STAFF`

Example scopes:
- `openid`
- `profile`
- `area.read`
- `resident.read`
- `resident.write`
- `booking.create`
- `booking.approve`
- `payment.read`
- `payment.create`
- `chat.send` fileciteturn1file0

## Event-Driven Flows

The design uses an event bus to decouple services.

### Example events
- `LeaseCreated`
- `LeaseTerminated`
- `ResidentRegistered`
- `UtilityBillGenerated`
- `InvoiceCreated`
- `PaymentCompleted`
- `PaymentFailed`
- `BookingApproved`
- `MessageSent` fileciteturn1file0

### Example workflows

#### Utility bill flow
1. `ProviderService` generates a bill
2. Publishes `UtilityBillGenerated`
3. `PaymentService` creates an invoice
4. Payment is executed
5. `PaymentCompleted` is published back to the originating service fileciteturn1file0

#### Facility booking flow
1. `BrowseRequestService` creates a booking request
2. Booking is approved
3. `PaymentService` optionally creates an invoice
4. Payment success confirms the booking fileciteturn1file0

## Indexing Guidance

Recommended first-wave indexes include:

### PostgreSQL
- `units(building_id, occupancy_status)`
- `leases(unit_id, lease_status)`
- `residents(unit_id, status)`
- `rental_listings(status, available_from)`
- `facility_bookings(facility_id, booking_date, start_time, end_time)`

### SQL Server
- `Users(Email, Username)`
- `OAuthClients(ClientId)`
- `RefreshTokens(UserId, ClientId, ExpiresAt)`
- `Invoices(PayerUserId, Status, DueDate)`
- `Payments(PaymentNo, Status, CompletedAt)`

### MongoDB
- `utility_bills.subscriptionId`
- `utility_bills.status`
- `messages.conversationId + sentAt`
- `conversations.participantIds`
- `read_receipts.messageId + userId` fileciteturn1file0

## Recommended Implementation Order

The suggested rollout order is:

1. AuthService
2. AreaService
3. TenantService
4. ResidentService
5. BrowseRequestService
6. ProviderService
7. PaymentService
8. ChatService fileciteturn1file0

## Suggested Repository Structure

```text
.
├── docs/
│   └── architecture/
├── services/
│   ├── auth-service/
│   ├── area-service/
│   ├── tenant-service/
│   ├── resident-service/
│   ├── browse-request-service/
│   ├── provider-service/
│   ├── payment-service/
│   └── chat-service/
├── gateway/
├── deploy/
├── docker-compose.yml
└── README.md
```

## Tech Stack

- **Backend:** Microservices
- **Auth:** OAuth2, OpenID Connect, JWT
- **Databases:** PostgreSQL, SQL Server, MongoDB, SQLite (offline only)
- **Messaging:** RabbitMQ or Kafka
- **API Layer:** Controllers or Minimal APIs, Swagger/OpenAPI fileciteturn1file0

## Getting Started

This repository currently follows the system design and can be implemented incrementally by service.

### Suggested startup sequence
1. Set up the API Gateway
2. Build `AuthService`
3. Build `AreaService`
4. Add lease and resident workflows
5. Integrate payment and provider workflows
6. Add chat and notification capabilities

## Final Recommendation

Use:
- **SQL Server** for `AuthService` and `PaymentService`
- **PostgreSQL** for `AreaService`, `TenantService`, `ResidentService`, and `BrowseRequestService`
- **MongoDB** for `ProviderService` and `ChatService`
- **SQLite** only for offline or edge-side sync/caching scenarios fileciteturn1file0

