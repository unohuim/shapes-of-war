# Game Overview

This prototype is a simple shape, resource, and action-card strategy game. It should be fast to learn while leaving room for strategic depth.

The design may later become:

- a card game
- a board/card hybrid
- a digital or mobile game

The current goal is to document a rules foundation clearly enough for a later prototype. This documentation does not define implementation details or a rules engine.

## Core Loop

Players build units, collect resources from those units, spend resources to buy units or upgrade their base, and use action cards or Battle Royale conflicts to attack, defend, and disrupt opponents.

## Players and Elimination

- The game supports multiple players, likely 2-4.
- Each player has a base.
- A player loses when their base reaches 0 points.
- When a player's base reaches 0 points, that player is eliminated.

## Design Pillars

- Use only a small set of readable components.
- Keep units represented by shape only.
- Keep the rules teachable without losing meaningful decisions.
- Use resource tension to create strategy.
- Keep finalized rules separate from open design questions.

## Current Component Families

- Bases: Wood Base, Stone Base, Metal Base
- Units: Triangle, Square, Circle
- Resources: Wood, Stone, Metal
- Action cards: Raid Base, Resource Theft, Unit Kill, Counter

## Explicit Non-Goals

Do not add these mechanics to the current foundation:

- levels
- strength values
- dice
- counter bonuses
- hidden stats
- victory points
- victory deck
- action cards beyond the current four
