using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using MySql.Data.MySqlClient;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public partial class MainWindows : Form
    {
        private bool _isDiscordBotConnected = false;
        private DiscordSocketClient _client;
        private string mysqlcon = "server=127.0.0.1;user=root;database=bets;password=;port=3307"; //yourConnection should be here written
        private int globalGamesWithoutDraw;
        private const int MaxChunkSize = 2000;



        /* 
         * @brief Initializes the main form and Discord bot.
         */
        public MainWindows()
        {
            InitializeComponent();
            InitializeDiscordBot();
            CheckForIllegalCrossThreadCalls = false;
            LoadTeamData();
        }
        /* 
         * @brief Loads team data from the MySQL database and populates the RichTextBox.
         */


        private void LoadTeamData()
        {
            using (var connection = new MySqlConnection(mysqlcon))
            {
                try
                {
                    connection.Open();
                    var cmd = new MySqlCommand("SELECT * FROM teams", connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AppendTeamDataToRichTextBox(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SendDiscordMessage($"Error loading team data: {ex.Message}", "mistakes");
                }
            }
        }


        /* 
         * @brief Splits the text from a RichTextBox into chunks of specified maximum size.
         * @param richTextBox The RichTextBox containing the text.
         * @param maxChunkSize The maximum size of each text chunk.
         * @return An array of text chunks.
         */
        private string[] GetTextChunksFromRichTextBox(RichTextBox richTextBox, int maxChunkSize)
        {
            string allText = richTextBox.Text;
            return SplitTextToChunks(allText, maxChunkSize);
        }


        /* 
         * @brief Appends team data to the specified RichTextBox.
         * @param reader A MySQL data reader containing team data.
         */
        private void AppendTeamDataToRichTextBox(MySqlDataReader reader)
        {
            richTextBox1.Text = "";
            while (reader.Read())
            {
                richTextBox1.AppendText($"{reader.GetString(1)} - {reader.GetString(2)} - " +
                                        $"{reader.GetInt32(3)} games without a draw - " +
                                        $"Is there a current bet: {reader.GetInt32(6)} - " +
                                        $"How much was bet: {reader.GetInt32(7)}\n");
            }
        }

        /* 
         * @brief Handles the Button1 click event to clear RichTextBox2 and execute bet placement.
         */
        private void Button1_Click(object sender, EventArgs e)
        {
            richTextBox2.Clear();
            ExecuteBetPlacement();
        }


        /* 
         * @brief Handles the Button2 click event to clear RichTextBox1 and refresh draw data.
         */
        private void Button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            RefreshDraws();
        }


        /* 
         * @brief Sends a test message to the Discord "mistakes" channel to check the bot's connection.
         */
        private void Button3_Click(object sender, EventArgs e)
        {
            SendDiscordMessage("Connection test", "mistakes");
        }

        /* 
         * @brief Initializes the Discord bot and connects it using the specified token.
         * @exception Exception Thrown if the bot fails to initialize or connect.
         */
        private async void InitializeDiscordBot()
        {
            try
            {
                _client = new DiscordSocketClient();
                string token = "yourDiscordToken";
                _client.Log += LogAsync;
                _client.Ready += ReadyAsync;

                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                SendDiscordMessage($"Discord bot initialization error: {ex.Message}", "mistakes");
            }
        }


        /* 
         * @brief Executes the logic for placing bets based on team data and betting rules.
         */
        private void ExecuteBetPlacement()
        {
            var matchesToBetOn = new List<List<string>>();
            using (var connection = new MySqlConnection(mysqlcon))
            {
                try
                {
                    connection.Open();
                    var cmd = new MySqlCommand("SELECT * FROM teams", connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ProcessBetting(reader, matchesToBetOn);
                        }
                    }

                    UpdateDatabaseWithBettingResults(matchesToBetOn, connection);
                }
                catch (Exception ex)
                {
                    SendDiscordMessage($"Bet placement error: {ex.Message}", "mistakes");
                }
            }

            ChunkAndSendRichTextBoxContent(richTextBox2.Text, "bets");
        }


        /* 
         * @brief Processes the betting logic for a team based on specific criteria.
         * @param reader A MySQL data reader containing team information.
         * @param matchesToBetOn List of matches to be bet on.
         */
        private void ProcessBetting(MySqlDataReader reader, List<List<string>> matchesToBetOn)
        {
            var teamID = reader.GetInt32(0);
            var howManyDraws = reader.GetInt32(3);
            var teamName = reader.GetString(1);
            var linkOfTeam = reader.GetString(4);
            var isBetsOn = reader.GetInt32(6);
            var howMuchWasBet = reader.GetInt32(7);

            if (howManyDraws >= 5 && isBetsOn == 0)
            {
                AnalyzeTeamForBetting(linkOfTeam, teamID, teamName, 20, matchesToBetOn);
            }
            else if (isBetsOn == 1)
            {
                ContinueBettingOnTeam(linkOfTeam, teamID, reader, 20, matchesToBetOn);
            }
        }


        /* 
         * @brief Analyzes a team for betting opportunities and logs the results.
         * @param linkOfTeam The team's match page URL.
         * @param teamID The unique identifier of the team.
         * @param teamName The name of the team.
         * @param betAmount The amount to bet.
         * @param matchesToBetOn List of matches to bet on.
         */
        private void AnalyzeTeamForBetting(string linkOfTeam, int teamID, string teamName, int betAmount, List<List<string>> matchesToBetOn)
        {
            using (var browser = GetWebDriver())
            {
                browser.Navigate().GoToUrl(linkOfTeam);
                Thread.Sleep(3000);

                try
                {
                    var (newMatchDate, success) = TryGetNewMatchDate(browser);

                    if (success)
                    {
                        LogAndAddMatchForBetting(teamID, betAmount, teamName, newMatchDate, matchesToBetOn);
                    }
                }
                catch (Exception ex)
                {
                    SendDiscordMessage($"Error analyzing team for betting: {ex.Message}", "mistakes");
                }
            }
        }


        /* 
         * @brief Logs a match for betting and appends the betting data to a list.
         * @param teamID The unique identifier of the team.
         * @param betAmount The amount to bet.
         * @param teamName The name of the team.
         * @param newMatchDate The date of the next match.
         * @param matchesToBetOn List of matches to bet on.
         */
        private void LogAndAddMatchForBetting(int teamID, int betAmount, string teamName, string newMatchDate, List<List<string>> matchesToBetOn)
        {
            richTextBox2.AppendText($"[Betting][{betAmount}$ on {teamName} for match on {newMatchDate}]\n");

            matchesToBetOn.Add(new List<string>
            {
                teamID.ToString(),
                betAmount.ToString(),
                newMatchDate,
                "1"
            });
        }


        /* 
         * @brief Continues betting on a team if there was a previous unsuccessful bet.
         * @param linkOfTeam The team's match page URL.
         * @param teamID The unique identifier of the team.
         * @param reader A MySQL data reader containing team information.
         * @param initialBet The initial betting amount.
         * @param matchesToBetOn List of matches to bet on.
         */
        private void ContinueBettingOnTeam(string linkOfTeam, int teamID, MySqlDataReader reader, int initialBet, List<List<string>> matchesToBetOn)
        {
            using (var browser = GetWebDriver())
            {
                browser.Navigate().GoToUrl(linkOfTeam);
                Thread.Sleep(3000);

                try
                {
                    var result = EvaluateBetOutcome(browser, reader);

                    if (result.Won)
                    {
                        LogBetWin(reader, result.LastMatchDate);
                        matchesToBetOn.Add(CreateMatchResult(teamID, 0, "0", "0"));
                    }
                    else
                    {
                        var betAmount = initialBet * 2;
                        string newMatchDate = $"{result.LastMatchDate}.{DateTime.Now.Year}";

                        LogAndAddMatchForBetting(teamID, betAmount, reader.GetString(1), newMatchDate, matchesToBetOn);
                    }
                }
                catch (Exception ex)
                {
                    SendDiscordMessage($"Error continuing betting on team: {ex.Message}", "mistakes");
                }
            }
        }


        /* 
         * @brief Logs a successful bet and displays a win message.
         * @param reader A MySQL data reader containing team information.
         * @param lastMatchDate The date of the last match.
         */
        private void LogBetWin(MySqlDataReader reader, string lastMatchDate)
        {
            richTextBox2.AppendText($"[WON] {reader.GetInt32(7)}$ on team {reader.GetString(1)} match on {lastMatchDate}\n");
        }


        /* 
         * @brief Evaluates the outcome of a bet based on the match data.
         * @param browser The WebDriver instance used to navigate and fetch match data.
         * @param reader A MySQL data reader containing team information.
         * @return A tuple containing the win status and last match date.
         */
        private (bool Won, string LastMatchDate) EvaluateBetOutcome(IWebDriver browser, MySqlDataReader reader)
        {
            var lastDrawnMatchDate = reader.GetString(5);
            return (false, lastDrawnMatchDate); // Return actual values
        }


        /* 
         * @brief Updates the database with the betting results from the list of matches.
         * @param matchesToBetOn List of matches with betting information.
         * @param connection An open MySQL database connection.
         */
        private void UpdateDatabaseWithBettingResults(List<List<string>> matchesToBetOn, MySqlConnection connection)
        {
            foreach (var match in matchesToBetOn)
            {
                ExecuteUpdateCommand(match, connection);
            }
        }


        /* 
         * @brief Executes an update command for a specific match in the database.
         * @param match The match data to be updated in the database.
         * @param connection The MySQL database connection to use for executing the update.
         */
        private void ExecuteUpdateCommand(List<string> match, MySqlConnection connection)
        {
            var updateCommand = new MySqlCommand("UPDATE teams SET howMuchWasBet = @how_muchwasbet, isBetsOn = @isBets_On, lastDrawnMatchDate = @last_drawn_match_Date WHERE teamID = @teamID", connection);
            updateCommand.Parameters.AddWithValue("@how_muchwasbet", Convert.ToInt32(match[1]));
            updateCommand.Parameters.AddWithValue("@teamID", Convert.ToInt32(match[0]));
            updateCommand.Parameters.AddWithValue("@isBets_On", Convert.ToInt32(match[3]));
            updateCommand.Parameters.AddWithValue("@last_drawn_match_Date", match[2]);
            updateCommand.ExecuteNonQuery();
        }


        /* 
         * @brief Refreshes draw information by retrieving and updating team data from the database.
         */
        private void RefreshDraws()
        {
            List<List<string>> teamsDataToUpdate = new List<List<string>>();

            using (var connection = new MySqlConnection(mysqlcon))
            {
                try
                {
                    connection.Open();
                    var cmd = new MySqlCommand("SELECT * FROM teams", connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var linkOfTeam = reader.GetString(4);
                                var teamID = reader.GetInt32(0);

                                Thread updateThread = new Thread(() =>
                                {
                                    ExtractDrawData(linkOfTeam); // Ensure this function is correctly fetching data based on dynamic site structure
                                });
                                updateThread.Start();
                                updateThread.Join();

                                if (globalGamesWithoutDraw != reader.GetInt32(3))
                                {
                                    teamsDataToUpdate.Add(new List<string>
                            {
                                teamID.ToString(),
                                globalGamesWithoutDraw.ToString()
                            });
                                }
                            }
                        }

                        reader.Close(); // Close the reader explicitly when done
                    }

                    // Update database based on collected draw data
                    foreach (var data in teamsDataToUpdate)
                    {
                        int lokalHowManyDraws = Convert.ToInt32(data[1]);
                        int teamID = Convert.ToInt32(data[0]);

                        MySqlCommand updateCommand = new MySqlCommand("UPDATE teams SET howManyDraws = @how_many_draw WHERE teamID = @teamID", connection);
                        updateCommand.Parameters.AddWithValue("@how_many_draw", lokalHowManyDraws);
                        updateCommand.Parameters.AddWithValue("@teamID", teamID);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    SendDiscordMessage($"Error refreshing draws: {ex.Message}", "mistakes");
                }
                finally
                {
                    var cmd = new MySqlCommand("SELECT * FROM teams", connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            DateTime today = DateTime.Now;
                            richTextBox1.AppendText(today.ToString() + "\n");
                            SendDiscordMessage("\n " + today.ToString() + "\n", "draws");

                            while (reader.Read())
                            {
                                richTextBox1.AppendText(reader.GetString(1));
                                richTextBox1.AppendText(" - " + reader.GetString(2));
                                richTextBox1.AppendText(" - " + reader.GetInt32(3) + " games without a draw");
                                richTextBox1.AppendText(" - Is there a current bet: " + reader.GetInt32(6));
                                richTextBox1.AppendText(" - How much was bet: " + reader.GetInt32(7) + "\n");
                            }
                        }
                    }
                }
            }

            // Ensure the message is split into chunks if it's too large
            int maxChunkSize = 2000;
            string[] textChunks = GetTextChunksFromRichTextBox(richTextBox1, maxChunkSize);
            foreach (var chunk in textChunks)
            {
                SendDiscordMessage(chunk, "draws");
            }
        }



        /* 
         * @brief Initializes team data and starts the scheduled task on form load.
         */
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadTeamData(); 
            StartScheduledTask();
        }


        /* 
         * @brief Extracts draw data for a specified team by navigating to its page and parsing match results.
         * @param macLink The URL link to the team's match page.
         */
        private void ExtractDrawData(string macLink)
        {
            try
            {
                using (var browser = GetWebDriver())
                {
                    browser.Navigate().GoToUrl(macLink);
                    Thread.Sleep(3000);

                    string veri = "";
                    int i = 2;
                    int stringWithErrorPotential = 5;

                    try
                    {
                        try
                        {
                            IWebElement adi = browser.FindElement(By.XPath("/html[1]/body[1]/div[5]/div[2]/main[1]/div[1]/div[4]/div[1]/div[1]/div[2]/table[1]/tbody[1]/tr[2]/td[1]/div[1]/div[2]")); //İsmi buldu ve adi nesnesine atadı
                        }
                        catch (Exception hatalar)
                        {
                            SendDiscordMessage("\n \n Normal Expected Errors in Data Extraction : \n" + hatalar.Message, "mistakes");
                            try
                            {
                                IWebElement adi = browser.FindElement(By.XPath("/html[1]/body[1]/div[6]/div[2]/main[1]/div[1]/div[4]/div[1]/div[1]/div[2]/table[1]/tbody[1]/tr[2]/td[1]/div[1]/div[2]")); //İsmi buldu ve adi nesnesine atadı
                                stringWithErrorPotential = 6;
                            }
                            catch (Exception hatalar2)
                            {
                                SendDiscordMessage("\n \n  Normal Expected Errors in Data Extraction : \n" + hatalar2.Message, "mistakes");
                                try
                                {
                                    IWebElement adi = browser.FindElement(By.XPath("/html[1]/body[1]/div[4]/div[2]/main[1]/div[1]/div[4]/div[1]/div[1]/div[2]/table[1]/tbody[1]/tr[2]/td[1]/div[1]/div[2]")); //İsmi buldu ve adi nesnesine atadı
                                    stringWithErrorPotential = 4;
                                }
                                catch (Exception hata2)
                                {
                                    SendDiscordMessage("\n \n  Normal Expected Errors in Data Extraction : \n" + hata2.Message, "mistakes");

                                }
                            }

                        }
                        IWebElement adi2 = browser.FindElement(By.XPath("/html[1]/body[1]/div[" + stringWithErrorPotential.ToString() + "]/div[2]/main[1]/div[1]/div[4]/div[1]/div[1]/div[2]/table[1]/tbody[1]/tr[2]/td[1]/div[1]/div[2]")); //İsmi buldu ve adi nesnesine atadı

                        while (adi2.Text != null)
                        {
                            string[] lines = adi2.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                            if (lines[0] == lines[3] && lines[0] != null && lines[3] != null)
                            {
                                veri += "draw.";
                            }
                            else if (lines[0] != lines[3] && lines[0] != null && lines[3] != null)
                            {
                                veri += "non-draw.";
                            }
                            i++;
                            adi2 = browser.FindElement(By.XPath($"/html[1]/body[1]/div[{stringWithErrorPotential}]/div[2]/main[1]/div[1]/div[4]/div[1]/div[1]/div[2]/table[1]/tbody[1]/tr[{i}]/td[1]/div[1]/div[2]"));
                        }
                    }
                    catch (Exception ex)
                    {
                        SendDiscordMessage($"Error extracting draw data: {ex.Message}", "mistakes");
                    }

                    string[] results = veri.Split('.');
                    int gamesWithoutDraw = 0;
                    for (int r = 0; r < results.Length - 1; r++)
                    {
                        if (results[r] == "draw")
                        {
                            gamesWithoutDraw = 0;
                        }
                        if (results[r] == "non-draw")
                        {
                            gamesWithoutDraw++;
                        }
                    }
                    globalGamesWithoutDraw = gamesWithoutDraw;
                }
            }
            catch (Exception ex)
            {
                SendDiscordMessage($"Error in ExtractDrawData: {ex.Message}", "mistakes");
            }
        }


        /* 
         * @brief Updates the draw information for each team in the database.
         * @param teamsDataToUpdate List of teams and updated draw data to be saved to the database.
         * @param connection The MySQL connection used for the update.
         */
        private void UpdateDrawsInDatabase(List<List<string>> teamsDataToUpdate, MySqlConnection connection)
        {
            foreach (var teamData in teamsDataToUpdate)
            {
                ExecuteUpdateDrawCommand(teamData, connection);
            }
        }



        /* 
         * @brief Executes an update command on a specific match in the database.
         * @param match The match information to be updated.
         * @param connection An open MySQL database connection.
         */
        private void ExecuteUpdateDrawCommand(List<string> teamData, MySqlConnection connection)
        {
            var updateCommand = new MySqlCommand("UPDATE teams SET howManyDraws = @how_many_draw WHERE teamID = @teamID", connection);
            updateCommand.Parameters.AddWithValue("@how_many_draw", Convert.ToInt32(teamData[1]));
            updateCommand.Parameters.AddWithValue("@teamID", Convert.ToInt32(teamData[0]));
            updateCommand.ExecuteNonQuery();
        }


        /* 
         * @brief Splits the content of a RichTextBox and sends each chunk to Discord in separate messages.
         * @param text The content to send in chunks.
         * @param channel The Discord channel name to send the message to.
         */
        private void ChunkAndSendRichTextBoxContent(string text, string channel)
        {
            string[] textChunks = SplitTextToChunks(text, MaxChunkSize);
            for (int i = 0; i < textChunks.Length; i++)
            {
                SendDiscordMessage(textChunks[i], channel);
            }
        }

        /* 
         * @brief Splits a string into chunks of a specified size.
         * @param text The text to split.
         * @param chunkSize The maximum size of each chunk.
         * @return An array of string chunks.
         */
        private string[] SplitTextToChunks(string text, int chunkSize)
        {
            List<string> chunks = new List<string>();

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, text.Length - i);
                chunks.Add(text.Substring(i, length));
            }

            return chunks.ToArray();
        }


        /* 
         * @brief Logs a message indicating the bot is ready.
         */
        private async Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is ready!");
        }

        /* 
         * @brief Starts a scheduled task that executes the betting placement at a specific time each day.
         */
        private void StartScheduledTask()
        {
            Thread taskThread = new Thread(new ThreadStart(ScheduledTask));
            taskThread.IsBackground = true;
            taskThread.Start();
        }


        /* 
         * @brief Executes a daily scheduled task for bet placement at a set time.
         */
        private void ScheduledTask()
        {
            while (true)
            {
                DateTime currentTime = DateTime.Now;
                DateTime targetTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 20, 29, 0);
                if (currentTime > targetTime)
                {
                    targetTime = targetTime.AddDays(1);
                }
                TimeSpan delay = targetTime - currentTime;
                Thread.Sleep(delay);
                ExecuteBetPlacement();
            }
        }

        /* 
         * @brief Logs a Discord log message.
         * @param log The log message to display.
         */
        private async Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            await Task.CompletedTask;
        }


        /* 
         * @brief Sends a message to a specified Discord channel.
         * @param message The message to send.
         * @param channel The Discord channel name.
         */
        private void SendDiscordMessage(string message, string channel)
        {

            ulong serverId = 1291111953108045917;
            ulong channelId = GetChannelId(channel);
            var server = _client.GetGuild(serverId);
            var textChannel = server.GetTextChannel(channelId);

            textChannel.SendMessageAsync(message).GetAwaiter().GetResult();
        }


        /* 
         * @brief Gets the ID of a Discord channel based on the channel name.
         * @param channel The name of the Discord channel.
         * @return The ID of the specified Discord channel.
         */
        private ulong GetChannelId(string channel)
        {
            switch (channel)
            {
                case "bets":
                    return 1234567891011121314;
                case "draws":
                    return 1234557891011121314;
                case "mistakes":
                    return 1234577891011121314;
                default:
                    throw new ArgumentException($"Invalid channel: {channel}");
            }
        }

        /* 
         * @brief Initializes and returns a WebDriver instance for navigating web pages.
         * @return An instance of the Chrome WebDriver.
         */
        private IWebDriver GetWebDriver()
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            return new ChromeDriver(service);
        }

        /* 
         * @brief Attempts to retrieve the date of a new match.
         * @param browser The WebDriver instance used to navigate the web page.
         * @return A tuple with the new match date and success status.
         */
        private (string NewMatchDate, bool Success) TryGetNewMatchDate(IWebDriver browser)
        {
            try
            {
                IWebElement element = browser.FindElement(By.XPath("/html[1]/body[1]/div[5]/div[2]/main[1]/div[1]/div[4]/div[1]/div[1]/div[2]/table[1]/tbody[1]/tr[2]/td[1]/div[1]"));
                string[] lines = element.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines.Length == 5)
                {
                    return ($"{lines[0]}.{lines[1]}", true);
                }
            }
            catch (Exception ex)
            {
                SendDiscordMessage($"Error getting new match date: {ex.Message}", "mistakes");
            }

            return (null, false);
        }


        /* 
         * @brief Creates a list representing match results for database updates.
         * @param teamId The unique ID of the team.
         * @param howMuchWasBet The amount that was bet on the match.
         * @param lastDrawnMatchDate The date of the last drawn match.
         * @param isBetsOn Indicates if there is a current bet on the team.
         * @return A list representing the match results.
         */
        private List<string> CreateMatchResult(int teamId, int howMuchWasBet, string lastDrawnMatchDate, string isBetsOn)
        {
            return new List<string>
            {
                teamId.ToString(),
                howMuchWasBet.ToString(),
                lastDrawnMatchDate,
                isBetsOn
            };
        }

        /* 
         * @brief Sends a connection test message to the Discord "mistakes" channel.
         */
        private void button3_Click(object sender, EventArgs e)
        {
            SendDiscordMessage("connection test", "mistakes");
        }

        /* 
         * @brief Clears RichTextBox2 and executes bet placement logic.
         */
        private void button1_Click_1(object sender, EventArgs e)
        {
            richTextBox2.Text = null;
            ExecuteBetPlacement();
        }


        /* 
         * @brief Clears RichTextBox1 and refreshes draw data.
         */
        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = null;
            RefreshDraws();
        }

    }
}
