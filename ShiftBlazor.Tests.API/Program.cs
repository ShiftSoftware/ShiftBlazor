using Bogus;
using Bogus.Bson;
using Bogus.DataSets;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ShiftBlazor.Tests.Shared.DTOs;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

var app = builder.Build();

var usersList = User.GenerateData();
var revisionList = RevisionObj.GenerateData(12, 20);
var testItemList = TestItem.GenerateData(5, 10);

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/TestItem", () =>
{
    return new ODataDTO<TestItem>
    {
        Count = testItemList.Count,
        Value = testItemList,
    };
});

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

app.MapGet("/api/User/{id}/revisions", (string? id,
                                        [FromQuery(Name = "$top")] int? top,
                                        [FromQuery(Name = "$skip")] int? skip) =>
{
    var value = revisionList.AsEnumerable();
    var itemCount = revisionList.Count;

    if (skip != null) value = value.Skip(skip.Value);
    if (top != null) value = value.Take(top.Value);

    return new ODataDTO<RevisionObj>
    {
        Count = itemCount,
        Value = value.ToList(),
    };
});

app.MapGet("/odata/IdentityPublicUser", ([FromQuery(Name = "$filter")] string filter) =>
{
    var values = Regex.Matches(filter, "'([0-9]{4})'").Select(x => new SystemUser
    {
        ID = x.Groups[1].Value,
        Name = $"User {x.Groups[1].Value}",
    });

    return new ODataDTO<SystemUser>
    {
        Count = values.Count(),
        Value = values.ToList(),
    };
});

app.UseCors(x => x.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());
app.Run();
