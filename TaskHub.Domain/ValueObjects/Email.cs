using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskHub.Domain.ValueObjects
{
    public class Email
    {
        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty");

            if (!value.Contains('@'))
                throw new ArgumentException("Email format is invalid");

            return new Email(value.ToLower().Trim());
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj)
            => obj is Email other && Value == other.Value;

        public override int GetHashCode() => Value.GetHashCode();
    }
}