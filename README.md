# NetPinProc.Game

![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)  [![netpinproc.release](https://github.com/FlippingFlips/NetPinProc.Game/actions/workflows/netpinproc.game.release-nuget.yml/badge.svg)](https://github.com/FlippingFlips/NetPinProc.Game/actions/workflows/netpinproc.game.release-nuget.yml)

This is a fork continuing on from Compy's pyprocgame port. The main reason for using smaller modules from the other branch is to keep the game separate from a PinProc, vice versa and for future unit tests.

## Uses

* [NetPinProc](https://github.com/FlippingFlips/NetPinProc)
* [NetPinProc.Domain](https://github.com/FlippingFlips/NetPinProc.Domain)

## NetProcGame - Compy
---
[NetProcGame](https://github.com/Compy/NetProcGame) is a port of the [pyprocgame](http://www.github.com/preble/pyprocgame) to the C# programming language. The port was done by Jimmy Lipham and includes most of the major functionality of the pyprocgame framework.

## GameController
---

This library has default implementations for an `IGameController`. This `IGameController` can initialize a `ProcDevice` or a `FakeProcDevice` depending on the `simulation` parameter.

## BaseGameController
---
Implements GameController. Base a game on this class to automate Trough building.

### Examples + Tests
---

🧪 [Tests](tests/NetPinProc.Game.Tests)

🎲 [FakeProcGame P3-ROC](examples/P3-ROC/NetPinProc.FakeProcDevice/)

---

[License](LICENSE.md)