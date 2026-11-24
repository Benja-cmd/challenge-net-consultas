using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultasPlan.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestringirBorradosEnCascada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Afiliados_AfiliadoId",
                table: "Consultas");

            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Profesionales_ProfesionalId",
                table: "Consultas");

            migrationBuilder.DropForeignKey(
                name: "FK_Profesionales_Sedes_SedeId",
                table: "Profesionales");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Afiliados_AfiliadoId",
                table: "Consultas",
                column: "AfiliadoId",
                principalTable: "Afiliados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Profesionales_ProfesionalId",
                table: "Consultas",
                column: "ProfesionalId",
                principalTable: "Profesionales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Profesionales_Sedes_SedeId",
                table: "Profesionales",
                column: "SedeId",
                principalTable: "Sedes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Afiliados_AfiliadoId",
                table: "Consultas");

            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Profesionales_ProfesionalId",
                table: "Consultas");

            migrationBuilder.DropForeignKey(
                name: "FK_Profesionales_Sedes_SedeId",
                table: "Profesionales");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Afiliados_AfiliadoId",
                table: "Consultas",
                column: "AfiliadoId",
                principalTable: "Afiliados",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Profesionales_ProfesionalId",
                table: "Consultas",
                column: "ProfesionalId",
                principalTable: "Profesionales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Profesionales_Sedes_SedeId",
                table: "Profesionales",
                column: "SedeId",
                principalTable: "Sedes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
