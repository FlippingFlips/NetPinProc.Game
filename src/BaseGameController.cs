using NetPinProc.Domain;
using NetPinProc.Domain.Modes;
using NetPinProc.Domain.PinProc;
using System;

namespace NetPinProc.Game
{
    /// <summary>
    /// This class uses the <see cref="GameController"/> base with added helper methods and setup automation. <para/>
    /// - Tag your trough switches, early switches for a ball trough setup //TODO: setup ball search and saves
    /// </summary>
    public abstract class BaseGameController : GameController
    {
        /// <summary>
        /// Creates a trough mode. <see cref="GameController"/>
        /// </summary>
        /// <param name="machineType"></param>
        /// <param name="logger"></param>
        /// <param name="simulated"></param>
        /// <param name="configuration"></param>
        public BaseGameController(MachineType machineType, ILogger logger = null, bool simulated = false, MachineConfiguration configuration = null) : base(machineType, logger, simulated, configuration)
        {
            //BallSearch bs = new BallSearch()
            //BallSave bs = new BallSave(this, "");
        }

        /// <summary>
        /// Trough mode managed by this game controller
        /// </summary>
        public Trough Trough { get; private set; }
        /// <inheritdoc/>
        public override void BallEnded()
        {
            base.BallEnded();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(BallEnded), LogLevel.Debug);
        }

        /// <inheritdoc/>
        public override void BallStarting()
        {
            base.BallStarting();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(BallStarting), LogLevel.Debug);
        }

        /// <inheritdoc/>
        public override void GameEnded()
        {
            base.GameEnded();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(GameEnded), LogLevel.Debug);
        }

        /// <inheritdoc/>
        public override void GameStarted()
        {
            base.GameStarted();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(GameStarted), LogLevel.Debug);
        }

        /// <summary>
        /// Callback when a ball drains into the <see cref="Trough"/>. <para/>
        /// Calls <see cref="GameController.EndBall"/> when <see cref="Trough.NumBallsInPlay"/> is zero
        /// </summary>
        public virtual void OnBallDrainedTrough()
        {
            Logger?.Log(nameof(BaseGameController) + ": Ball drained", LogLevel.Debug);
            if (Trough.NumBallsInPlay == 0) EndBall();
        }

        /// <summary>
        /// Resets game, adds trough mode, creates trough if doesn't exist.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Logger?.Log(nameof(BaseGameController) + ":" + nameof(Reset)+": adding trough to game modes.", LogLevel.Debug);

            //create a Trough if doesn't exist
            if (Trough == null)
                SetupTrough();

            //add the mode
            Modes.Add(Trough);
        }

        /// <inheritdoc/>
        public override void SetUp()
        {
            base.SetUp();
            Logger?.Log(nameof(BaseGameController) + ":" + nameof(SetUp) + ": game setup.", LogLevel.Debug);
        }

        /// <summary>
        /// Creates a new Trough mode. Called by the game when constructed.
        /// </summary>
        public virtual void SetupTrough()
        {
            //add call back to OnBallDrained which other game classes can use
            var callback = new Action(OnBallDrainedTrough);
            //create the trough
            Trough = new Trough(this, callback);
        }

        /// <inheritdoc/>
        public override void ShootAgain()
        {
            base.ShootAgain();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(ShootAgain), LogLevel.Debug);
        }

        /// <inheritdoc/>
        public override void StartBall()
        {
            base.StartBall();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(StartBall), LogLevel.Debug);
        }

        /// <inheritdoc/>
        public override void StartGame()
        {
            base.StartGame();
            Logger.Log(nameof(BaseGameController) + ":" + nameof(StartGame), LogLevel.Debug);
        }

        /// <inheritdoc/>
        public override void UpdateLamps()
        {
            base.UpdateLamps();            
            if(Modes?.Modes?.Count > 0)
            {
                Logger.Log(nameof(BaseGameController) + ":" + nameof(UpdateLamps) + ": updating all modes lamps", LogLevel.Debug);
                Modes.Modes.ForEach(x => x.UpdateLamps());
            }
            else { Logger.Log(nameof(BaseGameController) + ":" + nameof(UpdateLamps) + ": no modes running", LogLevel.Warning); }
        }
    }
}
