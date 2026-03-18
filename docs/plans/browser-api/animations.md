# Animations

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed Web Animations API. Generated from `web-animations.idl` with hand-written keyframe builders.

## Use Cases

- **Typed keyframe definitions** — CSS properties as C# objects, not string dictionaries
- **Awaitable animations** — `await animation.Finished`
- **Animation control** — play, pause, reverse, cancel with typed API

## Key Interfaces

| WebIDL | C# Class | Key Members |
|--------|----------|-------------|
| `Animation` | `Animation` | `Play()`, `Pause()`, `Reverse()`, `Cancel()`, `Finished` (Promise) |
| `KeyframeEffect` | `KeyframeEffect` | `Target`, `GetKeyframes()`, `SetKeyframes()` |
| `AnimationTimeline` | `AnimationTimeline` | `CurrentTime` |
| `DocumentTimeline` | `DocumentTimeline` | inherits `AnimationTimeline` |

## Ergonomic Additions (hand-written)

- **Keyframe builder** — `new Keyframes().From(s => { ... }).To(s => { ... })`
- **AnimationOptions** — `Duration`, `Easing`, `Fill`, `Delay`, `Iterations` as typed properties
- **Easing functions** — `Easing.CubicBezier(0.16, 1, 0.3, 1)`, `Easing.Spring(...)` (Level 2)
- **Element extension** — `element.Animate(keyframes, options)` returns awaitable `Animation`

## Scope

Phase 1: Animation, KeyframeEffect, basic playback
Phase 2: Animation groups, timeline, sequencing
Phase 3: Spring physics (Web Animations Level 2)
