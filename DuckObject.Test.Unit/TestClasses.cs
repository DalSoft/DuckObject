using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DalSoft.Dynamic
{
    public enum OfficeSize { Big, Medium, Small }

    public class Person : DuckObject
    {
        public string Message(string message)
        {
            return message;
        }

        public int Id { get; set; }
        public string FirstName { get; private set; }
        public string LastName { get; set; }
        public DateTime Dob { get; set; }
        public Office Office { get; set; }

        public static Person CreateTestClasses()
        {
            return new Person
            {
                Dob = new DateTime(2012, 12, 25),
                Id = 100,
                //FirstName = "John",
                LastName = "Lennon",
                Office = new Office
                {
                    Id = 101,
                    Address = "Tour Eiffel Champ de Mars, Paris",
                    OfficeSize = OfficeSize.Medium,
                    Name = "Paris Office",
                    OfficeBudget = new Budget
                    {
                        Cost = 2.99m,
                        LineItems = new List<string> { "computer equipment", "stationary", "staff expenses" }
                    },
                    Department = new Department
                    {
                        Id = 102,
                        Name = "IT"
                        //List<>
                    }
                }
            };
        }
    }

    public class Office
    {
        public int Id { get; set; }
        public OfficeSize OfficeSize { get; set; }
        //public Budget OfficeBudget { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public IDepartment Department { get; set; }
        public Budget OfficeBudget { get; set; }
    }

    public class Department : IDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? TeamSize { get; set; }
    }

    public class Budget
    {
        public decimal Cost { get; set; }
        public List<string> LineItems { get; set; }
    }

    public interface IPerson
    {
        //TODO method duck typing
        //string Message(string message); 
        int Id { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        DateTime Dob { get; set; }
        Office Office { get; set; }
    }

    public interface IDepartment
    {
        int Id { get; set; }
        string Name { get; set; }
        int? TeamSize { get; set; }
    }

    public interface IInterfaceWithMethod
    {
        void MyMethod();
    }

    public class ClassWithOutParameterlessConstructor
    {
        public ClassWithOutParameterlessConstructor(string v) {}
    }
}
