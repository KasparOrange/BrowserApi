# Storage

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed wrappers for browser storage APIs — localStorage, sessionStorage, and IndexedDB. Generated from `webstorage.idl` and `IndexedDB.idl`.

## Use Cases

- **Generic typed storage** — `storage.GetAsync<UserPrefs>()` instead of raw string get + manual deserialization
- **Cross-tab reactivity** — `StorageEvent` typed and observable
- **IndexedDB** — typed object stores, indexes, transactions

## Key Interfaces

| WebIDL | C# Class | Key Members |
|--------|----------|-------------|
| `Storage` | `Storage` | `GetItem`, `SetItem`, `RemoveItem`, `Clear`, `Length` |
| `StorageEvent` | `StorageEvent` | `Key`, `OldValue`, `NewValue`, `Url` |
| `IDBFactory` | `IdbFactory` | `Open`, `DeleteDatabase` |
| `IDBDatabase` | `IdbDatabase` | `CreateObjectStore`, `Transaction` |
| `IDBObjectStore` | `IdbObjectStore` | `Put`, `Get`, `Delete`, `GetAll` |
| `IDBTransaction` | `IdbTransaction` | `ObjectStore`, `Abort`, `OnComplete` |

## Ergonomic Additions (hand-written)

- **TypedStorage<T>** — generic wrapper with serialization
- **Reactive storage** — `OnChanged` event for cross-tab sync
- **Typed IndexedDB** — schema definition in C#, typed queries

## Scope

Phase 1: localStorage, sessionStorage with typed wrapper
Phase 2: StorageEvent, cross-tab reactivity
Phase 3: IndexedDB typed API
