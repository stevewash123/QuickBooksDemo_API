using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuickBooksDemo.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickBooksSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerType = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    QuickBooksId = table.Column<string>(type: "TEXT", nullable: true),
                    QuickBooksSyncDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Specialties = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    JobType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    QuotedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedTechnicianId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Technicians_AssignedTechnicianId",
                        column: x => x.AssignedTechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaborHours = table.Column<int>(type: "INTEGER", nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineItems_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "CustomerType", "Email", "Name", "Notes", "Phone", "QuickBooksId", "QuickBooksSyncDate" },
                values: new object[,]
                {
                    { "cust_001", "123 Main St, Springfield", "Residential", "john@email.com", "John Smith", "Prefers morning appointments", "555-0100", null, null },
                    { "cust_002", "456 Industrial Blvd, Springfield", "Commercial", "facilities@acme.com", "Acme Manufacturing", "Requires safety check-in at front desk", "555-0200", null, null }
                });

            migrationBuilder.InsertData(
                table: "Technicians",
                columns: new[] { "Id", "Active", "Email", "Name", "Phone", "Specialties" },
                values: new object[,]
                {
                    { "tech_001", true, "mike@electricco.com", "Mike Johnson", "555-0301", "[\"residential\",\"service_calls\"]" },
                    { "tech_002", true, "sarah@electricco.com", "Sarah Chen", "555-0302", "[\"commercial\",\"high_voltage\",\"installation\"]" }
                });

            migrationBuilder.InsertData(
                table: "Jobs",
                columns: new[] { "Id", "ActualAmount", "AssignedTechnicianId", "CompletedDate", "CreatedDate", "CustomerId", "Description", "JobType", "QuotedAmount", "ScheduledDate", "Status" },
                values: new object[,]
                {
                    { "CCE_0001", null, null, null, new DateTime(2025, 10, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "cust_001", "Install EV charger in garage", "Installation", 1200.00m, null, "Quote" },
                    { "CCE_0002", null, "tech_002", null, new DateTime(2025, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "cust_002", "Emergency - partial power outage in building", "ServiceCall", 500.00m, new DateTime(2025, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "InProgress" },
                    { "CCE_0003", 275.00m, "tech_001", new DateTime(2025, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "cust_001", "Replace faulty outlets in kitchen", "Repair", 250.00m, new DateTime(2025, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Completed" }
                });

            migrationBuilder.InsertData(
                table: "LineItems",
                columns: new[] { "Id", "Description", "JobId", "LaborCost", "LaborHours", "MaterialCost" },
                values: new object[,]
                {
                    { "item_001", "Level 2 EV Charger", "CCE_0001", 400.00m, 4, 600.00m },
                    { "item_002", "Electrical panel upgrade", "CCE_0001", 200.00m, 2, 150.00m },
                    { "item_003", "Diagnose and repair circuit", "CCE_0002", 300.00m, 3, 100.00m },
                    { "item_004", "Replace 4 GFCI outlets", "CCE_0003", 195.00m, 2, 80.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_AssignedTechnicianId",
                table: "Jobs",
                column: "AssignedTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CustomerId",
                table: "Jobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_JobId",
                table: "LineItems",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineItems");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Technicians");
        }
    }
}
