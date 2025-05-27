# Can probably optimize here:

- Cache ctbans rather than doing database operations during every check (would need invalidation checks, possibly store expiry date in memory too)
- Add cooldown to commands, especially commands involving database operations by players (like trying to join queue)