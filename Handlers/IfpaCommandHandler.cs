using ConsoleTables;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;
using PinballApi.Models.WPPR.v2.Rankings;
using SlackNet.Blocks;
using SlackNet;
using SlackNet.Interaction;
using SlackNet.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IfpaSlackBot.BlockBuilders;

namespace IfpaSlackBot.Handlers
{
    public class IfpaCommandHandler : ISlashCommandHandler
    {
        public PinballRankingApiV2 IFPAApi { get; set; }
        public PinballRankingApiV1 IFPALegacyApi { get; set; }

        public const string SlashCommand = "/ifpa";

        public List<string> Commands => new List<string> { "help", "rank", "series", "player", "tournaments" };

        public enum RankType
        {
            Main,
            Women,
            Youth,
            Country
        }

        public IfpaCommandHandler(PinballRankingApiV2 pinballRankingApi, PinballRankingApiV1 apiV1)
        {
            IFPAApi = pinballRankingApi;
            IFPALegacyApi = apiV1;
        }

        public async Task<SlashCommandResponse> Handle(SlashCommand command)
        {
            Console.WriteLine($"{command.UserName} used the {SlashCommand} slash command in the {command.ChannelName} channel");

            var tokens = command.Text.ToLower().Split(' ');
            var commandToken = tokens?.FirstOrDefault();
            var commandDetailsTokens = tokens?.Skip(1).ToArray();

            if (tokens == null || tokens.Any() == false || Commands.Contains(commandToken) == false)
            {
                // input string does not start with a valid command
                return new SlashCommandResponse
                {
                    Message = new Message
                    {
                        Text = $"No valid commands found in `{command.Text}`."
                    },
                    ResponseType = ResponseType.Ephemeral
                };
            }

            switch (commandToken)
            {
                case "rank":
                    return await Rank(commandDetailsTokens);
                case "player":
                    return await Player(commandDetailsTokens);
                case "help":
                default:
                    return await Help();
            }
        }

        private async Task<SlashCommandResponse> Player(string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return new SlashCommandResponse
                {
                    Message = new Message
                    {
                        Text = $"You must provide a player id or player name to search."
                    },
                    ResponseType = ResponseType.Ephemeral
                };
            }

            Player playerDetails;

            if (int.TryParse(tokens[0], out var playerId) == true)
            {
                playerDetails = await IFPAApi.GetPlayer(playerId);
            }
            else
            {
                var name = string.Join(' ', tokens);
                var players = await IFPAApi.GetPlayersBySearch(new PlayerSearchFilter { Name = name });
                if (players.Results.Count > 0)
                {
                    playerDetails = await IFPAApi.GetPlayer(players.Results.First().PlayerId);
                }
                else
                {
                    return new SlashCommandResponse
                    {
                        Message = new Message
                        {
                            Text = $"Search Criteria did not find any players."
                        },
                        ResponseType = ResponseType.Ephemeral
                    };
                }
            }

            var playerTourneyResults = await IFPAApi.GetPlayerResults(playerDetails.PlayerId);

            return new SlashCommandResponse
            {
                Message = new Message
                {
                    Blocks = PlayerBlockBuilder.FromPlayer(playerDetails, playerTourneyResults)
                },
                ResponseType = ResponseType.InChannel
            };
        }

        private async Task<SlashCommandResponse> Rank(string[] tokens)
        {

            Enum.TryParse(tokens.FirstOrDefault(), true, out RankType rankType);

            var table = new ConsoleTable("Rank", "Player", "Points");
            var index = 1;

            if (rankType == RankType.Main)
            {
                var rankings = await IFPAApi.GetWpprRanking(1, 40);

                foreach (var ranking in rankings.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }
            else if (rankType == RankType.Women)
            {
                WomensRanking womensRanking = await IFPAApi.GetRankingForWomen(TournamentType.Open, 1, 40);
                foreach (var ranking in womensRanking.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }
            else if (rankType == RankType.Youth)
            {
                YouthRanking youthRanking = await IFPAApi.GetRankingForYouth(1, 40);
                foreach (var ranking in youthRanking.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }
            else if (rankType == RankType.Country && tokens.Length > 1)
            {
                var countryRankings = await IFPAApi.GetRankingForCountry(tokens[1], 1, 40);

                foreach (var ranking in countryRankings.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }

            var responseTable = table.ToMinimalString();
            responseTable = $"Top of the current IFPA {rankType} rankings\n```{responseTable.Substring(0, Math.Min(responseTable.Length, 1950))}```";

            return new SlashCommandResponse
            {
                Message = new Message
                {
                    Text = responseTable
                },
                ResponseType = ResponseType.InChannel
            };
        }

        private async Task<SlashCommandResponse> Help()
        {
            var helpText = @"The following commands are available:
```rank [ranktype] [optional:countryname]
series [series] [optional:year] [optional:region]
player [IFPA ID or Player Name]
tournaments [radiusInMiles] [location]
```";

            return new SlashCommandResponse
            {
                Message = new Message
                {
                    Text = helpText
                },
                ResponseType = ResponseType.InChannel
            };
        }


    }
}
