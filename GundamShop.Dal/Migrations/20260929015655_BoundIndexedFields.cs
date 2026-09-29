using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GundamShop.Dal.Migrations
{
    /// <inheritdoc />
    public partial class BoundIndexedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Vouchers_Code", table: "Vouchers");
            migrationBuilder.DropIndex(name: "IX_Variants_Sku", table: "Variants");
            migrationBuilder.DropIndex(name: "IX_Products_Slug", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Payments_ExternalSessionId", table: "Payments");
            migrationBuilder.DropIndex(name: "IX_Categories_Slug", table: "Categories");
            migrationBuilder.DropPrimaryKey(name: "PK_PaymentWebhookEvents", table: "PaymentWebhookEvents");
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Variants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "EventId",
                table: "PaymentWebhookEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalSessionId",
                table: "Payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddPrimaryKey(name: "PK_PaymentWebhookEvents", table: "PaymentWebhookEvents", column: "EventId");
            migrationBuilder.CreateIndex(name: "IX_Vouchers_Code", table: "Vouchers", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Variants_Sku", table: "Variants", column: "Sku", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Products_Slug", table: "Products", column: "Slug", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Payments_ExternalSessionId", table: "Payments", column: "ExternalSessionId", unique: true, filter: "[ExternalSessionId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_Categories_Slug", table: "Categories", column: "Slug", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Vouchers_Code", table: "Vouchers");
            migrationBuilder.DropIndex(name: "IX_Variants_Sku", table: "Variants");
            migrationBuilder.DropIndex(name: "IX_Products_Slug", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Payments_ExternalSessionId", table: "Payments");
            migrationBuilder.DropIndex(name: "IX_Categories_Slug", table: "Categories");
            migrationBuilder.DropPrimaryKey(name: "PK_PaymentWebhookEvents", table: "PaymentWebhookEvents");
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vouchers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Variants",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "EventId",
                table: "PaymentWebhookEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalSessionId",
                table: "Payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AddPrimaryKey(name: "PK_PaymentWebhookEvents", table: "PaymentWebhookEvents", column: "EventId");
            migrationBuilder.CreateIndex(name: "IX_Vouchers_Code", table: "Vouchers", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Variants_Sku", table: "Variants", column: "Sku", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Products_Slug", table: "Products", column: "Slug", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Payments_ExternalSessionId", table: "Payments", column: "ExternalSessionId", unique: true, filter: "[ExternalSessionId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_Categories_Slug", table: "Categories", column: "Slug", unique: true);
        }
    }
}
