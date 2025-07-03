using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// ───── services ─────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(p =>
    p.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ───── GET /api/doctors ─────
app.MapGet("/api/doctors", async (IConfiguration cfg) =>
{
    var list = new List<DoctorDto>();
    var cs = cfg.GetConnectionString("MySql");

    await using var con = new MySqlConnection(cs);
    await con.OpenAsync();

    const string sql = """
        SELECT id,name,specialty,slot,rating,photo,degree,experience,reviews,
               availability,patients_treated,hours_per_week,surgeries
        FROM doctors;
        """;

    await using var cmd = new MySqlCommand(sql, con);
    await using var rd = await cmd.ExecuteReaderAsync();

    while (await rd.ReadAsync())
        list.Add(new DoctorDto(
            rd.GetInt32(0),                      // id
            rd.GetString(1),                     // name
            rd.GetString(2),                     // specialty
            rd.GetString(3),                     // slot
            rd.GetDecimal(4).ToString("0.0"),    // rating
            rd.GetString(5), rd.GetString(6),
            rd.GetString(7), rd.GetInt32(8).ToString(),
            rd.GetString(9), rd.GetString(10),
            rd.GetString(11), rd.GetString(12)
        ));

    return Results.Ok(list);
});

app.Urls.Add("http://0.0.0.0:5167");
app.Run();

// DTO shape must match the MAUI record
record DoctorDto(int Id, string Name, string Specialty, string Slot,
                 string Rating, string Photo, string Degree, string Experience,
                 string Reviews, string Availability, string PatientsTreated,
                 string HoursPerWeek, string Surgeries);
