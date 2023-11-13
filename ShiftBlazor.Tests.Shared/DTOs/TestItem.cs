using Bogus;
using FluentValidation;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftBlazor.Tests.Shared.DTOs
{
    public class TestItem : ShiftEntityDTO
    {
        public override string? ID { get; set; }
        public string StringType { get; set; }
        public int IntType { get; set; }
        public TestItemEnum EnumType { get; set; }
        public DateTime DateTimeType { get; set; }
        public bool BoolType { get; set; }
        public Guid GuidType { get; set; }
        public DateTimeOffset DateTimeOffsetType { get; set; }

        public static List<TestItem> GenerateData(int min = 50, int max = 500, bool includeDeleted = true)
        {
            var userGenerator = new Faker<TestItem>()
                .RuleFor(u => u.ID, (f, u) => f.Random.Replace("####"))
                .RuleFor(u => u.StringType, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.IntType, (f, u) => f.Random.Int())
                .RuleFor(u => u.EnumType, (f, u) => f.Random.Enum<TestItemEnum>())
                .RuleFor(u => u.DateTimeType, (f, u) => f.Date.Soon())
                .RuleFor(u => u.BoolType, (f, u) => f.Random.Bool())
                .RuleFor(u => u.GuidType, (f, u) => f.Random.Guid())
                .RuleFor(u => u.DateTimeOffsetType, (f, u) => f.Date.SoonOffset())
                .RuleFor(u => u.IsDeleted, (f, u) => f.Random.Bool(includeDeleted ? 0.1f : 0f));

            return userGenerator.GenerateBetween(min, max);
        }
    }

    public enum TestItemEnum
    {
        enum1,
        enum2,
        enum3,
    }
}
