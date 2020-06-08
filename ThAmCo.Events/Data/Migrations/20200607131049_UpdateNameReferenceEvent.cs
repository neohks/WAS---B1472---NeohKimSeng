using Microsoft.EntityFrameworkCore.Migrations;

namespace ThAmCo.Events.Data.Migrations
{
    public partial class UpdateNameReferenceEvent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VenueCode",
                schema: "thamco.events",
                table: "Events",
                newName: "VenueReference");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VenueReference",
                schema: "thamco.events",
                table: "Events",
                newName: "VenueCode");
        }
    }
}
