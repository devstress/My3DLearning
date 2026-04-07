# Terranes

An immersive 3D property platform where buyers can explore a virtual village of fully designed homes, walk through each home, test-fit designs onto their own land, and receive end-to-end indicative quotes — all before committing.

Think **CanIBuild + Envis + Matterport** in one integrated platform, where agents and users can post their own built homes and the entire real estate and construction network connects seamlessly behind the scenes.

---

## The Problem

Building a home in Australia is fragmented. Buyers separately coordinate builder, landscaping, interiors, furniture, and smart home. They often:

- Cannot fully visualise the final outcome
- Do not see total project cost upfront
- Enter contracts without full clarity
- Custom builders quote more than double the expected price

## Our Solution

We are building an immersive 3D "virtual village" platform where buyers can:

1. **Explore** a virtual village of fully designed homes, complete with furniture and landscaping
2. **Walk through** each home in an immersive 3D environment (like [Envis](https://envis.com))
3. **View** completed homes via virtual tours or physical inspections (like [Matterport](https://matterport.com))
4. **Test-fit** a preferred design onto their own block of land (like [CanIBuild](https://canibuild.com))
5. **Modify** designs in real-time 3D on their own block of land
6. **Redesign** — tailored to specific site conditions (subject to consultation and feasibility)
7. **Receive** an indicative quote before proceeding

Instead of imagining the home — they **experience** it.
Instead of guessing the budget — they **understand** it early.

## Business Model

We act as a broker and facilitator connecting clients with:

- Volume or custom builders
- Landscapers
- Interior & furnishing providers
- Smart home suppliers

All legal contracts remain directly between client and each party. We coordinate, streamline, and generate highly qualified leads.

## Long-Term Vision

We aim to build a real-world "Sims-style", metaverse and GTA-inspired property ecosystem where buyers design digitally in an immersive 3D world, and the entire real estate and construction network connects seamlessly behind the scenes.

The platform will integrate:

- **Land Providers** — Supplying available land inventory directly into the system
- **Site Planner App** — Pulling real-scale government land data for accurate block analysis and compliance checks
- **Real Estate Agents** — Managing and listing land and homes within the ecosystem
- **Homeowner Marketplace** — Allowing owners to self-manage and sell their property without agents
- **Solicitors** — Connecting clients with property lawyers for contracts and transactions
- **Compliance/Legal AI (Per Country)** — An AI-powered regulatory engine providing building law guidance tailored to each country

---

## Highlights

- **Immersive 3D Village** — Walk through fully designed homes with furniture and landscaping
- **Site-Aware Placement** — Test-fit any design onto real land blocks using government data
- **Real-Time 3D Editor** — Modify home designs live on your own block
- **End-to-End Quoting** — Indicative quotes from builders, landscapers, furniture, and smart home suppliers
- **AI Video-to-3D** — Record a house and turn it into a 3D model
- **User-Generated Content** — Agents and users post their own built homes
- **Partner Marketplace** — Builders, landscapers, interior designers, and suppliers connected via standardised APIs

## Tech Stack

| Component | Technology | Version |
|---|---|---|
| Runtime | .NET | 10 |
| Language | C# | 14 |
| Testing | NUnit 4.4.0, NSubstitute 5.3.0 | Latest |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run

```bash
cd Terranes

# Restore and build
dotnet restore
dotnet build

# Run the tests
dotnet test
```

## Project Structure

```
Terranes/
├── src/
│   ├── Contracts/                    # Core DTOs, interfaces, and canonical models
│   ├── Models3D/                     # 3D model loading, placement, manipulation (Phase 2)
│   ├── Land/                         # Land data, block analysis, zoning (Phase 2)
│   ├── Quoting/                      # Quote aggregation engine (Phase 2)
│   ├── Marketplace/                  # Home listings, search, agent/owner management (Phase 2)
│   ├── Compliance/                   # AI-powered building regulation checks (Phase 2)
│   └── Platform.Api/                 # User-facing REST API (Phase 5)
├── tests/
│   └── UnitTests/                    # Fast, isolated unit tests
├── docs/                             # Architecture, ADRs, design docs
├── deploy/                           # Kubernetes manifests, Helm charts
├── tutorials/                        # Learning resources
└── rules/                            # Development milestones, coding standards, architecture rules
```

## Documentation

- [Architecture Overview](docs/architecture-overview.md)
- [Developer Setup Guide](docs/developer-setup.md)
- [Milestones](rules/milestones.md)
- [Coding Standards](rules/coding-standards.md)

## Contributing

Contributions are welcome. Please read the [coding standards](rules/coding-standards.md) and [architecture rules](rules/architecture-rules.md) before submitting a pull request.

## License

This project is available under the terms specified in the repository. See the root of the repository for license details.
