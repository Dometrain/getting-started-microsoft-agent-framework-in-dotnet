# Microsoft Agent Framework course examples

The solution is organized with one solution folder per course chapter. Each lesson is a separate source file and can be selected by passing its two-digit demo number to the corresponding console project.

## Prerequisites

- .NET SDK 10.0.103 or later
- `GITHUB_TOKEN` for examples that use GitHub Models
- Azure CLI authentication and the endpoint variables described in Demo 01 for Foundry
- Ollama for the local-agent lesson
- Redis on `localhost:6379` for Redis persistence
- An OTLP receiver on `localhost:4317` for tracing

## Run a console lesson

Use `dotnet run --project <project> -- <demo-number>`. For example, Chapter 3 Demo 02 uses project `src/Chapter03.Console` and argument `02`.

## Lesson map

| Notes | Project | Demo argument |
| --- | --- | --- |
| Chapter 2 Demo 00-02, 04-06 | Chapter02.Console | 00-02, 04-06 |
| Chapter 2 Demo 03-04 | Chapter02.DevUI | not applicable; the host includes DevUI and its multi-turn session behavior |
| Chapter 3 Demo 00-03 | Chapter03.Console | 00-03 |
| Chapter 3 Demo 04 | Chapter03.DevUI | not applicable |
| Chapter 4 Demo 00-03 | Chapter04.Console | 00-03 |
| Chapter 5 Demo 00-05 | Chapter05.Console | 00-05 |
| Chapter 6 Demo 00-03 | Chapter06.Console | 00-03 |
