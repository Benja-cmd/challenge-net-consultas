using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultasPlan.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPoliticasDeCupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoliticasCupo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Plan = table.Column<string>(type: "TEXT", nullable: false),
                    Especialidad = table.Column<string>(type: "TEXT", nullable: false),
                    Modalidad = table.Column<string>(type: "TEXT", nullable: false),
                    LimiteAnual = table.Column<int>(type: "INTEGER", nullable: false),
                    LimiteMensual = table.Column<int>(type: "INTEGER", nullable: false),
                    InasistenciaConsumeCupo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasCupo", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PoliticasCupo",
                columns: new[] { "Id", "Especialidad", "InasistenciaConsumeCupo", "LimiteAnual", "LimiteMensual", "Modalidad", "Plan" },
                values: new object[] { 1, "Nutrición", true, 6, 1, "Presencial", "Plan Integral" });

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasCupo_Plan_Especialidad_Modalidad",
                table: "PoliticasCupo",
                columns: new[] { "Plan", "Especialidad", "Modalidad" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoliticasCupo");
        }
    }
}
