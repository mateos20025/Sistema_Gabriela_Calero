using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consultorio.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Pacientes_y_Auditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    campos = table.Column<string[]>(type: "text[]", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cedula = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    correo_electronico = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    consentimiento_aceptado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consentimiento_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consentimiento_origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    deshabilitado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacientes", x => x.id);
                    table.CheckConstraint("ck_pacientes_cedula", "cedula ~ '^[0-9]{10}$'");
                    table.CheckConstraint("ck_pacientes_telefono", "telefono ~ '^\\+593[2-9][0-9]{7,8}$'");
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_entidad_entidad_id_creado_en",
                table: "auditoria",
                columns: new[] { "entidad", "entidad_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_usuario_id",
                table: "auditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_apellidos_nombres",
                table: "pacientes",
                columns: new[] { "apellidos", "nombres" });

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_cedula",
                table: "pacientes",
                column: "cedula",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "pacientes");
        }
    }
}
