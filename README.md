# Residential Area Management System

Residential Area Management System is a microservices-based platform for residential communities, apartment complexes, and gated societies.

The repository is organized around domain-focused services for areas, tenants, residents, bookings, payments, chat, and identity. Each service owns its own data and communicates with other services through APIs or events when needed.

## What the system covers

- Residential area and building management
- Unit, facility, and parking slot tracking
- Tenant and lease workflows
- Resident profiles, household data, vehicles, and complaints
- Rental listings, viewing requests, and facility bookings
- Utility billing and payment processing
- Messaging and conversation handling
- Authentication and authorization

## Architecture

The project follows a service-per-domain approach:

- Each service has a clear responsibility
- Data is owned by the service that uses it
- Synchronous calls happen through APIs
- Asynchronous workflows can be handled through events
- Identity is designed around OAuth2 and OpenID Connect

## Services

- AuthService for users, roles, permissions, and tokens
- AreaService for residential areas, buildings, units, facilities, and parking slots
- TenantService for tenants, leases, occupants, and move requests
- ResidentService for residents, households, vehicles, visitor passes, and complaints
- BrowseRequestService for listings, applications, and bookings
- ProviderService for utility providers, subscriptions, readings, and bills
- PaymentService for invoices, payments, refunds, and ledger entries
- ChatService for conversations, messages, and read receipts

## Tech Stack

- Backend: .NET microservices
- Authentication: OAuth2, OpenID Connect, JWT
- Messaging: RabbitMQ or Kafka
- Data stores: PostgreSQL, SQL Server, MongoDB, and SQLite where appropriate
- API layer: Controllers or Minimal APIs with OpenAPI support

## Repository Structure

```text
.
├── docs/
├── services/
├── gateway/
├── deploy/
├── docker-compose.yml
└── README.md
```

## Project Status

This repository is being organized as a modular system that can be implemented service by service. The current focus is on keeping the structure clean, readable, and easy to extend.

