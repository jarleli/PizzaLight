using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PizzaLight.Models;
using PizzaLight.Resources.ExtensionClasses;

namespace PizzaLight.Tests.Unit
{
    [TestFixture, Category("Unit")]
    public class PersonExtensionsTests
    {
        private List<Person> _people;

        [OneTimeSetUp]
        public void Setup()
        {
            var array = new[] {"a", "b", "c", "d"}.ToList();
            _people = array.Select(a => new Person() {UserName = a}).ToList();
        }

        [Test]
        public void NoList()
        {
            var a = _people.GetRange(0, 0).ToList();
            var res = a.GetStringListOfPeople();
            Assert.AreEqual("", res);
        }

        [Test]
        public void FirstList()
        {
            var a = _people.GetRange(0, 1).ToList();
            var res = a.GetStringListOfPeople();
            StringAssert.DoesNotContain(",", res);
            StringAssert.DoesNotContain("and", res);
            Assert.AreEqual("a", res);
        }

        [Test]
        public void SecondList()
        {

            var a = _people.GetRange(0, 2).ToList();
            var str1 = a.GetStringListOfPeople();
            StringAssert.DoesNotContain(",", str1);
            StringAssert.Contains("and", str1);
        }

        [Test]
        public void ThirdList()
        {

            var a = _people.GetRange(0, 3).ToList();
            var str = a.GetStringListOfPeople();
            StringAssert.Contains(",", str);
            StringAssert.Contains("and c", str);
            StringAssert.Contains("b", str);
        }

        [Test]
        public void FourthList()
        {
            var a = _people.GetRange(0, 4).ToList();
            var str = a.GetStringListOfPeople();
            Assert.AreEqual("a, b, c and d", str);
        }
    }
}