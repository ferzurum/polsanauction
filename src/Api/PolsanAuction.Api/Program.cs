var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

/// <summary>Entry point; public so integration tests can host the app (WebApplicationFactory).</summary>
public partial class Program;
