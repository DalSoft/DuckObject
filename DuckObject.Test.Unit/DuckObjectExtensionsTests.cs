using System.Collections.Generic;
using DalSoft.Dynamic.DynamicExpressions;
using NUnit.Framework;
using System;

namespace DalSoft.Dynamic
{
    [TestFixture]
    public class DuckObjectExtensionsTests : UnitTestStopWatch
    {
        [Test]
        public void WhenUsedAsABaseClass_AsIfDynamic_ReturnsDynamic()
        {
            Person duckObject = Person.CreateTestClasses();
            var @dynamic = duckObject.AsIf<dynamic>();
            Assert.That(@dynamic.Office.Department.Name, Is.EqualTo(Person.CreateTestClasses().Office.Department.Name));
        }

        [Test]
        public void AsIf_ClassWithoutParameterlessConstructor_ThrowsArgumentException()
        {
            var duckObject = new DuckObject();
            Assert.Throws<ArgumentException>(()=>duckObject.AsIf<ClassWithOutParameterlessConstructor>());
        }

        [Test]
        public void AsIf_InterfaceWithInterfaceThatHasMethodMembers_ThrowsArgumentException()
        {
            var duckObject = new DuckObject();
            Assert.Throws<ArgumentException>(() => duckObject.AsIf<IInterfaceWithMethod>());
        }
        
        [Test]
        public void AsIf_Interface_ReturnsNewObjectImplementingInterfaceMappingValuesFromTheDuckObject()
        {
            var dob = DateTime.MinValue;
            var duckObject = new DuckObject(new { Dob = dob });
            
            var person = duckObject.AsIf<IPerson>();
            
            Assert.That(person.Dob, Is.EqualTo(dob));
        }

        [Test]
        public void AsIf_InterfaceWithAClassThatHasComposition_ReturnsNewObjectImplementingInterfaceMappingValuesFromTheDuckObject()
        {
            const string officeName = "London";
            var duckObject = new DuckObject(new { Office = new { Name = officeName } });
            
            IPerson person = duckObject.AsIf<IPerson>();
            
            Assert.That(person.Office.Name, Is.EqualTo(officeName));            
        }

        [Test]
        public void WhenUsedAsABaseClass_AsIfInterfaceWithAnInterfaceThatHasComposition_ReturnsNewObjectImplementingInterfacesMappingValuesFromTheDuckObject()
        {
            Person duckObject = Person.CreateTestClasses();
            IPerson person = duckObject.AsIf<IPerson>();
            Assert.That(person.Office.Department.Name, Is.EqualTo(Person.CreateTestClasses().Office.Department.Name));
        }

        [Test]
        public void AsIf_Class_ReturnsNewInstanceOfTheClassMappingValuesFromTheDuckObject()
        {
            var dob = DateTime.MinValue;
            var duckObject = new DuckObject(new { Dob=dob });
            
            var person = duckObject.AsIf<Person>();

            Assert.That(person.Dob, Is.EqualTo(dob));
        }

        [Test]
        public void AsIf_ClassThatHasComposition_ReturnsNewInstanceOfTheClassMappingValuesFromTheDuckObject()
        {
            const string name = "London";
            var duckObject = new DuckObject(new { Name = name, Department= new { Name = name } });

            Office office = duckObject.AsIf<Office>();

            Assert.That(office.Name, Is.EqualTo(name));
            Assert.That(office.Department.Name, Is.EqualTo(name));
        }

        [Test]
        public void DuckObject_AsIfAnonymousType_SetDefaultsFromAnonymousType()
        {
            var anonymousType = new
            {
                CompanyId = 100,
                Person = new {PersonId = 22, FirstName = "Default FirstName", Date = DateTime.MaxValue }
            };

            var duckObject = new DuckObject().AsIf(anonymousType);
            
            Assert.That(duckObject.CompanyId, Is.EqualTo(anonymousType.CompanyId));
            Assert.That(duckObject.Person.PersonId, Is.EqualTo(anonymousType.Person.PersonId));
            Assert.That(duckObject.Person.Date, Is.EqualTo(anonymousType.Person.Date));
        }

        [Test]
        public void DuckObject_AsIfAnonymousType_MapsAnonymousTypeUsingValuesFromDuckObject()
        {
            var anonymousType = new
            {
                CompanyId = 0,
                Person = new { PersonId = 0, FirstName = "Default FirstName",  Department=new { DepartmentId=0, DepartmentName="" } }
            };

            var duckObject = new DuckObject(new
            {
                CompanyId = 1, 
                Person = new { PersonId = 22, Department = new { DepartmentName = "IT" } }
            });

            var newAnonymousType = duckObject.AsIf(anonymousType);

            Assert.That(newAnonymousType.CompanyId, Is.EqualTo(duckObject.AsIf<dynamic>().CompanyId));
            Assert.That(newAnonymousType.Person.PersonId, Is.EqualTo(duckObject.AsIf<dynamic>().Person.PersonId));
            Assert.That(newAnonymousType.Person.Department.DepartmentName, Is.EqualTo(duckObject.AsIf<dynamic>().Person.Department.DepartmentName));
        }

        [Test]
        public void DuckObject_AsIfAnonymousType_MapsAnonymousTypeUsingValuesFromDuckObjectConvertingTypesWherePossible()
        {
            const string intValueAsString = "22"; 
            var anonymousType = new
            {
                CompanyId = 0, Person = new { PersonId = 0 }
            };

            var duckObject = new DuckObject(new
            {
                CompanyId = intValueAsString, Person = new { PersonId = intValueAsString }
            });

            var newAnonymousType = duckObject.AsIf(anonymousType);

            Assert.That(newAnonymousType.CompanyId, Is.EqualTo(int.Parse(intValueAsString)));
            Assert.That(newAnonymousType.Person.PersonId, Is.EqualTo(int.Parse(intValueAsString)));
        }

        [Test]
        public void DuckObject_AsIfAnonymousTypeUsingArray_MapsAnonymousTypeUsingValuesFromDuckObjectConvertingTypesWherePossible()
        {
            var duckObject = new DuckObject(new { FirstName = "c", TheArray = new[] { "1", "2", "3" } });

            duckObject.AsIf(new { TheArray = new[] { "" } }); //TODO this should convert the duckobject to a duckobject just containing the TheArray with the mapped duckobject values
        }

        [Test]
        public void DuckObject_AsIfAnonymousTypeUsingList_MapsAnonymousTypeUsingValuesFromDuckObjectConvertingTypesWherePossible()
        {
            var duckObject = new DuckObject(new { FirstName = "c", TheArray = new List<string> { "1", "2", "3" } });
            dynamic result = duckObject.AsIf(new { TheList = new List<string>() });

            Assert.That(result.TheList[0], Is.EqualTo("1")); //TODO this should convert the duckobject to a duckobject just containing the TheList with the mapped duckobject values
        }

        [Test]
        public void DuckObject_AsIfAnonymousTypeConvertingListToArray_MapsAnonymousTypeUsingValuesFromDuckObjectConvertingTypesWherePossible()
        {
            //TODO
        }

        [Test]
        public void DuckObject_AsIfAnonymousTypeConvertingArrayToList_MapsAnonymousTypeUsingValuesFromDuckObjectConvertingTypesWherePossible()
        {
            //TODO
        }

        [Test]
        public void DuckObject_AsIfAnonymousTypeConvertingDictionaryToArray_MapsAnonymousTypeUsingValuesFromDuckObjectConvertingTypesWherePossible()
        {
            //TODO
        }

        [Test]
        public void WhenUsedAsABaseClass_AsIfAnonymousType_MapsAnonymousTypeUsingValuesFromDerivedProperties()
        {
            var anonymousType = new
            {
                Id = 0,
                Office = new { Department = new { DepartmentId = 0, Name = "" } }
            };

            var person = Person.CreateTestClasses();
            var newAnonymousType = person.AsIf(anonymousType);

            Assert.That(newAnonymousType.Id, Is.EqualTo(person.Id));
            Assert.That(newAnonymousType.Office.Department.Name, Is.EqualTo(person.Office.Department.Name));
        }

        [Test]
        public void WhenUsedAsABaseClass_ExtendingWithADerivedPropertyThatAlreadyExists_ThrowsArgumentException()
        {
            var person = Person.CreateTestClasses();
            Assert.Throws<ArgumentException>(() => person.Extend(new { Office = 100 }));
        }

        [Test]
        public void WhenUsedAsABaseClass_ExtendingWithExpressionThatIsADerivedPropertyThatAlreadyExists_ThrowsArgumentException()
        {
            var person = Person.CreateTestClasses();
            Assert.Throws<ArgumentException>(() => person.Extend(x => x.Office.Department = "Darran"));
        }

        [Test]
        public void DuckObject_ExtendingWithAPropertyThatAlreadyExists_ThrowsArgumentException()
        {
            var duckObject = new DuckObject(new { Id = 100 });
            Assert.Throws<ArgumentException>(() => duckObject.Extend(new { Id = 100 }));
        }

        [Test]
        public void DuckObject_ExtendingWithAnonymousType_ExtendsUsingValuesFromAnonymousType()
        {
            var duckObject = new DuckObject();
            var anonymousType = new
            {
                CompanyId = 11,
                Person = new { FirstName = "Darran", Office = new { Department = new { DepartmentName = "R&D" } } }
            };
            
            duckObject.Extend(anonymousType);

            Assert.That(duckObject.AsIf(anonymousType).CompanyId, Is.EqualTo(anonymousType.CompanyId));
            Assert.That(duckObject.AsIf(anonymousType).Person.FirstName, Is.EqualTo(anonymousType.Person.FirstName));
            Assert.That(duckObject.AsIf(anonymousType).Person.Office.Department.DepartmentName, Is.EqualTo(anonymousType.Person.Office.Department.DepartmentName));
        }

        [Test]
        public void DuckObject_ExtendingWithClass_ExtendsUsingValuesFromClass()
        {
            var duckObject = new DuckObject();
            var person = Person.CreateTestClasses().Office;
            
            duckObject.Extend(person);

            Assert.That(duckObject.AsIf(person).Name, Is.EqualTo(person.Name));
            Assert.That(duckObject.AsIf(person).Department.TeamSize, Is.EqualTo(person.Department.TeamSize));
        }

        [Test]
        public void DuckObject_ExtendingWithExpression_ExtendsUsingValuesFromExpression()
        {
           var duckObject = new DuckObject().Extend(x => x.Person.Department.Name = "Darran");
           Assert.That(duckObject.AsIf<dynamic>().Person.Department.Name, Is.EqualTo("Darran"));
        }

        [Test]
        public void DuckObject_ExtendingWithExpressionOfAPropertyThatAlreadyExists_ThrowsArgumentException()
        {
            var duckObject = new DuckObject(new { Id = 100 });
            Assert.Throws<ArgumentException>(() => duckObject.Extend(x=> x.Id = 200));
        }

        [Test]
        public void DuckObject_SetWithPropertyThatDoesNotExist_ThrowsArgumentException()
        {
            var duckObject = new DuckObject(new { Office = new { Id = 100, Name = "London", Size = 1000 } });
            Assert.Throws<ArgumentException>(() => duckObject.Set(new { Office = new { PropertyThatDoesNotExist = "" } }));
        }

        [Test]
        public void DuckObject_SetWithClassThatIsNotAnAnonymousType_ThrowsArgumentException()
        {
            var duckObject = new DuckObject();
            Assert.Throws<ArgumentException>(() => duckObject.Set(new Person()));
        }

        [Test]
        public void DuckObject_SetWithAnonymousType_SetsThePropertyUsingValuesFromTheAnonymousTypeWhilstMaintainingTheRestOfTheClass()
        {
            const int newId = 22;
            var duckObject = new DuckObject(new { Office = new { Id = 100, Name = "London", Size = 1000 } });

            duckObject.Set(new { Office = new { Id = newId } });

            Assert.That(duckObject.AsIf<dynamic>().Office.Id, Is.EqualTo(newId));
            Assert.That(duckObject.AsIf<dynamic>().Office.Name, Is.EqualTo("London"));
        }

        [Test]
        public void WhenUsedAsABaseClass_SetWithAnonymousType_SetsThePropertyUsingValuesFromTheAnonymousTypeWhilstMaintainingTheRestOfTheClass()
        {
            const int newId = 22;
            var person = Person.CreateTestClasses();

            person.Set(new { Office = new { Department = new { Id=newId } } });

            Assert.That(person.Office.Department.Id, Is.EqualTo(newId));
            Assert.That(person.Office.Name, Is.EqualTo(person.Office.Name));
        }

    }
}
