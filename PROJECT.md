# Project

## Purpose

TextAid is a resident Windows application that captures selected text from another application, applies one configurable AI transformation, previews the result, and optionally replaces the original selection. Its subtitle is **Local-first text transformer & translator**. The official product page is `https://www.e-naxos.com/textaid`.

## Problem

Users need quick rewriting, correction, translation, shortening, expansion, simplification, tone changes, and summarization in the application where they work, without switching to a chat workflow or sending text to a remote service by default.

## Users and actors

- A Windows user selects text and triggers TextAid with `Ctrl+C+C`.
- The source application owns the original selection and receives the accepted replacement.
- A local Ollama instance is the default model provider; the user may explicitly configure a remote provider.
- The user is the final authority for `Accept`, `Cancel`, configuration, and Pro-Spec human validations.

## Scope

The V1 flow is selection → double copy → capture → choose transformation → model response → preview → `Accept` or `Cancel`. It includes a dark WPF interface, tray access, Settings, the eight built-in transformations, declarative custom actions, local-first policy, optional remote connection, English source UI with generated locale catalogs, optional debug logging, and a single self-contained Windows x64 executable.

## Out of scope

V1 excludes chat, autonomous agents, RAG, MCP, workflows, conversation memory, transformation history, rich-text preservation, OCR, voice, file processing, cloud synchronization, accounts, telemetry, automatic updates, and complex installers. Later possibilities in source section 92 are ideas, not planned V1 lots.

## Stable functional characteristics

- One selected text, one transformation, one model invocation, one result.
- The main window is a short-lived transaction with only `Accept` and `Cancel` outcomes and at most one active session.
- Local model use works without a remote server. Strict Local allows loopback endpoints only.
- The user can add an action without recompiling the application.
- The product remains small enough to be explained as: “TextAid captures selected text, applies one configurable AI transformation through MAF, and optionally replaces the original text.”

## Glossary

- **Action:** declarative text transformation and its prompt template.
- **Connection:** provider type, endpoint, and authentication method.
- **Model profile:** connection, model, generation settings, and provider-specific options.
- **Invocation session:** captured text, source window, selected action, result, and transaction state.
- **Strict Local:** mode that forbids model traffic beyond the local machine.
- **MAF:** Microsoft Agent Framework, the sole application AI middleware.
