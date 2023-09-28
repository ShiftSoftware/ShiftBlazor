﻿using Bogus;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftBlazor.Tests.Viewer.Models
{
    public class User : ShiftEntityDTO
    {
        public override string? ID { get; set; }
        [Required]
        public string Name { get; set; }
        public string Email { get; set; }

        public static List<User> GenerateData(int min = 50, int max = 500, bool includeDeleted = true)
        {
            var userGenerator = new Faker<User>()
                .RuleFor(u => u.ID, (f, u) => f.Random.Replace("####"))
                .RuleFor(u => u.Name, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
                .RuleFor(u => u.IsDeleted, (f, u) => f.Random.Bool(includeDeleted ? 0.1f : 0f));

            return userGenerator.GenerateBetween(min, max);
        }
    }
}