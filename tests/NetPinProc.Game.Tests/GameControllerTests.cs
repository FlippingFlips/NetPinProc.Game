using NetPinProc.Domain;
using NetPinProc.Domain.PinProc;
using NetPinProc.Game;
using NetPinProc.Game.Modes;

namespace NetProc.Game.Tests
{
    public class GameControllerTests
    {
        /// <summary>
        /// path to machine configuration to set the board with
        /// </summary>
        const string MACHINE_JSON = "machine.json";

        /// <summary>
        /// Testing a simulated FAKE <see cref="BaseGameController"/>. No game loop, just config, game and mode creating. Uses the `machine.json` for configuration.
        /// </summary>
        [Fact]
        public void CreateGameControllerAndConfig_PDB_Tests()
        {
            //new method:
            //load config first and pass that to the game, the game can then setup the machine
            var config = MachineConfiguration.FromFile(MACHINE_JSON);
            var game = new TestBaseGameController(MachineType.PDB, null, true, config);

            //assert
            Assert.True(game.Switches?.Count > 0);
            Assert.True(game.LEDS?.Count > 0);
            Assert.True(game.Coils?.Count > 0);

            //reset to create trough add mode
            game.Reset();

            //test method on trough, it will be empty and show this isn't null
            Assert.True(game.Trough.IsFull() == false);
        }

        /// <summary>
        /// Testing a simulated FAKE <see cref="BaseGameController"/>. No game loop. Getting events from P-ROC, setting the trough switches and checking if full.
        /// </summary>
        [Fact]
        public async Task CreateGameControllerAndConfig_FillTrough_RunGame_PDB_Tests()
        {
            //load config first and pass that to the game, the game can then setup the machine
            var config = MachineConfiguration.FromFile(MACHINE_JSON);
            var game = new TestBaseGameController(MachineType.PDB, null, true, config);
            var fakeProc = (game.PROC as IFakeProcDevice);

            //assert
            Assert.True(game.Switches?.Count > 0);
            Assert.True(game.LEDS?.Count > 0);
            Assert.True(game.Coils?.Count > 0);

            //reset to create trough add mode
            game.Reset();

            //test method on trough, it will be empty and show this isn't null
            Assert.True(game.Trough.IsFull() == false);

            //fill the trough - create some switch events to fake a board
            var troughSwitches = game.Switches?.Values.Where(x => x.Name.Contains("trough"))?.ToArray();
            if (troughSwitches != null)
            {
                for (int i = 0; i < troughSwitches.Length; i++) { fakeProc?.AddSwitchEvent(troughSwitches[i].Number, EventType.SwitchClosedDebounced); }
            }
            else { throw new NullReferenceException("no trough switches"); }

            //get events, no dmd and process them then start a game
            var events = game.GetEvents(false);
            if (events != null)
            {
                //process each of the trough events we have. This will set the switch in code and add to a modes `HandleEvent`
                Assert.True(events?.Count() == 4);

                ProcessEvents(game, events);

                // get events with dmd = 16. The trough switches have been cleared, this would have been 20 if not for previous step
                events = game.GetEvents(true);
                Assert.True(events?.Count() == 16);

                //trough should be full in this machine config for 4 switches
                Assert.True(game.Trough.IsFull());

                //add attract mode to start the game, Ball = 0
                var attract = new Attract(game, 50);
                game.Modes.Add(attract);
                Assert.Equal(game.Ball, 0);

                //process start button open / closed. Attract mode should pick this up in handler
                var startBtn = game.Switches?.Values.Where(x => x.Name.Contains("start"))?.FirstOrDefault();
                fakeProc?.AddSwitchEvent(startBtn.Number, EventType.SwitchClosedDebounced);
                fakeProc?.AddSwitchEvent(startBtn.Number, EventType.SwitchOpenDebounced);
                events = game.GetEvents(false);
                ProcessEvents(game, events);

                //game started, Ball = 1
                Assert.Equal(game.Ball, 1);

                //launch a ball
                game.Trough.LaunchBalls(1, null, false);
                Assert.Equal(game.Trough.NumBallsInPlay, 1);

                //enable the trough switches and raise event with plungerlane switch
                // todo: add delay testing. Trough runs check switches after any active / inactive
                game.Switches["trough0"].SetState(false);
                game.Switches["trough1"].SetState(false);
                game.Switches["trough2"].SetState(false);
                game.Switches["trough3"].SetState(false);
                fakeProc?.AddSwitchEvent(game.Switches["plungerLane"].Number, EventType.SwitchClosedDebounced);
                events = game.GetEvents(false);
                ProcessEvents(game, events);

                game.Switches["trough0"].SetState(true);
                game.Switches["trough1"].SetState(true);
                fakeProc?.AddSwitchEvent(game.Switches["trough2"].Number, EventType.SwitchClosedDebounced);                
                events = game.GetEvents(false);
                ProcessEvents(game, events);

                //drain the ball
                fakeProc?.AddSwitchEvent(game.Switches["plungerLane"].Number, EventType.SwitchClosedDebounced);
                fakeProc?.AddSwitchEvent(game.Switches["trough3"].Number, EventType.SwitchClosedDebounced);
                events = game.GetEvents(false);
                ProcessEvents(game, events);

                //next ball in play, ball 2
                Assert.Equal(game.Ball, 2);
                game.Trough.LaunchBalls(1, null, false);
            }
            else { throw new NullReferenceException("should be event here from the fake P-ROC switch event"); }
        }

        void ProcessEvents(IGameController game, Event[] events) { for (int i = 0; i < events?.Length; i++) { game.ProcessEvent(events[i]); } }

        /// <summary>
        /// Create a simulated FAKE game controller with no machine config, that shouldn't crash but have zero machine items. <para/>
        /// This Game creates a trough so that should fail nicely and remind dev there is no config set
        /// </summary>
        [Fact]
        public void CreateGameControllerNoConfig_SwitchesEqualToZero_Tests()
        {
            //create game no machine config
            var game = new TestBaseGameController(MachineType.PDB, null, true, null);

            //assert
            Assert.True(game.Switches.Count == 0);
        }
    }

    /// <summary>
    /// Base controller abstract, need to create your own game
    /// </summary>
    public class TestBaseGameController : BaseGameController
    {
        public TestBaseGameController(MachineType machineType, ILogger? logger = null, bool simulated = false, MachineConfiguration? configuration = null) : base(machineType, logger, simulated, configuration)
        {
        }
    }
}