using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibekSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpSupportToPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastOtpSentAt",
                table: "PasswordResetTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtpAttempts",
                table: "PasswordResetTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "PasswordResetTokens",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOtpSentAt",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "OtpAttempts",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "PasswordResetTokens");
        }
    }
}
