# My3DLearning

## Projects

### [Enterprise Integration Platform](EnterpriseIntegrationPlatform/)

A modern, AI-driven enterprise integration platform built on **.NET 10**, replacing legacy middleware (BizTalk Server) with a cloud-native, horizontally scalable architecture. Implements [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/) using configurable message brokers, durable workflow orchestration, and a self-hosted RAG knowledge system.

### [Terranes](Terranes/)

An immersive 3D property platform where buyers can explore a virtual village of fully designed homes, walk through each home in 3D, test-fit designs onto their own land (like [CanIBuild](https://canibuild.com)), and receive end-to-end indicative quotes from builders, landscapers, furniture suppliers, and smart home providers — all before committing. Agents and users can post their own built homes. The platform acts as a broker and facilitator connecting clients with the entire construction and real estate network.

## Quick Start

### Enterprise Integration Platform

```bash
cd EnterpriseIntegrationPlatform
dotnet restore && dotnet build && dotnet test
cd src/AppHost && dotnet run
```

### Terranes

```bash
cd Terranes
dotnet restore && dotnet build && dotnet test
```

## License

This project is available under the terms specified in the repository.
