using NetPinProc.Domain;
using NetPinProc.Domain.PinProc;
using NetPinProc.Game;

namespace NetProc.Game.Tests
{
    public class FakeGameTests
    {

        [Fact]
        public async void CreateFakeGameController_Tests() 
        {
            var game = new FakeGame(MachineType.PDB, null, true);
            game.LoadConfig("machine.json");

            var tokenSource = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() =>
            {
                game.Coils["trough"].Pulse(200);
                game.RunLoop(cancellationToken:tokenSource);
                ;
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            var mode = new Mode(game, 10);
            game.Modes.Add(mode);

            mode.AddSwitchHandler("trough1", SwitchHandleType.active, 0, new SwitchAcceptedHandler(OnSwitch));
            await Task.Delay(1000);
            var fakeDevice = game.PROC as IFakeProcDevice;
            fakeDevice?.AddSwitchEvent(32, EventType.SwitchClosedDebounced);

            

            await Task.Delay(1000);
            await Task.Delay(25000);
            tokenSource.Cancel();
            await Task.Delay(1000);
        }

        private bool OnSwitch(Switch sw)
        {
            return true;
        }
    }

    public class FakeGame : GameController
    {
        public FakeGame(MachineType machineType, ILogger? logger = null, bool simulated = false) : base(machineType, logger, simulated)
        {
        }
    }
}
