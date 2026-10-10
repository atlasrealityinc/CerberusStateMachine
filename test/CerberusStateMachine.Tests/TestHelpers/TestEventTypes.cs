using System;

namespace Cerberus.Tests.TestHelpers
{
    //Non-enum event id types. These are public because NSubstitute's proxy generator needs every
    //generic argument of a substituted interface to be accessible, for example IEventTrigger<TestStructEventId>.

    /// <summary>A value-type event id with value equality, the shape that keeps lookups allocation-free.</summary>
    public readonly struct TestStructEventId : IEquatable<TestStructEventId>
    {
        public static readonly TestStructEventId Alpha = new TestStructEventId(1);
        public static readonly TestStructEventId Beta = new TestStructEventId(2);
        public static readonly TestStructEventId Unregistered = new TestStructEventId(99);

        public int Id { get; }

        public TestStructEventId(int id)
        {
            Id = id;
        }

        public bool Equals(TestStructEventId other) => Id == other.Id;
        public override bool Equals(object obj) => obj is TestStructEventId other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => $"TestStructEventId({Id})";
    }

    /// <summary>A reference-type event id with compiler-generated value equality.</summary>
    public sealed record TestRecordEventId(string Name);

    /// <summary>An event id declared through an interface, used to exercise the exact-type dispatch rule.</summary>
    public interface ITestEventId
    {
    }

    /// <summary>A reference-type event id with reference equality only.</summary>
    public sealed class TestConcreteEventId : ITestEventId
    {
        public static readonly TestConcreteEventId Shared = new TestConcreteEventId("shared");

        public string Name { get; }

        public TestConcreteEventId(string name)
        {
            Name = name;
        }
    }

    /// <summary>A value-type event id that carries data, used to drive predicate events that split on a field.</summary>
    public readonly struct TestFlagEventId : IEquatable<TestFlagEventId>
    {
        public static readonly TestFlagEventId On = new TestFlagEventId(true, 0);
        public static readonly TestFlagEventId Off = new TestFlagEventId(false, 0);

        public bool IsOn { get; }
        public int Amount { get; }

        public TestFlagEventId(bool isOn, int amount)
        {
            IsOn = isOn;
            Amount = amount;
        }

        public bool Equals(TestFlagEventId other) => IsOn == other.IsOn && Amount == other.Amount;
        public override bool Equals(object obj) => obj is TestFlagEventId other && Equals(other);
        public override int GetHashCode() => (IsOn ? 1 : 0) * 31 + Amount;
        public override string ToString() => $"TestFlagEventId({(IsOn ? "on" : "off")}, {Amount})";
    }

    public static class TestStringEventId
    {
        public const string Go = "go";
        public const string Stop = "stop";
        public const string Unregistered = "unregistered";
    }
}
