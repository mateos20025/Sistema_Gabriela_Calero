using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consultorio.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Pagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "estado_nuevo",
                table: "auditoria",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "configuracion_pagos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    precio_consulta = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    concepto_predeterminado = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tipo_cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    numero_cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    titular = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    identificacion_titular = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracion_pagos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cita_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    concepto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    numero_comprobante = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    comprobante_archivo_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    comprobante_tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    comprobante_tamano_bytes = table.Column<long>(type: "bigint", nullable: true),
                    comprobante_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    registrado_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resuelto_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resuelto_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_resolucion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagos", x => x.id);
                    table.CheckConstraint("ck_pagos_monto", "monto > 0");
                    table.CheckConstraint("ck_pagos_transferencia_con_comprobante", "metodo <> 'Transferencia' OR comprobante_hash IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_pagos_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagos_usuarios_registrado_por_id",
                        column: x => x.registrado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagos_usuarios_resuelto_por_id",
                        column: x => x.resuelto_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pagos_estado_creado_en",
                table: "pagos",
                columns: new[] { "estado", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_pagos_paciente_id",
                table: "pagos",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_registrado_por_id",
                table: "pagos",
                column: "registrado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_resuelto_por_id",
                table: "pagos",
                column: "resuelto_por_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagos_comprobante_hash_vigente",
                table: "pagos",
                column: "comprobante_hash",
                unique: true,
                filter: "comprobante_hash IS NOT NULL AND estado <> 'Anulado'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracion_pagos");

            migrationBuilder.DropTable(
                name: "pagos");

            migrationBuilder.DropColumn(
                name: "estado_nuevo",
                table: "auditoria");
        }
    }
}
