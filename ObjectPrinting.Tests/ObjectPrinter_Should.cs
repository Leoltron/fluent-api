﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FluentAssertions;
using NUnit.Framework;

namespace ObjectPrinting.Tests
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class ObjectPrinter_Should
    {
        private Person person;

        [SetUp]
        public void SetUp()
        {
            person = new Person
            {
                Age = 18,
                Height = 178.5d,
                Id = Guid.Parse("12345678-9abc-def1-2345-6789abcdef01"),
                Name = "Mike"
            };
        }

        [Test]
        public void Print_Integer()
        {
            var config = new PrintingConfig<int>();

            config.PrintToString(3).Should().BeEquivalentTo("3" + Environment.NewLine);
        }

        [Test]
        public void Print_Person()
        {
            var config = new PrintingConfig<Person>();

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tHeight = 178,5",
                "\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\tName = Mike");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_Person_WithoutGuid()
        {
            var config = new PrintingConfig<Person>().Exclude<Guid>();

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tHeight = 178,5",
                "\tName = Mike");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_Person_WithoutSeveralTypes()
        {
            var config = new PrintingConfig<Person>().Exclude<double>().Exclude<string>();

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tId = 12345678-9abc-def1-2345-6789abcdef01");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_Person_WithoutProperty()
        {
            var config = new PrintingConfig<Person>().Exclude(p => p.Height);

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\tName = Mike");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_PersonWith_AlternativeTypeSerialization()
        {
            var config = new PrintingConfig<Person>().Serializing<Guid>()
                .Using(guid => guid.ToString().ToUpperInvariant());

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tHeight = 178,5",
                "\tId = 12345678-9ABC-DEF1-2345-6789ABCDEF01",
                "\tName = Mike");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_PersonWith_AlternativePropertySerialization()
        {
            var config = new PrintingConfig<Person>().Serializing(p => p.Name).Using(s => s.Replace("M", "Blabla"));

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tHeight = 178,5",
                "\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\tName = Blablaike");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_Person_WithDifferentDoubleCulture()
        {
            var config = new PrintingConfig<Person>().Serializing<double>()
                .Using(CultureInfo.GetCultureInfoByIetfLanguageTag("RU-ru"));

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tHeight = 178,5",
                "\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\tName = Mike");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_Person_WithTruncation()
        {
            var config = new PrintingConfig<Person>().Serializing(p => p.Name).TrimToLength(2);

            var expected = string.Join(Environment.NewLine,
                "Person",
                "\tAge = 18",
                "\tHeight = 178,5",
                "\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\tName = Mi");

            config.PrintToString(person).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_ParentWithCyclicReference()
        {
            var parent = new Parent {Age = 10, Height = 100, Id = Guid.Empty, Name = "Ivan"};
            parent.Child = parent;

            var config = new PrintingConfig<Parent>();

            var expected = string.Join(Environment.NewLine,
                "Parent",
                "\tAge = 10",
                "\tChild = Cyclic reference to level 0",
                "\tHeight = 100",
                "\tId = 00000000-0000-0000-0000-000000000000",
                "\tName = Ivan");

            config.PrintToString(parent).Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        [Test]
        public void Print_Collections()
        {
            var parent = new ParentOfMany
            {
                Age = 10,
                Height = 100,
                Id = Guid.Empty,
                Name = "Ivan",
                Children = new List<Person> {person, person, person}
            };
            var config = new PrintingConfig<ParentOfMany>();
            var expected = string.Join(Environment.NewLine,
                "ParentOfMany",
                "\tAge = 10",
                "\tChildren = \tList`1",
                "\t\t[",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\t]",
                "\tHeight = 100",
                "\tId = 00000000-0000-0000-0000-000000000000",
                "\tName = Ivan");

            var printToString = config.PrintToString(parent);
            printToString.Should().BeEquivalentTo(expected + Environment.NewLine);
        }

        private IEnumerable<Person> InfinitePerson()
        {
            while (true)
            {
                yield return person;
            }

            // ReSharper disable once IteratorNeverReturns
        }

        [Test]
        public void Print_InfiniteCollections()
        {
            var parent = new ParentOfMany
            {
                Age = 10,
                Height = 100,
                Id = Guid.Empty,
                Name = "Ivan",
                Children = InfinitePerson()
            };
            var config = new PrintingConfig<ParentOfMany>();

            var expected = string.Join(Environment.NewLine,
                "ParentOfMany",
                "\tAge = 10",
                "\tChildren = \t<InfinitePerson>d__13",
                "\t\t[",

                #region Person_x10

                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",
                "\t\tPerson",
                "\t\t\tAge = 18",
                "\t\t\tHeight = 178,5",
                "\t\t\tId = 12345678-9abc-def1-2345-6789abcdef01",
                "\t\t\tName = Mike",

                #endregion

                "\t\t...",
                "\t\t]",
                "\tHeight = 100",
                "\tId = 00000000-0000-0000-0000-000000000000",
                "\tName = Ivan");

            config.PrintToString(parent).Should().BeEquivalentTo(expected + Environment.NewLine);
        }
    }
}
