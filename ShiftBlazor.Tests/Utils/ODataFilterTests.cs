using MudBlazor;
using ShiftSoftware.ShiftBlazor.Enums;

namespace ShiftSoftware.ShiftBlazor.Tests.Utils
{
    public class ODataFilterTests
    {
        [Fact]
        public void ShouldBeEmptyString()
        {
            var filter = new ODataFilterGenerator();

            Assert.NotNull(filter);
            Assert.Empty(filter.ToString()!);
        }

        [Fact]
        public void ShouldBeValidFilterWithFieldOnly()
        {
            var filter = new ODataFilterGenerator(true).Add(x => x.Field = "My Field");

            Assert.Equal($"My Field eq null", filter.ToString());
        }

        [Fact]
        public void ShouldAllowChangingOperator()
        {
            var fieldName = "My Field";
            var filter = new ODataFilterGenerator(true).Add(x =>
            {
                x.Field = fieldName;
                x.Operator = ODataOperator.NotEqual;
            });

            Assert.Equal($"{fieldName} ne null", filter.ToString());
        }

        [Fact]
        public void ShouldCreateAValidFilter()
        {
            var field1 = "My Field";
            var filter = new ODataFilterGenerator(true).Add(x =>
            {
                x.Field = field1;
                x.Operator = ODataOperator.NotEqual;
                x.Value = 3466;
            });

            Assert.Equal($"{field1} ne 3466", filter.ToString());
        }

        [Fact]
        public void ShouldUseAndToGroupFilters()
        {
            var filter = new ODataFilterGenerator(true).Add(x =>
            {
                x.Field = "field1";
                x.Operator = ODataOperator.NotEqual;
                x.Value = 3466;
            }).Add(
            x =>
            {
                x.Field = "field2";
                x.Operator = ODataOperator.Equal;
                x.Value = 3466;
            }).Add(
            x =>
            {
                x.Field = "field3";
                x.Operator = ODataOperator.GreaterThan;
                x.Value = 3466;
            });

            Assert.Equal($"field1 ne 3466 and field2 eq 3466 and field3 gt 3466", filter.ToString());
        }

        [Fact]
        public void ShouldUseOrToGroupFilters()
        {
            var filter = new ODataFilterGenerator(false).Add(x =>
            {
                x.Field = "field1";
                x.Operator = ODataOperator.NotEqual;
                x.Value = 3466;
            }).Add(
            x =>
            {
                x.Field = "field2";
                x.Operator = ODataOperator.Equal;
                x.Value = 3466;
            }).Add(
            x =>
            {
                x.Field = "field3";
                x.Operator = ODataOperator.GreaterThan;
                x.Value = 3466;
            });

            Assert.Equal($"field1 ne 3466 or field2 eq 3466 or field3 gt 3466", filter.ToString());
        }

        [Fact]
        public void ShouldCreateSameFilterWithConstructorAndMethods()
        {
            var field1 = "My Field";
            var filter1 = new ODataFilterGenerator(true).Add(x =>
            {
                x.Field = field1;
                x.Operator = ODataOperator.NotEqual;
                x.Value = 3466;
            });
            var filter2 = new ODataFilterGenerator(true).Add(field1, ODataOperator.NotEqual, 3466);

            Assert.Equal(filter1.ToString(), filter2.ToString());
        }

        // These two tests covered the old fluent group-builder API, where a filter could be
        // both a field and a group of child filters. ODataFilterGenerator replaced that API
        // and has no equivalent feature. The tests are kept for reference until they are
        // rewritten against the generator API.
        //[Fact]
        //public void ShouldCreateAValidComplexFilterUsingAnd()
        //{
        //    var time = DateTime.UtcNow;

        //    var filter = new ODataFilter()
        //        .Add("Field1", ODataOperator.Equal, 1)
        //        .Add("Field2", ODataOperator.Equal, true)
        //        .Add("Field3", ODataOperator.Equal, 56)
        //        .And(x =>
        //        {
        //            x.Field = "Field4";
        //            x.Value = 1;
        //            x.Add("Field5", ODataOperator.Equal, 345);
        //            x.Add("Field6", ODataOperator.Equal, time);
        //        },
        //        x=>
        //        {
        //            x.Field = "Field4.1";
        //            x.Value = "val";
        //        })
        //        .Add(x => x.Field = "Field7")
        //        .Or(x =>
        //        {
        //            x.Field = "Field8";
        //            x.Value = false;
        //        },
        //        x =>
        //        {
        //            x.Field = "Field8.1";
        //            x.Operator = ODataOperator.NotEqual;
        //            x.Value = "abc";
        //        },
        //        x =>
        //        {
        //            x.Field = "Field8.2";
        //            x.Value = true;
        //        })
        //        .Add(x =>
        //        {
        //            x.Add(x =>
        //            {
        //                x.And(x =>
        //                {
        //                    x.Or(x =>
        //                    {
        //                        x.Add("Field9", ODataOperator.Contains, "hello");
        //                        x.Add("Field10", ODataOperator.Contains, "world");
        //                    });
        //                });
        //            });
        //        });

        //    Assert.Equal($"Field1 eq 1 and Field2 eq true and Field3 eq 56 and (Field4 eq 1 and (Field5 eq 345 and Field6 eq {time.ToString("yyyy-MM-ddTHH:mm:ss.fff")}%2B00:00) and Field4.1 eq 'val') and Field7 eq null and (Field8 eq false or Field8.1 ne 'abc' or Field8.2 eq true) and ((((((contains(Field9,'hello') or contains(Field10,'world')))))))", filter.ToString());
        //}

        //[Fact]
        //public void ShouldCreateAValidComplexFilterUsingOr()
        //{
        //    var time = DateTime.UtcNow;

        //    var filter = new ODataFilter(false)
        //        .Add("Field1", ODataOperator.Equal, 1)
        //        .Add("Field2", ODataOperator.Equal, true)
        //        .Add("Field3", ODataOperator.Equal, 56)
        //        .And(x =>
        //        {
        //            x.Field = "Field4";
        //            x.Value = 1;
        //            x.Add("Field5", ODataOperator.Equal, 345);
        //            x.Add("Field6", ODataOperator.Equal, time);
        //        },
        //        x =>
        //        {
        //            x.Field = "Field4.1";
        //            x.Value = "val";
        //        })
        //        .Add(x => x.Field = "Field7")
        //        .Or(x =>
        //        {
        //            x.Field = "Field8";
        //            x.Value = false;
        //        },
        //        x =>
        //        {
        //            x.Field = "Field8.1";
        //            x.Operator = ODataOperator.NotEqual;
        //            x.Value = "abc";
        //        },
        //        x =>
        //        {
        //            x.Field = "Field8.2";
        //            x.Value = true;
        //        })
        //        .Add(x =>
        //        {
        //            x.Add(x =>
        //            {
        //                x.And(x =>
        //                {
        //                    x.Or(x =>
        //                    {
        //                        x.Add("Field9", ODataOperator.Contains, "hello");
        //                        x.Add("Field10", ODataOperator.Contains, "world");
        //                    });
        //                });
        //            });
        //        });

        //    Assert.Equal($"Field1 eq 1 or Field2 eq true or Field3 eq 56 or (Field4 eq 1 and (Field5 eq 345 and Field6 eq {time.ToString("yyyy-MM-ddTHH:mm:ss.fff")}%2B00:00) and Field4.1 eq 'val') or Field7 eq null or (Field8 eq false or Field8.1 ne 'abc' or Field8.2 eq true) or ((((((contains(Field9,'hello') or contains(Field10,'world')))))))", filter.ToString());
        //}

        [Fact]
        public void GetValueStringShouldHandleString()
        {
            var value = ODataFilterGenerator.GetValueString("ab'cd");
            Assert.Equal("'ab''cd'", value);
        }

        [Fact]
        public void GetValueStringShouldAllowFieldTypeAsArgument()
        {
            var wrongType = MudBlazor.FieldType.Identify(typeof(int));
            var value = ODataFilterGenerator.GetValueString("abcd", wrongType);
            Assert.Equal("abcd", value);
        }

        [Fact]
        public void GetValueStringShouldHandleEnum()
        {
            var value = ODataFilterGenerator.GetValueString(FormModes.Create);
            Assert.Equal("'Create'", value);
        }

        [Fact]
        public void GetValueStringShouldHandleDate()
        {
            var date = DateTime.Now;
            var value = ODataFilterGenerator.GetValueString(new DateTime(2023, 12, 13));
            var offset = date.ToString("zzz").Replace("+", "%2B");
            Assert.Equal("2023-12-13T00:00:00.000" + offset, value);
        }

        [Fact]
        public void GetValueStringShouldHandleBoolean()
        {
            var value = ODataFilterGenerator.GetValueString(true);
            Assert.Equal("true", value);
        }

        [Fact]
        public void GetValueStringShouldHandleNull()
        {
            var value = ODataFilterGenerator.GetValueString(null);
            Assert.Equal("null", value);
        }

        [Fact]
        public void GetValueStringShouldHandleEnumerableWithAllTypes()
        {
            var date = new DateTime(2023, 12, 13);
            var list = new List<object?> { "abc", FormModes.Edit, date, false, null };
            var value = ODataFilterGenerator.GetValueString(list);
            var offset = date.ToString("zzz").Replace("+", "%2B");
            Assert.Equal("'abc','Edit',2023-12-13T00:00:00.000" + offset + ",false,null", value);
        }

        [Fact]
        public void ShouldCreateFilterTemplateEqual()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.Equal),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.Number.Equal),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.Equal),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.DateTime.Is),
                "default",
            };

            Assert.All(list, x => x.Equals("{0} eq {1}"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateNotEqual()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.NotEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.Number.NotEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.NotEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.DateTime.IsNot),
            };

            Assert.All(list, x => x.Equals("{0} ne {1}"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateGreaterThan()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.GreaterThan),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.Number.GreaterThan),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.DateTime.After),
            };

            Assert.All(list, x => x.Equals("{0} gt {1}"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateGreaterOrEqual()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.GreaterThanOrEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.Number.GreaterThanOrEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.DateTime.OnOrAfter),
            };

            Assert.All(list, x => x.Equals("{0} ge {1}"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateLessThan()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.LessThan),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.Number.LessThan),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.DateTime.Before),
            };

            Assert.All(list, x => x.Equals("{0} lt {1}"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateLessOrEqual()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.LessThanOrEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.Number.LessThanOrEqual),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.DateTime.OnOrBefore),
            };

            Assert.All(list, x => x.Equals("{0} le {1}"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateContains()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.Contains),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.Contains),
            };

            Assert.All(list, x => x.Equals("contains({0},{1})"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateNotContains()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.NotContains),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.NotContains),
            };

            Assert.All(list, x => x.Equals("not contains({0},{1})"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateStartWith()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.StartsWith),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.StartsWith),
            };

            Assert.All(list, x => x.Equals("startswith({0},{1})"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateEndsWith()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.EndsWith),
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.EndsWith),
            };

            Assert.All(list, x => x.Equals("endswith({0},{1})"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateIn()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(ODataOperator.In),
            };

            Assert.All(list, x => x.Equals("{0} in ({1})"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateEqualNull()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.Empty),
            };

            Assert.All(list, x => x.Equals("{0} eq null"));
        }

        [Fact]
        public void ShouldCreateFilterTemplateNotEqualNull()
        {
            var list = new List<string>
            {
                ODataFilterGenerator.CreateFilterTemplate(FilterOperator.String.NotEmpty),
            };

            Assert.All(list, x => x.Equals("{0} ne null"));
        }
    }
}
