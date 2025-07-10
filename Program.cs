using MySqlConnector;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ───── services ─────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(p =>
    p.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ───── PORT handling (Render or local) ─────
var port = Environment.GetEnvironmentVariable("PORT") ?? "5167";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ───── GET  /api/doctors ─────
app.MapGet("/api/doctors", async (IConfiguration cfg, ILogger<Program> log) =>
{
    try
    {
        var list = new List<DoctorDto>();
        var cs = cfg.GetConnectionString("MySql");

        // log the CS once so you can verify which DB it’s hitting
        log.LogInformation("CS = {cs}", cs);

        await using var con = new MySqlConnection(cs);
        await con.OpenAsync();

        const string sql = @"
                    SELECT id,name,specialty,slot,rating,photo,degree,experience,reviews,
                    availability,patients_treated,hours_per_week,surgeries,phone
                    FROM doctors;
        ";

        await using var cmd = new MySqlCommand(sql, con);
        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new DoctorDto(
                rd.GetInt32(0),
                rd.GetString(1),
                rd.GetString(2),
                rd.GetString(3),
                rd.GetDecimal(4).ToString("0.0"),
                rd.GetString(5),
                rd.GetString(6),
                rd.GetString(7),
                rd.GetInt32(8).ToString(),
                rd.GetString(9),
                rd.GetString(10),
                rd.GetString(11),
                rd.GetString(12),
                rd.GetString(13)
            ));
        }

        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "MySQL error");
        return Results.Problem("Database connection failed: " + ex.Message);
    }
});

// ───── GET  /api/ping  (debug helper) ─────
app.MapGet("/api/ping", (IConfiguration cfg) =>
{
    var cs = cfg.GetConnectionString("MySql");
    return Results.Ok(new { ConnectionString = cs });
});

app.Run();

// DTO shape must match the MAUI record
record DoctorDto(int Id, string Name, string Specialty, string Slot,
                 string Rating, string Photo, string Degree, string Experience,
                 string Reviews, string Availability, string PatientsTreated,
                 string HoursPerWeek, string Surgeries, string Phone);
