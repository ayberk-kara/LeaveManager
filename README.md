# Leave Manager

Leave Manager is a desktop leave management system built with .NET 8 and WPF.  
It is designed as a structured, maintainable, and offline-capable solution for small to mid-sized teams.

The application is distributed using ClickOnce and versioned through GitHub Pages.

---

## Overview

Leave Manager was built with a focus on:

- Clear domain modeling
- Predictable UI behavior
- Local-first data persistence
- Structured versioned deployment

Instead of relying on SaaS infrastructure, the system uses a local relational database while maintaining a clean separation between presentation, domain logic, and data access.

---

## Core Capabilities

- Monthly leave calendar overview
- Employee-based leave tracking
- Manager hierarchy filtering (via relational mapping)
- Aggregated leave totals per employee
- Excel export functionality
- Automatic versioned desktop updates
- Offline-first architecture

---

## Architecture

The system follows a layered architecture:

### Presentation Layer
- WPF-based UI
- Structured layout with separation of concerns
- Data-bound components

### Application Layer
- Business rules and domain coordination
- Leave aggregation logic
- Manager-based filtering logic
- Validation and consistency checks

### Data Access Layer
- Entity Framework Core
- Relational modeling
- Migration-based schema evolution

This separation ensures scalability as features grow.

---

## Database Design

The application uses a relational database managed by Entity Framework Core.

### Key Characteristics

- Strongly typed entity models
- Relationship mapping 
- Navigation properties for hierarchical queries
- Indexed lookups for performance
- Migration-based schema management


Relationships are enforced through foreign keys and navigation properties.  
Aggregate calculations (such as total leave days per employee) are computed through structured LINQ queries.

The design allows future expansion, such as:

- Role-based authorization
- Audit logging
- Leave balance tracking
- Multi-level management hierarchies

---

## Excel Export

Leave data can be exported to Excel format for reporting and archival purposes.

The export pipeline:

1. Queries filtered leave data
2. Applies aggregation logic
3. Generates structured tabular output
4. Writes formatted Excel files

This allows integration into reporting workflows without external dependencies.

---

## Deployment Strategy

Leave Manager uses ClickOnce for desktop distribution.

Versioning workflow:

1. Application is published locally.
2. Output is versioned.
3. Deployment artifacts are pushed to the `gh-pages` branch.
4. GitHub Pages serves the update manifests.
5. Users receive automatic update prompts.

This approach enables:

- Controlled version rollout
- Simple update mechanism
- No server maintenance overhead

---

## Installation

Download the latest release:

https://ayberk-kara.github.io/LeaveManager/

Steps:

1. Download `setup.exe`
2. Run installer
3. Desktop shortcut is created
4. Future updates are handled automatically

---

## Technology Stack

- .NET 8
- WPF
- Entity Framework Core
- Relational Database (local persistence)
- ClickOnce
- GitHub Pages

---

## Development

Clone:

```bash
git clone https://github.com/ayberk-kara/LeaveManager.git
