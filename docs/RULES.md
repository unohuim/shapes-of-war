# Rules

These are the current foundational rules. Values and unresolved cases may be tuned later only after being recorded as open questions.

## Bases

Each player starts with a Wood Base.

| Base | Points |
| --- | ---: |
| Wood Base | 3 |
| Stone Base | 5 |
| Metal Base | 7 |

Base upgrades are linear:

- Wood Base -> Stone Base
- Stone Base -> Metal Base

Upgrade costs:

- Wood Base -> Stone Base costs 2 Stone.
- Stone Base -> Metal Base costs 2 Metal.

When a base reaches 0 points, that player is eliminated.

## Starting Setup

Each player starts with:

- 3 Squares
- 0 resources
- a Wood Base with 3 points

## Units

There are three unit types:

- Triangle
- Square
- Circle

Units are represented only by shape. Units do not have levels, strength values, hidden stats, or counter bonuses.

## Unit Costs

| Unit | Cost |
| --- | --- |
| Triangle | 1 Metal |
| Square | 1 Stone |
| Circle | 1 Wood |

## Combat Foundation

The current assumed hierarchy is:

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

This is a design foundation and may be tuned later.

## Resources

There are three resources:

- Wood
- Stone
- Metal

Current unit production:

| Unit | Produces |
| --- | --- |
| Triangle | Wood |
| Square | Stone |
| Circle | Metal |

This creates tension because Circles are individually weak but produce Metal, which is valuable.

Resource storage is unlimited. Players may hold unlimited Wood, Stone, and Metal.

Resources are public. Each player's Wood, Stone, and Metal totals are visible to all players.

Unit and resource supplies are effectively unlimited. Spent resources and discarded or sacrificed units return to circulation. Physical component scarcity is not a gameplay rule.

Trading and negotiation are not allowed.

## Turn Structure

On a player's turn:

1. Count units.
2. Collect resources based on units.
3. Spend resources.
4. Take an action phase option.

During the spend resources step, the active player may:

- buy units
- upgrade their base
- sacrifice or discard 1 unit to draw 1 action card

Bought units enter play immediately. Units purchased during the spend phase are available for later choices in that same turn, unless an existing rule explicitly prevents it.

Units sacrificed or discarded to draw action cards return to the shared supply or circulation. They are not permanently removed from the game.

During the action phase, the active player may choose one:

- start Battle Royale
- play 1 action card
- pass

A player may not both start Battle Royale and play an action card on the same turn.

## Action Cards

There are only four current action cards:

- Raid Base
- Resource Theft
- Unit Kill
- Counter

Resource Theft steals 1 resource from another player.

Unit Kill destroys 1 unit controlled by another player.

Raid Base attacks another player's base using 1 unit.

Counter cancels an action card used against you or a Counter in the current response chain.

Action deck composition:

| Card | Count |
| --- | ---: |
| Raid Base | 10 |
| Resource Theft | 10 |
| Unit Kill | 10 |
| Counter | 20 |

Action card hand size is unlimited.

Action card identities are private and hidden. Whether action card hand counts are public or hidden is unresolved.

Raid Base, Resource Theft, and Unit Kill may only be played during the active player's action phase. Counter may only be played as a response.

Counter can respond to Raid Base, Resource Theft, Unit Kill, or another Counter. Counter chains may continue until players stop responding or run out of Counter cards.

## Action Card Defense

- Resource Theft can only be stopped by Counter.
- Unit Kill can only be stopped by Counter.
- Raid Base can be stopped by Counter or by defending with units.
- Units cannot stop Resource Theft.
- Units cannot stop Unit Kill.
- Units can defend against Raid Base.
- Only Raid Base can be defended with units.

## Raid Base Flow

1. The active player plays Raid Base.
2. The active player commits 1 unit to raid another player's base.
3. The target player may respond with Counter.
4. If not Countered, the target player may defend with enough units to beat the raiding unit.
5. If the raid is not stopped or successfully defended, the target base loses 1 point.
6. The raiding unit is discarded after resolution.

## Battle Royale

A player may start Battle Royale during their action phase.

Battle Royale structure:

- Starting Battle Royale requires playing 1 unit.
- Players go around the table in order.
- Each player may pass or play enough units to beat the current winning play.
- Passing removes that player from the current Battle Royale.
- The battle naturally escalates.
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
- There are no victory points currently tied to Battle Royale.

Same-shape plays do not beat the current play. A later play must beat the current play, not merely match it. For example, 1 Square does not beat 1 Square.

Mixed-shape Battle Royale plays are not currently allowed. Players must commit only one shape type in a Battle Royale play. Mixed-shape combat remains an open question for future variants, not part of the current core rules.

## Action Card Acquisition

Current action card sources:

- Sacrifice or discard 1 unit during the spend phase to draw 1 action card.
- Win Battle Royale to draw 1 action card.
- Eliminate another player to draw 1 action card.

No other action-card acquisition methods are currently defined.

## Elimination

When a player's base reaches 0 points, that player is eliminated.

On elimination:

- The eliminated player's units are discarded.
- The eliminated player's resources are discarded.
- The eliminated player's action cards are discarded.
- The player who eliminated them draws 1 action card.
