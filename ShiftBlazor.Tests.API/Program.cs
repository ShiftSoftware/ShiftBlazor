using Bogus;
using Microsoft.AspNetCore.Mvc;
using ShiftBlazor.Tests.Viewer.Models;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

var app = builder.Build();

var usersList = User.GenerateData();

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/Users", ([FromQuery(Name = "$top")] int? top,
                          [FromQuery(Name = "$skip")] int? skip,
                          [FromQuery(Name = "$count")] bool? count,
                          [FromQuery(Name = "$filter")] string? filter) =>
{
    var value = usersList.AsEnumerable();
    var itemCount = usersList.Count;

    switch (filter)
    {
        case "IsDeleted eq true":
            value = value.Where(x => x.IsDeleted == true);
            break;
        case "IsDeleted eq true or IsDeleted eq false":
            break;
        default:
            value = value.Where(x => x.IsDeleted == false);
            break;
    }

    if (filter?.Contains("endswith(Email,'yahoo.com')") == true)
    {
        value = value.Where(x => x.Email.EndsWith("yahoo.com"));
    }

    itemCount = value.Count();
    if (skip != null) value = value.Skip(skip.Value);
    if (top != null) value = value.Take(top.Value);

    return new ODataDTO<User>
    {
        Count = itemCount,
        Value = value.ToList(),
    };
});

app.MapGet("/api/User/{id}", (string? id) =>
{
    User? user = null;
    if (!string.IsNullOrWhiteSpace(id))
    {
        user = usersList.Find(x => x.ID == id) ?? usersList.First();
    }
    return new ShiftEntityResponse<User> { Entity = user};
});

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());
app.Run();
