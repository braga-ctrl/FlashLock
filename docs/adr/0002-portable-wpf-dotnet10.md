# ADR 0002 — Build the portable UI with WPF on .NET 10

**Status:** Accepted

## Context

FlashLock should run from a USB without requiring a web server, browser extension, or machine-wide app installation.

## Decision

Use WPF on .NET 10 and publish self-contained Windows builds. Keep the UI dependency-light and use native Windows capabilities where possible.

## Consequences

- Windows-only v1.
- Self-contained publish is larger than framework-dependent publish.
- Straightforward access to Win32 and Windows security APIs.
