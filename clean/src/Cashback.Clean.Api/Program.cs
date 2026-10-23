using Cashback.Clean.Api.Filters;
using Cashback.Clean.Application;
using Cashback.Clean.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ExceptionFilter>())
    // 404 sem corpo, como diz o contrato (contratos/compras-v1.yaml)
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
// o painel roda em outra porta; "expor" os headers deixa a aba Loja parceira ler a resposta inteira
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseCors();

// Qual versão respondeu (aula 3): no canário, duas versões ficam no ar ao mesmo tempo, e o painel mostra quem atendeu.
// O nome vem da configuração "Versao" (o canario.sh passa "estavel" e "canario"); rodando sozinha, "local".
var versao = app.Configuration["Versao"] ?? "local";
app.Use((context, next) =>
{
    context.Response.Headers["X-Versao"] = versao;
    return next(context);
});

app.MapControllers();
app.Run();
