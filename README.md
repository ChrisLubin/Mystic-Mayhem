# Mystic-Mayhem

Made with Unity

To deal with latency and to have a pleasant user-experience in a fast-paced fighting game, client-side prediction istf needed. I worked on CSP for 2 months to later find out that I was limited by Unity's animator not being deterministic. The game's core mechanics relies on animations for when a player is able to attack, when an attack is able to be parried, when to play damage animations, etc. Given Unity's animator is currently not deterministic and CSP being needed to make this game playable, I had to stop development until Unity changes its animator to be deterministic. Not sure when that will happen, or even if it ever will.

Tried a few workarounds and alternate solutions to no avail; I learned a lot though.
