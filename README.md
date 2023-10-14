## NetProcGame
---

NetPinProcGame is a port of the [pyprocgame](http://www.github.com/preble/pyprocgame) to the C# programming language. The port was done by Jimmy Lipham and includes most of the major functionality of the pyprocgame framework.

This library has the default implementations for an `IGameController`. This `IGameController` can initialize a `ProcDevice` or a `FakeProcDevice` depending on the `simulation` parameter.

## GameController
---
Base implementation of `IGameController`

## BaseGameController
---
Implements GameController. Base a game from this to automate Trough building.

[License](LICENSE.md)