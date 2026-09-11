# Microsoft Agent Framework course examples

The solution is organized with one solution folder per course chapter. Each lesson is a separate source file and can be selected by passing its two-digit demo number to the corresponding console project.

## Prerequisites

- .NET SDK 10.0.103 or later
- An OpenAI-compatible model provider configured through `OPENAI_API_KEY`, `OPENAI_ENDPOINT` and `OPENAI_MODEL` (see [Model provider setup](#model-provider-setup))
- Azure CLI authentication and the endpoint variables described in Demo 01 for Foundry
- Ollama for the local-agent lesson
- Redis on `localhost:6379` for Redis persistence
- An OTLP receiver on `localhost:4317` for tracing

## Model provider setup

> **Note:** The course videos use GitHub Models (`https://models.github.ai/inference` with a `GITHUB_TOKEN`). GitHub retired that service on 30 July 2026, so the samples now read the provider from environment variables instead. The code is otherwise identical to what is shown in the videos: it still uses `OpenAIClient` from the `Microsoft.Agents.AI.OpenAI` package, so any OpenAI-compatible endpoint works.

| Variable | Required | Default | Purpose |
| --- | --- | --- | --- |
| `OPENAI_API_KEY` | yes | | API key (or any non-empty placeholder for local servers such as Ollama) |
| `OPENAI_ENDPOINT` | no | `https://api.openai.com/v1` | Base URL of an OpenAI-compatible endpoint |
| `OPENAI_MODEL` | no | `gpt-4o-mini` | Model or deployment name |

### OpenAI

```bash
export OPENAI_API_KEY="sk-..."
```

### Azure OpenAI / Microsoft Foundry

Use the OpenAI-compatible endpoint of your Foundry resource and the name of a chat deployment:

```bash
export OPENAI_ENDPOINT="https://<resource>.openai.azure.com/openai/v1"
export OPENAI_API_KEY="<azure-api-key>"
export OPENAI_MODEL="<deployment-name>"
```

### Ollama (local, free)

Ollama exposes an OpenAI-compatible endpoint. Pull a model that supports tool calling, then:

```bash
ollama pull qwen3
export OPENAI_ENDPOINT="http://localhost:11434/v1"
export OPENAI_API_KEY="ollama"
export OPENAI_MODEL="qwen3"
```

Chapter 4 relies on structured output and Chapters 3-6 rely on tool calling; small local models may follow the schema less reliably than `gpt-4o-mini`.

On Windows PowerShell use `$env:OPENAI_API_KEY = "..."` instead of `export`.

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
