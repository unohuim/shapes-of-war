# Codex Operating Guide

## Documentation Intake

Before planning or editing, Codex must read all markdown and documentation files relevant to the task. This includes:

- `AGENTS.md`
- root and nested `README.md` files
- `docs/`
- design notes
- architecture notes
- convention files
- any other markdown files that define project direction or standards

Ignore generated dependency documentation such as Unity package cache files unless the task explicitly concerns those dependencies.

## Current Task Boundary

This repository is currently establishing foundational game documentation only.

Codex must not:

- implement game code
- create a rules engine
- add tests unless documentation tooling already exists and is clearly safe

## Game Design Constraints

Do not introduce:

- levels
- strength values
- dice
- counter bonuses
- hidden stats
- victory points
- a victory deck
- more than the four current action cards

The only current action cards are:

- Raid Base
- Resource Theft
- Unit Kill
- Counter

## Authority Order

Treat project direction documents in this order:

1. `AGENTS.md`
2. `README.md`
3. `docs/GAME_OVERVIEW.md`
4. `docs/RULES.md`
5. `docs/BALANCE_TABLES.md`
6. `docs/COMPONENTS.md`
7. `docs/OPEN_QUESTIONS.md`
8. Any other existing docs or convention files

## Work Style

- Make the smallest useful documentation change.
- Clearly separate finalized rules from open questions.
- Do not silently resolve open questions.
- If a rule is ambiguous, document it in `docs/OPEN_QUESTIONS.md`.
- At the end of each task, summarize files created or updated and assumptions made.
