# LLM Bootstrap

This file gives future LLMs a concise onboarding snapshot for Shapes of War. Read the source docs before making changes; this file is a summary, not a replacement for `AGENTS.md` or `docs/`.

## 1. Project Identity

Shapes of War is a simple shape, resource, and action-card strategy game. It is intended to be fast to learn and strategically deep.

The design may later become:

- a card game
- a board/card hybrid
- a digital or mobile game

The current focus is rules clarity and planning the first playable prototype.

## 2. Operating Rules for LLMs

- Read relevant documentation before planning or editing, including `AGENTS.md`, `README.md` files, `docs/`, and any project-authored design or convention files.
- Treat documentation and rules files as the source of truth.
- Ignore generated Unity dependency documentation under `Library/` and package cache folders unless the task explicitly concerns those dependencies.
- Keep changes small, scoped, and consistent with existing docs.
- Do not invent mechanics.
- Clearly separate finalized rules from open questions.
- Do not silently resolve open questions; record ambiguity in `docs/OPEN_QUESTIONS.md`.
- For documentation-only tasks, do not implement game code, create a rules engine, or add tests unless documentation tooling already exists and is clearly safe.

## 3. Explicit Design Constraints

Current core rules must not include:

- levels
- strength values
- dice
- counter bonuses
- hidden unit stats
- victory points
- a victory deck
- mixed-shape Battle Royale plays
- more than the four current action cards

The only current action cards are:

- Raid Base
- Resource Theft
- Unit Kill
- Counter

## 4. Finalized Core Rules

### Players

- The game likely supports 2-4 players.
- A player loses when their base points reach 0.
- When a player's base reaches 0, that player is eliminated.
- Game end when only one player remains is in `docs/PR_ROADMAP.md` for PR-007, but is not yet finalized in `docs/RULES.md`; treat it as pending clarification before implementation.

### Bases

- Each player starts with a Wood Base.
- Wood Base has 3 points.
- Stone Base has 5 points.
- Metal Base has 7 points.
- Base upgrades are linear: Wood -> Stone -> Metal.
- Wood Base -> Stone Base costs 2 Stone.
- Stone Base -> Metal Base costs 2 Metal.

### Units

- Unit types are Triangle, Square, and Circle.
- Units have shape only.
- Units do not have levels, strength values, hidden stats, or counter bonuses.

### Unit Costs

- Triangle costs 1 Metal.
- Square costs 1 Stone.
- Circle costs 1 Wood.
- Bought units enter play immediately.
- Units purchased during the spend phase are available for later choices in that same turn unless an existing rule explicitly prevents it.

### Starting Setup

Each player starts with:

- Wood Base
- 3 base points
- 3 Squares
- 0 resources
- 0 action cards, per the implementation roadmap default

### Resources

- Resources are Wood, Stone, and Metal.
- Resources are public.
- Each player's Wood, Stone, and Metal totals are visible to all players.
- Resource storage is unlimited.
- Resource supplies are effectively unlimited and recycled.
- Spent resources return to circulation.
- Trading and negotiation are not allowed.

### Resource Production

- Triangle produces Wood.
- Square produces Stone.
- Circle produces Metal.

### Action Cards

- Action card count is public.
- Action card identities are private and hidden.
- Players may know how many action cards each opponent has.
- Players may not know which action cards opponents hold unless a future rule explicitly reveals them.
- Action card hand size is unlimited.

Action deck composition:

| Card | Count |
| --- | ---: |
| Raid Base | 10 |
| Resource Theft | 10 |
| Unit Kill | 10 |
| Counter | 20 |

Action card acquisition:

- Sacrifice or discard 1 unit during the spend phase to draw 1 action card.
- Win Battle Royale to draw 1 action card.
- Eliminate another player to draw 1 action card.

Sacrificed or discarded units return to shared supply or circulation and are not permanently removed from the game.

### Turn Structure

On a player's turn:

1. Count units.
2. Collect resources based on units.
3. Spend resources.
4. Take an action phase option.

During the spend resources step, the active player may:

- buy units
- upgrade their base
- sacrifice or discard 1 unit to draw 1 action card

During the action phase, the active player chooses exactly one:

- start Battle Royale
- play 1 action card
- pass

A player may not both start Battle Royale and play an action card on the same turn.

### Combat Foundation

Current assumed hierarchy:

- Triangle is strongest individually.
- Square is middle.
- Circle is weakest individually, but can win by numbers.

Current combat assumptions:

- 1 Triangle beats 1 Square.
- 1 Triangle beats 1 Circle.
- 2 Squares beat 1 Triangle.
- 1 Square beats 1 Circle.
- 2 Circles beat 1 Square.
- 3 Circles beat 1 Triangle.

Same-shape plays do not beat the current play. A later play must beat the current play, not merely match it.

### Action Card Rules

- Raid Base, Resource Theft, and Unit Kill may only be played during the active player's action phase.
- Counter may only be played as a response.
- Counter can respond to Raid Base, Resource Theft, Unit Kill, or another Counter.
- Counter chains may continue until players stop responding or run out of Counter cards.

Resource Theft:

- Steals 1 resource from another player.
- Can only be stopped by Counter.
- Cannot be defended with units.

Unit Kill:

- Destroys 1 unit controlled by another player.
- Can only be stopped by Counter.
- Cannot be defended with units.

### Raid Base

Raid Base flow:

1. Active player plays Raid Base.
2. Active player commits 1 unit to raid another player's base.
3. Target player may respond with Counter.
4. If not Countered, target player may defend with enough units to beat the raiding unit.
5. If the raid is not stopped or successfully defended, the target base loses 1 point.
6. The raiding unit is discarded after resolution.

Only Raid Base can be defended with units.

### Battle Royale

- A player may start Battle Royale during their action phase.
- Starting Battle Royale requires playing 1 unit.
- Players go around the table in order.
- Each player may pass or play enough units to beat the current winning play.
- Passing removes that player from the current Battle Royale.
- There are no mixed-shape Battle Royale plays for now.
- Each Battle Royale play must use only one shape type.
- Subsequent plays must beat the current winning play.
- Matching the same shape does not beat the current play.
- The first play remains winning until another player beats it.
- If nobody joins after the starter plays 1 unit, the starter instantly wins.
- The winner draws 1 action card.
- The winner keeps 1 unit from their winning committed play.
- All other committed units are discarded.
- There is no victory deck.
- There are no victory points tied to Battle Royale.

Mixed-shape combat is a future variant only and is not part of the current core rules.

### Elimination

When a player's base reaches 0 points, that player is eliminated.

On elimination:

- The eliminated player's units are discarded.
- The eliminated player's resources are discarded.
- The eliminated player's action cards are discarded.
- The player who eliminated them draws 1 action card.

## 5. Components and Supplies

Current component families:

- Bases: Wood Base, Stone Base, Metal Base
- Units: Triangle, Square, Circle
- Resources: Wood, Stone, Metal
- Action cards: Raid Base, Resource Theft, Unit Kill, Counter

Unit and resource supplies are effectively unlimited for gameplay. Physical component scarcity is not a gameplay rule. Physical production counts for a printed prototype are not defined.

## 6. PR Roadmap Summary

The implementation roadmap is in `docs/PR_ROADMAP.md`.

- PR-001: Core Game State Model.
- PR-002: Setup and Economy Flow.
- PR-003: Action Card Deck and Spend-Phase Card Acquisition.
- PR-004: Action Phase Choice and Non-Raid Action Cards.
- PR-005: Raid Base.
- PR-006: Battle Royale.
- PR-007: Elimination and Game End.
- PR-008: Minimal Playable UI / Debug Harness.

Known roadmap blockers:

- Raid defense does not currently specify whether defending units used to stop a raid are discarded after resolution.
- Action-card discard, deck exhaustion, and reshuffle behavior need clarification before implementing deck exhaustion behavior.
- Game end when only one player remains appears in the roadmap, but should be finalized in `docs/RULES.md` before or with PR-007.

## 7. Open Questions

Current unresolved question from `docs/OPEN_QUESTIONS.md`:

- How might mixed-shape combat work in a future variant?

Do not implement mixed-shape combat in the first playable prototype.

## 8. Source Docs

Use these files for authoritative detail:

- `AGENTS.md`
- `docs/GAME_OVERVIEW.md`
- `docs/RULES.md`
- `docs/BALANCE_TABLES.md`
- `docs/COMPONENTS.md`
- `docs/OPEN_QUESTIONS.md`
- `docs/PR_ROADMAP.md`

