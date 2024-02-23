using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGIdotNET.SpikeTime.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddContactToAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Contact_Email",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Contact_PhoneNumber",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_Contact_Email",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_Contact_PhoneNumber",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Contact_Email",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Contact_PhoneNumber",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress_Contact_Email",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Contact_PhoneNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_Contact_Email",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_Contact_PhoneNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_Contact_Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Address_Contact_PhoneNumber",
                table: "Customers");
        }
    }
}
