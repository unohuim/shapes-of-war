You are working in the Shapes of War game prototype repo.

Before making changes, read all existing project documentation and markdown files relevant to this task, especially:

- AGENTS.md
- README.md files, if any
- docs/GAME_OVERVIEW.md
- docs/RULES.md
- docs/BALANCE_TABLES.md
- docs/COMPONENTS.md
- docs/OPEN_QUESTIONS.md
- docs/PR_ROADMAP.md
- any other project-authored markdown files that define rules, conventions, roadmap, or design direction

Ignore Unity dependency/generated documentation under Library/ or package cache folders.

Task:
Create or overwrite a root-level `LLM_BOOTSTRAP.md` file that consolidates the important foundational information from the existing docs into one concise onboarding document for any future LLM.

This is documentation-only.

Do not implement game code.
Do not create a rules engine.
Do not add tests unless documentation tooling already exists and is clearly safe.
Do not invent new mechanics.
Do not add levels, strength values, dice, counter bonuses, hidden stats, victory points, mixed-shape combat, or a victory deck.

Purpose of `LLM_BOOTSTRAP.md`:
Give a future LLM enough context to understand the game, rules, constraints, roadmap, and current unresolved questions before proposing changes.

The bootstrap doc should include:

## 1. Project Identity

Summarize:

- Project name: Shapes of War
- Simple shape/resource/action-card strategy game
- Intended to be simple, fast to learn, and strategically deep
- May become a card game, board/card hybrid, or digital/mobile game
- Current focus is rules clarity and first playable prototype planning

## 2. Operating Rules for LLMs

Consolidate repo guidance from AGENTS.md, including:

- Read relevant docs before planning or editing
- Documentation and rules are the source of truth
- Do not silently resolve open questions
- Keep changes small and scoped
- Do not invent mechanics
- Clearly separate finalized rules from open questions

## 3. Explicit Design Constraints

State that current core rules must not include:

- levels
- strength values
- dice
- counter bonuses
- hidden unit stats
- victory points
- victory deck
- mixed-shape Battle Royale plays
- more than the four current action cards

Current action cards are only:

- Raid Base
- Resource Theft
- Unit Kill
- Counter

## 4. Finalized Core Rules

Consolidate from RULES.md and BALANCE_TABLES.md:

### Players

- Supports likely 2–4 players
- Player loses when base points reach 0
- Game ends when only one player remains if finalized in docs; if not finalized, mark as pending clarification

### Bases

- Each player starts with Wood Base
- Wood Base has 3 points
- Stone Base has 5 points
- Metal Base has 7 points
- Base upgrade path is linear:
  - Wood → Stone → Metal
- Wood Base → Stone Base costs 2 Stone
- Stone Base → Metal Base costs 2 Metal

### Units

- Triangle
- Square
- Circle
- Units have shape only
- No levels or strength values

### Unit Costs

- Triangle costs 1 Metal
- Square costs 1 Stone
- Circle costs 1 Wood
- Bought units enter play immediately

### Starting Setup

- Each player starts with:
  - Wood Base
  - 3 base points
  - 3 Squares
  - 0 resources
  - 0 action cards, if documented as current default

### Resources

- Wood
- Stone
- Metal
- Resources are public
- Resource storage is unlimited
- Resource supplies are effectively unlimited/recycled

### Resource Production

- Triangle produces Wood
- Square produces Stone
- Circle produces Metal

### Action Cards

- Action card count is public
- Action card identities are private/hidden
- Action card hand size is unlimited
- Action deck composition:
  - 10 Raid Base
  - 10 Resource Theft
  - 10 Unit Kill
  - 20 Counter

### Action Card Acquisition

- Sacrifice/discard 1 unit during spend phase to draw 1 action card
- Winner of Battle Royale draws 1 action card
- Player who eliminates another player draws 1 action card
- Sacrificed/discarded units return to supply/circulation

### Turn Structure

1. Count units
2. Collect resources based on units
3. Spend resources:
   - buy units
   - upgrade base
   - sacrifice/discard a unit to draw an action card
4. Action phase:
   - start Battle Royale
   - play one action card
   - pass

### Action Phase

- Player chooses exactly one:
  - start Battle Royale
  - play one action card
  - pass
- Player cannot both start Battle Royale and play an action card on the same turn

### Action Card Effects

- Resource Theft steals 1 resource from another player
- Unit Kill destroys 1 unit controlled by another player
- Raid Base attacks another player’s base using 1 unit
- Counter cancels an action card used against you
- Counter can respond to Raid Base, Resource Theft, Unit Kill, or another Counter
- Counter chains continue until players stop responding or run out of Counters
- Resource Theft and Unit Kill can only be stopped by Counter
- Raid Base can be stopped by Counter or defending units

### Raid Base

Include current finalized raid flow:

- Active player plays Raid Base
- Active player commits 1 unit to raid another player’s base
- Target player may respond with Counter
- If not Countered, target may defend with enough units to beat the raiding unit
- If raid succeeds, target base loses 1 point
- Raiding unit is discarded after resolution
- If docs resolve defending unit discard behavior, include it
- If not resolved, mark it as an implementation blocker

### Combat Hierarchy

Include current one-shape hierarchy:

- 1 Triangle beats 1 Square
- 1 Triangle beats 1 Circle
- 2 Squares beat 1 Triangle
- 1 Square beats 1 Circle
- 2 Circles beat 1 Square
- 3 Circles beat 1 Triangle
- Same-shape plays do not beat the current play

### Battle Royale

Include current finalized Battle Royale rules:

- A player may start Battle Royale during action phase
- Starting Battle Royale requires playing 1 unit
- Players go around the table in order
- Each player may pass or play enough units to beat the current winning play
- Passing removes that player from current Battle Royale
- No mixed-shape Battle Royale plays
- Each Battle Royale play must use only one shape type
- Subsequent plays must beat the current winning play
- Matching the same shape does not beat current play
- First play remains winning until another player beats it
- If nobody joins after starter plays 1 unit, starter instantly wins
- Winner draws 1 action card
- Winner keeps 1 unit from their winning committed play
- All other committed units are discarded
- No victory deck
- No victory points

### Elimination

- Player is eliminated when base reaches 0 points
- Eliminated player’s units are discarded
- Eliminated player’s resources are discarded
- Eliminated player’s action cards are discarded
- Player who eliminates them draws 1 action card

## 5. Current PR Roadmap Summary

Summarize docs/PR_ROADMAP.md, including:

- PR-001: Core Game State Model
- PR-002: Setup and Economy Flow
- PR-003: Action Card Deck and Spend-Phase Card Acquisition
- PR-004: Action Phase Choice and Non-Raid Action Cards
- PR-005: Raid Base
- PR-006: Battle Royale
- PR-007: Elimination and Game End
- PR-008: Minimal Playable UI / Debug Harness

For each PR, include only a short 1–3 sentence summary.

Do not duplicate the entire roadmap.

## 6. Remaining Open Questions / Blockers

Consolidate remaining unresolved questions from OPEN_QUESTIONS.md and PR_ROADMAP.md.

At minimum, preserve:

- Mixed-shape combat is future-variant only and not part of current core rules

Also include any current roadmap blockers still present, such as:

- Whether raid defending units are discarded, if not already resolved in docs
- Action-card discard/deck exhaustion behavior, if not already resolved in docs
- Game-end rule, if not already resolved in docs

Do not resolve these silently.

## 7. Source Docs

At the end, list the docs used to create the bootstrap, including:

- AGENTS.md
- docs/GAME_OVERVIEW.md
- docs/RULES.md
- docs/BALANCE_TABLES.md
- docs/COMPONENTS.md
- docs/OPEN_QUESTIONS.md
- docs/PR_ROADMAP.md

Output requirements:

- Create or overwrite root `LLM_BOOTSTRAP.md`
- Keep it concise but complete
- Make it easy for an LLM to quickly understand the project
- Use clear headings
- Do not contradict existing source docs
- If docs conflict, call out the conflict instead of choosing silently
- At the end of your response, summarize files created/updated and list any assumptions made