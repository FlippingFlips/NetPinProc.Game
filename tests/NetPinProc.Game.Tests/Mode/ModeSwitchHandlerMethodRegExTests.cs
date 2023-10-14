using NetPinProc.Domain;
using NetPinProc.Game;
using NetProc.Game;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NetProc.Domain.Tests
{
    public class ModeSwitchHandlerMethodRegExTests
    {
        const string snakeCaseRegExPattern 
            = "sw_(?<name>[a-zA-Z0-9]+)_(?<state>open|closed|active|inactive)(?<after>_for_(?<time>[0-9]+)(?<units>ms|s))?";

        const string pascalCaseRegExPattern
            = "Sw(?<name>[a-zA-Z0-9]+)(?<state>Open|Closed|Active|Inactive)(?<after>For(?<time>[0-9]+)(?<units>ms|s))?";

        [Theory]
        [InlineData("sw_shooterLane_active", 3)]
        [InlineData("sw_shooterLane_inactive", 3)]
        [InlineData("sw_shooterLane_open", 3)]
        [InlineData("sw_shooterLane_closed", 3)]        
        [InlineData("sw_shooterLane_active_for_2s", 6)]
        [InlineData("sw_shooterLane_inactive_for_2000ms", 6)]
        [InlineData("sw_shooterLane_closed_for_2000ms", 6)]
        //[InlineData("sw_shooter_lane_closed_for_2000ms", 6)] Fail snake case switch name. shooter_lane
        public void SwitchHandlerMethodRegEx_Tests(string methodName, int expected)
        {
            Regex pattern = new Regex(snakeCaseRegExPattern);
            MatchCollection matches = pattern.Matches(methodName);

            int total = 0;
            foreach (Match match in matches)
            {                
                foreach (Group group in match.Groups)
                {
                    if (group.Success)
                        total++;
                }
            }

            Assert.True(total == expected);
        }

        [Theory]
        [InlineData("SwShooterLaneActive", 3)]
        [InlineData("SwShooterLaneInactive", 3)]
        [InlineData("SwShooterLaneOpen", 3)]
        [InlineData("SwShooterLaneClosed", 3)]
        [InlineData("SwShooterLaneActiveFor2s", 6)]
        [InlineData("SwShooterLaneInactiveFor2000ms", 6)]
        [InlineData("SwShooterLaneClosedFor2000ms", 6)]        
        public void SwitchHandlerMethodRegExPascalCased_Tests(string methodName, int expected)
        {
            Regex pattern = new Regex(pascalCaseRegExPattern);
            MatchCollection matches = pattern.Matches(methodName);

            int total = 0;
            foreach (Match match in matches)
            {
                foreach (Group group in match.Groups)
                {
                    if (group.Success)
                        total++;
                }
            }

            Assert.True(total == expected);
        }

        [Fact]
        public void ScanAllSwitchHandler_Tests()
        {
            var assembly = Assembly.GetAssembly(typeof(GameController));
            var types = assembly?.GetTypes()?.Where(x => x.BaseType == typeof(Mode));
            var results = new List<string>();
            if (types?.Count() > 0) 
            {
                foreach (var type in types)
                {
                    results.Add("Searching " + type.Name);
                    results.AddRange(ScanSwitchHandlers(type));
                }
            }            
        }

        /// <summary>
        /// Scan all statically defined switch handlers in mode classes and wire up handling events. <para/>
        /// open|closed|active|inactive
        /// </summary>
        public string[] ScanSwitchHandlers(Type type)
        {
            // Get all methods in the mode class that match a certain regular expression
            MethodInfo[] methods = type.GetMethods();
            string regexPattern = "sw_(?<name>[a-zA-Z0-9]+)_(?<state>open|closed|active|inactive)(?<after>_for_(?<time>[0-9]+)(?<units>ms|s))?";
            Regex pattern = new Regex(regexPattern);
            var list = new List<string>();
            foreach (MethodInfo m in methods)
            {
                MatchCollection matches = pattern.Matches(m.Name);
                string switchName = "";
                string switchState = "";
                bool hasTimeSpec = false;
                double switchTime = 0;
                string switchUnits = "";
                foreach (Match match in matches)
                {
                    int i = 0;
                    foreach (Group group in match.Groups)
                    {
                        if (group.Success == true)
                        {
                            string gName = pattern.GroupNameFromNumber(i)?.ToLower() ?? string.Empty;
                            string gValue = group.Value;
                            if (gName == "name")
                            {
                                switchName = gValue;
                            }
                            if (gName == "state")
                                switchState = gValue;

                            if (gName == "after")
                                hasTimeSpec = true;

                            if (gName == "time")
                                switchTime = Int32.Parse(gValue);

                            if (gName == "units")
                                switchUnits = gValue;

                        }
                        i++;
                    }
                }
                if (switchName != "" && switchState != "")
                {
                    if (hasTimeSpec && switchUnits == "ms")
                        switchTime = switchTime / 1000.0;


                    list.Add($"{m.Name}");
                    //SwitchAcceptedHandler swh = (SwitchAcceptedHandler)Delegate.CreateDelegate(typeof(SwitchAcceptedHandler), this, m);
                    //AddSwitchHandler(switchName, switchState, switchTime, swh); //add handler
                }
            }

            return list.ToArray();
        }
    }
}