// 1. Создаем нашего настройщика
var builder = WebApplication.CreateBuilder(args);


// 2. Настраиваем наше приложение для запуска, регистрируя сервисы







// 3. Запуск
builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.Run();

