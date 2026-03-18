# Fetch

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed Fetch API — Request, Response, Headers, and a fluent HTTP client. Generated from `fetch.idl` with hand-written builder pattern.

## Use Cases

- **Type-safe HTTP requests** from Blazor/WASM
- **Fluent request builder** — method chaining with typed options
- **Streaming responses** — `await foreach` over response body
- **Typed error handling** — pattern match on HTTP errors vs network errors

## Key Interfaces

| WebIDL | C# Class | Key Members |
|--------|----------|-------------|
| `Request` | `Request` | `Url`, `Method`, `Headers`, `Body` |
| `Response` | `Response` | `Status`, `Headers`, `Json<T>()`, `Text()` |
| `Headers` | `Headers` | `Get`, `Set`, `Has`, `Append` |
| `AbortController` | `AbortController` | `Signal`, `Abort()` |
| `AbortSignal` | `AbortSignal` | `Aborted`, `Reason` |
| `FormData` | `FormData` | `Append`, `Get`, `Has` |

## Ergonomic Additions (hand-written)

- **Fluent builder** — `Http.Get<T>(url).WithHeader(...).WithTimeout(...).SendAsync()`
- **Generic deserialization** — `Http.Get<User>(url)` deserializes JSON response
- **Result-based error handling** — `TrySendAsync()` returns discriminated result
- **Progress tracking** — `OnUploadProgress` / `OnDownloadProgress` callbacks
- **Streaming** — `await foreach (var chunk in response.StreamAsync())`

## Scope

Phase 1: Request, Response, Headers, basic fetch
Phase 2: AbortController, FormData, streaming
Phase 3: Cache API, Service Worker integration types
