using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theoremone.SmartAc.Infrastructure.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceAlerts",
                columns: table => new
                {
                    DeviceAlertId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<string>(type: "TEXT", nullable: false),
                    DateRecorded = table.Column<string>(type: "TEXT", nullable: false),
                    DateLastRecorded = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceSerialNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<double>(type: "REAL", nullable: false),
                    DataNonNumeric = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAlerts", x => x.DeviceAlertId);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SharedSecret = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "TEXT", nullable: false),
                    FirstRegistrationDate = table.Column<string>(type: "TEXT", nullable: true),
                    LastRegistrationDate = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.SerialNumber);
                });

            migrationBuilder.CreateTable(
                name: "DeviceReadings",
                columns: table => new
                {
                    DeviceReadingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Temperature = table.Column<double>(type: "decimal(5, 2)", nullable: false),
                    Humidity = table.Column<double>(type: "decimal(5, 2)", nullable: false),
                    CarbonMonoxide = table.Column<double>(type: "decimal(5, 2)", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedDateTime = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedDateTime = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceSerialNumber = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceReadings", x => x.DeviceReadingId);
                    table.ForeignKey(
                        name: "FK_DeviceReadings_Devices_DeviceSerialNumber",
                        column: x => x.DeviceSerialNumber,
                        principalTable: "Devices",
                        principalColumn: "SerialNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceRegistrations",
                columns: table => new
                {
                    DeviceRegistrationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceSerialNumber = table.Column<string>(type: "TEXT", nullable: false),
                    RegistrationDate = table.Column<string>(type: "TEXT", nullable: false),
                    TokenId = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceRegistrations", x => x.DeviceRegistrationId);
                    table.ForeignKey(
                        name: "FK_DeviceRegistrations_Devices_DeviceSerialNumber",
                        column: x => x.DeviceSerialNumber,
                        principalTable: "Devices",
                        principalColumn: "SerialNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceReadings_DeviceSerialNumber",
                table: "DeviceReadings",
                column: "DeviceSerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_DeviceSerialNumber",
                table: "DeviceRegistrations",
                column: "DeviceSerialNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceAlerts");

            migrationBuilder.DropTable(
                name: "DeviceReadings");

            migrationBuilder.DropTable(
                name: "DeviceRegistrations");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
