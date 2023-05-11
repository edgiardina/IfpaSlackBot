// See https://aka.ms/new-console-template for more information
using IfpaSlackBot.Config;
using IfpaSlackBot.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using PinballApi;
using SlackNet.Events;
using SlackNet.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

var settings = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build()
    .Get<AppSettings>();

var services = new ServiceCollection();
services.AddSingleton(settings);
services.AddScoped<PinballRankingApiV2>(x => new PinballRankingApiV2(settings.PinballApi.IFPAApiKey));
services.AddScoped<PinballRankingApiV1>(x => new PinballRankingApiV1(settings.PinballApi.IFPAApiKey));

services.AddSlackNet(c => c
    .UseApiToken(settings.ApiToken) // This gets used by the API client
    .UseAppLevelToken(settings.AppLevelToken)
    //.RegisterEventHandler<MessageEvent, PingHandler>());
    // Echo demo - a slash command for telling you what you already know
    .RegisterSlashCommandHandler<IfpaCommandHandler>(IfpaCommandHandler.SlashCommand));



var provider = services.BuildServiceProvider();

Console.WriteLine("Connecting...");
var client = provider.SlackServices().GetSocketModeClient();
await client.Connect();
Console.WriteLine("Connected. Press any key to exit...");
CancellationToken cancellationToken = default;

while (true)
{
    await Task.Delay(int.MaxValue, cancellationToken);
}