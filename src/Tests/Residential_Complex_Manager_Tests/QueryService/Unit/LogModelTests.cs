using BuildingBlocks.Messaging.KafkaLogger;

namespace Residential_Complex_Manager_Tests.QueryService.Unit
{
    public class LogModelTests
    {
        [Fact]
        public void Default_Id_is_a_new_GUID_string()
        {
            var a = new LogModel();
            var b = new LogModel();
            Guid.TryParse(a.Id, out _).Should().BeTrue();
            a.Id.Should().NotBe(b.Id);
        }

        [Fact]
        public void Default_Timestamp_is_close_to_UtcNow()
        {
            var before = DateTime.UtcNow;
            var m = new LogModel();
            m.Timestamp.Should().BeCloseTo(before, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Properties_dictionary_is_initialised_to_empty()
        {
            new LogModel().Properties.Should().NotBeNull().And.BeEmpty();
        }
    }
}
