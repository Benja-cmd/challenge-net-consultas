using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultasPlan.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceUnicoDniAfiliado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_DNI",
                table: "Afiliados",
                column: "DNI",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Afiliados_DNI",
                table: "Afiliados");
        }
    }
}
