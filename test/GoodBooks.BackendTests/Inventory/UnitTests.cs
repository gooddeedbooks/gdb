using System.ComponentModel.DataAnnotations;
using Dto.Inventory;
using Xunit;

namespace GoodBooks.BackendTests.Inventory
{
    public class UnitTests
    {
        [Theory]
        [InlineData("EA", "Each")]
        [InlineData("HR", "Hour")]
        [InlineData("MO", "Monthly")]
        [InlineData("PK", "Pack")]
        public void Validate_ExistingSeedUnit_AcceptsCodeAndDescription(string code, string description)
        {
            Assert.Empty(Validate(new Unit { Code = code, Description = description }));
        }

        [Theory]
        [InlineData("", "Each", "Code")]
        [InlineData("   ", "Each", "Code")]
        [InlineData("EA", "", "Description")]
        [InlineData("EA", "   ", "Description")]
        public void Validate_MissingRequiredValue_RejectsField(string code, string description, string member)
        {
            Assert.Contains(Validate(new Unit { Code = code, Description = description }),
                error => error.MemberNames.Contains(member));
        }

        [Theory]
        [InlineData(20, 100, true)]
        [InlineData(21, 100, false)]
        [InlineData(20, 101, false)]
        public void Validate_LengthLimits_EnforcesBoundaries(int codeLength, int descriptionLength, bool valid)
        {
            var errors = Validate(new Unit { Code = new string('C', codeLength), Description = new string('D', descriptionLength) });

            Assert.Equal(valid, errors.Count == 0);
        }

        private static List<ValidationResult> Validate(Unit unit)
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(unit, new ValidationContext(unit), errors, true);
            return errors;
        }
    }
}
