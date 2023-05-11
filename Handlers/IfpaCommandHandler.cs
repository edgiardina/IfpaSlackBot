using ConsoleTables;
using PinballApi;
using SlackNet.Interaction;
using SlackNet.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfpaSlackBot.Handlers
{
    public class IfpaCommandHandler : ISlashCommandHandler
    {
        public PinballRankingApiV2 IFPAApi { get; set; }
        public PinballRankingApiV1 IFPALegacyApi { get; set; }

        public const string SlashCommand = "/ifpa";

        public IfpaCommandHandler(PinballRankingApiV2 pinballRankingApi, PinballRankingApiV1 apiV1)
        {
            IFPAApi = pinballRankingApi;
            IFPALegacyApi = apiV1;  
        }

        public async Task<SlashCommandResponse> Handle(SlashCommand command)
        {
            Console.WriteLine($"{command.UserName} used the {SlashCommand} slash command in the {command.ChannelName} channel");


            var table = new ConsoleTable("Rank", "Player", "Points");
            var index = 1;

            var rankings = await IFPAApi.GetWpprRanking(1, 40);

            foreach (var ranking in rankings.Rankings)
            {
                table.AddRow(index,
                             ranking.FirstName + " " + ranking.LastName,
                             ranking.WpprPoints.ToString("N2"));
                index++;
            }

            var responseTable = $"```{table.ToMinimalString()}```";

            return new SlashCommandResponse
            {
                Message = new Message
                {
                    Text = responseTable
                }
            };
        }
    }
}
