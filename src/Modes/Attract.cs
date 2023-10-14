using NetPinProc.Domain;

namespace NetPinProc.Game.Modes
{
    /// <summary>
    /// Base attract mode that just handles starting a game
    /// </summary>
    public class Attract : Mode
    {
        private BaseGameController _game;

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="game"></param>
        /// <param name="priority"></param>
        public Attract(BaseGameController game, int priority) : base(game, priority) { _game = game; }

        /// <summary>
        /// Start button, starts game and adds a player if the trough is full
        /// </summary>
        /// <param name="sw"></param>
        /// <returns></returns>
        public bool sw_start_active(Switch sw)
        {
            Game.Logger?.Log("start button active", Domain.PinProc.LogLevel.Debug);
            if (_game.Trough?.IsFull() ?? false) //todo: credit check?
            {
                Game.Logger.Log("start button, trough full", Domain.PinProc.LogLevel.Debug);
                Game.StartGame();
                Game.AddPlayer();
                Game.StartBall();
                this.Game.Modes.Remove(this);
            }
            else
            {
                Game.Logger?.Log("attract start. trough balls:" + _game.Trough.NumBalls(), Domain.PinProc.LogLevel.Debug);
                //TODO: Ball search
            }
            return SWITCH_CONTINUE;
        }
    }
}
