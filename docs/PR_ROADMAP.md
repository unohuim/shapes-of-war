# PR Roadmap

This roadmap breaks the finalized rules into small implementation PRs for the first playable Shapes of War prototype. It is documentation-only and does not implement game code or a rules engine.

Future implementation work must not add levels, strength values, dice, counter bonuses, hidden stats, victory points, mixed-shape combat, a victory deck, or action cards beyond Raid Base, Resource Theft, Unit Kill, and Counter.

## PR-001: Core Game State Model

Status: Implemented.

Implemented in:

- `Assets/Scripts/Domain/ShapesOfWar/`
- `Assets/Tests/EditMode/ShapesOfWar/GameStateModelTests.cs`

Validation:

- Temporary .NET/NUnit compile validation passed with 0 warnings and 0 errors.
- Unity batch EditMode test run could not complete in this environment because Unity exited with code 127 and produced no log or result file.

Goal: Create the basic in-memory/domain representation for a game.

Scope:

- Game state.
- Players.
- Base type and base points.
- Unit shape counts for Triangle, Square, and Circle.
- Resource counts for Wood, Stone, and Metal.
- Public action card hand counts.
- Private action card identities.
- Shared action card deck.
- Discard and circulation concepts.
- Player elimination state.

Out of scope:

- Full turn flow.
- Resource collection.
- Buying units.
- Base upgrades.
- Action card effects.
- Raids.
- Battle Royale.
- Game-end handling.

Key rules covered:

- Supports 2-4 players as the expected range.
- Bases, units, resources, action cards, and elimination state are the core state categories.
- Action card counts are public; card identities are private.
- Unit and resource supplies are effectively unlimited for gameplay.

Suggested tests or validation checks:

- Can create a game state with 2, 3, or 4 players.
- Reject or clearly block unsupported player counts.
- Each player can hold unit counts, resource counts, base state, action cards, and elimination state.
- Public views do not expose opponent action card identities.

Documentation impact:

- Update docs only if implementation uncovers a mismatch between the state model and the current rules.

Implementation blockers:

- None.

## PR-002: Setup and Economy Flow

Status: Implemented.

Implemented in:

- `Assets/Scripts/Domain/ShapesOfWar/`
- `Assets/Tests/EditMode/ShapesOfWar/GameStateModelTests.cs`

Validation:

- Temporary .NET/NUnit compile validation passed with 0 warnings and 0 errors.
- Unity batch EditMode test run could not complete in this environment because Unity exited with code 127 and produced no log or result file.

Goal: Implement starting setup and basic turn economy.

Scope:

- 2-4 player setup.
- Each player starts with Wood Base, 3 base points, 3 Squares, 0 resources, and 0 action cards.
- Public resources.
- Public action card counts.
- Private action card identities.
- Unit production:
  - Triangle produces Wood.
  - Square produces Stone.
  - Circle produces Metal.
- Resource collection.
- Buying units:
  - Triangle costs 1 Metal.
  - Square costs 1 Stone.
  - Circle costs 1 Wood.
- Bought units enter play immediately.
- Base upgrades:
  - Wood Base to Stone Base costs 2 Stone.
  - Stone Base to Metal Base costs 2 Metal.
  - Wood Base has 3 points.
  - Stone Base has 5 points.
  - Metal Base has 7 points.
- Unlimited and recycled unit and resource supplies.

Out of scope:

- Action card deck implementation.
- Sacrificing units for action cards.
- Action phase choices.
- Action card effects.
- Raid Base.
- Battle Royale.
- Elimination rewards.

Key rules covered:

- Starting setup.
- Resource production.
- Unit costs.
- Immediate unit entry after purchase.
- Base upgrade path and costs.
- Public resource totals.
- No trading or negotiation.

Suggested tests or validation checks:

- Setup creates each player with Wood Base, 3 base points, 3 Squares, and 0 resources.
- Resource collection grants resources based on current unit counts.
- Buying a unit spends the correct resource and increases the matching unit count immediately.
- Base upgrades require the correct target resource and update base points.
- Resources cannot be gained through trading or negotiation.

Documentation impact:

- Update balance or rules docs only if implementation discovers an ambiguity in economy sequencing.

Implementation blockers:

- None.

## PR-003: Action Card Deck and Spend-Phase Card Acquisition

Status: Implemented.

Implemented in:

- `Assets/Scripts/Domain/ShapesOfWar/`
- `Assets/Tests/EditMode/ShapesOfWar/GameStateModelTests.cs`

Validation:

- Temporary .NET/NUnit compile validation passed with 0 warnings and 0 errors.
- Unity batch EditMode test run could not complete in this environment because Unity exited with code 127 and produced no log or result file.

Goal: Implement the action card deck and action card acquisition.

Scope:

- Action deck composition:
  - 10 Raid Base.
  - 10 Resource Theft.
  - 10 Unit Kill.
  - 20 Counter.
- Private action card identities.
- Public action card counts.
- Unlimited action card hand size.
- Sacrifice or discard 1 unit during the spend phase to draw 1 action card.
- Sacrificed units return to supply or circulation.
- Used action cards go to an action card discard pile after resolution.
- Empty action deck draws reshuffle the discard pile into a new deck.
- Empty deck plus empty discard pile draws no card.

Out of scope:

- Action card effects.
- Counter chains.
- Raid Base.
- Battle Royale rewards.
- Elimination rewards.

Key rules covered:

- Action deck composition.
- Action card visibility.
- Unlimited action card hand size.
- Spend-phase unit sacrifice for card draw.
- Discarded or sacrificed units return to circulation.

Suggested tests or validation checks:

- Deck starts with exactly 50 cards in the documented composition.
- Drawing a card increases only the drawing player's public hand count in opponent views.
- Drawing a card preserves hidden card identity from opponents.
- Sacrificing a unit reduces that player's selected unit count by 1 and draws 1 action card.
- Cannot sacrifice a unit the player does not have.

Documentation impact:

- Action-card discard, deck exhaustion, and reshuffle behavior are finalized in `docs/RULES.md`.

Implementation blockers:

- None.

## PR-004: Action Phase Choice and Non-Raid Action Cards

Status: Implemented.

Implemented in:

- `Assets/Scripts/Domain/ShapesOfWar/`
- `Assets/Tests/EditMode/ShapesOfWar/GameStateModelTests.cs`

Validation:

- Temporary .NET/NUnit compile validation passed with 0 warnings and 0 errors.
- Focused local PR-004 domain validation passed for Resource Theft, Unit Kill, and odd/even Counter chains.
- Unity batch EditMode test run could not complete in this environment because Unity exited with code 127 and produced no log or result file.

Goal: Implement exclusive action phase choice and simple targeted actions.

Scope:

- During action phase, the active player chooses exactly one:
  - start Battle Royale.
  - play one action card.
  - pass.
- A player may not both start Battle Royale and play an action card on the same turn.
- Resource Theft:
  - steals 1 resource from another player.
  - can only be stopped by Counter.
- Unit Kill:
  - destroys 1 unit controlled by another player.
  - can only be stopped by Counter.
- Counter:
  - can respond to Resource Theft, Unit Kill, Raid Base, or another Counter.
  - Counter chains continue until players stop responding or run out of Counters.

Out of scope:

- Raid Base effect and unit defense.
- Battle Royale resolution.
- Full turn advancement and automatic action phase choice reset.
- Elimination and game end.
- Mixed-shape combat.
- Additional action cards.

Key rules covered:

- Exclusive action phase choice.
- Resource Theft.
- Unit Kill.
- Counter timing and chains.
- Units cannot stop Resource Theft or Unit Kill.

Suggested tests or validation checks:

- A player cannot both start Battle Royale and play an action card in the same action phase.
- Resource Theft transfers exactly 1 resource if not Countered.
- Unit Kill destroys exactly 1 target unit if not Countered.
- Counter cancels the targeted action card or Counter.
- Counter chains resolve consistently until no one continues.
- Public hand counts update as cards are played.

Documentation impact:

- If target-selection restrictions are needed, document them before implementation.

Implementation blockers:

- None. Raid Base remains unimplemented until PR-005, and Battle Royale remains represented only as an action phase choice until PR-006.
- Action phase choice reset is deferred future turn-flow work, not a PR-004 bug.

## PR-005: Raid Base

Status: Implemented.

Implemented in:

- `Assets/Scripts/Domain/ShapesOfWar/`
- `Assets/Tests/EditMode/ShapesOfWar/GameStateModelTests.cs`

Validation:

- Temporary .NET/NUnit compile validation passed with 0 warnings and 0 errors.
- Focused local PR-005 domain validation passed for successful raids, defended raids, and Counter-stopped raids.
- Unity batch EditMode test run could not complete in this environment because Unity exited with code 127 and produced no log or result file.

Goal: Implement Raid Base action and unit-based raid defense.

Scope:

- Active player plays Raid Base during action phase.
- Active player commits 1 unit to raid another player's base.
- Target may respond with Counter.
- If not Countered, target may defend with enough units to beat the raiding unit.
- Only Raid Base can be defended with units.
- Resource Theft and Unit Kill cannot be defended with units.
- If the raid succeeds, target base loses 1 point.
- Raiding unit is discarded after resolution.
- Defending units committed to stop the raid are discarded after resolution.

Out of scope:

- Battle Royale.
- Elimination reward and game-end handling, unless base reduction reaches 0 and existing elimination state must be flagged.
- Mixed-shape combat.
- Additional Raid Base effects.

Key rules covered:

- Raid Base flow.
- Unit defense applies only to Raid Base.
- Current combat assumptions for beating the committed raiding unit.
- Raiding unit is discarded after resolution.

Suggested tests or validation checks:

- Raid Base can be played only during the active player's action phase.
- Counter stops Raid Base before unit defense.
- If not Countered, sufficient defending units stop the raid.
- If not Countered or defended, target base loses 1 point.
- Raiding unit is discarded after resolution.
- Resource Theft and Unit Kill cannot use unit defense.

Documentation impact:

- No blocker remains for defending-unit discard behavior.

Implementation blockers:

- None.

## PR-006: Battle Royale

Status: Implemented.

Implemented in:

- `Assets/Scripts/Domain/ShapesOfWar/`
- `Assets/Tests/EditMode/ShapesOfWar/GameStateModelTests.cs`

Validation:

- Temporary .NET/NUnit compile validation passed with 0 warnings and 0 errors.
- Focused local PR-006 domain validation passed for starter wins, challenger wins, same-shape rejection, winner draw, winner unit retention, and losing committed unit discard.
- Unity batch EditMode test run could not complete in this environment because Unity exited with code 127 and produced no log or result file.

Goal: Implement escalating Battle Royale.

Scope:

- Player may start Battle Royale during action phase.
- Starting Battle Royale requires playing 1 unit.
- Players go around table order.
- Each player may pass or play enough units to beat the current winning play.
- Passing removes that player from the current Battle Royale.
- No mixed-shape Battle Royale plays.
- Each Battle Royale play must use only one shape type.
- Matching the same shape does not beat current play.
- First play remains winning until another player beats it.
- If nobody joins after starter plays 1 unit, starter instantly wins.
- Winner draws 1 action card.
- Winner keeps 1 unit from their winning committed play.
- All other committed units are discarded.
- No victory deck.
- No victory points.

Out of scope:

- Mixed-shape combat.
- Victory points.
- Victory deck.
- Additional Battle Royale rewards.
- Changes to action phase exclusivity.

Key rules covered:

- Battle Royale start condition.
- Escalating single-shape plays.
- Passing removes a player from the current battle.
- Same-shape plays do not beat the current play.
- Winner reward and committed-unit cleanup.

Suggested tests or validation checks:

- Starter can begin with exactly 1 unit.
- A player cannot make a mixed-shape play.
- A same-shape match does not beat the current play.
- A valid later play must beat the current winning play.
- Passing prevents a player from rejoining the current Battle Royale.
- If all other players pass after the starter, the starter wins.
- Winner draws 1 action card and keeps 1 unit from the winning committed play.
- All other committed units are discarded.

Documentation impact:

- Do not define mixed-shape combat in this PR. Leave it in `docs/OPEN_QUESTIONS.md` as a future variant.

Implementation blockers:

- None for current core Battle Royale. Mixed-shape combat remains intentionally unresolved and out of scope.

## PR-007: Elimination and Game End

Goal: Implement player elimination and game-end handling.

Scope:

- Player is eliminated when base points reach 0.
- Eliminated player's units are discarded.
- Eliminated player's resources are discarded.
- Eliminated player's action cards are discarded.
- Player who eliminates another player draws 1 action card.
- Game ends when only one player remains.

Out of scope:

- Scoring.
- Victory points.
- Victory deck.
- Post-game rewards.
- Eliminated player comeback rules.

Key rules covered:

- Base points reaching 0 eliminates a player.
- Eliminated player's holdings are discarded.
- Eliminating player draws 1 action card.
- Last remaining player wins the game.

Suggested tests or validation checks:

- Reducing a base to 0 marks that player eliminated.
- Eliminated player's units, resources, and action cards are discarded.
- Eliminating player draws exactly 1 action card.
- Eliminated players cannot take future turns or actions.
- Game ends when only one player remains.

Documentation impact:

- Game-end wording is finalized in `docs/RULES.md`.

Implementation blockers:

- None.

## PR-008: Minimal Playable UI / Debug Harness

Goal: Create the simplest playable prototype interface or debug harness after the domain rules exist.

Scope:

- Start a 2-4 player game.
- Display public player state:
  - base type.
  - base points.
  - unit counts.
  - resource counts.
  - action card count.
- Keep action card identities hidden from opponents.
- Allow turn progression.
- Allow collecting resources.
- Allow buying units.
- Allow base upgrades.
- Allow action card acquisition.
- Allow action phase choice.
- Allow raids and Battle Royale if prior PRs are complete.

Out of scope:

- Overbuilt visuals.
- Animations.
- AI.
- Matchmaking.
- Persistence.
- Networking.
- Mobile-specific polish.
- Mechanics not already documented.

Key rules covered:

- Public and private information boundaries.
- Core turn flow.
- Economy.
- Action cards.
- Raid Base.
- Battle Royale.
- Elimination state and game end if PR-007 is complete.

Suggested tests or validation checks:

- A local user can complete a basic 2-player flow through setup, resource collection, buying, action card acquisition, action phase choice, Raid Base, Battle Royale, and elimination.
- UI or debug harness never reveals opponent card identities.
- Public action card counts, resources, base state, and unit counts are visible.
- Invalid actions are blocked or clearly unavailable.

Documentation impact:

- Add usage notes only if the harness requires manual steps that are not obvious from the interface.

Implementation blockers:

- This PR depends on PR-001 through PR-007 being complete enough to expose playable domain behavior.

## Remaining Future Variant

- Mixed-shape combat remains a future variant only and must not be implemented in the first playable prototype.
