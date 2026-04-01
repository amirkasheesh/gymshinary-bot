// 1. Создаем нашего настройщика
using Gymshinary.Bot.Configuration;
using Gymshinary.Bot.Services;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);


// 2. Настраиваем наше приложение для запуска, регистрируя сервисы
builder.Services.Configure<BotConfiguration>(builder.Configuration.GetRequiredSection("BotConfiguration"));
builder.Services.AddSingleton<ITelegramBotClient>(serviceProvider =>
{
    var botConfig = serviceProvider.GetRequiredService<IOptions<BotConfiguration>>().Value;
    return new TelegramBotClient(botConfig.BotToken);
});

builder.Services.AddHostedService<TelegramBotService>();




// 3. Запуск
builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.Run();

