using Bogus;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftBlazor.Tests.Shared.DTOs
{
    public class RevisionObj 
    {
        public  string? ID { get; set; }
        public DateTimeOffset? ValidFrom { get; set; }
        public DateTimeOffset? ValidTo { get; set; }
        public string? SavedByUserID { get; set; }

        public static List<RevisionObj> GenerateData(int min = 50, int max = 500, bool includeDeleted = true)
        {
            var userGenerator = new Faker<RevisionObj>()
                .RuleFor(u => u.ID, (f, u) => f.Random.Replace("####"))
                .RuleFor(u => u.ValidFrom, (f, u) => f.Date.Between(new DateTime(2018, 1, 1), new DateTime(2021, 1, 24)))
                .RuleFor(u => u.ValidTo, (f, u) => f.Date.Between(new DateTime(2020, 3, 4), new DateTime(2023, 12, 10)))
                .RuleFor(u => u.SavedByUserID, (f, u) => f.Random.Replace("####"));

            return userGenerator.GenerateBetween(min, max);
        }
    }
}
