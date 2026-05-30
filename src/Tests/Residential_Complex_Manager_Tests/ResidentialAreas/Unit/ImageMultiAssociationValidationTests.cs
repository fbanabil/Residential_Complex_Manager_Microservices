using ResidentialAreas.API.Enum;
using ResidentialAreas.API.EntityModels;
using System.ComponentModel.DataAnnotations;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Unit
{
    /// <summary>
    /// Exercises the IValidatableObject rules on Image, which enforce that an image must
    /// belong to exactly one of Area/Building/Unit/Facility/ParkingSpace.
    /// </summary>
    public class ImageMultiAssociationValidationTests
    {
        private static List<ValidationResult> Validate(Image image)
        {
            var ctx = new ValidationContext(image);
            var results = new List<ValidationResult>();
            // Run our custom IValidatableObject as well as DataAnnotations:
            Validator.TryValidateObject(image, ctx, results, validateAllProperties: true);
            // Manually invoke the IValidatableObject to ensure execution
            results.AddRange(image.Validate(ctx));
            return results;
        }

        [Fact]
        public void Image_with_no_associated_owner_is_invalid()
        {
            var img = new Image { Id = Guid.NewGuid(), ImageType = ImageType.Area, Url = "x.png" };
            var errors = Validate(img);
            errors.Should().NotBeEmpty();
        }

        [Fact]
        public void Image_with_a_single_owner_passes_custom_validation()
        {
            var img = new Image { Id = Guid.NewGuid(), ImageType = ImageType.Area, Url = "x.png", AreaCode = 1_000_000_001 };
            var custom = img.Validate(new ValidationContext(img)).ToList();
            custom.Should().BeEmpty();
        }

        [Fact]
        public void Image_with_two_owners_is_rejected()
        {
            var img = new Image
            {
                Id = Guid.NewGuid(), ImageType = ImageType.Building, Url = "x.png",
                AreaCode = 1_000_000_001, BuildingCode = 2_000_000_001
            };
            var custom = img.Validate(new ValidationContext(img)).ToList();
            custom.Should().NotBeEmpty();
            custom.Single().ErrorMessage.Should().Contain("only be associated with one");
        }
    }
}
