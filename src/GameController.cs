using System;
using System.Collections.Generic;
using System.Threading;
using NetPinProc.Domain;
using NetPinProc.Domain.Pdb;
using NetPinProc.Domain.PinProc;
using NetPinProc.Domain.Players;

namespace NetPinProc.Game
{
    /// <summary>
    /// Core object representing the game itself.
    /// Usually a game developer will create a new game by inheriting this class.
    /// Consider inheriting 'BasicGame' instead if you are using a DMD, as it makes use of several helpful modes and controllers. See P-ROC/NetProcGameTest
    /// </summary>
    public abstract class GameController : IGameController
    {
        /// <summary>
        /// Thread synchronization object for coils
        /// </summary>
        protected object _coil_lock_object = new object();

        /// <summary>
        /// Coils, used by the machine when setting up
        /// </summary>
        protected AttrCollection<ushort, string, IDriver> _coils;

        /// <summary>
        /// The configuration object loaded by load_config()
        /// </summary>
        protected MachineConfiguration _config;

        /// <summary>
        /// Are the flippers and bumpers currently Enabled?
        /// </summary>
        public bool FlippersEnabled { get; private set; }

        /// <summary>
        /// Contains information specific to the particular location installation (high scores, audits, etc).
        /// </summary>
        protected object _game_data;

        /// <summary>
        /// Game loops waits for this cancel request token source
        /// </summary>
        private CancellationTokenSource _gameLoopCancelToken;

        /// <summary>
        /// List of GI drivers
        /// </summary>
        protected AttrCollection<ushort, string, IDriver> _gi;

        /// <summary>
        /// List of Lamp Drivers
        /// </summary>
        protected AttrCollection<ushort, string, IDriver> _lamps;

        /// <summary>
        /// 
        /// </summary>
        protected AttrCollection<ushort, string, LED> _leds;
        /// <summary>
        /// Machine type used to configure the proc device
        /// </summary>
        protected MachineType _machineType;        

        /// <summary>
        /// 
        /// </summary>
        protected IModeQueue _modes;

        /// <summary>
        /// The total number of balls in the machine
        /// </summary>
        protected int _num_balls_total = 5;

        /// <summary>
        /// A collection of old player objects if Reset is called.
        /// </summary>
        protected List<IPlayer> _old_players;

        /// <summary>
        /// A collection of player objects
        /// </summary>
        protected List<IPlayer> _players;

        /// <summary>
        /// A pinproc class instance, created in the constructor with the Machine_Type attribute
        /// </summary>
        protected IProcDevice _proc;

        /// <summary>
        /// List of coils to drive that is manipulated from outside/UI threads
        /// TODO: This is set to be removed in favor of the UI process communication model
        /// </summary>
        protected List<SafeCoilDrive> _safe_coil_drive_queue = new List<SafeCoilDrive>();

        /// <summary>
        /// TODO: implement switch object lists
        /// </summary>
        protected AttrCollection<ushort, string, Switch> _switches;

        /// <inheritdoc/>

        protected AttrCollection<ushort, string, PdStepper> _steppers;

        /// <inheritdoc/>

        protected AttrCollection<ushort, string, PdServo> _servos;

        /// <inheritdoc/>

        protected AttrCollection<ushort, string, PdSerialLed> _serialLeds;

        /// <summary>
        /// Contains local game information such as volume
        /// </summary>
        protected object _user_settings;

        /// <inheritdoc/>
        public int Ball { get; private set; }

        /// <summary>
        /// The ending time of the current ball
        /// </summary>
        protected double BallEndTime;

        /// <summary>
        /// The number of balls per game
        /// </summary>
        protected byte BallsPerGame = 3;

        /// <summary>
        /// The starting time of the current ball
        /// </summary>
        protected double BallStartTime;

        /// <summary>
        /// The date/time when the framework was started (machine powered up)
        /// </summary>
        protected double BootTime = 0;

        private bool _simulated;

        /// <summary>
        /// Creates a new GameController object with the given machine type and logging infrastructure. <para/>
        /// Calls <see cref="SetUp"/> which any derived classes should override
        /// </summary>
        /// <param name="machineType">Machine type to use (WPC, STERN, PDB etc)</param>
        /// <param name="logger">The logger the interface will use. If set to null the console logger is used</param>
        /// <param name="simulated">If true then a <see cref="FakePinProc"/> will be created instead of a <see cref="ProcDevice"/></param>
        public GameController(MachineType machineType, ILogger logger = null, bool simulated = false)
        {
            if (logger == null) logger = new ConsoleLogger(LogLevel.Verbose);
            Logger = logger;
            _machineType = machineType;
            _simulated = simulated;

            //run setup for p-roc
            SetUp();
        }

        /// <summary>
        /// Sets up the P-ROC device, will create IFakeProcDevice if Game was created as simulated. Initializes all machine items, boot time, players
        /// </summary>
        public virtual void SetUp()
        {
            if (_proc != null) return;

            if (_simulated)
            {
                Logger.Log(nameof(GameController) + nameof(SetUp) + ":creating fake p-roc", LogLevel.Debug);
                _proc = new FakePinProc(_machineType, Logger);
                _proc.Reset(1);
            }
            else
            {
                Logger.Log(nameof(GameController) + nameof(SetUp) +":creating p-roc handle", LogLevel.Debug);
                _proc = new ProcDevice(_machineType, Logger);
                _proc.Reset(1);
            }

            _modes = new ModeQueue(this);
            BootTime = Time.GetTime();
            _coils = new AttrCollection<ushort, string, IDriver>();
            _switches = new AttrCollection<ushort, string, Switch>();
            _steppers = new AttrCollection<ushort, string, PdStepper>();
            _servos = new AttrCollection<ushort, string, PdServo>();
            _serialLeds = new AttrCollection<ushort, string, PdSerialLed>();
            _lamps = new AttrCollection<ushort, string, IDriver>();
            _leds = new AttrCollection<ushort, string, LED>();
            _gi = new AttrCollection<ushort, string, IDriver>();
            _old_players = new List<IPlayer>();
            _players = new List<IPlayer>();

            LampController = new LampController(this);

            Logger.Log(nameof(GameController) + ":" + nameof(SetUp) + ": game, lamp controller created", LogLevel.Debug);
        }

        /// <summary>
        /// Creates a new GameController object with the given machine type and logging infrastructure. <para/>
        /// This constructor loads a machine configuration. see <see cref="LoadConfig(MachineConfiguration)"/>
        /// </summary>
        /// <param name="machineType">Machine type to use (WPC, STERN, PDB etc)</param>
        /// <param name="logger">The logger interface to use</param>
        /// <param name="simulated">If true then a <see cref="FakePinProc"/> will be created instead of a <see cref="ProcDevice"/></param>
        /// <param name="configuration">Optional machine configuration to setup the machine with.</param>
        public GameController(MachineType machineType, ILogger logger, bool simulated = false, MachineConfiguration configuration = null) : this(machineType, logger, simulated)
        {
            _config = configuration;
            if (_config != null)
            {
                LoadConfig(configuration);
            }
            else
                Logger?.Log(nameof(GameController) + " no machine configuration loaded.", LogLevel.Warning);            
        }

        /// <summary>
        /// De constructor, set p-roc to null
        /// </summary>
        ~GameController()
        {
            this._proc = null;
        }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, IDriver> Coils
        {
            get { return _coils; }
            set { _coils = value; }
        }
        /// <inheritdoc/>
        public MachineConfiguration Config => _config;
        /// <inheritdoc/>
        public int CurrentPlayerIndex { get; private set; }

        /// <inheritdoc/>
        public void EnableFlippers(bool enable = true, byte pulseTime = 34)
        {
            //already enabled don't add any more switch rules
            if (FlippersEnabled) return;

            Logger.Log("Setting flippers_enabled to " + enable.ToString(), LogLevel.Debug);
            FlippersEnabled = enable;

            if (_machineType == MachineType.WPC || _machineType == MachineType.WPC95
                || _machineType == MachineType.WPCAlphanumeric || _machineType == MachineType.PDB)
            {
                //Link the all flipper switches to Main and Hold coils
                foreach (var flipper in _config.PRFlippers)
                {
                    // Hold all of the linked coils * 2 (one for Main, one for Hold)
                    DriverState[] drivers = new DriverState[2];
                    IDriver main_coil, hold_coil;

                    ushort switch_num = _switches[flipper].Number;
                    int driverIdx = 0;

                    //get the hold coils for the flipper
                    main_coil = _coils[flipper + "Main"];
                    hold_coil = _coils[flipper + "Hold"];

                    if (FlippersEnabled)
                    {
                        drivers[driverIdx] = _proc.DriverStatePulse(main_coil.State, pulseTime);
                        driverIdx++;
                        drivers[driverIdx] = _proc.DriverStatePulse(hold_coil.State, 0);

                        // Add switch rule for activating flippers (when switch closes)
                        _proc.SwitchUpdateRule(switch_num,
                            EventType.SwitchClosedNondebounced,
                            new SwitchRule { NotifyHost = false, ReloadActive = false },
                            drivers,
                            false
                        );

                        // --------------------------------------------------------------
                        // Now Add the rule for open switches and disabling flippers
                        // --------------------------------------------------------------
                        driverIdx = 0;
                        drivers[driverIdx] = _proc.DriverStateDisable(main_coil.State);
                        driverIdx++;
                        drivers[driverIdx] = _proc.DriverStateDisable(hold_coil.State);

                        _proc.SwitchUpdateRule(switch_num,
                            EventType.SwitchOpenNondebounced,
                            new SwitchRule { NotifyHost = false, ReloadActive = false },
                            drivers,
                            false
                        );
                    }
                    else
                    {
                        // Remove all switch linkages
                        _proc.SwitchUpdateRule(switch_num,
                            EventType.SwitchClosedNondebounced,
                            new SwitchRule { NotifyHost = false, ReloadActive = false },
                            null,
                            false
                        );
                        _proc.SwitchUpdateRule(switch_num,
                            EventType.SwitchOpenNondebounced,
                            new SwitchRule { NotifyHost = false, ReloadActive = false },
                            null,
                            false
                        );
                        // Disable flippers
                        main_coil.Disable();
                        hold_coil.Disable();
                    }
                }
            }

            // Enable the flipper relay on WPC alpha numeric machines
            if (_machineType == MachineType.WPCAlphanumeric)
            {
                if (FlippersEnabled)
                    _coils[79].Pulse(0);
                else
                    _coils[79].Disable();
            }
            else if (_machineType == MachineType.SternWhitestar || _machineType == MachineType.SternSAM)
            {
                foreach (string flipper in _config.PRFlippers)
                {
                    //get the coil and switch number matched to the switch name
                    IDriver main_coil = _coils[flipper + "Main"];
                    ushort switch_num = PinProc.PRDecode(_machineType, _switches[flipper].Number.ToString());

                    //create switch rules for coils
                    //Add drivers if the flippers are enabled to fire the coil
                    DriverState[] drivers = new DriverState[1];
                    if (FlippersEnabled)
                        drivers[0] = _proc.DriverStatePatter(main_coil.State, 2, 18, pulseTime);
                    else
                        drivers = null;

                    //map the drivers to the switch number so to fire when active without scripting it
                    _proc.SwitchUpdateRule(switch_num,
                        EventType.SwitchClosedNondebounced,
                        new SwitchRule { NotifyHost = false, ReloadActive = false },
                        drivers,
                        false
                    );

                    //Add drivers if the flippers are enabled to disable the coil
                    drivers = new DriverState[1];
                    if (FlippersEnabled)
                        drivers[0] = _proc.DriverStateDisable(main_coil.State);
                    else
                        drivers = null;

                    //map the drivers to the switch number so to fire when active without scripting it
                    _proc.SwitchUpdateRule(switch_num,
                        EventType.SwitchOpenNondebounced,
                        new SwitchRule { NotifyHost = false, ReloadActive = false },
                        drivers,
                        false
                    );

                    if (!FlippersEnabled)
                        main_coil.Disable();
                }
            }

            foreach (string bumper in _config.PRBumpers)
            {
                ushort switch_num = _switches[bumper].Number;
                IDriver coil = _coils[bumper];

                DriverState[] drivers = new DriverState[1];
                if (FlippersEnabled)
                    drivers[0] = _proc.DriverStatePulse(coil.State, coil.PulseTime);
                else
                    drivers = null;

                _proc.SwitchUpdateRule(switch_num,
                    EventType.SwitchClosedNondebounced,
                    new SwitchRule { NotifyHost = false, ReloadActive = true },
                    drivers,
                    false
                );
            }
        }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, IDriver> GI
        {
            get { return _gi; }
            set { _gi = value; }
        }

        /// <inheritdoc/>
        public ILampController LampController { get; set; }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, IDriver> Lamps
        {
            get { return _lamps; }
            set { _lamps = value; }
        }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, LED> LEDS
        {
            get { return _leds; }
            set { _leds = value; }
        }

        /// <inheritdoc/>
        public ILogger Logger { get; set; }

        /// <inheritdoc/>
        public IModeQueue Modes
        {
            get { return _modes; }
            set { _modes = value; }
        }

        /// <inheritdoc/>
        public List<IPlayer> Players
        {
            get { return _players; }
            set { _players = value; }
        }

        /// <inheritdoc/>
        public IProcDevice PROC => _proc;

        /// <inheritdoc/>
        public AttrCollection<ushort, string, Switch> Switches
        {
            get { return _switches; }
            set { _switches = value; }
        }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, PdStepper> Steppers
        {
            get { return _steppers; }
            set { _steppers = value; }
        }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, PdServo> Servos
        {
            get { return _servos; }
            set { _servos = value; }
        }

        /// <inheritdoc/>
        public AttrCollection<ushort, string, PdSerialLed> SerialLeds
        {
            get { return _serialLeds; }
            set { _serialLeds = value; }
        }

        /// <inheritdoc/>
        public virtual IPlayer AddPlayer()
        {
            IPlayer newPlayer = CreatePlayer("Player " + (_players.Count + 1).ToString());
            _players.Add(newPlayer);
            return newPlayer;
        }

        /// <inheritdoc/>
        public virtual void AddPoints(long points)
        {
            var cp = CurrentPlayer();
            if (cp != null) cp.Score += points;
        }

        /// <summary>
        /// <inheritdoc/>. Classes should override this. Games can have different bonuses for players so deal with elsewhere.
        /// </summary>
        public virtual void AddBonus(long bonus) { }

        /// <inheritdoc/>
        public virtual void BallEnded() { }
        /// <inheritdoc/>
        public virtual void BallStarting() => this.SaveBallStartTime();

        /// <inheritdoc/>
        public virtual IPlayer CreatePlayer(string name) => new Player(name);

        /// <inheritdoc/>
        public IPlayer CurrentPlayer()
        {
            if (this._players.Count > this.CurrentPlayerIndex)
                return this._players[this.CurrentPlayerIndex];
            else
                return null;
        }

        /// <inheritdoc/>
        public virtual void DmdEvent() { }

        /// <summary>
        /// Ends the ball. Sets <see cref="BallEndTime"/> and the current players game time. <para/>
        /// Calls <see cref="BallEnded"/> and returns if a player has extra balls, see <see cref="ShootAgain"/>. <para/>
        /// Checks if this is the end of game or start of a new ball. see <see cref="EndGame"/> and <see cref="StartBall"/>
        /// </summary>
        public void EndBall()
        {
            BallEndTime = Time.GetTime();
            CurrentPlayer().GameTime += GetBallTime();
            BallEnded();
            Logger.Log(nameof(GameController) + ":" + nameof(EndBall) + ": next ball: " + Ball, LogLevel.Debug);

            //shoot again extra ball
            if (CurrentPlayer().ExtraBalls > 0)
            {
                Logger.Log(nameof(GameController) + ":"+nameof(EndBall)+": player extra ball, shoot again", LogLevel.Debug);
                CurrentPlayer().ExtraBalls -= 1;
                ShootAgain();
                return;
            }

            //increment the ball count
            if (CurrentPlayerIndex + 1 == _players.Count)
            {
                Ball += 1;
                CurrentPlayerIndex = 0;
                Logger.Log(nameof(GameController) + ":" + nameof(EndBall)+": next ball: " + Ball, LogLevel.Debug);
            }
            else CurrentPlayerIndex += 1;

            //end game or start new ball
            if (Ball > BallsPerGame) EndGame();
            else StartBall();
        }

        /// <inheritdoc/>
        public void EndGame()
        {
            Logger.Log(nameof(GameController) + ":" + nameof(EndGame), LogLevel.Debug);
            GameEnded();
            Ball = 0;
        }

        /// <inheritdoc/>
        public void EndRunLoop() => _gameLoopCancelToken?.Cancel();

        /// <inheritdoc/>        
        public virtual void GameEnded() { }

        /// <inheritdoc/>     
        public virtual void GameStarted()
        {
            Ball = 1;
            _players = new List<IPlayer>();
            CurrentPlayerIndex = 0;
        }

        /// <inheritdoc/>     
        public double GetBallTime() => this.BallEndTime - this.BallStartTime;
        /// <inheritdoc/>
        public virtual Event[] GetEvents(bool dmdEvents = true) => _proc.Getevents(dmdEvents);
        /// <inheritdoc/>
        public double GetGameTime(int player) => _players[player].GameTime;
        /// <inheritdoc/>
        public void LinkFlipperSwitch(string switch_name, string[] linked_coils, byte pulseMain = 34)
        {
            
        }
        /// <summary>
        /// Create a new machine configuration representation in memory from a json file on disk. and invokes <see cref="LoadConfig(MachineConfiguration)"/>
        /// </summary>
        /// <param name="PathToFile"></param>
        public void LoadConfig(string PathToFile)
        {
            var config = MachineConfiguration.FromFile(PathToFile);
            LoadConfig(config);
        }

        /// <summary>
        /// Sets up the machine from a MachineConfiguration. Uses <see cref="IProcDevice.SetupProcMachine"/> <para/>
        /// SetUp machine items, this can be called by Fake and Normal P-ROC
        /// </summary>
        /// <param name="config"></param>
        public void LoadConfig(MachineConfiguration config)
        {
            _config = config;
            if (config.PRGame.NumBalls <= 0) throw new NullReferenceException("Number of balls in machine configuration is set to zero");

            _num_balls_total = config.PRGame.NumBalls;
            
            Logger?.Log(nameof(GameController) + " setting up P-ROC machine from config.", LogLevel.Info);
            if (PROC != null) PROC.SetupProcMachine(config, _coils, _switches, _lamps, _leds, _gi, _steppers, _servos, _serialLeds);
        }

        /// <inheritdoc/>
        public void ProcessEvent(Event evt)
        {
            if (evt.Type == EventType.None) { }
            // Invalid event type, end run loop perhaps
            else if (evt.Type == EventType.Invalid) { }
            // DMD events
            else if (evt.Type == EventType.DMDFrameDisplayed) { this.DmdEvent(); }
            //switch events
            else
            {
                Switch sw = _switches[(ushort)evt.Value];
                bool recvd_state = evt.Type == EventType.SwitchClosedDebounced;

                Logger.Log(nameof(ProcessEvent) + ":" + evt.ToString(), LogLevel.Verbose);

                if (!sw.IsState(recvd_state))
                {
                    sw.SetState(recvd_state);
                    _modes.HandleEvent(evt);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Reset()
        {
            Logger.Log(nameof(Reset) + ": clearing layers modes and players", LogLevel.Debug);
            Ball = 0;
            _old_players.Clear();
            _old_players.AddRange(_players);
            _players.Clear();
            CurrentPlayerIndex = 0;
            _modes.Clear();
        }

        /// <summary>
        /// Main run loop of the program. Performs the following logic until the loop ends: <para/>
        ///     - Get events from PROC<para/>
        ///     - Process this list of events across all modes<para/>
        ///     - 'Tick' modes<para/>
        ///     - Tickle watchdog<para/>
        /// </summary>
        /// <param name="delay">Thread delay, less CPU, default 0</param>
        /// <param name="cancellationToken">The cancellation token to end the loop from <see cref="EndRunLoop"/></param>
        public virtual void RunLoop(byte delay = 0, CancellationTokenSource cancellationToken = default)
        {
            if (cancellationToken == null)
                _gameLoopCancelToken = new CancellationTokenSource();
            else
                _gameLoopCancelToken = cancellationToken;

            long loops = 0;
            DmdEvent();
            Event[] events;

            try
            {
                while (!_gameLoopCancelToken.IsCancellationRequested)
                {
                    loops++;
                    events = GetEvents();
                    if (events != null)
                    {
                        foreach (Event evt in events)
                        {
                            ProcessEvent(evt);
                        }
                    }

                    this.Tick();
                    TickVirtualDrivers();
                    this._modes.Tick();

                    // Do we have any events waiting such as pulses from the UI
                    lock (_coil_lock_object)
                    {
                        SafeCoilDrive c;
                        for (int i = 0; i < _safe_coil_drive_queue.Count; i++)
                        {
                            c = _safe_coil_drive_queue[i];
                            if (c.pulse)
                                Coils[c.coil_name].Pulse((byte)c.pulse_time);
                            if (c.disable)
                                Coils[c.coil_name].Disable();
                        }
                        _safe_coil_drive_queue.Clear();
                    }

                    if (_proc != null)
                        _proc.WatchDogTickle();

                    if (delay > 0)
                        Thread.Sleep(delay);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("RUN LOOP EXCEPTION: " + ex.ToString(), LogLevel.Error);
            }
            finally
            {
                Logger?.Log("Run loop ended", LogLevel.Info);
                if (loops != 0)
                {
                    double dt = Time.GetTime() - BootTime;
                }
                _proc.Close();
            }
        }

        /// <inheritdoc/>
        public void SafeDisableCoil(string coilName)
        {
            SafeCoilDrive d = new SafeCoilDrive();
            d.coil_name = coilName;
            d.disable = true;
            lock (_coil_lock_object)
            {
                _safe_coil_drive_queue.Add(d);
            }
        }

        /// <inheritdoc/>
        public void SafeDriveCoil(string coilName, ushort pulse_time = 30)
        {
            SafeCoilDrive d = new SafeCoilDrive();
            d.coil_name = coilName;
            d.pulse = true;
            d.pulse_time = pulse_time;
            lock (_coil_lock_object)
            {
                _safe_coil_drive_queue.Add(d);
            }
        }

        /// <inheritdoc/>
        public void SaveBallStartTime() => this.BallStartTime = Time.GetTime();
        /// <inheritdoc/>
        public virtual void ShootAgain() => this.BallStarting();

        /// <summary>
        /// Calls see <see cref="BallStarting"/>
        /// </summary>
        public virtual void StartBall() => this.BallStarting();

        /// <inheritdoc/>
        public virtual void StartGame() => this.GameStarted();

        /// <inheritdoc/>
        public virtual void Tick() { }

        /// <inheritdoc/>
        public virtual void TickVirtualDrivers()
        {
            foreach (Driver coil in _coils.Values)
                coil.Tick();

            foreach (Driver lamp in _lamps.Values)
                lamp.Tick();

            foreach (LED led in _leds.Values)
                led.Tick();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void UpdateLamps() { }

        /// <summary>
        /// Log the specified text to the given logger. If no logger is set up, log to the trace output
        /// </summary>
        /// <param name="text"></param>
        protected void Log(string text)
        {
            if (this.Logger != null)
                this.Logger.Log(text);
            else
                System.Diagnostics.Trace.WriteLine(text);
        }
    }
}
